using System;
using System.Collections.Generic;

namespace ParaMoon
{
    /// <summary>
    /// Provides context for the injection process.
    /// </summary>
    public class InjectionContext
    {
        /// <summary>
        /// Map of already resolved instances to prevent circular dependencies.
        /// </summary>
        internal Dictionary<Type, object> ResolvedInstances { get; }

        /// <summary>
        /// Current depth of the injection to detect circular dependencies.
        /// </summary>
        internal int InjectionDepth { get; set; }

        /// <summary>
        /// Maximum allowed injection depth to prevent stack overflows.
        /// </summary>
        internal const int MaxInjectionDepth = 10;

        public InjectionContext()
        {
            ResolvedInstances = new Dictionary<Type, object>();
            InjectionDepth = 0;
        }
    }
}