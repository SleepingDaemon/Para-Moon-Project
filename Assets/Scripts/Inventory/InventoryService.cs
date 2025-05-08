using System;
using UnityEngine;
using static Unity.VisualScripting.Member;

namespace ParaMoon
{
    public class InventoryService
    {
        public event Action<IInventory, IInventory, IItem> OnItemTransferred;

        // Singleton implementation (consider dependency injection in the future)
        private static InventoryService _instance;
        public static InventoryService Instance => _instance ??= new InventoryService();

        private InventoryService() { }

        public bool TransferItem(IInventory fromInventory, IInventory toInventory, IItem item)
        {
            // Find item in source inventory
            Vector2Int? fromPosition = null;
            foreach (var (storedItem, position) in fromInventory.GetAllItems())
            {
                if (storedItem == item)
                {
                    fromPosition = position;
                    break;
                }
            }

            if (!fromPosition.HasValue)
                return false;

            // Try to add to target inventory
            if (toInventory.TryAddItem(item, out _))
            {
                // Remove from source inventory on success
                fromInventory.TryRemoveItem(fromPosition.Value, out _);
                OnItemTransferred?.Invoke(fromInventory, toInventory, item);
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

        /// <summary>
        /// Transfers an item from a specialized slot back to a regular inventory, restoring its original size
        /// </summary>
        public bool TransferFromSpecializedSlot(IInventory specializedInventory, IInventory regularInventory,
                                                     IItem item, Vector2Int sourcePosition)
        {
            if (specializedInventory == null || regularInventory == null || item == null)
            {
                Debug.LogError("TransferFromSpecializedSlot: One or more parameters are null");
                return false;
            }

            // First check if the item exists at the source position
            if (specializedInventory.GetItemAt(sourcePosition) != item)
            {
                Debug.LogWarning($"Item {item.Name} not found at position {sourcePosition}");
                return false;
            }

            // If it's an Item instance (which it should be), reset its size
            if (item is Item actualItem)
                actualItem.ResetSize();

            // Now try to add it to the regular inventory (using its full size)
            if (regularInventory.TryAddItem(item, out Vector2Int newPosition))
            {
                // If successful, remove from specialized inventory
                specializedInventory.TryRemoveItem(sourcePosition, out _);
                OnItemTransferred?.Invoke(specializedInventory, regularInventory, item);
                return true;
            }
            else
            {
                // If the transfer failed, force size back to 1x1 for specialized inventory
                if (item is Item concreteItemRevert)
                {
                    concreteItemRevert.ForceSize(new Vector2Int(1, 1));
                }

                return false;
            }
        }

        public bool TransferToSpecializedSlot(IInventory regularInventory, IInventory specializedInventory,
                                              IItem item, Vector2Int targetPosition)
        {
            if (regularInventory == null || specializedInventory == null || item == null)
                return false;

            // Find item position in source
            Vector2Int sourcePosition = Vector2Int.zero;
            bool found = false;

            foreach (var (storedItem, position) in regularInventory.GetAllItems())
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

            // Clone the item for specialized slot
            IItem specializedItem = item.Clone();
            if (specializedItem is not Item actualItem)
            {
                Debug.LogError("Failed to clone item for specialized slot");
                return false;
            }

            // Force 1x1 size for specialized slot
            actualItem.ForceSize(new Vector2Int(1, 1));

            // Try to add to target position
            if (specializedInventory.TryAddItem(specializedItem, targetPosition))
            {
                // Remove from source on success
                regularInventory.TryRemoveItem(sourcePosition, out _);
                return true;
            }

            return false;
        }
    }
}