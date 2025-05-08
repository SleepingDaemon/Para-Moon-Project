using TMPro;
using UnityEngine;

namespace ParaMoon
{
    public class ContainerInteractionUI : MonoBehaviour
    {
        [Header("Inventory Windows")]
        [SerializeField] RectTransform _containerPanel;
        [SerializeField] RectTransform _playerContainerPanel;
        [SerializeField] TMP_Text _containerTitle;

        [SerializeField] InventoryGridView _containerInventoryUI;

        InventoryGridView _playerInventoryUI;
        InventoryManager _containerInventory;
        InventoryManager _playerInventory;

        private void Awake()
        {
            _containerPanel.gameObject.SetActive(false);
        }

        public void Initialize(InventoryManager containerInventory, InventoryManager playerInventory, string containerName = "Container")
        {
            _containerInventory = containerInventory;
            _playerInventory = playerInventory;

            if (_playerInventoryUI == null)
                _playerInventoryUI = playerInventory.InventoryUI;

            // Set titles
            if (_containerTitle != null)
                _containerTitle.text = containerName;

            // Connect the inventory to their UI
            _containerInventoryUI.Initialize(containerInventory.Inventory);

            // Toggle the player inventory window since it's part of the Menu
            MenuManager menuManager = GameObject.FindFirstObjectByType<MenuManager>();
            if (menuManager != null && !_playerContainerPanel.gameObject.activeSelf)
                menuManager.ToggleWindowByReference(_playerContainerPanel.gameObject, true, true);

            // Show the container window
            if (_containerPanel != null)
            {
                _containerPanel.gameObject.SetActive(true);
                _containerPanel.SetAsLastSibling();
            }

            Debug.Log($"[ContainerInteractionUI] Container UI initialized with container:{containerInventory.name}, player:{playerInventory.name}");
        }
    }
}
