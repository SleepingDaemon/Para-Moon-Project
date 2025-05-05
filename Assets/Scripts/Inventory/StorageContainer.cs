using System;
using UnityEngine;

namespace ParaMoon
{
    public class StorageContainer : HighlightableBase, IContainer
    {
        [SerializeField] InteractionData _interactionData;

        InventoryManager _containerInventory;

        private void Awake()
        {
            InitializeInventory();
        }

        private void Start()
        {
            if (string.IsNullOrEmpty(_interactionData.PromptText))
                _interactionData.PromptText = $"Open {_displayName}";

            if (_interactionData.Type != InteractionType.Open)
                _interactionData.Type = InteractionType.Open;

            if (string.IsNullOrEmpty(_displayName) && _containerInventory.InventoryData != null)
                _displayName = _containerInventory.InventoryData.name;
        }

        public bool CanInteract(IInteractor interactor)
        {
            return interactor != null && interactor.GameObject.GetComponent<InventoryManager>().InventoryUI.Inventory != null;
        }

        public InteractionData GetInteractionData()
        {
            return _interactionData;
        }
        private void InitializeInventory()
        {
            _containerInventory = GetComponent<InventoryManager>();
            if (_containerInventory == null)
                _containerInventory = gameObject.AddComponent<InventoryManager>();

            if (_containerInventory.InventoryData == null)
            {
                Debug.LogError($"[StorageContainer] InventoryData is not assigned for {gameObject.name}");
                return;
            }
        }

        public void Open(IInteractor interactor)
        {
            // Get the interactor
            if (!interactor.GameObject.TryGetComponent<InventoryManager>(out var playerInventory))
                Debug.LogError($"[StorageContainer] Interactor/EntityInventory is null");

            if (ServiceLocator.Instance.TryGetService<UIManager>(out var uiManager))
                uiManager.OpenContainerUI(_containerInventory, playerInventory, _displayName);
        }

        public void Close()
        {
            // Close the container UI
            if (ServiceLocator.Instance.TryGetService<UIManager>(out var uiManager))
                uiManager.CloseContainerUI();
        }
    }
}