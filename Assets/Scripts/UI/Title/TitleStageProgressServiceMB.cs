using UnityEngine;

namespace BC.UI.Title
{
    // タイトル画面のステージ解放状態を PlayerPrefs で永続化するサービス。
    // ステージ 0 は常時解放。ステージ N は ステージ N-1 クリア済みで解放される。
    public sealed class TitleStageProgressServiceMB : MonoBehaviour
    {
        private const string KeyPrefix = "Stage_Cleared_";

        public static TitleStageProgressServiceMB Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>インデックス <paramref name="stageIndex"/> が遊べる状態かどうかを返す。</summary>
        public bool IsUnlocked(int stageIndex)
        {
            if (stageIndex <= 0) return true;
            return IsCleared(stageIndex - 1);
        }

        /// <summary>インデックス <paramref name="stageIndex"/> をクリア済みとしてマークする。</summary>
        public bool IsCleared(int stageIndex)
        {
            return PlayerPrefs.GetInt(KeyPrefix + stageIndex, 0) == 1;
        }

        /// <summary>インデックス <paramref name="stageIndex"/> をクリア済みとして保存する。</summary>
        public void SetCleared(int stageIndex)
        {
            PlayerPrefs.SetInt(KeyPrefix + stageIndex, 1);
            PlayerPrefs.Save();
        }

        /// <summary>デバッグ用: 全ステージのクリアデータを消去する。</summary>
        [ContextMenu("Debug: Reset All Stage Progress")]
        private void DebugResetAll()
        {
            for (int i = 0; i < 32; i++)
            {
                PlayerPrefs.DeleteKey(KeyPrefix + i);
            }
            PlayerPrefs.Save();
            Debug.Log($"[{nameof(TitleStageProgressServiceMB)}] All stage progress reset.");
        }
    }
}
