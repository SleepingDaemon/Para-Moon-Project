using UnityEngine;

namespace ParaMoon
{
    public class PlayerLook : MonoBehaviour
    {
        [Header("Look Settings")]
        [SerializeField] private float _lookSensitivity = 2.0f;
        [SerializeField] private float _lookSmoothness = 0.1f;
        [SerializeField] private float _maxLookAngle = 85.0f;

        private Player controller;
        private float rotationX = 0;
        private Vector2 currentLookDelta = Vector2.zero;
        private Vector2 targetLookDelta = Vector2.zero;

        private void Awake()
        {
            controller = GetComponent<Player>();
        }

        private void Update()
        {
            HandleMouseLook();
        }

        private void HandleMouseLook()
        {
            // Get mouse input from input handler
            Vector2 lookInput = InputManager.Instance.Look;

            // Apply sensitivity
            float mouseX = lookInput.x * _lookSensitivity * (0.1f / 10f);
            float mouseY = lookInput.y * _lookSensitivity * (0.1f / 10f);

            // Apply direct mouse movement with minimal smoothing for more responsive feel
            targetLookDelta = new Vector2(mouseX, mouseY);
            //currentLookDelta = Vector2.Lerp(currentLookDelta, targetLookDelta, Time.deltaTime * (1f / lookSmoothness));

            // Calculate vertical camera rotation with inverted Y
            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -_maxLookAngle, _maxLookAngle);

            // Apply rotation to camera and player
            if (controller.transform.Find("CameraHolder") != null)
            {
                controller.transform.Find("CameraHolder").localRotation = Quaternion.Euler(rotationX, 0, 0);
            }
            else
            {
                Debug.LogError("Camera holder is null. Please assign it in the inspector.");
            }

            // Apply horizontal rotation to the player body
            transform.Rotate(Vector3.up * mouseX);
        }

        public float GetVerticalLookAngle() => rotationX;
    }
}
