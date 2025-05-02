using UnityEngine;

namespace ParaMoon
{
    /**
     * HelmetUIController is responsible for managing the helmet UI element in the game.
     * It handles the positioning and rotation of the UI based on the player's camera movement.
     * 
     * Usage:
     * - Attach this script to the helmet UI GameObject.
     * - Assign the player camera and helmet UI canvas in the inspector.
     */
    public class HelmetUIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _playerCamera;
        [SerializeField] private Canvas _helmetUICanvas;

        [Header("UI Movement Settings")]
        [SerializeField] private float _movementLag = 0.1f; // How delayed UI follows head
        [SerializeField] private float _movementAmplitude = 0.02f; // How much UI moves
        [SerializeField] private float _forwardCorrectionFactor = 0.5f; // Reduces forward/back movement
        [SerializeField] private float _screenConversionFactor = 100.0f; // Converts world space to screen space

        [Header("Look Movement Settings")]
        [SerializeField] private float _lookInfluenceAmount = 0.01f; // How much mouse look affects UI position
        [SerializeField] private float _lookInfluenceSmoothing = 5.0f; // Smoothing for look influence

        [Header("Breathing Effect")]
        [SerializeField] private float _breathingSpeed = 1f;
        [SerializeField] private float _breathingAmount = 0.005f;

        [Header("Wobble Settings")]
        [SerializeField] private float _wobbleDelay = 0.2f; // Delay for wobble effect
        [SerializeField] private float _bobFrequency = 2.0f; // How fast the UI bobs
        [SerializeField] private float _bobAmount = 0.025f; // How much the UI bobs
        [SerializeField] private float _horizontalMultiplier = 0.6f; // Multiplier for horizontal movement
        [SerializeField] private float _wobbleSmoothing = 8.0f; // How smooth the wobble effect is

        FPSController _fpsController;
        RectTransform _rectTransform;
        Vector2 _initialAnchoredPosition;
        Vector2 _targetAnchoredPosition;
        Vector3 _lastCameraVelocity;
        Vector3 _delayedCameraVelocity;
        Vector2 _lastLookDelta = Vector2.zero;
        Vector2 _currentLookInfluence = Vector2.zero;
        float _bobTimer = 0f;
        float _currentBobAmount = 0f;
        float _currentBobFrequency = 0f;

        void Awake()
        {
            if (_playerCamera == null)
                Debug.LogError("Player camera not found in any loaded scene!");

            if (_helmetUICanvas != null && transform.parent != _helmetUICanvas.transform)
            {
                transform.SetParent(_helmetUICanvas.transform, false);

                if (_helmetUICanvas != null)
                {
                    // Set to Screen Space - Camera for best results with your controller
                    _helmetUICanvas.renderMode = RenderMode.ScreenSpaceCamera;
                    _helmetUICanvas.worldCamera = Camera.main; // Or your specific UI camera
                    _helmetUICanvas.sortingOrder = 10; // Make sure it's above other UI elements

                    // Ensure the rect transform covers the whole screen
                    RectTransform canvasRect = _helmetUICanvas.GetComponent<RectTransform>();
                    canvasRect.anchorMin = Vector2.zero;
                    canvasRect.anchorMax = Vector2.one;
                    canvasRect.offsetMin = Vector2.zero;
                    canvasRect.offsetMax = Vector2.zero;
                }
            }

            _fpsController = FindFirstObjectByType<FPSController>();
            _rectTransform = GetComponent<RectTransform>();

            if (_fpsController == null)
            {
                Debug.LogError("FPSController not found in any loaded scene!");
                return;
            }

            if (_rectTransform == null)
            {
                Debug.LogError("RectTransform component not found on HelmetUIController!");
                return;
            }

            _initialAnchoredPosition = _rectTransform.anchoredPosition;
            _targetAnchoredPosition = _initialAnchoredPosition;

            // Initialize wobble variables
            _currentBobAmount = _bobAmount;
            _currentBobFrequency = _bobFrequency;
        }

