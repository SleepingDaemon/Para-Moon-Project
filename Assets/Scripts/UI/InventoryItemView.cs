using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ParaMoon
{
    public class InventoryItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [SerializeField] Image _itemIcon;
        [SerializeField] TMP_Text _itemName;
        [SerializeField] TMP_Text _stackCountText;

        IItem _item;
        Vector2Int _gridPosition;
        InventoryGridView _parentView;
        RectTransform _rectTransform;
        CanvasGroup _canvasGroup;
        Vector3 _startPosition;

        public IItem Item => _item;
        public Vector2Int GridPosition => _gridPosition;
        public InventoryGridView ParentView => _parentView;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();

            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void Initialize(IItem inventoryItem, Vector2Int position, InventoryGridView inventory)
        {
            _item = inventoryItem;
            _gridPosition = position;
            _parentView = inventory;

            // Set up visual elements
            UpdatedVisuals();
            UpdateStackCount();

            // Ensure the root GameObject can receive raycasts
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = true;
            }
        }

        private void UpdatedVisuals()
        {
            if (_itemIcon != null)
            {
                _itemIcon.sprite = _item.Icon;
                _itemIcon.preserveAspect = true;
                _itemIcon.raycastTarget = true;
                _itemIcon.enabled = true;
                _itemIcon.color = new Color(1f, 1f, 1f, 1f);

                // Make icon fill the item container
                RectTransform iconRect = _itemIcon.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;
            }

            if (_itemName != null)
                _itemName.text = _item.Name;
        }

        public void UpdateStackCount()
        {
            if (_stackCountText != null)
            {
                _stackCountText.gameObject.SetActive(_item.IsStackable && _item.StackCount > 1);
                _stackCountText.text = _item.StackCount.ToString();
            }
        }

        public void SetGridPosition(Vector2Int toPosition)
        {
            _gridPosition = toPosition;
        }

        public void ResetPosition()
        {
            if (_parentView != null)
                _rectTransform.anchoredPosition = _parentView.GridToLocalPosition(_gridPosition);
        }

        // Drag and drop functionality
        public void OnBeginDrag(PointerEventData eventData)
        {
            _startPosition = transform.position;

            // Make the item semi-transparent and pass through raycasts
            _canvasGroup.alpha = 0.6f;
            _canvasGroup.blocksRaycasts = false;

            // Bring to front
            transform.SetAsLastSibling();

            // Notify the inventory UI
            _parentView.BeginItemDrag(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Move the item with the cursor
            transform.position = eventData.position;

            // Notify the inventory UI for highlighting
            _parentView.DragItem(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // Restore opacity and raycast blocking
            _canvasGroup.alpha = 1.0f;
            _canvasGroup.blocksRaycasts = true;

            // Notify the inventory UI
            _parentView.EndItemDrag(eventData.position);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                // TODO: Right-click functionality (e.g., show context menu, use item, etc.)
                Debug.Log($"Right-clicked on item: {_item.Name}");
            }
        }

    }
}
