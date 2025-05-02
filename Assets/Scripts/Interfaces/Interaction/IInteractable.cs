namespace ParaMoon
{
    /**
    * Interface defining objects that can be interacted with in the game world.
    * Any object that should be interactable must implement this interface.
    *
    * Usage:
    * - Implement this interface on any GameObject that should respond to player interaction
    * - Return interaction data when requested
    * - Determine if interaction is possible with a specific interactor
    */
    public interface IInteractable
    {
        /**
         * Gets interaction data for UI prompts and processing.
         * 
         * @return Data containing prompt text, icon, sound, and interaction type
         */
        InteractionData GetInteractionData();

        /**
         * Determines if this object can be interacted with by the specified interactor.
         * 
         * @param interactor The entity attempting to interact with this object
         * @return True if interaction is possible, false otherwise
         */
        bool CanInteract(IInteractor interactor);
    }
}