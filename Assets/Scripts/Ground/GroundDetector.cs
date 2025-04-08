using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ParaMoon
{
    public class GroundDetector : MonoBehaviour
    {
        [Header("Collider Settings")]
        [SerializeField] float _colliderHeight = 2f;
        [SerializeField] float _colliderThickness = 1f;
        [SerializeField][Range(0f, 1f)] float _stepHeightRatio = 0.25f;
        [SerializeField] float _sensorRadiusModifier = 0.95f;

        [Header("Sensor Settings")]
        [SerializeField] Sensor.CastType _sensorType = Sensor.CastType.SphereCast;
        [SerializeField] int _sensorArrayRows = 2;
        [SerializeField] int _sensorArrayRayCount = 8;
        [SerializeField] bool _sensorArrayRowsAreOffset = true;
        [SerializeField] bool _isInDebugMode = false;
        [SerializeField] bool _isUsingExtendedSensorRange = false;

        Rigidbody _rigidbody;
        Collider _collider;
        BoxCollider _boxCollider;
        SphereCollider _sphereCollider;
        CapsuleCollider _capsuleCollider;
        Transform _transform;
        Sensor _sensor;
        int _currentLayer;

        // Sensor Properties
        float _baseSensorRange;

        // State
        bool _isGrounded = false;
        bool _wasGroundedLastFrame = false;
        bool _hasJustLanded = false;
        Vector3 _currentGroundAdjustmentVelocity = Vector3.zero;

        private void Awake()
        {
            Setup();
        }

        private void Setup()
        {
            _transform = transform;
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();

            _boxCollider = _collider as BoxCollider;
            _sphereCollider = _collider as SphereCollider;
            _capsuleCollider = _collider as CapsuleCollider;

            _sensor = new();

            RecalibrateSensor();
        }

        //Recalibrate sensor variables;
        void RecalibrateSensor()
        {
            //Set sensor ray origin and direction;
            _sensor.SetCastOrigin(GetColliderCenter());
            _sensor.SetCastDirection(Sensor.CastDirection.Down);

            //Calculate sensor layermask;
            RecalculateSensorLayerMask();

            //Set sensor cast type;
            _sensor.castType = _sensorType;

            //Calculate sensor radius/width;
            float _radius = _colliderThickness / 2f * _sensorRadiusModifier;

            //Multiply all sensor lengths with '_safetyDistanceFactor' to compensate for floating point errors;
            float _safetyDistanceFactor = 0.001f;

            //Fit collider height to sensor radius;
            if (_boxCollider)
                _radius = Mathf.Clamp(_radius, _safetyDistanceFactor, (_boxCollider.size.y / 2f) * (1f - _safetyDistanceFactor));
            else if (_sphereCollider)
                _radius = Mathf.Clamp(_radius, _safetyDistanceFactor, _sphereCollider.radius * (1f - _safetyDistanceFactor));
            else if (_capsuleCollider)
                _radius = Mathf.Clamp(_radius, _safetyDistanceFactor, (_capsuleCollider.height / 2f) * (1f - _safetyDistanceFactor));

            //Set sensor variables;

            //Set sensor radius;
            _sensor.sphereCastRadius = _radius * _transform.localScale.x;

            //Calculate and set sensor length;
            float _length = 0f;
            _length += (_colliderHeight * (1f - _stepHeightRatio)) * 0.5f;
            _length += _colliderHeight * _stepHeightRatio;
            _baseSensorRange = _length * (1f + _safetyDistanceFactor) * _transform.localScale.x;
            _sensor.castLength = _length * _transform.localScale.x;

            //Set sensor array variables;
            _sensor.ArrayRows = _sensorArrayRows;
            _sensor.arrayRayCount = _sensorArrayRayCount;
            _sensor.offsetArrayRows = _sensorArrayRowsAreOffset;
            _sensor.isInDebugMode = _isInDebugMode;

            //Set sensor spherecast variables;
            _sensor.calculateRealDistance = true;
            _sensor.calculateRealSurfaceNormal = true;

            //Recalibrate sensor to the new values;
            _sensor.RecalibrateRaycastArrayPositions();
        }

        //Recalculate sensor layermask based on current physics settings;
        void RecalculateSensorLayerMask()
        {
            int _layerMask = 0;
            int _objectLayer = this.gameObject.layer;

            //Calculate layermask;
            for (int i = 0; i < 32; i++)
            {
                if (!Physics.GetIgnoreLayerCollision(_objectLayer, i))
                    _layerMask = _layerMask | (1 << i);
            }

            //Make sure that the calculated layermask does not include the 'Ignore Raycast' layer;
            if (_layerMask == (_layerMask | (1 << LayerMask.NameToLayer("Ignore Raycast"))))
            {
                _layerMask ^= (1 << LayerMask.NameToLayer("Ignore Raycast"));
            }

            //Set sensor layermask;
            _sensor.layermask = _layerMask;

            //Save current layer;
            _currentLayer = _objectLayer;
        }

        //Returns the collider's center in world coordinates;
        Vector3 GetColliderCenter()
        {
            if (_collider == null)
                Setup();

            return _collider.bounds.center;
        }

        //Check if mover is grounded;
        //Store all relevant collision information for later;
        //Calculate necessary adjustment velocity to keep the correct distance to the ground;
        void Check()
        {
            //Reset ground adjustment velocity;
            _currentGroundAdjustmentVelocity = Vector3.zero;

            //Set sensor length;
            if (_isUsingExtendedSensorRange)
                _sensor.castLength = _baseSensorRange + (_colliderHeight * _transform.localScale.x) * _stepHeightRatio;
            else
                _sensor.castLength = _baseSensorRange;

            _sensor.Cast();

            //If sensor has not detected anything, set flags and return;
            if (!_sensor.HasDetectedHit())
            {
                _isGrounded = false;
                return;
            }

            //Set flags for ground detection;
            _isGrounded = true;

            //Get distance that sensor ray reached;
            float _distance = _sensor.GetDistance();

            //Calculate how much mover needs to be moved up or down;
            float _upperLimit = ((_colliderHeight * _transform.localScale.x) * _stepHeightRatio) * 0.5f;
            float _middle = _upperLimit + (_colliderHeight * _transform.localScale.x) * _stepHeightRatio;
            float _distanceToGo = _middle - _distance;

            //Set new ground adjustment velocity for the next frame;
            _currentGroundAdjustmentVelocity = _transform.up * (_distanceToGo / Time.fixedDeltaTime);
        }

        //Check if mover is grounded;
        public void CheckForGround()
        {
            //Check if object layer has been changed since last frame;
            //If so, recalculate sensor layer mask;
            if (_currentLayer != this.gameObject.layer)
                RecalculateSensorLayerMask();

            _wasGroundedLastFrame = _isGrounded;
            Check();
            _hasJustLanded = !_wasGroundedLastFrame && _isGrounded;
        }

        //Set mover velocity;
        public void SetVelocity(Vector3 _velocity)
        {
            _rigidbody.linearVelocity = _velocity + _currentGroundAdjustmentVelocity;
        }

        //Returns 'true' if mover is touching ground and the angle between hte 'up' vector and ground normal is not too steep (e.g., angle < slope_limit);
        public bool IsGrounded()
        {
            return _isGrounded;
        }
    }
}