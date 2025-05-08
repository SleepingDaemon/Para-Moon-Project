using UnityEngine;

namespace ParaMoon
{
    public class SceneContext : MonoBehaviour
    {
        [SerializeField] private string _sceneName;

        private void Awake()
        {
            if (string.IsNullOrEmpty(_sceneName))
            {
                _sceneName = gameObject.scene.name;
            }

            // Add CrossSceneProcessor to this object
            if (GetComponent<SceneDependencyProcessor>() == null)
            {
                var processor = gameObject.AddComponent<SceneDependencyProcessor>();
            }

            Debug.Log($"[SceneContext] Initialized for scene '{_sceneName}'");
        }
    }
}