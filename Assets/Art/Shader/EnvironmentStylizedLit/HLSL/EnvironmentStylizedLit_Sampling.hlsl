#ifndef BC_ENVIRONMENT_STYLIZED_LIT_SAMPLING_INCLUDED
#define BC_ENVIRONMENT_STYLIZED_LIT_SAMPLING_INCLUDED

#include "EnvironmentStylizedLit_Input.hlsl"
#include "EnvironmentStylizedLit_Common.hlsl"
#include "EnvironmentStylizedLit_Triplanar.hlsl"

float2 ESL_TransformBaseUV(float2 uv)
{
	return uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
}

half4 ESL_SampleBaseColor(float2 uv)
{
	float2 baseUV = ESL_TransformBaseUV(uv);
	return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, baseUV) * half4(_BaseColor);
}

half4 ESL_SampleBaseColor(float2 uv, float3 positionWS, float3 normalWS)
{
	if (ESL_IsTriplanarBaseMapEnabled())
	{
		return ESL_SampleTriplanarBaseColor(positionWS, normalWS);
	}

	return ESL_SampleBaseColor(uv);
}

half ESL_SampleBaseAlpha(float2 uv)
{
	return ESL_SampleBaseColor(uv).a;
}

half ESL_SampleBaseAlpha(float2 uv, float3 positionWS, float3 normalWS)
{
	return ESL_SampleBaseColor(uv, positionWS, normalWS).a;
}

float3 ESL_SampleNormalTS(float2 uv)
{
	float2 baseUV = ESL_TransformBaseUV(uv);
	return UnpackNormalScale(
		SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, baseUV),
		_NormalScale);
}

float3 ESL_SampleSurfaceNormalWS(float2 uv, float3 positionWS, float3 normalWS, float4 tangentWS)
{
	if (ESL_IsTriplanarNormalMapEnabled())
	{
		return ESL_SampleTriplanarNormalWS(positionWS, normalWS);
	}

	return ESL_TransformNormalTangentToWorld(ESL_SampleNormalTS(uv), normalWS, tangentWS);
}

float3 ESL_SampleEmission(float2 uv)
{
	float2 baseUV = ESL_TransformBaseUV(uv);
	half3 emissionSample = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, baseUV).rgb;
	return emissionSample * _EmissionColor.rgb * _EmissionStrength;
}

void ESL_ApplyAlphaClip(float alpha)
{
	if (_AlphaClip > 0.5)
	{
		clip(alpha - _Cutoff);
	}
}

void ESL_ApplyAlphaClipFromUV(float2 uv)
{
	ESL_ApplyAlphaClip(ESL_SampleBaseAlpha(uv));
}

void ESL_ApplyAlphaClipFromSurface(float2 uv, float3 positionWS, float3 normalWS)
{
	ESL_ApplyAlphaClip(ESL_SampleBaseAlpha(uv, positionWS, normalWS));
}

#endif