using System.Collections;
using UnityEngine;

namespace ParaMoon
{
    /// <summary>
    /// Extension to the Bootstrapper that adds DI support.
    /// </summary>
    public class BootstrapperExtension : Bootstrapper
    {
        protected override void Awake()
        {
            // Initialize the dependency injector with our ServiceLocator resolver
            DependencyInjector.Initialize(new ServiceLocatorResolver(ServiceLocator.Instance));

            base.Awake();
        }

        // After the existing WaitForServicesInitialization method
        // Add this method to process scene objects
        private IEnumerator ProcessDIForBootScene()
        {
            // Wait for a frame to ensure all services are registered
            yield return null;

            // Find all root objects in the Boot scene
            var rootObjects = gameObject.scene.GetRootGameObjects();

            // Add CrossSceneProcessor to important objects that need cross-scene references
            foreach (var rootObject in rootObjects)
            {
                // Skip the DI system objects
                if (rootObject.name.StartsWith("[DI-") ||
                    rootObject.name.StartsWith("[SceneDependency"))
                    continue;

                // Add CrossSceneProcessor if it doesn't have one
                if (rootObject.GetComponent<SceneDependencyProcessor>() == null)
                {
                    rootObject.AddComponent<SceneDependencyProcessor>();
                }
            }

            Debug.Log("[Bootstrapper] Added CrossSceneProcessors to Boot scene objects");
        }

        // Modify your OnInitializationComplete method
        protected override void OnInitializationComplete()
        {
            // Add DI processing for the Boot scene
            StartCoroutine(ProcessDIForBootScene());

            // Continue with existing code
            base.OnInitializationComplete();
        }
    }
}