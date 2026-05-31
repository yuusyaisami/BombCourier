#ifndef BC_ENVIRONMENT_STYLIZED_LIT_LIGHTING_INCLUDED
#define BC_ENVIRONMENT_STYLIZED_LIT_LIGHTING_INCLUDED

#include "EnvironmentStylizedLit_Input.hlsl"
#include "EnvironmentStylizedLit_Common.hlsl"
#include "EnvironmentStylizedLit_Ambient.hlsl"
#include "EnvironmentStylizedLit_Specular.hlsl"

struct ESL_MainLightData
{
	float3 directionWS;
	float3 color;
	float distanceAttenuation;
	float shadowAttenuation;
};

#define ESL_ADDITIONAL_LIGHT_MODE_OFF 0
#define ESL_ADDITIONAL_LIGHT_MODE_FILL_ONLY 1
#define ESL_ADDITIONAL_LIGHT_MODE_QUANTIZED 2
#define ESL_ADDITIONAL_LIGHT_MODE_CONTINUOUS 3

struct ESL_IndirectLightingData
{
	float indirectMask;
	float3 ambientColor;
	float3 bounceColor;
	float3 bakedGIColor;
	float3 indirectShadowTint;
	float indirectAmbientOcclusion;
	float3 cavityTint;
	float3 combinedColor;
};

struct ESL_AdditionalLightingData
{
	float3 fillColor;
	float3 quantizedColor;
	float3 continuousColor;
	float3 combinedColor;
};

float ESL_ApplyBandContrastAndOffset(float value);
float ESL_ApplyBandContrastAndOffset(float value, float bandOffset);

int ESL_GetAdditionalLightMode()
{
	return (int)round(_AdditionalLightMode);
}

// 追加光のシャドウ影響度をブレンドします（1=影無視、0=完全反映）。
float ESL_EvaluateAdditionalLightShadowAttenuation(float shadowAttenuation)
{
	return lerp(1.0, saturate(shadowAttenuation), saturate(_AdditionalLightShadowInfluence));
}

// 追加光色の彩度影響を制御します（輝度寄りに倒す用途）。
float3 ESL_EvaluateAdditionalLightColor(float3 lightColor)
{
	float luminance = dot(lightColor, float3(0.2126, 0.7152, 0.0722));
	return lerp(luminance.xxx, lightColor, saturate(_AdditionalLightColorInfluence));
}

float ESL_EvaluateAdditionalLightLuminance(float3 lightColor)
{
	return dot(max(lightColor, 0.0), float3(0.2126, 0.7152, 0.0722));
}

// 主光+追加光の合算強度を、発光判定に使う0..1へ正規化します。
float ESL_EvaluateCombinedLightIntensity(float steppedLight, ESL_MainLightData mainLightData, float shadowAttenuation, float3 additionalCombinedColor)
{
	float mainLightLuminance = ESL_EvaluateAdditionalLightLuminance(mainLightData.color)
		* saturate(mainLightData.distanceAttenuation)
		* saturate(shadowAttenuation)
		* saturate(steppedLight);
	float additionalLightLuminance = ESL_EvaluateAdditionalLightLuminance(additionalCombinedColor) * max(_LightBandEmissionAdditionalWeight, 0.0);
	float combinedLuminance = max(mainLightLuminance + additionalLightLuminance, 0.0);
	float response = max(_LightBandEmissionResponse, 1e-3);
	return saturate(1.0 - exp2(-combinedLuminance * response));
}

float ESL_EvaluateLightBandRangeMask(float combinedLightIntensity)
{
	float rangeMin = min(_LightBandEmissionMin, _LightBandEmissionMax);
	float rangeMax = max(_LightBandEmissionMin, _LightBandEmissionMax);
	float feather = max(_LightBandEmissionFeather, 1e-4);
	float lower = smoothstep(rangeMin - feather, rangeMin + feather, combinedLightIntensity);
	float upper = 1.0 - smoothstep(rangeMax - feather, rangeMax + feather, combinedLightIntensity);
	return saturate(lower * upper);
}

