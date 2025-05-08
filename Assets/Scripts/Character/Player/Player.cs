using UnityEngine;

namespace ParaMoon
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class Player : Character
    {
        [Header("Component References")]
        [SerializeField] private Transform playerBody;
        [SerializeField] private Transform cameraHolder;

        PlayerInventory _inventory;

        public FPSController Controller;
        public PlayerInventory Inventory => _inventory;
        public Rigidbody PlayerRigidbody { get; private set; }
        public CapsuleCollider PlayerCollider { get; private set; }

        public static Player Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // Setup references if not assigned
            if (playerBody == null)
                playerBody = transform;

            if (cameraHolder == null)
                cameraHolder = Camera.main.transform;

            Controller = GetComponent<FPSController>();
            _inventory = GetComponent<PlayerInventory>();
            PlayerRigidbody = GetComponent<Rigidbody>();
            PlayerCollider = GetComponent<CapsuleCollider>();

            // Configure rigidbody for FPS controller
            PlayerRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            PlayerRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Setup cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // For external scripts to disable/enable movement
        public void SetMovementEnabled(bool enabled)
        {
            Controller.enabled = enabled;

            if (!enabled)
                PlayerRigidbody.linearVelocity = Vector3.zero;
        }

        public bool IsMoving()
        {
            return Controller.Velocity.magnitude > 0.1f;
        }

        public bool IsRunning()
        {
            return Controller.IsSprinting;
        }

        public bool IsWalking()
        {
            return Controller.IsWalking;
        }

        // Optional: Add footstep sounds based on movement
        public float GetMovementIntensity()
        {
            if (!Controller.IsGrounded) 
                return 0;

            Vector3 flatVelocity = new(Controller.Velocity.x, 0, Controller.Velocity.z);
            return flatVelocity.magnitude / Controller.SprintSpeed; // 0 to 1 value
        }
    }
    
}
