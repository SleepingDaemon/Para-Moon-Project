using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
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
        [SerializeField] private Color _normalSlotColor = Color.white;
        [SerializeField] private Color _highlightSlotColor = new(0.8f, 0.8f, 1f);
        [SerializeField] private Color _invalidSlotColor = new(1f, 0.6f, 0.6f);
        [SerializeField] bool _useSpecializedSlots = false;

        IInventory _inventory;
        Dictionary<Vector2Int, InventorySlotUI> _slots = new();
        Dictionary<IItem, InventoryItemUI> _itemUIs = new();

        InventoryItemUI _draggedItem;
        Vector2Int _originalPosition;
        List<InventorySlotUI> _highlightedSlots = new();

        public InventoryData InventoryData => _inventoryData;
        public IInventory Inventory => _inventory;
        public bool UseSpecializedSlots
        {
            get => _useSpecializedSlots;
            set
            {
                _useSpecializedSlots = value;
            }
        }
        public RectTransform GridContainer => _gridContainer;

        private void Awake()
        {
            if (_inventoryData == null)
            {
                Debug.LogError("InventoryData is not assigned!");
                return;
            }

            // Create inventory
            _inventory = new InventorySystem(_inventoryData);

            // Subscribe to inventory events
            _inventory.OnItemAdded += HandleItemAdded;
            _inventory.OnItemRemoved += HandleItemRemoved;
            _inventory.OnItemMoved += HandleItemMoved;

            // Only initialize grid UI if not using specialized slots
            if (!_useSpecializedSlots)
                InitializeGridUI();
            else
                InitializeGridContainer();
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

        public void InitializeGridContainer()
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

        public void RefreshGrid()
        {
            // Clear all highlights
            ClearHighlights();

            // Reset any dragged item state
            _draggedItem = null;

            // Get all items and their positions from the inventory
            var inventoryItems = _inventory.GetAllItems().ToList();

            // Track items that need to be created or updated
            HashSet<IItem> processedItems = new();

            // Update existing item UIs
            foreach (var (existingItem, itemUI) in _itemUIs)
            {
                bool found = false;

                // Check if the item is still in the inventory
                foreach (var (invItem, position) in inventoryItems)
                {
                    if (invItem == existingItem)
                    {
                        // Mark item as processed
                        processedItems.Add(existingItem);

                        // ALWAYS update position for items in specialized slots, even if the grid position hasn't changed
                        // This ensures that after a failed drop, items are properly repositioned
                        InventorySlotUI slot = null;
                        bool isSpecializedSlot = _useSpecializedSlots && _slots.TryGetValue(position, out slot) &&
                                                 slot is SpecializedSlotUI;

                        // Update if position changed OR if this is a specialized slot (to ensure correct positioning)
                        if (itemUI.GridPosition != position || isSpecializedSlot)
                        {
                            itemUI.SetGridPosition(position);

                            if (isSpecializedSlot)
                            {
                                RectTransform itemRect = itemUI.GetComponent<RectTransform>();
                                RectTransform slotRect = slot.GetComponent<RectTransform>();

                                // Always ensure specialized items are direct children of their slots
                                itemRect.SetParent(slotRect, false);

                                // Reset position - force to zero to fix positioning issues
                                itemRect.anchoredPosition = Vector2.zero;

                                if (existingItem is Item actualItem)
                                    actualItem.ForceSize(new Vector2Int(1, 1));
                            }
                            else
                            {
                                // Always update regular grid positioning too
                                itemUI.transform.SetParent(_gridContainer);
                                itemUI.GetComponent<RectTransform>().anchoredPosition = GridToLocalPosition(position);

                                if (existingItem is Item actualItem)
                                    actualItem.ResetSize();
                            }
                        }

                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    Destroy(itemUI.gameObject);
                    _itemUIs.Remove(existingItem);
                }
            }

            // Create UIs for any items in inventory that don't have UIs yet
            foreach (var (newItem, position) in inventoryItems)
            {
                if (!processedItems.Contains(newItem))
                {
                    CreateItemUI(newItem, position);

                    if (_useSpecializedSlots && _slots.TryGetValue(position, out var slot))
                    {
                        if (_itemUIs.TryGetValue(newItem, out var newItemUI))
                        {
                            RectTransform itemRect = newItemUI.GetComponent<RectTransform>();
                            RectTransform slotRect = slot.GetComponent<RectTransform>();

                            itemRect.SetParent(slotRect, false);
                            itemRect.anchoredPosition = Vector2.zero;

                            if (newItem is Item actualItem)
                            {
                                actualItem.ForceSize(new Vector2Int(1, 1));
                            }
                        }
                    }
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

        public Vector2 GridToLocalPosition(Vector2Int gridPosition)
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
        public void HandleItemAdded(IItem item, Vector2Int position)
        {
            CreateItemUI(item, position);
        }

        public void HandleItemRemoved(IItem item, Vector2Int position)
        {
            if (_itemUIs.TryGetValue(item, out InventoryItemUI itemUI))
            {
                Destroy(itemUI.gameObject);
                _itemUIs.Remove(item);
            }
        }

        public void HandleItemMoved(IItem item, Vector2Int fromPosition, Vector2Int toPosition)
        {
            if (_itemUIs.TryGetValue(item, out InventoryItemUI itemUI))
            {
                // Update the item UI's position
                UpdateItemUI(item, toPosition);
            }
        }

        private void CreateItemUI(IItem item, Vector2Int position)
        {
            // Check if this is a specialized slot
            InventorySlotUI slotUI = null;
            bool isSpecializedSlot = _slots.TryGetValue(position, out slotUI) && slotUI is SpecializedSlotUI;

            // Create parent for the item
            Transform parent = isSpecializedSlot ? slotUI.transform : _gridContainer;

            GameObject itemObject = Instantiate(_itemPrefab, parent);
            RectTransform rectTransform = itemObject.GetComponent<RectTransform>();

            if (isSpecializedSlot)
            {
                // For specialized slots, position directly within the slot
                rectTransform.anchoredPosition = Vector2.zero;

                // Force size to 1x1 for specialized slots
                if (item is Item actualItem)
                {
                    actualItem.ForceSize(new Vector2Int(1, 1));
                }

                float itemWidth = _inventoryData.CellSize.x;
                float itemHeight = _inventoryData.CellSize.y;
                rectTransform.sizeDelta = new Vector2(itemWidth, itemHeight);
            }
            else
            {
                // Normal inventory positioning
                rectTransform.anchoredPosition = GridToLocalPosition(position);

                // Size the item
                float itemWidth = item.Size.x * _inventoryData.CellSize.x +
                    (item.Size.x - 1) * _inventoryData.Spacing.x;
                float itemHeight = item.Size.y * _inventoryData.CellSize.y +
                    (item.Size.y - 1) * _inventoryData.Spacing.y;

                rectTransform.sizeDelta = new Vector2(itemWidth, itemHeight);
            }

            // Set up item UI component
            if (itemObject.TryGetComponent<InventoryItemUI>(out var itemUI))
            {
                itemUI.Initialize(item, position, this);
                _itemUIs[item] = itemUI;
            }
        }

        public void UpdateItemUI(IItem item, Vector2Int position)
        {
            if (_itemUIs.TryGetValue(item, out InventoryItemUI itemUI))
            {
                InventorySlotUI slotUI = null;
                bool isSpecializedSlot = _useSpecializedSlots && _slots.TryGetValue(position, out slotUI) && 
                                         slotUI is SpecializedSlotUI;

                // Update position
                itemUI.SetGridPosition(position);

                // Update parenting and positioning
                if (isSpecializedSlot)
                {
                    // Reparent to specialized slot
                    itemUI.transform.SetParent(slotUI.transform);
                    itemUI.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                    // Force size for specialized slots
                    if (item is Item actualItem)
                    {
                        actualItem.ForceSize(new Vector2Int(1, 1));
                    }
                }
                else
                {
                    // Regular grid positioning
                    itemUI.transform.SetParent(_gridContainer);
                    itemUI.GetComponent<RectTransform>().anchoredPosition = GridToLocalPosition(position);

                    // Reset size
                    if (item is Item actualItem)
                    {
                        actualItem.ResetSize();
                    }
                }
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

            // If dragged item is over a different inventory, clear highlights
            if (!RectTransformUtility.RectangleContainsScreenPoint(_gridContainer, screenPosition, null))
            {
                ClearHighlights();
            }
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

            // Check if the item was dropped on a valid target
            // If it's dropped somewhere else (another inventory's slot), let the handler deal with it
            bool droppedOnValidTarget = false;

            // Only try to handle internal movement if the pointer is over the same inventory
            if (RectTransformUtility.RectangleContainsScreenPoint(_gridContainer, screenPosition, null))
            {
                // Convert screen position to grid position
                Vector2Int targetPosition = ScreenToGridPosition(screenPosition);

                // Try to move the item within our own inventory
                droppedOnValidTarget = _inventory.TryMoveItem(_originalPosition, targetPosition);

                // If the move failed and we're using specialized slots, we need to ensure the item
                // returns to its original specialized slot correctly
                if (!droppedOnValidTarget && _useSpecializedSlots)
                {
                    // Check if the original position was a specialized slot
                    bool wasInSpecializedSlot = _slots.TryGetValue(_originalPosition, out InventorySlotUI originalSlot) &&
                                                 originalSlot is SpecializedSlotUI;

                    if (wasInSpecializedSlot)
                    {
                        // Reset the item directly to the original specialized slot
                        _draggedItem.transform.SetParent(originalSlot.transform);
                        _draggedItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                    }
                    else
                    {
                        // Regular grid position reset
                        _draggedItem.GetComponent<RectTransform>().anchoredPosition =
                            GridToLocalPosition(_originalPosition);
                    }
                }
                else if (!droppedOnValidTarget)
                {
                    // Standard reset for non-specialized slots
                    _draggedItem.GetComponent<RectTransform>().anchoredPosition =
                        GridToLocalPosition(_originalPosition);
                }
            }
            else
            {
                // If dropped outside this inventory, reset to original position
                // Check if it was in a specialized slot
                InventorySlotUI originalSlot = null;
                bool wasInSpecializedSlot = _useSpecializedSlots &&
                                           _slots.TryGetValue(_originalPosition, out originalSlot) &&
                                           originalSlot is SpecializedSlotUI;

                if (wasInSpecializedSlot)
                {
                    // Reset to the specialized slot
                    _draggedItem.transform.SetParent(originalSlot.transform);
                    _draggedItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                }
                else
                {
                    // Standard position reset
                    _draggedItem.GetComponent<RectTransform>().anchoredPosition =
                        GridToLocalPosition(_originalPosition);
                }
            }

            // Reset the dragged item reference if dropped on a valid target
            if (droppedOnValidTarget)
            {
                _draggedItem = null;
            }

            RefreshGrid();
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
            Color highlightColor = isValid ? this._highlightSlotColor : _invalidSlotColor;

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

        public void SetInventory(IInventory inventory)
        {
            // Unsubscribe from old inventory events
            if (_inventory != null)
            {
                _inventory.OnItemAdded -= HandleItemAdded;
                _inventory.OnItemRemoved -= HandleItemRemoved;
                _inventory.OnItemMoved -= HandleItemMoved;
            }

            ClearUI();
            _inventory = inventory;

            // Subscribe to new inventory events
            if (_inventory != null)
            {
                _inventory.OnItemAdded += HandleItemAdded;
                _inventory.OnItemRemoved += HandleItemRemoved;
                _inventory.OnItemMoved += HandleItemMoved;
            }

            // Only initialize grid UI if not using specialized slots
            if (!_useSpecializedSlots)
            {
                InitializeGridUI();

                // Add existing items to the UI
                if (_inventory != null)
                {
                    foreach (var (item, position) in _inventory.GetAllItems())
                    {
                        CreateItemUI(item, position);
                    }
                }
            }
            else
            {
                InitializeGridContainer();
            }
        }

        private void ClearUI()
        {
            foreach (var itemUI in _itemUIs.Values)
            {
                Destroy(itemUI.gameObject);
            }

            _itemUIs.Clear();

            foreach (Transform child in _gridContainer)
            {
                Destroy(child.gameObject);
            }

            _slots.Clear();
        }

        public void CancelItemDrag()
        {
            if (_draggedItem != null)
            {
                // Return item to original position
                _draggedItem.GetComponent<RectTransform>().anchoredPosition =
                    GridToLocalPosition(_originalPosition);

                // Reset state
                _draggedItem = null;
                ClearHighlights();
            }
        }

        public void RegisterSpecializedSlot(InventorySlotUI slot, Vector2Int position)
        {
            _slots[position] = slot;
        }
    }
}
