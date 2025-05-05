using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ParaMoon
{
    public class SpecializedSlotUI : InventorySlotUI
    {
        [SerializeField] private Image _slotIcon;
        [SerializeField] private TMP_Text _slotLabel;

        private ItemType _acceptedItemType;
        private object _slotType; // ArmorSlot, ImplantSlot types, etc.

        public void Initialize(Vector2Int position, InventoryGridUI inventory,
                              ItemType acceptedType, object slotType, Sprite icon = null, string label = null)
        {
            base.Initialize(position, inventory);

            _acceptedItemType = acceptedType;
            _slotType = slotType;

            // Position the slot correctly using the inventory's grid positioning
            RectTransform rectTransform = GetComponent<RectTransform>();
            rectTransform.anchoredPosition = inventory.GridToLocalPosition(position);
            rectTransform.sizeDelta = inventory.InventoryData.CellSize;

            if (_slotIcon != null && icon != null)
            {
                _slotIcon.sprite = icon;
                _slotIcon.gameObject.SetActive(true);
            }

            if (_slotLabel != null && !string.IsNullOrEmpty(label))
            {
                _slotLabel.text = label;
                _slotLabel.gameObject.SetActive(true);
            }
        }

        public override void OnDrop(PointerEventData eventData)
        {
            // Get the dragged item
            InventoryItemUI draggedItemUI = eventData.pointerDrag?.GetComponent<InventoryItemUI>();
            if (draggedItemUI == null)
            {
                Debug.Log("No dragged item found.");
                return;
            }

            Debug.Log($"Attempting to drop {draggedItemUI.Item.Name} (Type: {draggedItemUI.Item.ItemType}) on {_slotType} slot");

            // Check if the item type matches what this slot accepts
            if (draggedItemUI.Item.ItemType != _acceptedItemType)
            {
                Debug.Log($"Item type mismatch! Item is {draggedItemUI.Item.ItemType}, slot accepts {_acceptedItemType}");

                // Visual feedback for invalid drop
                StartCoroutine(ShowInvalidDropFeedback());
                return;
            }

            // Additional validation based on slot type
            bool isValid = false;

            if (_acceptedItemType == ItemType.Armor && _slotType is ArmorSlot armorSlot)
            {
                if (draggedItemUI.Item.Data is ArmorItem armorItem && armorItem.Slot == armorSlot)
                {
                    isValid = true;
                }
            }
            else if (_acceptedItemType == ItemType.Implant && _slotType is ImplantSlot implantSlot)
            {
                if (draggedItemUI.Item.Data is ImplantItem implantItem && implantItem.Slot == implantSlot)
                {
                    isValid = true;
                }
            }

            if (!isValid)
            {
                // Visual feedback for invalid slot type
                StartCoroutine(ShowInvalidDropFeedback());
                return;
            }

            // If validation passes, handle it directly rather than using base.OnDrop
            // This allows us to control the size behavior for specialized slots

            InventoryGridUI sourceInventory = draggedItemUI.ParentInventory;
            if (sourceInventory == null || _parentInventory == null)
            {
                Debug.LogError("Source or parent inventory is null");
                return;
            }

            // Get the item and create a size-adjusted copy for the specialized slot
            IItem originalItem = draggedItemUI.Item;
            Vector2Int originalSize = originalItem.Size;
            Vector2Int sourcePosition = draggedItemUI.GridPosition;

            try
            {
                // Create a specialized transfer helper that temporarily adjusts item size
                bool success = TransferToSpecializedSlot(
                    sourceInventory.Inventory,
                    _parentInventory.Inventory,
                    originalItem,
                    GridPosition);

                Debug.Log($"Specialized transfer result: {success}");

                if (!success)
                {
                    sourceInventory.CancelItemDrag();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error during specialized transfer: {ex.Message}");
                sourceInventory.CancelItemDrag();
            }
        }

        private bool TransferToSpecializedSlot(IInventory sourceInventory, IInventory targetInventory, IItem item, Vector2Int targetPosition)
        {
            if (sourceInventory == null || targetInventory == null || item == null)
                return false;

            // Find the item's position in the source inventory
            Vector2Int sourcePosition = Vector2Int.zero;
            bool found = false;

            foreach (var (storedItem, position) in sourceInventory.GetAllItems())
            {
                if (storedItem == item)
                {
                    sourcePosition = position;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Debug.LogWarning($"Item {item.Name} not found in source inventory");
                return false;
            }

            // Create a specialized version of the item with 1x1 size
            if (item.Clone() is not Item specializedItem)
            {
                Debug.LogError("Failed to clone item for specialized slot");
                return false;
            }

            Debug.Log($"Transferring item - Original size: {item.Size}, Target position: {targetPosition}");

            // Force 1x1 size for specialized slot validation
            specializedItem.ForceSize(new Vector2Int(1, 1));

            // Try to add specialized item to target position
            if (targetInventory.TryAddItem(specializedItem, targetPosition))
            {
                // Successfully added to target, now remove from source
                sourceInventory.TryRemoveItem(sourcePosition, out _);
                return true;
            }

            Debug.LogWarning($"Failed to add item to position {targetPosition}");
            return false;
        }

        private IEnumerator ShowInvalidDropFeedback()
        {
            // Implement visual feedback for invalid drop
            Image background = GetComponentInChildren<Image>();
            Color originalColor = background.color;

            background.color = Color.red;
            yield return new WaitForSeconds(0.3f);
            background.color = originalColor;
        }
    }
}
