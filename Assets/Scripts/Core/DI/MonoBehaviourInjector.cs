using UnityEngine;

namespace ParaMoon
{
    /// <summary>
    /// Component that injects dependencies into the attached MonoBehaviour.
    /// </summary>
    public class MonoBehaviourInjector : MonoBehaviour
    {
        [SerializeField]
        private bool _injectOnAwake = true;

        [SerializeField]
        private bool _injectOnEnable = false;

        [SerializeField]
        private MonoBehaviour[] _additionalTargets;

        private void Awake()
        {
            if (_injectOnAwake)
            {
                InjectAll();
            }
        }

        private void OnEnable()
        {
            if (_injectOnEnable)
            {
                InjectAll();
            }
        }

        /// <summary>
        /// Manually trigger injection for this GameObject.
        /// </summary>
        public void InjectAll()
        {
            // Inject into the parent component
            var components = GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                // Skip injecting into self to avoid unnecessary work
                if (component != this)
                {
                    DependencyInjector.InjectInto(component);
                }
            }

            // Inject into additional targets if specified
            if (_additionalTargets != null)
            {
                foreach (var target in _additionalTargets)
                {
                    if (target != null)
                    {
                        DependencyInjector.InjectInto(target);
                    }
                }
            }
        }
    }
}