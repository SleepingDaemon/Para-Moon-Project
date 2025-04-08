using System.Collections.Generic;
using UnityEngine;

namespace ParaMoon
{
    /// <summary>
    /// Sensor class for detecting collisions and surfaces in a 3D environment.
    /// </summary>
    public class Sensor
    {
        // Cast direction enum
        public enum CastDirection { Up, Down, Left, Right, Forward, Backward, Custom }

        // Cast type enum
        public enum CastType { Raycast, SphereCast }

        // Sensor properties
        public float castLength = 1f;
        public float sphereCastRadius = 0.25f;
        public CastType castType = CastType.SphereCast;
        public int layermask = -1;
        public bool calculateRealDistance = true;
        public bool calculateRealSurfaceNormal = true;

        // Ray array properties
        public int ArrayRows = 1;
        public int arrayRayCount = 1;
        public bool offsetArrayRows = true;
        public bool isInDebugMode = false;

        // Internal variables
        Vector3 _castOrigin;
        Vector3 _castDir = Vector3.down;
        bool _hasDetectedHit = false;
        RaycastHit _closestHit;
        List<RaycastHit> _allHits = new List<RaycastHit>();
        Vector3[] _raycastArrayPositions;

        // Constructor
        public Sensor()
        {
            RecalibrateRaycastArrayPositions();
        }

        // Set cast origin point
        public void SetCastOrigin(Vector3 origin)
        {
            _castOrigin = origin;
        }

        // Set direction to cast
        public void SetCastDirection(CastDirection direction)
        {
            UpdateCastDirection(direction);
        }

        // Set custom direction
        public void SetCustomDirection(Vector3 direction)
        {
            _castDir = direction.normalized;
        }

        // Update the actual vector for casting based on the direction enum
        private void UpdateCastDirection(CastDirection direction)
        {
            switch (direction)
            {
                case CastDirection.Up:
                    _castDir = Vector3.up;
                    break;
                case CastDirection.Down:
                    _castDir = Vector3.down;
                    break;
                case CastDirection.Left:
                    _castDir = Vector3.left;
                    break;
                case CastDirection.Right:
                    _castDir = Vector3.right;
                    break;
                case CastDirection.Forward:
                    _castDir = Vector3.forward;
                    break;
                case CastDirection.Backward:
                    _castDir = Vector3.back;
                    break;
                case CastDirection.Custom:
                    // Keep existing custom direction
                    break;
            }
        }

        // Recalculate array positions for multi-raycasting
        public void RecalibrateRaycastArrayPositions()
        {
            // Ensure at least one row and ray
            ArrayRows = Mathf.Max(1, ArrayRows);
            arrayRayCount = Mathf.Max(1, arrayRayCount);

            // Calculate total number of rays
            int totalRays = (arrayRayCount <= 1) ? 1 : ((ArrayRows <= 1) ? arrayRayCount : arrayRayCount * ArrayRows);
            _raycastArrayPositions = new Vector3[totalRays];

            // Single ray case
            if (totalRays == 1)
            {
                _raycastArrayPositions[0] = Vector3.zero;
                return;
            }

            // Multiple rays in a single row
            if (ArrayRows <= 1)
            {
                float angleStep = 360f / arrayRayCount;
                for (int i = 0; i < arrayRayCount; i++)
                {
                    float angle = angleStep * i;
                    Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                    _raycastArrayPositions[i] = direction * sphereCastRadius;
                }
                return;
            }

            // Multiple rays in multiple rows
            float rowStep = sphereCastRadius / ArrayRows;

            int rayIndex = 0;
            for (int row = 0; row < ArrayRows; row++)
            {
                float currentRadius = rowStep * (row + 1);
                float currentAngleStep = 360f / arrayRayCount;
                float angleOffset = offsetArrayRows ? (currentAngleStep * 0.5f * (row % 2)) : 0f;

                for (int i = 0; i < arrayRayCount; i++)
                {
                    float angle = (currentAngleStep * i) + angleOffset;
                    Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                    _raycastArrayPositions[rayIndex] = direction * currentRadius;
                    rayIndex++;
                }
            }
        }

        // Perform the cast operation
        public void Cast()
        {
            _hasDetectedHit = false;
            float closestDistance = float.MaxValue;

            // Calculate reference plane for distance calculation
            Plane referencePlane = new Plane(_castDir, _castOrigin);

            // Reset hit list
            _allHits.Clear();

            // Loop through each ray position in the array
            for (int i = 0; i < _raycastArrayPositions.Length; i++)
            {
                Vector3 origin = _castOrigin + Vector3.ProjectOnPlane(_raycastArrayPositions[i], _castDir);
                bool hitDetected = false;
                RaycastHit hit = new RaycastHit();

                // Perform raycast or spherecast based on cast type
                if (castType == CastType.Raycast)
                {
                    hitDetected = Physics.Raycast(origin, _castDir, out hit, castLength, layermask);

                    if (isInDebugMode)
                        Debug.DrawRay(origin, _castDir * castLength, hitDetected ? Color.green : Color.red, 0.01f);
                }
                else // SphereCast
                {
                    hitDetected = Physics.SphereCast(origin, sphereCastRadius, _castDir, out hit, castLength, layermask);

                    if (isInDebugMode)
                    {
                        Debug.DrawRay(origin, _castDir * castLength, hitDetected ? Color.green : Color.red, 0.01f);
                        if (hitDetected)
                            Debug.DrawLine(origin, hit.point, Color.yellow, 0.01f);
                    }
                }

                // Process hit results
                if (hitDetected)
                {
                    // Store hit data
                    _allHits.Add(hit);

                    // Calculate real distance from the origin plane
                    float distance = hit.distance;
                    if (calculateRealDistance)
                    {
                        distance = Mathf.Abs(referencePlane.GetDistanceToPoint(hit.point));
                    }

                    // Track closest hit
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        _closestHit = hit;
                        _hasDetectedHit = true;
                    }
                }
            }

            // Calculate adjusted surface normal if needed
            if (_hasDetectedHit && calculateRealSurfaceNormal && _allHits.Count > 1)
            {
                // Use average of all hit normals for more stable surface detection
                Vector3 averageNormal = Vector3.zero;
                foreach (var hit in _allHits)
                {
                    averageNormal += hit.normal;
                }
                averageNormal.Normalize();
                _closestHit.normal = averageNormal;
            }
        }

        // Draw debug visualization - called from owner object's OnDrawGizmos
        public void DrawDebugVisualization()
        {
            if (!isInDebugMode || !_hasDetectedHit) return;

            // Draw ray array origin
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_castOrigin, 0.05f);

            // Draw hit point
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_closestHit.point, 0.05f);

            // Draw normal
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(_closestHit.point, _closestHit.normal * 0.5f);

            // Draw ray directions
            Gizmos.color = Color.cyan;
            foreach (Vector3 rayPos in _raycastArrayPositions)
            {
                Vector3 origin = _castOrigin + Vector3.ProjectOnPlane(rayPos, _castDir);
                Gizmos.DrawLine(origin, origin + _castDir * 0.2f);
            }
        }

        // Public accessors
        public bool HasDetectedHit() { return _hasDetectedHit; }
        public float GetDistance() { return _closestHit.distance; }
        public Vector3 GetNormal() { return _closestHit.normal; }
        public Vector3 GetPosition() { return _closestHit.point; }
        public Collider GetCollider() { return _closestHit.collider; }
        public RaycastHit GetHitInfo() { return _closestHit; }
    }
}