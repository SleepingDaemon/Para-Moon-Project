using UnityEngine;

namespace ParaMoon
{
    [System.Serializable]
    public class Item : IItem
    {
        [SerializeField] ItemData _data;
        [SerializeField] int _stackSize = 1;
        [SerializeField] bool _canRotate = false;

        // IItem implementation
        public string ID => _data.ID;
        public string Name => _data.Name;
        public string Description => _data.Description;
        public Sprite Icon => _data.Icon;
        public Vector2Int Size => _canRotate ?
            new Vector2Int(_data.Size.y, _data.Size.x) :
            _data.Size;
        public bool IsStackable => _data.IsStackable;
        public int MaxStackSize => _data.MaxStackSize;
        public int CurrentStackSize
        {
            get => _stackSize;
            set => _stackSize = Mathf.Clamp(value, 1, MaxStackSize);
        }

        public ItemData Data => _data;
        public bool CanRotate => _canRotate;

        public Item(ItemData data, int stackSize = 1)
        {
            _data = data;
            _stackSize = Mathf.Clamp(stackSize, 1, data.MaxStackSize);
        }

        public void Rotate()
        {
            _canRotate = !_canRotate;
        }

        public Item Clone()
        {
            Item clone = new(_data, _stackSize);
            clone._canRotate = _canRotate;
            return clone;
        }
    }
}