using Unity.VisualScripting;
using UnityEngine;

namespace ParaMoon
{
    public class InventoryManager : MonoBehaviour, IInventoryProvider
    {
        [SerializeField] InventoryData _inventoryData;
        [SerializeField] ContainerType _type = ContainerType.Storage;

        InventorySystem _inventory;
        InventoryGridView _inventoryUI;


        public InventoryData InventoryData => _inventoryData;
        public IInventory Inventory => _inventory;
        public InventoryGridView InventoryUI => _inventoryUI;

        private void Awake()
        {
            InitializeInventory();
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

        //[ContextMenu("Add Test Items")]
        //private void AddTestItems()
        //{
        //    // Add null checks to prevent NullReferenceException
        //    if (_inventoryUI == null)
        //    {
        //        Debug.LogWarning("[InventoryManager] Cannot add test items: _inventoryUI is null");
        //        return;
        //    }

        //    if (_testItems == null || _testItems.Length == 0)
        //    {
        //        Debug.Log("[InventoryManager] No test items to add");
        //        return;
        //    }

        //    foreach (var itemDef in _testItems)
        //    {
        //        if (itemDef != null)
        //        {
        //            // Create an item instance
        //            var item = itemDef.CreateItem();

        //            if (item is IItem itemInstance)
        //            {
        //                // Add to inventory at random position or first available
        //                _inventoryUI.AddItem(itemInstance);
        //            }
        //        }
        //    }
        //}
    }
}