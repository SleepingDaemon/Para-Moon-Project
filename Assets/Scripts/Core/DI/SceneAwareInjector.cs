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
            foreach (var rootObject in rootObjects)
            {
                injectableCount += AddInjectorsRecursively(rootObject);

                // Yield occasionally to avoid freezing for large scenes
                if (injectableCount % 10 == 0)
                    yield return null;
            }

            // Let the scene finish initializing
            yield return new WaitForEndOfFrame();

            // Second pass: inject dependencies
            foreach (var rootObject in rootObjects)
            {
                InjectDependenciesRecursively(rootObject);
                yield return null;
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
            int count = 0;

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
            if (needsInjector && gameObject.GetComponent<MonoBehaviourInjector>() == null)
            {
                gameObject.AddComponent<MonoBehaviourInjector>();
            }

            // Process children
            foreach (Transform child in gameObject.transform)
            {
                count += AddInjectorsRecursively(child.gameObject);
            }

            return count;
        }

        private void InjectDependenciesRecursively(GameObject gameObject)
        {
            // Perform injection if this object has an injector
            var injector = gameObject.GetComponent<MonoBehaviourInjector>();
            if (injector != null)
            {
                injector.InjectAll();
            }

            // Process children
            foreach (Transform child in gameObject.transform)
            {
                InjectDependenciesRecursively(child.gameObject);
            }
        }
    }
}