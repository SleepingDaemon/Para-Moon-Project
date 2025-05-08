using System;
using UnityEngine;

namespace ParaMoon
{
    /// <summary>
    /// Automatically adds injectors to GameObjects that have components marked with [Injectable].
    /// </summary>
    [DefaultExecutionOrder(-9500)] // Run before Bootstrapper
    public class AutoInjectExtension : MonoBehaviour
    {
        private static AutoInjectExtension _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // Create a persistent GameObject to hold the AutoInjectExtension
            if (_instance == null)
            {
                var go = new GameObject("[AutoInjectExtension]");
                _instance = go.AddComponent<AutoInjectExtension>();
                DontDestroyOnLoad(go);
            }
        }

        private void Awake()
        {
            // Set up the dependency injector with the service locator
            DependencyInjector.Initialize(new ServiceLocatorResolver(ServiceLocator.Instance));
        }

        private void OnEnable()
        {
            // Subscribe to scene loaded events to inject into new objects
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            // Find all root GameObjects in the loaded scene
            var rootObjects = scene.GetRootGameObjects();
            foreach (var rootObject in rootObjects)
            {
                ProcessGameObject(rootObject);
            }
        }

        private void ProcessGameObject(GameObject go)
        {
            // Check if any component on this object is marked as Injectable
            var components = go.GetComponents<MonoBehaviour>();
            bool needsInjector = false;

            foreach (var component in components)
            {
                if (component == null) continue;

                var type = component.GetType();
                if (Attribute.IsDefined(type, typeof(InjectableAttribute)))
                {
                    needsInjector = true;
                    break;
                }
            }

            // Add injector if needed
            if (needsInjector && go.GetComponent<MonoBehaviourInjector>() == null)
            {
                go.AddComponent<MonoBehaviourInjector>();
            }

            // Process children recursively
            foreach (Transform child in go.transform)
            {
                ProcessGameObject(child.gameObject);
            }
        }
    }
}