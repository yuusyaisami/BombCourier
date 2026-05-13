using System;
using UnityEngine;

namespace BC.Rendering
{
    [Serializable]
    public sealed class ToyDioramaPostProcessSettings
    {
        [SerializeField] private bool enabled = true;
        [SerializeField] private ToyDioramaQualityTier qualityTier = ToyDioramaQualityTier.Medium;
        [SerializeField] private ToyDioramaDebugView debugView = ToyDioramaDebugView.Off;

        [Header("Core Color Grade")]
        [SerializeField, Range(-4f, 4f)] private float exposure = 0f;
        [SerializeField, Range(0f, 3f)] private float contrast = 1f;
        [SerializeField, Range(0f, 2f)] private float saturation = 1f;
        [SerializeField, Range(0f, 1f)] private float blackLift = 0.08f;
        [SerializeField, Range(0f, 1f)] private float whiteSoftClamp = 0.25f;

        [Header("Pastel Compression")]
        [SerializeField, Range(0f, 1f)] private float pastelStrength = 0.30f;
        [SerializeField, Range(0f, 1f)] private float highSaturationCompress = 0.50f;
        [SerializeField, Range(-1f, 1f)] private float pastelLuminanceBias = 0.15f;

        [Header("Cream Highlight")]
        [SerializeField] private Color creamHighlightColor = new Color(1f, 0.98f, 0.93f, 1f);
        [SerializeField, Range(0f, 1f)] private float creamHighlightStrength = 0.25f;
        [SerializeField, Range(0f, 1f)] private float creamHighlightThreshold = 0.70f;
        [SerializeField, Range(0.001f, 1f)] private float creamHighlightSoftness = 0.10f;

        [Header("Edge Tone")]
        [SerializeField] private bool edgeToneEnabled = true;
        [SerializeField] private Color edgeToneColor = new Color(1f, 0.98f, 0.95f, 1f);
        [SerializeField, Range(0f, 1f)] private float edgeToneStrength = 0.12f;
        [SerializeField, Range(0f, 1f)] private float edgeToneRadius = 0.62f;
        [SerializeField, Range(0.001f, 1f)] private float edgeToneSoftness = 0.22f;
        [SerializeField, Range(0f, 1f)] private float edgeSaturationFade = 0.18f;
        [SerializeField, Range(-0.5f, 0.5f)] private float edgeBrightnessOffset = 0f;

        [Header("Depth Haze")]
        [SerializeField] private bool depthHazeEnabled = true;
        [SerializeField] private Color depthHazeColor = new Color(0.78f, 0.86f, 0.92f, 1f);
        [SerializeField, Range(0f, 1f)] private float depthHazeStrength = 0.10f;
        [SerializeField, Range(0f, 1f)] private float depthHazeStart = 0.45f;
        [SerializeField, Range(0f, 1f)] private float depthHazeEnd = 0.95f;
        [SerializeField, Range(0f, 1f)] private float depthHazeSaturationFade = 0.18f;
        [SerializeField, Range(0f, 0.5f)] private float depthHazeBrightnessLift = 0.04f;

        [Header("Soft Bloom")]
        [SerializeField] private bool softBloomEnabled = true;
        [SerializeField, Range(0f, 1f)] private float softBloomThreshold = 0.82f;
        [SerializeField, Range(0f, 1f)] private float softBloomSoftKnee = 0.18f;
        [SerializeField, Range(0f, 1f)] private float softBloomIntensity = 0.14f;
        [SerializeField, Range(0f, 1f)] private float softBloomRadius = 0.65f;
        [SerializeField] private Color softBloomTint = new Color(1f, 0.96f, 0.92f, 1f);

        [Header("Halation")]
        [SerializeField] private bool halationEnabled = true;
        [SerializeField, Range(0f, 1f)] private float halationStrength = 0.04f;
        [SerializeField] private Color halationColor = new Color(1f, 0.74f, 0.62f, 1f);
        [SerializeField, Range(0f, 1f)] private float halationThreshold = 0.88f;
        [SerializeField, Range(0f, 1f)] private float halationRadius = 0.55f;

        [Header("Clean Grain")]
        [SerializeField] private Texture2D blueNoiseTex;
        [SerializeField] private bool grainEnabled = true;
        [SerializeField, Range(0f, 0.2f)] private float grainStrength = 0.02f;
        [SerializeField, Range(0.25f, 8f)] private float grainScale = 1f;
        [SerializeField, Range(0f, 1f)] private float grainResponse = 0.60f;
        [SerializeField, Range(0f, 1f)] private float grainTemporalStrength = 0.10f;

