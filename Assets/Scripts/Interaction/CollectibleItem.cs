using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace ParaMoon
{
    public class CollectibleItem : HighlightableBase, ICollectable
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

            // Set the interaction type to Pickup
            if (_interactionData.Type != InteractionType.Pickup)
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
            var hasInventoryProvider = interactor.GameObject.GetComponent<IInventoryProvider>() != null;
            if (!hasInventoryProvider)
            {
                Debug.LogWarning($"[CollectibleItem] Interactor does not have an inventory provider: {interactor.GameObject.name}");

                // Log the components on the interactor for debugging
                Debug.Log($"Components on {interactor.GameObject.name}: {string.Join(", ", interactor.GameObject.GetComponents<Component>().Select(c => c.GetType().Name))}");
            }

            // Check if the interactor has an inventory
            return hasInventoryProvider;
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
        public bool Collect(IInteractor interactor, IInventory inventory)
        {
            if (interactor == null || inventory == null)
            {
                Debug.LogError("[CollectibleItem] Null interactor or inventory");
                return false;
            }

            Debug.Log($"[CollectibleItem] Collecting {_item.Name} into inventory of type {inventory.GetType().Name}");

            // Create the item
            var item = _item.CreateItem(_quantity);

            // Use the provided inventory directly
            bool added = inventory.TryAddItem(item, out _);

            if (added)
            {
                Debug.Log($"Successfully collected {_quantity}x {_item.Name}");
                // Show feedback notification
                // TODO: NotificationManager.Instance.ShowNofifcation($"Picked up {_quantity}x {_item.Name}");

                // Remove item from the world
                gameObject.SetActive(false);
                return true;
            }
            else
            {
                // NotificationManager.Instance.ShowNotification("Inventory is full");
                Debug.LogWarning($"[CollectibleItem] Failed to add {_item.Name} - inventory full or invalid position");
                return false;
            }
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