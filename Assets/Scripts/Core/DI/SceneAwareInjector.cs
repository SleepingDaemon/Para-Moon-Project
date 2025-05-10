using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ParaMoon
{
    // Event for notifying when scene injection is complete
    public struct SceneInjectionCompletedEvent
    {
        public string SceneName;
    }

    [DefaultExecutionOrder(-9800)] // Earlier than Bootstrapper
    public class SceneAwareInjector : MonoBehaviour
    {
        [SerializeField] private bool _logInjectionActivity = true;

        private static SceneAwareInjector _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize the DI system
            DependencyInjector.Initialize(new ServiceLocatorResolver(ServiceLocator.Instance));

            // Subscribe to scene loading events
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                _instance = null;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.isLoaded)
            {
                if (_logInjectionActivity)
                    Debug.LogWarning($"[SceneAwareInjector] Scene '{scene.name}' is marked as not loaded. Skipping injection.");
                return;
            }

            if (_logInjectionActivity)
                Debug.Log($"[SceneAwareInjector] Scene '{scene.name}' loaded with mode: {mode}. Injecting dependencies...");

            // Process all root GameObjects in the newly loaded scene
            var rootObjects = scene.GetRootGameObjects();
            StartCoroutine(ProcessSceneObjects(scene.name, rootObjects));
        }

        private IEnumerator ProcessSceneObjects(string sceneName, GameObject[] rootObjects)
        {
            int injectableCount = 0;

            // First pass: add injectors to all objects with [Injectable] components
            for (int i = 0; i < rootObjects.Length; i++)
            {
                GameObject rootObject = rootObjects[i];
                if (rootObject != null)  // Add null check
                {
                    injectableCount += AddInjectorsRecursively(rootObject);

                    // Yield occasionally to avoid freezing for large scenes
                    if (injectableCount % 10 == 0)
                        yield return null;
                }
            }

            // Let the scene finish initializing
            yield return new WaitForEndOfFrame();

            // Second pass: inject dependencies
            for (int i = 0; i < rootObjects.Length; i++)
            {
                GameObject rootObject = rootObjects[i];
                if (rootObject != null)  // Add null check
                {
                    InjectDependenciesRecursively(rootObject);
                    yield return null;
                }
            }

            if (_logInjectionActivity)
                Debug.Log($"[SceneAwareInjector] Completed injection for scene '{sceneName}'. Found {injectableCount} injectable components.");

            // Notify any listeners that might need to know injection is complete for this scene
            if (ServiceLocator.Instance.TryGetService<EventBus>(out var eventBus))
            {
                eventBus.Publish(new SceneInjectionCompletedEvent
                {
                    SceneName = sceneName
                });
            }
        }

        private int AddInjectorsRecursively(GameObject gameObject)
        {
            if (gameObject == null)
                return 0;

            int count = 0;

            try
            {
                // Check components on this GameObject
                var components = gameObject.GetComponents<MonoBehaviour>();
                bool needsInjector = false;

                foreach (var component in components)
                {
                    if (component == null) continue;

                    var type = component.GetType();
                    if (System.Attribute.IsDefined(type, typeof(InjectableAttribute)))
                    {
                        needsInjector = true;
                        count++;
                        break;
                    }
                }

                // Add injector if needed
                if (needsInjector && gameObject != null && gameObject.GetComponent<MonoBehaviourInjector>() == null)
                {
                    gameObject.AddComponent<MonoBehaviourInjector>();
                }

                // Process children - with safety check
                if (gameObject != null && gameObject.transform != null)
                {
                    foreach (Transform child in gameObject.transform)
                    {
                        if (child != null)
                            count += AddInjectorsRecursively(child.gameObject);
                    }
                }
            }
            catch (System.Exception e)
            {
                // Log the error but don't let it crash the entire process
                Debug.LogError($"[SceneAwareInjector] Error processing GameObject: {e.Message}");
            }

            return count;
        }

        private void InjectDependenciesRecursively(GameObject gameObject)
        {
            // Add null check at the beginning
            if (gameObject == null)
                return;

            try
            {
                // Perform injection if this object has an injector
                var injector = gameObject.GetComponent<MonoBehaviourInjector>();
                if (injector != null)
                {
                    injector.InjectAll();
                }

                // Process children - with safety check
                if (gameObject != null && gameObject.transform != null)
                {
                    foreach (Transform child in gameObject.transform)
                    {
                        if (child != null)
                            InjectDependenciesRecursively(child.gameObject);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SceneAwareInjector] Error injecting dependencies: {e.Message}");
            }
        }
    }
}