#ifndef BC_ENVIRONMENT_STYLIZED_LIT_TRIPLANAR_INCLUDED
#define BC_ENVIRONMENT_STYLIZED_LIT_TRIPLANAR_INCLUDED

#include "EnvironmentStylizedLit_Input.hlsl"

struct ESL_TriplanarData
{
	float3 weights;
	float2 uvX;
	float2 uvY;
	float2 uvZ;
	float3 axisSign;
};

// キーワード有効時のみトライプラナー機能をONにします。
bool ESL_IsTriplanarBaseMapEnabled()
{
	#if defined(_ESL_TRIPLANAR_BASEMAP)
	return _TriplanarBaseMapEnabled > 0.5;
	#else
	return false;
	#endif
}

bool ESL_IsTriplanarNormalMapEnabled()
{
	#if defined(_ESL_TRIPLANAR_NORMALMAP)
	return _TriplanarNormalMapEnabled > 0.5;
	#else
	return false;
	#endif
}

bool ESL_IsTriplanarNoiseEnabled()
{
	#if defined(_ESL_TRIPLANAR_NOISE)
	return _TriplanarNoiseEnabled > 0.5;
	#else
	return false;
	#endif
}

// UVのスケール・オフセット変換。
float2 ESL_TransformTriplanarUV(float2 uv, float2 scale, float2 offset)
{
	return uv * scale + offset;
}

float2 ESL_TransformTriplanarUV(float2 uv)
{
	return ESL_TransformTriplanarUV(uv, _BaseMap_ST.xy * _TriplanarScale, _BaseMap_ST.zw);
}

// 法線ベースの重みを作り、X/Y/Z投影用UVを構築します。
// BlendSharpnessを上げるほど軸境界がシャープになります。
ESL_TriplanarData ESL_BuildTriplanarData(float3 positionWS, float3 normalWS, float2 uvScale, float2 uvOffset)
{
	ESL_TriplanarData triplanarData;
	float3 safeNormalWS = normalize(normalWS);
	float3 absNormalWS = abs(safeNormalWS);
	float sharpness = max(_TriplanarBlendSharpness, 1.0);
	float3 weightedNormalWS = pow(max(absNormalWS, 1e-4), sharpness);
	triplanarData.weights = weightedNormalWS / max(dot(weightedNormalWS, 1.0.xxx), 1e-4);
	triplanarData.axisSign = safeNormalWS >= 0.0 ? 1.0.xxx : -1.0.xxx;
	triplanarData.uvX = ESL_TransformTriplanarUV(float2(positionWS.z * triplanarData.axisSign.x, positionWS.y), uvScale, uvOffset);
	triplanarData.uvY = ESL_TransformTriplanarUV(float2(positionWS.x, positionWS.z * triplanarData.axisSign.y), uvScale, uvOffset);
	triplanarData.uvZ = ESL_TransformTriplanarUV(float2(positionWS.x * triplanarData.axisSign.z, positionWS.y), uvScale, uvOffset);
	return triplanarData;
}

ESL_TriplanarData ESL_BuildTriplanarData(float3 positionWS, float3 normalWS)
{
	return ESL_BuildTriplanarData(positionWS, normalWS, _BaseMap_ST.xy * _TriplanarScale, _BaseMap_ST.zw);
}

// 3軸テクスチャを重み付き合成してベース色を返します。
half4 ESL_SampleTriplanarBaseColor(float3 positionWS, float3 normalWS)
{
	ESL_TriplanarData triplanarData = ESL_BuildTriplanarData(positionWS, normalWS);
	half4 sampleX = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, triplanarData.uvX);
	half4 sampleY = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, triplanarData.uvY);
	half4 sampleZ = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, triplanarData.uvZ);
	half4 blendedSample = sampleX * triplanarData.weights.x
		+ sampleY * triplanarData.weights.y
		+ sampleZ * triplanarData.weights.z;
	return blendedSample * half4(_BaseColor);
}

// 各投影軸で、タンジェント法線を対応するワールド軸へ再配置します。
float3 ESL_ReorientTriplanarNormalX(float3 tangentNormal, float axisSign)
{
	tangentNormal.xy *= float2(axisSign, 1.0);
	return normalize(float3(tangentNormal.z * axisSign, tangentNormal.y, tangentNormal.x));
}

float3 ESL_ReorientTriplanarNormalY(float3 tangentNormal, float axisSign)
{
	tangentNormal.xy *= float2(1.0, axisSign);
	return normalize(float3(tangentNormal.x, tangentNormal.z * axisSign, tangentNormal.y));
}

float3 ESL_ReorientTriplanarNormalZ(float3 tangentNormal, float axisSign)
{
	tangentNormal.xy *= float2(axisSign, 1.0);
	return normalize(float3(tangentNormal.x, tangentNormal.y, tangentNormal.z * axisSign));
}

// 3軸法線を再配置後に重み付き合成し、最終ワールド法線を得ます。
float3 ESL_SampleTriplanarNormalWS(float3 positionWS, float3 normalWS)
{
	ESL_TriplanarData triplanarData = ESL_BuildTriplanarData(positionWS, normalWS);
	float3 tangentNormalX = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, triplanarData.uvX), _NormalScale);
	float3 tangentNormalY = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, triplanarData.uvY), _NormalScale);
	float3 tangentNormalZ = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, triplanarData.uvZ), _NormalScale);
	float3 worldNormalX = ESL_ReorientTriplanarNormalX(tangentNormalX, triplanarData.axisSign.x);
	float3 worldNormalY = ESL_ReorientTriplanarNormalY(tangentNormalY, triplanarData.axisSign.y);
	float3 worldNormalZ = ESL_ReorientTriplanarNormalZ(tangentNormalZ, triplanarData.axisSign.z);
	return normalize(worldNormalX * triplanarData.weights.x + worldNormalY * triplanarData.weights.y + worldNormalZ * triplanarData.weights.z);
}

#endif