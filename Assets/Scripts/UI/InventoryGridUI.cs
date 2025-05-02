using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParaMoon
{
    public class InventoryGridUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] RectTransform _gridContainer;
        [SerializeField] GameObject _slotPrefab;
        [SerializeField] GameObject _itemPrefab;

        [Header("Settings")]
        [SerializeField] InventoryData _inventoryData;
        [SerializeField] private Color normalSlotColor = Color.white;
        [SerializeField] private Color highlightSlotColor = new(0.8f, 0.8f, 1f);
        [SerializeField] private Color invalidSlotColor = new(1f, 0.6f, 0.6f);

        IInventory _inventory;
        Dictionary<Vector2Int, InventorySlotUI> _slots = new();
        Dictionary<IItem, InventoryItemUI> _itemUIs = new();

        InventoryItemUI _draggedItem;
        Vector2Int _originalPosition;
        List<InventorySlotUI> _highlightedSlots = new();

        private void Awake()
        {
            if (_inventoryData == null)
            {
                Debug.LogError("InventoryData is not assigned!");
                return;
            }

            // Create inventory grid
            _inventory = new InventoryGrid(_inventoryData);

            // Subscribe to inventory events
            _inventory.OnItemAdded += HandleItemAdded;
            _inventory.OnItemRemoved += HandleItemRemoved;
            _inventory.OnItemMoved += HandleItemMoved;

            InitializeGridUI();
        }

        private void OnDestroy()
        {
            if (_inventory != null)
            {
                _inventory.OnItemAdded -= HandleItemAdded;
                _inventory.OnItemRemoved -= HandleItemRemoved;
                _inventory.OnItemMoved -= HandleItemMoved;
            }
        }

        private void InitializeGridUI()
        {
            // Clear existing grid elements
            foreach (Transform child in _gridContainer)
            {
                Destroy(child.gameObject);
            }

            _slots.Clear();

            // Calculate grid dimensions
            float cellWidth = _inventoryData.CellSize.x;
            float cellHeight = _inventoryData.CellSize.y;
            float spacingX = _inventoryData.Spacing.x;
            float spacingY = _inventoryData.Spacing.y;

            float totalWidth = _inventoryData.Size.x * (cellWidth + spacingX) - spacingX;
            float totalHeight = _inventoryData.Size.y * (cellHeight + spacingY) - spacingY;

            // Set grid container size
            _gridContainer.sizeDelta = new Vector2(totalWidth, totalHeight);

            // Create slots
            for (int y = 0; y < _inventoryData.Size.y; y++)
            {
                for (int x = 0; x < _inventoryData.Size.x; x++)
                {
                    Vector2Int gridPosition = new(x, y);
                    CreateSlot(gridPosition);
                }
            }
        }

        private void CreateSlot(Vector2Int gridPosition)
        {
            GameObject slotObj = Instantiate(_slotPrefab, _gridContainer);
            RectTransform slotRect = slotObj.GetComponent<RectTransform>();

            // Position the slot
            Vector2 position = GridToLocalPosition(gridPosition);
            slotRect.anchoredPosition = position;
            slotRect.sizeDelta = _inventoryData.CellSize;

            // Setup slot component
            InventorySlotUI slot = slotObj.GetComponent<InventorySlotUI>();
            if (slot != null)
            {
                slot.Initialize(gridPosition, this);
                _slots[gridPosition] = slot;
            }
        }

        private Vector2 GridToLocalPosition(Vector2Int gridPosition)
        {
            float x = gridPosition.x * (_inventoryData.CellSize.x + _inventoryData.Spacing.x);
            float y = -gridPosition.y * (_inventoryData.CellSize.y + _inventoryData.Spacing.y);
            return new Vector2(x, y);
        }

        private Vector2Int ScreenToGridPosition(Vector2 screenPosition)
        {
            // Convert screen position to local position in the grid container
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _gridContainer, screenPosition, null, out Vector2 localPosition);

            float cellWidth = _inventoryData.CellSize.x + _inventoryData.Spacing.x;
            float cellHeight = _inventoryData.CellSize.y + _inventoryData.Spacing.y;

            int x = Mathf.FloorToInt(localPosition.x / cellWidth);
            int y = Mathf.FloorToInt(-localPosition.y / cellHeight); // Negate Y due to UI coordinate system

            return new Vector2Int(x, y);
        }

        public bool AddItem(IItem item)
        {
            return _inventory.TryAddItem(item, out _);
        }

        public bool AddItemAt(IItem item, Vector2Int position)
        {
            return _inventory.TryAddItem(item, position);
        }

        // Event handlers for inventory changes
        private void HandleItemAdded(IItem item, Vector2Int position)
        {
            CreateItemUI(item, position);
        }

        private void HandleItemRemoved(IItem item, Vector2Int position)
        {
            if (_itemUIs.TryGetValue(item, out InventoryItemUI itemUI))
            {
                Destroy(itemUI.gameObject);
                _itemUIs.Remove(item);
            }
        }

        private void HandleItemMoved(IItem item, Vector2Int fromPosition, Vector2Int toPosition)
        {
            if (_itemUIs.TryGetValue(item, out InventoryItemUI itemUI))
            {
                // Update the item UI's position
                itemUI.SetGridPosition(toPosition);
                itemUI.GetComponent<RectTransform>().anchoredPosition = GridToLocalPosition(toPosition);
            }
        }

        private void CreateItemUI(IItem item, Vector2Int position)
        {
            GameObject itemObject = Instantiate(_itemPrefab, _gridContainer);
            RectTransform rectTransform = itemObject.GetComponent<RectTransform>();

            // Position the item
            rectTransform.anchoredPosition = GridToLocalPosition(position);

            // Size the item
            float itemWidth = item.Size.x * _inventoryData.CellSize.x +
                (item.Size.x - 1) * _inventoryData.Spacing.x;
            float itemHeight = item.Size.y * _inventoryData.CellSize.y +
                (item.Size.y - 1) * _inventoryData.Spacing.y;

            rectTransform.sizeDelta = new Vector2(itemWidth, itemHeight);

            // Set up item UI component
            InventoryItemUI itemUI = itemObject.GetComponent<InventoryItemUI>();
            if (itemUI != null)
            {
                itemUI.Initialize(item, position, this);
                _itemUIs[item] = itemUI;
            }
        }

        // Handle item drag operations
        public void BeginItemDrag(InventoryItemUI itemUI)
        {
            _draggedItem = itemUI;
            _originalPosition = itemUI.GridPosition;
        }

        public void DragItem(Vector2 screenPosition)
        {
            if (_draggedItem == null)
                return;

            // Move the item to follow the cursor
            _draggedItem.transform.position = screenPosition;

            // Convert screen position to grid position
            Vector2Int targetPosition = ScreenToGridPosition(screenPosition);

            // Adjust target position for multi-cell items
            targetPosition = AdjustPositionForMultiCellItem(targetPosition);

            // Check if the position is valid for the dragged item
            ClearHighlights();

            // Highlight potential drop area
            HighlightDropArea(targetPosition, _draggedItem.Item.Size,
                _inventory.IsPositionFreeExcept(targetPosition, _draggedItem.Item.Size, _draggedItem.Item));
        }

        private Vector2Int AdjustPositionForMultiCellItem(Vector2Int targetPosition)
        {
            // Adjust the target position to ensure it fits within the grid
            if (_draggedItem.Item.Size.x > 1)
            {
                targetPosition.x = Mathf.Clamp(targetPosition.x, 0, _inventoryData.Size.x - _draggedItem.Item.Size.x);
            }
            if (_draggedItem.Item.Size.y > 1)
            {
                targetPosition.y = Mathf.Clamp(targetPosition.y, 0, _inventoryData.Size.y - _draggedItem.Item.Size.y);
            }
            return targetPosition;
        }

        public void EndItemDrag(Vector2 screenPosition)
        {
            if (_draggedItem == null)
                return;

            // Convert screen position to grid position
            Vector2Int targetPosition = ScreenToGridPosition(screenPosition);

            // Try to move the item
            bool success = _inventory.TryMoveItem(_originalPosition, targetPosition);

            if (!success)
            {
                // Return item to original position
                _draggedItem.GetComponent<RectTransform>().anchoredPosition =
                    GridToLocalPosition(_originalPosition);
            }

            _draggedItem = null;
            ClearHighlights();
        }

        private void ClearHighlights()
        {
            foreach (var slot in _highlightedSlots)
            {
                slot.SetHighlight(false);
            }
            _highlightedSlots.Clear();
        }

        private void HighlightDropArea(Vector2Int position, Vector2Int size, bool isValid)
        {
            Color highlightColor = isValid ? this.highlightSlotColor : invalidSlotColor;

            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    Vector2Int slotPosition = position + new Vector2Int(x, y);

                    if (_slots.TryGetValue(slotPosition, out InventorySlotUI slot))
                    {
                        slot.SetHighlight(true, highlightColor);
                        _highlightedSlots.Add(slot);
                    }
                }
            }
        }

        // IGridInventory interface exposure
        public IInventory Inventory => _inventory;

        // Public methods for external interaction
        public bool TryGetItemAt(Vector2Int position, out IItem item)
        {
            item = _inventory.GetItemAt(position);
            return item != null;
        }

        public bool TryRemoveItemAt(Vector2Int position, out IItem item)
        {
            return _inventory.TryRemoveItem(position, out item);
        }
    }
}
