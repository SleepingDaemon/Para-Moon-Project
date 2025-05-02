using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ParaMoon
{
    public class InventoryItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [SerializeField] Image _itemIcon;
        [SerializeField] TMP_Text _itemName;
        [SerializeField] TMP_Text _stackCountText;

        IItem _item;
        Vector2Int _gridPosition;
        InventoryGridUI _parentInventory;
        Canvas _canvas;
        CanvasGroup _canvasGroup;
        RectTransform _rectTransform;
        Vector3 _startPosition;

        public IItem Item => _item;
        public Vector2Int GridPosition => _gridPosition;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();

            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            _canvas = GetComponentInParent<Canvas>();
        }

        public void Initialize(IItem inventoryItem, Vector2Int position, InventoryGridUI inventory)
        {
            _item = inventoryItem;
            _gridPosition = position;
            _parentInventory = inventory;

            // Set up visual elements
            if (_itemIcon != null)
            {
                _itemIcon.sprite = _item.Icon;
                _itemIcon.preserveAspect = true;
                _itemIcon.raycastTarget = true;

                // Make icon fill the item container
                //RectTransform iconRect = _itemIcon.GetComponent<RectTransform>();
                //iconRect.anchorMin = Vector2.zero;
                //iconRect.anchorMax = Vector2.one;
                //iconRect.offsetMin = Vector2.zero;
                //iconRect.offsetMax = Vector2.zero;
            }

            // Ensure the root GameObject can receive raycasts
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = true;
            }

            if (_itemName != null)
                _itemName.text = _item.Name;

            UpdateStackCount();
        }

        public void UpdateStackCount()
        {
            if (_stackCountText != null)
            {
                _stackCountText.gameObject.SetActive(_item.IsStackable && _item.CurrentStackSize > 1);
                _stackCountText.text = _item.CurrentStackSize.ToString();
            }
        }

        public void SetGridPosition(Vector2Int toPosition)
        {
            _gridPosition = toPosition;
        }

        // Drag and drop functionality
        public void OnBeginDrag(PointerEventData eventData)
        {
            _startPosition = transform.position;

            // Make the item semi-transparent and pass through raycasts
            _canvasGroup.alpha = 0.6f;
            _canvasGroup.blocksRaycasts = false;

            // Bring to front
            transform.SetAsLastSibling();

            // Notify the inventory UI
            _parentInventory.BeginItemDrag(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Move the item with the cursor
            transform.position = eventData.position;

            // Notify the inventory UI for highlighting
            _parentInventory.DragItem(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // Restore opacity and raycast blocking
            _canvasGroup.alpha = 1.0f;
            _canvasGroup.blocksRaycasts = true;

            // Notify the inventory UI
            _parentInventory.EndItemDrag(eventData.position);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                // TODO: Right-click functionality (e.g., show context menu, use item, etc.)
                Debug.Log($"Right-clicked on item: {_item.Name}");
            }
        }
    }
}
