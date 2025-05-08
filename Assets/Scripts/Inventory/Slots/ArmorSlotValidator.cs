using UnityEngine;
using UnityEngine.UIElements;

namespace ParaMoon
{
    public class ArmorSlotValidator : ItemTypeValidator
    {
        readonly ArmorSlot _slotType;

        public ArmorSlotValidator(ArmorSlot slotType) : base(ItemType.Armor)
        {
            _slotType = slotType;
        }

        public override bool CanAcceptItem(IItem item, Vector2Int slotPosition)
        {
            if (!base.CanAcceptItem(item, slotPosition)) 
                return false;

            return item.Data is ArmorItem armorItem && armorItem.Slot == _slotType;
        }
    }
}
