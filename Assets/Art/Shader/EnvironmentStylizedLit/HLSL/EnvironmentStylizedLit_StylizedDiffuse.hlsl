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
	float combinedLightIntensity;
	float lightBandRangeMask;
	float lightBandStepMask;
	float lightBandEmissionMask;
	float3 lightBandEmissionColor;
	float simpleBoostFresnelTerm;
	float3 simpleBoostEmissionColor;
	float3 emissionAddColor;
	float3 finalColor;
	float3 diffuseColor;
};

// 主光の段階化拡散、間接光、追加光、スペキュラを統合して最終色を算出します。
ESL_StylizedDiffuseData ESL_EvaluateStylizedDiffuse(ESL_InputData inputData, ESL_SurfaceData surfaceData)
{
	ESL_MainLightData mainLight = ESL_GetMainLightData(inputData);

	ESL_StylizedDiffuseData diffuseData;
	diffuseData.ndotl = saturate(dot(inputData.normalWS, mainLight.directionWS));
	diffuseData.worldNoise = surfaceData.worldNoise;
	diffuseData.surfaceNoise = surfaceData.surfaceNoise;
	diffuseData.bandNoise = surfaceData.bandNoise;
	diffuseData.wrappedLight = ESL_ComputeWrappedLight(diffuseData.ndotl, _WrapLighting);
	// ライト段にバンドノイズと局所オフセットを加えて、トゥーン段差を決定します。
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

	// ライト帯発光の評価。主光/追加光の合算強度を帯域判定に使います。
	diffuseData.combinedLightIntensity = ESL_EvaluateCombinedLightIntensity(
		diffuseData.steppedLight,
		mainLight,
		diffuseData.shadowAttenuation,
		additionalLightingData.combinedColor);
	diffuseData.lightBandRangeMask = ESL_EvaluateLightBandRangeMask(diffuseData.combinedLightIntensity);
	diffuseData.lightBandStepMask = ESL_EvaluateLightBandStepMask(diffuseData.combinedLightIntensity);
	diffuseData.lightBandEmissionMask = ESL_EvaluateLightBandEmissionMask(diffuseData.combinedLightIntensity);

	if (_LightBandEmissionEnabled > 0.5 && _LightBandEmissionIntensity > 1e-4)
	{
		float specialMaskWeight = lerp(1.0, saturate(surfaceData.specialMask), saturate(_LightBandEmissionSpecialMaskInfluence));
		float gradientSourceMask = _WorldYGradientEnabled > 0.5 ? saturate(surfaceData.worldYGradientMask) : 1.0;
		float gradientMaskWeight = lerp(1.0, gradientSourceMask, saturate(_LightBandEmissionGradientInfluence));
		float stylizedMask = diffuseData.lightBandEmissionMask * specialMaskWeight * gradientMaskWeight;
		diffuseData.lightBandEmissionColor = _LightBandEmissionColor.rgb * (_LightBandEmissionIntensity * stylizedMask);
	}
	else
	{
		diffuseData.lightBandEmissionColor = 0.0;
	}

	// 単純強発光 + Fresnelベースの視線依存強度。
	if (_SimpleBoostEmissionEnabled > 0.5 && _SimpleBoostEmissionIntensity > 1e-4)
	{
		float ndotv = saturate(dot(inputData.normalWS, inputData.viewDirectionWS));
		float fresnelTerm = pow(saturate(1.0 - ndotv), max(_SimpleBoostFresnelPower, 1e-3));
		diffuseData.simpleBoostFresnelTerm = _SimpleBoostFresnelInvert > 0.5 ? 1.0 - fresnelTerm : fresnelTerm;
		float viewScale = 1.0 + diffuseData.simpleBoostFresnelTerm * max(_SimpleBoostFresnelStrength, 0.0);
		diffuseData.simpleBoostEmissionColor = _SimpleBoostEmissionColor.rgb * (_SimpleBoostEmissionIntensity * viewScale);
	}
	else
	{
		diffuseData.simpleBoostFresnelTerm = 0.0;
		diffuseData.simpleBoostEmissionColor = 0.0;
	}

	diffuseData.emissionAddColor = diffuseData.lightBandEmissionColor + diffuseData.simpleBoostEmissionColor;
	diffuseData.finalColor = surfaceData.albedo * diffuseData.shadowedBandColor * mainLight.color * mainLight.distanceAttenuation
		+ diffuseData.indirectColor
		+ diffuseData.additionalLightColor
		+ specularData.combinedSpecular;
	diffuseData.diffuseColor = diffuseData.finalColor;
	return diffuseData;
}

#endif