        [Header("Tints")]
        [SerializeField] private Color shadowTint = new Color(0.46f, 0.50f, 0.68f, 1f);
        [SerializeField] private Color midTint = Color.white;
        [SerializeField] private Color highlightTint = new Color(1f, 0.95f, 0.86f, 1f);

        [Header("Tint Strength")]
        [SerializeField, Range(0f, 1f)] private float shadowTintStrength = 0.35f;
        [SerializeField, Range(0f, 1f)] private float midTintStrength = 0f;
        [SerializeField, Range(0f, 1f)] private float highlightTintStrength = 0.20f;

        public bool Enabled { get => enabled; set => enabled = value; }
        public ToyDioramaQualityTier QualityTier { get => qualityTier; set => qualityTier = value; }
        public ToyDioramaDebugView DebugView { get => debugView; set => debugView = value; }
        public float Exposure { get => exposure; set => exposure = value; }
        public float Contrast { get => contrast; set => contrast = value; }
        public float Saturation { get => saturation; set => saturation = value; }
        public float BlackLift { get => blackLift; set => blackLift = value; }
        public float WhiteSoftClamp { get => whiteSoftClamp; set => whiteSoftClamp = value; }
        public float PastelStrength { get => pastelStrength; set => pastelStrength = value; }
        public float HighSaturationCompress { get => highSaturationCompress; set => highSaturationCompress = value; }
        public float PastelLuminanceBias { get => pastelLuminanceBias; set => pastelLuminanceBias = value; }
        public Color ShadowTint { get => shadowTint; set => shadowTint = value; }
        public Color MidTint { get => midTint; set => midTint = value; }
        public Color HighlightTint { get => highlightTint; set => highlightTint = value; }
        public Color CreamHighlightColor { get => creamHighlightColor; set => creamHighlightColor = value; }
        public bool EdgeToneEnabled { get => edgeToneEnabled; set => edgeToneEnabled = value; }
        public Color EdgeToneColor { get => edgeToneColor; set => edgeToneColor = value; }
        public bool DepthHazeEnabled { get => depthHazeEnabled; set => depthHazeEnabled = value; }
        public Color DepthHazeColor { get => depthHazeColor; set => depthHazeColor = value; }
        public bool SoftBloomEnabled { get => softBloomEnabled; set => softBloomEnabled = value; }
        public Color SoftBloomTint { get => softBloomTint; set => softBloomTint = value; }
        public bool HalationEnabled { get => halationEnabled; set => halationEnabled = value; }
        public Color HalationColor { get => halationColor; set => halationColor = value; }
        public Texture2D BlueNoiseTex { get => blueNoiseTex; set => blueNoiseTex = value; }
        public bool GrainEnabled { get => grainEnabled; set => grainEnabled = value; }
        public float ShadowTintStrength { get => shadowTintStrength; set => shadowTintStrength = value; }
        public float MidTintStrength { get => midTintStrength; set => midTintStrength = value; }
        public float HighlightTintStrength { get => highlightTintStrength; set => highlightTintStrength = value; }
        public float CreamHighlightStrength { get => creamHighlightStrength; set => creamHighlightStrength = value; }
        public float CreamHighlightThreshold { get => creamHighlightThreshold; set => creamHighlightThreshold = value; }
        public float CreamHighlightSoftness { get => creamHighlightSoftness; set => creamHighlightSoftness = value; }
        public float EdgeToneStrength { get => edgeToneStrength; set => edgeToneStrength = value; }
        public float EdgeToneRadius { get => edgeToneRadius; set => edgeToneRadius = value; }
        public float EdgeToneSoftness { get => edgeToneSoftness; set => edgeToneSoftness = value; }
        public float EdgeSaturationFade { get => edgeSaturationFade; set => edgeSaturationFade = value; }
        public float EdgeBrightnessOffset { get => edgeBrightnessOffset; set => edgeBrightnessOffset = value; }
        public float DepthHazeStrength { get => depthHazeStrength; set => depthHazeStrength = value; }
        public float DepthHazeStart { get => depthHazeStart; set => depthHazeStart = value; }
        public float DepthHazeEnd { get => depthHazeEnd; set => depthHazeEnd = value; }
        public float DepthHazeSaturationFade { get => depthHazeSaturationFade; set => depthHazeSaturationFade = value; }
        public float DepthHazeBrightnessLift { get => depthHazeBrightnessLift; set => depthHazeBrightnessLift = value; }
        public float SoftBloomThreshold { get => softBloomThreshold; set => softBloomThreshold = value; }
        public float SoftBloomSoftKnee { get => softBloomSoftKnee; set => softBloomSoftKnee = value; }
        public float SoftBloomIntensity { get => softBloomIntensity; set => softBloomIntensity = value; }
        public float SoftBloomRadius { get => softBloomRadius; set => softBloomRadius = value; }
        public float HalationStrength { get => halationStrength; set => halationStrength = value; }
        public float HalationThreshold { get => halationThreshold; set => halationThreshold = value; }
        public float HalationRadius { get => halationRadius; set => halationRadius = value; }
        public float GrainStrength { get => grainStrength; set => grainStrength = value; }
        public float GrainScale { get => grainScale; set => grainScale = value; }
        public float GrainResponse { get => grainResponse; set => grainResponse = value; }
        public float GrainTemporalStrength { get => grainTemporalStrength; set => grainTemporalStrength = value; }

