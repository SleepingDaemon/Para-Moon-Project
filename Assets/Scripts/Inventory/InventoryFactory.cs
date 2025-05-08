using UnityEngine;

namespace ParaMoon
{
    public class InventoryFactory
    {
        public static IInventory CreateInventory(InventoryData config)
        {
            return new InventorySystem(config);
        }

        public static IInventory CreateEquipmentInventory(InventoryData config, EquipmentType type)
        {
            SpecializedInventory inventory = new(config);

            switch (type)
            {
                case EquipmentType.Armor:
                    SetupArmorSlots(inventory);
                    break;
                case EquipmentType.Implant:
                    SetupImplantSlots(inventory);
                    break;
            }

            return inventory;
        }

        private static void SetupArmorSlots(SpecializedInventory inventory)
        {
            foreach (ArmorSlot slotType in System.Enum.GetValues(typeof(ArmorSlot)))
            {
                int slotIndex = (int)slotType;
                Vector2Int slotPosition = new(0, slotIndex);

                inventory.RegisterSlotValidators(slotPosition,
                    new ArmorSlotValidator(slotType));
            }
        }

        private static void SetupImplantSlots(SpecializedInventory inventory)
        {
            foreach (ImplantSlot slotType in System.Enum.GetValues(typeof(ImplantSlot)))
            {
                int slotIndex = (int)slotType;
                Vector2Int slotPosition = new(0, slotIndex);

                inventory.RegisterSlotValidators(slotPosition,
                    new ImplantSlotValidator(slotType));
            }
        }
    }
}