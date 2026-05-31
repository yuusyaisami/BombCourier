using System;
using UnityEngine;
using UnityEngine.UI;

using Sirenix.OdinInspector;

namespace BC.Base
{
    public enum SpriteAnimationTargetKind
    {
        SpriteRenderer,
        Image,
    }

    public sealed class SpriteAnimationPlayerMB : MonoBehaviour
    {
        [Title("Target")]
        [EnumToggleButtons]
        [OnValueChanged(nameof(OnTargetKindChanged))]
        [SerializeField]
        private SpriteAnimationTargetKind targetKind = SpriteAnimationTargetKind.SpriteRenderer;

        [ShowIf(nameof(IsSpriteRendererTarget))]
        [Required]
        [SerializeField]
        private SpriteRenderer spriteRenderer;

        [ShowIf(nameof(IsImageTarget))]
        [Required]
        [SerializeField]
        private Image image;


        [ShowIf(nameof(IsInitialOnceToLoop))]
        [SerializeReference]
        [InlineProperty]

        private IAnimationSpriteClipSource initialLoopClip =
            new InlineAnimationSpriteClipSource();

        [Title("Playback")]
        [SerializeField]
        private bool playOnEnable = true;

        [SerializeField]
        private bool useUnscaledTime;

        [Min(0.0001f)]
        [SerializeField]
        private float globalSpeed = 1f;

        [SerializeField]
        private bool clearSpriteWhenStopped;
        [Title("Initial Animation")]
        [SerializeReference, ShowIf(nameof(playOnEnable))]
        [InlineProperty]
        private IAnimationSpriteClipSource initialClip =
            new InlineAnimationSpriteClipSource();

        [EnumToggleButtons]
        [SerializeField, ShowIf(nameof(playOnEnable))]
        private SpriteAnimationPlayMode initialPlayMode = SpriteAnimationPlayMode.Loop;

        private AnimationSpriteClip currentClip;
        private AnimationSpriteClip loopAfterClip;

        private SpriteAnimationPlayMode currentPlayMode;
        private bool hasCurrentClip;
        private bool hasLoopAfterClip;
        private bool isPlaying;

        private int frameIndex;
        private float frameTimer;

        public bool IsPlaying => isPlaying;
        public int CurrentFrameIndex => frameIndex;
        public SpriteAnimationPlayMode CurrentPlayMode => currentPlayMode;

        private bool IsSpriteRendererTarget => targetKind == SpriteAnimationTargetKind.SpriteRenderer;
        private bool IsImageTarget => targetKind == SpriteAnimationTargetKind.Image;
        private bool IsInitialOnceToLoop => initialPlayMode == SpriteAnimationPlayMode.OnceToLoop;

        private void Reset()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            image = GetComponent<Image>();

            if (spriteRenderer != null)
                targetKind = SpriteAnimationTargetKind.SpriteRenderer;
            else if (image != null)
                targetKind = SpriteAnimationTargetKind.Image;
        }

        private void OnEnable()
        {
            if (!playOnEnable)
                return;

            PlayInitial();
        }

        private void Update()
        {
            if (!isPlaying || !hasCurrentClip)
                return;

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            Tick(dt);
        }

        public void PlayInitial()
        {
            if (initialClip == null)
            {
                Stop();
                return;
            }

            if (!initialClip.TryGetClip(out AnimationSpriteClip clip))
            {
                Stop();
                return;
            }

            if (initialPlayMode == SpriteAnimationPlayMode.OnceToLoop)
            {
                AnimationSpriteClip loopClip = clip;

                if (initialLoopClip != null &&
                    initialLoopClip.TryGetClip(out AnimationSpriteClip resolvedLoopClip))
                {
                    loopClip = resolvedLoopClip;
                }

                PlayOnceToLoop(clip, loopClip);
                return;
            }

            Play(clip, initialPlayMode);
        }

        public void Play(
            AnimationSpriteClip clip,
            SpriteAnimationPlayMode playMode)
        {
            if (!clip.IsValid)
            {
                Stop();
                return;
            }

            currentClip = clip;
            currentPlayMode = playMode;
            hasCurrentClip = true;

            loopAfterClip = default;
            hasLoopAfterClip = false;

            isPlaying = true;
            frameIndex = 0;
            frameTimer = GetCurrentFrameDuration();

            ApplyFrame(0);
        }

        public void Play(
            IAnimationSpriteClipSource source,
            SpriteAnimationPlayMode playMode)
        {
            if (source == null || !source.TryGetClip(out AnimationSpriteClip clip))
            {
                Stop();
                return;
            }

            Play(clip, playMode);
        }

