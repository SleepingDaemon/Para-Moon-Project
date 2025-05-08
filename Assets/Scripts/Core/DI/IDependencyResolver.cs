using System;

namespace ParaMoon
{
    /// <summary>
    /// Interface for resolving dependencies.
    /// </summary>
    public interface IDependencyResolver
    {
        /// <summary>
        /// Resolves a dependency of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of dependency to resolve.</typeparam>
        /// <returns>The resolved dependency.</returns>
        T Resolve<T>() where T : class;

        /// <summary>
        /// Tries to resolve a dependency of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of dependency to resolve.</typeparam>
        /// <param name="result">The resolved dependency if successful.</param>
        /// <returns>True if the dependency was resolved, false otherwise.</returns>
        bool TryResolve<T>(out T result) where T : class;

        /// <summary>
        /// Resolves a dependency of the specified type.
        /// </summary>
        /// <param name="type">The type of dependency to resolve.</param>
        /// <returns>The resolved dependency.</returns>
        object Resolve(Type type);

        /// <summary>
        /// Tries to resolve a dependency of the specified type.
        /// </summary>
        /// <param name="type">The type of dependency to resolve.</param>
        /// <param name="result">The resolved dependency if successful.</param>
        /// <returns>True if the dependency was resolved, false otherwise.</returns>
        bool TryResolve(Type type, out object result);
    }
}