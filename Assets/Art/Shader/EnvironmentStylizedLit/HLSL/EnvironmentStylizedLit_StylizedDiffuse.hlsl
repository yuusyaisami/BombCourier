#ifndef BC_ENVIRONMENT_STYLIZED_LIT_STYLIZED_DIFFUSE_INCLUDED
#define BC_ENVIRONMENT_STYLIZED_LIT_STYLIZED_DIFFUSE_INCLUDED

#include "EnvironmentStylizedLit_Surface.hlsl"
#include "EnvironmentStylizedLit_Lighting.hlsl"

struct ESL_StylizedDiffuseData
{
	float ndotl;
	float worldNoise;
	float surfaceNoise;
	float bandNoise;
	float wrappedLight;
	float steppedLight;
	float3 bandColor;
	float shadowAttenuation;
	float3 shadowedBandColor;
	float3 ambientColor;
	float3 bounceColor;
	float3 bakedGIColor;
	float3 indirectColor;
	float3 additionalLightColor;
	float specularTerm;
	float3 specularColor;
	float edgeSheenTerm;
	float3 edgeSheenColor;
	float3 finalColor;
	float3 diffuseColor;
};

ESL_StylizedDiffuseData ESL_EvaluateStylizedDiffuse(ESL_InputData inputData, ESL_SurfaceData surfaceData)
{
	ESL_MainLightData mainLight = ESL_GetMainLightData(inputData);

	ESL_StylizedDiffuseData diffuseData;
	diffuseData.ndotl = saturate(dot(inputData.normalWS, mainLight.directionWS));
	diffuseData.worldNoise = surfaceData.worldNoise;
	diffuseData.surfaceNoise = surfaceData.surfaceNoise;
	diffuseData.bandNoise = surfaceData.bandNoise;
	diffuseData.wrappedLight = ESL_ComputeWrappedLight(diffuseData.ndotl, _WrapLighting);
	diffuseData.steppedLight = ESL_ComputeSteppedLight(
		ESL_ApplyBandContrastAndOffset(diffuseData.wrappedLight + diffuseData.bandNoise, ESL_EvaluateSurfaceBandOffset(surfaceData)),
		_LightStepCount,
		saturate(_LightStepSmoothness));
	diffuseData.bandColor = ESL_EvaluateBandColor(diffuseData.steppedLight);
	diffuseData.shadowedBandColor = ESL_EvaluateShadowedBandColor(
		diffuseData.bandColor,
		mainLight.shadowAttenuation,
		diffuseData.shadowAttenuation);
	ESL_IndirectLightingData indirectLightingData = ESL_EvaluateIndirectLighting(inputData, surfaceData, diffuseData.shadowAttenuation, diffuseData.ndotl);
	ESL_AdditionalLightingData additionalLightingData = ESL_EvaluateAdditionalLighting(inputData, diffuseData.shadowAttenuation, diffuseData.ndotl);
	ESL_SpecularData specularData = ESL_EvaluateSpecularLighting(inputData, mainLight, diffuseData.shadowAttenuation);
	diffuseData.ambientColor = indirectLightingData.ambientColor;
	diffuseData.bounceColor = indirectLightingData.bounceColor;
	diffuseData.bakedGIColor = indirectLightingData.bakedGIColor;
	diffuseData.indirectColor = surfaceData.albedo * indirectLightingData.combinedColor;
	diffuseData.additionalLightColor = surfaceData.albedo * additionalLightingData.combinedColor;
	diffuseData.specularTerm = specularData.specularTerm;
	diffuseData.specularColor = specularData.specularColor;
	diffuseData.edgeSheenTerm = specularData.edgeSheenTerm;
	diffuseData.edgeSheenColor = specularData.edgeSheenColor;
	diffuseData.finalColor = surfaceData.albedo * diffuseData.shadowedBandColor * mainLight.color * mainLight.distanceAttenuation
		+ diffuseData.indirectColor
		+ diffuseData.additionalLightColor
		+ specularData.combinedSpecular;
	diffuseData.diffuseColor = diffuseData.finalColor;
	return diffuseData;
}

#endif