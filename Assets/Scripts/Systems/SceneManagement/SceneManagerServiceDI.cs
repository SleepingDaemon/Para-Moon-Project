using UnityEngine;

namespace ParaMoon
{
    [Injectable]
    public class SceneManagerServiceDI : SceneManagerService
    {
        [SerializeField] private bool _enableCrossSceneReferences = true;

        public override void Initialize()
        {
            base.Initialize();

            // Register for our own events to handle cross-scene reference cleanup
            OnSceneUnloadStarted += HandleSceneUnloadForReferences;
        }

        private void HandleSceneUnloadForReferences(string sceneName)
        {
            if (!_enableCrossSceneReferences) return;

            // When a scene is about to be unloaded, clean up its references
            if (ServiceLocator.Instance.TryGetService<SceneDependencyManager>(out var manager))
            {
                manager.UnregisterScene(sceneName);
                Debug.Log($"[SceneManagerServiceDI] Unregistered cross-scene references for scene '{sceneName}'");
            }
        }

        protected override void OnDestroy()
        {
            OnSceneUnloadStarted -= HandleSceneUnloadForReferences;
            base.OnDestroy();
        }
    }
}