#ifndef BC_TOY_DIORAMA_COLOR_GRADE_INCLUDED
#define BC_TOY_DIORAMA_COLOR_GRADE_INCLUDED

struct ToyDiorama_ColorGradeMasks
{
    float luminance;
    float shadow;
    float mid;
    float highlight;
};

struct ToyDiorama_ColorPipelineData
{
    float3 beforePastel;
    float3 afterPastel;
    float3 afterColorGrade;
    ToyDiorama_ColorGradeMasks colorGradeMasks;
    ToyDiorama_PastelData pastelData;
    ToyDiorama_CreamHighlightData creamHighlightData;
};

float3 ToyDiorama_PrepareColorGradeBase(float3 sourceColor)
{
    float3 color = ToyDiorama_ApplyExposure(sourceColor, _ToyDioramaExposure);
    color = ToyDiorama_ApplyContrast(color, _ToyDioramaContrast);
    return max(color, 0.0);
}

ToyDiorama_ColorGradeMasks ToyDiorama_CalculateColorGradeMasks(float3 color)
{
    ToyDiorama_ColorGradeMasks masks;
    masks.luminance = saturate(ToyDiorama_Luminance(color));
    masks.shadow = ToyDiorama_ShadowMask(masks.luminance);
    masks.mid = ToyDiorama_MidMask(masks.luminance);
    masks.highlight = ToyDiorama_HighlightMask(masks.luminance);
    return masks;
}

float3 ToyDiorama_ApplyBlackLift(float3 color, ToyDiorama_ColorGradeMasks masks)
{
    float lift = saturate(_ToyDioramaBlackLift) * masks.shadow;
    return color + _ToyDioramaShadowTint.rgb * lift * 0.35;
}

float3 ToyDiorama_ApplyColorGradeTints(float3 color, ToyDiorama_ColorGradeMasks masks)
{
    color = ToyDiorama_TintByLuminance(
        color,
        _ToyDioramaShadowTint.rgb,
        masks.shadow * _ToyDioramaShadowTintStrength);

    color = ToyDiorama_TintByLuminance(
        color,
        _ToyDioramaMidTint.rgb,
        masks.mid * _ToyDioramaMidTintStrength);

    color = ToyDiorama_TintByLuminance(
        color,
        _ToyDioramaHighlightTint.rgb,
        masks.highlight * _ToyDioramaHighlightTintStrength);

    return color;
}

float3 ToyDiorama_ApplyBaseColorGrade(float3 sourceColor, out ToyDiorama_ColorGradeMasks masks)
{
    float3 color = ToyDiorama_PrepareColorGradeBase(sourceColor);
    masks = ToyDiorama_CalculateColorGradeMasks(color);

    color = ToyDiorama_ApplyBlackLift(color, masks);
    color = ToyDiorama_ApplyColorGradeTints(color, masks);
    color = ToyDiorama_SoftClampWhite(color, masks.highlight * _ToyDioramaWhiteSoftClamp);

    return saturate(color);
}

ToyDiorama_ColorPipelineData ToyDiorama_EvaluateColorPipeline(float3 sourceColor)
{
    ToyDiorama_ColorPipelineData data;

    if (_ToyDioramaEnabled < 0.5)
    {
        data.beforePastel = sourceColor;
        data.afterPastel = sourceColor;
        data.afterColorGrade = sourceColor;
        data.colorGradeMasks = ToyDiorama_CalculateColorGradeMasks(saturate(sourceColor));
        data.pastelData.color = sourceColor;
        data.pastelData.highSaturationMask = 0.0;
        data.pastelData.pastelMask = 0.0;
        data.creamHighlightData.color = sourceColor;
        data.creamHighlightData.mask = 0.0;
        return data;
    }

    data.beforePastel = ToyDiorama_ApplyBaseColorGrade(sourceColor, data.colorGradeMasks);
    data.pastelData = ToyDiorama_ApplyPastelCompression(data.beforePastel);
    data.afterPastel = data.pastelData.color;
    data.creamHighlightData = ToyDiorama_ApplyCreamHighlight(data.afterPastel);
    data.afterColorGrade = data.creamHighlightData.color;

    return data;
}

float3 ToyDiorama_ApplyColorGrade(float3 sourceColor)
{
    return ToyDiorama_EvaluateColorPipeline(sourceColor).afterColorGrade;
}

#endif