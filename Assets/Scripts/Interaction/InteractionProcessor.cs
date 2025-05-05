using System.Collections.Generic;
using UnityEngine;

namespace ParaMoon
{
    /**
     * Processes interactions using the Strategy pattern to handle different interaction types.
     * This static class maintains a registry of interaction strategies and routes interactions
     * to the appropriate handler.
     *
     * Dependencies:
     * - Requires strategy implementations for each interaction type
     *
     * Usage:
     * - Called by PlayerInteractor.TryInteract to process interactions
     * - Add new interaction types by implementing IInteractionStrategy and registering them
     */
    public class InteractionProcessor
    {
        /**
         * Dictionary mapping interaction types to their handling strategies.
         */
        static readonly Dictionary<InteractionType, IInteractionStrategy> _strategies = new()
        {
            { InteractionType.Pickup, new PickupInteractionStrategy() },
            //{ InteractionType.Use, new UseInteractionStrategy() },
            { InteractionType.Open, new OpenInteractionStrategy() },
            //{ InteractionType.Move, new MoveInteractionStrategy() },
            //{ InteractionType.Read, new ReadInteractionStrategy() },
            //{ InteractionType.TalkTo, new TalkToInteractionStrategy() }
        };

        public static bool Process(IInteractor interactor, IInteractable interactable)
        {
            InteractionData data = interactable.GetInteractionData();
            if (_strategies.TryGetValue(data.Type, out IInteractionStrategy strategy))
                return strategy.ExecuteInteraction(interactor, interactable);

            Debug.LogWarning($"No strategy found for interaction type: {data.Type}");
            return false;
        }
    }
}