#ifndef BC_PARTICLES_PARTICLE_UNLIT_COMMON_INCLUDED
#define BC_PARTICLES_PARTICLE_UNLIT_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

float BC_ParticleSafePositive(float value, float fallbackValue)
{
    return max(value, fallbackValue);
}

float BC_ParticleUsesPremultiply(float blendMode)
{
    return step(1.5, blendMode);
}

float2 BC_ParticleScrollUV(float2 uv, float2 scrollSpeed, float timeSeconds)
{
    return uv + scrollSpeed * timeSeconds;
}

float2 BC_ParticleBuildCustomNoiseOffset(float customNoiseOffset)
{
    return float2(customNoiseOffset, customNoiseOffset * 0.6180339);
}

float BC_ParticleBuildCameraFadeFactor(float3 positionWS, float useCameraFade, float fadeNear, float fadeFar)
{
    if (useCameraFade < 0.5)
    {
        return 1.0;
    }

    float3 viewVector = _WorldSpaceCameraPos.xyz - positionWS;
    float cameraDistance = length(viewVector);
    float resolvedFadeNear = max(fadeNear, 0.0);
    float resolvedFadeFar = max(fadeFar, resolvedFadeNear + 0.001);
    return saturate((cameraDistance - resolvedFadeNear) / (resolvedFadeFar - resolvedFadeNear));
}

float BC_ParticleBuildSceneDepthFadeFactor(float3 positionWS, float4 positionCS, float useSoftParticles, float softParticleDistance)
{
    if (useSoftParticles < 0.5)
    {
        return 1.0;
    }

    float2 screenUV = GetNormalizedScreenSpaceUV(positionCS);
    float rawSceneDepth = SampleSceneDepth(screenUV);
    // Depth texture が unavailable な camera / pass では invalid sample を no-op 扱いに戻す。
    if (rawSceneDepth <= 0.0)
    {
        return 1.0;
    }

    float sceneEyeDepth = LinearEyeDepth(rawSceneDepth, _ZBufferParams);
    float particleEyeDepth = max(-TransformWorldToView(positionWS).z, 0.0);
    float depthDistance = BC_ParticleSafePositive(softParticleDistance, 0.001);
    return saturate((sceneEyeDepth - particleEyeDepth) / depthDistance);
}

// M4 の NoiseSpace は ParticleUV のみを実装し、他空間は将来拡張用に予約する。
float2 BC_ParticleBuildNoiseSpaceUV(float2 rawUV, float noiseSpace)
{
    return rawUV;
}

float BC_ParticleCalculateDissolveMask(float dissolveValue, float dissolveAmount, float dissolveSoftness)
{
    float amount = saturate(dissolveAmount);
    float softness = BC_ParticleSafePositive(dissolveSoftness, 0.001);
    float threshold = amount * (1.0 + softness) - softness;
    return saturate((dissolveValue - threshold) / softness);
}

// M2 では四角い板感を抑えることだけを責務にし、複雑な shape mode は入れない。
float BC_ParticleCalculateSoftCircle(float2 uv, float softCircleStrength, float edgeFadePower, float edgeFadeStrength)
{
    float2 centeredUV = uv * 2.0 - 1.0;
    float distanceToCenter = length(centeredUV);
    float circleMask = 1.0 - saturate(distanceToCenter);
    float softCircle = pow(circleMask, BC_ParticleSafePositive(edgeFadePower, 0.001));
    float edgeFade = lerp(1.0, saturate(softCircle), saturate(edgeFadeStrength));
    return lerp(1.0, edgeFade, saturate(softCircleStrength));
}

#endif // BC_PARTICLES_PARTICLE_UNLIT_COMMON_INCLUDED