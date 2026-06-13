using BC.UI.Title;
using unityroom.Api;
using UnityEngine;

namespace BC.Online
{
    // unityroom のスコアボードへ「獲得した星の合計数」をスコアとして送信するサービス。
    //
    // 配置: ApplicationKernelMB と同じ GameObject に付ける想定（その GameObject は
    // DontDestroyOnLoad なので、本サービスもシーンをまたいで常駐し、ゲーム中に星合計が
    // 変動した瞬間にスコアボードへ送れる）。
    //
    // スコア: TitleStageProgressServiceMB.GetTotalStarCount()（全ステージ星の合計, int）。
    //         星合計が変わるたび（クリア結果保存・進捗削除）に送信する。
    //
    // 送信先: unityroom の UnityroomApiClient（このライブラリのプレハブをシーンに別途配置し、
    //         HMAC 認証キーを設定しておく）。本サービスは静的 Instance 経由で呼ぶだけ。
    //
    // オンオフ: Itch.io など unityroom 以外でも公開するため enableScoreboard で切り替えられる。
    //           オフ（= Itch.io ビルド等）のときは購読も送信も一切行わない。
    public sealed class UnityroomScoreboardServiceMB : MonoBehaviour
    {
        [Header("Unityroom Scoreboard")]
        [Tooltip("スコアボード連携のオン/オフ。Itch.io など unityroom 以外で公開するビルドでは false にする。")]
        [SerializeField] private bool enableScoreboard = true;

        [Tooltip("送信先 unityroom スコアボードのボード番号（unityroom のゲーム設定画面で確認）。")]
        [SerializeField] private int scoreboardNo = 1;

        [Tooltip("スコアの書き込みモード。星合計は多いほど良いので通常は HighScoreDesc（降順ハイスコア）。")]
        [SerializeField] private ScoreboardWriteMode writeMode = ScoreboardWriteMode.HighScoreDesc;

        [Tooltip("起動時にも現在の星合計を一度送信して同期する。")]
        [SerializeField] private bool sendOnStart = true;

        /// <summary>スコアボード連携が有効か。</summary>
        public bool EnableScoreboard => enableScoreboard;

        private void OnEnable()
        {
            if (!enableScoreboard)
                return;

            // 星合計の変動を購読する（TitleStageProgressServiceMB がクリア結果保存・進捗削除で発火）。
            TitleStageProgressServiceMB.StarsChanged += OnStarsChanged;
        }

        private void OnDisable()
        {
            // 購読していなくても無害なので、無条件に解除して static イベントのリークを防ぐ。
            TitleStageProgressServiceMB.StarsChanged -= OnStarsChanged;
        }

        private void Start()
        {
            // 前回までに獲得済みの星合計を起動時にも同期しておく（HighScoreDesc なら同値/低値の再送は無害）。
            if (enableScoreboard && sendOnStart)
                SendCurrentScore();
        }

        private void OnStarsChanged()
        {
            SendCurrentScore();
        }

        /// <summary>現在の星合計を unityroom スコアボードへ送信する。連携オフ時は何もしない。</summary>
        public void SendCurrentScore()
        {
            if (!enableScoreboard)
                return;

            // UnityroomApiClient.Instance はシーンに無いと自前で LogError する（= プレハブ未配置の通知）。
            // ここで二重に警告は出さず、見つからなければ送信をスキップするだけにする。
            IUnityroomApiClient client = UnityroomApiClient.Instance;
            if (client == null)
                return;

            int totalStars = TitleStageProgressServiceMB.GetTotalStarCount();

            // SendScore の score 引数は float。星合計(int)はそのまま暗黙変換で渡す。
            client.SendScore(scoreboardNo, totalStars, writeMode);
        }
    }
}