float ESL_EvaluateLightBandStepMask(float combinedLightIntensity)
{
	float stepCount = max(round(_LightStepCount), 1.0);
	float stepIndex = floor(saturate(combinedLightIntensity) * (stepCount - 1.0) + 1.0);
	float stepMin = min(round(_LightBandEmissionStepMin), round(_LightBandEmissionStepMax));
	float stepMax = max(round(_LightBandEmissionStepMin), round(_LightBandEmissionStepMax));
	return step(stepMin - 0.5, stepIndex) * (1.0 - step(stepMax + 0.5, stepIndex));
}

float ESL_EvaluateLightBandEmissionMask(float combinedLightIntensity)
{
	float rangeMask = ESL_EvaluateLightBandRangeMask(combinedLightIntensity);
	float stepMask = ESL_EvaluateLightBandStepMask(combinedLightIntensity);
	float bandStepBlend = saturate(_LightBandEmissionBandStepBlend);
	return lerp(rangeMask, rangeMask * stepMask, bandStepBlend);
}

float ESL_EvaluateAdditionalFillMask(float mainShadowAttenuation, float mainNdotL)
{
	// 主光の影/未照射を優先して埋めるため、どちらか大きい方を採用します。
	float shadowMask = 1.0 - saturate(mainShadowAttenuation);
	float unlitMask = 1.0 - saturate(mainNdotL);
	return saturate(max(shadowMask, unlitMask));
}

float ESL_EvaluateAdditionalDominanceMask(float mainShadowAttenuation, float mainNdotL)
{
	// 主光が強い面では追加光をやや抑え、主光優位の見た目を維持します。
	float mainLightMask = saturate(mainShadowAttenuation * saturate(mainNdotL));
	return lerp(1.0, 0.6, mainLightMask);
}

float3 ESL_AccumulateAdditionalLight(float3 accumulatedColor, float3 contribution)
{
	// 累積が明るくなるほど寄与を圧縮し、過飽和を抑制します。
	float accumulatedLuminance = ESL_EvaluateAdditionalLightLuminance(accumulatedColor);
	return accumulatedColor + contribution * rcp(1.0 + accumulatedLuminance);
}

float ESL_EvaluateAdditionalContinuousTerm(float ndotl)
{
	return ESL_ComputeWrappedLight(saturate(ndotl), saturate(_WrapLighting) * 0.5);
}

float ESL_EvaluateAdditionalQuantizedTerm(float ndotl)
{
	float continuousTerm = ESL_EvaluateAdditionalContinuousTerm(ndotl);
	return ESL_ComputeSteppedLight(
		ESL_ApplyBandContrastAndOffset(continuousTerm),
		max(_LightStepCount, 1.0),
		saturate(_LightStepSmoothness));
}

float ESL_EvaluateAdditionalLightModeTerm(int additionalLightMode, float ndotl, float mainShadowAttenuation, float mainNdotL)
{
	float modeTerm = 0.0;

	if (additionalLightMode == ESL_ADDITIONAL_LIGHT_MODE_FILL_ONLY)
	{
		// FillOnlyは「主光の影・未照射を補う」用途。
		// ただし光量そのものは量子化し、LightStepCountで段数を制御します。
		modeTerm = ESL_EvaluateAdditionalQuantizedTerm(ndotl) * ESL_EvaluateAdditionalFillMask(mainShadowAttenuation, mainNdotL);
	}
	else if (additionalLightMode == ESL_ADDITIONAL_LIGHT_MODE_QUANTIZED)
	{
		modeTerm = ESL_EvaluateAdditionalQuantizedTerm(ndotl);
	}
	else if (additionalLightMode == ESL_ADDITIONAL_LIGHT_MODE_CONTINUOUS)
	{
		modeTerm = ESL_EvaluateAdditionalContinuousTerm(ndotl);
	}

	return modeTerm;
}

