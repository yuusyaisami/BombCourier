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

// 0ベクトル近傍で不安定化しないよう、フォールバック付きで正規化します。
float3 ESL_SafeNormalize(float3 value, float3 fallback)
{
	float valueLengthSquared = dot(value, value);
	return valueLengthSquared > 1e-8 ? value * rsqrt(valueLengthSquared) : fallback;
}

// 両面描画時に面の向きを +1/-1 として取得します。
float ESL_GetFaceSign(FRONT_FACE_TYPE facing)
{
	return IS_FRONT_VFACE(facing, 1.0, -1.0);
}

// 背面時は法線を反転して、ライティング評価を一貫させます。
float3 ESL_ApplyFaceSignToNormal(float3 normalWS, FRONT_FACE_TYPE facing)
{
	return normalWS * ESL_GetFaceSign(facing);
}

// ラップドライティング（NdotLを持ち上げる拡散項）を計算します。
float ESL_ComputeWrappedLight(float ndotl, float wrapLighting)
{
	float clampedWrap = saturate(wrapLighting);
	return saturate((ndotl + clampedWrap) / (1.0 + clampedWrap));
}

// 段階的なトゥーン階調を作る関数です。smoothnessで段の境界を滑らかにできます。
float ESL_ComputeSteppedLight(float value, float stepCount, float smoothness)
{
	value = saturate(value);
	float bands = max(round(stepCount), 1.0);

	if (bands <= 1.0)
	{
		// 段数1は量子化せずそのまま返します。
		return value;
	}

	float scaled = value * bands;
	float lowerIndex = min(floor(scaled), bands - 1.0);
	float upperIndex = min(lowerIndex + 1.0, bands - 1.0);
	float lower = lowerIndex / (bands - 1.0);

	if (lowerIndex >= bands - 1.0)
	{
		// 最上段は必ず1.0へ固定します（上端の揺れ防止）。
		return 1.0;
	}

	if (smoothness <= 1e-4)
	{
		// 完全ハード段差モード。
		return saturate(lower);
	}

	float upper = upperIndex / (bands - 1.0);
	float fractional = frac(scaled);
	float edgeStart = 1.0 - saturate(smoothness);
	float transition = smoothstep(edgeStart, 1.0, fractional);
	return saturate(lerp(lower, upper, transition));
}

// タンジェント空間法線をワールド空間へ変換します。
// tangent.w の符号を使ってビタンジェントの向きを補正します。
float3 ESL_TransformNormalTangentToWorld(float3 normalTS, float3 normalWS, float4 tangentWS)
{
	float3 normalizedNormalWS = ESL_SafeNormalize(normalWS, float3(0.0, 0.0, 1.0));
	float3 normalizedTangentWS = ESL_SafeNormalize(tangentWS.xyz, float3(1.0, 0.0, 0.0));
	float3 bitangentWS = ESL_SafeNormalize(cross(normalizedNormalWS, normalizedTangentWS) * tangentWS.w, float3(0.0, 1.0, 0.0));
	float3x3 tangentToWorld = float3x3(normalizedTangentWS, bitangentWS, normalizedNormalWS);
	return ESL_SafeNormalize(mul(normalTS, tangentToWorld), normalizedNormalWS);
}

#endif