        public void PlayOnceToLoop(
            AnimationSpriteClip onceClip,
            AnimationSpriteClip loopClip)
        {
            if (!onceClip.IsValid)
            {
                Stop();
                return;
            }

            currentClip = onceClip;
            currentPlayMode = SpriteAnimationPlayMode.OnceToLoop;
            hasCurrentClip = true;

            loopAfterClip = loopClip;
            hasLoopAfterClip = loopClip.IsValid;

            isPlaying = true;
            frameIndex = 0;
            frameTimer = GetCurrentFrameDuration();

            ApplyFrame(0);
        }

        public void PlayOnceToLoop(
            IAnimationSpriteClipSource onceSource,
            IAnimationSpriteClipSource loopSource)
        {
            if (onceSource == null || !onceSource.TryGetClip(out AnimationSpriteClip onceClip))
            {
                Stop();
                return;
            }

            AnimationSpriteClip loopClip = onceClip;

            if (loopSource != null &&
                loopSource.TryGetClip(out AnimationSpriteClip resolvedLoopClip))
            {
                loopClip = resolvedLoopClip;
            }

            PlayOnceToLoop(onceClip, loopClip);
        }

        public void Stop()
        {
            isPlaying = false;
            hasCurrentClip = false;
            hasLoopAfterClip = false;
            frameIndex = 0;
            frameTimer = 0f;

            if (clearSpriteWhenStopped)
            {
                ApplySprite(null);
            }
        }

        public void Pause()
        {
            isPlaying = false;
        }

        public void Resume()
        {
            if (hasCurrentClip)
                isPlaying = true;
        }

        public void SetGlobalSpeed(float speed)
        {
            globalSpeed = Mathf.Max(0.0001f, speed);
        }

        private void Tick(float dt)
        {
            if (dt <= 0f)
                return;

            frameTimer -= dt;

            while (frameTimer <= 0f && isPlaying && hasCurrentClip)
            {
                AdvanceFrame();

                if (!isPlaying || !hasCurrentClip)
                    break;

                frameTimer += GetCurrentFrameDuration();
            }
        }

        private void AdvanceFrame()
        {
            int nextIndex = frameIndex + 1;

            if (nextIndex < currentClip.FrameCount)
            {
                frameIndex = nextIndex;
                ApplyFrame(frameIndex);
                return;
            }

            switch (currentPlayMode)
            {
                case SpriteAnimationPlayMode.Loop:
                    frameIndex = 0;
                    ApplyFrame(frameIndex);
                    return;

                case SpriteAnimationPlayMode.Once:
                    frameIndex = currentClip.FrameCount - 1;
                    ApplyFrame(frameIndex);
                    isPlaying = false;
                    return;

                case SpriteAnimationPlayMode.OnceToLoop:
                    if (hasLoopAfterClip)
                    {
                        currentClip = loopAfterClip;
                        currentPlayMode = SpriteAnimationPlayMode.Loop;
                        hasLoopAfterClip = false;
                        loopAfterClip = default;

                        frameIndex = 0;
                        ApplyFrame(frameIndex);
                        return;
                    }

                    frameIndex = currentClip.FrameCount - 1;
                    ApplyFrame(frameIndex);
                    isPlaying = false;
                    return;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private float GetCurrentFrameDuration()
        {
            float clipFrameDuration = currentClip.GetFrameDuration(frameIndex);
            float speed = globalSpeed <= 0f ? 1f : globalSpeed;

            return clipFrameDuration / speed;
        }

        private void ApplyFrame(int index)
        {
            Sprite sprite = currentClip.GetSprite(index);
            ApplySprite(sprite);
        }

        private void ApplySprite(Sprite sprite)
        {
            switch (targetKind)
            {
                case SpriteAnimationTargetKind.SpriteRenderer:
                    if (spriteRenderer == null)
                    {
                        Debug.LogError($"{nameof(SpriteAnimationPlayerMB)}: SpriteRenderer target is null.", this);
                        enabled = false;
                        return;
                    }

                    spriteRenderer.sprite = sprite;
                    return;

                case SpriteAnimationTargetKind.Image:
                    if (image == null)
                    {
                        Debug.LogError($"{nameof(SpriteAnimationPlayerMB)}: Image target is null.", this);
                        enabled = false;
                        return;
                    }

                    image.sprite = sprite;
                    return;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (globalSpeed <= 0f)
                globalSpeed = 1f;

            if (targetKind == SpriteAnimationTargetKind.SpriteRenderer)
            {
                if (spriteRenderer == null)
                    spriteRenderer = GetComponent<SpriteRenderer>();
            }
            else
            {
                if (image == null)
                    image = GetComponent<Image>();
            }
        }
#endif

        private void OnTargetKindChanged()
        {
            // Odin上で切り替えた時に片方だけ使う意図を明確にする。
            // 参照を消すと戻した時に再設定が面倒なので、ここでは消さない。
            // Inspector表示だけ ShowIf で片方を隠す。
        }

    }
}