ESL_AdditionalLightingData ESL_EvaluateAdditionalLighting(ESL_InputData inputData, float mainShadowAttenuation, float mainNdotL)
{
	ESL_AdditionalLightingData additionalLightingData = (ESL_AdditionalLightingData)0;
	int additionalLightMode = ESL_GetAdditionalLightMode();

	if (additionalLightMode == ESL_ADDITIONAL_LIGHT_MODE_OFF || _AdditionalLightIntensity <= 1e-4)
	{
		return additionalLightingData;
	}

	#if defined(_ADDITIONAL_LIGHTS_VERTEX) && !defined(_ADDITIONAL_LIGHTS)
	// 頂点追加光パスでは、補間済みVertexLightingをモード別に再解釈します。
	float3 vertexLighting = max(inputData.vertexLighting, 0.0) * inputData.directAmbientOcclusion;
	float vertexLightLuminance = ESL_EvaluateAdditionalLightLuminance(vertexLighting);

	if (vertexLightLuminance <= 1e-4)
	{
		return additionalLightingData;
	}

	float3 vertexLightColor = ESL_EvaluateAdditionalLightColor(vertexLighting);
	float3 vertexContribution = vertexLightColor * saturate(_AdditionalLightIntensity);
	float dominanceMask = ESL_EvaluateAdditionalDominanceMask(mainShadowAttenuation, mainNdotL);

	if (additionalLightMode == ESL_ADDITIONAL_LIGHT_MODE_FILL_ONLY)
	{
		float quantizedVertexLight = ESL_ComputeSteppedLight(
			ESL_ApplyBandContrastAndOffset(saturate(vertexLightLuminance)),
			max(_LightStepCount, 1.0),
			saturate(_LightStepSmoothness));
		float quantizedScale = vertexLightLuminance <= 1e-4 ? 0.0 : saturate(quantizedVertexLight / vertexLightLuminance);
		additionalLightingData.fillColor = vertexContribution
			* quantizedScale
			* ESL_EvaluateAdditionalFillMask(mainShadowAttenuation, mainNdotL);
	}
	else if (additionalLightMode == ESL_ADDITIONAL_LIGHT_MODE_QUANTIZED)
	{
		float quantizedVertexLight = ESL_ComputeSteppedLight(
			ESL_ApplyBandContrastAndOffset(saturate(vertexLightLuminance)),
			max(_LightStepCount, 1.0),
			saturate(_LightStepSmoothness));
		float quantizedScale = vertexLightLuminance <= 1e-4 ? 0.0 : saturate(quantizedVertexLight / vertexLightLuminance);
		additionalLightingData.quantizedColor = vertexContribution * quantizedScale * dominanceMask;
	}
	else if (additionalLightMode == ESL_ADDITIONAL_LIGHT_MODE_CONTINUOUS)
	{
		additionalLightingData.continuousColor = vertexContribution * dominanceMask;
	}

	additionalLightingData.combinedColor = additionalLightingData.fillColor
		+ additionalLightingData.quantizedColor
		+ additionalLightingData.continuousColor;
	return additionalLightingData;
	#endif

	// ピクセル追加光パス。ライトレイヤー一致時のみ寄与を加算します。
	uint additionalLightsCount = (uint)GetAdditionalLightsCount();
	uint meshRenderingLayers = GetMeshRenderingLayer();

	LIGHT_LOOP_BEGIN(additionalLightsCount)
		Light additionalLight = GetAdditionalLight(lightIndex, inputData.positionWS, inputData.shadowMask);

		#ifdef _LIGHT_LAYERS
		if (!IsMatchingLightLayer(additionalLight.layerMask, meshRenderingLayers))
		{
			continue;
		}
		#endif

		float modeTerm = ESL_EvaluateAdditionalLightModeTerm(
			additionalLightMode,
			dot(inputData.normalWS, additionalLight.direction),
			mainShadowAttenuation,
			mainNdotL);

		if (modeTerm <= 1e-4 || additionalLight.distanceAttenuation <= 1e-4)
		{
			continue;
		}

		float shadowAttenuation = ESL_EvaluateAdditionalLightShadowAttenuation(additionalLight.shadowAttenuation);
		float3 lightColor = ESL_EvaluateAdditionalLightColor(additionalLight.color * inputData.directAmbientOcclusion);
		float dominanceMask = additionalLightMode == ESL_ADDITIONAL_LIGHT_MODE_FILL_ONLY ? 1.0 : ESL_EvaluateAdditionalDominanceMask(mainShadowAttenuation, mainNdotL);
		float3 contribution = lightColor
			* additionalLight.distanceAttenuation
			* shadowAttenuation
			* modeTerm
			* dominanceMask
			* saturate(_AdditionalLightIntensity);

		if (additionalLightMode == ESL_ADDITIONAL_LIGHT_MODE_FILL_ONLY)
		{
			additionalLightingData.fillColor = ESL_AccumulateAdditionalLight(additionalLightingData.fillColor, contribution);
		}
		else if (additionalLightMode == ESL_ADDITIONAL_LIGHT_MODE_QUANTIZED)
		{
			additionalLightingData.quantizedColor = ESL_AccumulateAdditionalLight(additionalLightingData.quantizedColor, contribution);
		}
		else if (additionalLightMode == ESL_ADDITIONAL_LIGHT_MODE_CONTINUOUS)
		{
			additionalLightingData.continuousColor = ESL_AccumulateAdditionalLight(additionalLightingData.continuousColor, contribution);
		}
	LIGHT_LOOP_END

	additionalLightingData.combinedColor = additionalLightingData.fillColor
		+ additionalLightingData.quantizedColor
		+ additionalLightingData.continuousColor;
	return additionalLightingData;
}

