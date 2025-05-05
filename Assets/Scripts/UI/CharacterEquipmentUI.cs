using System.Collections.Generic;
using UnityEngine;

namespace ParaMoon
{
    public class CharacterEquipmentUI : MonoBehaviour
    {
        [Header("Armor UI")]
        [SerializeField] private RectTransform _armorWindow;
        [SerializeField] private GameObject _armorSlotPrefab;
        [SerializeField] private List<Sprite> _armorSlotIcons = new();
        
        [Header("Implant UI")]
        [SerializeField] private RectTransform _implantWindow;
        [SerializeField] private GameObject _implantSlotPrefab;
        [SerializeField] private List<Sprite> _implantSlotIcons = new();
        
        [Header("References")]
        [SerializeField] private GameObject _itemPrefab;
        [SerializeField] private CharacterEquipmentManager _equipmentManager;
        
        private InventoryGridUI _armorGridUI;
        private InventoryGridUI _implantGridUI;
        
        private void Start()
        {
            if (_equipmentManager == null)
            {
                _equipmentManager = GetComponent<CharacterEquipmentManager>();
            }
            
            if (_equipmentManager == null)
            {
                Debug.LogError("CharacterEquipmentManager reference is missing!");
                return;
            }
            
            InitializeArmorUI();
            //InitializeImplantUI();
        }
        
        private void InitializeArmorUI()
        {
            GetArmorUI();

            _armorGridUI.UseSpecializedSlots = true;

            // Set up references
            _armorGridUI.SetInventory(_equipmentManager.GetArmorInventory());

            // Create slots for each armor type
            int index = 0;
            foreach (ArmorSlot slotType in System.Enum.GetValues(typeof(ArmorSlot)))
            {
                // Create slot
                GameObject slotObj = Instantiate(_armorSlotPrefab, _armorGridUI.GridContainer);
                SpecializedSlotUI slotUI = slotObj.GetComponent<SpecializedSlotUI>();

                // Get icon for this slot type (if available)
                Sprite icon = index < _armorSlotIcons.Count ? _armorSlotIcons[index] : null;

                // Name the slot object
                slotObj.name = slotType.ToString() + "_Slot";

                // Initialize slot
                Vector2Int slotPosition = new Vector2Int(0, index); // Vertical layout
                slotUI.Initialize(slotPosition, _armorGridUI, ItemType.Armor, slotType, icon, slotType.ToString());

                // Register the slot with the grid UI
                _armorGridUI.RegisterSpecializedSlot(slotUI, slotPosition);

                index++;
            }
        }

        private void GetArmorUI()
        {
            if (ServiceLocator.Instance.TryGetService(out UIManager uiManager))
            {
                _armorGridUI = uiManager.GetArmorInventoryUI();

                if (_armorGridUI == null)
                {
                    ServiceLocator.Instance.WhenAvailable<UIManager>(uiManager =>
                    {
                        _armorGridUI = uiManager.GetArmorInventoryUI();
                    });
                }
            }

            if (_armorGridUI == null)
            {
                Debug.LogError("ArmorGridUI reference is missing!");
                return;
            }
        }

        private void InitializeImplantUI()
        {
            // Create implant grid UI component
            _implantGridUI = _implantWindow.gameObject.AddComponent<InventoryGridUI>();
            
            // Set up references
            _implantGridUI.SetInventory(_equipmentManager.GetImplantInventory());
            
            // Create slots for each implant type
            int index = 0;
            foreach(ImplantSlot slotType in System.Enum.GetValues(typeof(ImplantSlot)))
            {
                // Create slot
                GameObject slotObj = Instantiate(_implantSlotPrefab, _implantWindow);
                SpecializedSlotUI slotUI = slotObj.GetComponent<SpecializedSlotUI>();
                
                // Get icon for this slot type (if available)
                Sprite icon = index < _implantSlotIcons.Count ? _implantSlotIcons[index] : null;
                
                // Initialize slot
                Vector2Int slotPosition = new Vector2Int(0, index);
                slotUI.Initialize(slotPosition, _implantGridUI, ItemType.Implant, slotType, icon, slotType.ToString());
                
                index++;
            }
        }
    }
}
