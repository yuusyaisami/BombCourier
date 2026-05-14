using UnityEngine;
using BC.Utility;
using Unity.VisualScripting;
namespace BC.Manager
{
    public enum GameState
    {
        Loading, // Logic側がステージをロードしている状態。StageManagerMBがこの状態のとき、GameLogicManagerMBはまだステージの情報を持っていない可能性がある。
        Starting,
        Intro,
        SetupPlaying,
        FusePlaying,
        Exploded, // 爆弾が爆発した状態。プレイヤーはこの状態になったときにリロードを促すUIを表示する。
        Goaling, // ゴールに到達した状態。プレイヤーはこの状態になったときにステージクリアのUIを表示する。
        NextStage, // 次のステージに進むための準備をしている状態。プレイヤーはこの状態になったときに次のステージに進むためのUIを表示する。
        Reload,
        GameOver // 爆弾爆発
    }
    public class GameStateManagerMB : MonoBehaviour
    {
        public static GameStateManagerMB Instance { get; private set; }
        private StateMachine<GameState> _stateMachine = new StateMachine<GameState>();
        public GameState CurrentState => _stateMachine.CurrentState;
        public StateMachine<GameState> StateMachine => _stateMachine;
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

        }
    }
}