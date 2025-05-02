using UnityEngine;

namespace ParaMoon
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class FPSController : MonoBehaviour
    {
        [Header("Player Body Settings")]
        [SerializeField] Transform _playerBody;
        [SerializeField] Rigidbody _playerRigidbody;
        [SerializeField] CapsuleCollider _playerCollider;
        [SerializeField] LayerMask _groundLayers;
        [SerializeField] float _skinWidth = 0.08f;
        [SerializeField] PhysicsMaterial _frictionlessPhysicMaterial;

        [Header("Movement Settings")]
        [SerializeField] float _walkSpeed = 5.0f;
        [SerializeField] float _runSpeed = 9.0f;
        [SerializeField] float _sprintSpeed = 12.0f;
        [SerializeField] float _acceleration = 10.0f;
        [SerializeField] float _deceleration = 20.0f;
        [SerializeField] float _airControlFactor = 0.5f;
        [SerializeField] float _groundDrag = 5.0f;
        [SerializeField] float _airDrag = 0.5f;
        [SerializeField] float _jumpForce = 5.0f;
        [SerializeField] int _maxJumps = 1;
        [SerializeField] float _gravityMultiplier = 1.0f;
        [SerializeField] float _fallMultiplier = 2.5f;
        [SerializeField] float _jumpCooldown = 0.1f;

        [SerializeField] float _crouchSpeed = 3.0f;
        [SerializeField] float _crouchHeight = 1.0f;
        [SerializeField] float _standingHeight = 2.0f;
        [SerializeField] float _crouchTransitionSpeed = 10.0f;

        [Header("Slope Handling")]
        [SerializeField] float _maxSlopeAngle = 45f;
        [SerializeField] float _slopeCheckDistance = 0.5f;
        [SerializeField] float _slopeSlideSpeed = 8f;
        [SerializeField] float _slopeForceFactor = 2.0f;

        [Header("Step Handling")]
        [SerializeField] float _maxStepHeight = 0.4f;
        [SerializeField] float _stepCheckDistance = 0.2f;
        [SerializeField] float _stepSmoothing = 0.1f;
        [SerializeField] Transform _stepRayBottom;
        [SerializeField] Transform _stepRayTop;

        [Header("Look Settings")]
        [SerializeField] Transform _cameraHolder;
        [SerializeField] float _lookSensitivity = 2.0f;
        [SerializeField] float _lookSmoothness = 0.1f;
        [SerializeField] float _maxLookAngle = 85.0f;

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
        [SerializeField] float _cameraLeanAmount = 5f;
        [SerializeField] float _cameraLeanSpeed = 8f;

        [Header("Advanced Physics")]
        [SerializeField] bool _useAdvancedGroundChecks = true;
        [SerializeField] float _groundCheckRadius = 0.25f;
        [SerializeField] float _groundCheckHeightOffset = 0.1f;
        [SerializeField] float _groundStickForce = 5f;
        [SerializeField] float _impactThreshold = 10f;
        [SerializeField] float _momentumRetention = 0.9f;
        [SerializeField] bool _useCustomGravity = true;
        [SerializeField] Vector3 _gravity = new Vector3(0, -9.81f, 0);

        [Header("Debug Settings")]
        [SerializeField] bool _debug = false;
        [SerializeField] bool _showGroundChecks = false;
        [SerializeField] bool _showMovementForces = false;
        [SerializeField] bool _showSlopeData = false;

        // Internal variables
        PlayerAnimation _playerAnim;
        Vector3 _moveDirection = Vector3.zero;
        Vector3 _desiredVelocity = Vector3.zero;
        Vector3 _currentGroundNormal = Vector3.up;
        Vector3 _originalCameraPosition;
        Vector3 _targetStepPosition;
        Vector3 _lastVelocityBeforeCollision;
        Vector2 _currentLookDelta = Vector2.zero;
        Vector2 _targetLookDelta = Vector2.zero;
        RaycastHit _slopeHit;
        float _currentSpeed = 0f;
        int _jumpCount = 0;
        float _rotationX = 0;
        float _bobTimer = 0;
        float _currentBobFrequency;
        float _currentBobAmount;
        float _landTimer = 0;
        float _jumpCooldownTimer = 0;
        float _currentCameraLean = 0f;
        float _targetCameraLean = 0f;
        float _timeSinceGrounded = 0f;
        float _groundedTimer = 0f;
        float _currentHeight;
        float _targetHeight;
        bool _wasInAir = false;
        bool _wasJumpPressed = false;
        bool _wasCrouchPressed = false;
        bool _isGrounded;
        bool _isOnSlope;
        bool _isSlidingDownSlope;
        bool _isStepAdjusting = false;
        bool _isCrouching = false;
        bool _isWalking = false;
        bool _isSprinting = false;

        // Cached velocity for wobble calculations
        InputManager _inputManager;
        Vector3 _playerVelocity;
        Vector3 _lastPosition;

        // Public properties
        public Vector3 Velocity => _playerVelocity;
        public float SprintSpeed => _sprintSpeed;
        public bool IsSprinting
        {
            get => _isSprinting && _moveDirection.magnitude > 0.1f;
            set => _isSprinting = value;
        }
        public bool IsWalking
        {
            get => _isWalking && _moveDirection.magnitude > 0.1f;
            set => _isWalking = value;
        }
        public bool IsGrounded => _isGrounded;
        public bool IsSliding => _isSlidingDownSlope;
        public bool IsCrouching
        {
            get => _isCrouching;
            set
            {
                if (_isCrouching != value)
                {
                    _isCrouching = value;
                    _targetHeight = _isCrouching ? _crouchHeight : _standingHeight;
                    _playerCollider.height = Mathf.Lerp(_playerCollider.height, _targetHeight, Time.deltaTime * _crouchTransitionSpeed);
                    _playerCollider.center = new Vector3(0, _playerCollider.height / 2, 0);
                }
            }
        }
        public float CurrentSpeed => _currentSpeed;

        public float WalkSpeed => _walkSpeed;
        public float RunSpeed => _runSpeed;
        public float CrouchSpeed => _crouchSpeed;

        private void Start()
        {
            // Setup references if not assigned
            if (ServiceLocator.Instance.TryGetService<InputManager>(out var inputManager))
                InitializeInput(inputManager);
            else
                ServiceLocator.Instance.WhenAvailable<InputManager>(InitializeInput);

            if (_playerAnim == null)
                _playerAnim = GetComponentInChildren<PlayerAnimation>();

            if (_playerRigidbody == null)
                _playerRigidbody = GetComponent<Rigidbody>();

            if (_playerCollider == null)
                _playerCollider = GetComponent<CapsuleCollider>();

            if (_playerBody == null)
                _playerBody = transform;

            if (_cameraHolder == null)
                _cameraHolder = Camera.main.transform;

            _targetStepPosition = transform.position;

            // Configure rigidbody for FPS controller
            _playerRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            _playerRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            //_playerRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _playerRigidbody.useGravity = !_useCustomGravity;
            _playerRigidbody.mass = 70f; // Average human mass

            // Create or assign physicMaterial to prevent sticking on walls
            if (_frictionlessPhysicMaterial == null)
            {
                _frictionlessPhysicMaterial = new PhysicsMaterial("Frictionless");
                _frictionlessPhysicMaterial.dynamicFriction = 0f;
                _frictionlessPhysicMaterial.staticFriction = 0f;
                _frictionlessPhysicMaterial.bounciness = 0f;
                _frictionlessPhysicMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;
                _frictionlessPhysicMaterial.bounceCombine = PhysicsMaterialCombine.Minimum;
            }
            _playerCollider.material = _frictionlessPhysicMaterial;

            // Save camera's original local position for wobble calculations
            _originalCameraPosition = _cameraHolder.localPosition;

            _lastPosition = transform.position;
            if (_stepRayTop != null)
                _stepRayTop.transform.position = new Vector3(_stepRayTop.transform.position.x, _maxStepHeight, _stepRayTop.transform.position.z);

            _currentHeight = _playerCollider.height;
            _targetHeight = _currentHeight;

            // Lock and hide cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void InitializeInput(InputManager service)
        {
            _inputManager = service;
        }

        private void Update()
        {
            if (_inputManager == null)
                return;

            // Handle player look controls
            HandleMouseLook();

            // Process jump inputs
            ProcessJumpInput();

            // Handle crouch input
            HandleCrouch();

            // Apply camera effects
            ApplyCameraWobble();
            UpdateCameraLean();

            // Update timers
            if (_jumpCooldownTimer > 0)
                _jumpCooldownTimer -= Time.deltaTime;

            if (!_isGrounded)
                _timeSinceGrounded += Time.deltaTime;
            else
                _groundedTimer += Time.deltaTime;

            // Debug visualization
            if (_debug)
                DrawDebugVisuals();
        }

        private void FixedUpdate()
        {
            if (_inputManager == null)
                return;

            // Calculate ground state first
            CheckGrounded();

            // Handle movement physics
            HandleMovement();

            // Handle slopes and steps
            HandleSlopes();
            CheckForSteps();

            // Apply custom gravity if enabled
            if (_useCustomGravity)
                ApplyCustomGravity();

            // Apply ground stick force to prevent bouncing on slopes
            ApplyGroundStickForce();

            // Update cached velocity for wobble and other calculations
            _playerVelocity = (transform.position - _lastPosition) / Time.fixedDeltaTime;
            _lastPosition = transform.position;

            // Store velocity for impact calculations
            if (!_isGrounded)
                _lastVelocityBeforeCollision = _playerRigidbody.linearVelocity;

            UpdateAnimationState();
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Handle impact physics
            HandleImpact(collision);
        }

        private void UpdateAnimationState()
        {
            if (_playerAnim == null) return;

            // Update movement
            float normalizedSpeed = Mathf.InverseLerp(0, IsSprinting ? _sprintSpeed : (IsWalking ? _walkSpeed : _runSpeed), CurrentSpeed);
            _playerAnim.SetMovement(normalizedSpeed);

            // Update grounding
            _playerAnim.SetGrounding(_isGrounded);

            // add more animation parameters here
        }

        private void HandleImpact(Collision collision)
        {
            // Calculate impact force for camera shake and other effects
            if (_wasInAir && collision.contactCount > 0)
            {
                float impactVelocity = Mathf.Abs(_lastVelocityBeforeCollision.y);

                // Only process significant impacts
                if (impactVelocity > _impactThreshold)
                {
                    float impactFactor = Mathf.InverseLerp(_impactThreshold, 30f, impactVelocity);

                    // Add camera shake
                    Vector3 shakeImpulse = new Vector3(
                        Random.Range(-0.1f, 0.1f) * impactFactor,
                        -0.2f * impactFactor,
                        0
                    );

                    AddImpulseToCameraWobble(shakeImpulse);

                    // You could also trigger sound effects or particle effects here
                }
            }
        }

        private void ProcessJumpInput()
        {
            bool jumpPressed = _inputManager.Jump;

            // Detect rising edge of jump button
            bool jumpTriggered = jumpPressed && !_wasJumpPressed;
            _wasJumpPressed = jumpPressed;

            // Jump if we can
            if (jumpTriggered && _jumpCount < _maxJumps && _jumpCooldownTimer <= 0)
            {
                Jump();
                _jumpCooldownTimer = _jumpCooldown; // Apply jump cooldown
            }

            // Allow jump buffering (small time window where pressing jump right before landing will trigger a jump)
            if (jumpTriggered && !_isGrounded && _timeSinceGrounded < 0.15f)
            {
                // Buffer the jump for a short period
                Invoke(nameof(Jump), 0.05f);
            }
        }

        private void ApplyCustomGravity()
        {
            if (!_isGrounded)
            {
                // Apply custom gravity
                Vector3 gravityForce = _gravity * _gravityMultiplier;

                // Apply stronger gravity when falling to make jumps feel better
                if (GetLinearVelocity().y < 0)
                {
                    gravityForce *= _fallMultiplier;
                }
                // Apply weaker gravity at the peak of the jump for a slight hang-time effect
                else if (_playerRigidbody.linearVelocity.y > 0 && !_inputManager.Jump)
                {
                    gravityForce *= 0.7f;
                }

                _playerRigidbody.AddForce(gravityForce, ForceMode.Acceleration);
            }
        }

        private Vector3 GetLinearVelocity()
        {
            return _playerRigidbody.linearVelocity;
        }

        private void ApplyGroundStickForce()
        {
            // Apply a small downward force when grounded to prevent bouncing and keep player stuck to slopes
            if (_isGrounded && !_isSlidingDownSlope)
            {
                // Apply a bit more force when using built-in gravity to prevent bouncing
                float stickMultiplier = _playerRigidbody.useGravity ? 1.2f : 1.0f;
                _playerRigidbody.AddForce(-_currentGroundNormal * _groundStickForce * stickMultiplier, ForceMode.Acceleration);
            }
        }

        private void CheckGrounded()
        {
            bool wasGrounded = _isGrounded;
            _isGrounded = false;

            if (_useAdvancedGroundChecks)
            {
                AdvancedGroundCheck();
            }
            else
            {
                SimpleGroundCheck();
            }

            // If we just landed
            if (!wasGrounded && _isGrounded)
            {
                _timeSinceGrounded = 0f;
                _jumpCount = 0;
                _landTimer = _landBobDuration;

                // Apply landing effects
                if (_playerVelocity.y < -4f)
                {
                    // Stronger effect for harder landings
                    float landingIntensity = Mathf.InverseLerp(-4f, -15f, _playerVelocity.y);
                    AddImpulseToCameraWobble(new Vector3(0, -_landBobAmount * landingIntensity, 0));

                    if (_playerAnim != null)
                    {
                        _playerAnim.SetGrounding(true);
                    }
                }
            }

            // Apply appropriate drag based on grounded state
            _playerRigidbody.linearDamping = _isGrounded ? _groundDrag : _airDrag;

            // Track if we were in air
            _wasInAir = !_isGrounded;

            // Update grounded state in animator
            if (_playerAnim != null)
            {
                _playerAnim.SetGrounding(_isGrounded);
            }
        }

        private void SimpleGroundCheck()
        {
            // Calculate ray start position (slightly inside the capsule)
            Vector3 rayStart = _playerBody.position + Vector3.up * (_playerCollider.radius - _skinWidth);

            // Calculate ray length
            float rayLength = _playerCollider.height * 0.25f;

            if (_debug && _showGroundChecks)
            {
                // Debug ray
                Debug.DrawRay(rayStart, Vector3.down * rayLength, Color.red);
            }

            // Check if grounded with a downward raycast
            _isGrounded = Physics.SphereCast(
                rayStart,
                _playerCollider.radius - _skinWidth,
                Vector3.down,
                out RaycastHit hit,
                rayLength,
                _groundLayers
            );

            // If we have problems with ground detection, try multiple points
            if (!_isGrounded)
            {
                // Try additional points around the character
                float checkRadius = _playerCollider.radius * 0.8f;
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
                        _isGrounded = true;
                        break;
                    }
                }
            }

            // Update ground normal and check slope
            if (_isGrounded)
            {
                _currentGroundNormal = hit.normal;
                CheckSlope(hit);
            }
            else
            {
                _currentGroundNormal = Vector3.up;
            }
        }

        private void AdvancedGroundCheck()
        {
            // Start position for ground checks (at the bottom of the capsule)
            Vector3 capsuleBottom = transform.position +
                                    Vector3.up * (_playerCollider.radius + _groundCheckHeightOffset);

            // Create an overlapping sphere to detect ground
            Collider[] groundColliders = Physics.OverlapSphere(
                capsuleBottom,
                _groundCheckRadius,
                _groundLayers
            );

            if (_debug && _showGroundChecks)
            {
                DebugExtension.DrawWireSphere(capsuleBottom, Color.green, _groundCheckRadius);
            }

            if (groundColliders.Length > 0)
            {
                // Find closest point on any of these colliders
                float closestDistance = float.MaxValue;
                RaycastHit closestHit = new RaycastHit();
                bool foundValidHit = false;

                foreach (Collider col in groundColliders)
                {
                    // Skip our own collider
                    if (col == _playerCollider) continue;

                    // Cast a ray from slightly above to check the surface
                    Vector3 rayStart = transform.position + Vector3.up * (_playerCollider.height * 0.5f);
                    Vector3 rayDirection = Vector3.down;
                    float rayLength = _playerCollider.height * 0.75f;

                    if (Physics.Raycast(rayStart, rayDirection, out RaycastHit hit, rayLength, _groundLayers))
                    {
                        // Check if this is the closest point
                        float distance = Vector3.Distance(capsuleBottom, hit.point);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestHit = hit;
                            foundValidHit = true;

                            if (_debug && _showGroundChecks)
                            {
                                Debug.DrawLine(rayStart, hit.point, Color.yellow);
                                Debug.DrawRay(hit.point, hit.normal * 0.5f, Color.blue);
                            }
                        }
                    }
                }

                // If we found a valid ground surface
                if (foundValidHit && closestDistance <= _groundCheckRadius + 0.1f)
                {
                    _isGrounded = true;
                    _currentGroundNormal = closestHit.normal;
                    CheckSlope(closestHit);
                    return;
                }
            }

            // If no ground found with advanced method, fall back to a simple check
            SimpleGroundCheck();
        }

        private void CheckSlope(RaycastHit hit)
        {
            // Calculate the angle between the ground normal and the up vector
            float angle = Vector3.Angle(hit.normal, Vector3.up);

            // Store the slope hit
            _slopeHit = hit;

            // Determine if we're on a slope and how steep it is
            _isOnSlope = angle > 0.1f;
            _isSlidingDownSlope = angle > _maxSlopeAngle;

            if (_debug && _showSlopeData)
            {
                Debug.DrawRay(hit.point, hit.normal, _isSlidingDownSlope ? Color.red : (_isOnSlope ? Color.yellow : Color.green), 0.1f);

                if (_isOnSlope)
                {
                    Vector3 slopeDirection = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
                    Debug.DrawRay(hit.point, slopeDirection * 0.5f, Color.blue, 0.1f);
                }
            }
        }

        private void HandleSlopes()
        {
            if (!_isGrounded) return;

            if (_isOnSlope)
            {
                // Calculate the angle between the ground normal and the up vector
                float slopeAngle = Vector3.Angle(_slopeHit.normal, Vector3.up);

                if (slopeAngle > _maxSlopeAngle)
                {
                    // We're on a slope that's too steep - slide down
                    Vector3 slopeDirection = Vector3.ProjectOnPlane(Vector3.down, _slopeHit.normal).normalized;
                    float slideForce = _slopeSlideSpeed * (slopeAngle / 90.0f);

                    // Cancel upward velocity to prevent climbing
                    if (_playerRigidbody.linearVelocity.y > 0)
                    {
                        _playerRigidbody.linearVelocity = new Vector3(_playerRigidbody.linearVelocity.x, 0, _playerRigidbody.linearVelocity.z);
                    }

                    // Apply sliding force
                    _playerRigidbody.AddForce(slopeDirection * slideForce, ForceMode.Acceleration);

                    if (_debug && _showSlopeData)
                    {
                        Debug.DrawRay(transform.position, slopeDirection * slideForce * 0.1f, Color.red);
                    }
                }
                else
                {
                    // On a navigable slope
                    // Restrict jump if moving uphill and slope is steep
                    if (slopeAngle > 30f && Vector3.Dot(_moveDirection, _slopeHit.normal) < 0)
                    {
                        // Reduce jump count if player is moving uphill on a steep slope
                        _jumpCount = Mathf.Min(_jumpCount, _maxJumps - 1);
                    }

                    // Apply downward force to keep player grounded on steep slopes
                    if (slopeAngle > 20f)
                    {
                        float downforceAmount = _slopeForceFactor * (slopeAngle / _maxSlopeAngle);
                        _playerRigidbody.AddForce(-_slopeHit.normal * downforceAmount, ForceMode.Acceleration);
                    }
                }
            }
        }

        private void CheckForSteps()
        {
            if (!_isGrounded || _isSlidingDownSlope || _stepRayBottom == null || _stepRayTop == null)
                return;

            // Front raycast
            TryStepCheck(Vector3.forward);

            // 45-degree raycasts
            TryStepCheck(new Vector3(1f, 0, 1f).normalized);
            TryStepCheck(new Vector3(-1f, 0, 1f).normalized);
        }

        private void TryStepCheck(Vector3 direction)
        {
            Vector3 worldDirection = transform.TransformDirection(direction);

            if (_debug)
            {
                Debug.DrawRay(_stepRayBottom.position, worldDirection * _stepCheckDistance, Color.red);
                Debug.DrawRay(_stepRayTop.position, worldDirection * _stepCheckDistance, Color.green);
            }

            // Check for obstacles at the lower height
            if (Physics.Raycast(_stepRayBottom.position, worldDirection, out RaycastHit hitLower, _stepCheckDistance))
            {
                // Check if there's space at the higher position
                if (!Physics.Raycast(_stepRayTop.position, worldDirection, out _, _stepCheckDistance))
                {
                    // We can step up here
                    float stepUpAmount = _stepSmoothing * Time.fixedDeltaTime *
                                        (Vector3.Dot(_playerRigidbody.linearVelocity.normalized, worldDirection) + 0.5f);

                    // Apply step-up motion
                    _playerRigidbody.position += Vector3.up * stepUpAmount;

                    // Maintain horizontal momentum
                    float currentSpeedMagnitude = new Vector3(_playerRigidbody.linearVelocity.x, 0, _playerRigidbody.linearVelocity.z).magnitude;

                    // If we're moving fast, add a small boost in the direction we're going to overcome the step
                    if (currentSpeedMagnitude > 1f)
                    {
                        Vector3 horizontalBoost = worldDirection * stepUpAmount * 2f;
                        _playerRigidbody.AddForce(horizontalBoost, ForceMode.VelocityChange);
                    }
                }
            }
        }

        private void HandleMovement()
        {
            // Get input for movement
            float moveX = _inputManager.Move.x;
            float moveZ = _inputManager.Move.y;

            // Create movement vector relative to player orientation
            Vector3 forward = transform.forward * moveZ;
            Vector3 right = transform.right * moveX;
            _moveDirection = (forward + right).normalized;

            // Determine target speed based on movement state
            bool isWalking = _inputManager.Walk;
            bool isSprinting = _inputManager.Sprint;
            float targetSpeed = isSprinting ? _sprintSpeed :
                   (isWalking ? _walkSpeed :
                   (_isCrouching ? _crouchSpeed : _runSpeed));

            // Calculate the desired velocity vector
            _desiredVelocity = _moveDirection * targetSpeed;

            // Get current horizontal velocity
            Vector3 currentHorizontalVelocity = new Vector3(_playerRigidbody.linearVelocity.x, 0, _playerRigidbody.linearVelocity.z);
            float currentSpeed = currentHorizontalVelocity.magnitude;

            // Calculate acceleration to use
            float accelRate = (currentSpeed < 0.1f || Vector3.Dot(currentHorizontalVelocity.normalized, _moveDirection) < 0)
                ? _deceleration  // Use deceleration when stopping or changing direction
                : _acceleration; // Use acceleration when speeding up

            // No input case - apply deceleration
            if (_moveDirection.magnitude < 0.1f)
            {
                // Apply friction/deceleration force when no input
                if (_isGrounded && currentSpeed > 0.1f)
                {
                    Vector3 decelerationForce = -currentHorizontalVelocity.normalized * _deceleration;
                    _playerRigidbody.AddForce(decelerationForce, ForceMode.Acceleration);

                    if (_debug && _showMovementForces)
                    {
                        Debug.DrawRay(transform.position, decelerationForce * 0.1f, Color.red);
                    }
                }

                return;
            }

            // Apply different forces based on grounded state
            if (_isGrounded)
            {
                // Handle different ground movement scenarios
                if (_isOnSlope && !_isSlidingDownSlope)
                {
                    // Project movement onto slope
                    Vector3 slopeDirection = Vector3.ProjectOnPlane(_moveDirection, _slopeHit.normal).normalized;

                    // Calculate slope angle for dynamic handling
                    float slopeAngle = Vector3.Angle(_slopeHit.normal, Vector3.up);

                    // Apply appropriate force based on slope angle
                    float slopeFactor = 1.0f - (slopeAngle / _maxSlopeAngle) * 0.3f; // Reduce speed on steeper slopes
                    Vector3 movementForce = accelRate * targetSpeed * slopeFactor * slopeDirection;

                    // Calculate the amount of force needed to reach the desired velocity
                    Vector3 velocityDifference = (_desiredVelocity - currentHorizontalVelocity);
                    Vector3 forceToApply = Vector3.ClampMagnitude(velocityDifference * accelRate, accelRate * targetSpeed);

                    // Project force along slope
                    forceToApply = Vector3.ProjectOnPlane(forceToApply, _slopeHit.normal);

                    // Apply the force
                    _playerRigidbody.AddForce(forceToApply, ForceMode.Acceleration);

                    if (_debug && _showMovementForces)
                    {
                        Debug.DrawRay(transform.position, forceToApply * 0.1f, Color.yellow);
                    }
                }
                else if (!_isSlidingDownSlope)
                {
                    // Regular ground movement - calculate the force needed to reach desired velocity
                    Vector3 velocityDifference = (_desiredVelocity - currentHorizontalVelocity);
                    Vector3 forceToApply = Vector3.ClampMagnitude(velocityDifference * accelRate, accelRate * targetSpeed);

                    // Apply the force
                    _playerRigidbody.AddForce(forceToApply, ForceMode.Acceleration);

                    if (_debug && _showMovementForces)
                    {
                        Debug.DrawRay(transform.position, forceToApply * 0.1f, Color.green);
                    }
                }
            }
            else
            {
                // Air control - more limited but still responsive
                Vector3 airMovement = targetSpeed * _airControlFactor * _moveDirection;

                // Calculate air control force with less direct control
                Vector3 velocityDifference = (airMovement - currentHorizontalVelocity);

                // Apply directional inertia - keep momentum in the direction of travel
                // but allow some control to change direction
                float currentDot = Vector3.Dot(currentHorizontalVelocity.normalized, _moveDirection);
                float controlFactor = Mathf.Lerp(0.2f, 1.0f, (1f - currentDot) * 0.5f);

                // We maintain more momentum when trying to change direction in air
                Vector3 airForce = velocityDifference * _acceleration * _airControlFactor * controlFactor;
                airForce = Vector3.ClampMagnitude(airForce, _acceleration * _airControlFactor * targetSpeed);

                _playerRigidbody.AddForce(airForce, ForceMode.Acceleration);

                if (_debug && _showMovementForces)
                {
                    Debug.DrawRay(transform.position, airForce * 0.2f, Color.blue);
                }
            }

            // Limit horizontal velocity to prevent excessive speed
            LimitHorizontalVelocity(targetSpeed);

            // Update movement states for external access
            IsWalking = _isWalking && _moveDirection.magnitude > 0.1f;
            IsSprinting = _isSprinting && _moveDirection.magnitude > 0.1f;
        }

        private void LimitHorizontalVelocity(float maxSpeed)
        {
            // Get horizontal velocity
            Vector3 horizontalVelocity = new Vector3(_playerRigidbody.linearVelocity.x, 0, _playerRigidbody.linearVelocity.z);

            // If we're sliding down a slope, allow higher speeds
            if (_isSlidingDownSlope)
            {
                maxSpeed *= 1.5f;
            }

            // If we're faster than max speed, limit it
            if (horizontalVelocity.magnitude > maxSpeed)
            {
                Vector3 limitedVelocity = horizontalVelocity.normalized * maxSpeed;
                _playerRigidbody.linearVelocity = new Vector3(limitedVelocity.x, _playerRigidbody.linearVelocity.y, limitedVelocity.z);
            }
        }

        private void Jump()
        {
            // Reset vertical velocity before applying jump force for consistent jump height
            Vector3 currentVel = _playerRigidbody.linearVelocity;
            _playerRigidbody.linearVelocity = new Vector3(currentVel.x, 0, currentVel.z);

            // Calculate the jump force vector
            Vector3 jumpVector = Vector3.up * _jumpForce;

            // If we're on a slope, adjust jump direction slightly to jump away from the slope
            if (_isGrounded && _isOnSlope && !_isSlidingDownSlope)
            {
                jumpVector = (_slopeHit.normal + Vector3.up).normalized * _jumpForce;
            }

            // Apply the jump force
            _playerRigidbody.AddForce(jumpVector, ForceMode.Impulse);

            // Increment jump counter and update state
            _jumpCount++;
            _isGrounded = false;
            _timeSinceGrounded = 0.5f; // Prevent immediate re-detection of ground

            // Set jump animation
            if (_playerAnim != null)
            {
                _playerAnim.SetJump(true);

                // Optional: Reset jump animation after a delay
                // You may need to implement a coroutine or use Invoke for this
                Invoke(nameof(ResetJumpAnimation), 0.5f);
            }
        }

        private void ResetJumpAnimation()
        {
            if (_playerAnim != null)
            {
                _playerAnim.SetJump(false);
            }
        }

        private void HandleCrouch()
        {
            bool crouchInput = _inputManager.Crouch;

            // Toggle crouch state on button press (not hold)
            if (crouchInput && !_wasCrouchPressed)
            {
                _isCrouching = !_isCrouching;
            }
            _wasCrouchPressed = crouchInput;

            // Can't stand up if there's something above us
            if (!_isCrouching && Physics.Raycast(
                transform.position,
                Vector3.up,
                _standingHeight,
                _groundLayers))
            {
                // Use a raycast from the player's center instead of base
                Vector3 rayStart = transform.position + Vector3.up * (_currentHeight * 0.5f);
                float rayLength = _standingHeight - _currentHeight + 0.1f; // Add a small buffer

                if (Physics.Raycast(rayStart, Vector3.up, rayLength, _groundLayers))
                {
                    _isCrouching = true;
                }
            }

            // Set target height based on crouch state
            _targetHeight = _isCrouching ? _crouchHeight : _standingHeight;

            // Only process if height is changing
            if (!Mathf.Approximately(_currentHeight, _targetHeight))
            {
                // Store the previous height for position adjustment
                float previousHeight = _currentHeight;

                // Smoothly interpolate height
                _currentHeight = Mathf.Lerp(
                    _currentHeight,
                    _targetHeight,
                    Time.deltaTime * _crouchTransitionSpeed
                );

                // Calculate position adjustment to prevent "sinking" into ground
                float heightDifference = previousHeight - _currentHeight;

                // Apply height to collider
                _playerCollider.height = _currentHeight;
                _playerCollider.center = new Vector3(0, _currentHeight / 2f, 0);

                // If the character is getting shorter, we need to move them up slightly
                // to prevent them from "sinking" into the ground
                if (_isCrouching && heightDifference > 0)
                {
                    // Move player up by half the height difference to maintain foot position
                    transform.position += Vector3.up * (heightDifference * 0.5f);
                }
            }

            // Update animator if available
            if (_playerAnim != null)
            {
                _playerAnim.SetCrouch(_isCrouching);
            }
        }

        private void HandleMouseLook()
        {
            // Get mouse input with sensitivity adjustment
            float mouseX = _inputManager.Look.x * (0.1f / _lookSensitivity);
            float mouseY = _inputManager.Look.y * (0.1f / _lookSensitivity);

            // Apply smoothing for more controlled feel
            _targetLookDelta = new Vector2(mouseX, mouseY);
            _currentLookDelta = Vector2.Lerp(_currentLookDelta, _targetLookDelta, Time.deltaTime * (1f / _lookSmoothness) * 10f);

            // Calculate vertical camera rotation with inverted Y
            _rotationX -= _currentLookDelta.y;
            _rotationX = Mathf.Clamp(_rotationX, -_maxLookAngle, _maxLookAngle);

            // Apply rotation to camera
            if (_cameraHolder != null)
            {
                _cameraHolder.localRotation = Quaternion.Euler(_rotationX, 0, _currentCameraLean);
            }
            else
            {
                Debug.LogError("Camera holder is null. Please assign it in the inspector.");
            }

            // Apply horizontal rotation to the player body
            transform.Rotate(Vector3.up * _currentLookDelta.x);
        }

        private void UpdateCameraLean()
        {
            // Calculate target lean based on strafing input
            float strafeInput = _inputManager.Move.x;

            // If moving, apply strafe lean
            if (Mathf.Abs(strafeInput) > 0.1f && _isGrounded)
            {
                _targetCameraLean = -strafeInput * _cameraLeanAmount;
            }
            else
            {
                // Otherwise reset lean
                _targetCameraLean = 0;
            }

            // Smoothly interpolate current lean towards target
            _currentCameraLean = Mathf.Lerp(_currentCameraLean, _targetCameraLean, Time.deltaTime * _cameraLeanSpeed);
        }

        private void ApplyCameraWobble()
        {
            Vector3 basePosition = _originalCameraPosition;

            // If crouching, adjust the base position
            if (_isCrouching)
            {
                float crouchProgress = 1 - (_currentHeight - _crouchHeight) / (_standingHeight - _crouchHeight);
                float crouchingCameraY = _originalCameraPosition.y - (_standingHeight - _crouchHeight) * 0.7f;
                basePosition.y = Mathf.Lerp(_originalCameraPosition.y, crouchingCameraY, crouchProgress);
            }

            Vector3 targetCameraPosition = basePosition;

            // Get movement magnitude (ignoring y movement)
            Vector3 flatVelocity = new(_playerVelocity.x, 0, _playerVelocity.z);
            float movementMagnitude = flatVelocity.magnitude;

            // Only bob when moving on ground
            if (movementMagnitude > 0.1f && _isGrounded)
            {
                // Set target values based on running state
                float targetBobFrequency = IsSprinting ? _sprintBobFrequency : (IsWalking ? _walkBobFrequency : _runBobFrequency);
                float targetBobAmount = IsSprinting ? _sprintBobAmount : (IsWalking ? _walkBobAmount : _runBobAmount);

                // Smoothly transition between walk and run parameters
                if (_currentBobFrequency == 0) _currentBobFrequency = targetBobFrequency;
                if (_currentBobAmount == 0) _currentBobAmount = targetBobAmount;

                _currentBobFrequency = Mathf.Lerp(_currentBobFrequency, targetBobFrequency, Time.deltaTime * 3f);
                _currentBobAmount = Mathf.Lerp(_currentBobAmount, targetBobAmount, Time.deltaTime * 5f);

                // Calculate the reference speed based on the current movement state
                float referenceSpeed = IsSprinting ? _sprintSpeed : (IsWalking ? _walkSpeed : _runSpeed);

                // Calculate a normalized speed factor with a hard cap
                // This ensures speedFactor never goes above 1.5, even at extreme speeds
                float speedFactor = Mathf.Min(movementMagnitude / referenceSpeed, 1.5f);

                // Add a minimum value to prevent very slow bobbing at low speeds
                speedFactor = Mathf.Max(speedFactor, 0.5f);

                // Increase timer based on frequency and clamped speed factor
                _bobTimer += Time.deltaTime * _currentBobFrequency * speedFactor;

                // Calculate bob offset
                float verticalBob = Mathf.Sin(_bobTimer) * _currentBobAmount;
                float horizontalBob = Mathf.Sin(_bobTimer * 0.5f) * _currentBobAmount * _horizontalMultiplier;

                // Apply vertical and horizontal bob
                targetCameraPosition += new Vector3(horizontalBob, verticalBob, 0);
            }
            else
            {
                // When not moving, gradually reset bob timers
                _bobTimer = 0;
            }

            // Apply landing bob effect
            if (_landTimer > 0)
            {
                float landingProgress = 1 - (_landTimer / _landBobDuration);
                float landingBobAmount = _landBobAmount * (1 - Mathf.Pow(landingProgress, 2));
                targetCameraPosition.y -= landingBobAmount;
                _landTimer -= Time.deltaTime;
            }

            // Apply breathing effect when still
            if (movementMagnitude < 0.1f && _isGrounded)
            {
                float breathingEffect = Mathf.Sin(Time.time * _breathingFrequency) * _breathingAmount;
                targetCameraPosition.y += breathingEffect;
            }

            // Apply position changes with smoothing
            _cameraHolder.localPosition = Vector3.Lerp(
                _cameraHolder.localPosition,
                targetCameraPosition,
                Time.deltaTime * _wobbleSmoothing
            );
        }

        // Public method for external impulses (for telekinesis effects)
        public void AddImpulseToCameraWobble(Vector3 impulse, float duration = 0.2f)
        {
            _cameraHolder.localPosition += impulse;
        }

        // For external scripts to disable/enable movement
        public void SetMovementEnabled(bool enabled)
        {
            this.enabled = enabled;

            if (!enabled)
            {
                _playerRigidbody.linearVelocity = Vector3.zero;
            }
        }

        // For external scripts to access movement state
        public bool IsMoving()
        {
            return _playerVelocity.magnitude > 0.1f;
        }

        public bool IsRunning()
        {
            Vector3 flatVelocity = new(_playerVelocity.x, 0, _playerVelocity.z);
            return flatVelocity.magnitude > _walkSpeed + 0.5f;
        }

        // Optional: Add footstep sounds based on movement
        public float GetMovementIntensity()
        {
            if (!_isGrounded) return 0;

            Vector3 flatVelocity = new(_playerVelocity.x, 0, _playerVelocity.z);
            return flatVelocity.magnitude / _runSpeed; // 0 to 1 value
        }

        // Add velocity directly (for external forces, pushes, etc.)
        public void AddVelocity(Vector3 velocityToAdd)
        {
            _playerRigidbody.linearVelocity += velocityToAdd;
        }

        // Add external force with optional mode
        public void AddExternalForce(Vector3 force, ForceMode mode = ForceMode.Force)
        {
            _playerRigidbody.AddForce(force, mode);
        }

        private void DrawDebugVisuals()
        {
            if (_showGroundChecks)
            {
                // Show player collider
                DebugExtension.DrawWireCapsule(transform.position, transform.rotation, _playerCollider.radius, _playerCollider.height, Color.cyan);

                // Show grounded sphere
                Vector3 groundCheckPos = transform.position + Vector3.up * (_playerCollider.radius);
                Debug.DrawLine(transform.position, groundCheckPos, Color.magenta);
                DebugExtension.DrawWireSphere(groundCheckPos, Color.green, _groundCheckRadius);
            }

            if (_showMovementForces)
            {
                // Show movement direction
                Debug.DrawRay(transform.position, _moveDirection * 1.5f, Color.blue);

                // Show velocity
                Debug.DrawRay(transform.position, _playerRigidbody.linearVelocity.normalized * 2f, Color.red);

                // Show desired velocity
                Debug.DrawRay(transform.position, _desiredVelocity.normalized * 2.5f, Color.green);
            }
        }
    }

    // Helper class for debug visualization - add this to your project
    public static class DebugExtension
    {
        public static void DrawWireSphere(Vector3 position, Color color, float radius = 1.0f)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.DrawRay(position + Vector3.up * radius, Vector3.right * radius, color);
            UnityEngine.Debug.DrawRay(position + Vector3.up * radius, -Vector3.right * radius, color);
            UnityEngine.Debug.DrawRay(position + Vector3.down * radius, Vector3.right * radius, color);
            UnityEngine.Debug.DrawRay(position + Vector3.down * radius, -Vector3.right * radius, color);

            UnityEngine.Debug.DrawRay(position + Vector3.right * radius, Vector3.up * radius, color);
            UnityEngine.Debug.DrawRay(position + Vector3.right * radius, -Vector3.up * radius, color);
            UnityEngine.Debug.DrawRay(position + Vector3.left * radius, Vector3.up * radius, color);
            UnityEngine.Debug.DrawRay(position + Vector3.left * radius, -Vector3.up * radius, color);

            UnityEngine.Debug.DrawRay(position + Vector3.forward * radius, Vector3.up * radius, color);
            UnityEngine.Debug.DrawRay(position + Vector3.forward * radius, -Vector3.up * radius, color);
            UnityEngine.Debug.DrawRay(position + Vector3.back * radius, Vector3.up * radius, color);
            UnityEngine.Debug.DrawRay(position + Vector3.back * radius, -Vector3.up * radius, color);
#endif
        }

        public static void DrawWireCapsule(Vector3 position, Quaternion rotation, float radius, float height, Color color)
        {
#if UNITY_EDITOR
            Matrix4x4 angleMatrix = Matrix4x4.TRS(position, rotation, Vector3.one);
            Vector3 angleRight = angleMatrix.MultiplyVector(Vector3.right);
            Vector3 angleUp = angleMatrix.MultiplyVector(Vector3.up);
            Vector3 angleForward = angleMatrix.MultiplyVector(Vector3.forward);

            float halfHeight = height / 2;
            Vector3 top = position + angleUp * (halfHeight - radius);
            Vector3 bottom = position + angleUp * -(halfHeight - radius);

            // Draw top and bottom circles
            DrawCircle(top, angleRight, angleForward, color, radius);
            DrawCircle(bottom, angleRight, angleForward, color, radius);

            // Draw vertical lines connecting circles
            UnityEngine.Debug.DrawLine(top + angleRight * radius, bottom + angleRight * radius, color);
            UnityEngine.Debug.DrawLine(top - angleRight * radius, bottom - angleRight * radius, color);
            UnityEngine.Debug.DrawLine(top + angleForward * radius, bottom + angleForward * radius, color);
            UnityEngine.Debug.DrawLine(top - angleForward * radius, bottom - angleForward * radius, color);
#endif
        }

        private static void DrawCircle(Vector3 center, Vector3 right, Vector3 forward, Color color, float radius, int segments = 12)
        {
#if UNITY_EDITOR
            float angle = 0;
            float angleStep = 2 * Mathf.PI / segments;

            Vector3 previousPoint = center + (right * Mathf.Cos(angle) + forward * Mathf.Sin(angle)) * radius;

            for (int i = 0; i < segments + 1; i++)
            {
                angle += angleStep;
                Vector3 nextPoint = center + (right * Mathf.Cos(angle) + forward * Mathf.Sin(angle)) * radius;
                UnityEngine.Debug.DrawLine(previousPoint, nextPoint, color);
                previousPoint = nextPoint;
            }
#endif
        }
    }
}