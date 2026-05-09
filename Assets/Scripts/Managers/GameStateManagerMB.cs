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
        private StateMachine<GameState> _stateMachine = new StateMachine<GameState>();

        private void Start()
        {
            _stateMachine.ChangeState(GameState.Starting);
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