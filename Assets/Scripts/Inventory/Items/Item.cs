using System;
using UnityEngine;

namespace ParaMoon
{
    [Serializable]
    public class Item : IItem
    {
        ItemData _data;
        int _stackCount;

        // For specialized slots - override the normal size
        Vector2Int? _forcedSize = null;

        // IItem implementation
        public string ID => _data.ID;
        public string Name => _data.Name;
        public string Description => _data.Description;
        public ItemType ItemType => _data.ItemType;
        public Sprite Icon => _data.Icon;
        public Vector2Int Size => _forcedSize ?? _data.Size;
        public bool IsStackable => _data.IsStackable;
        public int MaxStackSize => _data.MaxStackSize;
        public int StackCount
        {
            get => _stackCount;
            set => _stackCount = Mathf.Clamp(value, 1, MaxStackSize);
        }
        public ItemData Data => _data;

        public Item(ItemData data, int stackCount = 1)
        {
            _data = data;
            _stackCount = Mathf.Clamp(stackCount, 1, data.MaxStackSize);
        }

        public Item(Item other)
        {
            _data = other._data;
            _stackCount = other._stackCount;
            _forcedSize = other._forcedSize;
        }

        // Method to force a different size for specialized slots
        public void ForceSize(Vector2Int newSize)
        {
            _forcedSize = newSize;
        }

        // Reset to original size
        public void ResetSize()
        {
            _forcedSize = null;
        }

        public bool TryAddToStack(int count)
        {
            if (!_data.IsStackable) 
                return false;

            int newTotal = _stackCount + count;
            if (newTotal <= _data.MaxStackSize)
            {
                _stackCount = newTotal;
                return true;
            }
            return false;
        }

        public bool TryRemoveFromStack(int count, out int removed)
        {
            removed = 0;

            if (count <= 0) return false;

            if (count >= _stackCount)
            {
                removed = _stackCount;
                _stackCount = 0;
                return true;
            }

            _stackCount -= count;
            removed = count;
            return true;
        }

        public Item Clone()
        {
            return new(this);
        }
    }
}