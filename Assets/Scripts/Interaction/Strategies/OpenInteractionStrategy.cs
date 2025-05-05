namespace ParaMoon
{
    /// <summary>
    /// Strategy for handling open interactions in the game.
    /// </summary>
    public class OpenInteractionStrategy : IInteractionStrategy
    {
        public bool ExecuteInteraction(IInteractor interactor, IInteractable interactable)
        {
            if (interactable is IContainer container)
            {
                container.Open(interactor);
                return true;
            }

            return false;
        }
    }
}