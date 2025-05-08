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

        public void Initialize(Vector2Int position, InventoryGridView parentView,
                              ItemType acceptedType, object slotType, Sprite icon = null, string label = null)
        {
            base.Initialize(position, parentView);

            _acceptedItemType = acceptedType;
            _slotType = slotType;

            // Position the slot correctly using the inventory's grid positioning
            RectTransform rectTransform = GetComponent<RectTransform>();
            rectTransform.anchoredPosition = parentView.GridToLocalPosition(position);
            //rectTransform.sizeDelta = parentView.InventoryData.CellSize;

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
            InventoryItemView draggedItemUI = eventData.pointerDrag?.GetComponent<InventoryItemView>();
            if (draggedItemUI == null)
                return;

            Debug.Log($"Attempting to drop {draggedItemUI.Item.Name} (Type: {draggedItemUI.Item.ItemType}) on {_slotType} slot");

            // Check if the item type matches what this slot accepts
            if (draggedItemUI.Item.ItemType != _acceptedItemType)
            {
                Debug.Log($"Item type mismatch! Item is {draggedItemUI.Item.ItemType}, slot accepts {_acceptedItemType}");

                // Visual feedback for invalid drop
                StartCoroutine(ShowInvalidDropFeedback());
                return;
            }

            // Validate specific slot requirements
            bool isValid = ValidateSlotRequirements(draggedItemUI.Item);

            if (!isValid)
            {
                // Visual feedback for invalid slot type
                StartCoroutine(ShowInvalidDropFeedback());
                return;
            }

            // Handle transfer
            InventoryGridView sourceView = draggedItemUI.ParentView;
            if (sourceView == null || _parentView == null)
            {
                Debug.LogError("Source or parent inventory is null");
                return;
            }

            // Get the item and create a size-adjusted copy for the specialized slot
            IItem originalItem = draggedItemUI.Item;
            Vector2Int sourcePosition = draggedItemUI.GridPosition;

            try
            {
                // Create a specialized transfer helper that temporarily adjusts item size
                bool success = InventoryService.Instance.TransferToSpecializedSlot(
                    sourceView.Inventory,
                    _parentView.Inventory,
                    originalItem,
                    GridPosition);

                Debug.Log($"Specialized transfer result: {success}");

                if (!success)
                {
                    draggedItemUI.ResetPosition();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error during specialized transfer: {ex.Message}");
                draggedItemUI.ResetPosition();
            }
        }

        private bool ValidateSlotRequirements(IItem item)
        {
            // Validate based on slot type
            if (_acceptedItemType == ItemType.Armor && _slotType is ArmorSlot armorSlot)
            {
                return item.Data is ArmorItem armorItem && armorItem.Slot == armorSlot;
            }
            else if (_acceptedItemType == ItemType.Implant && _slotType is ImplantSlot implantSlot)
            {
                return item.Data is ImplantItem implantItem && implantItem.Slot == implantSlot;
            }

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
