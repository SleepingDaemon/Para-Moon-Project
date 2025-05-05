using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParaMoon
{
    public class SpecializedInventory : InventorySystem
    {
        Dictionary<Vector2Int, ISlotValidator> _slotValidators = new();

        public SpecializedInventory(InventoryData data) : base(data) { }

        public void RegisterSlotValidators(Vector2Int position, ISlotValidator validator)
        {
            if (_slotValidators.ContainsKey(position))
            {
                Debug.LogWarning($"Slot validator already registered at {position}");
                return;
            }
            _slotValidators[position] = validator;
        }

        public override bool TryAddItem(IItem item, Vector2Int position)
        {
            // Log the item properties for debugging
            Debug.Log($"Attempting to add {item.Name} with size {item.Size} to position {position}");

            // First check if this position has a validator
            if (_slotValidators.TryGetValue(position, out ISlotValidator validator))
            {
                // If it does, validate the item
                if (!validator.CanAcceptItem(item, position))
                {
                    Debug.LogWarning($"Item {item.Name} is not valid for this slot at {position}");
                    return false; // Item is not valid for this slot
                }

                // For specialized slots, we'll assume 1x1 size
                Debug.Log($"Item {item.Name} passed validator for specialized slot at {position}");
            }

            // If validation passes or there's no validator, proceed with normal add
            return base.TryAddItem(item, position);
        }

        public override bool TryMoveItem(Vector2Int fromPosition, Vector2Int toPosition)
        {
            // Get the item at the from position
            IItem item = GetItemAt(fromPosition);
            if (item == null)
                return false;

            // Check if the target position has a validator
            if (_slotValidators.TryGetValue(toPosition, out ISlotValidator validator))
            {
                // Validate the item for the target position
                if (!validator.CanAcceptItem(item, toPosition))
                {
                    Debug.LogWarning($"Cannot move item {item.Name} to position {toPosition} - failed validation");
                    return false;
                }
            }

            // If validation passes or there's no validator, proceed with base move
            return base.TryMoveItem(fromPosition, toPosition);
        }
    }
}