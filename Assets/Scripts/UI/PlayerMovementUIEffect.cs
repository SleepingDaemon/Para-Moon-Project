using UnityEngine;

namespace ParaMoon
{
    public class PlayerMovementUIEffect : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform playerHead;
        [SerializeField] private RectTransform helmetUI;

        [Header("Movement Response")]
        [SerializeField] private float positionResponseStrength = 0.02f;
        [SerializeField] private float rotationResponseStrength = 2.0f;
        [SerializeField] private float returnSpeed = 3.0f;

        private Vector3 lastHeadPosition;
        private Quaternion lastHeadRotation;
        private Vector2 uiOffset = Vector2.zero;
        private float uiRotation = 0f;

        void Start()
        {
            if (playerHead == null)
                playerHead = Camera.main.transform;

            lastHeadPosition = playerHead.position;
            lastHeadRotation = playerHead.rotation;
        }

        void LateUpdate()
        {
            // Calculate movement delta
            Vector3 headDelta = playerHead.position - lastHeadPosition;
            Quaternion rotationDelta = playerHead.rotation * Quaternion.Inverse(lastHeadRotation);

            // Convert rotation to angle axis
            rotationDelta.ToAngleAxis(out float angle, out Vector3 axis);

            // Calculate UI offset based on head movement
            Vector2 movementOffset = new Vector2(-headDelta.x, -headDelta.y) * positionResponseStrength;

            // Calculate rotation effect - simplified to just use the Y component for side-to-side head movement
            float rotationEffect = 0;
            if (!float.IsNaN(angle) && !float.IsInfinity(angle))
                rotationEffect = -angle * axis.y * rotationResponseStrength;

            // Apply movement to UI with smooth damping
            uiOffset = Vector2.Lerp(uiOffset, movementOffset, Time.deltaTime * 10f);
            uiRotation = Mathf.Lerp(uiRotation, rotationEffect, Time.deltaTime * 10f);

            // Return to center position when movement stops
            if (headDelta.magnitude < 0.001f)
                uiOffset = Vector2.Lerp(uiOffset, Vector2.zero, Time.deltaTime * returnSpeed);

            // Apply to UI
            helmetUI.anchoredPosition = uiOffset * 100f; // Scale up for better visibility
            helmetUI.localRotation = Quaternion.Euler(0, 0, uiRotation);

            // Store current positions for next frame
            lastHeadPosition = playerHead.position;
            lastHeadRotation = playerHead.rotation;
        }
    }
}