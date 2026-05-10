using UnityEngine;
using BC.Utility;
namespace BC.Manager
{
    public enum GameState
    {
        Starting,
        Intro,
        Playing,
        StageClear,
        GameOver // 爆弾爆発
    }
    public class GameStateManagerMB : MonoBehaviour
    {
        public static GameStateManagerMB Instance { get; private set; }
        private StateMachine<GameState> _stateMachine = new StateMachine<GameState>();
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        private void Start()
        {
            _stateMachine.ChangeState(GameState.Starting);
        }
        public void ChangeState(GameState newState)
        {
            _stateMachine.ChangeState(newState);
        }
        private void Update()
        {
            switch (_stateMachine.CurrentState)
            {
                case GameState.Starting:
                    //HandleStarting();
                    break;
                case GameState.Intro:
                    //HandleIntro();
                    break;
                case GameState.Playing:
                    //HandlePlaying();
                    break;
                case GameState.StageClear:
                    //HandleStageClear();
                    break;
                case GameState.GameOver:
                    //HandleGameOver();
                    break;
            }
        }
    }
}