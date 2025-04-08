using UnityEngine;

namespace ParaMoon
{
    /// <summary>
    /// Handles camera wobble effects based on player movement and actions.
    /// </summary>
    public class CameraWobble : MonoBehaviour
    {
        [Header("Camera Wobble Settings")]
        [SerializeField] float _walkBobFrequency = 2.0f;
        [SerializeField] float _walkBobAmount = 0.05f;
        [SerializeField] float _runBobFrequency = 2.6f;
        [SerializeField] float _runBobAmount = 0.075f;
        [SerializeField] float _sprintBobFrequency = 3.0f;
        [SerializeField] float _sprintBobAmount = 0.1f;
        [SerializeField] float _wobbleSmoothing = 8.0f;
        [SerializeField] float _horizontalMultiplier = 0.6f;
        [SerializeField] float _landBobAmount = 0.15f;
        [SerializeField] float _landBobDuration = 0.3f;
        [SerializeField] float _breathingAmount = 0.02f;
        [SerializeField] float _breathingFrequency = 0.8f;

        Player controller;
        Vector3 originalCameraPosition;
        float bobTimer = 0;
        float currentBobFrequency;
        float currentBobAmount;
        float landTimer = 0;

        private void Awake()
        {
            controller = GetComponent<Player>();
        }

        private void Start()
        {
            // Save camera's original local position for wobble calculations
            if (controller.transform.Find("CameraHolder") != null)
            {
                originalCameraPosition = controller.transform.Find("CameraHolder").localPosition;
            }
            else
            {
                Debug.LogError("Camera holder is null. Please assign it in the inspector.");
            }
        }

        private void Update()
        {
            ApplyCameraWobble();
        }

        private void ApplyCameraWobble()
        {
            // Get camera holder
            Transform cameraHolder = controller.transform.Find("CameraHolder");
            if (cameraHolder == null) return;

            Vector3 targetCameraPosition = originalCameraPosition;

            // Get movement magnitude (ignoring y movement)
            Vector3 flatVelocity = new(controller.Movement.Velocity.x, 0, controller.Movement.Velocity.z);
            float movementMagnitude = flatVelocity.magnitude;

            // Check for landing effect
            if (controller.GroundDetection.JustLanded)
            {
                landTimer = _landBobDuration;
            }
            
            bool isWalking = controller.Movement.IsWalking && !controller.Movement.IsSprinting;

            // Determine running state using input
            bool isSprinting = InputManager.Instance.Sprint && movementMagnitude > 0.5f;

            // Only bob when moving on ground
            if (movementMagnitude > 0.1f && controller.GroundDetection.IsGrounded)
            {
                // Set target values based on running state
                float targetBobFrequency = isSprinting ? _sprintBobFrequency : (isWalking ? _walkBobFrequency : _runBobFrequency);
                float targetBobAmount = isSprinting ? _sprintBobAmount : (isWalking ? _walkBobAmount : _runBobAmount);

                // Smoothly transition between walk and run parameters
                if (currentBobFrequency == 0) currentBobFrequency = targetBobFrequency; // Initialize on first use
                if (currentBobAmount == 0) currentBobAmount = targetBobAmount;

                currentBobFrequency = Mathf.Lerp(currentBobFrequency, targetBobFrequency, Time.deltaTime * 5f);
                currentBobAmount = Mathf.Lerp(currentBobAmount, targetBobAmount, Time.deltaTime * 5f);

                // Scale bob frequency based on movement speed - faster movement = faster bobbing
                float speedFactor = movementMagnitude / (isSprinting 
                                                         ? controller.Movement.SprintSpeed 
                                                         : (isWalking 
                                                         ? controller.Movement.WalkSpeed 
                                                         : controller.Movement.RunSpeed));

                speedFactor = Mathf.Clamp(speedFactor, 0.5f, 1.5f); // Limit range

                // Increase timer based on frequency and speed
                bobTimer += Time.deltaTime * currentBobFrequency * speedFactor;

                // Calculate bob offset
                float verticalBob = Mathf.Sin(bobTimer) * currentBobAmount;
                float horizontalBob = Mathf.Sin(bobTimer * 0.5f) * currentBobAmount * _horizontalMultiplier;

                // Apply vertical and horizontal bob
                targetCameraPosition += new Vector3(horizontalBob, verticalBob, 0);
            }
            else
            {
                // When not moving, gradually reset bob timers
                bobTimer = 0;
            }

            if (landTimer > 0)
            {
                float landingProgress = 1 - (landTimer / _landBobDuration);
                float landingBobAmount = _landBobAmount * (1 - Mathf.Pow(landingProgress, 2));
                targetCameraPosition.y -= landingBobAmount;
                landTimer -= Time.deltaTime;
            }

            if (movementMagnitude < 0.1f && controller.GroundDetection.IsGrounded)
            {
                float breathingEffect = Mathf.Sin(Time.time * _breathingFrequency) * _breathingAmount;
                targetCameraPosition.y += breathingEffect;
            }

            if (movementMagnitude > 0.1f && controller.GroundDetection.IsGrounded)
            {
                float tiltAmount = Mathf.Sin(bobTimer * 0.5f) * 0.5f;
                float verticalLookAngle = controller.Look.GetVerticalLookAngle();
                Quaternion tiltRotation = Quaternion.Euler(0, 0, tiltAmount);
                cameraHolder.localRotation = Quaternion.Euler(verticalLookAngle, 0, 0) * tiltRotation;
            }

            cameraHolder.localPosition = Vector3.Lerp(
                cameraHolder.localPosition,
                targetCameraPosition,
                Time.deltaTime * _wobbleSmoothing
            );
        }

        // Public method for external impulses (for telekinesis effects)
        public void AddImpulseToCameraWobble(Vector3 impulse, float duration = 0.2f)
        {
            Transform cameraHolder = controller.transform.Find("CameraHolder");
            if (cameraHolder != null)
            {
                cameraHolder.localPosition += impulse;
            }
        }
    }
}
