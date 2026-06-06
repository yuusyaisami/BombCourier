#ifndef BC_ENVIRONMENT_STYLIZED_LIT_INPUT_INCLUDED
#define BC_ENVIRONMENT_STYLIZED_LIT_INPUT_INCLUDED

CBUFFER_START(UnityPerMaterial)
    // Base/Surface
    float4 _BaseColor;
    float4 _BaseMap_ST;
    float _FaceAlpha;
    float4 _EdgeColor;
    float _EdgeWidth;

    float _SurfaceMode;
    float _AlphaClip;
    float _Cutoff;
    float _Cull;
    float _SrcBlend;
    float _DstBlend;
    float _ZWrite;

    float _NormalScale;

    float _OcclusionStrength;

    float4 _EmissionColor;
    float _EmissionStrength;

    float _Metallic;
    float _Smoothness;
    float4 _SpecularColor;
    float _SpecularMode;
    float _SpecularStrength;
    float _SpecularStepCount;
    float _SpecularStepSmoothness;
    float _EdgeSheenStrength;
    float _EdgeSheenPower;
    float4 _EdgeSheenColor;

    float _LightStepCount;
    float _LightStepSmoothness;
    float _WrapLighting;
    float _BandContrast;
    float _BandOffset;

    float4 _DeepShadowColor;
    float4 _ShadowColor;
    float4 _MidColor;
    float4 _LightColor;
    float4 _HighlightColor;

    float _ShadowInfluence;
    float _ShadowSoftFill;
    float _ShadowColorBlend;
    float _ShadowStrength;

    float4 _AmbientTopColor;
    float4 _AmbientSideColor;
    float4 _AmbientBottomColor;
    float _AmbientStrength;

    float4 _BounceColor;
    float _BounceStrength;
    float4 _BounceDirection;
    float _BounceWrap;
    float4 _IndirectShadowColor;
    float _IndirectStrength;
    float _IndirectStylizeStrength;
    float _CavityStrength;
    float4 _CavityColor;

    float _AdditionalLightMode;
    float _AdditionalLightIntensity;
    float _AdditionalLightShadowInfluence;
    float _AdditionalLightColorInfluence;

    float _LightBandEmissionEnabled;
    float4 _LightBandEmissionColor;
    float _LightBandEmissionIntensity;
    float _LightBandEmissionMin;
    float _LightBandEmissionMax;
    float _LightBandEmissionFeather;
    float _LightBandEmissionStepMin;
    float _LightBandEmissionStepMax;
    float _LightBandEmissionBandStepBlend;
    float _LightBandEmissionAdditionalWeight;
    float _LightBandEmissionResponse;
    float _LightBandEmissionSpecialMaskInfluence;
    float _LightBandEmissionGradientInfluence;

    float _SimpleBoostEmissionEnabled;
    float4 _SimpleBoostEmissionColor;
    float _SimpleBoostEmissionIntensity;
    float _SimpleBoostFresnelStrength;
    float _SimpleBoostFresnelPower;
    float _SimpleBoostFresnelInvert;

    float _TriplanarBaseMapEnabled;
    float _TriplanarNormalMapEnabled;
    float _TriplanarNoiseEnabled;
    float _TriplanarScale;
    float _TriplanarBlendSharpness;

    float _VertexColorEnabled;
    float _VertexColorCavityStrength;
    float _VertexColorBandOffsetStrength;
    float _VertexColorColorVariationStrength;

    float _WorldYGradientEnabled;
    float4 _WorldYGradientTopColor;
    float4 _WorldYGradientBottomColor;
    float _WorldYGradientMin;
    float _WorldYGradientMax;
    float _WorldYGradientStrength;

    float _NoiseSpace;
    float _AlbedoNoiseStrength;
    float _WorldNoiseScale;
    float _WorldNoiseStrength;
    float _WorldNoiseContrast;
    float _LightBandNoiseStrength;
    float _LightBandNoiseScale;
    float _NoiseDistanceFadeStart;
    float _NoiseDistanceFadeEnd;

    float _DebugView;
CBUFFER_END

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);

TEXTURE2D(_OcclusionMap);
SAMPLER(sampler_OcclusionMap);

TEXTURE2D(_EmissionMap);
SAMPLER(sampler_EmissionMap);

// SurfaceMode判定ヘルパー群。C#側と同じ閾値規約を使います。
bool ESL_IsTransparentSurfaceMode()
{
    return _SurfaceMode > 0.5 && _SurfaceMode < 1.5;
}

bool ESL_IsEdgeOnlySurfaceMode()
{
    return _SurfaceMode > 1.5;
}

bool ESL_IsBlendedSurfaceMode()
{
    return _SurfaceMode > 0.5;
}

bool ESL_IsOpaqueSurfaceMode()
{
    return !ESL_IsBlendedSurfaceMode();
}

#endif