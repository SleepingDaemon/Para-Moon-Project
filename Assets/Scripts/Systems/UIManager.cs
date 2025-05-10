using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ParaMoon
{
    [Injectable]
    public class UIManager : ServiceBehaviour<UIManager>
    {
        public event Action OnUIInitialized;

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

        [Inject] GameManager _gameManager;
        [Inject] InputManager _inputManager;
        //[Inject] HighlightManager _highlightManager;
        InventoryUIController _inventoryController;
        InteractionUIController _interactionPrompt;
        PlayerInventory _playerInventory;
        InventoryGridView _playerInventoryUI;
        InventoryGridView _containerInventoryUI;
        InventoryGridView _armorInventoryUI;
        LoadingScreenController _loadingScreen;
        bool _uiReferencesFound = false;

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
            _gameManager.OnGameStateChanged -= OnGameStateChanged;
            _inputManager.OnMenuToggleRequested -= ToggleEROSMenu;

            base.OnDestroy();
        }

        #endregion

        public override void Initialize()
        {
            base.Initialize();

            FindUIReferences();

            ServiceLocator.Instance.WhenAvailable<GameManager>(gm =>
                gm.OnGameStateChanged += OnGameStateChanged);

            ServiceLocator.Instance.WhenAvailable<InputManager>(im =>
                im.OnMenuToggleRequested += ToggleEROSMenu);

            SetUIState(UIState.Gameplay);

            OnUIInitialized?.Invoke();
            Debug.Log("[UIManager] UI Manager fully initialized and ready");
        }

        private void FindUIReferences()
        {
            // Find all UI layers in the "GameUI" scene
            try
            {
                // First ensure we're looking in the right scene
                Scene gameUIScene = SceneManager.GetSceneByName("GameUI");
                if (!gameUIScene.isLoaded)
                {
                    Debug.LogError("[UIManager] GameUI scene is not loaded! Cannot find UI references.");
                    return;
                }

                // Check if we've already found references successfully
                if (_uiReferencesFound)
                    return;

                Debug.Log("[UIManager] Finding UI references in GameUI scene");

                // Find canvases first
                _mainCanvas = GameObject.FindGameObjectWithTag("MainCanvas")?.GetComponent<Canvas>();
                if (_mainCanvas == null)
                    Debug.LogWarning("[UIManager] Could not find MainCanvas with tag 'MainCanvas'");

                _cameraCanvas = GameObject.Find("CameraCanvas")?.GetComponent<Canvas>();
                _worldSpaceCanvas = GameObject.Find("WorldSpaceCanvas")?.GetComponent<Canvas>();

                // Find UI layers
                _persistentLayer = GameObject.Find("PersistentLayer");
                _gameplayHUDLayer = GameObject.Find("GameplayHUDLayer");

                // Find InteractionUIController
                if (_persistentLayer != null)
                {
                    _interactionPrompt = FindFirstObjectByType<InteractionUIController>(FindObjectsInactive.Include);
                    if (_interactionPrompt == null)
                        _interactionPrompt = GameObject.Find("InteractionPrompts")?.GetComponent<InteractionUIController>();

                    if (_interactionPrompt == null)
                        Debug.LogWarning("[UIManager] Could not find InteractionUIController in PersistentLayer");
                }

                // Find EROSLayer as child of MainCanvas
                if (_mainCanvas != null)
                {
                    _erosLayer = _mainCanvas.transform.Find("EROSPanel")?.gameObject;
                    if (_erosLayer == null)
                        Debug.LogWarning("[UIManager] Could not find EROSLayer under MainCanvas");
                    else if (_erosLayer.activeInHierarchy)
                        _erosLayer.SetActive(false);
                }

                _reticleLayer = GameObject.Find("ReticleLayer");
                _highlighter = GameObject.Find("Highlighter");

                // Find inventory windows
                _playerInventoryWindow = GameObject.Find("InventoryWindow");
                _playerInventoryUI = _playerInventoryWindow?.GetComponent<InventoryGridView>();
                _containerInventoryWindow = GameObject.Find("ContainerWindow");
                _containerInventoryUI = _containerInventoryWindow?.GetComponent<InventoryGridView>();
                _armorInventoryWindow = GameObject.Find("ArmorWindow");

                // Find loading screen
                //_loadingScreen = GameObject.FindObjectOfType<LoadingScreenController>();

                // Check if we found most essential elements
                bool essentialsFound = _mainCanvas != null && _erosLayer != null && _gameplayHUDLayer != null && _persistentLayer != null;
                if (essentialsFound)
                {
                    _uiReferencesFound = true;
                    Debug.Log("[UIManager] Essential UI references found successfully");
                }
                else
                {
                    Debug.LogError("[UIManager] Failed to find essential UI elements. UI functionality will be limited.");
                }

                // Log reference statuses for debugging
                LogUIReferencesStatus();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIManager] Error finding UI references: {ex.Message}");
            }
        }

        private void LogUIReferencesStatus()
        {
            Debug.Log($"[UIManager] UI References Status:\n" +
                $"MainCanvas: {(_mainCanvas != null ? "Found" : "Missing")}\n" +
                $"CameraCanvas: {(_cameraCanvas != null ? "Found" : "Missing")}\n" +
                $"WorldSpaceCanvas: {(_worldSpaceCanvas != null ? "Found" : "Missing")}\n" +
                $"PersistentLayer: {(_persistentLayer != null ? "Found" : "Missing")}\n" +
                $"GameplayHUDLayer: {(_gameplayHUDLayer != null ? "Found" : "Missing")}\n" +
                $"EROSLayer: {(_erosLayer != null ? "Found" : "Missing")}\n" +
                $"ReticleLayer: {(_reticleLayer != null ? "Found" : "Missing")}\n" +
                $"Highlighter: {(_highlighter != null ? "Found" : "Missing")}\n" +
                $"PlayerInventoryWindow: {(_playerInventoryWindow != null ? "Found" : "Missing")}\n" +
                $"ContainerInventoryWindow: {(_containerInventoryWindow != null ? "Found" : "Missing")}\n" +
                $"ArmorInventoryWindow: {(_armorInventoryWindow != null ? "Found" : "Missing")}\n" +
                $"LoadingScreen: {(_loadingScreen != null ? "Found" : "Missing")}");
        }

        /// <summary>
        /// Toggles the eROS layer visibility and switches input action map
        /// </summary>
        /// <summary>
        /// Toggles the EROS menu visibility and handles all related state changes in one place
        /// </summary>
        public void ToggleEROSMenu()
        {
            if (_erosLayer == null)
            {
                Debug.LogWarning("[UIManager] EROS layer is not assigned, cannot toggle menu");
                FindUIReferences();
                if (_erosLayer == null)
                    return;
            }

            bool showingErosMenu = _currentState == UIState.EROSMenu;

            // Toggle EROS menu state
            if (showingErosMenu)
            {
                // Explicitly close container inventory if open
                var containerWindow = _inventoryController.GetContainerWindow();
                if (containerWindow != null && containerWindow.activeInHierarchy)
                    CloseContainerUI();

                // Return to gameplay
                _erosLayer.SetActive(false);
                PopUIState();

                if (_gameManager != null)
                    _gameManager.SetGameState(GameManager.GameState.Gameplay);

                if (_inputManager != null)
                    _inputManager.SetInputMode(false);
            }
            else
            {
                // Show EROS menu
                _erosLayer.SetActive(true);
                PushUIState(UIState.EROSMenu);

                if (_gameManager != null)
                    _gameManager.SetGameState(GameManager.GameState.EROS);

                if (_inputManager != null)
                    _inputManager.SetInputMode(true);
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

        public void OpenContainerUI(IInventory containerInventory)
        {
            // Set UI state to EROS (menu)
            PushUIState(UIState.EROSMenu);

            _inventoryController.OpenContainerUI(containerInventory);
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
            return _interactionPrompt;
        }

        public LoadingScreenController GetLoadingScreenController()
        {
            return _loadingScreen;
        }

        public InventoryUIController GetInventoryUIController()
        {
            return _inventoryController;
        }

        internal void SetInventoryUIController(InventoryUIController inventoryUIController)
        {
            _inventoryController = inventoryUIController;
        }

        internal void SetInteractionUIController(InteractionUIController interactionUIController)
        {
            _interactionPrompt = interactionUIController;
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
