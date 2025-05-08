using System.Collections.Generic;
using UnityEngine;

namespace ParaMoon
{
    [Injectable]
    [SceneExported("InventoryUIController")]
    public class InventoryGridView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] protected InventoryData _inventoryData;
        [SerializeField] protected RectTransform _gridContainer;
        [SerializeField] protected GameObject _slotPrefab;
        [SerializeField] protected GameObject _itemPrefab;

        [Header("Settings")]
        [SerializeField] protected Color _normalSlotColor = Color.white;
        [SerializeField] protected Color _highlightSlotColor = new(0.8f, 0.8f, 1f);
        [SerializeField] protected Color _invalidSlotColor = new(1f, 0.6f, 0.6f);

        protected IInventory _inventory;
        protected Dictionary<Vector2Int, InventorySlotUI> _slots = new();
        protected Dictionary<IItem, InventoryItemView> _itemViews = new();

        protected InventoryItemView _draggedItem;
        protected Vector2Int _originalPosition;
        protected List<InventorySlotUI> _highlightedSlots = new();

        public InventoryData InventoryData => _inventoryData;
        public IInventory Inventory => _inventory;
        public RectTransform GridContainer => _gridContainer;

        private void OnDestroy()
        {
            if (_inventory != null)
            {
                UnsubscribeInventoryEvents();
            }
        }

        public virtual void Initialize(IInventory inventory)
        {
            if (_inventory != null)
            {
                // Unsubscribe from old inventory
                UnsubscribeInventoryEvents();
            }

            _inventory = inventory;

            if (_inventory != null)
            {
                // Subscribe to new inventory events
                SubscribeInventoryEvents();
                CreateGridUI();
            }
        }

        protected virtual void CreateGridUI()
        {
            Debug.Log($"Creating grid UI for inventory: {_inventory.GetType().Name}");

            ClearUI();
            ConfigureGridContainer();
            CreateSlots();

            // Add existing items
            foreach (var (item, position) in _inventory.GetAllItems())
            {
                CreateItemUI(item, position);
            }
        }

        protected virtual void CreateSlots()
        {
            // Create slot views for the inventory
            for (int y = 0; y < _inventory.GridSize.y; y++)
            {
                for (int x = 0; x < _inventory.GridSize.x; x++)
                {
                    Vector2Int position = new(x, y);
                    CreateSlot(position);
                }
            }
        }

        protected virtual void CreateSlot(Vector2Int gridPosition)
        {
            Debug.Log($"Creating slot at position: {gridPosition}");

            GameObject slotObj = Instantiate(_slotPrefab, _gridContainer);
            InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();

            // Position the slot
            RectTransform slotRect = slotObj.GetComponent<RectTransform>();
            slotRect.anchoredPosition = GridToLocalPosition(gridPosition);
            slotRect.sizeDelta = _inventoryData.CellSize;

            // Setup slot component
            if (slotUI != null)
            {
                slotUI.Initialize(gridPosition, this);
                _slots[gridPosition] = slotUI;
            }
        }

        protected virtual void ConfigureGridContainer()
        {
            // Set the grid container size based on inventory data
            float cellWidth = _inventoryData.CellSize.x;
            float cellHeight = _inventoryData.CellSize.y;
            float spacingX = _inventoryData.Spacing.x;
            float spacingY = _inventoryData.Spacing.y;

            float totalWidth = _inventory.GridSize.x * (cellWidth + spacingX) - spacingX;
            float totalHeight = _inventory.GridSize.y * (cellHeight + spacingY) - spacingY;

            _gridContainer.sizeDelta = new Vector2(totalWidth, totalHeight);
        }

        private void SubscribeInventoryEvents()
        {
            _inventory.OnItemAdded += HandleItemAdded;
            _inventory.OnItemRemoved += HandleItemRemoved;
            _inventory.OnItemMoved += HandleItemMoved;
        }

        private void UnsubscribeInventoryEvents()
        {
            _inventory.OnItemAdded -= HandleItemAdded;
            _inventory.OnItemRemoved -= HandleItemRemoved;
            _inventory.OnItemMoved -= HandleItemMoved;
        }

        public Vector2 GridToLocalPosition(Vector2Int gridPosition)
        {
            float x = gridPosition.x * (_inventoryData.CellSize.x + _inventoryData.Spacing.x);
            float y = -gridPosition.y * (_inventoryData.CellSize.y + _inventoryData.Spacing.y);
            return new Vector2(x, y);
        }

        public Vector2Int ScreenToGridPosition(Vector2 screenPosition)
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

        #region Event Handlers
        protected virtual void HandleItemAdded(IItem item, Vector2Int position)
        {
            CreateItemUI(item, position);
        }

        protected virtual void HandleItemRemoved(IItem item, Vector2Int position)
        {
            if (_itemViews.TryGetValue(item, out InventoryItemView itemUI))
            {
                Destroy(itemUI.gameObject);
                _itemViews.Remove(item);
            }
        }

        protected virtual void HandleItemMoved(IItem item, Vector2Int fromPosition, Vector2Int toPosition)
        {
            if (_itemViews.TryGetValue(item, out InventoryItemView itemView))
            {
                itemView.SetGridPosition(toPosition);
                itemView.GetComponent<RectTransform>().anchoredPosition = GridToLocalPosition(toPosition);
            }
        }
        #endregion

        public virtual void CreateItemUI(IItem item, Vector2Int position)
        {
            GameObject itemObj = Instantiate(_itemPrefab, _gridContainer);
            InventoryItemView itemView = itemObj.GetComponent<InventoryItemView>();

            // Position and size the item
            RectTransform itemRect = itemObj.GetComponent<RectTransform>();
            itemRect.anchoredPosition = GridToLocalPosition(position);

            // Calculate size based on item dimensions
            float itemWidth = item.Size.x * _inventoryData.CellSize.x +
                             (item.Size.x - 1) * _inventoryData.Spacing.x;
            float itemHeight = item.Size.y * _inventoryData.CellSize.y +
                              (item.Size.y - 1) * _inventoryData.Spacing.y;

            itemRect.sizeDelta = new Vector2(itemWidth, itemHeight);

            // Initialize the item view
            itemView.Initialize(item, position, this);
            _itemViews[item] = itemView;
        }

        protected virtual Vector2Int AdjustPositionForMultiCellItem(Vector2Int targetPosition)
        {
            // Ensure multi-cell items don't go out of bounds
            if (_draggedItem == null) 
                return targetPosition;

            Vector2Int size = _draggedItem.Item.Size;

            int maxX = _inventory.GridSize.x - size.x;
            int maxY = _inventory.GridSize.y - size.y;

            return new Vector2Int(
                Mathf.Clamp(targetPosition.x, 0, maxX),
                Mathf.Clamp(targetPosition.y, 0, maxY)
            );
        }

        // Handle item drag operations
        public void BeginItemDrag(InventoryItemView itemUI)
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

            // Check if cursor is over this inventory
            if (RectTransformUtility.RectangleContainsScreenPoint(_gridContainer, screenPosition, null))
            {
                // Highlight the potential drop area
                bool isValid = _inventory.IsPositionFreeExcept(
                    targetPosition, _draggedItem.Item.Size, _draggedItem.Item);

                HighlightDropArea(targetPosition, _draggedItem.Item.Size, isValid);
            }
        }

        public void EndItemDrag(Vector2 screenPosition)
        {
            if (_draggedItem == null) return;

            // Check if dropped on this inventory
            if (RectTransformUtility.RectangleContainsScreenPoint(_gridContainer, screenPosition, null))
            {
                // Calculate target position
                Vector2Int targetPosition = ScreenToGridPosition(screenPosition);
                targetPosition = AdjustPositionForMultiCellItem(targetPosition);

                // Try to move item within this inventory
                bool success = _inventory.TryMoveItem(_originalPosition, targetPosition);

                if (!success)
                {
                    // If move failed, reset position
                    _draggedItem.GetComponent<RectTransform>().anchoredPosition =
                        GridToLocalPosition(_originalPosition);
                }
            }
            else
            {
                // Dropped outside - reset position
                _draggedItem.GetComponent<RectTransform>().anchoredPosition =
                    GridToLocalPosition(_originalPosition);
            }

            // Reset state
            _draggedItem = null;
            ClearHighlights();
        }

        protected virtual void ClearHighlights()
        {
            foreach (var slot in _highlightedSlots)
            {
                slot.SetHighlight(false);
            }
            _highlightedSlots.Clear();
        }

        protected virtual void HighlightDropArea(Vector2Int position, Vector2Int size, bool isValid)
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

        protected virtual void ClearUI()
        {
            foreach (var itemUI in _itemViews.Values)
            {
                Destroy(itemUI.gameObject);
            }

            _itemViews.Clear();

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
    }
}
