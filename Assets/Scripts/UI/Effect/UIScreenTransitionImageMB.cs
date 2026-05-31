using System;
using System.Threading;
using BC.Rendering.Transition;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace BC.UI.Effect
{
    [DisallowMultipleComponent]
    public sealed class UIScreenTransitionImageMB : MonoBehaviour
    {
        private static readonly int TextureFromId = Shader.PropertyToID("_TextureFrom");
        private static readonly int TextureToId = Shader.PropertyToID("_TextureTo");
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int ModeId = Shader.PropertyToID("_Mode");
        private static readonly int FeatherId = Shader.PropertyToID("_Feather");
        private static readonly int DirectionId = Shader.PropertyToID("_Direction");
        private static readonly int CenterId = Shader.PropertyToID("_Center");
        private static readonly int AspectId = Shader.PropertyToID("_Aspect");
        private static readonly int NoiseTexId = Shader.PropertyToID("_NoiseTex");
        private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
        private static readonly int NoiseStrengthId = Shader.PropertyToID("_NoiseStrength");
        private static readonly int SeedId = Shader.PropertyToID("_Seed");
        private static readonly int FromUvScaleOffsetId = Shader.PropertyToID("_FromUvScaleOffset");
        private static readonly int ToUvScaleOffsetId = Shader.PropertyToID("_ToUvScaleOffset");

        private static readonly Vector4 IdentityUvScaleOffset = new(1f, 1f, 0f, 0f);
        private const string TransitionShaderName = "Hidden/BC/UI/ScreenTransitionImage";

        [Header("References")]
        [SerializeField] private RawImage targetImage;
        [SerializeField] private Shader transitionShader;

        [Header("Defaults")]
        [SerializeField] private ScreenTransitionProfileSO defaultProfile;
        [SerializeField] private bool useUnscaledTime = true;

        private Material runtimeMaterial;
        private Sprite currentSprite;
        private CancellationTokenSource activeTransitionCts;
        private int transitionVersion;

        public Sprite CurrentSprite => currentSprite;

        private void Awake()
        {
            EnsureTargetImage();
        }

        private void OnDestroy()
        {
            CancelActiveTransition();

            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
                runtimeMaterial = null;
            }
        }

        public void SetDefaultProfile(ScreenTransitionProfileSO profile)
        {
            defaultProfile = profile;
        }

        public bool SetImmediateSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                Debug.LogError($"[{nameof(UIScreenTransitionImageMB)}] SetImmediateSprite requires non-null sprite.", this);
                return false;
            }

            CancelActiveTransition();
            transitionVersion++;

            if (!EnsureTargetImage())
                return false;

            ReleaseTransitionMaterial();
            ApplyStaticSprite(sprite);
            currentSprite = sprite;
            return true;
        }

        public async UniTask TransitionToSpriteAsync(
            Sprite toSprite,
            float? overrideDuration = null,
            ScreenTransitionProfileSO profile = null,
            CancellationToken ct = default)
        {
            if (toSprite == null)
            {
                Debug.LogError($"[{nameof(UIScreenTransitionImageMB)}] Transition target sprite is null.", this);
                return;
            }

            if (!EnsureTargetImage())
                return;

            if (currentSprite == null)
            {
                SetImmediateSprite(toSprite);
                return;
            }

            if (currentSprite == toSprite)
            {
                SetImmediateSprite(toSprite);
                return;
            }

            ScreenTransitionProfileSO resolvedProfile = profile != null ? profile : defaultProfile;
            float duration = overrideDuration ?? (resolvedProfile != null ? resolvedProfile.Duration : 0.25f);
            duration = Mathf.Max(0f, duration);

            CancelActiveTransition();
            int expectedVersion = ++transitionVersion;
            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            activeTransitionCts = linkedCts;
            CancellationToken linkedToken = linkedCts.Token;

            try
            {
                if (!EnsureRuntimeMaterial())
                    return;

                ApplyProfile(resolvedProfile);
                ApplySprite(TextureFromId, FromUvScaleOffsetId, currentSprite);
                ApplySprite(TextureToId, ToUvScaleOffsetId, toSprite);
                runtimeMaterial.SetFloat(AspectId, ResolveAspect());
                ApplyTransitionMaterial();

                if (duration <= 0f)
                {
                    if (expectedVersion != transitionVersion)
                        return;

                    currentSprite = toSprite;
                    ApplyStaticSprite(currentSprite);
                    ReleaseTransitionMaterial();
                    return;
                }

                float elapsed = 0f;
                while (elapsed < duration)
                {
                    linkedToken.ThrowIfCancellationRequested();

                    elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float eased = EvaluateEasing(resolvedProfile != null ? resolvedProfile.Easing : null, t);
                    runtimeMaterial.SetFloat(ProgressId, eased);

                    await UniTask.Yield(PlayerLoopTiming.Update, linkedToken);
                }

                if (expectedVersion != transitionVersion)
                    return;

                currentSprite = toSprite;
                ApplyStaticSprite(currentSprite);
                ReleaseTransitionMaterial();
            }
            catch (OperationCanceledException)
            {
                if (expectedVersion != transitionVersion)
                    return;

                if (currentSprite != null)
                    ApplyStaticSprite(currentSprite);

                ReleaseTransitionMaterial();
            }
            finally
            {
                if (ReferenceEquals(activeTransitionCts, linkedCts))
                {
                    activeTransitionCts.Dispose();
                    activeTransitionCts = null;
                }
            }
        }

        private bool EnsureTargetImage()
        {
            if (targetImage == null)
                targetImage = GetComponent<RawImage>();

            if (targetImage == null)
            {
                Debug.LogError($"[{nameof(UIScreenTransitionImageMB)}] RawImage reference is missing.", this);
                return false;
            }

            targetImage.raycastTarget = false;
            return true;
        }

        private bool EnsureRuntimeMaterial()
        {
            if (!EnsureTargetImage())
                return false;

            if (transitionShader == null)
                transitionShader = Shader.Find(TransitionShaderName);

            if (transitionShader == null)
            {
                Debug.LogError($"[{nameof(UIScreenTransitionImageMB)}] Shader '{TransitionShaderName}' not found.", this);
                return false;
            }

            if (runtimeMaterial == null)
            {
                runtimeMaterial = new Material(transitionShader)
                {
                    name = $"{nameof(UIScreenTransitionImageMB)}_RuntimeMaterial",
                    hideFlags = HideFlags.DontSave
                };
                runtimeMaterial.SetVector(FromUvScaleOffsetId, IdentityUvScaleOffset);
                runtimeMaterial.SetVector(ToUvScaleOffsetId, IdentityUvScaleOffset);
            }

            return true;
        }

        private void ApplyProfile(ScreenTransitionProfileSO profile)
        {
            ScreenTransitionMode mode = profile != null ? profile.Mode : ScreenTransitionMode.SmoothCrossFade;
            runtimeMaterial.SetInt(ModeId, (int)mode);
            runtimeMaterial.SetFloat(ProgressId, 0f);

            float feather = profile != null ? profile.Feather : 0.04f;
            runtimeMaterial.SetFloat(FeatherId, feather);

            Vector2 direction = profile != null ? profile.Direction : Vector2.right;
            runtimeMaterial.SetVector(DirectionId, new Vector4(direction.x, direction.y, 0f, 0f));

            Vector2 center = profile != null ? profile.Center : new Vector2(0.5f, 0.5f);
            runtimeMaterial.SetVector(CenterId, new Vector4(center.x, center.y, 0f, 0f));

            runtimeMaterial.SetFloat(NoiseScaleId, profile != null ? profile.NoiseScale : 8f);
            runtimeMaterial.SetFloat(NoiseStrengthId, profile != null ? profile.NoiseStrength : 1f);
            runtimeMaterial.SetFloat(SeedId, profile != null ? profile.Seed : 0f);

            Texture2D noiseTexture = profile != null ? profile.NoiseTexture : null;
            runtimeMaterial.SetTexture(NoiseTexId, noiseTexture != null ? noiseTexture : Texture2D.whiteTexture);
        }

        private void ApplyStaticSprite(Sprite sprite)
        {
            targetImage.texture = sprite.texture;
            targetImage.uvRect = ResolveRawImageUvRect(sprite);
        }

        private void ApplyTransitionMaterial()
        {
            if (targetImage.material != runtimeMaterial)
                targetImage.material = runtimeMaterial;
        }

        private void ReleaseTransitionMaterial()
        {
            if (targetImage != null && targetImage.material != null)
                targetImage.material = null;
        }

        private static float EvaluateEasing(AnimationCurve curve, float t)
        {
            if (curve == null || curve.length == 0)
                return t;

            return Mathf.Clamp01(curve.Evaluate(t));
        }

        private float ResolveAspect()
        {
            RectTransform rectTransform = targetImage != null ? targetImage.rectTransform : null;
            if (rectTransform == null)
                return 1f;

            Rect rect = rectTransform.rect;
            return rect.height > 0.001f ? rect.width / rect.height : 1f;
        }

        private void ApplySprite(int texturePropertyId, int uvPropertyId, Sprite sprite)
        {
            Texture texture = sprite != null ? sprite.texture : Texture2D.whiteTexture;
            runtimeMaterial.SetTexture(texturePropertyId, texture);
            runtimeMaterial.SetVector(uvPropertyId, ResolveUvScaleOffset(sprite));
        }

        private static Vector4 ResolveUvScaleOffset(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
                return IdentityUvScaleOffset;

            Texture texture = sprite.texture;
            Rect textureRect = sprite.textureRect;
            float textureWidth = Mathf.Max(1f, texture.width);
            float textureHeight = Mathf.Max(1f, texture.height);

            Vector2 scale = new(textureRect.width / textureWidth, textureRect.height / textureHeight);
            Vector2 offset = new(textureRect.x / textureWidth, textureRect.y / textureHeight);
            return new Vector4(scale.x, scale.y, offset.x, offset.y);
        }

        private static Rect ResolveRawImageUvRect(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
                return new Rect(0f, 0f, 1f, 1f);

            Texture texture = sprite.texture;
            Rect textureRect = sprite.textureRect;
            float textureWidth = Mathf.Max(1f, texture.width);
            float textureHeight = Mathf.Max(1f, texture.height);
            return new Rect(
                textureRect.x / textureWidth,
                textureRect.y / textureHeight,
                textureRect.width / textureWidth,
                textureRect.height / textureHeight);
        }

        private void CancelActiveTransition()
        {
            if (activeTransitionCts == null)
                return;

            activeTransitionCts.Cancel();
            activeTransitionCts.Dispose();
            activeTransitionCts = null;
        }
    }
}
