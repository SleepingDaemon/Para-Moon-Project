using System.Collections;
using UnityEngine;

namespace ParaMoon
{
    /// <summary>
    /// Extension to the Bootstrapper that adds DI support.
    /// </summary>
    public class BootstrapperExtension : Bootstrapper
    {
        [SerializeField] GameObject _uiManagerPrefab;

        protected override void Awake()
        {
            // Initialize the dependency injector with our ServiceLocator resolver
            DependencyInjector.Initialize(new ServiceLocatorResolver(ServiceLocator.Instance));

            base.Awake();
        }

        // After the existing WaitForServicesInitialization method
        // Add this method to process scene objects
        private IEnumerator ProcessDIForBootScene()
        {
            // Wait for a frame to ensure all services are registered
            yield return null;

            // Find all root objects in the Boot scene
            var rootObjects = gameObject.scene.GetRootGameObjects();

            // Add CrossSceneProcessor to important objects that need cross-scene references
            foreach (var rootObject in rootObjects)
            {
                // Skip the DI system objects
                if (rootObject.name.StartsWith("[DI-") ||
                    rootObject.name.StartsWith("[SceneDependency"))
                    continue;

                // Add CrossSceneProcessor if it doesn't have one
                if (rootObject.GetComponent<SceneDependencyProcessor>() == null)
                {
                    rootObject.AddComponent<SceneDependencyProcessor>();
                }
            }

            Debug.Log("[Bootstrapper] Added CrossSceneProcessors to Boot scene objects");
        }

        protected override void OnInitializationComplete()
        {
            // Add DI processing for the Boot scene
            StartCoroutine(ProcessDIForBootScene());

            // Manually instantiate and initialize UIManager
            StartCoroutine(ManuallyInitializeUIManager());

            // Don't call base.OnInitializationComplete() to prevent duplicate UIManager initialization
            // Instead, handle any other initialization from the base method if needed
        }

        private IEnumerator ManuallyInitializeUIManager()
        {
            // Wait for SceneManagerService to be available
            SceneManagerService sceneManager = null;
            while (sceneManager == null)
            {
                if (ServiceLocator.Instance.TryGetService<SceneManagerService>(out sceneManager))
                    break;
                yield return null;
            }

            // Ensure GameUI scene is loaded
            if (!sceneManager.IsSceneLoaded("GameUI"))
            {
                bool sceneLoaded = false;
                sceneManager.LoadSceneAdditively("GameUI", () => { sceneLoaded = true; });

                // Wait for scene to be loaded with timeout
                float timeout = Time.time + 5f;
                while (!sceneLoaded && Time.time < timeout)
                    yield return null;

                // Give the scene time to initialize
                yield return new WaitForSeconds(0.2f);
            }

            // Find UIManager prefab
            GameObject uiManagerPrefab = _uiManagerPrefab;
            if (uiManagerPrefab == null)
            {
                Debug.LogError("[BootstrapperExtension] UIManager prefab not found in Resources/Prefabs/Services");
                yield break;
            }

            // Check if UIManager already exists to avoid duplicates
            if (!ServiceLocator.Instance.TryGetService<UIManager>(out _))
            {
                // Instantiate UIManager
                GameObject uiManagerObj = Instantiate(uiManagerPrefab);
                uiManagerObj.name = "UIManager";
                DontDestroyOnLoad(uiManagerObj);

                // Add MonoBehaviourInjector to handle dependencies
                if (uiManagerObj.GetComponent<MonoBehaviourInjector>() == null)
                    uiManagerObj.AddComponent<MonoBehaviourInjector>();

                Debug.Log("[BootstrapperExtension] UIManager instantiated manually");

                // Wait a frame for the UIManager to register itself with ServiceLocator
                yield return null;
            }

            // Initialize the UIManager
            if (ServiceLocator.Instance.TryGetService<UIManager>(out var uiManager))
            {
                // Wait a bit more to ensure all scene objects are ready
                yield return new WaitForSeconds(0.1f);
                uiManager.Initialize();
                Debug.Log("[BootstrapperExtension] UIManager initialized manually");
            }
            else
            {
                Debug.LogError("[BootstrapperExtension] Failed to get UIManager instance after manual instantiation");
            }
        }
    }
}