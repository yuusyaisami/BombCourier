#ifndef BC_ENVIRONMENT_STYLIZED_LIT_SURFACE_INCLUDED
#define BC_ENVIRONMENT_STYLIZED_LIT_SURFACE_INCLUDED

#include "EnvironmentStylizedLit_Sampling.hlsl"
#include "EnvironmentStylizedLit_Noise.hlsl"

struct ESL_SurfaceData
{
	float3 albedo;
	float alpha;

	float3 normalTS;
	float metallic;
	float smoothness;
	float occlusion;

	float3 emission;

	float cavity;
	float colorVariationMask;
	float bandOffsetMask;
	float specialMask;
	float localBandOffset;
	float surfaceNoise;
	float worldNoise;
	float objectSpaceNoise;
	float noiseDistanceFade;
	float bandNoise;
	float worldYGradientMask;
	float3 normalWSOverride;
	float hasNormalWSOverride;
};

struct ESL_SurfaceContext
{
	float2 uv;
	float3 positionWS;
	float3 normalWS;
	float4 vertexColor;
};

void ESL_InitializeSurfaceData(out ESL_SurfaceData surfaceData)
{
	surfaceData.albedo = 1.0;
	surfaceData.alpha = 1.0;
	surfaceData.normalTS = float3(0.0, 0.0, 1.0);
	surfaceData.metallic = 0.0;
	surfaceData.smoothness = 0.35;
	surfaceData.occlusion = 1.0;
	surfaceData.emission = 0.0;
	surfaceData.cavity = 0.0;
	surfaceData.colorVariationMask = 0.0;
	surfaceData.bandOffsetMask = 0.0;
	surfaceData.specialMask = 0.0;
	surfaceData.localBandOffset = 0.0;
	surfaceData.surfaceNoise = 0.0;
	surfaceData.worldNoise = 0.0;
	surfaceData.objectSpaceNoise = 0.0;
	surfaceData.noiseDistanceFade = 0.0;
	surfaceData.bandNoise = 0.0;
	surfaceData.worldYGradientMask = 0.0;
	surfaceData.normalWSOverride = 0.0;
	surfaceData.hasNormalWSOverride = 0.0;
}

float3 ESL_ApplyAlbedoNoise(float3 albedo, float surfaceNoise)
{
	if (_AlbedoNoiseStrength <= 1e-4 || abs(surfaceNoise) <= 1e-4)
	{
		return albedo;
	}

	float albedoNoiseScale = max(1.0 + surfaceNoise * saturate(_AlbedoNoiseStrength), 0.0);
	return albedo * albedoNoiseScale;
}

float ESL_EvaluateWorldYGradientMask(float positionY)
{
	if (_WorldYGradientEnabled <= 0.5)
	{
		return 0.0;
	}

	float gradientMin = min(_WorldYGradientMin, _WorldYGradientMax);
	float gradientMax = max(_WorldYGradientMin, _WorldYGradientMax);
	float gradientRange = max(gradientMax - gradientMin, 1e-4);
	return saturate((positionY - gradientMin) / gradientRange);
}

void ESL_ApplyWorldYGradient(inout ESL_SurfaceData surfaceData, float3 positionWS)
{
	surfaceData.worldYGradientMask = ESL_EvaluateWorldYGradientMask(positionWS.y);

	if (_WorldYGradientEnabled <= 0.5 || _WorldYGradientStrength <= 1e-4)
	{
		return;
	}

	float3 gradientTint = lerp(_WorldYGradientBottomColor.rgb, _WorldYGradientTopColor.rgb, surfaceData.worldYGradientMask);
	surfaceData.albedo *= lerp(1.0.xxx, gradientTint, saturate(_WorldYGradientStrength));
}

void ESL_ApplyVertexColorMasks(inout ESL_SurfaceData surfaceData)
{
	if (_VertexColorEnabled <= 0.5)
	{
		surfaceData.cavity = 0.0;
		surfaceData.colorVariationMask = 0.0;
		surfaceData.bandOffsetMask = 0.0;
		surfaceData.specialMask = 0.0;
		surfaceData.localBandOffset = 0.0;
		return;
	}

	float cavityMask = saturate(surfaceData.cavity) * saturate(_VertexColorCavityStrength);
	float colorVariationMask = saturate(surfaceData.colorVariationMask) * saturate(_VertexColorColorVariationStrength);
	float bandOffsetMask = saturate(surfaceData.bandOffsetMask);
	surfaceData.specialMask = saturate(surfaceData.specialMask);
	surfaceData.occlusion = saturate(surfaceData.occlusion * (1.0 - cavityMask));
	surfaceData.albedo *= 1.0 - cavityMask * 0.35;
	surfaceData.albedo *= 1.0 + surfaceData.surfaceNoise * colorVariationMask;
	surfaceData.localBandOffset = (bandOffsetMask * 2.0 - 1.0) * saturate(_VertexColorBandOffsetStrength);
}

float ESL_EvaluateSurfaceBandOffset(ESL_SurfaceData surfaceData)
{
	return _BandOffset + surfaceData.localBandOffset;
}

ESL_SurfaceData ESL_BuildSurfaceData(ESL_SurfaceContext surfaceContext)
{
	ESL_SurfaceData surfaceData;
	ESL_InitializeSurfaceData(surfaceData);

	half4 baseColor = ESL_SampleBaseColor(surfaceContext.uv, surfaceContext.positionWS, surfaceContext.normalWS);
	ESL_NoiseData noiseData = ESL_EvaluateNoiseData(surfaceContext.positionWS, surfaceContext.normalWS);

	surfaceData.albedo = ESL_ApplyAlbedoNoise(baseColor.rgb, noiseData.surfaceNoise);
	surfaceData.alpha = baseColor.a;
	surfaceData.normalTS = ESL_SampleNormalTS(surfaceContext.uv);
	if (ESL_IsTriplanarNormalMapEnabled())
	{
		surfaceData.normalWSOverride = ESL_SampleTriplanarNormalWS(surfaceContext.positionWS, surfaceContext.normalWS);
		surfaceData.hasNormalWSOverride = 1.0;
	}

	float2 baseUV = ESL_TransformBaseUV(surfaceContext.uv);
	half occlusionSample = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, baseUV).g;
	surfaceData.occlusion = lerp(1.0, occlusionSample, saturate(_OcclusionStrength));

	surfaceData.emission = ESL_SampleEmission(surfaceContext.uv);
	surfaceData.metallic = saturate(_Metallic);
	surfaceData.smoothness = saturate(_Smoothness);
	surfaceData.cavity = surfaceContext.vertexColor.r;
	surfaceData.bandOffsetMask = surfaceContext.vertexColor.g;
	surfaceData.colorVariationMask = surfaceContext.vertexColor.b;
	surfaceData.specialMask = surfaceContext.vertexColor.a;
	surfaceData.surfaceNoise = noiseData.surfaceNoise;
	surfaceData.worldNoise = noiseData.worldNoise;
	surfaceData.objectSpaceNoise = noiseData.objectSpaceNoise;
	surfaceData.noiseDistanceFade = noiseData.distanceFade;
	surfaceData.bandNoise = noiseData.bandNoise;
	ESL_ApplyVertexColorMasks(surfaceData);
	ESL_ApplyWorldYGradient(surfaceData, surfaceContext.positionWS);

	return surfaceData;
}
#endif