        public void CopyFrom(ToyDioramaPostProcessSettings other)
        {
            if (other == null)
            {
                return;
            }

            enabled = other.enabled;
            qualityTier = other.qualityTier;
            debugView = other.debugView;
            CopyAuthoringValuesFrom(other);
        }

        public void CopyAuthoringValuesFrom(ToyDioramaPostProcessSettings other)
        {
            if (other == null)
            {
                return;
            }

            exposure = other.exposure;
            contrast = other.contrast;
            saturation = other.saturation;
            blackLift = other.blackLift;
            whiteSoftClamp = other.whiteSoftClamp;
            pastelStrength = other.pastelStrength;
            highSaturationCompress = other.highSaturationCompress;
            pastelLuminanceBias = other.pastelLuminanceBias;
            creamHighlightColor = other.creamHighlightColor;
            creamHighlightStrength = other.creamHighlightStrength;
            creamHighlightThreshold = other.creamHighlightThreshold;
            creamHighlightSoftness = other.creamHighlightSoftness;
            edgeToneEnabled = other.edgeToneEnabled;
            edgeToneColor = other.edgeToneColor;
            edgeToneStrength = other.edgeToneStrength;
            edgeToneRadius = other.edgeToneRadius;
            edgeToneSoftness = other.edgeToneSoftness;
            edgeSaturationFade = other.edgeSaturationFade;
            edgeBrightnessOffset = other.edgeBrightnessOffset;
            depthHazeEnabled = other.depthHazeEnabled;
            depthHazeColor = other.depthHazeColor;
            depthHazeStrength = other.depthHazeStrength;
            depthHazeStart = other.depthHazeStart;
            depthHazeEnd = other.depthHazeEnd;
            depthHazeSaturationFade = other.depthHazeSaturationFade;
            depthHazeBrightnessLift = other.depthHazeBrightnessLift;
            softBloomEnabled = other.softBloomEnabled;
            softBloomThreshold = other.softBloomThreshold;
            softBloomSoftKnee = other.softBloomSoftKnee;
            softBloomIntensity = other.softBloomIntensity;
            softBloomRadius = other.softBloomRadius;
            softBloomTint = other.softBloomTint;
            halationEnabled = other.halationEnabled;
            halationStrength = other.halationStrength;
            halationColor = other.halationColor;
            halationThreshold = other.halationThreshold;
            halationRadius = other.halationRadius;
            blueNoiseTex = other.blueNoiseTex;
            grainEnabled = other.grainEnabled;
            grainStrength = other.grainStrength;
            grainScale = other.grainScale;
            grainResponse = other.grainResponse;
            grainTemporalStrength = other.grainTemporalStrength;
            shadowTint = other.shadowTint;
            midTint = other.midTint;
            highlightTint = other.highlightTint;
            shadowTintStrength = other.shadowTintStrength;
            midTintStrength = other.midTintStrength;
            highlightTintStrength = other.highlightTintStrength;
        }

        public void ApplyPreset(ToyDioramaPostProcessPreset preset)
        {
            if (preset == null)
            {
                return;
            }

            preset.ApplyTo(this);
        }