        void Update()
        {
            if (_rectTransform == null) return;

            // Get current camera velocity
            Vector3 cameraVelocity = _fpsController?.Velocity ?? Vector3.zero;

            // Track mouse look movement from FPSController
            Vector2 lookDelta = Vector2.zero;
            if (_fpsController != null)
            {
                lookDelta = GetLookDelta();
            }

            // Process mouse look influence on UI position
            ProcessLookInfluence(lookDelta);

            // Apply delay to camera velocity (smoothly transition from last velocity)
            _delayedCameraVelocity = Vector3.Lerp(_delayedCameraVelocity, cameraVelocity, Time.deltaTime * (1f / _wobbleDelay));

            // Convert velocity to local space relative to camera
            Vector3 localVelocity = _playerCamera.InverseTransformDirection(_delayedCameraVelocity);

            // Reduce forward/backward movement effect (z-axis)
            localVelocity.z *= _forwardCorrectionFactor;

            // Calculate base target position with modified local velocity
            Vector2 offsetPosition = new Vector2(
                localVelocity.x * _movementAmplitude * _movementLag * _screenConversionFactor,  // Sideways movement
                localVelocity.y * _movementAmplitude * _movementLag * _screenConversionFactor   // Vertical movement
            );

            // Apply look influence to offset position - scaled for screen space
            offsetPosition.x += _currentLookInfluence.x * _screenConversionFactor;
            offsetPosition.y += _currentLookInfluence.y * _screenConversionFactor;

            _targetAnchoredPosition = _initialAnchoredPosition + offsetPosition;

            // Get flat velocity (horizontal movement only) for bob calculations
            Vector3 flatVelocity = new Vector3(_delayedCameraVelocity.x, 0, _delayedCameraVelocity.z);
            float movementMagnitude = flatVelocity.magnitude;

            // Add wobble effect when moving
            if (movementMagnitude > 0.1f && _fpsController != null && _fpsController.IsGrounded)
            {
                if (!ServiceLocator.Instance.TryGetService<InputManager>(out var inputManager))
                    Debug.LogError("InputManager not found!");

                // Calculate a speed factor based on player movement
                bool isWalking = inputManager.Walk;
                bool isSprinting = inputManager.Sprint;
                bool isCrouching = inputManager.Crouch;
                float referenceSpeed = isSprinting ? _fpsController.SprintSpeed :
                       (isWalking ? _fpsController.WalkSpeed :
                       (isCrouching ? _fpsController.CrouchSpeed : _fpsController.RunSpeed));

                float speedFactor = Mathf.Min(movementMagnitude / referenceSpeed, 1.5f);
                speedFactor = Mathf.Max(speedFactor, 0.5f);

                // Increase bob timer based on frequency and speed
                _bobTimer += Time.deltaTime * _currentBobFrequency * speedFactor;

                // Calculate bob offset with some delay - scale for screen space
                float verticalBob = Mathf.Sin(_bobTimer) * _currentBobAmount * _screenConversionFactor;
                float horizontalBob = Mathf.Sin(_bobTimer * 0.5f) * _currentBobAmount * _horizontalMultiplier * _screenConversionFactor;

                // Add bob movement to the target position
                _targetAnchoredPosition += new Vector2(horizontalBob, verticalBob);
            }
            else
            {
                // Reset bob timer when not moving
                _bobTimer = 0;
            }

            // Add breathing effect - scaled for screen space
            float breathingOffset = Mathf.Sin(Time.time * _breathingSpeed) * _breathingAmount * _screenConversionFactor;
            _targetAnchoredPosition.y += breathingOffset;

            // Smoothly move UI to target position
            _rectTransform.anchoredPosition = Vector2.Lerp(_rectTransform.anchoredPosition, _targetAnchoredPosition, Time.deltaTime * _wobbleSmoothing);

            // Store current velocity for next frame
            _lastCameraVelocity = cameraVelocity;
        }


        /*
         * This method retrieves the look delta from the FPSController using reflection.
         * It assumes that the FPSController has a private field named "_lookDelta".
         */
        private void ProcessLookInfluence(Vector2 lookDelta)
        {
            // Calculate target look influence
            Vector2 targetInfluence = new Vector2(
                -lookDelta.x * _lookInfluenceAmount, // Horizontal influence (inverted)
                lookDelta.y * _lookInfluenceAmount    // Vertical influence
            );

            // Apply smoothing to look influence
            _currentLookInfluence = Vector2.Lerp(
                _currentLookInfluence,
                targetInfluence,
                Time.deltaTime * _lookInfluenceSmoothing
            );

            // Natural return to center when no input
            if (lookDelta.magnitude < 0.001f)
            {
                _currentLookInfluence = Vector2.Lerp(
                    _currentLookInfluence,
                    Vector2.zero,
                    Time.deltaTime * 3f
                );
            }
        }

        // Function to get mouse look delta from Input Manager
        private Vector2 GetLookDelta()
        {
            // Since we can't directly access FPSController's _currentLookDelta or InputManager
            // We'll estimate it based on mouse movement between frames
            Vector2 mouseDelta = Vector2.zero;

            if (ServiceLocator.Instance.TryGetService<InputManager>(out var inputManager))
            {
                // Access the look input from InputManager
                mouseDelta = inputManager.Look;

                // Scale down the raw input to match FPSController's processing
                mouseDelta *= 0.01f;
            }

            return mouseDelta;
        }
    }
}