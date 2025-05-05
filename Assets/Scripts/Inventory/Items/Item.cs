using System;
using UnityEngine;

namespace ParaMoon
{
    [Serializable]
    public class Item : IItem
    {
        [SerializeField] ItemData _data;
        [SerializeField] int _stackSize = 1;
        [SerializeField] bool _canRotate = false;

        // For specialized slots - override the normal size
        private Vector2Int? _forcedSize = null;

        // IItem implementation
        public string ID => _data.ID;
        public string Name => _data.Name;
        public string Description => _data.Description;
        public ItemType ItemType => _data.ItemType;
        public Sprite Icon => _data.Icon;
        public Vector2Int Size
        {
            get
            {
                // Return forced size if it's set, otherwise use data size
                return _forcedSize ?? new Vector2Int(_data.Width, _data.Height);
            }
        }
        public bool IsStackable => _data.IsStackable;
        public int MaxStackSize => _data.MaxStackSize;
        public int CurrentStackSize
        {
            get => _stackSize;
            set => _stackSize = Mathf.Clamp(value, 1, MaxStackSize);
        }

        public ItemData Data => _data;

        public Item(ItemData data, int stackSize = 1)
        {
            _data = data;
            _stackSize = Mathf.Clamp(stackSize, 1, data.MaxStackSize);
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

        public Item Clone()
        {
            Item clone = new(_data, _stackSize);
            clone._canRotate = _canRotate;

            return clone;
        }
    }
}