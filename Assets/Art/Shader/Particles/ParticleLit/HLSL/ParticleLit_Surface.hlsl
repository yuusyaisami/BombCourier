#ifndef BC_PARTICLES_PARTICLE_LIT_SURFACE_INCLUDED
#define BC_PARTICLES_PARTICLE_LIT_SURFACE_INCLUDED

half4 BC_ParticleLitBuildSurfaceColor(float2 baseUV, half4 vertexColor, float3 normalWS, float4 tangentWS, float3 positionWS)
{
    half4 baseSample = BC_ParticleLitSampleBase(baseUV);
    half4 baseColor = baseSample * _BaseColor;

    UNITY_BRANCH
    if (_UseVertexColor > 0.5)
    {
        baseColor *= vertexColor;
    }

    float alpha = saturate(baseColor.a * saturate(_Alpha));
    float3 normalTS = BC_ParticleLitSampleNormalTS(baseUV);
    float3 resolvedNormalWS = BC_ParticleLitBuildNormalWS(normalTS, normalWS, tangentWS, positionWS);
    BC_ParticleLitLightingData lightingData = BC_ParticleLitBuildLightingData(baseColor.rgb, resolvedNormalWS, positionWS);

    half4 finalColor;
    finalColor.rgb = lightingData.litColor + lightingData.emission;
    finalColor.a = alpha;

    if (BC_ParticleLitUsesPremultiply(_BlendMode) > 0.5)
    {
        finalColor.rgb *= finalColor.a;
    }

    return finalColor;
}

#endif // BC_PARTICLES_PARTICLE_LIT_SURFACE_INCLUDED