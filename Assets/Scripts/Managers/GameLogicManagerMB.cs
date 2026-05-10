using System;
using System.Collections.Generic;
using BC.Bomb;
using UnityEngine;
namespace BC.Manager
{
    public class GameLogicManagerMB : UnityEngine.MonoBehaviour
    {
        // ゲームのロジックを管理するクラス
        // 例えば、ゲームの状態管理、スコア管理、レベル管理などを担当することができます。
        public static GameLogicManagerMB Instance { get; private set; }
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // 爆弾Ref
        private BombMB currentBomb;
        public Action<BombMB> OnCurrentBombChanged; // 現在の爆弾が変わったときに呼び出されるイベント
        private float currentGameStage;

        public void SetCurrentBomb(BombMB bomb)
        {
            currentBomb = bomb;
            OnCurrentBombChanged?.Invoke(currentBomb);
        }

        public void LoadGameStage()
        {
            StageLoadResult result = StageManagerMB.Instance.LoadStage((int)currentGameStage);
            if (result.bombs.Count > 0)
            {
                SetCurrentBomb(result.bombs[0]);
            }
            // playerをテレポートさせる
            if (result.spawnPoints.Count > 0)
            {
                // とりあえず最初のスポーンポイントにテレポートさせる
                PlayerSpawnPointMB spawnPoint = result.spawnPoints[0];
            }
            GameStateManagerMB.Instance.ChangeState(GameState.Intro);
        }



    }
}