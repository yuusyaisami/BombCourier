#ifndef BC_UI_ANIMATED_INCLUDED
#define BC_UI_ANIMATED_INCLUDED

// ============================================================
// HSV Utilities
// ============================================================

// RGB to HSV conversion
float3 BC_RgbToHsv(float3 rgb)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(rgb.bg, K.wz), float4(rgb.gb, K.xy), step(rgb.b, rgb.g));
    float4 q = lerp(float4(p.xyw, rgb.r), float4(rgb.r, p.yzx), step(p.x, rgb.r));
    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

// HSV to RGB conversion
float3 BC_HsvToRgb(float3 hsv)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(hsv.xxx + K.xyz) * 6.0 - K.www);
    return hsv.z * lerp(K.xxx, saturate(p - K.xxx), hsv.y);
}

// ============================================================
// Striped Overlay
// ============================================================

// Returns 1 if the UV falls inside a stripe, 0 otherwise.
// angle     : rotation angle in degrees
// frequency : number of stripes per UV unit
// offset    : animation phase offset (driven by sin)
// thickness : stripe fill ratio [0, 1]
float BC_CalculateStripeMask(float2 uv, float angle, float frequency, float offset, float thickness)
{
    float rad = radians(angle);
    float2 dir = float2(cos(rad), sin(rad));

    // Project UV onto stripe direction, centred at (0.5, 0.5)
    float proj = dot(uv - float2(0.5, 0.5), dir) * frequency + offset;
    float stripe = frac(proj);
    return step(1.0 - thickness, stripe);
}

// Apply the striped overlay onto baseColor using HSV compositing.
//
// baseColor        : input RGBA color from the sprite/texture
// uv               : texture-space UV coordinates
// angle            : stripe rotation angle in degrees
// frequency        : stripe density (stripes per UV unit)
// thickness        : stripe fill ratio [0, 1]
// speed            : sin-wave animation speed (radians/s)
// time             : current time (_Time.y)
// overlayH/S/V     : overlay colour in HSV (each [0, 1])
// overlayAlpha     : lerp weight between base HSV and overlay HSV [0, 1]
float4 BC_ApplyStripedOverlay(
    float4 baseColor,
    float2 uv,
    float  angle,
    float  frequency,
    float  thickness,
    float  speed,
    float  time,
    float  overlayH,
    float  overlayS,
    float  overlayV,
    float  overlayAlpha)
{
    float animOffset = sin(time * speed) * 0.5;
    float mask = BC_CalculateStripeMask(uv, angle, frequency, animOffset, thickness);

    UNITY_BRANCH
    if (mask < 0.5)
        return baseColor;

    // Blend in HSV space
    float3 baseHsv    = BC_RgbToHsv(baseColor.rgb);
    float3 overlayHsv = float3(overlayH, overlayS, overlayV);
    float3 blendedHsv = lerp(baseHsv, overlayHsv, overlayAlpha);
    float3 blendedRgb = BC_HsvToRgb(blendedHsv);

    return float4(blendedRgb, baseColor.a);
}

// ============================================================
// UI Clipping Utility (replaces UnityGet2DClipping from CG)
// ============================================================
float BC_Get2DClipping(float2 position, float4 clipRect)
{
    float2 inside = step(clipRect.xy, position) * step(position, clipRect.zw);
    return inside.x * inside.y;
}

#endif // BC_UI_ANIMATED_INCLUDED
