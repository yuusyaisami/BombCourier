#ifndef BC_PARTICLES_PARTICLE_DISTORTION_SURFACE_INCLUDED
#define BC_PARTICLES_PARTICLE_DISTORTION_SURFACE_INCLUDED

half4 BC_ParticleDistortionBuildSurfaceColor(float2 rawUV, half4 vertexColor, float4 positionCS, float timeSeconds)
{
    float2 distortionUV = BC_ParticleDistortionBuildDistortionUV(rawUV, timeSeconds);
    float4 distortionSample = BC_ParticleDistortionSampleDistortion(distortionUV);
    float edgeFade = BC_ParticleDistortionBuildEdgeFade(rawUV);
    float alpha = BC_ParticleDistortionBuildAlpha(distortionSample.a, vertexColor, edgeFade);

    float noiseFactor = 1.0;
    if (_NoiseStrength > 0.001)
    {
        float sampledNoise = BC_ParticleDistortionSampleNoise(BC_ParticleDistortionBuildNoiseUV(rawUV, timeSeconds));
        noiseFactor = lerp(1.0, sampledNoise, saturate(_NoiseStrength));
    }

    float2 signedOffset = BC_ParticleDistortionBuildSignedOffset(distortionSample);
    float2 screenOffset = signedOffset * (_DistortionStrength * 0.02) * noiseFactor * alpha;
    float2 sceneUV = saturate(GetNormalizedScreenSpaceUV(positionCS) + screenOffset);

    half4 finalColor;
    finalColor.rgb = SampleSceneColor(sceneUV);
    finalColor.a = alpha;

    if (BC_ParticleDistortionUsesPremultiply(_BlendMode) > 0.5)
    {
        finalColor.rgb *= finalColor.a;
    }

    return finalColor;
}

#endif // BC_PARTICLES_PARTICLE_DISTORTION_SURFACE_INCLUDED