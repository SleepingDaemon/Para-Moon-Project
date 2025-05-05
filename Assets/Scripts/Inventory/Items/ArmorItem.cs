using UnityEngine;

namespace ParaMoon
{
    public enum ArmorSlot
    {
        Head,
        Chest,
        Arms,
        Legs,
        Feet,
    }

    [CreateAssetMenu(fileName = "New Armor", menuName = "Para Moon/Inventory/Armor Item")]
    public class ArmorItem : ItemData
    {
        [Header("Equipment Properties")]
        [SerializeField] ArmorSlot _slot;
        //[SerializeField] List<StatType> _stats = new();
        [SerializeField] GameObject _armorModel;

        public ArmorSlot Slot => _slot;
    }
}