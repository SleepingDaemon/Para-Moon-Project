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

        [Inject] UIManager _uiManager;
        [Inject] InputManager _inputManager;
        InteractionUIController _interactionUIController;
        InteractionDetector _detector;

        public GameObject GameObject => gameObject;
        public Transform InteractionSource => _cameraTransform;

        #region Unity Methods

        private void Awake()
        {
            //if (gameObject.GetComponent<MonoBehaviourInjector>() == null)
            //{
            //    gameObject.AddComponent<MonoBehaviourInjector>();
            //}

            ServiceLocator.Instance.WhenAvailable<UIManager>(ui =>
            {
                _uiManager = ui;
            });

            ServiceLocator.Instance.WhenAvailable<InputManager>(input =>
            {
                _inputManager = input;
            });
        }

        private void Start()
        {
            if (_uiManager == null)
            {
                Debug.LogError("[PlayerInteractor] UIManager is not available.");
                return;
            }

            // Get InteractionUIController if we don't have it yet
            if (_interactionUIController == null)
            {
                _interactionUIController = _uiManager.GetInteractionUIController();
                if (_interactionUIController == null)
                {
                    Debug.LogError("[PlayerInteractor] InteractionUIController not found in UIManager.");
                    return;
                }
            }

            _detector = new InteractionDetector(_cameraTransform, _maxInteractionDistance, _maxHighlightDistance, _interactableMask);
        }

        private void Update()
        {
            // Check that we have all dependencies before proceeding
            if (_interactionUIController == null || _inputManager == null || _detector == null)
            {
                return;
            }

            IInteractable interactable = _detector.GetInteractableInView();

            // Handle UI updates
            if (interactable != null && interactable.CanInteract(this))
            {
                _interactionUIController.ShowInteractionPrompt(interactable.GetInteractionData());

                if (_inputManager.Interact)
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
            {
                _interactionUIController.HideInteractionPrompt();
            }
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