float3 ESL_EvaluateIndirectShadowTint(float indirectMask)
{
	return lerp(float3(1.0, 1.0, 1.0), _IndirectShadowColor.rgb, saturate(indirectMask));
}

float3 ESL_EvaluateCavityTint(float occlusion)
{
	float cavityMask = saturate(1.0 - saturate(occlusion)) * saturate(_CavityStrength);
	return lerp(float3(1.0, 1.0, 1.0), _CavityColor.rgb, cavityMask);
}

float3 ESL_EvaluateStylizedBakedGI(float3 bakedGI, float3 indirectShadowTint)
{
	return lerp(bakedGI, bakedGI * indirectShadowTint, saturate(_IndirectStylizeStrength));
}

ESL_MainLightData ESL_GetMainLightData(inout ESL_InputData inputData)
{
	// シャドウ有無に応じて主光取得APIを切り替えます。
	#if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
	Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask);
	#else
	Light mainLight = GetMainLight();
	#endif

	MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, inputData.shadowMask);
	mainLight.color *= inputData.directAmbientOcclusion;

	ESL_MainLightData mainLightData;
	mainLightData.directionWS = mainLight.direction;
	mainLightData.color = mainLight.color;
	mainLightData.distanceAttenuation = mainLight.distanceAttenuation;
	mainLightData.shadowAttenuation = mainLight.shadowAttenuation;
	return mainLightData;
}

// 影の柔らかさ/影響度を反映した実効シャドウ減衰。
float ESL_EvaluateShadowAttenuation(float rawShadowAttenuation)
{
	float softenedShadow = lerp(saturate(rawShadowAttenuation), 1.0, saturate(_ShadowSoftFill));
	return lerp(1.0, softenedShadow, saturate(_ShadowInfluence));
}

float ESL_EvaluateShadowMask(float shadowAttenuation)
{
	return saturate(1.0 - shadowAttenuation);
}

float3 ESL_EvaluateShadowColor(float shadowMask)
{
	return lerp(_ShadowColor.rgb, _DeepShadowColor.rgb, saturate(shadowMask));
}

float3 ESL_ApplyShadowColorBlend(float3 bandColor, float shadowAttenuation)
{
	float shadowMask = ESL_EvaluateShadowMask(shadowAttenuation);
	float3 shadowColor = ESL_EvaluateShadowColor(shadowMask);
	return lerp(bandColor, shadowColor, saturate(_ShadowColorBlend) * shadowMask);
}