        public ToyDioramaDebugView GetResolvedDebugView()
        {
#if UNITY_EDITOR
            return debugView;
#else
            return Debug.isDebugBuild ? debugView : ToyDioramaDebugView.Off;
#endif
        }

        public bool IsDepthHazeEnabledForQuality()
        {
            return qualityTier != ToyDioramaQualityTier.Low && depthHazeEnabled && depthHazeStrength > 0f;
        }

        public bool IsSoftBloomEnabledForQuality()
        {
            return qualityTier != ToyDioramaQualityTier.Low && softBloomEnabled && softBloomIntensity > 0f;
        }

        public bool IsHalationEnabledForQuality()
        {
            return qualityTier >= ToyDioramaQualityTier.High && halationEnabled && halationStrength > 0f;
        }

        public bool IsGrainEnabledForQuality(Texture2D fallbackBlueNoiseTexture = null)
        {
            return qualityTier != ToyDioramaQualityTier.Low && grainEnabled && grainStrength > 0f;
        }

        public bool RequiresDepthTexture()
        {
            ToyDioramaDebugView resolvedDebugView = GetResolvedDebugView();

            return IsDepthHazeEnabledForQuality() ||
                resolvedDebugView == ToyDioramaDebugView.RawDepth ||
                resolvedDebugView == ToyDioramaDebugView.LinearDepth ||
                resolvedDebugView == ToyDioramaDebugView.DepthHazeMask ||
                resolvedDebugView == ToyDioramaDebugView.BeforeDepthHaze ||
                resolvedDebugView == ToyDioramaDebugView.AfterDepthHaze;
        }

        public bool RequiresBloomPass()
        {
            ToyDioramaDebugView resolvedDebugView = GetResolvedDebugView();
            return IsBloomDebugView(resolvedDebugView) || IsSoftBloomEnabledForQuality() || IsHalationEnabledForQuality();
        }

        public bool RequiresFinalCompositePass()
        {
            return !IsPreBloomDebugView(GetResolvedDebugView());
        }

        public int GetBloomDownsampleDivisor()
        {
            return qualityTier >= ToyDioramaQualityTier.High ? 2 : 4;
        }

        public int GetBloomBlurPassPairCount()
        {
            if (qualityTier == ToyDioramaQualityTier.Cinematic)
            {
                return 3;
            }

            return qualityTier == ToyDioramaQualityTier.High ? 2 : 1;
        }

        public int GetBloomRasterPassCount()
        {
            if (!RequiresBloomPass())
            {
                return 0;
            }

            ToyDioramaDebugView resolvedDebugView = GetResolvedDebugView();
            int passCount = 1;

            if (resolvedDebugView == ToyDioramaDebugView.BloomPrefilter)
            {
                return passCount;
            }

            passCount += GetBloomBlurPassPairCount() * 2;

            if (resolvedDebugView == ToyDioramaDebugView.BloomBlur)
            {
                return passCount;
            }

            return passCount + 1;
        }

        public int GetTotalRasterPassCount()
        {
            if (!enabled)
            {
                return 0;
            }

            int passCount = 1;
            passCount += GetBloomRasterPassCount();

            if (RequiresFinalCompositePass())
            {
                passCount++;
            }

            return passCount;
        }

