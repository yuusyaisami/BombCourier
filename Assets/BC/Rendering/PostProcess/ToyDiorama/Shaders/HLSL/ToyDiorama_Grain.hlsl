#ifndef BC_TOY_DIORAMA_GRAIN_INCLUDED
#define BC_TOY_DIORAMA_GRAIN_INCLUDED

struct ToyDiorama_GrainData
{
	float3 beforeGrain;
	float3 afterGrain;
	float3 color;
	float noiseValue;
	float grainContribution;
};

float ToyDiorama_Hash12(float2 value)
{
	float3 value3 = frac(float3(value.xyx) * 0.1031);
	value3 += dot(value3, value3.yzx + 33.33);
	return frac((value3.x + value3.y) * value3.z);
}

ToyDiorama_GrainData ToyDiorama_CreateGrainNoOp(float3 color)
{
	ToyDiorama_GrainData data;
	data.beforeGrain = color;
	data.afterGrain = color;
	data.color = color;
	data.noiseValue = 0.5;
	data.grainContribution = 0.0;
	return data;
}

float ToyDiorama_GrainResponseMask(float luminance)
{
	float midResponse = 1.0 - abs(luminance * 2.0 - 1.0);
	return lerp(1.0, saturate(0.2 + midResponse * 0.8), saturate(_ToyDioramaGrainResponse));
}

ToyDiorama_GrainData ToyDiorama_ApplyGrain(float3 color, float2 uv)
{
	if (_ToyDioramaGrainEnabled < 0.5 || _ToyDioramaGrainStrength <= 0.0)
	{
		return ToyDiorama_CreateGrainNoOp(color);
	}

	ToyDiorama_GrainData data;
	data.beforeGrain = color;

	float2 screenPixels = uv * _ScreenParams.xy;
	float grainCellSize = max(_ToyDioramaGrainScale, 0.25);
	float2 grainCoordinate = floor(screenPixels / grainCellSize);
	float temporalBlend = saturate(_ToyDioramaGrainTemporalStrength);
	float temporalRate = lerp(0.0, 8.0, temporalBlend);
	float temporalPhase = floor(_Time.y * temporalRate);
	float temporalFraction = frac(_Time.y * temporalRate);
	float2 phaseOffsetA = float2(temporalPhase, temporalPhase * 0.61803399);
	float2 phaseOffsetB = float2(temporalPhase + 1.0, (temporalPhase + 1.0) * 0.61803399);

	float noiseA = ToyDiorama_Hash12(grainCoordinate + phaseOffsetA);
	float noiseB = ToyDiorama_Hash12(grainCoordinate + phaseOffsetB);

	data.noiseValue = lerp(noiseA, noiseB, temporalFraction);

	float centeredNoise = data.noiseValue * 2.0 - 1.0;
	float response = ToyDiorama_GrainResponseMask(saturate(ToyDiorama_Luminance(color)));
	float strength = saturate(_ToyDioramaGrainStrength) * response;

	data.grainContribution = centeredNoise * strength;
	data.afterGrain = saturate(color + data.grainContribution.xxx);
	data.color = data.afterGrain;

	return data;
}

#endif