// 影色ブレンドを適用した段階色を返します。
float3 ESL_EvaluateShadowedBandColor(float3 bandColor, float rawShadowAttenuation, out float shadowAttenuation)
{
	shadowAttenuation = ESL_EvaluateShadowAttenuation(rawShadowAttenuation);
	return ESL_ApplyShadowColorBlend(bandColor, shadowAttenuation);
}

float ESL_EvaluateIndirectMask(float shadowAttenuation, float ndotl)
{
	return saturate(max(ESL_EvaluateShadowMask(shadowAttenuation), 1.0 - saturate(ndotl)));
}

// 間接光（環境光/バウンス/ベイクGI）をスタイライズして合成します。
ESL_IndirectLightingData ESL_EvaluateIndirectLighting(ESL_InputData inputData, ESL_SurfaceData surfaceData, float shadowAttenuation, float ndotl)
{
	ESL_AmbientData ambientData = ESL_EvaluateAmbientData(inputData);
	ESL_IndirectLightingData indirectLightingData;
	indirectLightingData.indirectMask = ESL_EvaluateIndirectMask(shadowAttenuation, ndotl);
	indirectLightingData.indirectAmbientOcclusion = inputData.indirectAmbientOcclusion;
	indirectLightingData.ambientColor = ambientData.directionalAmbient * indirectLightingData.indirectAmbientOcclusion;
	indirectLightingData.bounceColor = ambientData.bounceColor * indirectLightingData.indirectMask * indirectLightingData.indirectAmbientOcclusion;
	indirectLightingData.indirectShadowTint = ESL_EvaluateIndirectShadowTint(indirectLightingData.indirectMask);
	indirectLightingData.bakedGIColor = ESL_EvaluateStylizedBakedGI(inputData.bakedGI * indirectLightingData.indirectAmbientOcclusion, indirectLightingData.indirectShadowTint);
	indirectLightingData.cavityTint = ESL_EvaluateCavityTint(surfaceData.occlusion);
	indirectLightingData.combinedColor = (indirectLightingData.ambientColor + indirectLightingData.bounceColor + indirectLightingData.bakedGIColor)
		* indirectLightingData.cavityTint
		* saturate(_IndirectStrength);
	return indirectLightingData;
}

// 主光由来のスペキュラ評価を委譲します。
ESL_SpecularData ESL_EvaluateSpecularLighting(ESL_InputData inputData, ESL_MainLightData mainLightData, float shadowAttenuation)
{
	return ESL_EvaluateSpecularData(
		inputData,
		mainLightData.directionWS,
		mainLightData.color,
		mainLightData.distanceAttenuation,
		shadowAttenuation);
}

// ライト段階値へコントラスト・オフセットを適用します。
float ESL_ApplyBandContrastAndOffset(float value)
{
	float contrastedValue = (saturate(value) - 0.5) * max(_BandContrast, 0.0001) + 0.5 + _BandOffset;
	return saturate(contrastedValue);
}

// ローカルオフセット版（頂点カラー等の局所制御用）。
float ESL_ApplyBandContrastAndOffset(float value, float bandOffset)
{
	float contrastedValue = (saturate(value) - 0.5) * max(_BandContrast, 0.0001) + 0.5 + bandOffset;
	return saturate(contrastedValue);
}

// 段階値から5色グラデーション（DeepShadow〜Highlight）を評価します。
float3 ESL_EvaluateBandColor(float steppedLight)
{
	float clampedLight = saturate(steppedLight);

	if (clampedLight <= 0.25)
	{
		return lerp(_DeepShadowColor.rgb, _ShadowColor.rgb, smoothstep(0.0, 0.25, clampedLight));
	}

	if (clampedLight <= 0.5)
	{
		return lerp(_ShadowColor.rgb, _MidColor.rgb, smoothstep(0.25, 0.5, clampedLight));
	}

	if (clampedLight <= 0.75)
	{
		return lerp(_MidColor.rgb, _LightColor.rgb, smoothstep(0.5, 0.75, clampedLight));
	}

	return lerp(_LightColor.rgb, _HighlightColor.rgb, smoothstep(0.75, 1.0, clampedLight));
}

#endif