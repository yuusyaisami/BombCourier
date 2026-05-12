#ifndef BC_TOY_DIORAMA_PASTEL_INCLUDED
#define BC_TOY_DIORAMA_PASTEL_INCLUDED

struct ToyDiorama_PastelData
{
	float3 color;
	float highSaturationMask;
	float pastelMask;
};

float ToyDiorama_CalculateHighSaturationMask(float3 color)
{
	float saturation = ToyDiorama_HsvSaturation(color);
	return smoothstep(0.38, 0.75, saturation);
}

float ToyDiorama_CalculatePastelMask(float3 color, float highSaturationMask)
{
	float luminance = ToyDiorama_Luminance(color);
	float luminanceWeight = smoothstep(0.12, 0.78, luminance);
	return saturate(highSaturationMask * lerp(0.65, 1.0, luminanceWeight));
}

ToyDiorama_PastelData ToyDiorama_ApplyPastelCompression(float3 color)
{
	ToyDiorama_PastelData data;

	float3 saturatedColor = ToyDiorama_ApplySaturation(color, _ToyDioramaSaturation);
	float saturation = ToyDiorama_HsvSaturation(saturatedColor);

	data.highSaturationMask = ToyDiorama_CalculateHighSaturationMask(saturatedColor);
	data.pastelMask = ToyDiorama_CalculatePastelMask(saturatedColor, data.highSaturationMask);

	float compression = saturate(_ToyDioramaPastelStrength) *
		saturate(lerp(1.0, saturation, saturate(_ToyDioramaHighSaturationCompress)));
	float saturationFactor = saturate(1.0 - data.pastelMask * compression);

	data.color = ToyDiorama_ApplySaturation(saturatedColor, saturationFactor);

	float luminance = ToyDiorama_Luminance(data.color);
	float luminanceOffset = data.pastelMask * _ToyDioramaPastelLuminanceBias * (1.0 - luminance) * 0.20;
	data.color = saturate(data.color + luminanceOffset.xxx);

	return data;
}

#endif