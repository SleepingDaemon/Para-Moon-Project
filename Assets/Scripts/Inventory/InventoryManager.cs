using UnityEngine;

namespace ParaMoon
{
    public class InventoryManager : MonoBehaviour, IInventoryProvider
    {
        [SerializeField] InventoryData _inventoryData;
        [SerializeField] ContainerType _type = ContainerType.Storage;

        [Header("Test Items")]
        [SerializeField] private ItemData[] _testItems;

        InventorySystem _inventory;
        InventoryGridUI _inventoryUI;


        public InventoryData InventoryData => _inventoryData;
        public IInventory Inventory => _inventory;
        public InventoryGridUI InventoryUI => _inventoryUI;

        private void Awake()
        {
            InitializeInventory();
        }

        private void Start()
        {
            if (_inventoryUI == null)
            {
                ServiceLocator.Instance.WhenAvailable<UIManager>(uiManager =>
                {
                    if (_type == ContainerType.Player)
                    {
                        // Try to get the inventory UI from the UIManager
                        _inventoryUI = uiManager.GetPlayerInventoryUI();
                    }
                    else
                    {
                        // Get the inventory UI for other container types
                        _inventoryUI = uiManager.GetContainerUI(_type);
                    }

                    // Sync the inventory UI with the inventory system
                    if (_inventoryUI != null)
                        _inventoryUI.SetInventory(_inventory);
                    else
                        Debug.LogError("[InventoryManager] UIManager available but GetInventoryUI returned null");
                });
            }
            else
            {
                // If UI is already assigned, set it directly
                _inventoryUI.SetInventory(_inventory);
            }
        }

        private void InitializeInventory()
        {
            if (_inventoryData == null)
            {
                Debug.LogError($"[EntityInventory] InventoryData is not assigned for {gameObject.name}");
                return;
            }

            _inventory = new InventorySystem(_inventoryData);
        }

        [ContextMenu("Add Test Items")]
        private void AddTestItems()
        {
            // Add null checks to prevent NullReferenceException
            if (_inventoryUI == null)
            {
                Debug.LogWarning("[InventoryManager] Cannot add test items: _inventoryUI is null");
                return;
            }

            if (_testItems == null || _testItems.Length == 0)
            {
                Debug.Log("[InventoryManager] No test items to add");
                return;
            }

            foreach (var itemDef in _testItems)
            {
                if (itemDef != null)
                {
                    // Create an item instance
                    var item = itemDef.CreateItem();

                    if (item is IItem itemInstance)
                    {
                        // Add to inventory at random position or first available
                        _inventoryUI.AddItem(itemInstance);
                    }
                }
            }
        }
    }
}