#ifndef BC_PARTICLES_TRAIL_UNLIT_SAMPLING_INCLUDED
#define BC_PARTICLES_TRAIL_UNLIT_SAMPLING_INCLUDED

half4 BC_TrailSampleBase(float2 uv)
{
    return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
}

half BC_TrailSampleNoise(float2 uv)
{
    return SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, uv).r;
}

float2 BC_TrailBuildBaseUV(float2 uv, float timeSeconds)
{
    float2 baseUV = uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
    return BC_TrailScrollUV(baseUV, _UVScrollSpeed.xy, timeSeconds);
}

float2 BC_TrailBuildNoiseUV(float2 uv, float timeSeconds)
{
    float noiseScale = BC_TrailSafePositive(_NoiseScale, 0.001);
    float2 noiseUV = uv * _NoiseMap_ST.xy + _NoiseMap_ST.zw;
    noiseUV *= noiseScale;
    return BC_TrailScrollUV(noiseUV, _NoiseScrollSpeed.xy, timeSeconds);
}

#endif // BC_PARTICLES_TRAIL_UNLIT_SAMPLING_INCLUDED
