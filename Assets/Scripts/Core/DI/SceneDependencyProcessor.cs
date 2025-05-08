using System;
using System.Reflection;
using UnityEngine;

namespace ParaMoon
{
    /// <summary>
    /// Processes cross-scene attributes and manages cross-scene references
    /// </summary>
    [DefaultExecutionOrder(-9500)] // After SceneAwareInjector but before other scripts
    public class SceneDependencyProcessor : MonoBehaviour
    {
        [SerializeField] private string _currentSceneName;

        private void Awake()
        {
            if (string.IsNullOrEmpty(_currentSceneName))
            {
                _currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            }

            // Find and register all [SceneExported] components in this GameObject and its children
            RegisterExportedComponents(gameObject);

            // Process all [SceneReference] fields in this GameObject and its children
            ProcessSceneReferences(gameObject);
        }

        private void OnDestroy()
        {
            // When this GameObject is destroyed, unregister from CrossSceneReferenceManager
            if (ServiceLocator.Instance.TryGetService<SceneDependencyManager>(out var manager))
            {
                if (gameObject.scene.isLoaded) // Check if the scene is being unloaded
                {
                    // Only unregister this specific GameObject's components
                    // (Scene-level unregistration happens in SceneManagerService)
                }
                else
                {
                    // If the entire scene is being unloaded, unregister all objects from this scene
                    manager.UnregisterScene(_currentSceneName);
                }
            }
        }

        private void RegisterExportedComponents(GameObject go)
        {
            // Check each component on this GameObject
            var components = go.GetComponents<MonoBehaviour>();

            foreach (var component in components)
            {
                if (component == null) continue;

                var type = component.GetType();
                var attr = type.GetCustomAttribute<SceneExportedAttribute>();

                if (attr != null)
                {
                    // Determine the ID to use
                    string id = !string.IsNullOrEmpty(attr.Id) ? attr.Id : go.name;

                    // Register with CrossSceneReferenceManager
                    if (ServiceLocator.Instance.TryGetService<SceneDependencyManager>(out var manager))
                    {
                        manager.RegisterSceneObject(component, id, _currentSceneName);
                    }
                }
            }

            // Process children recursively
            foreach (Transform child in go.transform)
            {
                RegisterExportedComponents(child.gameObject);
            }
        }

        private void ProcessSceneReferences(GameObject go)
        {
            var components = go.GetComponents<MonoBehaviour>();

            foreach (var component in components)
            {
                if (component == null) continue;

                var type = component.GetType();
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (var field in fields)
                {
                    var attr = field.GetCustomAttribute<SceneReferenceAttribute>();
                    if (attr == null) continue;

                    // Try to get the reference immediately
                    if (ServiceLocator.Instance.TryGetService<SceneDependencyManager>(out var manager))
                    {
                        if (string.IsNullOrEmpty(attr.SceneName))
                        {
                            // Search in all scenes
                            var method = typeof(SceneDependencyManager)
                                .GetMethod("GetSceneObject", new Type[] { typeof(string) })
                                .MakeGenericMethod(field.FieldType);

                            var result = method.Invoke(manager, new object[] { attr.TargetId });

                            if (result != null)
                            {
                                field.SetValue(component, result);
                            }
                            else if (!attr.Optional)
                            {
                                Debug.LogWarning($"[CrossSceneProcessor] Could not find {field.FieldType.Name} with ID '{attr.TargetId}' for {component.GetType().Name}.{field.Name}");
                            }
                        }
                        else
                        {
                            // First try to get from the specified scene
                            var method = typeof(SceneDependencyManager)
                                .GetMethod("GetSceneObject", new Type[] { typeof(string), typeof(string) })
                                .MakeGenericMethod(field.FieldType);

                            var result = method.Invoke(manager, new object[] { attr.TargetId, attr.SceneName });

                            if (result != null)
                            {
                                field.SetValue(component, result);
                            }
                            else
                            {
                                // If not found and the scene isn't loaded yet, request delayed injection
                                var delayedMethod = typeof(SceneDependencyManager)
                                    .GetMethod("RequestDelayedInjection")
                                    .MakeGenericMethod(field.FieldType);

                                delayedMethod.Invoke(manager, new object[] {
                                    component, field.Name, attr.TargetId, attr.SceneName
                                });

                                if (!attr.Optional)
                                {
                                    Debug.Log($"[CrossSceneProcessor] Requesting delayed injection of {field.FieldType.Name} '{attr.TargetId}' from scene '{attr.SceneName}' for {component.GetType().Name}.{field.Name}");
                                }
                            }
                        }
                    }
                }
            }

            // Process children recursively
            foreach (Transform child in go.transform)
            {
                ProcessSceneReferences(child.gameObject);
            }
        }
    }
}