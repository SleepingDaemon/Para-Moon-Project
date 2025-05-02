using UnityEngine;

namespace ParaMoon
{

    [CreateAssetMenu(fileName = "New Grid Item", menuName = "Para Moon/Inventory/Grid Item")]
    public class ItemData : ScriptableObject
    {
        [Header("Basic Information")]
        [SerializeField] string _itemID;
        [SerializeField] Sprite _icon;
        [SerializeField] string _itemName;
        [SerializeField, TextArea(2, 5)] string _description;
        [SerializeField] ItemType _itemType = ItemType.None;

        [Header("Grid Properties")]
        [Min(1)]
        [SerializeField] int _width = 1;
        [Min(1)]
        [SerializeField] int _height = 1;
        [SerializeField] bool _canRotate = false;

        [Header("Stack Properties")]
        [SerializeField] bool _isStackable = false;
        [SerializeField] int _maxStackSize = 1;

        [Header("Item Properties")]
        [SerializeField] int _value;
        [SerializeField] float _weight;

        public string ID => string.IsNullOrEmpty(_itemID) ? name : _itemID;
        public string Name => _itemName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public ItemType ItemType => _itemType;
        public Vector2Int Size => new(_width, _height);
        public bool IsStackable => _isStackable;
        public int MaxStackSize => _isStackable ? _maxStackSize : 1;
        public int Value => _value;
        public float Weight => _weight;

        // Create an instance of the item
        public Item CreateItem(int stackSize = 1)
        {
            return new Item(this, stackSize);
        }
    }
}