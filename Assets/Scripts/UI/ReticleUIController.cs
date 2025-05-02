using UnityEngine;
using UnityEngine.UI;

namespace ParaMoon
{
    /** 
     * ReticleUIController is responsible for updating the position of the reticle UI element
     * based on the vertical offset provided by the game logic.
     */
    public class ReticleUIController : MonoBehaviour
    {
        [SerializeField] Image _reticleImage;
        [SerializeField] float _reticleBaseYPosition = 0f;
        [SerializeField] float _screenConversionFactor = 100f;
        [SerializeField] bool _useAccurateScreenProjection = true;

        RectTransform _reticleRect;
        Vector2 _reticleOriginPosition;
        Camera _mainCamera;
        Canvas _parentCanvas;

        private void OnValidate()
        {
            UpdateReticlePosition(_reticleBaseYPosition);
        }

        private void Start()
        {
            if (_reticleImage != null)
            {
                _reticleRect = _reticleImage.GetComponent<RectTransform>();
                _reticleOriginPosition = _reticleRect.anchoredPosition;
                _mainCamera = Camera.main;
                _parentCanvas = GetComponentInParent<Canvas>();
            }
            else
            {
                Debug.LogError("Reticle Image is not assigned in the inspector.");
            }
        }

        /**
         * Updates the reticle position based on the vertical offset.
         * This method should be called whenever the vertical offset changes.
         * 
         * @param verticalOffset The vertical offset to apply to the reticle position
         */
        public void UpdateReticlePosition(float verticalOffset)
        {
            if (_reticleRect == null)
                return;

            float screenOffset;

            if (_useAccurateScreenProjection && _mainCamera != null && _parentCanvas != null)
            {
                // Get camera's center point
                Vector3 cameraCenter = _mainCamera.transform.position;

                // Get the position with offset
                Vector3 offsetPosition = cameraCenter + (_mainCamera.transform.up * verticalOffset);

                // Project both positions to screen space
                Vector3 centerScreen = _mainCamera.WorldToScreenPoint(cameraCenter);
                Vector3 offsetScreen = _mainCamera.WorldToScreenPoint(offsetPosition);

                // Calculate the difference in Y position
                screenOffset = offsetScreen.y - centerScreen.y;

                // Adjust based on canvas scaling
                if (_parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    // no adjustment needed
                }
                else if (_parentCanvas.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    // Adjust for camera space
                    screenOffset *= _parentCanvas.scaleFactor;
                }
            }
            else
            {
                // Fallback to a simple calculation
                screenOffset = verticalOffset * _screenConversionFactor;
            }

            // Debug.Log($"World offset: {verticalOffset}, Screen offset: {screenOffset}");

            // Update the reticle position
            _reticleRect.anchoredPosition = new Vector2(
                _reticleOriginPosition.x,
                _reticleBaseYPosition + screenOffset
                );
        }
    }
}