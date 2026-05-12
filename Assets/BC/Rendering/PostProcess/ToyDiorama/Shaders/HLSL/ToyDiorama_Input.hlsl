#ifndef BC_TOY_DIORAMA_INPUT_INCLUDED
#define BC_TOY_DIORAMA_INPUT_INCLUDED

CBUFFER_START(UnityPerMaterial)
    float _ToyDioramaEnabled;
    float _ToyDioramaQualityTier;
    float _ToyDioramaDebugView;

    float _ToyDioramaExposure;
    float _ToyDioramaContrast;
    float _ToyDioramaSaturation;
    float _ToyDioramaBlackLift;
    float _ToyDioramaWhiteSoftClamp;
    float _ToyDioramaPastelStrength;
    float _ToyDioramaHighSaturationCompress;
    float _ToyDioramaPastelLuminanceBias;

    float4 _ToyDioramaShadowTint;
    float4 _ToyDioramaMidTint;
    float4 _ToyDioramaHighlightTint;
    float4 _ToyDioramaCreamHighlightColor;

    float _ToyDioramaShadowTintStrength;
    float _ToyDioramaMidTintStrength;
    float _ToyDioramaHighlightTintStrength;
    float _ToyDioramaCreamHighlightStrength;
    float _ToyDioramaCreamHighlightThreshold;
    float _ToyDioramaCreamHighlightSoftness;
CBUFFER_END

#endif