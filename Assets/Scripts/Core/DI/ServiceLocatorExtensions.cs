namespace ParaMoon
{
    /// <summary>
    /// Extensions for ServiceLocator to support dependency injection.
    /// </summary>
    public static class ServiceLocatorExtensions
    {
        /// <summary>
        /// Creates and registers a service with its dependencies injected.
        /// </summary>
        /// <typeparam name="T">The type of service to create.</typeparam>
        /// <param name="serviceLocator">The service locator.</param>
        /// <param name="initialize">Whether to initialize the service immediately.</param>
        /// <returns>The created service instance.</returns>
        public static T CreateAndRegisterService<T>(this ServiceLocator serviceLocator, bool initialize = true) where T : class, new()
        {
            var service = DependencyInjector.Create<T>();
            serviceLocator.RegisterService(service, initialize);
            return service;
        }

        /// <summary>
        /// Gets a service, creating it if it doesn't exist.
        /// </summary>
        /// <typeparam name="T">The type of service to get or create.</typeparam>
        /// <param name="serviceLocator">The service locator.</param>
        /// <returns>The service instance.</returns>
        public static T GetOrCreateService<T>(this ServiceLocator serviceLocator) where T : class, new()
        {
            if (!serviceLocator.TryGetService<T>(out var service))
            {
                service = serviceLocator.CreateAndRegisterService<T>();
            }
            return service;
        }
    }
}