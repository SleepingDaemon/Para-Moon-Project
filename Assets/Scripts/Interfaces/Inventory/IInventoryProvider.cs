namespace ParaMoon
{
    /**
     * Interface defining a provider for an inventory system.
     * Any class implementing this interface should provide access to an inventory instance.
     *
     * Usage:
     * - Implement this interface in your inventory provider class
     * - Use the Inventory property to access the inventory instance
     */
    public interface IInventoryProvider
    {
        IInventory Inventory { get; }
    }
}