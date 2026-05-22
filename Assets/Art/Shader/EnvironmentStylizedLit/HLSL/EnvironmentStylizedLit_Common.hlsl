#ifndef BC_ENVIRONMENT_STYLIZED_LIT_COMMON_INCLUDED
#define BC_ENVIRONMENT_STYLIZED_LIT_COMMON_INCLUDED

struct ESL_InputData
{
	float3 positionWS;
	float3 normalWS;
	float3 viewDirectionWS;
	float4 vertexColor;
	float3 vertexLighting;
	float3 bakedGI;
	float4 shadowCoord;
	float4 shadowMask;
	float2 normalizedScreenSpaceUV;
	float indirectAmbientOcclusion;
	float directAmbientOcclusion;
	float fogFactor;
};

float3 ESL_SafeNormalize(float3 value, float3 fallback)
{
	float valueLengthSquared = dot(value, value);
	return valueLengthSquared > 1e-8 ? value * rsqrt(valueLengthSquared) : fallback;
}

float ESL_GetFaceSign(FRONT_FACE_TYPE facing)
{
	return IS_FRONT_VFACE(facing, 1.0, -1.0);
}

float3 ESL_ApplyFaceSignToNormal(float3 normalWS, FRONT_FACE_TYPE facing)
{
	return normalWS * ESL_GetFaceSign(facing);
}

float ESL_ComputeWrappedLight(float ndotl, float wrapLighting)
{
	float clampedWrap = saturate(wrapLighting);
	return saturate((ndotl + clampedWrap) / (1.0 + clampedWrap));
}

float ESL_ComputeSteppedLight(float value, float stepCount, float smoothness)
{
	value = saturate(value);
	float bands = max(round(stepCount), 1.0);

	if (bands <= 1.0)
	{
		return value;
	}

	float scaled = value * bands;
	float lowerIndex = min(floor(scaled), bands - 1.0);
	float upperIndex = min(lowerIndex + 1.0, bands - 1.0);
	float lower = lowerIndex / (bands - 1.0);

	if (lowerIndex >= bands - 1.0)
	{
		return 1.0;
	}

	if (smoothness <= 1e-4)
	{
		return saturate(lower);
	}

	float upper = upperIndex / (bands - 1.0);
	float fractional = frac(scaled);
	float edgeStart = 1.0 - saturate(smoothness);
	float transition = smoothstep(edgeStart, 1.0, fractional);
	return saturate(lerp(lower, upper, transition));
}

float3 ESL_TransformNormalTangentToWorld(float3 normalTS, float3 normalWS, float4 tangentWS)
{
	float3 normalizedNormalWS = ESL_SafeNormalize(normalWS, float3(0.0, 0.0, 1.0));
	float3 normalizedTangentWS = ESL_SafeNormalize(tangentWS.xyz, float3(1.0, 0.0, 0.0));
	float3 bitangentWS = ESL_SafeNormalize(cross(normalizedNormalWS, normalizedTangentWS) * tangentWS.w, float3(0.0, 1.0, 0.0));
	float3x3 tangentToWorld = float3x3(normalizedTangentWS, bitangentWS, normalizedNormalWS);
	return ESL_SafeNormalize(mul(normalTS, tangentToWorld), normalizedNormalWS);
}

#endif