using UnityEngine;

namespace ParaMoon
{
    public class ArmorSlotValidator : ISlotValidator
    {
        readonly ArmorSlot _slotType;

        public ArmorSlotValidator(ArmorSlot slotType)
        {
            _slotType = slotType;
        }

        public bool CanAcceptItem(IItem item, Vector2Int slotPosition)
        {
            // Check if the item is an equipment item
            if (item.ItemType != ItemType.Armor)
                return false;

            // Get the equipment-specific data
            if (item.Data is ArmorItem armorItem)
            {
                // Check if the equipment slot type matches
                return armorItem.Slot == _slotType;
            }

            return false;
        }
    }
}
