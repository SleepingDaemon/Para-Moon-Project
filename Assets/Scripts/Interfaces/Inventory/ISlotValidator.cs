using UnityEngine;

namespace ParaMoon
{
    public interface ISlotValidator
    {
        bool CanAcceptItem(IItem item, Vector2Int position);
    }
}