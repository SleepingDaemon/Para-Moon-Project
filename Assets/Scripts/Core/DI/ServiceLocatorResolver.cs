using System;
using UnityEngine;

namespace ParaMoon
{
    /// <summary>
    /// Resolves dependencies using the ServiceLocator.
    /// </summary>
    public class ServiceLocatorResolver : IDependencyResolver
    {
        private readonly ServiceLocator _serviceLocator;

        public ServiceLocatorResolver(ServiceLocator serviceLocator)
        {
            _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
        }

        public T Resolve<T>() where T : class
        {
            return _serviceLocator.GetService<T>();
        }

        public bool TryResolve<T>(out T result) where T : class
        {
            return _serviceLocator.TryGetService(out result);
        }

        public object Resolve(Type type)
        {
            // Use reflection to call the generic method
            try
            {
                var method = typeof(ServiceLocator).GetMethod("GetService").MakeGenericMethod(type);
                return method.Invoke(_serviceLocator, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to resolve service of type {type.Name}: {ex.Message}");
                throw new InvalidOperationException($"Could not resolve service of type {type.Name}", ex);
            }
        }

        public bool TryResolve(Type type, out object result)
        {
            try
            {
                var method = typeof(ServiceLocator).GetMethod("TryGetService").MakeGenericMethod(type);
                var parameters = new object[] { null };
                bool success = (bool)method.Invoke(_serviceLocator, parameters);
                result = parameters[0];
                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to resolve service of type {type.Name}: {ex.Message}");
                result = null;
                return false;
            }
        }
    }
}