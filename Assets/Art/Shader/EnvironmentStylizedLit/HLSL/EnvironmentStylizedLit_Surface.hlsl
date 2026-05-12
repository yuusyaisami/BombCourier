#ifndef BC_ENVIRONMENT_STYLIZED_LIT_SURFACE_INCLUDED
#define BC_ENVIRONMENT_STYLIZED_LIT_SURFACE_INCLUDED

#include "EnvironmentStylizedLit_Input.hlsl"

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
};

float2 ESL_TransformBaseUV(float2 uv)
{
	return uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
}

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
}

ESL_SurfaceData ESL_BuildSurfaceData(float2 uv)
{
	ESL_SurfaceData surfaceData;
	ESL_InitializeSurfaceData(surfaceData);

	float2 baseUV = ESL_TransformBaseUV(uv);
	half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, baseUV);
	half4 baseColor = baseSample * half4(_BaseColor);

	surfaceData.albedo = baseColor.rgb;
	surfaceData.alpha = baseColor.a;
	surfaceData.normalTS = UnpackNormalScale(
		SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, baseUV),
		_NormalScale);

	half occlusionSample = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, baseUV).g;
	surfaceData.occlusion = lerp(1.0, occlusionSample, saturate(_OcclusionStrength));

	half3 emissionSample = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, baseUV).rgb;
	surfaceData.emission = emissionSample * _EmissionColor.rgb * _EmissionStrength;
	surfaceData.metallic = saturate(_Metallic);
	surfaceData.smoothness = saturate(_Smoothness);

	return surfaceData;
}

void ESL_ApplyAlphaClip(float alpha)
{
	if (_AlphaClip > 0.5)
	{
		clip(alpha - _Cutoff);
	}
}

#endif