using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ParaMoon
{
    public class UIManager : ServiceBehaviour<UIManager>
    {
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

        InteractionUIController _promptController;
        InventoryGridUI _playerInventoryUI;
        InventoryGridUI _containerInventoryUI;
        InventoryGridUI _armorInventoryUI;
        ContainerInteractionUI _containerInteractionUI;
        CharacterEquipmentUI _characterEquipmentUI;
        LoadingScreenController _loadingScreen;

        UIState _currentState = UIState.Gameplay;
        Stack<UIState> _stateHistory = new();

        public UIState CurrentState => _currentState;
        public GameObject WindowSystemLayer => _windowLayer;

        public enum UIState
        {
            Gameplay,
            EROSMenu,
            Cutscene,
            LoadingScreen,
            Dialogue
        }

        #region Unity Methods
        private void Start()
        {
            if (ServiceLocator.Instance.TryGetService<InputManager>(out var inputManager))
                inputManager.OnToggleMenu += ToggleMenuLayer;
            else
                ServiceLocator.Instance.WhenAvailable<InputManager>(im => im.OnToggleMenu += ToggleMenuLayer);

            if (_erosLayer != null)
                _erosLayer.SetActive(false);
        }


        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }


        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

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
            Debug.Log("[UIManager] Initializing UIManager");
            base.Initialize();

            // Make sure all layers are in the correct initial state
            SetUIState(UIState.Gameplay);

            // Register with game manager if needed
            if (ServiceLocator.Instance.TryGetService<GameManager>(out var gameManager))
            {
                gameManager.OnGameStateChanged += OnGameStateChanged;
                Debug.Log("[UIManager] Successfully registered with GameManager");
            }
            else
            {
                Debug.Log("[UIManager] GameManager not available yet, will register when available");
                ServiceLocator.Instance.WhenAvailable<GameManager>(gm =>
                {
                    gm.OnGameStateChanged += OnGameStateChanged;
                    Debug.Log("[UIManager] Later registered with GameManager");
                });
            }

            Debug.Log("[UIManager] Initialization complete");
        }

        private void InitializeLoadingScreen()
        {
            if (_loadingScreen == null)
            {
                _loadingScreen = GameObject.FindFirstObjectByType<LoadingScreenController>(FindObjectsInactive.Include);

                if (_loadingScreen == null)
                    Debug.LogError("[UIManager] LoadingScreenController not found in scene");
            }

            _loadingScreen.gameObject.SetActive(true);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "GameUI")
            {
                FindUIReferences();

                // Re-apply the current UI state to ensure consistency
                SetUIState(_currentState);
            }
        }

        private void FindUIReferences()
        {
            Debug.Log("[UIManager] Finding UI references");

            InitializeLoadingScreen();

            // Try to find canvas objects
            if (_mainCanvas == null)
            {
                _mainCanvas = GameObject.FindGameObjectWithTag("MainCanvas")?.GetComponent<Canvas>();
                if (_mainCanvas == null)
                    Debug.LogError("[UIManager] MainCanvas not found in scene");
                else
                    Debug.Log("[UIManager] Found MainCanvas");
            }

            if (_mainCanvas != null)
            {
                // Try to find layers
                if (_persistentLayer == null)
                    _persistentLayer = _mainCanvas.transform.Find("PersistentLayer")?.gameObject;

                if (_persistentLayer != null)
                {
                    _promptController = _persistentLayer.GetComponentInChildren<InteractionUIController>();
                    _reticleLayer = _persistentLayer.transform.Find("ReticleImage")?.gameObject;
                    _highlighter = _persistentLayer.transform.Find("Highlighter")?.gameObject;
                }

                if (_gameplayHUDLayer == null)
                    _gameplayHUDLayer = _mainCanvas.transform.Find("GameplayHUDLayer")?.gameObject;

                if (_erosLayer == null)
                {
                    _erosLayer = _mainCanvas.transform.Find("EROSPanel")?.gameObject;
                    if (_erosLayer == null)
                        Debug.LogError("[UIManager] EROSLayer not found in MainCanvas");

                    Debug.Log("[UIManager] Found EROSLayer");
                }

                if (_erosLayer != null)
                {
                    _containerInteractionUI = _erosLayer.GetComponentInChildren<ContainerInteractionUI>(true);
                    if (_containerInteractionUI != null)
                        _windowLayer = _containerInteractionUI.gameObject;
                    else
                        Debug.LogError("[UIManager] ContainerInteractionUI not found in EROSLayer");

                    if (_windowLayer != null)
                    {
                        _containerInteractionUI = _windowLayer.GetComponent<ContainerInteractionUI>();
                        if (_containerInteractionUI == null)
                            Debug.LogError("[UIManager] ContainerInteractionUI not found in WindowLayer");

                        _characterEquipmentUI = _windowLayer.GetComponent<CharacterEquipmentUI>();

                        _playerInventoryWindow = _windowLayer.transform.Find("InventoryWindow")?.gameObject;
                        _containerInventoryWindow = _windowLayer.transform.Find("ContainerWindow")?.gameObject;
                        _armorInventoryWindow = _windowLayer.transform.Find("ArmorWindow")?.gameObject;

                        if (_playerInventoryWindow == null || _containerInventoryWindow == null || _armorInventoryWindow == null)
                            Debug.LogError("[UIManager] InventoryWindow or ContainerWindow not found in WindowLayer");

                        _playerInventoryUI = _playerInventoryWindow.GetComponent<InventoryGridUI>();
                        _containerInventoryUI = _containerInventoryWindow.GetComponent<InventoryGridUI>();
                        _armorInventoryUI = _armorInventoryWindow.GetComponent<InventoryGridUI>();

                        if (_armorInventoryWindow == null)
                            Debug.LogError("[UIManager] ArmorInventoryWindow not found in WindowLayer");

                        if (_playerInventoryUI == null)
                            Debug.LogError("[UIManager] InventoryGridUI not found in UI hierarchy");
                    }



                    if (_tabLayer == null)
                        _tabLayer = _erosLayer.transform.Find("TabLayer")?.gameObject;
                }
            }

            if (_cameraCanvas == null)
                _cameraCanvas = GameObject.Find("CameraCanvas")?.GetComponent<Canvas>();

            if (_worldSpaceCanvas == null)
                _worldSpaceCanvas = GameObject.Find("WorldCanvas")?.GetComponent<Canvas>();
        }

        /// <summary>
        /// Toggles the eROS layer visibility and switches input action map
        /// </summary>
        public void ToggleMenuLayer()
        {
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
            }
        }

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

            switch (newState)
            {
                case UIState.Gameplay:
                    if (_persistentLayer != null)
                    {
                        _persistentLayer.SetActive(true);
                        _reticleLayer.SetActive(true);
                        _highlighter.SetActive(true);
                    }

                    if (_gameplayHUDLayer != null)
                        _gameplayHUDLayer.SetActive(true);
                    if (_erosLayer != null)
                        _erosLayer.SetActive(false);
                    if (_windowLayer != null)
                    {
                        // Close the container UI when EROS is closed
                        CloseContainerUI();
                        _windowLayer.SetActive(false);
                    }

                    if (_tabLayer != null)
                        _tabLayer.SetActive(false);

                    // Switch to player input map when in gameplay
                    if (ServiceLocator.Instance.TryGetService<InputManager>(out var inputManager))
                        inputManager.EnablePlayerActionMap();
                    break;

                case UIState.EROSMenu:
                    if (_persistentLayer != null)
                    {
                        _persistentLayer.SetActive(true);
                        _reticleLayer.SetActive(false);
                        _highlighter.SetActive(false);
                    }

                    if (_gameplayHUDLayer != null)
                        _gameplayHUDLayer.SetActive(true);
                    if (_erosLayer != null)
                        _erosLayer.SetActive(true);
                    if (_windowLayer != null)
                        _windowLayer.SetActive(true);
                    if (_tabLayer != null)
                        _tabLayer.SetActive(true);

                    // Switch to UI input map when in EROS
                    if (ServiceLocator.Instance.TryGetService<InputManager>(out var im))
                        im.EnableUIActionMap();
                    break;

                case UIState.Cutscene:
                    _persistentLayer.SetActive(false);
                    _gameplayHUDLayer.SetActive(false);
                    if (_erosLayer != null) 
                        _erosLayer.SetActive(false);
                    if (_windowLayer != null) 
                        _windowLayer.SetActive(false);
                    if (_tabLayer != null) 
                        _tabLayer.SetActive(false);
                    break;

                case UIState.LoadingScreen:
                    _persistentLayer.SetActive(false);
                    _gameplayHUDLayer.SetActive(false);
                    if (_erosLayer != null) 
                        _erosLayer.SetActive(false);
                    if (_windowLayer != null) 
                        _windowLayer.SetActive(false);
                    if (_tabLayer != null) 
                        _tabLayer.SetActive(false);
                    // Show loading screen
                    break;

                case UIState.Dialogue:
                    _persistentLayer.SetActive(true);
                    _gameplayHUDLayer.SetActive(false);
                    if (_erosLayer != null) _erosLayer.SetActive(false);
                    if (_windowLayer != null) _windowLayer.SetActive(false);
                    if (_tabLayer != null) _tabLayer.SetActive(false);
                    // Show dialogue UI
                    break;
            }

            // Notify other UI systems
            OnUIStateChanged(previousState, _currentState);
        }

        private void OnUIStateChanged(UIState previousState, UIState newState)
        {
            // Update cursor visibility/locking based on state
            UpdateCursorState(newState);

            // Notify any registered UI components
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

                case GameManager.GameState.Cutscene:
                    SetUIState(UIState.Cutscene);
                    break;
                case GameManager.GameState.Loading:
                    SetUIState(UIState.LoadingScreen);
                    break;
                case GameManager.GameState.Dialogue:
                    SetUIState(UIState.Dialogue);
                    break;
            }
        }

        // Get Current UI State
        public UIState GetCurrentUIState()
        {
            return _currentState;
        }

        #region Inventory

        public void OpenContainerUI(InventoryManager containerInventory, InventoryManager playerInventory, string containerName)
        {
            // Set UI state to EROS (menu)
            PushUIState(UIState.EROSMenu);

            // Make sure EROS layer is active first
            if (_erosLayer != null && !_erosLayer.activeSelf)
                _erosLayer.SetActive(true);

            // Initialize and show the container-player UI
            _containerInteractionUI.Initialize(containerInventory, playerInventory, containerName);
        }

        public void CloseContainerUI()
        {
            _containerInventoryWindow.SetActive(false);
        }

        #endregion

        #region Loading Screen

        public IEnumerator ShowTransition(float duration)
        {
            // Initialize loading screen if needed
            InitializeLoadingScreen();

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

        public InventoryGridUI GetPlayerInventoryUI()
        {
            return _playerInventoryUI;
        }

        public InventoryGridUI GetArmorInventoryUI()
        {
            return _armorInventoryUI;
        }

        public LoadingScreenController GetLoadingScreenController()
        {
            return _loadingScreen;
        }

        public InventoryGridUI GetContainerUI(ContainerType type)
        {
            if (type == ContainerType.Player)
                return _playerInventoryUI;
            
            if (type == ContainerType.Storage)
                return _containerInventoryUI;

            Debug.LogError($"[UIManager] No container UI found for type: {type}");
            return null;
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
