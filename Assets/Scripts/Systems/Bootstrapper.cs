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

        [Inject] SceneManagerService _sceneManager;
        [Inject] UIManager _ui;

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

                // Check the component type for ServiceBehaviour
                var serviceComponent = prefab.GetComponent<MonoBehaviour>();
                if (serviceComponent == null)
                {
                    Debug.LogError($"[Bootstrapper] Prefab {prefab.name} does not have a service component.");
                    continue;
                }

                var serviceType = serviceComponent.GetType();

                // Use new DI system to check registration
                bool isRegistered = false;
                if (DependencyInjector.Resolver.TryResolve(serviceType, out _))
                    isRegistered = true;

                if (!isRegistered)
                {
                    var instance = Instantiate(prefab);
                    DontDestroyOnLoad(instance);

                    // The service will self-register through ServiceBehaviour<T>
                    Debug.Log($"[Bootstrapper] Instantiated service: {prefab.name}");

                    // Add MonoBehaviourInjector to ensure dependencies are injected
                    if (instance.GetComponent<MonoBehaviourInjector>() == null)
                        instance.AddComponent<MonoBehaviourInjector>();
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
            if (_sceneManager != null)
            {
                if (_sceneManager.IsSceneLoaded("GameUI"))
                    InitializeUIManager();
                else
                {
                    // Wait for the UI scene to load first, then initialize UIManager
                    _sceneManager.LoadSceneAdditively("GameUI", () =>
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
            ServiceLocator.Instance.WhenAvailable<UIManager>(ui =>
            {
                // Initialize UIManager
                if (ui != null)
                {
                    ui.Initialize();
                    Debug.Log("[Bootstrapper] UIManager initialized");
                }
                else
                    StartCoroutine(DelayedUIManagerInitialization(ui));
            });

            //StartCoroutine(DelayedUIManagerInitialization(_ui));
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