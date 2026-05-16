#ifndef BC_PARTICLES_PARTICLE_UNLIT_INPUT_INCLUDED
#define BC_PARTICLES_PARTICLE_UNLIT_INPUT_INCLUDED

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
TEXTURE2D(_MaskMap);
SAMPLER(sampler_MaskMap);
TEXTURE2D(_NoiseMap);
SAMPLER(sampler_NoiseMap);

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _MaskMap_ST;
    float4 _NoiseMap_ST;
    half4 _BaseColor;
    half4 _EmissionColor;
    float4 _NoiseScrollSpeed;

    float _BlendMode;
    float _Alpha;
    float _Brightness;
    float _UseVertexColor;

    float _MaskStrength;
    float _NoiseStrength;
    float _NoiseScale;
    float _NoiseSpace;

    float _DissolveAmount;
    float _DissolveSoftness;

    float _EmissionStrength;
    float _EmissionAlphaInfluence;
    float _DebugMode;

    float _UseSoftParticles;
    float _SoftParticleDistance;
    float _UseCameraFade;
    float _CameraFadeNear;
    float _CameraFadeFar;

    float _SoftCircleStrength;
    float _EdgeFadePower;
    float _EdgeFadeStrength;
CBUFFER_END

#endif // BC_PARTICLES_PARTICLE_UNLIT_INPUT_INCLUDED