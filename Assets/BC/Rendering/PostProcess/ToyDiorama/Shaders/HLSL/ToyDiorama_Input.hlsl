#ifndef BC_TOY_DIORAMA_INPUT_INCLUDED
#define BC_TOY_DIORAMA_INPUT_INCLUDED

TEXTURE2D(_ToyDioramaBlueNoiseTex);
SAMPLER(sampler_ToyDioramaBlueNoiseTex);

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
    float _ToyDioramaEdgeToneEnabled;
    float _ToyDioramaEdgeToneStrength;
    float _ToyDioramaEdgeToneRadius;
    float _ToyDioramaEdgeToneSoftness;
    float _ToyDioramaEdgeSaturationFade;
    float _ToyDioramaEdgeBrightnessOffset;
    float _ToyDioramaDepthHazeEnabled;
    float _ToyDioramaDepthAvailable;
    float _ToyDioramaDepthHazeStrength;
    float _ToyDioramaDepthHazeStart;
    float _ToyDioramaDepthHazeEnd;
    float _ToyDioramaDepthHazeSaturationFade;
    float _ToyDioramaDepthHazeBrightnessLift;
    float _ToyDioramaGrainEnabled;
    float _ToyDioramaGrainStrength;
    float _ToyDioramaGrainScale;
    float _ToyDioramaGrainResponse;
    float _ToyDioramaGrainTemporalStrength;

    float4 _ToyDioramaEdgeToneColor;
    float4 _ToyDioramaDepthHazeColor;
CBUFFER_END

#endif