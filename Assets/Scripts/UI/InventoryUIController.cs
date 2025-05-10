using UnityEngine;

namespace ParaMoon
{
    [SceneExported("GameUI")]
    public class InventoryUIController : MonoBehaviour
    {
        [SerializeField] private InventoryGridView _playerInventoryView;
        [SerializeField] private InventoryGridView _containerInventoryView;
        [SerializeField] private SpecializedInventoryView _armorInventoryView;

        [Header("UI Elements")]
        [SerializeField] private GameObject _containerWindow;
        [SerializeField] private TMPro.TMP_Text _containerTitle;

        [Inject] UIManager _uiManager;
        InventoryService _inventoryService;
        EquipmentService _equipmentService;

        private void Awake()
        {
            ServiceLocator.Instance.WhenAvailable<UIManager>(ui =>
            {
                _uiManager = ui;
                _uiManager.SetInventoryUIController(this);
                Debug.Log("[InventoryUIController] Registered with UIManager");
            });

            _inventoryService = InventoryService.Instance;
            if (_containerWindow != null)
                _containerWindow.SetActive(false);
        }

        /// <summary>
        /// Initialize with player inventory
        /// </summary>
        public void Initialize(IInventory playerInventory, EquipmentService equipmentService)
        {
            _playerInventoryView.Initialize(playerInventory);
            _equipmentService = equipmentService;

            if (_equipmentService != null && _armorInventoryView != null)
            {
                _armorInventoryView.Initialize(_equipmentService.GetArmorInventory());
            }

            ServiceLocator.Instance.WhenAvailable<UIManager>(ui =>
            {
                _uiManager = ui;
                _uiManager.SetInventoryUIController(this);
            });
        }

        /// <summary>
        /// Open a container inventory UI
        /// </summary>
        public void OpenContainerUI(IInventory containerInventory)
        {
            _containerInventoryView.Initialize(containerInventory);

            MenuManager menuManager = GameObject.FindFirstObjectByType<MenuManager>();
            if (menuManager != null && !_playerInventoryView.gameObject.activeSelf)
                menuManager.ToggleWindowByReference(_playerInventoryView.gameObject, true, true);

            //if (_containerTitle != null)
            //    _containerTitle.text = containerName;

            // Show the container window
            if (_containerInventoryView != null)
            {
                _containerInventoryView.gameObject.SetActive(true);
                _containerInventoryView.gameObject.transform.SetAsLastSibling();
            }
        }

        /// <summary>
        /// Close the container UI
        /// </summary>
        public void CloseContainerUI()
        {
            if (_containerWindow != null)
                _containerWindow.SetActive(false);

            _containerInventoryView.Initialize(null);
        }

        public GameObject GetContainerWindow()
        {
            return _containerWindow;
        }

        /// <summary>
        /// Handle item transfer between inventories
        /// </summary>
        public void HandleItemTransfer(InventoryItemView itemView, InventoryGridView targetView)
        {
            if (itemView == null || targetView == null ||
                itemView.ParentView == null || targetView.Inventory == null)
                return;

            IInventory sourceInventory = itemView.ParentView.Inventory;
            IInventory targetInventory = targetView.Inventory;
            IItem item = itemView.Item;

            // Handle specialized slot transfers
            if (itemView.ParentView is SpecializedInventoryView)
            {
                _inventoryService.TransferFromSpecializedSlot(
                    sourceInventory, targetInventory, item, itemView.GridPosition);
            }
            else
            {
                _inventoryService.TransferItem(sourceInventory, targetInventory, item);
            }
        }
    }
}
