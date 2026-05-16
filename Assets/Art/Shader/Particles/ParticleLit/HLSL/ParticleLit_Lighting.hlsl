#ifndef BC_PARTICLES_PARTICLE_LIT_LIGHTING_INCLUDED
#define BC_PARTICLES_PARTICLE_LIT_LIGHTING_INCLUDED

struct BC_ParticleLitLightingData
{
    float3 baseColor;
    float3 litColor;
    float3 diffuse;
    float3 ambient;
    float3 specular;
    float3 emission;
};

float3 BC_ParticleLitEvaluateAmbient(float3 albedo)
{
    return albedo * 0.18;
}

float3 BC_ParticleLitEvaluateSpecular(float3 normalWS, float3 viewDirectionWS, float3 lightDirectionWS, float3 lightColor)
{
    float3 halfVector = BC_ParticleLitSafeNormalize(viewDirectionWS + lightDirectionWS, viewDirectionWS);
    float specularExponent = exp2(1.0 + saturate(_Smoothness) * 10.0);
    float specularMask = pow(saturate(dot(normalWS, halfVector)), specularExponent);
    float metallicInfluence = lerp(0.08, 1.0, saturate(_Metallic));
    return lightColor * specularMask * saturate(_Smoothness) * metallicInfluence;
}

BC_ParticleLitLightingData BC_ParticleLitBuildLightingData(float3 albedo, float3 normalWS, float3 positionWS)
{
    BC_ParticleLitLightingData lightingData;

    float3 viewDirectionWS = BC_ParticleLitSafeNormalize(GetWorldSpaceViewDir(positionWS), float3(0.0, 0.0, 1.0));
    Light mainLight = GetMainLight();
    float diffuseTerm = saturate(dot(normalWS, mainLight.direction));
    float3 diffuse = albedo * mainLight.color * mainLight.distanceAttenuation * mainLight.shadowAttenuation * diffuseTerm;
    float3 ambient = BC_ParticleLitEvaluateAmbient(albedo);
    float3 specular = BC_ParticleLitEvaluateSpecular(normalWS, viewDirectionWS, mainLight.direction, mainLight.color * mainLight.distanceAttenuation * mainLight.shadowAttenuation);
    float3 emission = _EmissionColor.rgb * _EmissionStrength;
    float3 baseColor = albedo;
    float3 litColor = diffuse + ambient + specular;

    lightingData.baseColor = baseColor;
    lightingData.litColor = lerp(baseColor, litColor, saturate(_LightInfluence));
    lightingData.diffuse = diffuse;
    lightingData.ambient = ambient;
    lightingData.specular = specular;
    lightingData.emission = emission;
    return lightingData;
}

#endif // BC_PARTICLES_PARTICLE_LIT_LIGHTING_INCLUDED