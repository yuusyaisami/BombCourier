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

        // 爆弾Prefab



    }
}