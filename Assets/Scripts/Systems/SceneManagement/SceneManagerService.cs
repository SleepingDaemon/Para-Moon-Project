using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ParaMoon
{
    public enum SceneTransitionType
    {
        None,
        Fade,
    }

    [Serializable]
    public class SceneTransitionSettings
    {
        public SceneTransitionType Type = SceneTransitionType.Fade;
        public float Duration = 0.5f;
        public Color FadeColor = Color.black;
    }

    public enum LoadingScreenType
    {
        None,
        Simple,
        Detailed,
    }

    [Serializable]
    public class LoadingScreenSettings
    {
        public LoadingScreenType Type = LoadingScreenType.Simple;
        public string LoadingSceneName = "LoadingScreen";
        public float MinDisplayTime = 0.5f;
        public bool ShowProgressBar = true;
    }

    public class SceneOperation
    {
        public string SceneName { get; private set; }
        public AsyncOperation Operation { get; private set; }
        public Action<float> OnProgressUpdated { get; set; }
        public Action OnComplete { get; set; }
        public float Progress => Operation?.progress ?? 0f; // Progress of the async operation (0 to 1)
        public bool IsDone => Operation?.isDone ?? false;

        public SceneOperation(string sceneName, AsyncOperation operation)
        {
            SceneName = sceneName;
            Operation = operation;
            if (operation != null)
            {
                operation.completed += OnOperationCompleted;
            }
        }

        private void OnOperationCompleted(AsyncOperation operation)
        {
            OnComplete?.Invoke();
        }
    }

    [Injectable]
    public class SceneManagerService : ServiceBehaviour<SceneManagerService>
    {
        [Header("Scene Names")]
        [SerializeField] string _bootSceneName = "Boot";
        [SerializeField] string _uiSceneName = "GameUI";

        [Header("Scene Transition Settings")]

        [SerializeField] SceneTransitionSettings _defaultTransition = new();
        [SerializeField] LoadingScreenSettings _defaultLoadingScreen = new();

        Dictionary<string, bool> _loadedScenes = new();
        Dictionary<string, List<string>> _sceneDependencies = new();
        private HashSet<string> _persistentScenes = new();
        Queue<Action> _sceneOperationQueue = new();
        bool _isProcessingQueue = false;
        bool _isProcessingOperation = false;
        
        static string _initialSceneName;

        public event Action<string> OnSceneLoadStarted;
        public event Action<string> OnSceneLoadCompleted;
        public event Action<string> OnSceneUnloadStarted;
        public event Action<string> OnSceneUnloadCompleted;
        public event Action<float> OnSceneLoadProgressUpdated;

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Your existing code...

            // Add CrossSceneProcessor to all root objects in the newly loaded scene
            if (ServiceLocator.Instance.TryGetService<SceneDependencyManager>(out _))
            {
                var rootObjects = scene.GetRootGameObjects();
                foreach (var rootObject in rootObjects)
                {
                    if (rootObject.GetComponent<SceneDependencyProcessor>() == null)
                    {
                        rootObject.AddComponent<SceneDependencyProcessor>();
                    }
                }

                Debug.Log($"[SceneManagerService] Added CrossSceneProcessors to {scene.name} scene objects");
            }
        }

        private void OnSceneUnloading(Scene scene)
        {
            // Clean up cross-scene references for this scene
            if (ServiceLocator.Instance.TryGetService<SceneDependencyManager>(out var manager))
            {
                manager.UnregisterScene(scene.name);
                Debug.Log($"[SceneManagerService] Unregistered cross-scene references for scene '{scene.name}'");
            }
        }

        // Add this to OnEnable
        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloading;
        }

        // Add this to OnDisable
        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloading;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void EnsureBootScene()
        {
            // Store the current active scene name before making any changes
            _initialSceneName = SceneManager.GetActiveScene().name;
            Debug.Log($"[SceneManagerService] Initial active scene: {_initialSceneName}");

            // Check if Boot scene is already loaded
            bool bootSceneLoaded = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == "Boot")
                {
                    bootSceneLoaded = true;
                    break;
                }
            }

            // If not loaded, load it additively
            if (!bootSceneLoaded)
            {
                Debug.Log("[SceneManagerService] Boot scene not loaded, loading it additively");
                SceneManager.LoadSceneAsync("Boot", LoadSceneMode.Additive);
            }

            // Also check for UI scene
            bool uiSceneLoaded = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == "GameUI")
                {
                    uiSceneLoaded = true;
                    break;
                }
            }

            // If UI not loaded, load it additively too
            if (!uiSceneLoaded)
            {
                Debug.Log("[SceneManagerService] GameUI scene not loaded, loading it additively");
                SceneManager.LoadSceneAsync("GameUI", LoadSceneMode.Additive);
            }
        }

        protected override void Awake()
        {
            base.Awake();

            // Register all initially loaded scenes
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                _loadedScenes[scene.name] = true;
            }

            // Mark Boot and UI scenes as persistent
            _persistentScenes.Add(_bootSceneName);
            _persistentScenes.Add(_uiSceneName);

            LoadPersistentScenes();
        }

        /// <summary>
        /// Loads persistent scenes (Boot and GameUI) if they're not already loaded
        /// </summary>
        public void LoadPersistentScenes()
        {
            // Check if Boot scene is already loaded
            if (!IsSceneLoaded(_bootSceneName))
            {
                Debug.Log($"[SceneManagerService] Loading persistent Boot scene: {_bootSceneName}");
                LoadSceneAdditively(_bootSceneName, () => {
                    // After Boot is loaded, check if UI scene is loaded
                    if (!IsSceneLoaded(_uiSceneName))
                    {
                        Debug.Log($"[SceneManagerService] Loading persistent UI scene: {_uiSceneName}");
                        LoadSceneAdditively(_uiSceneName);
                    }
                });
            }
            else if (!IsSceneLoaded(_uiSceneName))
            {
                // If Boot is loaded but UI isn't, load UI
                Debug.Log($"[SceneManagerService] Loading persistent UI scene: {_uiSceneName}");
                LoadSceneAdditively(_uiSceneName);
            }

            // Restore the initial scene as active if needed
            if (_initialSceneName != null && _initialSceneName != _bootSceneName && IsSceneLoaded(_initialSceneName))
            {
                SetActiveScene(_initialSceneName);
            }
        }

        public void MarkSceneAsPersistent(string sceneName)
        {
            _persistentScenes.Add(sceneName);
        }

        public void UnmarkSceneAsPersistent(string sceneName)
        {
            _persistentScenes.Remove(sceneName);
        }

        public bool IsScenePersistent(string sceneName)
        {
            return _persistentScenes.Contains(sceneName);
        }

        public void LoadSceneAdditively(string sceneName, Action onComplete = null)
        {
            LoadScene(sceneName, LoadSceneMode.Additive, _defaultTransition, LoadingScreenType.None, onComplete);
        }

        public void LoadSceneSingle(string sceneName, Action onComplete = null)
        {
            LoadScene(sceneName, LoadSceneMode.Single, _defaultTransition, _defaultLoadingScreen.Type, onComplete);
        }

        private void LoadScene(string sceneName, LoadSceneMode mode, SceneTransitionSettings transition,
                              LoadingScreenType loadingScreenType, Action onComplete = null)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneManagerService] Scene name is null or empty");
                return;
            }

            if (mode == LoadSceneMode.Additive && IsSceneLoaded(sceneName))
            {
                Debug.Log($"[SceneManagerService] Scene {sceneName} is already loaded");
                onComplete?.Invoke();
                return;
            }

            // Queue the operation - ensures operations happen in sequence
            _sceneOperationQueue.Enqueue(() => StartCoroutine(LoadSceneRoutine(sceneName, mode, transition, loadingScreenType, onComplete)));

            // Start processing the queue if not already
            if (!_isProcessingQueue)
            {
                StartCoroutine(ProcessOperationQueue());
            }
        }

        public void UnloadScene(string sceneName, Action onComplete = null)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneManagerService] Scene name is null or empty");
                return;
            }

            if (!IsSceneLoaded(sceneName))
            {
                Debug.Log($"[SceneManagerService] Scene {sceneName} is not loaded, cannot unload");
                onComplete?.Invoke();
                return;
            }

            _sceneOperationQueue.Enqueue(() => StartCoroutine(UnloadSceneRoutine(sceneName, onComplete)));

            if (!_isProcessingQueue)
            {
                StartCoroutine(ProcessOperationQueue());
            }
        }

        private IEnumerator ProcessOperationQueue()
        {
            _isProcessingQueue = true;

            while (_sceneOperationQueue.Count > 0)
            {
                Action nextOperation = _sceneOperationQueue.Dequeue();
                nextOperation?.Invoke();
                yield return new WaitUntil(() => !_isProcessingOperation);
            }

            _isProcessingQueue = false;
        }

        private IEnumerator LoadSceneRoutine(string sceneName, LoadSceneMode mode,
                                            SceneTransitionSettings transition,
                                            LoadingScreenType loadingScreenType,
                                            Action onComplete)
        {
            _isProcessingOperation = true;

            OnSceneLoadStarted?.Invoke(sceneName);

            // Handle transitions
            if (transition != null && transition.Type != SceneTransitionType.None)
            {
                yield return StartCoroutine(PerformTransitionIn(transition));
            }

            // Set up loading screen if needed
            bool useLoadingScreen = loadingScreenType != LoadingScreenType.None;
            if (useLoadingScreen)
            {
                yield return StartCoroutine(ShowLoadingScreen(_defaultLoadingScreen));
            }

            if (_sceneDependencies.TryGetValue(sceneName, out var dependencies))
            {
                // Load all dependent scenes first
                foreach (var dependentScene in dependencies)
                {
                    if (!IsSceneLoaded(dependentScene))
                    {
                        Debug.Log($"[SceneManagerService] Loading dependent scene: {dependentScene}");
                        // Load dependent scene additively first
                        AsyncOperation depOp = SceneManager.LoadSceneAsync(dependentScene, LoadSceneMode.Additive);
                        yield return depOp;
                        _loadedScenes[dependentScene] = true;
                    }
                }
            }

            // Begin actual scene loading
            Debug.Log($"[SceneManagerService] Loading scene: {sceneName} with mode: {mode}");

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, mode);
            loadOperation.allowSceneActivation = false;

            SceneOperation sceneOp = new SceneOperation(sceneName, loadOperation);

            // Track load progress
            float progress = 0;
            while (loadOperation.progress < 0.9f)
            {
                progress = loadOperation.progress / 0.9f;
                OnSceneLoadProgressUpdated?.Invoke(progress);

                if (ServiceLocator.Instance.TryGetService<UIManager>(out var uiManager))
                    uiManager.UpdateLoadingProgress(progress);

                yield return null;
            }

            // Ensure minimum display time for loading screen if used
            if (useLoadingScreen)
            {
                yield return new WaitForSeconds(_defaultLoadingScreen.MinDisplayTime);
            }

            // Set final progress
            if (ServiceLocator.Instance.TryGetService<UIManager>(out var ui))
                ui.UpdateLoadingProgress(1f);

            // Allow scene activation
            loadOperation.allowSceneActivation = true;

            // Wait for completion
            yield return loadOperation;

            // Handle transitions out
            if (transition != null && transition.Type != SceneTransitionType.None)
            {
                yield return StartCoroutine(PerformTransitionOut(transition));
            }

            // Hide loading screen if used
            if (useLoadingScreen)
            {
                yield return StartCoroutine(HideLoadingScreen());
            }

            // Update loaded scenes tracking
            if (mode == LoadSceneMode.Additive)
            {
                _loadedScenes[sceneName] = true;
            }
            else if (mode == LoadSceneMode.Single)
            {
                _loadedScenes.Clear();
                _loadedScenes[sceneName] = true;
            }

            OnSceneLoadCompleted?.Invoke(sceneName);

            Debug.Log($"[SceneManagerService] Successfully loaded {sceneName}");
            onComplete?.Invoke();

            _isProcessingOperation = false;
        }

        private IEnumerator UnloadSceneRoutine(string sceneName, Action onComplete)
        {
            _isProcessingOperation = true;

            // Skip unloading if this is a persistent scene
            if (IsScenePersistent(sceneName))
            {
                Debug.Log($"[SceneManagerService] Scene {sceneName} is persistent, skipping unload");
                onComplete?.Invoke();
                _isProcessingOperation = false;
                yield break;
            }

            OnSceneUnloadStarted?.Invoke(sceneName);

            // Add this to your UnloadSceneRoutine method
            if (_sceneDependencies.TryGetValue(sceneName, out var dependencies))
            {
                // Unload all dependent scenes first
                foreach (var dependentScene in dependencies)
                {
                    if (IsSceneLoaded(dependentScene))
                    {
                        Debug.Log($"[SceneManagerService] Unloading dependent scene: {dependentScene}");
                        AsyncOperation depOp = SceneManager.UnloadSceneAsync(dependentScene);
                        yield return depOp;
                        _loadedScenes.Remove(dependentScene);
                    }
                }
            }

            Debug.Log($"[SceneManagerService] Unloading scene: {sceneName}");

            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(sceneName);
            yield return unloadOperation;

            if (unloadOperation.isDone)
            {
                _loadedScenes.Remove(sceneName);
                OnSceneUnloadCompleted?.Invoke(sceneName);
                Debug.Log($"[SceneManagerService] Successfully unloaded {sceneName}");
            }
            else
            {
                Debug.LogError($"[SceneManagerService] Failed to unload {sceneName}");
            }

            onComplete?.Invoke();
            _isProcessingOperation = false;
        }

        private IEnumerator PerformTransitionIn(SceneTransitionSettings transition)
        {
            // Implementation would handle UI transitions in
            if (ServiceLocator.Instance.TryGetService<UIManager>(out var uiManager))
            {
                yield return StartCoroutine(uiManager.ShowTransition(transition.Duration));
                Debug.Log($"[SceneManagerService] Transition In: {transition.Type}");
            }
            else
            {
                Debug.LogWarning("[SceneManagerService] UIManager not available for transition, using fallback delay");
                yield return new WaitForSeconds(transition.Duration);
            }
        }

        private IEnumerator PerformTransitionOut(SceneTransitionSettings transition)
        {
            // Implementation would handle UI transitions out
            if (ServiceLocator.Instance.TryGetService<UIManager>(out var uiManager))
            {
                yield return StartCoroutine(uiManager.HideTransition(transition.Duration));
                Debug.Log($"[SceneManagerService] Transition Out: {transition.Type}");
            }
            else
            {
                Debug.LogWarning("[SceneManagerService] UIManager not available for transition, using fallback delay");
                yield return new WaitForSeconds(transition.Duration);
            }
        }

        private IEnumerator ShowLoadingScreen(LoadingScreenSettings settings)
        {
            Debug.Log("[SceneManagerService] Showing loading screen");

            // Could load a dedicated loading screen scene additively
            if (settings.Type == LoadingScreenType.Detailed && !string.IsNullOrEmpty(settings.LoadingSceneName))
            {
                AsyncOperation loadingScreenOp = SceneManager.LoadSceneAsync(settings.LoadingSceneName, LoadSceneMode.Additive);
                yield return loadingScreenOp;
            }
            else if (ServiceLocator.Instance.TryGetService<UIManager>(out var uiManager))
            {
                var loadingScreenController = uiManager.GetLoadingScreenController();
                if (loadingScreenController != null)
                {
                    // Ensure the GameObject is active
                    loadingScreenController.gameObject.SetActive(true);

                    // Use StartCoroutine to properly run the IEnumerator
                    yield return StartCoroutine(loadingScreenController.ShowLoadingScreen(settings.MinDisplayTime));
                }
                else
                {
                    Debug.LogError("[SceneManagerService] LoadingScreenController not found");
                }
            }
        }

        private IEnumerator HideLoadingScreen()
        {
            Debug.Log("[SceneManagerService] Hiding loading screen");

            // Unload loading screen scene if needed
            if (_defaultLoadingScreen.Type == LoadingScreenType.Detailed &&
                !string.IsNullOrEmpty(_defaultLoadingScreen.LoadingSceneName) &&
                IsSceneLoaded(_defaultLoadingScreen.LoadingSceneName))
            {
                AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(_defaultLoadingScreen.LoadingSceneName);
                yield return unloadOp;
            }
            else if (ServiceLocator.Instance.TryGetService<UIManager>(out var uiManager))
            {
                var loadingScreenController = uiManager.GetLoadingScreenController();
                if (loadingScreenController != null)
                {
                    yield return StartCoroutine(loadingScreenController.HideLoadingScreen(_defaultLoadingScreen.MinDisplayTime));
                }
            }
        }

        public void AddSceneDependency(string primaryScene, string dependentScene)
        {
            if (!_sceneDependencies.ContainsKey(primaryScene))
            {
                _sceneDependencies[primaryScene] = new List<string>();
            }

            if (!_sceneDependencies[primaryScene].Contains(dependentScene))
            {
                _sceneDependencies[primaryScene].Add(dependentScene);
            }
        }

        public void SetActiveScene(string sceneName)
        {
            if (IsSceneLoaded(sceneName))
            {
                Scene scene = SceneManager.GetSceneByName(sceneName);
                SceneManager.SetActiveScene(scene);
                Debug.Log($"[SceneManagerService] Scene {sceneName} set as active");
            }
            else
            {
                Debug.LogError($"[SceneManagerService] Cannot set {sceneName} as active - scene not loaded");
            }
        }

        public bool IsSceneLoaded(string sceneName)
        {
            // First check our cached dictionary for efficiency
            if (_loadedScenes.TryGetValue(sceneName, out bool isLoaded))
            {
                return isLoaded;
            }

            // Fall back to checking Unity's SceneManager directly
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name == sceneName)
                {
                    // Update our cache
                    _loadedScenes[sceneName] = true;
                    return true;
                }
            }

            return false;
        }

        public string GetActiveSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }
    }
}