#ifndef BC_ENVIRONMENT_STYLIZED_LIT_NOISE_INCLUDED
#define BC_ENVIRONMENT_STYLIZED_LIT_NOISE_INCLUDED

#include "EnvironmentStylizedLit_Input.hlsl"
#include "EnvironmentStylizedLit_Common.hlsl"
#include "EnvironmentStylizedLit_Triplanar.hlsl"

#define ESL_NOISE_SPACE_WORLD 0
#define ESL_NOISE_SPACE_OBJECT 1

struct ESL_NoiseData
{
	float worldNoise;
	float objectSpaceNoise;
	float surfaceNoise;
	float bandNoise;
	float distanceFade;
};

// ノイズ座標系（ワールド/オブジェクト）を取得します。
int ESL_GetNoiseSpace()
{
	return (int)round(_NoiseSpace);
}

// 3D/2D値ノイズ用のハッシュ関数。
float ESL_Hash13(float3 value)
{
	value = frac(value * 0.1031);
	value += dot(value, value.yzx + 33.33);
	return frac((value.x + value.y) * value.z);
}

float ESL_Hash12(float2 value)
{
	float3 value3 = frac(float3(value.xyx) * 0.1031);
	value3 += dot(value3, value3.yzx + 33.33);
	return frac((value3.x + value3.y) * value3.z);
}

// 3D value noise（格子8点を補間）を生成します。
float ESL_SampleValueNoise(float3 samplePosition)
{
	float3 cell = floor(samplePosition);
	float3 cellFraction = frac(samplePosition);
	float3 smoothFraction = cellFraction * cellFraction * (3.0 - 2.0 * cellFraction);

	float noise000 = ESL_Hash13(cell + float3(0.0, 0.0, 0.0));
	float noise100 = ESL_Hash13(cell + float3(1.0, 0.0, 0.0));
	float noise010 = ESL_Hash13(cell + float3(0.0, 1.0, 0.0));
	float noise110 = ESL_Hash13(cell + float3(1.0, 1.0, 0.0));
	float noise001 = ESL_Hash13(cell + float3(0.0, 0.0, 1.0));
	float noise101 = ESL_Hash13(cell + float3(1.0, 0.0, 1.0));
	float noise011 = ESL_Hash13(cell + float3(0.0, 1.0, 1.0));
	float noise111 = ESL_Hash13(cell + float3(1.0, 1.0, 1.0));

	float noiseX00 = lerp(noise000, noise100, smoothFraction.x);
	float noiseX10 = lerp(noise010, noise110, smoothFraction.x);
	float noiseX01 = lerp(noise001, noise101, smoothFraction.x);
	float noiseX11 = lerp(noise011, noise111, smoothFraction.x);
	float noiseY0 = lerp(noiseX00, noiseX10, smoothFraction.y);
	float noiseY1 = lerp(noiseX01, noiseX11, smoothFraction.y);
	return lerp(noiseY0, noiseY1, smoothFraction.z);
}

// 2D value noise（格子4点を補間）を生成します。
float ESL_SampleValueNoise2D(float2 samplePosition)
{
	float2 cell = floor(samplePosition);
	float2 cellFraction = frac(samplePosition);
	float2 smoothFraction = cellFraction * cellFraction * (3.0 - 2.0 * cellFraction);

	float noise00 = ESL_Hash12(cell + float2(0.0, 0.0));
	float noise10 = ESL_Hash12(cell + float2(1.0, 0.0));
	float noise01 = ESL_Hash12(cell + float2(0.0, 1.0));
	float noise11 = ESL_Hash12(cell + float2(1.0, 1.0));

	float noiseX0 = lerp(noise00, noise10, smoothFraction.x);
	float noiseX1 = lerp(noise01, noise11, smoothFraction.x);
	return lerp(noiseX0, noiseX1, smoothFraction.y);
}

// ノイズ分布のコントラストを調整します。
float ESL_ApplyNoiseContrast(float noiseValue)
{
	float contrastedValue = (saturate(noiseValue) - 0.5) * max(_WorldNoiseContrast, 0.0001) + 0.5;
	return saturate(contrastedValue);
}

