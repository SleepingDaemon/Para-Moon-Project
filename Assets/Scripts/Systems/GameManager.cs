using UnityEngine;

namespace ParaMoon
{
    [Injectable]
    [SceneExported("GameManager")]
    public class GameManager : ServiceBehaviour<GameManager>
    {
        public enum GameState
        {
            Boot,
            MainMenu,
            EROS,
            Gameplay,
            Loading,
        }

        private GameState currentState;
        public GameState CurrentState => currentState;

        public event System.Action<GameState> OnGameStateChanged;

        protected override void Awake()
        {
            base.Awake();

            Debug.Log("[GameManager] Initializing from Boot scene");

            // Initialize scene dependency manager
            if (!ServiceLocator.Instance.TryGetService<SceneDependencyManager>(out _))
            {
                ServiceLocator.Instance.CreateAndRegisterService<SceneDependencyManager>();
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            SetGameState(GameState.Gameplay);
        }

        public void SetGameState(GameState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                OnGameStateChanged?.Invoke(newState);
            }
        }
    }
}
