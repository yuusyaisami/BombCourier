#ifndef BC_PARTICLES_PARTICLE_DISTORTION_SAMPLING_INCLUDED
#define BC_PARTICLES_PARTICLE_DISTORTION_SAMPLING_INCLUDED

float2 BC_ParticleDistortionBuildDistortionUV(float2 rawUV, float timeSeconds)
{
    float distortionScale = BC_ParticleDistortionSafePositive(_DistortionScale, 0.01);
    float2 distortionUV = rawUV * _DistortionMap_ST.xy + _DistortionMap_ST.zw;
    distortionUV *= distortionScale;
    distortionUV += _DistortionScrollSpeed.xy * timeSeconds;
    return distortionUV;
}

float2 BC_ParticleDistortionBuildNoiseUV(float2 rawUV, float timeSeconds)
{
    float noiseScale = BC_ParticleDistortionSafePositive(_NoiseScale, 0.01);
    float2 noiseUV = rawUV * _NoiseMap_ST.xy + _NoiseMap_ST.zw;
    noiseUV *= noiseScale;
    noiseUV += _DistortionScrollSpeed.xy * timeSeconds * 0.5;
    return noiseUV;
}

float4 BC_ParticleDistortionSampleDistortion(float2 distortionUV)
{
    return SAMPLE_TEXTURE2D(_DistortionMap, sampler_DistortionMap, distortionUV);
}

float BC_ParticleDistortionSampleNoise(float2 noiseUV)
{
    return SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, noiseUV).r;
}

#endif // BC_PARTICLES_PARTICLE_DISTORTION_SAMPLING_INCLUDED