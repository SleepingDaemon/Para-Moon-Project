using System.Collections.Generic;
using UnityEngine;

namespace ParaMoon
{
    /// <summary>
    /// Specialized inventory view for equipment slots
    /// </summary>
    public class SpecializedInventoryView : InventoryGridView
    {
        [SerializeField] private GameObject _specializedSlotPrefab;
        [SerializeField] private List<Sprite> _slotIcons = new();

        [SerializeField] private ItemType _acceptedItemType = ItemType.Armor;
        [SerializeField] private bool _isVerticalLayout = true;

        public override void Initialize(IInventory inventory)
        {
            base.Initialize(inventory);

            // Recreate slots as specialized slots
            if (_inventory != null)
            {
                CreateSpecializedSlots();
            }
        }

        protected override void CreateGridUI()
        {
            ClearUI();
            ConfigureGridContainer(); // Only set up container size, specialized slots will be created separately
        }

        private void CreateSpecializedSlots()
        {
            // Clear existing slots
            foreach (var slot in _slots.Values)
            {
                Destroy(slot.gameObject);
            }
            _slots.Clear();

            // Armor slots
            if (_acceptedItemType == ItemType.Armor)
            {
                int index = 0;
                foreach (ArmorSlot slotType in System.Enum.GetValues(typeof(ArmorSlot)))
                {
                    // Get position based on layout
                    Vector2Int position = _isVerticalLayout
                        ? new Vector2Int(0, index)
                        : new Vector2Int(index, 0);

                    // Get icon if available
                    Sprite icon = index < _slotIcons.Count ? _slotIcons[index] : null;

                    // Create specialized slot
                    CreateSpecializedSlot(position, slotType.ToString(), icon, slotType);

                    index++;
                }
            }
            // Implant slots
            else if (_acceptedItemType == ItemType.Implant)
            {
                int index = 0;
                foreach (ImplantSlot slotType in System.Enum.GetValues(typeof(ImplantSlot)))
                {
                    // Get position based on layout
                    Vector2Int position = _isVerticalLayout
                        ? new Vector2Int(0, index)
                        : new Vector2Int(index, 0);

                    // Get icon if available
                    Sprite icon = index < _slotIcons.Count ? _slotIcons[index] : null;

                    // Create specialized slot
                    CreateSpecializedSlot(position, slotType.ToString(), icon, slotType);

                    index++;
                }
            }
        }

        private void CreateSpecializedSlot(Vector2Int position, string label, Sprite icon, object slotType)
        {
            GameObject slotObj = Instantiate(_specializedSlotPrefab, _gridContainer);
            SpecializedSlotUI slotView = slotObj.GetComponent<SpecializedSlotUI>();

            // Position the slot
            RectTransform slotRect = slotObj.GetComponent<RectTransform>();
            slotRect.anchoredPosition = GridToLocalPosition(position);
            slotRect.sizeDelta = _inventoryData.CellSize;

            // Initialize the specialized slot
            slotView.Initialize(position, this, _acceptedItemType, slotType, icon, label);
            _slots[position] = slotView;
        }

        public override void CreateItemUI(IItem item, Vector2Int position)
        {
            GameObject itemObj = Instantiate(_itemPrefab, _gridContainer);
            InventoryItemView itemView = itemObj.GetComponent<InventoryItemView>();

            // For specialized slots, we parent directly to the slot
            if (_slots.TryGetValue(position, out var slotView))
            {
                RectTransform itemRect = itemObj.GetComponent<RectTransform>();

                // Parent to slot instead of grid
                itemRect.SetParent(slotView.transform);

                // Reset position within slot
                itemRect.anchoredPosition = Vector2.zero;

                // Force item size to 1x1 for specialized slots
                if (item is Item actualItem)
                {
                    actualItem.ForceSize(new Vector2Int(1, 1));
                }

                // Standard size
                itemRect.sizeDelta = _inventoryData.CellSize;
            }
            else
            {
                // Standard positioning if not a specialized slot
                RectTransform itemRect = itemObj.GetComponent<RectTransform>();
                itemRect.anchoredPosition = GridToLocalPosition(position);

                // Normal sizing
                float itemWidth = item.Size.x * _inventoryData.CellSize.x +
                                (item.Size.x - 1) * _inventoryData.Spacing.x;
                float itemHeight = item.Size.y * _inventoryData.CellSize.y +
                                (item.Size.y - 1) * _inventoryData.Spacing.y;

                itemRect.sizeDelta = new Vector2(itemWidth, itemHeight);
            }

            // Initialize the item view
            itemView.Initialize(item, position, this);
            _itemViews[item] = itemView;
        }
    }
}
