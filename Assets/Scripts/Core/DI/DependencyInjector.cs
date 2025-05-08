using System;
using System.Reflection;
using UnityEngine;

namespace ParaMoon
{
    /// <summary>
    /// Marks a method to be called during the injection process with its parameters injected.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class InjectMethodAttribute : Attribute
    {
    }

    /// <summary>
    /// Handles dependency injection for objects.
    /// </summary>
    public static class DependencyInjector
    {
        private static IDependencyResolver _resolver;

        /// <summary>
        /// Initializes the injector with a resolver.
        /// </summary>
        /// <param name="resolver">The resolver to use for resolving dependencies.</param>
        public static void Initialize(IDependencyResolver resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        /// <summary>
        /// Gets the current resolver.
        /// </summary>
        public static IDependencyResolver Resolver
        {
            get
            {
                if (_resolver == null)
                {
                    // Default to ServiceLocator if not initialized
                    _resolver = new ServiceLocatorResolver(ServiceLocator.Instance);
                }
                return _resolver;
            }
        }

        /// <summary>
        /// Injects dependencies into an object.
        /// </summary>
        /// <param name="target">The object to inject into.</param>
        public static void InjectInto(object target)
        {
            InjectInto(target, new InjectionContext());
        }

        private static void InjectInto(object target, InjectionContext context)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            // Prevent circular dependencies and stack overflows
            context.InjectionDepth++;
            if (context.InjectionDepth > InjectionContext.MaxInjectionDepth)
            {
                throw new InvalidOperationException($"Maximum injection depth of {InjectionContext.MaxInjectionDepth} exceeded. Possible circular dependency detected.");
            }

            Type targetType = target.GetType();

            // Add to resolved instances to prevent circular dependencies
            context.ResolvedInstances[targetType] = target;

            // Inject into fields
            InjectFields(target, targetType, context);

            // Inject into properties
            InjectProperties(target, targetType, context);

            // Call [InjectMethod] methods
            InjectMethods(target, targetType, context);

            context.InjectionDepth--;
        }

        private static void InjectFields(object target, Type targetType, InjectionContext context)
        {
            var fields = targetType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                var injectAttr = field.GetCustomAttribute<InjectAttribute>();
                if (injectAttr == null) continue;

                Type fieldType = field.FieldType;

                bool success = Resolver.TryResolve(fieldType, out object dependency);

                if (success)
                {
                    field.SetValue(target, dependency);
                }
                else if (!injectAttr.Optional)
                {
                    throw new InvalidOperationException($"Failed to resolve dependency of type {fieldType.Name} for field {field.Name} in {targetType.Name}");
                }
            }
        }

        private static void InjectProperties(object target, Type targetType, InjectionContext context)
        {
            var properties = targetType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var property in properties)
            {
                var injectAttr = property.GetCustomAttribute<InjectAttribute>();
                if (injectAttr == null) continue;

                if (!property.CanWrite)
                {
                    Debug.LogWarning($"Cannot inject into read-only property {property.Name} in {targetType.Name}");
                    continue;
                }

                Type propertyType = property.PropertyType;

                bool success = Resolver.TryResolve(propertyType, out object dependency);

                if (success)
                {
                    property.SetValue(target, dependency);
                }
                else if (!injectAttr.Optional)
                {
                    throw new InvalidOperationException($"Failed to resolve dependency of type {propertyType.Name} for property {property.Name} in {targetType.Name}");
                }
            }
        }

        private static void InjectMethods(object target, Type targetType, InjectionContext context)
        {
            var methods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var method in methods)
            {
                var injectMethodAttr = method.GetCustomAttribute<InjectMethodAttribute>();
                if (injectMethodAttr == null) continue;

                var parameters = method.GetParameters();
                var arguments = new object[parameters.Length];

                bool allResolved = true;

                for (int i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    var parameterType = parameter.ParameterType;

                    var paramInjectAttr = parameter.GetCustomAttribute<InjectAttribute>();
                    bool isOptional = paramInjectAttr?.Optional ?? false;

                    bool success = Resolver.TryResolve(parameterType, out object dependency);

                    if (success)
                    {
                        arguments[i] = dependency;
                    }
                    else if (!isOptional)
                    {
                        allResolved = false;
                        throw new InvalidOperationException($"Failed to resolve dependency of type {parameterType.Name} for parameter {parameter.Name} in method {method.Name} of {targetType.Name}");
                    }
                }

                if (allResolved)
                {
                    method.Invoke(target, arguments);
                }
            }
        }

        /// <summary>
        /// Creates an instance of a type and injects its dependencies.
        /// </summary>
        /// <typeparam name="T">The type to create.</typeparam>
        /// <returns>The created instance with dependencies injected.</returns>
        public static T Create<T>() where T : class, new()
        {
            var instance = new T();
            InjectInto(instance);
            return instance;
        }
    }
}