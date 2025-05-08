using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParaMoon
{
    /// <summary>
    /// A robust Service Locator pattern implementation for handling cross-references between different scenes.
    /// Allows components to register and retrieve services without direct dependencies.
    /// </summary>
    public class ServiceLocator
    {
        #region Singleton Implementation

        private static ServiceLocator _instance;
        private static readonly object _lock = new object();

        public static ServiceLocator Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new ServiceLocator();
                    }
                    return _instance;
                }
            }
        }

        #endregion

        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private readonly Dictionary<Type, List<ServiceRegisteredCallback>> _pendingCallbacks = new Dictionary<Type, List<ServiceRegisteredCallback>>();
        private readonly HashSet<Type> _initializedServices = new HashSet<Type>();

        /// <summary>
        /// Delegate called when a service is registered.
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        /// <param name="service">Service instance</param>
        public delegate void ServiceRegisteredCallback<T>(T service);
        
        /// <summary>
        /// Delegate for non-generic service registration callbacks.
        /// </summary>
        private delegate void ServiceRegisteredCallback(object service);
        
        /// <summary>
        /// Registers a service with the locator.
        /// </summary>
        /// <typeparam name="T">Type of service to register</typeparam>
        /// <param name="service">Instance of the service</param>
        /// <param name="initialize">Whether to mark the service as initialized immediately</param>
        /// <exception cref="ArgumentNullException">Thrown if service is null</exception>
        public void RegisterService<T>(T service, bool initialize = true) where T : class
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            Type type = typeof(T);
            
            // Register service
            _services[type] = service;

            // Mark as initialized if specified
            if (initialize)
                _initializedServices.Add(type);

            // Notify any callbacks waiting for this service
            if (_pendingCallbacks.TryGetValue(type, out List<ServiceRegisteredCallback> callbacks))
            {
                foreach (var callback in callbacks)
                {
                    callback(service);
                }
                _pendingCallbacks.Remove(type);
            }
            
            Debug.Log($"[ServiceLocator] Registered service: {typeof(T).Name}");
        }

        /// <summary>
        /// Gets a registered service.
        /// </summary>
        /// <typeparam name="T">Type of service to get</typeparam>
        /// <returns>Instance of the service</returns>
        /// <exception cref="InvalidOperationException">Thrown if service is not registered</exception>
        public T GetService<T>() where T : class
        {
            Type type = typeof(T);
            
            if (_services.TryGetValue(type, out object service))
                return (T)service;

            throw new InvalidOperationException($"Service of type {typeof(T).Name} has not been registered.");
        }
        
        /// <summary>
        /// Tries to get a registered service without throwing an exception.
        /// </summary>
        /// <typeparam name="T">Type of service to get</typeparam>
        /// <param name="service">Output service parameter</param>
        /// <returns>True if service was found, false otherwise</returns>
        public bool TryGetService<T>(out T service) where T : class
        {
            Type type = typeof(T);
            
            if (_services.TryGetValue(type, out object serviceObj))
            {
                service = (T)serviceObj;
                return true;
            }

            service = null;
            return false;
        }
        
        /// <summary>
        /// Checks if a service is registered.
        /// </summary>
        /// <typeparam name="T">Type of service to check</typeparam>
        /// <returns>True if service is registered</returns>
        public bool IsServiceRegistered<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Checks if a service is initialized.
        /// </summary>
        /// <typeparam name="T">Type of service to check</typeparam>
        /// <returns>True if service is initialized</returns>
        public bool IsServiceInitialized<T>() where T : class
        {
            return _initializedServices.Contains(typeof(T));
        }

        /// <summary>
        /// Marks a service as initialized.
        /// </summary>
        /// <typeparam name="T">Type of service to mark as initialized</typeparam>
        public void MarkServiceInitialized<T>() where T : class
        {
            _initializedServices.Add(typeof(T));
        }

        /// <summary>
        /// Unregisters a service.
        /// </summary>
        /// <typeparam name="T">Type of service to unregister</typeparam>
        /// <returns>True if service was unregistered, false if it wasn't registered</returns>
        public bool UnregisterService<T>() where T : class
        {
            Type type = typeof(T);
            
            if (_services.ContainsKey(type))
            {
                _services.Remove(type);
                _initializedServices.Remove(type);
                Debug.Log($"[ServiceLocator] Unregistered service: {typeof(T).Name}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Executes an action when a service becomes available. If the service is already
        /// registered, the callback is executed immediately.
        /// </summary>
        /// <typeparam name="T">Type of service</typeparam>
        /// <param name="callback">Action to execute when service is available</param>
        public void WhenAvailable<T>(ServiceRegisteredCallback<T> callback) where T : class
        {
            Type type = typeof(T);
            
            if (_services.TryGetValue(type, out object service))
            {
                // Service is already registered, call immediately
                callback((T)service);
            }
            else
            {
                // Add to pending callbacks
                if (!_pendingCallbacks.TryGetValue(type, out List<ServiceRegisteredCallback> callbacks))
                {
                    callbacks = new List<ServiceRegisteredCallback>();
                    _pendingCallbacks[type] = callbacks;
                }
                
                callbacks.Add((serviceObj) => callback((T)serviceObj));
            }
        }

        public T GetOrCreateService<T>() where T : class, new()
        {
            if (!TryGetService<T>(out var service))
            {
                service = new T();
                RegisterService(service);
            }
            return service;
        }

        /// <summary>
        /// Clears all registered services. Useful during scene transitions or application shutdown.
        /// </summary>
        public void Clear()
        {
            _services.Clear();
            _pendingCallbacks.Clear();
            _initializedServices.Clear();
            Debug.Log("[ServiceLocator] All services cleared");
        }
    }
}
