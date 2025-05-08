using UnityEngine;

namespace ParaMoon
{
    public class ItemTypeValidator : BaseSlotValidator
    {
        readonly ItemType _acceptedItemType;

        public ItemTypeValidator(ItemType acceptedItemType)
        {
            _acceptedItemType = acceptedItemType;
        }

        public override bool CanAcceptItem(IItem item, Vector2Int position)
        {
            return item.ItemType == _acceptedItemType;
        }
    }
}
