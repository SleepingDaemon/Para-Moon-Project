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
        [SerializeField] string _sprint = "Sprint";

        InputActionMap _currentMap;
        InputAction _lookAction;
        InputAction _moveAction;
        InputAction _jumpAction;
        InputAction _sprintAction;

        public Vector2 Look { get; private set; }
        public Vector2 Move { get; private set; }
        public bool Jump { get; private set; }
        public bool Sprint { get; private set; }

        public static InputManager Instance { get; private set; }

        private void OnEnable()
        {
            _currentMap.Enable();
            _lookAction.Enable();
            _moveAction.Enable();
            _jumpAction.Enable();
            _sprintAction.Enable();
        }

        private void OnDisable()
        {
            _currentMap.Disable();
            _lookAction.Disable();
            _moveAction.Disable();
            _jumpAction.Disable();
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
            _sprintAction = _currentMap.FindAction(_sprint);

            RegisterInputActions();
        }

        private void RegisterInputActions()
        {
            _lookAction.performed += LookAction_performed;
            _lookAction.canceled += LookAction_canceled;
            _moveAction.performed += MoveAction_performed;
            _moveAction.canceled += MoveAction_canceled;
            _jumpAction.performed += JumpAction_performed;
            _jumpAction.canceled += JumpAction_canceled;
            _sprintAction.performed += SprintAction_performed;
            _sprintAction.canceled += SprintAction_canceled;
        }

        private void JumpAction_performed(InputAction.CallbackContext context)
        {
            Jump = context.ReadValueAsButton();
        }

        private void JumpAction_canceled(InputAction.CallbackContext context)
        {
            Jump = false;
        }

        private void SprintAction_performed(InputAction.CallbackContext context)
        {
            Sprint = context.ReadValueAsButton();
        }

        private void SprintAction_canceled(InputAction.CallbackContext context)
        {
            Sprint = false;
        }

        private void MoveAction_performed(InputAction.CallbackContext context)
        {
            Move = context.ReadValue<Vector2>();
        }

        private void MoveAction_canceled(InputAction.CallbackContext context)
        {
            Move = Vector2.zero;
        }

        private void LookAction_performed(InputAction.CallbackContext obj)
        {
            Look = obj.ReadValue<Vector2>();
        }

        private void LookAction_canceled(InputAction.CallbackContext context)
        {
            Look = Vector2.zero;
        }
    }
}
