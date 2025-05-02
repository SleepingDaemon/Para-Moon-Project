using UnityEngine;

namespace ParaMoon
{
    /**
     * Simple component that makes an object always face the camera.
     * Useful for name labels and UI elements in world space.
     *
     * Usage:
     * - Attach to any GameObject that should always face the camera
     * - Typically used for text above highlighted objects
     */
    public class Billboard : MonoBehaviour
    {
        Camera _mainCamera;

        /**
         * Find the main camera on start.
         */
        private void Start()
        {
            _mainCamera = Camera.main;
        }

        /**
         * Update rotation to face the camera each frame.
         * Using LateUpdate ensures this happens after all other updates.
         */
        private void LateUpdate()
        {
            if (_mainCamera != null)
                transform.rotation = _mainCamera.transform.rotation;
        }
    }
}