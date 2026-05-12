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

	float2 baseUv = frac(uv * max(_ToyDioramaGrainScale, 0.25));
	float temporalBlend = saturate(_ToyDioramaGrainTemporalStrength);
	float timePhase = _Time.y * temporalBlend;
	float2 offsetA = float2(0.06711056, 0.00583715) * timePhase;
	float2 offsetB = float2(0.75487767, 0.56984031) * timePhase;

	float noiseA = SAMPLE_TEXTURE2D(
		_ToyDioramaBlueNoiseTex,
		sampler_ToyDioramaBlueNoiseTex,
		frac(baseUv + offsetA)).r;

	float noiseB = SAMPLE_TEXTURE2D(
		_ToyDioramaBlueNoiseTex,
		sampler_ToyDioramaBlueNoiseTex,
		frac(baseUv * 1.37 + offsetB)).r;

	data.noiseValue = lerp(noiseA, noiseB, temporalBlend * 0.5);

	float centeredNoise = data.noiseValue * 2.0 - 1.0;
	float response = ToyDiorama_GrainResponseMask(saturate(ToyDiorama_Luminance(color)));
	float strength = saturate(_ToyDioramaGrainStrength) * response;

	data.grainContribution = centeredNoise * strength;
	data.afterGrain = saturate(color + data.grainContribution.xxx);
	data.color = data.afterGrain;

	return data;
}

#endif