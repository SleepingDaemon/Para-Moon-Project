using System;
using UnityEngine;

namespace ParaMoon
{
    public static class InventoryHelper
    {
        public static bool TransferItem(IInventory fromInventory, IInventory toInventory, IItem item)
        {
            if (fromInventory == null || toInventory == null || item == null)
                return false;

            // Find the item's position in the source inventory
            Vector2Int itemPosition = Vector2Int.zero;
            bool found = false;

            foreach (var (storedItem, position) in fromInventory.GetAllItems())
            {
                if (storedItem == item)
                {
                    itemPosition = position;
                    found = true;
                    break;
                }
            }

            if (!found)
                return false; // Item not found in the source inventory

            // Try to add to target inventory
            if (toInventory.TryAddItem(item, out _))
            {
                // Remove from source inventory
                fromInventory.TryRemoveItem(itemPosition, out _);
                return true;
            }
            return false;
        }

        // Add overload that allows specifying a target position
        public static bool TransferItem(IInventory fromInventory, IInventory toInventory,
                                       IItem item, Vector2Int targetPosition)
        {
            if (fromInventory == null || toInventory == null || item == null)
                return false;

            // Find the item's position in the source inventory
            Vector2Int itemPosition = Vector2Int.zero;
            bool found = false;

            foreach (var (storedItem, position) in fromInventory.GetAllItems())
            {
                if (storedItem == item)
                {
                    itemPosition = position;
                    found = true;
                    break;
                }
            }

            if (!found)
                return false; // Item not found in the source inventory

            // Check if the target position is valid in the destination inventory
            if (!toInventory.IsPositionValid(targetPosition, item.Size) ||
                !toInventory.IsPositionFree(targetPosition, item.Size))
                return false;

            // Try to add to target inventory at specific position
            if (toInventory.TryAddItem(item, targetPosition))
            {
                // Remove from source inventory
                fromInventory.TryRemoveItem(itemPosition, out _);
                return true;
            }
            return false;
        }

        // Helper method for specialized slots (equipment, tools, etc.)
        public static bool TryEquipItem(IInventory fromInventory, IInventory toInventory,
                                       IItem item, ItemType requiredType)
        {
            // First check if the item is of the right type for this slot
            if (item == null || item.ItemType != requiredType)
                return false;

            // Then attempt the normal transfer
            return TransferItem(fromInventory, toInventory, item);
        }

        /// <summary>
        /// Transfers an item from a specialized slot back to a regular inventory, restoring its original size
        /// </summary>
        public static bool TransferFromSpecializedSlot(IInventory specializedInventory, IInventory regularInventory,
                                                     IItem item, Vector2Int sourcePosition)
        {
            if (specializedInventory == null || regularInventory == null || item == null)
            {
                Debug.LogError("TransferFromSpecializedSlot: One or more parameters are null");
                return false;
            }

            try
            {
                // First check if the item exists at the source position
                if (specializedInventory.GetItemAt(sourcePosition) != item)
                {
                    Debug.LogWarning($"Item {item.Name} not found at position {sourcePosition}");
                    return false;
                }

                // If it's an Item instance (which it should be), reset its size
                if (item is Item concreteItem)
                {
                    // Reset forced size before transferring
                    concreteItem.ResetSize();
                    Debug.Log($"Reset size of {item.Name} to original: {item.Size}");
                }
                else
                {
                    Debug.LogWarning($"Item {item.Name} is not an Item instance, size reset not possible");
                }

                // Now try to add it to the regular inventory (using its full size)
                if (regularInventory.TryAddItem(item, out Vector2Int newPosition))
                {
                    // If successful, remove from specialized inventory
                    Debug.Log($"Successfully added {item.Name} to regular inventory at {newPosition}");
                    specializedInventory.TryRemoveItem(sourcePosition, out _);
                    return true;
                }
                else
                {
                    Debug.LogWarning($"No space in regular inventory for {item.Name} with size {item.Size}");

                    // If the transfer failed, force size back to 1x1 for specialized inventory
                    if (item is Item concreteItemRevert)
                    {
                        concreteItemRevert.ForceSize(new Vector2Int(1, 1));
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in TransferFromSpecializedSlot: {ex.Message}");
                return false;
            }
        }
    }
}