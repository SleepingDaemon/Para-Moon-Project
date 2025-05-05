using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ParaMoon
{
    public class InventorySlotUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] Image _background;

        protected InventoryGridUI _parentInventory;

        Vector2Int _gridPosition;
        Color _normalColor = Color.white;

        public Vector2Int GridPosition => _gridPosition;

        public virtual void Initialize(Vector2Int position, InventoryGridUI inventory)
        {
            _gridPosition = position;
            _parentInventory = inventory;

            if (_background != null)
                _normalColor = _background.color;

            name = $"Slot [{position.x}, {position.y}]";
        }

        public void SetHighlight(bool highlight, Color? color = null)
        {
            if (_background != null)
                _background.color = highlight ? (color ?? Color.yellow) : _normalColor;
        }

        public virtual void OnDrop(PointerEventData eventData)
        {
            // Get the dragged item
            InventoryItemUI draggedItemUI = eventData.pointerDrag?.GetComponent<InventoryItemUI>();
            if (draggedItemUI == null)
                return;

            // If from a different inventory, handle as a transfer
            InventoryGridUI sourceInventory = draggedItemUI.ParentInventory;
            if (sourceInventory == null)
            {
                Debug.LogError($"Source inventory is null for item {draggedItemUI.gameObject.name}");
                return;
            }

            if (_parentInventory == null)
            {
                Debug.LogError($"Parent inventory is null for slot {gameObject.name}");
                return;
            }

            if (sourceInventory != _parentInventory)
            {
                // Get the item's data
                IItem itemToTransfer = draggedItemUI.Item;
                Vector2Int sourcePosition = draggedItemUI.GridPosition;

                // Check if the source inventory is a specialized inventory
                bool sourceIsSpecialized = sourceInventory.UseSpecializedSlots;
                bool targetIsSpecialized = _parentInventory.UseSpecializedSlots;

                // If moving from specialized to regular
                if (sourceIsSpecialized && !targetIsSpecialized)
                {
                    // Use specialized transfer method that handles size restoration
                    bool success = InventoryHelper.TransferFromSpecializedSlot(
                        sourceInventory.Inventory,
                        _parentInventory.Inventory,
                        itemToTransfer,
                        sourcePosition);

                    // If transfer was unsuccessful, return to original position
                    if (!success)
                    {
                        sourceInventory.CancelItemDrag();
                    }
                    return;
                }

                // Regular transfer for other cases
                bool regularSuccess = InventoryHelper.TransferItem(
                    sourceInventory.Inventory,
                    _parentInventory.Inventory,
                    itemToTransfer);

                // If transfer was unsuccessful, return to original position
                if (!regularSuccess)
                {
                    sourceInventory.CancelItemDrag();
                }
            }
            // If from the same inventory, the inventory's EndItemDrag will handle it
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetHighlight(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHighlight(false);
        }
    }
}
