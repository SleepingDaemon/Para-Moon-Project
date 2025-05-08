using UnityEngine;

namespace ParaMoon
{
    public abstract class BaseSlotValidator : ISlotValidator
    {
        public virtual bool CanAcceptItem(IItem item, Vector2Int position)
        {
            return true; // Default implementation allows all items
        }
    }
}
