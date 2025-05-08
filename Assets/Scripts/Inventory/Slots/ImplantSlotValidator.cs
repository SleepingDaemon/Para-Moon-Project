using UnityEngine;

namespace ParaMoon
{
    public class ImplantSlotValidator : ItemTypeValidator
    {
        private readonly ImplantSlot _slotType;

        public ImplantSlotValidator(ImplantSlot slotType) : base(ItemType.Implant)
        {
            _slotType = slotType;
        }

        public override bool CanAcceptItem(IItem item, Vector2Int position)
        {
            if (!base.CanAcceptItem(item, position)) return false;

            return item.Data is ImplantItem implantItem && implantItem.Slot == _slotType;
        }
    }
}
