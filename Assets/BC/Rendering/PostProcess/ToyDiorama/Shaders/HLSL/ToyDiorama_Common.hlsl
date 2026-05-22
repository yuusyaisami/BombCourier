#ifndef BC_TOY_DIORAMA_COMMON_INCLUDED
#define BC_TOY_DIORAMA_COMMON_INCLUDED

// すべてのトーンマスク計算で使う線形輝度（Rec.709係数）を返します。
float ToyDiorama_Luminance(float3 color)
{
    return dot(color, float3(0.2126, 0.7152, 0.0722));
}

// RGB各チャネルの最大値/最小値を取得する補助関数です。
float ToyDiorama_Max3(float3 value)
{
    return max(value.r, max(value.g, value.b));
}

float ToyDiorama_Min3(float3 value)
{
    return min(value.r, min(value.g, value.b));
}

// RGBからHSV飽和度を近似計算します。暗部での0除算を防ぎます。
float ToyDiorama_HsvSaturation(float3 color)
{
    float maxChannel = ToyDiorama_Max3(color);
    float minChannel = ToyDiorama_Min3(color);

    if (maxChannel <= 0.0001)
    {
        return 0.0;
    }

    return saturate((maxChannel - minChannel) / maxChannel);
}

// 輝度を保ったまま彩度を調整します。
float3 ToyDiorama_ApplySaturation(float3 color, float saturation)
{
    float luminance = ToyDiorama_Luminance(color);
    return lerp(luminance.xxx, color, max(saturation, 0.0));
}

// 写真的な露出段で明るさを乗算します。
float3 ToyDiorama_ApplyExposure(float3 color, float exposureStops)
{
    return color * exp2(exposureStops);
}

// 正規化色空間で0.5を中心に対称なコントラストを適用します。
float3 ToyDiorama_ApplyContrast(float3 color, float contrast)
{
    return (color - 0.5) * max(contrast, 0.0) + 0.5;
}

// グレーディング各段で使うシャドウ/中間/ハイライト用マスクです。
float ToyDiorama_ShadowMask(float luminance)
{
    return 1.0 - smoothstep(0.16, 0.46, luminance);
}

float ToyDiorama_HighlightMask(float luminance)
{
    return smoothstep(0.56, 0.88, luminance);
}

float ToyDiorama_MidMask(float luminance)
{
    float lower = smoothstep(0.14, 0.46, luminance);
    float upper = 1.0 - smoothstep(0.54, 0.88, luminance);
    return saturate(lower * upper);
}

// 元の見かけ輝度を保ちながらティントを適用します。
float3 ToyDiorama_TintByLuminance(float3 color, float3 tint, float strength)
{
    float luminance = max(ToyDiorama_Luminance(color), 0.0001);
    float3 tintedColor = tint * luminance;
    return lerp(color, tintedColor, saturate(strength));
}

// 元の輝度を合わせたまま、ティント側の色相/彩度へ再配色します。
float3 ToyDiorama_ColorizePreserveLuminance(float3 color, float3 tint)
{
    float sourceLuminance = max(ToyDiorama_Luminance(color), 0.0001);
    float tintLuminance = max(ToyDiorama_Luminance(tint), 0.0001);
    return tint * (sourceLuminance / tintLuminance);
}

// フィルミックなショルダーで白飛びを抑えつつハイライトを圧縮します。
float3 ToyDiorama_SoftClampWhite(float3 color, float strength)
{
    float3 shoulderStart = 0.72;
    float3 overbright = max(color - shoulderStart, 0.0);
    float3 shouldered = shoulderStart + overbright / (1.0 + overbright);
    return lerp(color, shouldered, saturate(strength));
}

#endif