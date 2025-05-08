using UnityEngine;

namespace ParaMoon
{
    /// <summary>
    /// Component that automatically registers a specified component with the ReferenceRegistry
    /// </summary>
    public class RegisterReference : MonoBehaviour
    {
        [SerializeField] private Component _componentToRegister;
        [SerializeField] private string _identifier = "default";
        [SerializeField] private bool _registerOnAwake = true;
        [SerializeField] private bool _unregisterOnDestroy = true;

        private void Awake()
        {
            if (_registerOnAwake)
                Register();
        }

        private void OnDestroy()
        {
            if (_unregisterOnDestroy)
                Unregister();
        }

        public void Register()
        {
            if (_componentToRegister == null)
            {
                Debug.LogError($"[RegisterReference] No component assigned to register on {gameObject.name}");
                return;
            }

            if (ServiceLocator.Instance.TryGetService<ReferenceRegistry>(out var registry))
            {
                // Use reflection to call the generic RegisterReference method with the right type
                var methodInfo = typeof(ReferenceRegistry).GetMethod("RegisterReference");
                var genericMethod = methodInfo.MakeGenericMethod(_componentToRegister.GetType());
                genericMethod.Invoke(registry, new object[] { _componentToRegister, _identifier });
            }
            else
            {
                Debug.LogError("[RegisterReference] ReferenceRegistry service not available");
            }
        }

        public void Unregister()
        {
            if (_componentToRegister == null)
                return;

            if (ServiceLocator.Instance.TryGetService<ReferenceRegistry>(out var registry))
            {
                var methodInfo = typeof(ReferenceRegistry).GetMethod("UnregisterReference");
                var genericMethod = methodInfo.MakeGenericMethod(_componentToRegister.GetType());
                genericMethod.Invoke(registry, new object[] { _identifier });
            }
        }
    }
}
