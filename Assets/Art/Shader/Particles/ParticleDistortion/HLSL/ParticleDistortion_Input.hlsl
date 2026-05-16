#ifndef BC_PARTICLES_PARTICLE_DISTORTION_INPUT_INCLUDED
#define BC_PARTICLES_PARTICLE_DISTORTION_INPUT_INCLUDED

TEXTURE2D(_DistortionMap);
SAMPLER(sampler_DistortionMap);
TEXTURE2D(_NoiseMap);
SAMPLER(sampler_NoiseMap);

CBUFFER_START(UnityPerMaterial)
    float4 _DistortionMap_ST;
    float4 _NoiseMap_ST;
    float4 _DistortionScrollSpeed;

    float _BlendMode;
    float _DistortionStrength;
    float _DistortionScale;
    float _Alpha;
    float _UseVertexColor;
    float _EdgeFadePower;
    float _EdgeFadeStrength;
    float _NoiseStrength;
    float _NoiseScale;
CBUFFER_END

#endif // BC_PARTICLES_PARTICLE_DISTORTION_INPUT_INCLUDED