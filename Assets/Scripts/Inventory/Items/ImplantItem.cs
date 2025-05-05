using UnityEngine;

namespace ParaMoon
{
    public enum ImplantSlot
    {
        Brain,
        Eye,
        Heart,
        Bone,
        Nerve,
    }

    [CreateAssetMenu(fileName = "New Implant", menuName = "Para Moon/Inventory/Implant Item")]
    public class ImplantItem : ItemData
    {
        [Header("Implant Properties")]
        [SerializeField] ImplantSlot _slot;

        public ImplantSlot Slot => _slot;
    }
}