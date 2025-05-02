using UnityEngine;

namespace ParaMoon
{
    /**
     * Strategy for handling pickup interactions in the game.
     * This strategy allows the player to collect items from the game world.
     *
     * Usage:
     * - Implement this strategy in your interaction system
     * - Use it to handle item pickups when the player interacts with collectable objects
     */
    public class PickupInteractionStrategy : IInteractionStrategy
    {
        public bool ExecuteInteraction(IInteractor interactor, IInteractable interactable)
        {
            if (interactable is ICollectable collectable)
            {
                // Try to add to inventory
                IInventoryProvider inventoryProvider = interactor.GameObject.GetComponent<IInventoryProvider>();

                if (inventoryProvider != null)
                    return collectable.Collect(inventoryProvider.Inventory);
                else
                    Debug.LogWarning($"Interactor {interactor.GameObject.name} does not have an inventory provider.");
            }

            return false;
        }
    }
}