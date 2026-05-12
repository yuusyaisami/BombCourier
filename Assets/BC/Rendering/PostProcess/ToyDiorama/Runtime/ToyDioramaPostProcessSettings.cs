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
        public float ShadowTintStrength { get => shadowTintStrength; set => shadowTintStrength = value; }
        public float MidTintStrength { get => midTintStrength; set => midTintStrength = value; }
        public float HighlightTintStrength { get => highlightTintStrength; set => highlightTintStrength = value; }
        public float CreamHighlightStrength { get => creamHighlightStrength; set => creamHighlightStrength = value; }
        public float CreamHighlightThreshold { get => creamHighlightThreshold; set => creamHighlightThreshold = value; }
        public float CreamHighlightSoftness { get => creamHighlightSoftness; set => creamHighlightSoftness = value; }

        public void ApplyToMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            material.SetFloat(ShaderIds.Enabled, enabled ? 1f : 0f);
            material.SetFloat(ShaderIds.QualityTier, (float)qualityTier);
            material.SetFloat(ShaderIds.DebugView, (float)debugView);
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
            material.SetFloat(ShaderIds.ShadowTintStrength, shadowTintStrength);
            material.SetFloat(ShaderIds.MidTintStrength, midTintStrength);
            material.SetFloat(ShaderIds.HighlightTintStrength, highlightTintStrength);
            material.SetFloat(ShaderIds.CreamHighlightStrength, creamHighlightStrength);
            material.SetFloat(ShaderIds.CreamHighlightThreshold, creamHighlightThreshold);
            material.SetFloat(ShaderIds.CreamHighlightSoftness, creamHighlightSoftness);
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
            public static readonly int ShadowTintStrength = Shader.PropertyToID("_ToyDioramaShadowTintStrength");
            public static readonly int MidTintStrength = Shader.PropertyToID("_ToyDioramaMidTintStrength");
            public static readonly int HighlightTintStrength = Shader.PropertyToID("_ToyDioramaHighlightTintStrength");
            public static readonly int CreamHighlightStrength = Shader.PropertyToID("_ToyDioramaCreamHighlightStrength");
            public static readonly int CreamHighlightThreshold = Shader.PropertyToID("_ToyDioramaCreamHighlightThreshold");
            public static readonly int CreamHighlightSoftness = Shader.PropertyToID("_ToyDioramaCreamHighlightSoftness");
        }
    }
}