using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ParaMoon
{
    public class InventorySlotUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] Image _background;

        protected InventoryGridView _parentView;
        protected Vector2Int _gridPosition;
        protected Color _normalColor = Color.white;

        public Vector2Int GridPosition => _gridPosition;
        public InventoryGridView ParentView => _parentView;

        public virtual void Initialize(Vector2Int position, InventoryGridView parentView)
        {
            _gridPosition = position;
            _parentView = parentView;

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
            InventoryItemView draggedItemUI = eventData.pointerDrag?.GetComponent<InventoryItemView>();
            if (draggedItemUI == null)
                return;

            // If from a different inventory, handle as a transfer
            InventoryGridView sourceGridView = draggedItemUI.ParentView;
            if (sourceGridView == null)
            {
                Debug.LogError($"Source inventory is null for item {draggedItemUI.gameObject.name}");
                return;
            }

            if (_parentView == null)
            {
                Debug.LogError($"Parent inventory is null for slot {gameObject.name}");
                return;
            }

            if (sourceGridView != _parentView)
            {
                InventoryService inventoryService = InventoryService.Instance;

                // Get the item's data
                IItem itemToTransfer = draggedItemUI.Item;
                Vector2Int sourcePosition = draggedItemUI.GridPosition;

                // Handle specialized inventory transfers
                // Handle specialized transfers
                if (sourceGridView is SpecializedInventoryView)
                {
                    bool success = inventoryService.TransferFromSpecializedSlot(
                        sourceGridView.Inventory,
                        _parentView.Inventory,
                        itemToTransfer,
                        sourcePosition);

                    if (!success)
                    {
                        // Reset position if transfer failed
                        draggedItemUI.ResetPosition();
                    }
                }
                else
                {
                    // Regular transfer
                    bool success = inventoryService.TransferItem(
                        sourceGridView.Inventory,
                        _parentView.Inventory,
                        itemToTransfer);

                    if (!success)
                    {
                        // Reset position if transfer failed
                        draggedItemUI.ResetPosition();
                    }
                }
            }
            // If from same inventory, the inventory's EndItemDrag will handle movement
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
