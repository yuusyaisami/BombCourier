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

        // 全ステージ星3コンプ特典パネルを「一度でも開いたか」を覚えるフラグキー。
        // 一度立てたら永続（= NEW バッジを二度と出さない）。進捗削除時のみ畳む。
        private const string AllClearRewardSeenKey = "AllClearReward_Seen";

        // 進捗削除時に走査するステージインデックスの上限（0..MaxStageKeyScan-1 を消す）。
        // 実ステージ数(現状12)より十分大きく取り、将来ステージが増えても取りこぼさない。
        // PlayerPrefs.DeleteKey は存在しないキーに対し no-op なので、多めに回しても安全・無害。
        private const int MaxStageKeyScan = 64;

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
            StarsChanged?.Invoke();
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
            StarsChanged?.Invoke();
        }

        // ─────────────────────────────────────────────────────────────────
        // 全ステージ星3コンプ特典 / セーブデータ削除
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 獲得した星の合計数が変化したときに発火する（クリア結果保存・進捗削除時）。
        /// オンラインスコアボード連携など、星合計を購読したい側がここを subscribe する。
        /// 注意: static イベントなので、購読側は破棄時に必ず unsubscribe すること（リーク防止）。
        /// </summary>
        public static event System.Action StarsChanged;

        /// <summary>
        /// 全ステージで獲得した星の合計数を返す（0..MaxStageKeyScan-1 を走査）。
        /// 各ステージ最大3つ（クリア＋ボーナス＋速度クリア）。実ステージ数を知らなくても
        /// 未獲得ステージは 0 星なので、多めに走査しても合計は変わらない。
        /// </summary>
        public static int GetTotalStarCount()
        {
            int total = 0;
            for (int i = 0; i < MaxStageKeyScan; i++)
            {
                total += GetStageProgressPersisted(i).TotalStarCount;
            }

            return total;
        }

        /// <summary>
        /// 0..stageCount-1 の全ステージが星3つ（クリア＋ボーナス＋速度クリアの全報酬）を
        /// 獲得済みかどうかを返す。1ステージでも未達なら false。
        /// stageCount が 0 以下（レジストリ未設定など）のときは判定不能として false を返す
        /// ＝「特典ボタンを出さない」安全側に倒す。
        /// </summary>
        public static bool AreAllStagesFullyCompleted(int stageCount)
        {
            if (stageCount <= 0)
                return false;

            for (int i = 0; i < stageCount; i++)
            {
                if (GetStageProgressPersisted(i).TotalStarCount != 3)
                    return false;
            }

            return true;
        }

        /// <summary>全ステージ星3コンプ特典パネルを既に開いたことがあるか。</summary>
        public static bool HasSeenAllClearReward()
        {
            return PlayerPrefs.GetInt(AllClearRewardSeenKey, 0) == 1;
        }

        /// <summary>特典パネルを開封済みとして記録する（以後 NEW バッジは出さない）。</summary>
        public static void MarkAllClearRewardSeen()
        {
            PlayerPrefs.SetInt(AllClearRewardSeenKey, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// ゲーム進捗のセーブデータを全消去する（クリア／ボーナス星／速度クリア星＋特典開封フラグ）。
        /// 設定系キー（Settings.* / 言語など）は対象外＝保持する。
        /// 注意: シーンは再読込しない。タイトル/ステージ選択は次回表示時に PlayerPrefs を読み直すため、
        /// その時点で星リセット・特典ボタン非表示が反映される。
        /// </summary>
        public static void DeleteAllProgress()
        {
            for (int i = 0; i < MaxStageKeyScan; i++)
            {
                PlayerPrefs.DeleteKey(ClearedKeyPrefix + i);
                PlayerPrefs.DeleteKey(BonusRewardKeyPrefix + i);
                PlayerPrefs.DeleteKey(FastClearRewardKeyPrefix + i);
            }

            PlayerPrefs.DeleteKey(AllClearRewardSeenKey);
            PlayerPrefs.Save();
            StarsChanged?.Invoke();
        }

        /// <summary>デバッグ用: 全ステージのクリアデータを消去する。</summary>
        [ContextMenu("Debug: Reset All Stage Progress")]
        private void DebugResetAll()
        {
            DeleteAllProgress();
            Debug.Log($"[{nameof(TitleStageProgressServiceMB)}] All stage progress reset.");
        }
    }
}