// カメラ距離に応じたノイズ減衰マスクを計算します。
float ESL_EvaluateNoiseDistanceFade(float3 positionWS)
{
	float fadeStart = max(_NoiseDistanceFadeStart, 0.0);
	float fadeEnd = max(_NoiseDistanceFadeEnd, fadeStart);
	float cameraDistance = distance(GetCameraPositionWS(), positionWS);

	if (fadeEnd <= fadeStart + 1e-4)
	{
		return cameraDistance <= fadeStart ? 1.0 : 0.0;
	}

	return 1.0 - saturate((cameraDistance - fadeStart) / (fadeEnd - fadeStart));
}

// 値ノイズを -1..1 の符号付きノイズへ変換します。
float ESL_EvaluateSignedNoise(float3 samplePosition, float noiseStrength)
{
	if (noiseStrength <= 1e-4)
	{
		return 0.0;
	}

	float rawNoise = ESL_SampleValueNoise(samplePosition);
	float contrastedNoise = ESL_ApplyNoiseContrast(rawNoise);
	return (contrastedNoise - 0.5) * 2.0 * saturate(noiseStrength);
}

// ワールド座標ノイズ評価。
float ESL_EvaluateScaledWorldNoise(float3 positionWS, float noiseScale, float noiseStrength)
{
	if (noiseScale <= 1e-4)
	{
		return 0.0;
	}

	return ESL_EvaluateSignedNoise(positionWS * noiseScale, noiseStrength);
}

// トライプラナー合成で符号付きノイズを評価します。
float ESL_EvaluateSignedTriplanarNoise(float3 positionWS, float3 normalWS, float noiseScale, float noiseStrength)
{
	if (noiseScale <= 1e-4 || noiseStrength <= 1e-4)
	{
		return 0.0;
	}

	ESL_TriplanarData triplanarData = ESL_BuildTriplanarData(positionWS * noiseScale, normalWS, 1.0.xx, 0.0.xx);
	float rawNoise = ESL_SampleValueNoise2D(triplanarData.uvX) * triplanarData.weights.x
		+ ESL_SampleValueNoise2D(triplanarData.uvY) * triplanarData.weights.y
		+ ESL_SampleValueNoise2D(triplanarData.uvZ) * triplanarData.weights.z;
	float contrastedNoise = ESL_ApplyNoiseContrast(rawNoise);
	return (contrastedNoise - 0.5) * 2.0 * saturate(noiseStrength);
}

// オブジェクト空間ノイズ評価。
float ESL_EvaluateScaledObjectSpaceNoise(float3 positionWS, float noiseScale, float noiseStrength)
{
	if (noiseScale <= 1e-4)
	{
		return 0.0;
	}

	float3 positionOS = TransformWorldToObject(positionWS);
	return ESL_EvaluateSignedNoise(positionOS * noiseScale, noiseStrength);
}

// 用途別のノイズ公開関数群。
float ESL_EvaluateWorldNoise(float3 positionWS)
{
	float distanceFade = ESL_EvaluateNoiseDistanceFade(positionWS);
	return ESL_EvaluateScaledWorldNoise(positionWS, _WorldNoiseScale, _WorldNoiseStrength) * distanceFade;
}

float ESL_EvaluateObjectSpaceNoise(float3 positionWS)
{
	float distanceFade = ESL_EvaluateNoiseDistanceFade(positionWS);
	return ESL_EvaluateScaledObjectSpaceNoise(positionWS, _WorldNoiseScale, _WorldNoiseStrength) * distanceFade;
}

float ESL_EvaluateNoiseBySpace(float3 positionWS, float noiseScale, float noiseStrength, int noiseSpace)
{
	if (noiseSpace == ESL_NOISE_SPACE_OBJECT)
	{
		return ESL_EvaluateScaledObjectSpaceNoise(positionWS, noiseScale, noiseStrength);
	}

	return ESL_EvaluateScaledWorldNoise(positionWS, noiseScale, noiseStrength);
}

