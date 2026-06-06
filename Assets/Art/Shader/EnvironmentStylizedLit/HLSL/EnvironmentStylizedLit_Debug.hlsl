#ifndef BC_ENVIRONMENT_STYLIZED_LIT_DEBUG_INCLUDED
#define BC_ENVIRONMENT_STYLIZED_LIT_DEBUG_INCLUDED

#include "EnvironmentStylizedLit_StylizedDiffuse.hlsl"

#define ESL_DEBUG_OFF 0
#define ESL_DEBUG_NDOTL 1
#define ESL_DEBUG_WRAPPED_LIGHT 2
#define ESL_DEBUG_STEPPED_LIGHT 3
#define ESL_DEBUG_BAND_COLOR 4
#define ESL_DEBUG_WORLD_NOISE 5
#define ESL_DEBUG_BAND_NOISE 6
#define ESL_DEBUG_COMBINED_LIGHT_INTENSITY 7
#define ESL_DEBUG_LIGHT_BAND_EMISSION_MASK 8
#define ESL_DEBUG_SIMPLE_BOOST_FRESNEL 9
#define ESL_DEBUG_SHADOW_ATTENUATION 10
#define ESL_DEBUG_OCCLUSION_SHADOW 11

// 符号付きノイズを可視化用の0..1へ再マッピングします。
float3 ESL_EncodeDebugNoise(float noiseValue)
{
	if (abs(noiseValue) <= 1e-4)
	{
		return 0.0.xxx;
	}

	return saturate(noiseValue * 0.5 + 0.5).xxx;
}

// DebugView値が有効かを判定します。
bool ESL_IsDebugViewActive()
{
	return round(_DebugView) > ESL_DEBUG_OFF;
}

// DebugViewの種別に応じて、表示する中間値を切り替えます。
float3 ESL_ApplyDebugView(float3 color, ESL_StylizedDiffuseData diffuseData)
{
	int debugView = (int)round(_DebugView);

	if (debugView == ESL_DEBUG_NDOTL)
	{
		return diffuseData.ndotl.xxx;
	}

	if (debugView == ESL_DEBUG_WRAPPED_LIGHT)
	{
		return diffuseData.wrappedLight.xxx;
	}

	if (debugView == ESL_DEBUG_STEPPED_LIGHT)
	{
		return diffuseData.steppedLight.xxx;
	}

	if (debugView == ESL_DEBUG_BAND_COLOR)
	{
		return diffuseData.shadowedBandColor;
	}

	if (debugView == ESL_DEBUG_WORLD_NOISE)
	{
		return ESL_EncodeDebugNoise(diffuseData.worldNoise);
	}

	if (debugView == ESL_DEBUG_BAND_NOISE)
	{
		return ESL_EncodeDebugNoise(diffuseData.bandNoise);
	}

	if (debugView == ESL_DEBUG_COMBINED_LIGHT_INTENSITY)
	{
		return diffuseData.combinedLightIntensity.xxx;
	}

	if (debugView == ESL_DEBUG_LIGHT_BAND_EMISSION_MASK)
	{
		return diffuseData.lightBandEmissionMask.xxx;
	}

	if (debugView == ESL_DEBUG_SIMPLE_BOOST_FRESNEL)
	{
		return diffuseData.simpleBoostFresnelTerm.xxx;
	}

	// 受光した生のシャドウ。白=照射(1.0)、黒=完全遮蔽(0.0)。
	// 障害物の裏で黒くならない場合、シェーダーには影が届いていない（＝ライト/キャスト/パイプライン側の問題）。
	if (debugView == ESL_DEBUG_SHADOW_ATTENUATION)
	{
		return diffuseData.rawShadowAttenuation.xxx;
	}

	// _ShadowStrength / _ShadowInfluence 適用後の遮蔽係数。最終的な減光量の確認用。
	if (debugView == ESL_DEBUG_OCCLUSION_SHADOW)
	{
		return diffuseData.occlusionShadow.xxx;
	}

	return color;
}

#endif