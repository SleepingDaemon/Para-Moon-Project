using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ParaMoon
{
    [Injectable]
    [SceneExported("UIManager")]
    public class UIManager : ServiceBehaviour<UIManager>
    {
        public event Action OnUIInitialized;
        public bool IsFullyInitialized { get; private set; } = false;

        // Canvas References
        Canvas _mainCanvas;
        Canvas _cameraCanvas;
        Canvas _worldSpaceCanvas;

        // Layer References
        GameObject _persistentLayer;
        GameObject _gameplayHUDLayer;
        GameObject _erosLayer;
        GameObject _windowLayer;
        GameObject _tabLayer;
        GameObject _reticleLayer;
        GameObject _highlighter;
        GameObject _playerInventoryWindow;
        GameObject _containerInventoryWindow;
        GameObject _armorInventoryWindow;
        //GameObject notificationLayer;

        [Inject] InventoryUIController _inventoryController;
        InteractionUIController _promptController;
        InventoryGridView _playerInventoryUI;
        InventoryGridView _containerInventoryUI;
        InventoryGridView _armorInventoryUI;
        ContainerInteractionUI _containerInteractionUI;
        LoadingScreenController _loadingScreen;

        UIState _currentState = UIState.Gameplay;
        Stack<UIState> _stateHistory = new();

        public UIState CurrentState => _currentState;

        public enum UIState
        {
            Gameplay,
            EROSMenu,
            Cutscene,
            LoadingScreen,
            Dialogue
        }

        #region Unity Methods

        protected override void OnDestroy()
        {
            if (ServiceLocator.Instance.TryGetService<GameManager>(out var gameManager))
                gameManager.OnGameStateChanged -= OnGameStateChanged;

            if (ServiceLocator.Instance.TryGetService<InputManager>(out var inputManager))
                inputManager.OnToggleMenu -= ToggleMenuLayer;

            base.OnDestroy();
        }

        #endregion

        public override void Initialize()
        {
            base.Initialize();
            IsFullyInitialized = false;

            FindUIReferences();

            // Register with game manager
            if (ServiceLocator.Instance.TryGetService<GameManager>(out var gameManager))
                gameManager.OnGameStateChanged += OnGameStateChanged;
            else
                ServiceLocator.Instance.WhenAvailable<GameManager>(gm =>
                    gm.OnGameStateChanged += OnGameStateChanged);

            // Register with input manager
            if (ServiceLocator.Instance.TryGetService<InputManager>(out var inputManager))
                inputManager.OnToggleMenu += ToggleMenuLayer;
            else
                ServiceLocator.Instance.WhenAvailable<InputManager>(im =>
                    im.OnToggleMenu += ToggleMenuLayer);

            SetUIState(UIState.Gameplay);

            IsFullyInitialized = true;
            OnUIInitialized?.Invoke();
            Debug.Log("[UIManager] UI Manager fully initialized and ready");
        }

        private void FindUIReferences()
        {

            if (ServiceLocator.Instance.TryGetService<ReferenceRegistry>(out var registry))
            {
                if (registry == null)
                {
                    Debug.LogWarning("[UIManager] ReferenceRegistry service is null, waiting for it to be available");
                    ServiceLocator.Instance.WhenAvailable<ReferenceRegistry>(r => {
                        registry = r;
                        // Try to find references again when registry becomes available
                        FindUIReferences();
                    });
                    return;
                }

                // Get references from the registry
                _mainCanvas = registry.GetReference<Canvas>("MainCanvas");
                if (_mainCanvas == null)
                    Debug.LogWarning("[UIManager] Main canvas not found in ReferenceRegistry");

                _persistentLayer = registry.GetReference<RectTransform>("PersistentLayer")?.gameObject;
                if (_persistentLayer == null)
                    Debug.LogWarning("[UIManager] Persistent layer not found in ReferenceRegistry");

                _gameplayHUDLayer = registry.GetReference<RectTransform>("GameplayHUDLayer")?.gameObject;
                if (_gameplayHUDLayer == null)
                    Debug.LogWarning("[UIManager] Gameplay HUD layer not found in ReferenceRegistry");

                _erosLayer = registry.GetReference<RectTransform>("EROSPanel")?.gameObject;
                if (_erosLayer == null)
                    Debug.LogWarning("[UIManager] EROS layer not found in ReferenceRegistry");
                else
                    _erosLayer.SetActive(false);

                if (_persistentLayer != null)
                {
                    _promptController = registry.GetReference<InteractionUIController>();
                    _reticleLayer = registry.GetReference<Transform>("ReticleImage")?.gameObject;
                    _highlighter = registry.GetReference<RectTransform>("Highlighter")?.gameObject;
                }

                _inventoryController = registry.GetReference<InventoryUIController>();
                if (_inventoryController == null)
                    Debug.LogWarning("[UIManager] Inventory UI Controller not found in ReferenceRegistry");
            }
            else
            {
                Debug.LogError("[UIManager] ReferenceRegistry not available");
                //FallbackUIReferenceSearch();
            }
        }

        /// <summary>
        /// Toggles the eROS layer visibility and switches input action map
        /// </summary>
        public void ToggleMenuLayer()
        {
            if (_erosLayer == null)
            {
                Debug.LogWarning("[UIManager] eROS layer is not assigned, cannot toggle menu layer");

                // Try to find the reference again
                FindUIReferences();

                if (_erosLayer == null)
                    return;
            }

            // If currently in gameplay, show eROS
            if (_currentState == UIState.Gameplay)
            {
                // Set eROS layer active
                _erosLayer.SetActive(true);

                // Push the EROS state to the state history
                PushUIState(UIState.EROSMenu);
                if (ServiceLocator.Instance.TryGetService<GameManager>(out var gameManager))
                    gameManager.SetGameState(GameManager.GameState.EROS);
                else
                    Debug.LogWarning("[UIManager] GameManager not available, cannot set game state to EROS");

                //if (ServiceLocator.Instance.TryGetService<InputManager>(out var inputManager))
                //{
                //    // Switch to EROS input action map
                //    inputManager.EnableUIActionMap();
                //}
                //else
                //{
                //    Debug.LogWarning("[UIManager] InputManager not available, cannot set input action map to EROS");
                //}
            }
            // If in EROS state (eROS is active), hide it and return to gameplay
            else if (_currentState == UIState.EROSMenu)
            {
                // Hide eROS layer
                _erosLayer.SetActive(false);

                // Return to gameplay state
                PopUIState();
                if (ServiceLocator.Instance.TryGetService<GameManager>(out var gameManager))
                    gameManager.SetGameState(GameManager.GameState.Gameplay);
                else
                    Debug.LogWarning("[UIManager] GameManager not available, cannot set game state to EROS");

                //if (ServiceLocator.Instance.TryGetService<InputManager>(out var inputManager))
                //{
                //    // Switch to EROS input action map
                //    inputManager.EnableUIActionMap();
                //}
                //else
                //{
                //    Debug.LogWarning("[UIManager] InputManager not available, cannot set input action map to EROS");
                //}
            }
        }

        #region State Management
        public void PushUIState(UIState newState)
        {
            _stateHistory.Push(_currentState);
            SetUIState(newState);
        }

        public void PopUIState()
        {
            if (_stateHistory.Count > 0)
            {
                SetUIState(_stateHistory.Pop());
            }
            else
            {
                // Default to gameplay if stack is empty
                SetUIState(UIState.Gameplay);
            }
        }

        public void SetUIState(UIState newState)
        {
            UIState previousState = _currentState;
            _currentState = newState;

            UpdateUIVisibility(newState);
            UpdateCursorState(newState);
        }

        private void UpdateUIVisibility(UIState newState)
        {
            switch (newState)
            {
                case UIState.Gameplay:
                    if (_persistentLayer != null) _persistentLayer.SetActive(true);
                    if (_gameplayHUDLayer != null) _gameplayHUDLayer.SetActive(true);
                    if (_erosLayer != null) _erosLayer.SetActive(false);
                    break;

                case UIState.EROSMenu:
                    if (_persistentLayer != null) _persistentLayer.SetActive(true);
                    if (_gameplayHUDLayer != null) _gameplayHUDLayer.SetActive(true);
                    if (_erosLayer != null) _erosLayer.SetActive(true);
                    break;
            }
        }

        private void UpdateCursorState(UIState state)
        {
            switch (state)
            {
                case UIState.Gameplay:
                    // In gameplay, hide cursor and lock it
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    break;

                case UIState.EROSMenu:
                    // When in UI, show cursor and unlock it
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;
                case UIState.Dialogue:
                    // When in UI, show cursor and unlock it
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;

                case UIState.Cutscene:
                    // During cutscenes, hide cursor but don't lock it
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = false;
                    break;
            }
        }

        private void OnGameStateChanged(GameManager.GameState newGameState)
        {
            // Map game states to UI states
            switch (newGameState)
            {
                case GameManager.GameState.Gameplay:
                    SetUIState(UIState.Gameplay);
                    break;

                case GameManager.GameState.EROS:
                    SetUIState(UIState.EROSMenu);
                    break;

                case GameManager.GameState.Loading:
                    SetUIState(UIState.LoadingScreen);
                    break;
            }
        }

        #endregion

        #region Inventory

        public void OpenContainerUI(InventoryManager containerInventory, InventoryManager playerInventory, string containerName)
        {
            // Set UI state to EROS (menu)
            PushUIState(UIState.EROSMenu);

            _inventoryController.OpenContainerUI(containerInventory.Inventory, containerName);

            // Initialize and show the container-player UI
            //_containerInteractionUI.Initialize(containerInventory, playerInventory, containerName);
        }

        public void CloseContainerUI()
        {
            _inventoryController.CloseContainerUI();    
        }

        #endregion

        #region Loading Screen

        public IEnumerator ShowTransition(float duration)
        {
            // Initialize loading screen if needed
            //InitializeLoadingScreen();

            if (_loadingScreen != null)
            {
                yield return StartCoroutine(_loadingScreen.ShowLoadingScreen(duration));
            }
            else
            {
                Debug.LogError("[UIManager] Cannot show transition - loading screen not initialized");
                yield return new WaitForSeconds(duration);
            }
        }

        public IEnumerator HideTransition(float duration)
        {
            if (_loadingScreen != null)
            {
                yield return StartCoroutine(_loadingScreen.HideLoadingScreen(duration));
            }
            else
            {
                Debug.LogError("[UIManager] Cannot hide transition - loading screen not initialized");
                yield return new WaitForSeconds(duration);
            }
        }

        public void UpdateLoadingProgress(float progress)
        {
            if (_loadingScreen != null)
            {
                _loadingScreen.SetProgress(progress);
            }
        }

        #endregion

        #region Getters

        public InteractionUIController GetInteractionUIController()
        {
            return _promptController;
        }

        public LoadingScreenController GetLoadingScreenController()
        {
            return _loadingScreen;
        }

        public InventoryGridView GetContainerUI(ContainerType type)
        {
            if (type == ContainerType.Player)
                return _playerInventoryUI;
            
            if (type == ContainerType.Storage)
                return _containerInventoryUI;

            Debug.LogError($"[UIManager] No container UI found for type: {type}");
            return null;
        }

        public InventoryUIController GetInventoryUIController()
        {
            return _inventoryController;
        }

        internal void SetInventoryUIController(InventoryUIController inventoryUIController)
        {
            _inventoryController = inventoryUIController;
        }

        #endregion

        #region UI System Controls

        // Interaction UI
        //public void ShowInteractionPrompt(string promptText, InteractionType type)
        //{
        //    if (promptController != null)
        //        promptController.ShowPrompt(promptText, type);
        //}

        //public void HideInteractionPrompt()
        //{
        //    if (promptController != null)
        //        promptController.HidePrompt();
        //}

        //// Window Management
        //public void OpenWindow(string windowType)
        //{
        //    SetUIState(UIState.WindowFocus);

        //    if (windowController != null)
        //        windowController.OpenWindow(windowType);
        //}

        //public void CloseAllWindows()
        //{
        //    if (windowController != null)
        //    {
        //        windowController.CloseAllWindows();
        //        SetUIState(UIState.Gameplay);
        //    }
        //}

        //// Notification System
        //public void ShowNotification(string message, NotificationType type = NotificationType.Info)
        //{
        //    if (notificationSystem != null)
        //        notificationSystem.ShowNotification(message, type);
        //}

        #endregion
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
}
