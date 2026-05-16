#ifndef BC_PARTICLES_PARTICLE_UNLIT_DEBUG_INCLUDED
#define BC_PARTICLES_PARTICLE_UNLIT_DEBUG_INCLUDED

struct BC_ParticleDebugData
{
    half4 finalColor;
    half4 baseColor;
    half4 vertexColor;
    half4 maskValue;
    half4 custom1;
    float2 rawUV;
    half noiseValue;
    half dissolveMask;
    half softCircle;
    half3 emission;
};

half4 BC_ParticlePackDebugScalar(half value)
{
    half clampedValue = saturate(value);
    return half4(clampedValue, clampedValue, clampedValue, 1.0);
}

half4 BC_ParticlePackSignedDebugVector3(half3 value)
{
    return half4(saturate(value * 0.5 + 0.5), 1.0);
}

half4 BC_ParticleBuildDebugColor(float debugMode, BC_ParticleDebugData debugData)
{
    int resolvedMode = (int)round(debugMode);

    // M8 は debug 表示でも追加サンプルを増やさず、既存の中間値だけを可視化する。
    if (resolvedMode == 1)
    {
        return half4(saturate(debugData.baseColor.rgb), 1.0);
    }

    if (resolvedMode == 2)
    {
        return BC_ParticlePackDebugScalar(debugData.baseColor.a);
    }

    if (resolvedMode == 3)
    {
        return half4(saturate(debugData.vertexColor.rgb), 1.0);
    }

    if (resolvedMode == 4)
    {
        return BC_ParticlePackDebugScalar(debugData.vertexColor.a);
    }

    if (resolvedMode == 5)
    {
        return BC_ParticlePackDebugScalar(debugData.maskValue.r);
    }

    if (resolvedMode == 6)
    {
        return BC_ParticlePackDebugScalar(debugData.maskValue.g);
    }

    if (resolvedMode == 7)
    {
        return BC_ParticlePackDebugScalar(debugData.maskValue.b);
    }

    if (resolvedMode == 8)
    {
        return BC_ParticlePackDebugScalar(debugData.maskValue.a);
    }

    if (resolvedMode == 9)
    {
        return BC_ParticlePackDebugScalar(debugData.noiseValue);
    }

    if (resolvedMode == 10)
    {
        return BC_ParticlePackDebugScalar(debugData.dissolveMask);
    }

    if (resolvedMode == 11)
    {
        return half4(saturate(debugData.emission), 1.0);
    }

    if (resolvedMode == 12)
    {
        return BC_ParticlePackDebugScalar(debugData.softCircle);
    }

    if (resolvedMode == 13)
    {
        return BC_ParticlePackSignedDebugVector3(debugData.custom1.xyz);
    }

    if (resolvedMode == 15)
    {
        return half4(frac(debugData.rawUV), 0.0, 1.0);
    }

    return debugData.finalColor;
}

#endif // BC_PARTICLES_PARTICLE_UNLIT_DEBUG_INCLUDED