using System;
using UnityEngine;

namespace ParaMoon
{
    /// <summary>
    /// Handles equipment slot operations
    /// </summary>
    public class EquipmentService
    {
        readonly IInventory _armorInventory;
        readonly IInventory _implantInventory;

        // Events
        public event Action<ArmorSlot, IItem> OnArmorChanged;
        public event Action<ImplantSlot, IItem> OnImplantChanged;

        public EquipmentService(IInventory armorInventory, IInventory implantInventory)
        {
            _armorInventory = armorInventory;
            _implantInventory = implantInventory;

            // Subscribe to inventory changes
            if (_armorInventory != null)
            {
                _armorInventory.OnItemAdded += HandleArmorAdded;
                _armorInventory.OnItemRemoved += HandleArmorRemoved;
            }

            if (_implantInventory != null)
            {
                _implantInventory.OnItemAdded += HandleImplantAdded;
                _implantInventory.OnItemRemoved += HandleImplantRemoved;
            }
        }

        // Constructor for Armor
        public EquipmentService(IInventory armorInventory)
        {
            _armorInventory = armorInventory;
            _implantInventory = null;
            // Subscribe to inventory changes
            if (_armorInventory != null)
            {
                _armorInventory.OnItemAdded += HandleArmorAdded;
                _armorInventory.OnItemRemoved += HandleArmorRemoved;
            }
        }

        public IInventory GetArmorInventory() => _armorInventory;
        public IInventory GetImplantInventory() => _implantInventory;

        // Equipment operations
        public bool EquipArmor(IItem armorItem)
        {
            if (armorItem.Data is not ArmorItem armor)
                return false;

            ArmorSlot slot = armor.Slot;
            Vector2Int position = new(0, (int)slot);

            // Remove any existing item
            if (_armorInventory.GetItemAt(position) != null)
                _armorInventory.TryRemoveItem(position, out _);

            // Add the new item
            return _armorInventory.TryAddItem(armorItem, position);
        }

        public bool EquipImplant(IItem implantItem)
        {
            if (implantItem.Data is not ImplantItem implant)
                return false;

            ImplantSlot slot = implant.Slot;
            Vector2Int position = new(0, (int)slot);

            // Remove any existing item
            if (_implantInventory.GetItemAt(position) != null)
                _implantInventory.TryRemoveItem(position, out _);

            // Add the new item
            return _implantInventory.TryAddItem(implantItem, position);
        }

        // Event handlers
        private void HandleArmorAdded(IItem item, Vector2Int position)
        {
            if (item.Data is ArmorItem armorItem)
                OnArmorChanged?.Invoke(armorItem.Slot, item);
        }

        private void HandleArmorRemoved(IItem item, Vector2Int position)
        {
            if (item.Data is ArmorItem armorItem)
                OnArmorChanged?.Invoke(armorItem.Slot, null);
        }

        private void HandleImplantAdded(IItem item, Vector2Int position)
        {
            if (item.Data is ImplantItem implantItem)
                OnImplantChanged?.Invoke(implantItem.Slot, item);
        }

        private void HandleImplantRemoved(IItem item, Vector2Int position)
        {
            if (item.Data is ImplantItem implantItem)
                OnImplantChanged?.Invoke(implantItem.Slot, null);
        }
    }
}