using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParaMoon
{
    public class ReferenceRegistry : ServiceBehaviour<ReferenceRegistry>
    {
        // Dictionary to hold references by type and identifier
        readonly Dictionary<Type, Dictionary<string, UnityEngine.Object>> _references = new();

        public event Action<Type, string> OnReferenceRegistered;
        public event Action<Type, string> OnReferenceUnregistered;

        /// <summary>
        /// Get a registered reference by type and optional identifier
        /// </summary>
        public void RegisterReference<T>(T reference, string identifier = "Default") where T : UnityEngine.Object
        {
            Type type = typeof(T);

            // Create dictionary for this type if it doesn't exist
            if (!_references.TryGetValue(type, out var typeDict))
            {
                typeDict = new Dictionary<string, UnityEngine.Object>();
                _references[type] = typeDict;
            }

            // Register the reference
            typeDict[identifier] = reference;

            Debug.Log($"[ReferenceRegistry] Registered {type.Name}:{identifier}");
            OnReferenceRegistered?.Invoke(type, identifier);
        }

        /// <summary>
        /// Get a registered reference by type and optional identifier
        /// </summary>
        public T GetReference<T>(string identifier = "default") where T : UnityEngine.Object
        {
            Type type = typeof(T);

            if (_references.TryGetValue(type, out var typeDict) &&
                typeDict.TryGetValue(identifier, out var reference))
            {
                return reference as T;
            }

            return null;
        }

        /// <summary>
        /// Check if a reference exists
        /// </summary>
        public bool HasReference<T>(string identifier = "default") where T : UnityEngine.Object
        {
            Type type = typeof(T);
            return _references.TryGetValue(type, out var typeDict) && typeDict.ContainsKey(identifier);
        }

        /// <summary>
        /// Unregister a reference
        /// </summary>
        public void UnregisterReference<T>(string identifier = "default") where T : UnityEngine.Object
        {
            Type type = typeof(T);

            if (_references.TryGetValue(type, out var typeDict) && typeDict.ContainsKey(identifier))
            {
                typeDict.Remove(identifier);
                Debug.Log($"[ReferenceRegistry] Unregistered {type.Name}:{identifier}");
                OnReferenceUnregistered?.Invoke(type, identifier);
            }
        }

        /// <summary>
        /// Clear all references of a specific type
        /// </summary>
        public void ClearReferences<T>() where T : UnityEngine.Object
        {
            Type type = typeof(T);
            if (_references.ContainsKey(type))
            {
                _references.Remove(type);
            }
        }
    }
}
