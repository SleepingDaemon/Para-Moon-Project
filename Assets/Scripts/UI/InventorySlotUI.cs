using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ParaMoon
{
    public class InventorySlotUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] Image _background;

        Vector2Int _gridPosition;
        InventoryGridUI _parentInventory;
        Color _normalColor = Color.white;

        public Vector2Int GridPosition => _gridPosition;

        public void Initialize(Vector2Int position, InventoryGridUI inventory)
        {
            _gridPosition = position;
            _parentInventory = inventory;

            if (_background != null)
                _normalColor = _background.color;

            name = $"Slot [{position.x}, {position.y}]";
        }

        public void SetHighlight(bool highlight, Color? color = null)
        {
            if (_background != null)
                _background.color = highlight ? (color ?? Color.yellow) : _normalColor;
        }

        public void OnDrop(PointerEventData eventData)
        {
            // actual drop logic is handled in InventoryGridUI
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetHighlight(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHighlight(false);
        }
    }
}
