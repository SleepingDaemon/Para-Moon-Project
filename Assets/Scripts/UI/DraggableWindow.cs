using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ParaMoon
{
    public class DraggableWindow : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
    {
        [SerializeField] RectTransform _menu;
        [SerializeField] RectTransform _dragHeader;

        RectTransform _rectTransform;
        Canvas _canvas;
        CanvasGroup _canvasGroup;
        Vector2 _dragStartPosition;  // Track starting position when drag begins
        Vector3 _pointerStartPosition;  // Track starting pointer position in world space
        Vector2 _initialPosition;
        Vector2 _screenBounds;

        bool _isDragging = false;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();

            // Ensure the CanvasGroup component exists for drag-and-drop functionality
            if (!gameObject.TryGetComponent(out _canvasGroup))
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _initialPosition = _rectTransform.anchoredPosition;

            // Calculate screen bounds for restricting window position
            _screenBounds = new Vector2(Screen.width, Screen.height);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Only allow dragging from the header area
            if (_dragHeader != null)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(_dragHeader, eventData.position, eventData.pressEventCamera))
                    return;
            }

            if (!_isDragging)
            {
                _isDragging = true;
                _canvasGroup.blocksRaycasts = false; // Disable raycasting to allow drop events

                // Store the starting position of the window and pointer
                _dragStartPosition = _rectTransform.anchoredPosition;
                _pointerStartPosition = eventData.position;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_canvas == null || !_isDragging)
                return;

            // Calculate the delta movement based on pointer movement
            Vector2 pointerDelta = eventData.position - (Vector2)_pointerStartPosition;

            // Apply the delta to the starting position, accounting for canvas scale
            Vector2 newPosition = _dragStartPosition + pointerDelta / _canvas.scaleFactor;

            // Get window dimensions
            Vector2 windowSize = _rectTransform.rect.size * _canvas.scaleFactor;

            // Calculate menu dimensions and position in canvas space
            Vector2 menuSize = Vector2.zero;
            Vector2 menuPosition = Vector2.zero;
            float menuTop = 0;

            if (_menu != null)
            {
                menuSize = _menu.rect.size * _canvas.scaleFactor;

                // Get menu's position in canvas space
                Vector3[] menuCorners = new Vector3[4];
                _menu.GetWorldCorners(menuCorners);
                Vector3[] canvasCorners = new Vector3[4];
                (_canvas.transform as RectTransform).GetWorldCorners(canvasCorners);

                // Convert menu position to canvas local space
                Vector2 menuBottomLeft;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvas.transform as RectTransform,
                    RectTransformUtility.WorldToScreenPoint(null, menuCorners[0]),
                    null,
                    out menuBottomLeft);

                menuPosition = menuBottomLeft;
                menuTop = menuPosition.y + menuSize.y;
            }

            // Calculate canvas boundaries in local space, accounting for menu
            Vector2 minBounds = new(-_screenBounds.x / 2 + windowSize.x / 2, -_screenBounds.y / 2 + windowSize.y / 2);
            Vector2 maxBounds = new(_screenBounds.x / 2 - windowSize.x / 2, _screenBounds.y / 2 - windowSize.y / 2);

            // Adjust bounds to prevent window from hovering over menu
            if (_menu != null)
            {
                if (menuTop > 0)
                    maxBounds.y = menuPosition.y - windowSize.y / 2;
            }

            // Restrict the new position to the screen bounds
            newPosition.x = Mathf.Clamp(newPosition.x, minBounds.x, maxBounds.x);
            newPosition.y = Mathf.Clamp(newPosition.y, minBounds.y, maxBounds.y);

            _rectTransform.anchoredPosition = newPosition;

            // Bring the window to the front
            _rectTransform.SetAsLastSibling();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
            _canvasGroup.blocksRaycasts = true; // Enable raycasting again
            _initialPosition = _rectTransform.anchoredPosition;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _rectTransform.SetAsLastSibling();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}