using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParaMoon
{
    public interface IInventory
    {
        Vector2Int GridSize { get; }
        bool TryAddItem(IItem item, Vector2Int position);
        bool TryAddItem(IItem item, out Vector2Int position);
        bool TryRemoveItem(Vector2Int position, out IItem item);
        bool TryMoveItem(Vector2Int fromPosition, Vector2Int toPosition);
        IItem GetItemAt(Vector2Int position);
        bool IsPositionValid(Vector2Int position, Vector2Int size);
        bool IsPositionFree(Vector2Int position, Vector2Int size);
        bool IsPositionFreeExcept(Vector2Int toPosition, Vector2Int size, IItem exceptItem);
        IEnumerable<(Item item, Vector2Int position)> GetAllItems();

        event Action<IItem, Vector2Int> OnItemAdded;
        event Action<IItem, Vector2Int> OnItemRemoved;
        event Action<IItem, Vector2Int, Vector2Int> OnItemMoved;
    }
}