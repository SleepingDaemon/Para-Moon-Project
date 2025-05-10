using System.Runtime.InteropServices;
using UnityEngine;

namespace ParaMoon
{
    public class PlayerInventory : MonoBehaviour, IInventoryProvider
    {
        [Header("Inventory Configuration")]
        [SerializeField] InventoryData _inventoryData;
        [SerializeField] InventoryData _armorInventoryData;
        [SerializeField] InventoryData _implantInventoryData;

        [Header("Debug")]
        [SerializeField] private bool _addTestItems;
        [SerializeField] private ItemData[] _testItems;

        InventoryUIController _inventoryUIController;
        IInventory _inventory;
        EquipmentService _equipmentService;

        // Expose inventories through properties
        public IInventory Inventory => _inventory;
        public IInventory ArmorInventory => _equipmentService?.GetArmorInventory();
        public IInventory ImplantInventory => _equipmentService?.GetImplantInventory();
        public EquipmentService Equipment => _equipmentService;

        private void Awake()
        {
            DI.Process(gameObject);
        }

        private void Start()
        {
            InitializeInventories();
            ConnectToUIController();

            if (_addTestItems)
                AddTestItems();
        }

        private void OnDestroy()
        {
            if (_equipmentService != null)
            {
                _equipmentService.OnArmorChanged -= HandleArmorChanged;
                _equipmentService.OnImplantChanged -= HandleImplantChanged;
            }

            ClearInventories();
        }

        /// <summary>
        /// Initializes all inventory systems for the player
        /// </summary>
        private void InitializeInventories()
        {
            if (_inventoryData == null)
            {
                Debug.LogError("[PlayerInventory] Main inventory data is not assigned!");
                return;
            }

            // Create the main inventory
            _inventory = InventoryFactory.CreateInventory(_inventoryData);

            // Initialize equipment inventories and service if data is available
            if (_armorInventoryData != null)
            {
                IInventory armorInventory = InventoryFactory.CreateEquipmentInventory(_armorInventoryData, EquipmentType.Armor);
                _equipmentService = new EquipmentService(armorInventory);
                _equipmentService.OnArmorChanged += HandleArmorChanged;
            }
            else
            {
                Debug.LogWarning("[PlayerInventory] Equipment inventory data missing. Equipment functionality will be unavailable.");
            }

            // TODO: Initialize Armor with Implant
        }

        /// <summary>
        /// Connects to the UI controller to display the inventories
        /// </summary>
        private void ConnectToUIController()
        {
            DI.WhenAvailable<UIManager>(ui =>
            {
                _inventoryUIController = ui.GetInventoryUIController();

                if (_inventoryUIController != null)
                {
                    _inventoryUIController.Initialize(_inventory, _equipmentService);
                    Debug.Log("[PlayerInventory] Connected to InventoryUIController via WhenAvailable");
                }
                else
                {
                    Debug.LogWarning("[PlayerInventory] Inventory UI Controller not available in UIManager!");
                }
            });
        }

        /// <summary>
        /// Adds an item to the player's inventory
        /// </summary>
        public bool AddItem(IItem item)
        {
            if (_inventory == null || item == null)
                return false;

            return _inventory.TryAddItem(item, out _);
        }

        /// <summary>
        /// Attempts to equip an armor item in the appropriate slot
        /// </summary>
        public bool EquipArmor(IItem armorItem)
        {
            if (_equipmentService == null)
                return false;

            return _equipmentService.EquipArmor(armorItem);
        }

        /// <summary>
        /// Attempts to equip an implant item in the appropriate slot
        /// </summary>
        public bool EquipImplant(IItem implantItem)
        {
            if (_equipmentService == null)
                return false;

            return _equipmentService.EquipImplant(implantItem);
        }

        /// <summary>
        /// Handles armor equipment changes
        /// </summary>
        private void HandleArmorChanged(ArmorSlot slot, IItem item)
        {
            // Apply or remove armor effects as needed
            // Example: Update player stats, appearance, etc.
            Debug.Log($"[PlayerInventory] Armor changed in slot {slot}: {(item != null ? item.Name : "None")}");
        }

        /// <summary>
        /// Handles implant equipment changes
        /// </summary>
        private void HandleImplantChanged(ImplantSlot slot, IItem item)
        {
            // Apply or remove implant effects as needed
            // Example: Update player abilities, stats, etc.
            Debug.Log($"[PlayerInventory] Implant changed in slot {slot}: {(item != null ? item.Name : "None")}");
        }

        /// <summary>
        /// Adds test items to the inventory for debugging
        /// </summary>
        private void AddTestItems()
        {
            if (_testItems == null || _testItems.Length == 0 || _inventory == null)
                return;

            foreach (var itemData in _testItems)
            {
                if (itemData != null)
                {
                    // Create item instance
                    Item item = new(itemData);
                    AddItem(item);
                }
            }
        }

        /// <summary>
        /// Clears all inventories
        /// </summary>
        public void ClearInventories()
        {
            if (_inventory is InventorySystem invSystem)
                invSystem.Clear();

            // Clear equipment inventories if needed
            if (ArmorInventory is InventorySystem armorSystem)
                armorSystem.Clear();

            if (ImplantInventory is InventorySystem implantSystem)
                implantSystem.Clear();
        }

        /// <summary>
        /// Shows or hides the inventory UI
        /// </summary>
        public void ToggleInventoryUI()
        {
            if (ServiceLocator.Instance.TryGetService<UIManager>(out var uiManager))
            {
                if (uiManager.CurrentState == UIManager.UIState.Gameplay)
                    uiManager.ToggleEROSMenu();
                else if (uiManager.CurrentState == UIManager.UIState.EROSMenu)
                    uiManager.ToggleEROSMenu();
            }
        }
    }
}