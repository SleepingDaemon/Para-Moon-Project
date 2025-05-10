namespace ParaMoon
{
    /**
     * Interface defining objects that can be collected in the game world.
     * Any object that should be collectable must implement this interface.
     *
     * Usage:
     * - Implement this interface on any GameObject that should be collectable
     * - Provide a method to collect the item into an inventory
     */
    public interface ICollectable : IInteractable
    {
        /**
         * Collects the item into the specified inventory.
         * 
         * @param inventory The inventory to collect the item into
         * @return True if the item was collected successfully, false otherwise
         */
        bool Collect(IInteractor interactor, IInventory inventory);
    }

    public interface IContainer : IInteractable
    {
        void Open(IInteractor interactor);
        void Close(IInteractor interactor);
    }
}