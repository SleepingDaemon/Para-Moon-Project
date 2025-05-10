using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ParaMoon
{
    /// <summary>
    /// Singleton class to manage player input using Unity's new Input System.
    /// </summary>
    [Injectable]
    [SceneExported("InputManager")]
    public class InputManager : ServiceBehaviour<InputManager>
    {
        public event Action OnMenuToggleRequested;

        [Header("Input Settings")]
        [SerializeField] InputActionAsset _playerControls;

        [Header("Action Map Name References")]
        [SerializeField] string _playerActionMapName = "Player";
        [SerializeField] string _uiActionMapName = "UI";

        [Header("Action Name References")]
        [SerializeField] string _look = "Look";
        [SerializeField] string _move = "Move";
        [SerializeField] string _jump = "Jump";
        [SerializeField] string _walk = "Walk";
        [SerializeField] string _sprint = "Sprint";
        [SerializeField] string _crouch = "Crouch";
        [SerializeField] string _interact = "Interact";
        [SerializeField] string _menu = "Menu";

        InputActionMap _currentMap;
        InputActionMap _playerMap;
        InputActionMap _uiMap;

        InputAction _lookAction;
        InputAction _moveAction;
        InputAction _jumpAction;
        InputAction _walkAction;
        InputAction _sprintAction;
        InputAction _crouchAction;
        InputAction _interactAction;
        InputAction _playerEROSAction;
        InputAction _uiEROSAction;

        public Vector2 Look { get; private set; }
        public Vector2 Move { get; private set; }
        public bool Jump { get; private set; }
        public bool Walk { get; private set; }
        public bool Sprint { get; private set; }
        public bool Crouch { get; private set; }
        public bool Interact { get; private set; }
        public bool IsUIMode { get; private set; }

        public event Action OnToggleMenu;

        private void OnEnable()
        {
            if (_currentMap != null)
                _currentMap.Enable();
        }

        private void OnDisable()
        {
            _playerMap.Disable();
            _uiMap.Disable();
        }

        protected override void Awake()
        {
            base.Awake();

            // Initialize action maps
            _playerMap = _playerControls.FindActionMap(_playerActionMapName);
            _uiMap = _playerControls.FindActionMap(_uiActionMapName);
            _currentMap = _playerMap;

            _lookAction = _currentMap.FindAction(_look);
            _moveAction = _currentMap.FindAction(_move);
            _jumpAction = _currentMap.FindAction(_jump);
            _walkAction = _currentMap.FindAction(_walk);
            _sprintAction = _currentMap.FindAction(_sprint);
            _crouchAction = _currentMap.FindAction(_crouch);
            _interactAction = _currentMap.FindAction(_interact);
            _playerEROSAction = _currentMap.FindAction(_menu);

            // Initialize UI actions
            _playerEROSAction = _playerMap.FindAction(_menu);
            _uiEROSAction = _uiMap.FindAction(_menu);

            RegisterInputActions();
            EnablePlayerActionMap();
        }

        private void RegisterInputActions()
        {
            _lookAction.performed += ctx => Look = ctx.ReadValue<Vector2>();
            _lookAction.canceled += ctx => Look = Vector2.zero;
            _moveAction.performed += ctx => Move = ctx.ReadValue<Vector2>();
            _moveAction.canceled += ctx => Move = Vector2.zero;
            _jumpAction.performed += ctx => Jump = ctx.ReadValueAsButton();
            _jumpAction.canceled += ctx => Jump = false;
            _walkAction.performed += ctx => Walk = ctx.ReadValueAsButton();
            _walkAction.canceled += ctx => Walk = false;
            _sprintAction.performed += ctx => Sprint = ctx.ReadValueAsButton();
            _sprintAction.canceled += ctx => Sprint = false;
            _crouchAction.performed += ctx => Crouch = ctx.ReadValueAsButton();
            _crouchAction.canceled += ctx => Crouch = false;
            _interactAction.performed += ctx =>
            {
                Interact = ctx.ReadValueAsButton();
                Debug.Log("Interact key pressed: " + Interact);
            };
            _interactAction.canceled += ctx => Interact = false;

            // Simplified menu toggle handling
            if (_playerEROSAction != null)
            {
                _playerEROSAction.performed += ctx => OnMenuToggleRequested?.Invoke();
            }

            if (_uiEROSAction != null)
            {
                _uiEROSAction.performed += ctx => OnMenuToggleRequested?.Invoke();
            }
        }

        /// <summary>
        /// Toggles between player control mode and UI mode
        /// </summary>
        public void ToggleUIMode()
        {
            IsUIMode = !IsUIMode;

            if (IsUIMode)
                EnableUIActionMap();
            else
                EnablePlayerActionMap();
        }

        /// <summary>
        /// Switches input mode based on the target UI state
        /// </summary>
        public void SetInputMode(bool uiMode)
        {
            if (uiMode && !IsUIMode)
                EnableUIActionMap();
            else if (!uiMode && IsUIMode)
                EnablePlayerActionMap();
        }

        /// <summary>
        /// Enables the Player action map and disables the UI action map
        /// </summary>
        public void EnablePlayerActionMap()
        {
            _uiMap.Disable();
            _playerMap.Enable();
            _currentMap = _playerMap;
            IsUIMode = false;
        }

        /// <summary>
        /// Enables the UI action map and disables the Player action map
        /// </summary>
        public void EnableUIActionMap()
        {
            _playerMap.Disable();
            _uiMap.Enable();
            _currentMap = _uiMap;
            IsUIMode = true;
        }
    }
}