// ベース色へ掛けるサーフェスノイズ。
float ESL_EvaluateSurfaceNoise(float3 positionWS, float3 normalWS)
{
	float distanceFade = ESL_EvaluateNoiseDistanceFade(positionWS);
	int noiseSpace = ESL_GetNoiseSpace();

	if (ESL_IsTriplanarNoiseEnabled())
	{
		return ESL_EvaluateSignedTriplanarNoise(positionWS, normalWS, _WorldNoiseScale, _WorldNoiseStrength) * distanceFade;
	}

	return ESL_EvaluateNoiseBySpace(positionWS, _WorldNoiseScale, _WorldNoiseStrength, noiseSpace) * distanceFade;
}

// ライト段差に加える帯域ノイズ。
float ESL_EvaluateBandNoise(float3 positionWS, float3 normalWS)
{
	float distanceFade = ESL_EvaluateNoiseDistanceFade(positionWS);
	int noiseSpace = ESL_GetNoiseSpace();

	if (_LightBandNoiseScale <= 1e-4 || _LightBandNoiseStrength <= 1e-4 || distanceFade <= 1e-4)
	{
		return 0.0;
	}

	if (ESL_IsTriplanarNoiseEnabled())
	{
		return ESL_EvaluateSignedTriplanarNoise(positionWS, normalWS, _LightBandNoiseScale, _LightBandNoiseStrength) * distanceFade;
	}

	float selectedNoise = ESL_EvaluateNoiseBySpace(positionWS, _LightBandNoiseScale, 1.0, noiseSpace);
	return selectedNoise * saturate(_LightBandNoiseStrength) * distanceFade;
}

// デバッグ/他段再利用のため、各ノイズ寄与を一括評価します。
ESL_NoiseData ESL_EvaluateNoiseData(float3 positionWS, float3 normalWS)
{
	ESL_NoiseData noiseData;
	int noiseSpace = ESL_GetNoiseSpace();
	noiseData.distanceFade = ESL_EvaluateNoiseDistanceFade(positionWS);
	noiseData.worldNoise = 0.0;
	noiseData.objectSpaceNoise = 0.0;
	noiseData.surfaceNoise = 0.0;
	noiseData.bandNoise = 0.0;

	if (_WorldNoiseScale > 1e-4 && _WorldNoiseStrength > 1e-4 && noiseData.distanceFade > 1e-4)
	{
		noiseData.worldNoise = ESL_EvaluateScaledWorldNoise(positionWS, _WorldNoiseScale, _WorldNoiseStrength) * noiseData.distanceFade;

		if (ESL_IsTriplanarNoiseEnabled())
		{
			noiseData.surfaceNoise = ESL_EvaluateSignedTriplanarNoise(positionWS, normalWS, _WorldNoiseScale, _WorldNoiseStrength) * noiseData.distanceFade;
		}
		else if (noiseSpace == ESL_NOISE_SPACE_OBJECT)
		{
			noiseData.objectSpaceNoise = ESL_EvaluateScaledObjectSpaceNoise(positionWS, _WorldNoiseScale, _WorldNoiseStrength) * noiseData.distanceFade;
			noiseData.surfaceNoise = noiseData.objectSpaceNoise;
		}
		else
		{
			noiseData.surfaceNoise = noiseData.worldNoise;
		}
	}

	if (_LightBandNoiseScale > 1e-4 && _LightBandNoiseStrength > 1e-4 && noiseData.distanceFade > 1e-4)
	{
		if (ESL_IsTriplanarNoiseEnabled())
		{
			noiseData.bandNoise = ESL_EvaluateSignedTriplanarNoise(positionWS, normalWS, _LightBandNoiseScale, _LightBandNoiseStrength) * noiseData.distanceFade;
		}
		else
		{
			noiseData.bandNoise = ESL_EvaluateNoiseBySpace(positionWS, _LightBandNoiseScale, 1.0, noiseSpace)
				* saturate(_LightBandNoiseStrength)
				* noiseData.distanceFade;
		}
	}

	return noiseData;
}

#endif