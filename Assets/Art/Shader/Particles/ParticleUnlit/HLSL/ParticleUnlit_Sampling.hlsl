#ifndef BC_PARTICLES_PARTICLE_UNLIT_SAMPLING_INCLUDED
#define BC_PARTICLES_PARTICLE_UNLIT_SAMPLING_INCLUDED

half4 BC_ParticleSampleBase(float2 uv)
{
    return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
}

half4 BC_ParticleSampleMask(float2 uv)
{
    return SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uv);
}

half BC_ParticleSampleNoise(float2 uv)
{
    return SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, uv).r;
}

float2 BC_ParticleBuildBaseUV(float2 uv)
{
    return uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
}

float2 BC_ParticleBuildMaskUV(float2 uv)
{
    return uv * _MaskMap_ST.xy + _MaskMap_ST.zw;
}

float2 BC_ParticleBuildNoiseUV(float2 rawUV, float timeSeconds, float customNoiseOffset)
{
    float noiseScale = BC_ParticleSafePositive(_NoiseScale, 0.001);
    float2 noiseSpaceUV = BC_ParticleBuildNoiseSpaceUV(rawUV, _NoiseSpace);
    float2 noiseUV = noiseSpaceUV * _NoiseMap_ST.xy + _NoiseMap_ST.zw;
    noiseUV *= noiseScale;
    noiseUV += BC_ParticleBuildCustomNoiseOffset(customNoiseOffset);
    return BC_ParticleScrollUV(noiseUV, _NoiseScrollSpeed.xy, timeSeconds);
}

#endif // BC_PARTICLES_PARTICLE_UNLIT_SAMPLING_INCLUDED