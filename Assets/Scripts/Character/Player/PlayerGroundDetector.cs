using UnityEngine;

namespace ParaMoon
{
    public class PlayerGroundDetector : MonoBehaviour
    {
        [SerializeField] private LayerMask _groundLayers;
        [SerializeField] private float _skinWidth = 0.08f;
        [SerializeField] private float _maxSlopeAngle = 45f;
        [SerializeField] private float _slopeCheckDistance = 0.5f;
        [SerializeField] private PhysicsMaterial _slipperyMaterial;
        [SerializeField] private PhysicsMaterial _highFrictionMaterial;
        [SerializeField] private bool debug = false;

        private Player _controller;
        private RaycastHit _slopeHit;
        private bool _wasInAir = false;

        // Public properties
        public bool IsGrounded { get; private set; }
        public bool IsOnSlope { get; private set; }
        public RaycastHit SlopeHit => _slopeHit;
        public bool JustLanded { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<Player>();
        }

        private void Update()
        {
            // Calculate if grounded with custom check
            CheckGrounded();

            // Set JustLanded to true only on the first frame after landing
            JustLanded = _wasInAir && IsGrounded;
            _wasInAir = !IsGrounded;
        }

        private void CheckGrounded()
        {
            // Calculate ray start position (slightly inside the capsule)
            Vector3 rayStart = transform.position + Vector3.up * (_controller.PlayerCollider.radius - _skinWidth);

            // Calculate ray length
            float rayLength = _controller.PlayerCollider.height * 0.25f;

            if (debug)
            {
                // Debug ray
                Debug.DrawRay(rayStart, Vector3.down * rayLength, Color.red);
            }

            // Check if grounded with a downward raycast
            IsGrounded = Physics.SphereCast(
                rayStart,
                _controller.PlayerCollider.radius - _skinWidth,
                Vector3.down,
                out RaycastHit hit,
                rayLength,
                _groundLayers
            );

            // If we have problems with ground detection, try multiple points
            if (!IsGrounded)
            {
                // Try additional points around the character
                float checkRadius = _controller.PlayerCollider.radius * 0.8f;
                Vector3[] checkPoints = new Vector3[]
                {
                    rayStart + transform.forward * checkRadius,
                    rayStart - transform.forward * checkRadius,
                    rayStart + transform.right * checkRadius,
                    rayStart - transform.right * checkRadius
                };

                foreach (Vector3 point in checkPoints)
                {
                    if (Physics.Raycast(point, Vector3.down, out hit, rayLength, _groundLayers))
                    {
                        IsGrounded = true;
                        break;
                    }
                }
            }

            // Reset jump counter when grounded
            if (IsGrounded && _controller.PlayerRigidbody.linearVelocity.y <= 0.1f)
            {
                // Check if the ground is a slope
                CheckSlope(hit);

                // Apply appropriate material based on slope
                if (IsOnSlope && Vector3.Angle(hit.normal, Vector3.up) > _maxSlopeAngle)
                {
                    _controller.PlayerCollider.material = _slipperyMaterial;
                }
                else
                {
                    _controller.PlayerCollider.material = _highFrictionMaterial;
                }
            }

            // Apply appropriate drag based on grounded state
            _controller.PlayerRigidbody.linearDamping = IsGrounded ? _controller.Movement.GroundDrag : _controller.Movement.AirDrag;
        }

        private void CheckSlope(RaycastHit hit)
        {
            float angle = Vector3.Angle(hit.normal, Vector3.up);
            IsOnSlope = angle > 0.1f && angle <= _maxSlopeAngle;

            if (IsOnSlope)
            {
                _slopeHit = hit;

                if (debug)
                {
                    Debug.DrawRay(hit.point, hit.normal, Color.yellow, 0.1f);
                }
            }
        }

        // For inspector exposure
        public void SetGroundLayers(LayerMask layers) => _groundLayers = layers;
        public void SetMaxSlopeAngle(float angle) => _maxSlopeAngle = angle;
        public float GetMaxSlopeAngle() => _maxSlopeAngle;
    }
}
