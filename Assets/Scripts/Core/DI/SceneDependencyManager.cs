using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParaMoon
{

    public class SceneDependencyManager : ServiceBehaviour<SceneDependencyManager>
    {
        [SerializeField] private bool _debugMode = false;

        // Track scene-specific objects by type and ID
        private Dictionary<string, Dictionary<Type, Dictionary<string, object>>> _sceneObjects = new();

        // Track delayed injections
        private Dictionary<string, List<DelayedInjection>> _pendingInjections = new();

        // Track objects waiting for specific scene types
        private Dictionary<Type, List<Action<object>>> _typeWaiters = new();

        private struct DelayedInjection
        {
            public object Target;
            public string FieldName;
            public Type FieldType;
            public string ObjectId;
        }

        protected override void Awake()
        {
            base.Awake();

            // Listen for scene injection completion events
            if (ServiceLocator.Instance.TryGetService<EventBus>(out var eventBus))
            {
                eventBus.Subscribe<SceneInjectionCompletedEvent>(OnSceneInjectionCompleted);
            }
        }

        private void OnSceneInjectionCompleted(SceneInjectionCompletedEvent evt)
        {
            ProcessPendingInjections(evt.SceneName);
        }

        /// <summary>
        /// Register a scene-specific object that can be referenced by objects in other scenes
        /// </summary>
        public void RegisterSceneObject<T>(T obj, string objectId, string sceneName) where T : class
        {
            if (string.IsNullOrEmpty(objectId))
            {
                Debug.LogError("[CrossSceneReferenceManager] Cannot register object with null or empty ID");
                return;
            }

            // Initialize dictionaries if needed
            if (!_sceneObjects.TryGetValue(sceneName, out var typeDict))
            {
                typeDict = new Dictionary<Type, Dictionary<string, object>>();
                _sceneObjects[sceneName] = typeDict;
            }

            var type = typeof(T);
            if (!typeDict.TryGetValue(type, out var objectDict))
            {
                objectDict = new Dictionary<string, object>();
                typeDict[type] = objectDict;
            }

            // Store the object
            objectDict[objectId] = obj;

            if (_debugMode)
                Debug.Log($"[CrossSceneReferenceManager] Registered {type.Name} '{objectId}' in scene '{sceneName}'");

            // Process any waiters for this type
            if (_typeWaiters.TryGetValue(type, out var waiters))
            {
                foreach (var waiter in waiters)
                {
                    waiter?.Invoke(obj);
                }
                _typeWaiters.Remove(type);
            }
        }

        /// <summary>
        /// Get a registered object from any scene
        /// </summary>
        public T GetSceneObject<T>(string objectId) where T : class
        {
            var type = typeof(T);

            // Search through all scenes
            foreach (var sceneEntry in _sceneObjects)
            {
                if (sceneEntry.Value.TryGetValue(type, out var objectDict) &&
                    objectDict.TryGetValue(objectId, out var obj))
                {
                    return obj as T;
                }
            }

            if (_debugMode)
                Debug.LogWarning($"[CrossSceneReferenceManager] Could not find {type.Name} with ID '{objectId}' in any scene");

            return null;
        }

        /// <summary>
        /// Get a registered object from a specific scene
        /// </summary>
        public T GetSceneObject<T>(string objectId, string sceneName) where T : class
        {
            if (_sceneObjects.TryGetValue(sceneName, out var typeDict) &&
                typeDict.TryGetValue(typeof(T), out var objectDict) &&
                objectDict.TryGetValue(objectId, out var obj))
            {
                return obj as T;
            }

            if (_debugMode)
                Debug.LogWarning($"[CrossSceneReferenceManager] Could not find {typeof(T).Name} with ID '{objectId}' in scene '{sceneName}'");

            return null;
        }

        /// <summary>
        /// Request an injection when an object becomes available in a specific scene
        /// </summary>
        public void RequestDelayedInjection<T>(object target, string fieldName, string objectId, string sceneName) where T : class
        {
            if (!_pendingInjections.TryGetValue(sceneName, out var injections))
            {
                injections = new List<DelayedInjection>();
                _pendingInjections[sceneName] = injections;
            }

            injections.Add(new DelayedInjection
            {
                Target = target,
                FieldName = fieldName,
                FieldType = typeof(T),
                ObjectId = objectId
            });

            if (_debugMode)
                Debug.Log($"[CrossSceneReferenceManager] Added delayed injection of {typeof(T).Name} '{objectId}' to {target.GetType().Name}.{fieldName}");
        }

        /// <summary>
        /// Execute when an object of a specific type becomes available in any scene
        /// </summary>
        public void WhenAvailable<T>(Action<T> callback) where T : class
        {
            var type = typeof(T);

            // Check if we already have this type registered
            foreach (var sceneEntry in _sceneObjects)
            {
                if (sceneEntry.Value.TryGetValue(type, out var objectDict) && objectDict.Count > 0)
                {
                    // Use the first one we find
                    foreach (var obj in objectDict.Values)
                    {
                        callback?.Invoke(obj as T);
                        return;
                    }
                }
            }

            // Otherwise, add to waiters
            if (!_typeWaiters.TryGetValue(type, out var waiters))
            {
                waiters = new List<Action<object>>();
                _typeWaiters[type] = waiters;
            }

            waiters.Add((obj) => callback?.Invoke(obj as T));
        }

        private void ProcessPendingInjections(string sceneName)
        {
            if (!_pendingInjections.TryGetValue(sceneName, out var injections))
                return;

            if (_debugMode)
                Debug.Log($"[CrossSceneReferenceManager] Processing {injections.Count} pending injections for scene '{sceneName}'");

            // Process all pending injections for this scene
            for (int i = injections.Count - 1; i >= 0; i--)
            {
                var injection = injections[i];

                // Find the object in the scene
                if (_sceneObjects.TryGetValue(sceneName, out var typeDict) &&
                    typeDict.TryGetValue(injection.FieldType, out var objectDict) &&
                    objectDict.TryGetValue(injection.ObjectId, out var obj))
                {
                    // Perform the injection via reflection
                    var targetType = injection.Target.GetType();
                    var field = targetType.GetField(injection.FieldName,
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic);

                    if (field != null)
                    {
                        field.SetValue(injection.Target, obj);

                        if (_debugMode)
                            Debug.Log($"[CrossSceneReferenceManager] Injected {injection.FieldType.Name} '{injection.ObjectId}' into {targetType.Name}.{injection.FieldName}");

                        // Remove this injection from the list
                        injections.RemoveAt(i);
                    }
                    else
                    {
                        Debug.LogError($"[CrossSceneReferenceManager] Could not find field {injection.FieldName} on {targetType.Name}");
                    }
                }
            }

            // If all injections are processed, remove the scene entry
            if (injections.Count == 0)
            {
                _pendingInjections.Remove(sceneName);
            }
        }

        /// <summary>
        /// Clean up references for a scene that's being unloaded
        /// </summary>
        public void UnregisterScene(string sceneName)
        {
            if (_sceneObjects.ContainsKey(sceneName))
            {
                _sceneObjects.Remove(sceneName);

                if (_debugMode)
                    Debug.Log($"[CrossSceneReferenceManager] Unregistered all objects for scene '{sceneName}'");
            }

            if (_pendingInjections.ContainsKey(sceneName))
            {
                _pendingInjections.Remove(sceneName);
            }
        }
    }
}