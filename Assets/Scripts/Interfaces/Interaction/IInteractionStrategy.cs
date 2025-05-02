namespace ParaMoon
{
    /**
     * Interface defining the strategy for executing interactions between an interactor and an interactable.
     * This allows for different types of interactions (e.g., pickup, use) to be handled in a modular way.
     *
     * Usage:
     * - Implement this interface to define custom interaction behavior
     * - Use the ExecuteInteraction method to perform the interaction logic
     */
    public interface IInteractionStrategy
    {
        /**
         * Executes an interaction between an interactor and interactable.
         * 
         * @param interactor The entity initiating the interaction
         * @param interactable The object being interacted with
         * @return True if interaction was successful, false otherwise
         */
        bool ExecuteInteraction(IInteractor interactor, IInteractable interactable);
    }
}