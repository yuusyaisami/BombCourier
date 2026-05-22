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
    float3 beforeBloom;
    float3 afterColorGrade;
    ToyDiorama_ColorGradeMasks colorGradeMasks;
    ToyDiorama_PastelData pastelData;
    ToyDiorama_CreamHighlightData creamHighlightData;
    ToyDiorama_DepthHazeData depthHazeData;
    ToyDiorama_EdgeToneData edgeToneData;
    ToyDiorama_GrainData grainData;
};

struct ToyDiorama_FinalColorPipelineData
{
    float3 beforeBloom;
    float4 bloomComposite;
    float3 afterBloom;
    float3 afterColorGrade;
    ToyDiorama_EdgeToneData edgeToneData;
    ToyDiorama_GrainData grainData;
};

// 露出/コントラストの基礎整形。以降のマスク・ティント評価の前処理です。
float3 ToyDiorama_PrepareColorGradeBase(float3 sourceColor)
{
    float3 color = ToyDiorama_ApplyExposure(sourceColor, _ToyDioramaExposure);
    color = ToyDiorama_ApplyContrast(color, _ToyDioramaContrast);
    return max(color, 0.0);
}

// 複数段で再利用する輝度帯マスクを先に計算します。
ToyDiorama_ColorGradeMasks ToyDiorama_CalculateColorGradeMasks(float3 color)
{
    ToyDiorama_ColorGradeMasks masks;
    masks.luminance = saturate(ToyDiorama_Luminance(color));
    masks.shadow = ToyDiorama_ShadowMask(masks.luminance);
    masks.mid = ToyDiorama_MidMask(masks.luminance);
    masks.highlight = ToyDiorama_HighlightMask(masks.luminance);
    return masks;
}

// シャドウ帯に限定した黒浮きを、シャドウティント付きで付与します。
float3 ToyDiorama_ApplyBlackLift(float3 color, ToyDiorama_ColorGradeMasks masks)
{
    float lift = saturate(_ToyDioramaBlackLift) * masks.shadow;
    return color + _ToyDioramaShadowTint.rgb * lift * 0.35;
}

// シャドウ/中間/ハイライトの3帯域に、それぞれの強度でティントを重ねます。
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

// ブルーム前経路と統合経路の両方で使うベースグレーディング段です。
float3 ToyDiorama_ApplyBaseColorGrade(float3 sourceColor, out ToyDiorama_ColorGradeMasks masks)
{
    float3 color = ToyDiorama_PrepareColorGradeBase(sourceColor);
    masks = ToyDiorama_CalculateColorGradeMasks(color);

    color = ToyDiorama_ApplyBlackLift(color, masks);
    color = ToyDiorama_ApplyColorGradeTints(color, masks);
    color = ToyDiorama_SoftClampWhite(color, masks.highlight * _ToyDioramaWhiteSoftClamp);

    return saturate(color);
}

// ブルーム前の処理順: 基本グレード -> パステル圧縮 -> クリームハイライト -> 距離ヘイズ。
// エッジ着色と粒状ノイズは最終合成パス側で適用します。
ToyDiorama_ColorPipelineData ToyDiorama_EvaluatePreBloomPipeline(float3 sourceColor, float2 uv)
{
    ToyDiorama_ColorPipelineData data;

    if (_ToyDioramaEnabled < 0.5)
    {
        // エフェクト無効時でもデバッグ表示が壊れないよう全フィールドを埋めます。
        data.beforePastel = sourceColor;
        data.afterPastel = sourceColor;
        data.beforeBloom = sourceColor;
        data.afterColorGrade = sourceColor;
        data.colorGradeMasks = ToyDiorama_CalculateColorGradeMasks(saturate(sourceColor));
        data.pastelData.color = sourceColor;
        data.pastelData.highSaturationMask = 0.0;
        data.pastelData.pastelMask = 0.0;
        data.creamHighlightData.color = sourceColor;
        data.creamHighlightData.mask = 0.0;
        data.depthHazeData = ToyDiorama_CreateDepthHazeNoOp(sourceColor);
        data.edgeToneData = ToyDiorama_CreateEdgeToneNoOp(sourceColor);
        data.grainData = ToyDiorama_CreateGrainNoOp(sourceColor);
        return data;
    }

    data.beforePastel = ToyDiorama_ApplyBaseColorGrade(sourceColor, data.colorGradeMasks);
    data.pastelData = ToyDiorama_ApplyPastelCompression(data.beforePastel);
    data.afterPastel = data.pastelData.color;
    data.creamHighlightData = ToyDiorama_ApplyCreamHighlight(data.afterPastel);
    data.depthHazeData = ToyDiorama_ApplyDepthHaze(data.creamHighlightData.color, uv);
    data.beforeBloom = data.depthHazeData.afterDepthHaze;
    data.edgeToneData = ToyDiorama_CreateEdgeToneNoOp(data.beforeBloom);
    data.grainData = ToyDiorama_CreateGrainNoOp(data.beforeBloom);
    data.afterColorGrade = data.beforeBloom;

    return data;
}

// 最終段: ブルーム合成後にエッジ着色と粒状ノイズを適用します。
ToyDiorama_FinalColorPipelineData ToyDiorama_EvaluateFinalColorPipeline(float3 preBloomColor, float4 bloomComposite, float2 uv)
{
    ToyDiorama_FinalColorPipelineData data;
    data.beforeBloom = preBloomColor;
    data.bloomComposite = bloomComposite;
    data.afterBloom = saturate(preBloomColor + bloomComposite.rgb);
    data.edgeToneData = ToyDiorama_ApplyEdgeTone(data.afterBloom, uv);
    data.grainData = ToyDiorama_ApplyGrain(data.edgeToneData.afterEdgeTone, uv);
    data.afterColorGrade = data.grainData.afterGrain;
    return data;
}

// ブルーム分離前提でない旧呼び出し/デバッグ向けの互換経路です。
ToyDiorama_ColorPipelineData ToyDiorama_EvaluateColorPipeline(float3 sourceColor, float2 uv)
{
    ToyDiorama_ColorPipelineData data = ToyDiorama_EvaluatePreBloomPipeline(sourceColor, uv);
    ToyDiorama_FinalColorPipelineData finalData = ToyDiorama_EvaluateFinalColorPipeline(data.beforeBloom, float4(0.0, 0.0, 0.0, 0.0), uv);

    data.edgeToneData = finalData.edgeToneData;
    data.grainData = finalData.grainData;
    data.afterColorGrade = finalData.afterColorGrade;

    return data;
}

// 最終色のみを返す公開エントリ関数です。
float3 ToyDiorama_ApplyColorGrade(float3 sourceColor, float2 uv)
{
    return ToyDiorama_EvaluateColorPipeline(sourceColor, uv).afterColorGrade;
}

#endif