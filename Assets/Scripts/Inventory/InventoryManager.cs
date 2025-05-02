using UnityEngine;

namespace ParaMoon
{
    /* 
     * InventoryManager is responsible for managing the inventory system,
     * including adding, removing, and querying items.
     * It interacts with the GridInventoryUI to display items in the UI.
     */
    public class InventoryManager : MonoBehaviour
    {
        [Header("Inventory Configuration")]
        [SerializeField] private InventoryData _inventoryDefinition;
        [SerializeField] private InventoryGridUI _inventoryUI;

        [Header("Test Items")]
        [SerializeField] private ItemData[] _testItems;

        UIManager _uiManager;

        private void Start()
        {
            if (_inventoryUI == null)
                _inventoryUI = GameObject.FindFirstObjectByType<InventoryGridUI>();

            if (_inventoryUI == null && ServiceLocator.Instance.TryGetService<UIManager>(out var uiManager))
            {
                // Try to get the inventory UI from the UIManager
                _inventoryUI = uiManager.GetInventoryUI();
            }

            // IF still not available, wait for the UIManager
            if (_inventoryUI == null)
            {
                Debug.Log("[InventoryManager] Waiting for UIManager to initialize...");
                ServiceLocator.Instance.WhenAvailable<UIManager>(uiManager =>
                {
                    _inventoryUI = uiManager.GetInventoryUI();
                    if (_inventoryUI != null)
                        AddTestItems();
                    else
                        Debug.LogError("[InventoryManager] UIManager available but GetInventoryUI returned null");
                });
            }
            else
            {
                // If inventory UI is already available, add test items
                AddTestItems();
            }
        }

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

                    // Add to inventory at random position or first available
                    _inventoryUI.AddItem(item);
                }
            }
        }

        // Public methods to interact with inventory from other systems

        public bool AddItem(ItemData data, int count = 1)
        {
            if (data == null)
                return false;

            var item = data.CreateItem(count);
            return _inventoryUI.AddItem(item);
        }

        public bool RemoveItem(string itemId, int count = 1)
        {
            // Find the item in inventory
            var inventory = _inventoryUI.Inventory;

            foreach (var (item, position) in _inventoryUI.Inventory.GetAllItems())
            {
                if (item.ID == itemId)
                {
                    // If we need to remove the whole stack or what's left of it
                    if (count >= item.CurrentStackSize)
                    {
                        return inventory.TryRemoveItem(position, out _);
                    }
                    else
                    {
                        // Reduce stack size
                        item.CurrentStackSize -= count;
                        return true;
                    }
                }
            }

            return false;
        }

        public int GetItemCount(string itemId)
        {
            int count = 0;

            foreach (var (item, _) in _inventoryUI.Inventory.GetAllItems())
            {
                if (item.ID == itemId)
                {
                    count += item.CurrentStackSize;
                }
            }

            return count;
        }
    }
}