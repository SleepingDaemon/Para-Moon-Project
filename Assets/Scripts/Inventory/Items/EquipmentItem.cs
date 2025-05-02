using UnityEngine;

namespace ParaMoon
{
    [CreateAssetMenu(fileName = "New Equipment", menuName = "Para Moon/Inventory/Equipment Item")]
    public class EquipmentItem : ItemData
    {
        [Header("Equipment Properties")]
        [SerializeField] EquipmentSlot _slot;
        //[SerializeField] List<StatType> _stats = new();
        [SerializeField] GameObject _equipmentModel;

        public enum EquipmentSlot
        {
            Head,
            Chest,
            Legs,
            Arms,
            Feet,
        }
    }
}