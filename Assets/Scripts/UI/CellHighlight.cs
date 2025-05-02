using System;
using UnityEngine;
using UnityEngine.UI;

namespace ParaMoon
{
    public partial class GridInventoryUI
    {
        private class CellHighlight : MonoBehaviour
        {
            private Image _overlayImage;
            private Color _originalColor;

            private void Awake()
            {
                // Create or get overlay image
                _overlayImage = GetComponent<Image>();
                if (_overlayImage == null)
                {
                    _overlayImage = gameObject.AddComponent<Image>();
                }
                _originalColor = _overlayImage.color;
            }

            public void SetHighlight(bool highlight, Color highlightColor = default)
            {
                if (highlight)
                {
                    _overlayImage.color = highlightColor;
                }
                else
                {
                    // Return to original color/transparency
                    _overlayImage.color = _originalColor;
                }
            }
        }
    }
}
