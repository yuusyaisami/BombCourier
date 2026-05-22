#ifndef BC_TOY_DIORAMA_PASTEL_INCLUDED
#define BC_TOY_DIORAMA_PASTEL_INCLUDED

struct ToyDiorama_PastelData
{
	float3 color;
	float highSaturationMask;
	float pastelMask;
};

// パステル方向に圧縮すべき高彩度ピクセルを検出します。
float ToyDiorama_CalculateHighSaturationMask(float3 color)
{
	float saturation = ToyDiorama_HsvSaturation(color);
	return smoothstep(0.38, 0.75, saturation);
}

// 暗部ディテールを潰しすぎないよう、中〜高輝度を優先してマスクを強めます。
float ToyDiorama_CalculatePastelMask(float3 color, float highSaturationMask)
{
	float luminance = ToyDiorama_Luminance(color);
	float luminanceWeight = smoothstep(0.12, 0.78, luminance);
	return saturate(highSaturationMask * lerp(0.65, 1.0, luminanceWeight));
}

// パステル処理の本体です。基準彩度を適用後、選択的に彩度圧縮とトーン持ち上げを行います。
ToyDiorama_PastelData ToyDiorama_ApplyPastelCompression(float3 color)
{
	ToyDiorama_PastelData data;

	float3 saturatedColor = ToyDiorama_ApplySaturation(color, _ToyDioramaSaturation);
	float saturation = ToyDiorama_HsvSaturation(saturatedColor);

	data.highSaturationMask = ToyDiorama_CalculateHighSaturationMask(saturatedColor);
	data.pastelMask = ToyDiorama_CalculatePastelMask(saturatedColor, data.highSaturationMask);

	// 圧縮量はグローバル強度とピクセルごとの彩度を合成して決定します。
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