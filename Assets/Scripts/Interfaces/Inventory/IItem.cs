using UnityEngine;

namespace ParaMoon
{
    public interface IItem
    {
        ItemData Data { get; }
        string ID { get; }
        string Name { get; }
        string Description { get; }
        ItemType ItemType { get; }
        Sprite Icon { get; }
        Vector2Int Size { get; }
        bool IsStackable { get; }
        int MaxStackSize { get; }
        int CurrentStackSize { get; set; }

        Item Clone();
    }
}