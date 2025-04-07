using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ParaMoon
{
    /// <summary>
    /// Singleton class to manage player input using Unity's new Input System.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] InputActionAsset _playerControls;

        [Header("Action Map Name References")]
        [SerializeField] string _actionMapName = "Player";

        [Header("Action Name References")]
        [SerializeField] string _look = "Look";
        [SerializeField] string _move = "Move";
        [SerializeField] string _jump = "Jump";
        [SerializeField] string _walk = "Walk";
        [SerializeField] string _sprint = "Sprint";

        InputActionMap _currentMap;
        InputAction _lookAction;
        InputAction _moveAction;
        InputAction _jumpAction;
        InputAction _walkAction;
        InputAction _sprintAction;

        public Vector2 Look { get; private set; }
        public Vector2 Move { get; private set; }
        public bool Jump { get; private set; }
        public bool Walk { get; private set; }
        public bool Sprint { get; private set; }

        public static InputManager Instance { get; private set; }

        private void OnEnable()
        {
            _currentMap.Enable();
            _lookAction.Enable();
            _moveAction.Enable();
            _jumpAction.Enable();
            _walkAction.Enable();
            _sprintAction.Enable();
        }

        private void OnDisable()
        {
            _currentMap.Disable();
            _lookAction.Disable();
            _moveAction.Disable();
            _jumpAction.Disable();
            _walkAction.Disable();
            _sprintAction.Disable();
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            _currentMap = _playerControls.FindActionMap(_actionMapName);

            _lookAction = _currentMap.FindAction(_look);
            _moveAction = _currentMap.FindAction(_move);
            _jumpAction = _currentMap.FindAction(_jump);
            _walkAction = _currentMap.FindAction(_walk);
            _sprintAction = _currentMap.FindAction(_sprint);

            RegisterInputActions();
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
        }

        internal void ConsumeJump()
        {
            Jump = false;
        }
    }
}
