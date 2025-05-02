using UnityEngine;

namespace ParaMoon
{
    public class NPC : Character, IInteractable
    {
        [SerializeField] InteractionData _interactionData;
        [SerializeField] bool _isHostile = false;

        private void Start()
        {
            // Set highlight type based on hostility
            _highlightType = _isHostile ? HighlightableType.Enemy : HighlightableType.NPC;

            // Set default interaction data if not specified
            if (string.IsNullOrEmpty(_interactionData.PromptText) && !_isHostile)
            {
                _interactionData.PromptText = _isHostile ? "Attack" : "Talk to " + _displayName;
            }


            if (_interactionData.Type == InteractionType.Use)
                _interactionData.Type = _isHostile ? InteractionType.None : InteractionType.TalkTo;
        }

        public bool CanInteract(IInteractor interactor)
        {
            if (_isHostile)
                return false;

            // NPCs can always be interacted with
            return true;
        }

        public override Color GetHighlightColor()
        {
            // If custom color is set, use it
            if (base.GetHighlightColor() != Color.clear)
            {
                return base.GetHighlightColor();
            }

            // Otherwise use default color based on hostility
            return _isHostile ? Color.red : Color.cyan;
        }

        public override string GetHighlightName()
        {
            return _displayName + (_isHostile ? " (Hostile)" : "");
        }

        public override HighlightData[] GetHighlightData()
        {
            float healthPercentage = _healthSystem.GetHealthPercentage(100f);
            Color healthColor = healthPercentage > 66f ? Color.green :
                                healthPercentage > 33f ? Color.yellow : Color.red;

            return new HighlightData[]
            {
                new("Health", $"{_healthSystem.CurrentHealth}/{_healthSystem.MaxHealth}", healthColor),
            };
        }

        public InteractionData GetInteractionData()
        {
            return _interactionData;
        }
    }
}
