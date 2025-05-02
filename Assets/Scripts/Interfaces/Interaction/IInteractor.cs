using UnityEngine;

namespace ParaMoon
{
    public interface IInteractor
    {
        /**
         * The GameObject that is performing the interaction.
         */
        GameObject GameObject { get; }

        /**
         * The Transform from which interaction originates (usually camera transform).
         * Used for raycasting and interaction distance checks.
         */
        Transform InteractionSource { get; }

        /**
         * Attempts to interact with the specified interactable object.
         * 
         * @param interactable The object to interact with
         * @return True if the interaction was successful, false otherwise
         */
        bool TryInteract(IInteractable interactable);
    }
}