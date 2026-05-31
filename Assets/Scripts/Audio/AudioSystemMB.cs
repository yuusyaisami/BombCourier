using System.Collections.Generic;
using System.Threading;
using BC.Base;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace BC.Audio
{
    // AudioObjectMB のプールを持ち、SE と BGM を動的に管理するシステム。
    // DontDestroyOnLoad シングルトン。ApplicationKernel.KernelValueStore の
    // AppSettings.MusicVolume / SFXVolume を Watch して音量をリアルタイム反映する。
    //
    // BGM は同時に 1 つのみ再生できる。切り替え時は自動クロスフェードする。
    [DisallowMultipleComponent]
    public sealed class AudioSystemMB : MonoBehaviour
    {
        public static AudioSystemMB Instance { get; private set; }

        [SerializeField, Min(1)] private int poolSize = 16;
        [SerializeField] private AudioObjectMB audioObjectPrefab;

        private readonly Stack<AudioObjectMB> pool = new();
        private readonly HashSet<AudioObjectMB> activeSE = new();

        private AudioObjectMB activeBGM;
        private CancellationTokenSource bgmFadeCts;

        private ValueWatchHandle<float> musicVolumeHandle;
        private ValueWatchHandle<float> sfxVolumeHandle;
        private EventSubscription musicVolumeSub;
        private EventSubscription sfxVolumeSub;

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

            BuildPool();
        }

        private void Start()
        {
            KernelValueStoreService store = ApplicationKernelMB.Instance?.Kernel?.KernelValueStore;
            if (store == null)
            {
                Debug.LogWarning($"{nameof(AudioSystemMB)}: ApplicationKernel.KernelValueStore is not available. Volume sync disabled.", this);
                return;
            }

            musicVolumeHandle = store.GetHandle(ValueKeys.AppSettings.MusicVolume);
            sfxVolumeHandle = store.GetHandle(ValueKeys.AppSettings.SFXVolume);

            musicVolumeSub = musicVolumeHandle.Subscribe(OnMusicVolumeChanged);
            sfxVolumeSub = sfxVolumeHandle.Subscribe(OnSFXVolumeChanged);
        }

        private void OnDestroy()
        {
            musicVolumeSub?.Dispose();
            sfxVolumeSub?.Dispose();

            bgmFadeCts?.Cancel();
            bgmFadeCts?.Dispose();
            bgmFadeCts = null;

            if (Instance == this)
                Instance = null;
        }

        // ─────────────────────────────────────────────────────────────────
        // Public API — SE
        // ─────────────────────────────────────────────────────────────────

        // loopCount: 1 = 単発, 2+ = 指定回数ループ, -1 = 無制限ループ
        public void PlaySE(AudioClip clip, int loopCount = 1)
        {
            TryPlaySE(clip, loopCount);
        }

        // AudioDataSO を使った SE 再生。baseVolume と pitch を SO から読み取る。
        public void PlaySE(AudioDataSO data, int loopCount = 1)
        {
            TryPlaySE(data, loopCount);
        }

        // 再生成功可否を返す SE API。重要 SE でフォールバック判定したいときに使う。
        public bool TryPlaySE(AudioClip clip, int loopCount = 1)
        {
            if (clip == null)
                return false;

            AudioObjectMB obj = GetFromPool();
            if (obj == null)
            {
                Debug.LogWarning($"{nameof(AudioSystemMB)}: Pool exhausted. SE '{clip.name}' was not played.");
                return false;
            }

            float gameVolume = sfxVolumeHandle != null ? sfxVolumeHandle.CurrentValue : 1f;
            bool loop = loopCount < 0;
            obj.PlaySE(clip, gameVolume, loop, loopCount);
            activeSE.Add(obj);
            return true;
        }

        // 再生成功可否を返す SE API。重要 SE でフォールバック判定したいときに使う。
        public bool TryPlaySE(AudioDataSO data, int loopCount = 1)
        {
            if (data == null || data.Clip == null)
                return false;

            AudioObjectMB obj = GetFromPool();
            if (obj == null)
            {
                Debug.LogWarning($"{nameof(AudioSystemMB)}: Pool exhausted. SE '{data.Clip.name}' was not played.");
                return false;
            }

            float gameVolume = sfxVolumeHandle != null ? sfxVolumeHandle.CurrentValue : 1f;
            float effectiveVolume = data.BaseVolume * gameVolume;
            bool loop = loopCount < 0;
            obj.PlaySE(data.Clip, effectiveVolume, loop, loopCount, data.Pitch, data.BaseVolume);
            activeSE.Add(obj);
            return true;
        }

        public void StopAllSE()
        {
            foreach (AudioObjectMB obj in activeSE)
            {
                if (obj != null)
                    obj.StopImmediate();
            }
            // ReturnToPool コールバックで activeSE から除去される。
        }

        // ─────────────────────────────────────────────────────────────────
        // Public API — BGM
        // ─────────────────────────────────────────────────────────────────

        // loop: false のときは 1 回再生して停止する。
        // crossfadeDuration: 0 にすると即時切り替え。
        public void PlayBGM(AudioClip clip, bool loop = true, float crossfadeDuration = 1f)
        {
            if (clip == null) return;
            CrossfadeBGMAsync(clip, 1f, 1f, loop, crossfadeDuration).Forget();
        }

        // AudioDataSO を使った BGM 再生。baseVolume と pitch を SO から読み取る。
        public void PlayBGM(AudioDataSO data, bool loop = true, float crossfadeDuration = 1f)
        {
            if (data == null || data.Clip == null) return;
            CrossfadeBGMAsync(data.Clip, data.BaseVolume, data.Pitch, loop, crossfadeDuration).Forget();
        }

        public void StopBGM(float fadeDuration = 1f)
        {
            if (activeBGM == null) return;
            StopBGMAsync(fadeDuration).Forget();
        }

        /// <summary>
        /// アクティブ BGM の音量だけをフェードさせる。BGM を停止・プールに戻しません。
        /// Intro 演出中の BGM 一時消音 → 復帰に使います。
        /// </summary>
        public UniTask FadeBGMVolumeAsync(float targetVolume, float fadeDuration, CancellationToken ct = default)
        {
            if (activeBGM == null) return UniTask.CompletedTask;
            return FadeBGMVolumeInternalAsync(activeBGM, targetVolume, fadeDuration, ct);
        }

        // ─────────────────────────────────────────────────────────────────
        // BGM クロスフェード実装
        // ─────────────────────────────────────────────────────────────────

        private async UniTaskVoid CrossfadeBGMAsync(AudioClip clip, float baseVolume, float pitch, bool loop, float crossfadeDuration)
        {
            // 前のフェード操作をキャンセルして新しいトークンで進める。
            bgmFadeCts?.Cancel();
            bgmFadeCts?.Dispose();
            bgmFadeCts = new CancellationTokenSource();
            CancellationToken ct = bgmFadeCts.Token;

            // 既存 BGM をフェードアウト開始 (並走させる)。
            if (activeBGM != null)
            {
                AudioObjectMB fading = activeBGM;
                activeBGM = null;
                fading.FadeOutAsync(crossfadeDuration, ct).Forget();
            }

            // 新しい BGM をプールから取り出してフェードインする。
            AudioObjectMB newBGM = GetFromPool();
            if (newBGM == null)
            {
                Debug.LogWarning($"{nameof(AudioSystemMB)}: Pool exhausted. BGM '{clip.name}' was not played.");
                return;
            }

            float gameVolume = musicVolumeHandle != null ? musicVolumeHandle.CurrentValue : 1f;
            float targetVolume = baseVolume * gameVolume;
            newBGM.PlayBGM(clip, volume: 0f, loop: loop, pitch: pitch, baseVolume: baseVolume);
            activeBGM = newBGM;

            if (crossfadeDuration > 0f)
            {
                Tween fadeIn = DOTween.To(
                    () => newBGM.Volume,
                    v => newBGM.Volume = v,
                    targetVolume,
                    crossfadeDuration
                ).SetUpdate(true);

                await fadeIn.AsyncWaitForCompletion().AsUniTask()
                    .AttachExternalCancellation(ct)
                    .SuppressCancellationThrow();
            }
            else
            {
                newBGM.Volume = targetVolume;
            }
        }

        private async UniTaskVoid StopBGMAsync(float fadeDuration)
        {
            bgmFadeCts?.Cancel();
            bgmFadeCts?.Dispose();
            bgmFadeCts = new CancellationTokenSource();
            CancellationToken ct = bgmFadeCts.Token;

            AudioObjectMB fading = activeBGM;
            activeBGM = null;

            await fading.FadeOutAsync(fadeDuration, ct);
        }

        private static async UniTask FadeBGMVolumeInternalAsync(AudioObjectMB target, float targetVolume, float fadeDuration, CancellationToken ct)
        {
            if (fadeDuration <= 0f)
            {
                target.Volume = targetVolume;
                return;
            }

            Tween tween = DOTween.To(
                () => target.Volume,
                v => target.Volume = v,
                targetVolume,
                fadeDuration
            ).SetUpdate(true);

            await tween.AsyncWaitForCompletion().AsUniTask()
                .AttachExternalCancellation(ct)
                .SuppressCancellationThrow();
        }

        // ─────────────────────────────────────────────────────────────────
        // 音量変化ハンドラ
        // ─────────────────────────────────────────────────────────────────

        private void OnMusicVolumeChanged(float volume)
        {
            if (activeBGM != null)
                activeBGM.Volume = activeBGM.BaseVolume * volume;
        }

        private void OnSFXVolumeChanged(float volume)
        {
            foreach (AudioObjectMB obj in activeSE)
            {
                if (obj != null)
                    obj.Volume = obj.BaseVolume * volume;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // プール管理
        // ─────────────────────────────────────────────────────────────────

        private void BuildPool()
        {
            for (int i = 0; i < poolSize; i++)
            {
                AudioObjectMB obj = CreateAudioObject();
                obj.gameObject.SetActive(false);
                pool.Push(obj);
            }
        }

        private AudioObjectMB CreateAudioObject()
        {
            if (audioObjectPrefab != null)
            {
                AudioObjectMB obj = Instantiate(audioObjectPrefab, transform);
                obj.OnReturnToPool += HandleReturnToPool;
                return obj;
            }

            // プレハブ未設定の場合はランタイムで生成する。
            GameObject go = new GameObject("AudioObject");
            go.transform.SetParent(transform, false);
            AudioObjectMB created = go.AddComponent<AudioObjectMB>();
            created.OnReturnToPool += HandleReturnToPool;
            return created;
        }

        private AudioObjectMB GetFromPool()
        {
            AudioObjectMB obj;
            while (pool.Count > 0)
            {
                obj = pool.Pop();
                if (obj != null)
                {
                    obj.gameObject.SetActive(true);
                    return obj;
                }
            }
            return null;
        }

        private void HandleReturnToPool(AudioObjectMB obj)
        {
            activeSE.Remove(obj);

            if (obj == activeBGM)
                activeBGM = null;

            if (obj != null)
            {
                obj.gameObject.SetActive(false);
                pool.Push(obj);
            }
        }
    }
}