        public static bool IsPreBloomDebugView(ToyDioramaDebugView view)
        {
            switch (view)
            {
                case ToyDioramaDebugView.SourceColor:
                case ToyDioramaDebugView.Luminance:
                case ToyDioramaDebugView.UV:
                case ToyDioramaDebugView.ShadowMask:
                case ToyDioramaDebugView.MidMask:
                case ToyDioramaDebugView.HighlightMask:
                case ToyDioramaDebugView.BeforeColorGrade:
                case ToyDioramaDebugView.AfterColorGrade:
                case ToyDioramaDebugView.PastelMask:
                case ToyDioramaDebugView.HighSaturationMask:
                case ToyDioramaDebugView.CreamHighlightMask:
                case ToyDioramaDebugView.BeforePastel:
                case ToyDioramaDebugView.AfterPastel:
                case ToyDioramaDebugView.RawDepth:
                case ToyDioramaDebugView.LinearDepth:
                case ToyDioramaDebugView.DepthHazeMask:
                case ToyDioramaDebugView.BeforeDepthHaze:
                case ToyDioramaDebugView.AfterDepthHaze:
                case ToyDioramaDebugView.BeforeBloom:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsBloomDebugView(ToyDioramaDebugView view)
        {
            switch (view)
            {
                case ToyDioramaDebugView.BloomPrefilter:
                case ToyDioramaDebugView.BloomBlur:
                case ToyDioramaDebugView.BloomComposite:
                case ToyDioramaDebugView.HalationMask:
                    return true;

                default:
                    return false;
            }
        }

        public void ApplyToMaterial(Material material, Texture2D fallbackBlueNoiseTexture = null)
        {
            if (material == null)
            {
                return;
            }

            ToyDioramaDebugView resolvedDebugView = GetResolvedDebugView();
            bool depthHazeEnabledForQuality = IsDepthHazeEnabledForQuality();
            bool softBloomEnabledForQuality = IsSoftBloomEnabledForQuality();
            bool halationEnabledForQuality = IsHalationEnabledForQuality();
            Texture2D resolvedBlueNoiseTexture = blueNoiseTex != null ? blueNoiseTex : fallbackBlueNoiseTexture;
            bool grainEnabledForQuality = IsGrainEnabledForQuality(fallbackBlueNoiseTexture);

            material.SetFloat(ShaderIds.Enabled, enabled ? 1f : 0f);
            material.SetFloat(ShaderIds.QualityTier, (float)qualityTier);
            material.SetFloat(ShaderIds.DebugView, (float)resolvedDebugView);
            material.SetFloat(ShaderIds.Exposure, exposure);
            material.SetFloat(ShaderIds.Contrast, contrast);
            material.SetFloat(ShaderIds.Saturation, saturation);
            material.SetFloat(ShaderIds.BlackLift, blackLift);
            material.SetFloat(ShaderIds.WhiteSoftClamp, whiteSoftClamp);
            material.SetFloat(ShaderIds.PastelStrength, pastelStrength);
            material.SetFloat(ShaderIds.HighSaturationCompress, highSaturationCompress);
            material.SetFloat(ShaderIds.PastelLuminanceBias, pastelLuminanceBias);
            material.SetColor(ShaderIds.ShadowTint, shadowTint);
            material.SetColor(ShaderIds.MidTint, midTint);
            material.SetColor(ShaderIds.HighlightTint, highlightTint);
            material.SetColor(ShaderIds.CreamHighlightColor, creamHighlightColor);
            material.SetFloat(ShaderIds.EdgeToneEnabled, edgeToneEnabled ? 1f : 0f);
            material.SetColor(ShaderIds.EdgeToneColor, edgeToneColor);
            material.SetFloat(ShaderIds.DepthHazeEnabled, depthHazeEnabledForQuality ? 1f : 0f);
            material.SetFloat(ShaderIds.DepthAvailable, 0f);
            material.SetColor(ShaderIds.DepthHazeColor, depthHazeColor);
            material.SetFloat(ShaderIds.SoftBloomEnabled, softBloomEnabledForQuality ? 1f : 0f);
            material.SetColor(ShaderIds.SoftBloomTint, softBloomTint);
            material.SetFloat(ShaderIds.HalationEnabled, halationEnabledForQuality ? 1f : 0f);
            material.SetColor(ShaderIds.HalationColor, halationColor);

            material.SetTexture(ShaderIds.BlueNoiseTex, resolvedBlueNoiseTexture);
            material.SetFloat(ShaderIds.GrainEnabled, grainEnabledForQuality ? 1f : 0f);
            material.SetFloat(ShaderIds.ShadowTintStrength, shadowTintStrength);
            material.SetFloat(ShaderIds.MidTintStrength, midTintStrength);
            material.SetFloat(ShaderIds.HighlightTintStrength, highlightTintStrength);
            material.SetFloat(ShaderIds.CreamHighlightStrength, creamHighlightStrength);
            material.SetFloat(ShaderIds.CreamHighlightThreshold, creamHighlightThreshold);
            material.SetFloat(ShaderIds.CreamHighlightSoftness, creamHighlightSoftness);
            material.SetFloat(ShaderIds.EdgeToneStrength, edgeToneStrength);
            material.SetFloat(ShaderIds.EdgeToneRadius, edgeToneRadius);
            material.SetFloat(ShaderIds.EdgeToneSoftness, edgeToneSoftness);
            material.SetFloat(ShaderIds.EdgeSaturationFade, edgeSaturationFade);
            material.SetFloat(ShaderIds.EdgeBrightnessOffset, edgeBrightnessOffset);
            material.SetFloat(ShaderIds.DepthHazeStrength, depthHazeStrength);
            material.SetFloat(ShaderIds.DepthHazeStart, depthHazeStart);
            material.SetFloat(ShaderIds.DepthHazeEnd, depthHazeEnd);
            material.SetFloat(ShaderIds.DepthHazeSaturationFade, depthHazeSaturationFade);
            material.SetFloat(ShaderIds.DepthHazeBrightnessLift, depthHazeBrightnessLift);
            material.SetFloat(ShaderIds.SoftBloomThreshold, softBloomThreshold);
            material.SetFloat(ShaderIds.SoftBloomSoftKnee, softBloomSoftKnee);
            material.SetFloat(ShaderIds.SoftBloomIntensity, softBloomIntensity);
            material.SetFloat(ShaderIds.SoftBloomRadius, softBloomRadius);
            material.SetFloat(ShaderIds.HalationStrength, halationStrength);
            material.SetFloat(ShaderIds.HalationThreshold, halationThreshold);
            material.SetFloat(ShaderIds.HalationRadius, halationRadius);
            material.SetFloat(ShaderIds.GrainStrength, grainStrength);
            material.SetFloat(ShaderIds.GrainScale, grainScale);
            material.SetFloat(ShaderIds.GrainResponse, grainResponse);
            material.SetFloat(ShaderIds.GrainTemporalStrength, grainTemporalStrength);
        }

        public void ApplyBloomToMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            material.SetFloat(ShaderIds.SoftBloomEnabled, IsSoftBloomEnabledForQuality() ? 1f : 0f);
            material.SetColor(ShaderIds.SoftBloomTint, softBloomTint);
            material.SetFloat(ShaderIds.HalationEnabled, IsHalationEnabledForQuality() ? 1f : 0f);
            material.SetColor(ShaderIds.HalationColor, halationColor);
            material.SetFloat(ShaderIds.SoftBloomThreshold, softBloomThreshold);
            material.SetFloat(ShaderIds.SoftBloomSoftKnee, softBloomSoftKnee);
            material.SetFloat(ShaderIds.SoftBloomIntensity, softBloomIntensity);
            material.SetFloat(ShaderIds.SoftBloomRadius, softBloomRadius);
            material.SetFloat(ShaderIds.HalationStrength, halationStrength);
            material.SetFloat(ShaderIds.HalationThreshold, halationThreshold);
            material.SetFloat(ShaderIds.HalationRadius, halationRadius);
        }

        public static class ShaderIds
        {
            public static readonly int Enabled = Shader.PropertyToID("_ToyDioramaEnabled");
            public static readonly int QualityTier = Shader.PropertyToID("_ToyDioramaQualityTier");
            public static readonly int DebugView = Shader.PropertyToID("_ToyDioramaDebugView");
            public static readonly int Exposure = Shader.PropertyToID("_ToyDioramaExposure");
            public static readonly int Contrast = Shader.PropertyToID("_ToyDioramaContrast");
            public static readonly int Saturation = Shader.PropertyToID("_ToyDioramaSaturation");
            public static readonly int BlackLift = Shader.PropertyToID("_ToyDioramaBlackLift");
            public static readonly int WhiteSoftClamp = Shader.PropertyToID("_ToyDioramaWhiteSoftClamp");
            public static readonly int PastelStrength = Shader.PropertyToID("_ToyDioramaPastelStrength");
            public static readonly int HighSaturationCompress = Shader.PropertyToID("_ToyDioramaHighSaturationCompress");
            public static readonly int PastelLuminanceBias = Shader.PropertyToID("_ToyDioramaPastelLuminanceBias");
            public static readonly int ShadowTint = Shader.PropertyToID("_ToyDioramaShadowTint");
            public static readonly int MidTint = Shader.PropertyToID("_ToyDioramaMidTint");
            public static readonly int HighlightTint = Shader.PropertyToID("_ToyDioramaHighlightTint");
            public static readonly int CreamHighlightColor = Shader.PropertyToID("_ToyDioramaCreamHighlightColor");
            public static readonly int EdgeToneEnabled = Shader.PropertyToID("_ToyDioramaEdgeToneEnabled");
            public static readonly int EdgeToneColor = Shader.PropertyToID("_ToyDioramaEdgeToneColor");
            public static readonly int DepthHazeEnabled = Shader.PropertyToID("_ToyDioramaDepthHazeEnabled");
            public static readonly int DepthAvailable = Shader.PropertyToID("_ToyDioramaDepthAvailable");
            public static readonly int DepthHazeColor = Shader.PropertyToID("_ToyDioramaDepthHazeColor");
            public static readonly int SoftBloomEnabled = Shader.PropertyToID("_ToyDioramaSoftBloomEnabled");
            public static readonly int SoftBloomTint = Shader.PropertyToID("_ToyDioramaSoftBloomTint");
            public static readonly int HalationEnabled = Shader.PropertyToID("_ToyDioramaHalationEnabled");
            public static readonly int HalationColor = Shader.PropertyToID("_ToyDioramaHalationColor");
            public static readonly int BlueNoiseTex = Shader.PropertyToID("_ToyDioramaBlueNoiseTex");
            public static readonly int GrainEnabled = Shader.PropertyToID("_ToyDioramaGrainEnabled");
            public static readonly int ShadowTintStrength = Shader.PropertyToID("_ToyDioramaShadowTintStrength");
            public static readonly int MidTintStrength = Shader.PropertyToID("_ToyDioramaMidTintStrength");
            public static readonly int HighlightTintStrength = Shader.PropertyToID("_ToyDioramaHighlightTintStrength");
            public static readonly int CreamHighlightStrength = Shader.PropertyToID("_ToyDioramaCreamHighlightStrength");
            public static readonly int CreamHighlightThreshold = Shader.PropertyToID("_ToyDioramaCreamHighlightThreshold");
            public static readonly int CreamHighlightSoftness = Shader.PropertyToID("_ToyDioramaCreamHighlightSoftness");
            public static readonly int EdgeToneStrength = Shader.PropertyToID("_ToyDioramaEdgeToneStrength");
            public static readonly int EdgeToneRadius = Shader.PropertyToID("_ToyDioramaEdgeToneRadius");
            public static readonly int EdgeToneSoftness = Shader.PropertyToID("_ToyDioramaEdgeToneSoftness");
            public static readonly int EdgeSaturationFade = Shader.PropertyToID("_ToyDioramaEdgeSaturationFade");
            public static readonly int EdgeBrightnessOffset = Shader.PropertyToID("_ToyDioramaEdgeBrightnessOffset");
            public static readonly int DepthHazeStrength = Shader.PropertyToID("_ToyDioramaDepthHazeStrength");
            public static readonly int DepthHazeStart = Shader.PropertyToID("_ToyDioramaDepthHazeStart");
            public static readonly int DepthHazeEnd = Shader.PropertyToID("_ToyDioramaDepthHazeEnd");
            public static readonly int DepthHazeSaturationFade = Shader.PropertyToID("_ToyDioramaDepthHazeSaturationFade");
            public static readonly int DepthHazeBrightnessLift = Shader.PropertyToID("_ToyDioramaDepthHazeBrightnessLift");
            public static readonly int SoftBloomThreshold = Shader.PropertyToID("_ToyDioramaSoftBloomThreshold");
            public static readonly int SoftBloomSoftKnee = Shader.PropertyToID("_ToyDioramaSoftBloomSoftKnee");
            public static readonly int SoftBloomIntensity = Shader.PropertyToID("_ToyDioramaSoftBloomIntensity");
            public static readonly int SoftBloomRadius = Shader.PropertyToID("_ToyDioramaSoftBloomRadius");
            public static readonly int HalationStrength = Shader.PropertyToID("_ToyDioramaHalationStrength");
            public static readonly int HalationThreshold = Shader.PropertyToID("_ToyDioramaHalationThreshold");
            public static readonly int HalationRadius = Shader.PropertyToID("_ToyDioramaHalationRadius");
            public static readonly int GrainStrength = Shader.PropertyToID("_ToyDioramaGrainStrength");
            public static readonly int GrainScale = Shader.PropertyToID("_ToyDioramaGrainScale");
            public static readonly int GrainResponse = Shader.PropertyToID("_ToyDioramaGrainResponse");
            public static readonly int GrainTemporalStrength = Shader.PropertyToID("_ToyDioramaGrainTemporalStrength");
        }
    }
}