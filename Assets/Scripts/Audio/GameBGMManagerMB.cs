using BC.Audio;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace BC.Managers
{
    // ゲーム全体の BGM 遷移を一元管理するコンポーネント。
    // DontDestroyOnLoad シングルトン。
    // AudioSystemMB の PlayBGM / StopBGM / FadeBGMVolumeAsync をラップして、
    // シーンやゲーム状態の変化に応じた BGM 切り替えを提供する。
    [DisallowMultipleComponent]
    public sealed class GameBGMManagerMB : MonoBehaviour
    {
        public static GameBGMManagerMB Instance { get; private set; }

        [Header("BGM Assets")]
        [Tooltip("タイトル画面の BGM です。")]
        [SerializeField] private AudioDataSO titleBGM;
        [Tooltip("通常ゲームプレイ中の BGM です。")]
        [SerializeField] private AudioDataSO gameplayBGM;
        [Tooltip("エンディング（クリア後のエピローグ〜クレジット）の BGM です。")]
        [SerializeField] private AudioDataSO endingBGM;

        [Header("Fade Settings")]
        [Tooltip("BGM 切り替え時のクロスフェード時間です。")]
        [SerializeField, Min(0f)] private float crossfadeDuration = 1.5f;
        [Tooltip("Intro カメラ演出開始時の BGM フェードアウト時間です。")]
        [SerializeField, Min(0f)] private float introFadeOutDuration = 0.6f;

        [Header("Intro SE")]
        [Tooltip("Intro カメラ演出の開始時に流れる SE です。")]
        [SerializeField] private AudioDataSO introCameraStartSE;

        // ─────────────────────────────────────────────────────────────────
        // ライフサイクル
        // ─────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ─────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────

        /// <summary>タイトル BGM を再生する。</summary>
        public void PlayTitleBGM()
        {
            AudioSystemMB audio = AudioSystemMB.Instance;
            if (audio == null) return;

            if (titleBGM != null)
                audio.PlayBGM(titleBGM, loop: true, crossfadeDuration: crossfadeDuration);
            else
                audio.StopBGM(crossfadeDuration);
        }

        /// <summary>ゲームプレイ BGM を再生する。</summary>
        public void PlayGameplayBGM()
        {
            AudioSystemMB audio = AudioSystemMB.Instance;
            if (audio == null) return;

            if (gameplayBGM != null)
                audio.PlayBGM(gameplayBGM, loop: true, crossfadeDuration: crossfadeDuration);
            else
                audio.StopBGM(crossfadeDuration);
        }

        /// <summary>エンディング BGM を再生する。</summary>
        public void PlayEndingBGM()
        {
            AudioSystemMB audio = AudioSystemMB.Instance;
            if (audio == null) return;

            if (endingBGM != null)
                audio.PlayBGM(endingBGM, loop: true, crossfadeDuration: crossfadeDuration);
            else
                audio.StopBGM(crossfadeDuration);
        }

        /// <summary>現在の BGM をフェードアウト停止する。</summary>
        public void StopBGM(float fadeOutDuration = 0.5f)
        {
            AudioSystemMB audio = AudioSystemMB.Instance;
            if (audio == null) return;

            audio.StopBGM(Mathf.Max(0f, fadeOutDuration));
        }

        /// <summary>
        /// Intro カメラ演出開始時の BGM 制御。
        /// BGM を introFadeOutDuration でフェードアウトして停止し、introCameraStartSE を再生する。
        /// Intro 終了後は SetupPlaying 状態で PlayGameplayBGM() が呼ばれることで BGM が再開される。
        /// </summary>
        public void StopBGMForIntro()
        {
            AudioSystemMB audio = AudioSystemMB.Instance;
            if (audio == null) return;
            if (introCameraStartSE == null) return;

            audio.StopBGM(introFadeOutDuration);

            if (introCameraStartSE != null)
                audio.PlaySE(introCameraStartSE);
        }
    }
}
