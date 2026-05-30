using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace BC.Audio
{
    public enum AudioObjectType
    {
        SE,
        BGM,
    }

    // AudioSource のラッパー。AudioSystemMB のプールから取り出して使う。
    // 非ループ SE は再生完了を自動検知して OnReturnToPool を発火する。
    [RequireComponent(typeof(AudioSource))]
    [DisallowMultipleComponent]
    public sealed class AudioObjectMB : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;

        private Tween volumeTween;
        private int remainingLoopCount = -1; // -1 = 無制限
        private bool initialized = false;

        public AudioObjectType Type { get; private set; }
        public bool IsPlaying => audioSource != null && audioSource.isPlaying;
        public AudioClip Clip => audioSource != null ? audioSource.clip : null;

        // このオブジェクトが再生しているクリップの基準音量。
        // AudioSystemMB がゲーム側の SE/BGM 音量と乗算して実効音量を算出するために使用する。
        public float BaseVolume { get; private set; } = 1f;

        public float Volume
        {
            get => audioSource != null ? audioSource.volume : 0f;
            set { if (audioSource != null) audioSource.volume = value; }
        }

        // AudioSystemMB がプール返却を受け取るイベント。
        // 引数は this 自身を渡すので受け手が判別できる。
        public event Action<AudioObjectMB> OnReturnToPool;

        private void Reset()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            initialized = true;
        }

        private void Update()
        {
            // 非ループ SE の自動プール返却。
            if (!initialized) return;
            if (Type != AudioObjectType.SE) return;
            if (audioSource == null || audioSource.isPlaying) return;
            if (audioSource.loop) return; // ループ中は自動返却しない。

            // 指定回数ループ中のカウントダウン。
            if (remainingLoopCount > 1)
            {
                remainingLoopCount--;
                audioSource.Play();
                return;
            }

            ReturnToPool();
        }

        // ─────────────────────────────────────────────────────────────────
        // 再生 API
        // ─────────────────────────────────────────────────────────────────

        // loopCount: -1 = 無制限ループ, 0 or 1 = 単発, 2+ = 指定回数ループ
        // baseVolume: AudioDataSO.BaseVolume。音量変化イベント時の再計算に使う。直接指定しない場合は 1f。
        // pitch: AudioSource.pitch と同義。1f = 通常速度。
        public void PlaySE(AudioClip clip, float volume, bool loop, int loopCount = 1, float pitch = 1f, float baseVolume = 1f)
        {
            KillVolumeTween();
            Type = AudioObjectType.SE;
            BaseVolume = baseVolume;
            remainingLoopCount = loop ? -1 : Mathf.Max(1, loopCount);
            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.pitch = pitch;
            audioSource.loop = loop;
            audioSource.Play();
        }

        public void PlayBGM(AudioClip clip, float volume, bool loop, float pitch = 1f, float baseVolume = 1f)
        {
            KillVolumeTween();
            Type = AudioObjectType.BGM;
            BaseVolume = baseVolume;
            remainingLoopCount = -1;
            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.pitch = pitch;
            audioSource.loop = loop;
            audioSource.Play();
        }

        public void StopImmediate()
        {
            KillVolumeTween();
            audioSource.Stop();
            audioSource.clip = null;
            ReturnToPool();
        }

        // フェードアウト完了後に自動でプールへ返却する。
        // Time.timeScale=0 中でも動作するように SetUpdate(true) を使用する。
        public async UniTask FadeOutAsync(float duration, CancellationToken ct = default)
        {
            KillVolumeTween();

            if (duration > 0f && audioSource != null)
            {
                float startVolume = audioSource.volume;
                volumeTween = DOTween.To(
                    () => audioSource.volume,
                    v => audioSource.volume = v,
                    0f,
                    duration
                ).SetUpdate(true);

                await volumeTween.AsyncWaitForCompletion().AsUniTask()
                    .AttachExternalCancellation(ct)
                    .SuppressCancellationThrow();
            }

            if (audioSource != null)
                audioSource.Stop();

            ReturnToPool();
        }

        // ─────────────────────────────────────────────────────────────────
        // プール返却
        // ─────────────────────────────────────────────────────────────────

        private void ReturnToPool()
        {
            KillVolumeTween();
            if (audioSource != null)
            {
                audioSource.Stop();
                audioSource.clip = null;
                audioSource.loop = false;
                audioSource.volume = 1f;
                audioSource.pitch = 1f;
            }
            BaseVolume = 1f;
            remainingLoopCount = -1;
            OnReturnToPool?.Invoke(this);
        }

        private void KillVolumeTween()
        {
            volumeTween?.Kill();
            volumeTween = null;
        }

        private void OnDestroy()
        {
            KillVolumeTween();
        }
    }
}
