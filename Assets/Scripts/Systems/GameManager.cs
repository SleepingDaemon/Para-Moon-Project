using UnityEngine;

namespace ParaMoon
{
    public class GameManager : ServiceBehaviour<GameManager>
    {
        public static GameManager Instance { get; private set; }

        public enum GameState
        {
            MainMenu,
            Gameplay,
            EROS,
            Cutscene,
            Loading,
            Dialogue,
        }

        private GameState currentState;
        public GameState CurrentState => currentState;

        public event System.Action<GameState> OnGameStateChanged;

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
