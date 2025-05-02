using UnityEngine;

namespace ParaMoon
{
    /// <summary>
    /// Base class for services that should automatically register with the ServiceLocator.
    /// Inherit from this class to create services that persist between scenes and can be
    /// accessed from anywhere in the application.
    /// </summary>
    /// <typeparam name="T">The type of service to register (should be the derived class type)</typeparam>
    public abstract class ServiceBehaviour<T> : MonoBehaviour where T : ServiceBehaviour<T>
    {
        [Tooltip("If true, this service will persist between scene loads")]
        [SerializeField] protected bool _dontDestroyOnLoad = true;

        [Tooltip("If true, this service will be initialized in Awake, otherwise it needs manual initialization")]
        [SerializeField] protected bool _autoInitialize = true;

        protected virtual void Awake()
        {
            // Make sure we don't have duplicate services
            if (ServiceLocator.Instance.IsServiceRegistered<T>())
            {
                Debug.LogWarning($"[ServiceBehaviour] Service {typeof(T).Name} already registered. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            // Don't destroy on load if specified
            if (_dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);

            // Register with service locator
            ServiceLocator.Instance.RegisterService((T)this, _autoInitialize);
            
            // Additional setup if needed
            OnServiceRegistered();
        }

        protected virtual void OnDestroy()
        {
            // Only unregister if this is the registered instance
            if (ServiceLocator.Instance.TryGetService<T>(out T service) && service == this)
            {
                ServiceLocator.Instance.UnregisterService<T>();
                OnServiceUnregistered();
            }
        }

        /// <summary>
        /// Override this method to perform additional setup after the service is registered.
        /// </summary>
        protected virtual void OnServiceRegistered() { }

        /// <summary>
        /// Override this method to perform cleanup when the service is unregistered.
        /// </summary>
        protected virtual void OnServiceUnregistered() { }

        /// <summary>
        /// Explicitly initialize the service if auto-initialize is disabled.
        /// </summary>
        public virtual void Initialize()
        {
            ServiceLocator.Instance.MarkServiceInitialized<T>();
        }
    }
}
