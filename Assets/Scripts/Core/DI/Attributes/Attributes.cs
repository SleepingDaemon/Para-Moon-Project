using System;

namespace ParaMoon
{
    /// <summary>
    /// Marks a field or property for dependency injection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public class InjectAttribute : Attribute
    {
        /// <summary>
        /// If true, the injector will not throw an exception if the service is not available.
        /// </summary>
        public bool Optional { get; }

        /// <summary>
        /// Marks a dependency to be injected.
        /// </summary>
        /// <param name="optional">If true, the injector will not throw an exception if the service is not available.</param>
        public InjectAttribute(bool optional = false)
        {
            Optional = optional;
        }
    }

    /// <summary>
    /// Marks a class as injectable, allowing its dependencies to be resolved automatically.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class InjectableAttribute : Attribute
    {
    }
}