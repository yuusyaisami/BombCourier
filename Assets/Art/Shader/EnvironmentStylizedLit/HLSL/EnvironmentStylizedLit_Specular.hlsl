#ifndef BC_ENVIRONMENT_STYLIZED_LIT_SPECULAR_INCLUDED
#define BC_ENVIRONMENT_STYLIZED_LIT_SPECULAR_INCLUDED

#include "EnvironmentStylizedLit_Input.hlsl"
#include "EnvironmentStylizedLit_Common.hlsl"

#define ESL_SPECULAR_MODE_OFF 0
#define ESL_SPECULAR_MODE_SOFT 1
#define ESL_SPECULAR_MODE_QUANTIZED 2
#define ESL_SPECULAR_MODE_CERAMIC 3
#define ESL_SPECULAR_MODE_PLASTIC 4

struct ESL_SpecularData
{
	float ndoth;
	float viewFresnel;
	float specularTerm;
	float3 specularColor;
	float edgeSheenTerm;
	float3 edgeSheenColor;
	float3 combinedSpecular;
};

// Material設定のスペキュラモデルを整数モードへ変換します。
int ESL_GetSpecularMode()
{
	return (int)round(_SpecularMode);
}

// Smoothnessに応じて指数を補間し、ハイライトの鋭さを制御します。
float ESL_GetSpecularExponent(float smoothness, float minExponent, float maxExponent)
{
	return lerp(minExponent, maxExponent, saturate(smoothness));
}

// ソフトな連続スペキュラ。
float ESL_EvaluateSoftSpecularTerm(float ndoth, float smoothness)
{
	return pow(ndoth, ESL_GetSpecularExponent(smoothness, 6.0, 24.0));
}

// 指数ハイライトを段階化してトゥーン風の鏡面を作ります。
float ESL_EvaluateQuantizedSpecularTerm(float ndoth, float smoothness)
{
	float baseTerm = pow(ndoth, ESL_GetSpecularExponent(smoothness, 18.0, 72.0));
	return ESL_ComputeSteppedLight(baseTerm, max(_SpecularStepCount, 1.0), saturate(_SpecularStepSmoothness));
}

// 陶器系の硬い光沢向け。指数を高めてからsmoothstepで整形します。
float ESL_EvaluateCeramicSpecularTerm(float ndoth, float smoothness)
{
	float baseTerm = pow(ndoth, ESL_GetSpecularExponent(smoothness, 28.0, 120.0));
	return smoothstep(0.08, 0.92, baseTerm);
}

// 樹脂系のやや広い光沢向け。通常項とラップ項を混合します。
float ESL_EvaluatePlasticSpecularTerm(float ndoth, float smoothness)
{
	float baseTerm = pow(ndoth, ESL_GetSpecularExponent(smoothness, 14.0, 48.0));
	return saturate(lerp(baseTerm, ESL_ComputeWrappedLight(baseTerm, 0.25), 0.45));
}

// モード分岐で最終スペキュラ項を決定します。
float ESL_EvaluateSpecularTerm(float ndoth, float smoothness)
{
	int specularMode = ESL_GetSpecularMode();

	if (specularMode == ESL_SPECULAR_MODE_SOFT)
	{
		return ESL_EvaluateSoftSpecularTerm(ndoth, smoothness);
	}

	if (specularMode == ESL_SPECULAR_MODE_QUANTIZED)
	{
		return ESL_EvaluateQuantizedSpecularTerm(ndoth, smoothness);
	}

	if (specularMode == ESL_SPECULAR_MODE_CERAMIC)
	{
		return ESL_EvaluateCeramicSpecularTerm(ndoth, smoothness);
	}

	if (specularMode == ESL_SPECULAR_MODE_PLASTIC)
	{
		return ESL_EvaluatePlasticSpecularTerm(ndoth, smoothness);
	}

	return 0.0;
}

// 視線フレネルと面向きからエッジシーン項を評価します。
float ESL_EvaluateEdgeSheenTerm(float3 normalWS, float3 viewDirectionWS, float3 lightDirectionWS, float shadowAttenuation)
{
	float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirectionWS)), max(_EdgeSheenPower, 0.001));
	float lightFacing = ESL_ComputeWrappedLight(saturate(dot(normalWS, lightDirectionWS)), 0.45);
	return fresnel * lightFacing * saturate(_EdgeSheenStrength) * shadowAttenuation;
}

// 主光源に対するスペキュラ関連データ（本体+エッジ）をまとめて算出します。
ESL_SpecularData ESL_EvaluateSpecularData(
	ESL_InputData inputData,
	float3 lightDirectionWS,
	float3 lightColor,
	float distanceAttenuation,
	float shadowAttenuation)
{
	float3 normalizedNormalWS = ESL_SafeNormalize(inputData.normalWS, float3(0.0, 1.0, 0.0));
	float3 normalizedViewDirectionWS = ESL_SafeNormalize(inputData.viewDirectionWS, float3(0.0, 0.0, 1.0));
	float3 halfDirectionWS = ESL_SafeNormalize(normalizedViewDirectionWS + lightDirectionWS, lightDirectionWS);
	float lightFacing = ESL_ComputeWrappedLight(saturate(dot(normalizedNormalWS, lightDirectionWS)), 0.25);

	ESL_SpecularData specularData;
	specularData.ndoth = saturate(dot(normalizedNormalWS, halfDirectionWS));
	specularData.viewFresnel = pow(1.0 - saturate(dot(normalizedNormalWS, normalizedViewDirectionWS)), 5.0);
	specularData.specularTerm = ESL_EvaluateSpecularTerm(specularData.ndoth, _Smoothness)
		* saturate(_SpecularStrength)
		* lightFacing
		* distanceAttenuation
		* shadowAttenuation;
	specularData.specularColor = _SpecularColor.rgb * lightColor * specularData.specularTerm;
	specularData.edgeSheenTerm = ESL_EvaluateEdgeSheenTerm(normalizedNormalWS, normalizedViewDirectionWS, lightDirectionWS, shadowAttenuation)
		* distanceAttenuation;
	specularData.edgeSheenColor = _EdgeSheenColor.rgb * lightColor * specularData.edgeSheenTerm;
	specularData.combinedSpecular = specularData.specularColor + specularData.edgeSheenColor;
	return specularData;
}

#endif