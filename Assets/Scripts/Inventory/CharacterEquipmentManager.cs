using System;
using UnityEngine;

namespace ParaMoon
{
    public class CharacterEquipmentManager : MonoBehaviour
    {
        [SerializeField] private InventoryData _armorInventoryData;
        [SerializeField] private InventoryData _implantInventoryData;
        
        private SpecializedInventory _armorInventory;
        private SpecializedInventory _implantInventory;
        
        // Events for equipment changes
        public event Action<ArmorSlot, IItem> OnArmorChanged;
        public event Action<ImplantSlot, IItem> OnImplantChanged;
        
        private void Awake()
        {
            InitializeInventories();
        }

        private void OnDestroy()
        {
            if (_armorInventory != null)
            {
                _armorInventory.OnItemAdded -= HandleArmorAdded;
                _armorInventory.OnItemRemoved -= HandleArmorRemoved;
            }

            if (_implantInventory != null)
            {
                _implantInventory.OnItemAdded -= HandleImplantAdded;
                _implantInventory.OnItemRemoved -= HandleImplantRemoved;
            }
        }

        private void InitializeInventories()
        {
            // Initialize armor inventory
            _armorInventory = new SpecializedInventory(_armorInventoryData);
            
            // Register validators for each armor slot
            foreach(ArmorSlot slotType in Enum.GetValues(typeof(ArmorSlot)))
            {
                int slotIndex = (int)slotType;
                Vector2Int slotPosition = new(0, slotIndex); // Vertical layout

                _armorInventory.RegisterSlotValidators(slotPosition, 
                    new ArmorSlotValidator(slotType));
            }
            
            // Subscribe to equipment changes
            _armorInventory.OnItemAdded += HandleArmorAdded;
            _armorInventory.OnItemRemoved += HandleArmorRemoved;

            // Initialize implant inventory ===== TODO: UNFINISHED
            if (_implantInventoryData == null)
            {
                Debug.Log("Implant inventory data is not assigned!");
                return;
            }

            _implantInventory = new SpecializedInventory(_implantInventoryData);
            
            // Register validators for each implant slot
            foreach(ImplantSlot slotType in Enum.GetValues(typeof(ImplantSlot)))
            {
                int slotIndex = (int)slotType;
                Vector2Int slotPosition = new(0, slotIndex); // vertical layout
                
                _implantInventory.RegisterSlotValidators(slotPosition, 
                    new ImplantSlotValidator(slotType));
            }
            
            // Subscribe to implant changes
            _implantInventory.OnItemAdded += HandleImplantAdded;
            _implantInventory.OnItemRemoved += HandleImplantRemoved;
        }
        
        private void HandleArmorAdded(IItem item, Vector2Int position)
        {
            if (item.Data is ArmorItem armorItem)
            {
                OnArmorChanged?.Invoke(armorItem.Slot, item);
            }
        }
        
        private void HandleArmorRemoved(IItem item, Vector2Int position)
        {
            if (item.Data is ArmorItem equipItem)
            {
                OnArmorChanged?.Invoke(equipItem.Slot, null);
            }
        }
        
        private void HandleImplantAdded(IItem item, Vector2Int position)
        {
            if (item.Data is ImplantItem implantItem)
            {
                OnImplantChanged?.Invoke(implantItem.Slot, item);
            }
        }
        
        private void HandleImplantRemoved(IItem item, Vector2Int position)
        {
            if (item.Data is ImplantItem implantItem)
            {
                OnImplantChanged?.Invoke(implantItem.Slot, null);
            }
        }
        
        public IInventory GetArmorInventory() => _armorInventory;
        public IInventory GetImplantInventory() => _implantInventory;

        // Helper methods to equip/unequip items
        public bool EquipArmor(ArmorItem armorItem)
        {
            ArmorSlot slot = armorItem.Slot;
            Vector2Int position = new(0, (int)slot);

            // Remove any existing item in this slot
            if (_armorInventory.GetItemAt(position) != null)
            {
                _armorInventory.TryRemoveItem(position, out _);
            }

            // Add the new item
            return _armorInventory.TryAddItem(new Item(armorItem), position);
        }

        public bool EquipImplant(ImplantItem implantItem)
        {
            ImplantSlot slot = implantItem.Slot;
            Vector2Int position = new(0, (int)slot);

            // Remove any existing item in this slot
            if (_implantInventory.GetItemAt(position) != null)
            {
                _implantInventory.TryRemoveItem(position, out _);
            }

            // Add the new item
            return _implantInventory.TryAddItem(new Item(implantItem), position);
        }

        // Get equipped items
        public IItem GetEquippedArmor(ArmorSlot slot)
        {
            Vector2Int position = new(0, (int)slot);
            return _armorInventory.GetItemAt(position);
        }

        public IItem GetEquippedImplant(ImplantSlot slot)
        {
            Vector2Int position = new(0, (int)slot);
            return _implantInventory.GetItemAt(position);
        }
    }
}
