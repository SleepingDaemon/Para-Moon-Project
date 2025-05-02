using UnityEngine;

namespace ParaMoon
{
    /**
     * Implements player interaction capabilities by detecting interactable objects,
     * showing appropriate UI prompts, and processing interactions.
     *
     * Dependencies:
     * - Requires InteractionUIController component
     * - Uses InteractionDetector for object detection
     * - Uses AudioManager for playing interaction sounds
     *
     * Usage:
     * - Attach to player GameObject
     * - Configure camera reference and interaction settings
     * - Works with InteractionUIController to show interaction prompts
     */
    public class PlayerInteractor : MonoBehaviour, IInteractor
    {
        [SerializeField] Transform _cameraTransform;
        [SerializeField] float _maxInteractionDistance = 2.5f;
        [SerializeField] float _maxHighlightDistance = 50f;
        [SerializeField] LayerMask _interactableMask;

        [SerializeField] ReticleUIController _reticleUIController;

        InteractionUIController _interactionUIController;
        InteractionDetector _detector;

        public GameObject GameObject => gameObject;
        public Transform InteractionSource => _cameraTransform;

        #region Unity Methods

        private void Start()
        {
            if (_interactionUIController == null)
            {
                if (ServiceLocator.Instance.TryGetService<UIManager>(out var uiManager))
                {
                    _interactionUIController = uiManager.GetInteractionUIController();
                }
                else
                {
                    ServiceLocator.Instance.WhenAvailable<UIManager>(uiManager =>
                    {
                        _interactionUIController = uiManager.GetInteractionUIController();
                    });
                }
            }

            _detector = new InteractionDetector(_cameraTransform, _maxInteractionDistance, _maxHighlightDistance, _interactableMask);
        }

        private void Update()
        {
            if (_interactionUIController == null)
                return;

            IInteractable interactable = _detector.GetInteractableInView();

            // Handle UI updates
            if (interactable != null && interactable.CanInteract(this))
            {
                _interactionUIController.ShowInteractionPrompt(interactable.GetInteractionData());

                if (ServiceLocator.Instance.TryGetService<InputManager>(out var inputManager) &&
                    inputManager.Interact)
                {
                    InteractionData data = interactable.GetInteractionData();
                    Debug.LogFormat("<color=green>[INTERACTION]</color> Interacting with: {0} (Type: {1})",
                        data.PromptText, data.Type);

                    // Update reticle if available
                    if (_reticleUIController != null)
                        _reticleUIController.UpdateReticlePosition(0.1f); // Small feedback shift

                    TryInteract(interactable);
                }
            }
            else
                _interactionUIController.HideInteractionPrompt();
        }

        #endregion

        /**
         * Attempts to interact with the specified interactable object.
         * 
         * @param interactable The object to interact with
         * @return True if the interaction was successful, false otherwise
         */
        public bool TryInteract(IInteractable interactable)
        {
            if (interactable.CanInteract(this))
            {
                Debug.Log("Interacting with: " + interactable.GetInteractionData().PromptText);

                // Execute the interaction strategy based on the type
                InteractionData data = interactable.GetInteractionData();

                // Play interaction sound
                // TODO: Implement sound playing logic

                // Execute the interaction via an interaction processor
                return InteractionProcessor.Process(this, interactable);
            }

            return false;
        }
    }
}