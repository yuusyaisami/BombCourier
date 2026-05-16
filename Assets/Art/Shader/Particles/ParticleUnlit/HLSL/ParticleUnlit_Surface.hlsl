#ifndef BC_PARTICLES_PARTICLE_UNLIT_SURFACE_INCLUDED
#define BC_PARTICLES_PARTICLE_UNLIT_SURFACE_INCLUDED

half4 BC_ParticleBuildSurfaceColor(float2 baseUV, float2 rawUV, half4 vertexColor, float4 custom1, float3 positionWS, float4 positionCS, float timeSeconds)
{
    half4 baseSample = BC_ParticleSampleBase(baseUV);
    half4 baseColor = baseSample * _BaseColor;

    UNITY_BRANCH
    if (_UseVertexColor > 0.5)
    {
        baseColor *= vertexColor;
    }

    half4 maskValue = BC_ParticleSampleMask(BC_ParticleBuildMaskUV(rawUV));
    half noiseValue = BC_ParticleSampleNoise(BC_ParticleBuildNoiseUV(rawUV, timeSeconds, custom1.z));

    float alpha = baseColor.a * saturate(_Alpha);
    float maskStrength = saturate(_MaskStrength);
    float dissolveAmount = saturate(_DissolveAmount + custom1.x);
    float emissionStrength = max(_EmissionStrength + custom1.y, 0.0);
    // M4 では MaskMap の R/B/A を dissolve, noise influence, shape mask に固定する。
    float shapeMask = lerp(1.0, saturate(maskValue.a), maskStrength);
    // M5 の emission mask は _MaskStrength に依存させず、MaskMap.g の契約を常に維持する。
    float emissionMask = saturate(maskValue.g);
    float variationMask = lerp(1.0, saturate(maskValue.b), maskStrength);
    float noiseStrength = saturate(_NoiseStrength) * variationMask;
    float noiseInfluence = lerp(1.0, saturate(noiseValue), noiseStrength);
    float dissolveInput = lerp(saturate(noiseValue), saturate(noiseValue * maskValue.r), maskStrength);
    float dissolveMask = BC_ParticleCalculateDissolveMask(dissolveInput, dissolveAmount, _DissolveSoftness);
    float softCircle = BC_ParticleCalculateSoftCircle(rawUV, _SoftCircleStrength, _EdgeFadePower, _EdgeFadeStrength);
    float softParticleFade = BC_ParticleBuildSceneDepthFadeFactor(positionWS, positionCS, _UseSoftParticles, _SoftParticleDistance);
    float cameraFade = BC_ParticleBuildCameraFadeFactor(positionWS, _UseCameraFade, _CameraFadeNear, _CameraFadeFar);

    alpha *= shapeMask;
    alpha *= noiseInfluence;
    alpha *= dissolveMask;
    alpha *= softCircle;
    alpha *= softParticleFade;
    alpha *= cameraFade;

    half4 finalColor = baseColor;
    finalColor.a = saturate(alpha);
    finalColor.rgb *= max(_Brightness, 0.0);

    float emissionAlphaFactor = lerp(1.0, finalColor.a, saturate(_EmissionAlphaInfluence));
    half3 emission = baseSample.rgb
        * _EmissionColor.rgb
        * emissionStrength
        * emissionMask
        * emissionAlphaFactor;
    finalColor.rgb += emission;

    if (BC_ParticleUsesPremultiply(_BlendMode) > 0.5)
    {
        finalColor.rgb *= finalColor.a;
    }

    // M8 の debug view は final path と同じ sampled/intermediate data を可視化する。
    BC_ParticleDebugData debugData;
    debugData.finalColor = finalColor;
    debugData.baseColor = baseColor;
    debugData.vertexColor = vertexColor;
    debugData.maskValue = maskValue;
    debugData.custom1 = custom1;
    debugData.rawUV = rawUV;
    debugData.noiseValue = noiseValue;
    debugData.dissolveMask = dissolveMask;
    debugData.softCircle = softCircle;
    debugData.emission = emission;
    return BC_ParticleBuildDebugColor(_DebugMode, debugData);
}

#endif // BC_PARTICLES_PARTICLE_UNLIT_SURFACE_INCLUDED