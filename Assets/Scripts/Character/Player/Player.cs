using UnityEngine;
using UnityEngine.LowLevel;

namespace ParaMoon
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class Player : Character
    {
        [Header("Component References")]
        [SerializeField] private Transform playerBody;
        [SerializeField] private Transform cameraHolder;

        private PlayerMovement movement;
        private PlayerGroundDetector groundDetection;
        private PlayerLook look;
        private CameraWobble cameraWobble;

        // Properties to access components
        public PlayerMovement Movement => movement;
        public PlayerGroundDetector GroundDetection => groundDetection;
        public PlayerLook Look => look;
        public CameraWobble CameraWobble => cameraWobble;
        public Rigidbody PlayerRigidbody { get; private set; }
        public CapsuleCollider PlayerCollider { get; private set; }

        private void Awake()
        {
            // Setup references if not assigned
            if (playerBody == null)
                playerBody = transform;

            if (cameraHolder == null)
                cameraHolder = Camera.main.transform;

            PlayerRigidbody = GetComponent<Rigidbody>();
            PlayerCollider = GetComponent<CapsuleCollider>();

            // Configure rigidbody for FPS controller
            PlayerRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            PlayerRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Initialize components
            movement = gameObject.AddComponent<PlayerMovement>();
            groundDetection = gameObject.AddComponent<PlayerGroundDetector>();
            look = gameObject.AddComponent<PlayerLook>();
            cameraWobble = gameObject.AddComponent<CameraWobble>();

            // Setup cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // For external scripts to disable/enable movement
        public void SetMovementEnabled(bool enabled)
        {
            movement.enabled = enabled;
            look.enabled = enabled;

            if (!enabled)
            {
                PlayerRigidbody.linearVelocity = Vector3.zero;
            }
        }

        public bool IsMoving()
        {
            return movement.Velocity.magnitude > 0.1f;
        }

        public bool IsRunning()
        {
            return movement.IsSprinting;
        }

        public bool IsWalking()
        {
            return movement.IsWalking;
        }

        // Optional: Add footstep sounds based on movement
        public float GetMovementIntensity()
        {
            if (!groundDetection.IsGrounded) return 0;

            Vector3 flatVelocity = new(movement.Velocity.x, 0, movement.Velocity.z);
            return flatVelocity.magnitude / movement.RunSpeed; // 0 to 1 value
        }
    }
}
