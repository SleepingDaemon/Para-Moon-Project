using System;
using UnityEngine;

namespace ParaMoon
{
    [RequireComponent(typeof(Player))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _walkSpeed = 2f;
        [SerializeField] private float _runSpeed = 5f;
        [SerializeField] private float _sprintSpeed = 9f;
        [SerializeField] private float _jumpForce = 5f;
        [SerializeField] private int _maxJumps = 1;

        [Tooltip("The amount of time the player can hold the jump button to gain extra height. 0 = no extra height, 1 = full extra height")]
        [Range(0, 1)][SerializeField] private float _airControl = 0.5f;

        [Tooltip("The amount of drag applied to the player when in the air. 0 = no drag, 1 = full drag")]
        [Range(0, 1)][SerializeField] private float _airDrag = 0.5f;

        [Tooltip("The amount of drag applied to the player when on the ground. 0 = no drag, 1 = full drag")]
        [Range(0, 1)][SerializeField] private float _groundDrag = 4f;

        Player _player;
        Vector3 _moveDirection = Vector3.zero;
        Vector3 _lastPosition; // track the last position of the player
        int _jumpCount = 0;

        public Vector3 Velocity { get; private set; }
        public float WalkSpeed => _walkSpeed;
        public float RunSpeed => _runSpeed;
        public float SprintSpeed => _sprintSpeed;
        public float AirDrag => _airDrag;
        public float GroundDrag => _groundDrag;
        public bool IsWalking { get; private set; }
        public bool IsSprinting { get; private set; }

        private void Awake()
        {
            _player = GetComponent<Player>();
            _lastPosition = transform.position;
        }

        private void Update()
        {
            // Handle Jump Input
            if (InputManager.Instance.Jump && _jumpCount < _maxJumps)
            {
                Jump();
                InputManager.Instance.ConsumeJump(); // Prevent multiple jumps from one button press
            }

            // TODO: Reset jump counter when grounded
        }

        private void FixedUpdate()
        {
            Vector2 moveInput = InputManager.Instance.Move;

            // Create movement vector relative to player orientation
            Vector3 forward = transform.forward * moveInput.y;
            Vector3 right = transform.right * moveInput.x;
            _moveDirection = (forward + right).normalized;

            // Apply movement force based on state
            IsSprinting = InputManager.Instance.Sprint;
            float currentSpeed = IsSprinting ? _sprintSpeed : (IsWalking ? _walkSpeed : _runSpeed);

            // TODO: Check if grounded
        }

        private void Jump()
        {
            throw new NotImplementedException();
        }
    }
}
