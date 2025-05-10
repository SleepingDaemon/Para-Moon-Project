using System;
using Unity.VisualScripting;
using UnityEngine;

namespace ParaMoon
{
    public class InventoryManager : HighlightableBase, IInventoryProvider, IContainer
    {
        [SerializeField] InventoryData _inventoryData;
        [SerializeField] InteractionData _interactionData;

        InventoryUIController _inventoryUIController;
        InventorySystem _inventory;
        InventoryGridView _inventoryUI;


        public InventoryData InventoryData => _inventoryData;
        public IInventory Inventory => _inventory;
        public InventoryGridView InventoryUI => _inventoryUI;

        private void Awake()
        {
            InitializeInventory();
            ConnectToUIController();
        }

        private void ConnectToUIController()
        {
            DI.WhenAvailable<UIManager>(ui =>
            {
                _inventoryUIController = ui.GetInventoryUIController();

                if (_inventoryUIController != null)
                {
                    _inventoryUIController.Initialize(_inventory, null);
                }
                else
                {
                    Debug.LogWarning("[InventoryManager] Inventory UI Controller not available!");
                }
            });
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

        private void Start()
        {
            if (string.IsNullOrEmpty(_interactionData.PromptText))
                _interactionData.PromptText = $"Open {_displayName}";

            if (_interactionData.Type != InteractionType.Open)
                _interactionData.Type = InteractionType.Open;

            if (string.IsNullOrEmpty(_displayName) && _inventoryData != null)
                _displayName = _inventoryData.name;
        }

        public bool CanInteract(IInteractor interactor)
        {
            return interactor != null && interactor.GameObject.GetComponent<PlayerInventory>() != null;
        }

        public InteractionData GetInteractionData()
        {
            return _interactionData;
        }

        public void Open(IInteractor interactor)
        {
            // Get the interactor
            if (interactor == null)
                Debug.LogError($"[InventoryManager] Interactor/EntityInventory is null");

            UIManager ui = DI.Get<UIManager>();
            ui.OpenContainerUI(_inventory);
        }

        public void Close(IInteractor interactor)
        {
            UIManager ui = DI.Get<UIManager>();
            ui.CloseContainerUI();
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