using UnityEngine;

namespace ParaMoon
{
    public class ImplantSlotValidator : ISlotValidator
    {
        readonly ImplantSlot _slotType;

        public ImplantSlotValidator(ImplantSlot slotType)
        {
            _slotType = slotType;
        }

        public bool CanAcceptItem(IItem item, Vector2Int slotPosition)
        {
            // Check if the item is an implant item
            if (item.ItemType != ItemType.Implant)
                return false;

            // Get the implant-specific data
            if (item.Data is ImplantItem implantItem)
            {
                // Check if the implant slot type matches
                return implantItem.Slot == _slotType;
            }

            return false;
        }
    }
}
