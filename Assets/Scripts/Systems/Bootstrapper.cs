using UnityEngine;
using System.Collections;
using System;

namespace ParaMoon
{
    /* 
     * * The Bootstrapper class is responsible for initializing core services and loading the UI scene.
     * It ensures that essential services are registered and available before the game starts.
     * This class should be attached to a GameObject in the initial scene of the game.
     */
    [DefaultExecutionOrder(-9000)] 
    public class Bootstrapper : MonoBehaviour
    {
        [SerializeField] GameObject[] _coreServicePrefabs;
        [SerializeField] float _initializationTimeout = 10f;

        protected virtual void Awake()
        {
            RegisterCoreServices();
            StartCoroutine(WaitForServicesInitialization());
        }

        private void RegisterCoreServices()
        {
            Debug.Log($"[Bootstrapper] Registering {_coreServicePrefabs?.Length ?? 0} core services");

            // Instantiate core services if they aren't already available
            foreach (GameObject prefab in _coreServicePrefabs)
            {
                if (prefab == null)
                    continue;

                // If the prefab is a ServiceBehaviour, it will self-register
                // Otherwise we need to check if it's a service we should register manually
                if (!prefab.TryGetComponent<MonoBehaviour>(out var serviceComponent))
                {
                    Debug.LogError($"[Bootstrapper] Prefab {prefab.name} does not have a service component.");
                    continue;
                }

                // Use ServiceLocator to check if service already registered
                var serviceType = serviceComponent.GetType();
                var registerMethod = typeof(ServiceLocator).GetMethod("IsServiceRegistered").MakeGenericMethod(serviceType);
                bool isRegistered = (bool)registerMethod.Invoke(ServiceLocator.Instance, null);

                if (!isRegistered)
                {
                    var instance = Instantiate(prefab);
                    DontDestroyOnLoad(instance);
                    Debug.Log($"[Bootstrapper] Instantiated service: {prefab.name}");
                }
            }
        }

        private IEnumerator WaitForServicesInitialization()
        {
            float startTime = Time.time;
            bool allInitialized = false;

            while (!allInitialized && Time.time - startTime < _initializationTimeout)
            {
                allInitialized = true;

                // Check if all services are initialized
                foreach (GameObject prefab in _coreServicePrefabs)
                {
                    if (prefab == null)
                        continue;

                    if (!prefab.TryGetComponent<MonoBehaviour>(out var serviceComponent))
                        continue;

                    var serviceType = serviceComponent.GetType();
                    var genericMethod = typeof(ServiceLocator).GetMethod("IsServiceInitialized").MakeGenericMethod(serviceType);

                    // Skip checking prefabs that aren't ServiceBehaviours
                    if (genericMethod == null)
                        continue;

                    bool isInitialized = (bool)genericMethod.Invoke(ServiceLocator.Instance, null);

                    if (!isInitialized)
                    {
                        allInitialized = false;
                        break;
                    }
                }

                if (!allInitialized)
                    yield return null;
            }

            if (!allInitialized)
                Debug.LogWarning("[Bootstrapper] Service initialization timed out. Some services may not be ready.");
            else
                Debug.Log("[Bootstrapper] All services initialized successfully.");

            // Game is ready to start
            OnInitializationComplete();
        }

        protected virtual void OnInitializationComplete()
        {
            // Ensure UIManager is initialized AFTER UI scene is confirmed to be loaded
            if (ServiceLocator.Instance.TryGetService<SceneManagerService>(out var sceneManager))
            {
                if (sceneManager.IsSceneLoaded("GameUI"))
                    InitializeUIManager();
                else
                {
                    // Wait for the UI scene to load first, then initialize UIManager
                    sceneManager.LoadSceneAdditively("GameUI", () =>
                    {
                        InitializeUIManager();
                    });
                }
            }
            else
                InitializeUIManager();
        }

        private void InitializeUIManager()
        {
            if (ServiceLocator.Instance.TryGetService<UIManager>(out var uiManager))
            {
                Debug.Log("[Bootstrapper] Ensuring UIManager is initialized");

                if (uiManager == null)
                {
                    // Wait a frame to allow UI elements to register with ReferenceRegistry
                    StartCoroutine(DelayedUIManagerInitialization(uiManager));
                }
            }
            else
            {
                Debug.LogError("[Bootstrapper] UIManager not available after initialization.");
            }
        }

        private IEnumerator DelayedUIManagerInitialization(UIManager uiManager)
        {
            // Wait for two frames to allow UI elements to register
            yield return null;
            yield return null;

            // Now initialize the UI manager
            uiManager.Initialize();

            // Log confirmation
            Debug.Log("[Bootstrapper] UIManager initialized after delay");
        }
    }
}