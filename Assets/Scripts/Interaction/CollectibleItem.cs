using UnityEngine;

namespace ParaMoon
{
    public class CollectibleItem : HighlightableBase, IInteractable, ICollectable
    {
        [SerializeField] ItemData _item;
        [SerializeField] int _quantity = 1;
        [SerializeField] InteractionData _interactionData;
        [SerializeField] private bool _showQuantityInHighlight = true;

        #region Unity Methods

        private void Start()
        {
            // Ensure the interaction data is set up correctly
            if (string.IsNullOrEmpty(_interactionData.PromptText))
                _interactionData.PromptText = $"Pick up {_item.Name}";

            // Set the interaction type to Pickup if it's not already set to Use.
            // This ensures that the item is collected when interacted with.
            if (_interactionData.Type == InteractionType.Use)
                _interactionData.Type = InteractionType.Pickup;

            // Set display name from the item if not specified
            if (string.IsNullOrEmpty(_displayName) && _item != null)
            {
                _displayName = _item.Name;
            }

            _highlightType = HighlightableType.Item;
        }

        #endregion

        /**
         * Checks if the interactor can collect this item.
         */
        public bool CanInteract(IInteractor interactor)
        {
            // Check if the interactor has an inventory
            return interactor.GameObject.GetComponent<IInventoryProvider>() != null;
        }

        /**
         * Gets interaction data for the collectible.
         */
        public InteractionData GetInteractionData()
        {
            return _interactionData;
        }

        /**
         * Collects the item into the specified inventory.
         * 
         * @param inventory The inventory to collect the item into
         * @return True if the item was collected successfully, false otherwise
         */
        public bool Collect(IInventory inventory)
        {
            //if (inventory.TryAddItem(_item, _quantity))
            //{
            //    // Show feedback notification
            //    // TODO: NotificationManager.Instance.ShowNofifcation($"Picked up {_quantity}x {_item.Name}");

            //    // Remove item from the world
            //    gameObject.SetActive(false);
            //    return true;
            //}
            //else
            //{
            //    // NotificationManager.Instance.ShowNotification("Inventory is full");
            //    return false;
            //}

            return false; // Placeholder for actual inventory logic
        }

        /**
         * Override to provide item-specific display name.
         */
        public override string GetHighlightName()
        {
            if (_item != null)
            {
                string name = _item.Name;
                // Only show quantity in name if we're not showing it as data
                if (_quantity > 1 && !_showQuantityInHighlight)
                {
                    name += $" ({_quantity})";
                }
                return name;
            }

            return base.GetHighlightName();
        }

        /**
         * Override to provide item quantity as highlight data.
         */
        public override HighlightData[] GetHighlightData()
        {
            if (!_showQuantityInHighlight || _quantity <= 1 || _item == null)
            {
                return base.GetHighlightData();
            }

            // Show item quantity as separate data
            return new[] 
            {
                new HighlightData("Quantity", _quantity.ToString())
            };
        }
    }
}