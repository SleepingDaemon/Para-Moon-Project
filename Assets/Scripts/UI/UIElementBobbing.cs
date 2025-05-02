using UnityEngine;

namespace ParaMoon
{
    /**
     * UIElementBobbing is responsible for creating a bobbing effect on UI elements.
     * It uses sine waves to create a smooth bobbing and rotation effect.
     * 
     * Usage:
     * - Attach this script to the UI element you want to bob.
     * - Adjust the bobbing speed, amount, and rotation settings in the inspector.
     */
    public class UIElementBobbing : MonoBehaviour
    {
        [SerializeField] float _bobbingSpeed = 1f;
        [SerializeField] float _bobbingAmount = 2f;
        [SerializeField] float _rotationSpeed = 0.5f;
        [SerializeField] float _rotationAmount = 1f;

        RectTransform _rectTransform;
        Vector2 _initialPosition;

        void Start()
        {
            _rectTransform = GetComponent<RectTransform>();
            _initialPosition = _rectTransform.anchoredPosition;
        }

        void Update()
        {
            // Calculate bobbing position
            float bobX = Mathf.Sin(Time.time * _bobbingSpeed * 0.7f) * _bobbingAmount;
            float bobY = Mathf.Sin(Time.time * _bobbingSpeed) * _bobbingAmount;

            // Apply bobbing to UI element
            _rectTransform.anchoredPosition = _initialPosition + new Vector2(bobX, bobY);

            // Add subtle rotation
            float rotZ = Mathf.Sin(Time.time * _rotationSpeed) * _rotationAmount;
            _rectTransform.localRotation = Quaternion.Euler(0, 0, rotZ);
        }
    }
}