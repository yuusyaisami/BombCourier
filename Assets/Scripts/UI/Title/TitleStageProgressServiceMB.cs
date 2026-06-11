using UnityEngine;

namespace BC.UI.Title
{
    // タイトル画面のステージ解放状態を PlayerPrefs で永続化するサービス。
    // ステージ 0 は常時解放。ステージ N は ステージ N-1 クリア済みで解放される。
    public sealed class TitleStageProgressServiceMB : MonoBehaviour
    {
        private const string ClearedKeyPrefix = "Stage_Cleared_";
        private const string BonusRewardKeyPrefix = "Stage_Reward_Bonus_";
        private const string FastClearRewardKeyPrefix = "Stage_Reward_FastClear_";

        public static TitleStageProgressServiceMB Instance { get; private set; }

        public readonly struct StageProgressData
        {
            public StageProgressData(bool isCleared, bool hasBonusReward, bool hasFastClearReward)
            {
                IsCleared = isCleared;
                HasBonusReward = hasBonusReward;
                HasFastClearReward = hasFastClearReward;
            }

            public bool IsCleared { get; }
            public bool HasBonusReward { get; }
            public bool HasFastClearReward { get; }
            public int EarnedRewardCount => (HasBonusReward ? 1 : 0) + (HasFastClearReward ? 1 : 0);
            public int TotalStarCount => (IsCleared ? 1 : 0) + (HasBonusReward ? 1 : 0) + (HasFastClearReward ? 1 : 0);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            // M2 で確立した singleton 規約に合わせ、破棄時に static 参照を畳む
            // (title scene 再ロード後に Instance が破棄済みオブジェクトを指し続けないように)。
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>インデックス <paramref name="stageIndex"/> が遊べる状態かどうかを返す。</summary>
        public bool IsUnlocked(int stageIndex)
        {
            return IsUnlockedPersisted(stageIndex);
        }

        public static bool IsUnlockedPersisted(int stageIndex)
        {
            if (stageIndex <= 0)
                return true;

            if (IsClearedPersisted(stageIndex))
                return true;

            return IsClearedPersisted(stageIndex - 1);
        }

        /// <summary>インデックス <paramref name="stageIndex"/> をクリア済みとしてマークする。</summary>
        public bool IsCleared(int stageIndex)
        {
            return IsClearedPersisted(stageIndex);
        }

        /// <summary>インデックス <paramref name="stageIndex"/> をクリア済みとして保存する。</summary>
        public void SetCleared(int stageIndex)
        {
            SetClearedPersisted(stageIndex);
        }

        public StageProgressData GetStageProgress(int stageIndex)
        {
            return GetStageProgressPersisted(stageIndex);
        }

        public static StageProgressData GetStageProgressPersisted(int stageIndex)
        {
            return new StageProgressData(
                IsClearedPersisted(stageIndex),
                HasBonusRewardPersisted(stageIndex),
                HasFastClearRewardPersisted(stageIndex));
        }

        public static bool IsClearedPersisted(int stageIndex)
        {
            if (stageIndex < 0)
                return false;

            return PlayerPrefs.GetInt(ClearedKeyPrefix + stageIndex, 0) == 1;
        }

        public static bool HasBonusRewardPersisted(int stageIndex)
        {
            if (stageIndex < 0)
                return false;

            return PlayerPrefs.GetInt(BonusRewardKeyPrefix + stageIndex, 0) == 1;
        }

        public static bool HasFastClearRewardPersisted(int stageIndex)
        {
            if (stageIndex < 0)
                return false;

            return PlayerPrefs.GetInt(FastClearRewardKeyPrefix + stageIndex, 0) == 1;
        }

        public static void SetClearedPersisted(int stageIndex)
        {
            if (stageIndex < 0)
                return;

            PlayerPrefs.SetInt(ClearedKeyPrefix + stageIndex, 1);
            PlayerPrefs.Save();
        }

        // ステージクリア結果を保存する。報酬フラグ(bonus / fastClear)は「一度獲得したら true のまま」の
        // ベストエバー方式で、再挑戦で未獲得でも消さない（= 既に獲得した星を奪わない）。
        // そのため false 上書きはせず、true のときだけ立てる。clear フラグも true で確定する。
        // 注意: PlayerPrefs.Save() は void で失敗を検知できないが、解放判定は IsClearedPersisted(N-1)
        // からも導けるため、単発の保存失敗は「ロック側」へ縮退する（誤って解放しっぱなしにはならない）。
        public static void SaveStageResultPersisted(int stageIndex, bool isBonusItem, bool isFastClear)
        {
            if (stageIndex < 0)
                return;

            PlayerPrefs.SetInt(ClearedKeyPrefix + stageIndex, 1);

            if (isBonusItem)
            {
                PlayerPrefs.SetInt(BonusRewardKeyPrefix + stageIndex, 1);
            }

            if (isFastClear)
            {
                PlayerPrefs.SetInt(FastClearRewardKeyPrefix + stageIndex, 1);
            }

            PlayerPrefs.Save();
        }

        /// <summary>デバッグ用: 全ステージのクリアデータを消去する。</summary>
        [ContextMenu("Debug: Reset All Stage Progress")]
        private void DebugResetAll()
        {
            for (int i = 0; i < 32; i++)
            {
                PlayerPrefs.DeleteKey(ClearedKeyPrefix + i);
                PlayerPrefs.DeleteKey(BonusRewardKeyPrefix + i);
                PlayerPrefs.DeleteKey(FastClearRewardKeyPrefix + i);
            }
            PlayerPrefs.Save();
            Debug.Log($"[{nameof(TitleStageProgressServiceMB)}] All stage progress reset.");
        }
    }
}
