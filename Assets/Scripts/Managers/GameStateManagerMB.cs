using UnityEngine;
using BC.Utility;
using Unity.VisualScripting;
namespace BC.Manager
{
    // ゲーム全体の状態遷移を一元管理する MonoBehaviour。
    // Starting / Intro / Playing / Exploded / Reload などの流れをここで切り替える。
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
        ResetStage, // イントロを再生せずに、現在のステージ全体をやり直す。
        GameOver, // 爆弾爆発
        ReturnToTitle, // タイトルに戻る
    }

    // StateMachine の現在状態を保持し、各 Manager へ遷移通知を送る入口。
    public class GameStateManagerMB : MonoBehaviour
    {
        public static GameStateManagerMB Instance { get; private set; }
        private StateMachine<GameState> _stateMachine = new StateMachine<GameState>();
        public GameState CurrentState => _stateMachine.CurrentState;
        public StateMachine<GameState> StateMachine => _stateMachine;
        private void Awake()
        {
            // 1 シーン 1 つを想定した singleton。
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // 開始時は必ず Starting から進める。
        private void Start()
        {
            _stateMachine.ChangeState(GameState.Starting);
        }

        // 外部から状態遷移を要求するときの唯一の入口。
        public void ChangeState(GameState newState)
        {
            _stateMachine.ChangeState(newState);
        }

        // 1 シーン 1 インスタンス前提なので、破棄時に static 参照を必ず畳む。
        // これを怠ると scene reload 直後に Instance が破棄済みオブジェクトを指し続け、
        // 参照側が Unity の fake-null 挙動に依存する不安定な状態になる。
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}