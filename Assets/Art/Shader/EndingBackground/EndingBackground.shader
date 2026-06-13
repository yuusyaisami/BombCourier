// BC/EndingBackground
// URP unlit 背景 shader。UI Image の Material としても使えるよう、UI の描画設定と Mask/RectMask2D に対応する。
// 2 枚のノイズを独立スクロールし、合成結果を 3x3 の近似ガウスでぼかしてから 2 色グラデーション化する。
// さらに DirectionalWipe（方向ワイプ）で、指定角度の方向側から進行度に応じて alpha を fade out できる。
// LowColor / HighColor はそれぞれ、指定方向に沿って Start→End の色グラデーションにできる（_GradientStrength で有効化）。
Shader "BC/EndingBackground"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        _NoiseTexA ("Noise Texture A", 2D) = "white" {}
        _NoiseTexB ("Noise Texture B", 2D) = "white" {}

        _NoiseScaleA ("Noise Scale A", Float) = 1
        _NoiseScaleB ("Noise Scale B", Float) = 1
        _NoiseScrollA ("Noise Scroll A", Vector) = (0.02, 0.01, 0, 0)
        _NoiseScrollB ("Noise Scroll B", Vector) = (-0.01, 0.015, 0, 0)

        _NoiseBlendWeight ("Noise Blend Weight", Range(0, 1)) = 0.5
        _NoiseMultiplyStrength ("Noise Multiply Strength", Range(0, 2)) = 0.25
        _BlurRadius ("Blur Radius", Range(0, 0.1)) = 0.02

        _ValueMin ("Value Min", Range(0, 1)) = 0.2
        _ValueMax ("Value Max", Range(0, 1)) = 0.8
        _ValuePower ("Value Power", Range(0.1, 4)) = 1

        _LowColor ("Low Color (Gradient Start)", Color) = (0.2, 0.25, 0.32, 1)
        _HighColor ("High Color (Gradient Start)", Color) = (0.92, 0.9, 0.82, 1)

        // ---- Color Gradient (LowColor/HighColor をそれぞれ指定方向に沿って Start→End でグラデーション。_GradientStrength=0 で従来通り) ----
        _LowColorEnd ("Low Color End", Color) = (0.2, 0.25, 0.32, 1)
        _HighColorEnd ("High Color End", Color) = (0.92, 0.9, 0.82, 1)
        _GradientAngle ("Gradient Angle (deg)", Range(0, 360)) = 0
        _GradientStrength ("Gradient Strength", Range(0, 1)) = 0

        // ---- Directional Wipe (方向ワイプ: 進行度で、指定角度の方向側から alpha を fade out) ----
        _WipeProgress ("Wipe Progress", Range(0, 1)) = 0
        _WipeAngle ("Wipe Angle (deg)", Range(0, 360)) = 0
        _WipeSoftness ("Wipe Softness", Range(0, 1)) = 0.1

        // ---- Moon / Sun Light (UV 上の一点を光源に: 近傍の雲を照らし(乗算)遠方を黒へ落とす + 月本体の輝点(加算)) ----
        _SunCenter ("Light Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        [HDR] _SunColor ("Light Color", Color) = (1, 0.95, 0.8, 1)
        // 照明(乗算): 光源周辺だけ雲を残し、遠方を黒へ落とす。0=照明オフ(従来通り)、1=フル。
        _SunIntensity ("Illumination Amount", Range(0, 1)) = 0
        _SunRadius ("Illumination Radius (UV)", Range(0.01, 2)) = 0.6
        _SunFalloff ("Illumination Falloff", Range(0.1, 8)) = 2
        // コア(加算): 月本体の小さく強い輝点。
        _SunCoreIntensity ("Core Intensity", Range(0, 8)) = 0
        _SunCoreRadius ("Core Radius (UV)", Range(0.001, 0.5)) = 0.04
        _SunCoreFalloff ("Core Falloff", Range(0.1, 8)) = 3
        _SunAspect ("Light Aspect (x,y)", Vector) = (1, 1, 0, 0)

        // ---- Stencil (UI Mask 対応) ----
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "UniversalMaterialType" = "Unlit"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _ClipRect;
                float4 _NoiseTexA_ST;
                float4 _NoiseTexB_ST;
                float4 _NoiseScrollA;
                float4 _NoiseScrollB;
                float4 _LowColor;
                float4 _HighColor;
                float4 _LowColorEnd;
                float4 _HighColorEnd;
                float4 _SunCenter;
                float4 _SunColor;
                float4 _SunAspect;
                float _NoiseScaleA;
                float _NoiseScaleB;
                float _NoiseBlendWeight;
                float _NoiseMultiplyStrength;
                float _BlurRadius;
                float _ValueMin;
                float _ValueMax;
                float _ValuePower;
                float _WipeProgress;
                float _WipeAngle;
                float _WipeSoftness;
                float _GradientAngle;
                float _GradientStrength;
                float _SunIntensity;
                float _SunRadius;
                float _SunFalloff;
                float _SunCoreIntensity;
                float _SunCoreRadius;
                float _SunCoreFalloff;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTexA);
            SAMPLER(sampler_NoiseTexA);
            TEXTURE2D(_NoiseTexB);
            SAMPLER(sampler_NoiseTexB);

            float Get2DClipping(float2 position, float4 clipRect)
            {
                float2 inside = step(clipRect.xy, position) * step(position, clipRect.zw);
                return inside.x * inside.y;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.worldPosition = input.positionOS;
                output.color = input.color * _Color;
                output.uv = input.uv;
                return output;
            }

            float2 RepeatUv(float2 uv)
            {
                return frac(uv);
            }

            float SampleNoiseA(float2 uv)
            {
                float2 noiseUv = TRANSFORM_TEX(uv, _NoiseTexA);
                noiseUv = RepeatUv(noiseUv * _NoiseScaleA + _Time.y * _NoiseScrollA.xy);
                return SAMPLE_TEXTURE2D(_NoiseTexA, sampler_NoiseTexA, noiseUv).r;
            }

            float SampleNoiseB(float2 uv)
            {
                float2 noiseUv = TRANSFORM_TEX(uv, _NoiseTexB);
                noiseUv = RepeatUv(noiseUv * _NoiseScaleB + _Time.y * _NoiseScrollB.xy);
                return SAMPLE_TEXTURE2D(_NoiseTexB, sampler_NoiseTexB, noiseUv).r;
            }

            float SampleCombinedNoise(float2 uv)
            {
                float noiseA = SampleNoiseA(uv);
                float noiseB = SampleNoiseB(uv);
                float blended = lerp(noiseA, noiseB, saturate(_NoiseBlendWeight));
                return saturate(blended + (noiseA * noiseB) * _NoiseMultiplyStrength);
            }

            float BlurCombinedNoise(float2 uv)
            {
                float2 stepUv = float2(_BlurRadius, _BlurRadius);

                float value = 0.0;
                value += SampleCombinedNoise(uv + stepUv * float2(-1.0, -1.0)) * 1.0;
                value += SampleCombinedNoise(uv + stepUv * float2(0.0, -1.0)) * 2.0;
                value += SampleCombinedNoise(uv + stepUv * float2(1.0, -1.0)) * 1.0;
                value += SampleCombinedNoise(uv + stepUv * float2(-1.0, 0.0)) * 2.0;
                value += SampleCombinedNoise(uv) * 4.0;
                value += SampleCombinedNoise(uv + stepUv * float2(1.0, 0.0)) * 2.0;
                value += SampleCombinedNoise(uv + stepUv * float2(-1.0, 1.0)) * 1.0;
                value += SampleCombinedNoise(uv + stepUv * float2(0.0, 1.0)) * 2.0;
                value += SampleCombinedNoise(uv + stepUv * float2(1.0, 1.0)) * 1.0;
                return value / 16.0;
            }

            // 角度(度)で指定した方向に UV を射影し、方向軸上の位置を [0,1] に正規化して返す共有ヘルパ。
            // 0 = 方向の手前側、1 = 方向の指す側。単位正方形を方向に射影した全幅で正規化する。
            // DirectionalWipe（alpha）と Color Gradient（色）の両方がこの座標を使う。
            float DirectionalAxisCoord(float2 uv, float angleDeg)
            {
                float angleRad = radians(angleDeg);
                float2 dir = float2(cos(angleRad), sin(angleRad));
                float2 centered = uv - 0.5;
                float projection = dot(centered, dir);
                float halfExtent = 0.5 * (abs(dir.x) + abs(dir.y));
                return projection / max(2.0 * halfExtent, 1e-4) + 0.5;
            }

            // DirectionalWipe（方向ワイプ）の alpha 係数を返す。
            // _WipeAngle (度) で指定した方向側から、_WipeProgress (0..1) に応じて順に透明化する fade out。
            // 戻り値: 1 = 表示(不透明側)、0 = 透明側。
            // 不変条件: _WipeProgress=0 で全面 1（完全な no-op）、=1 で全面 0（完全透明）。
            //          soft band を [0,1] の外側まで掃かせる boundary マッピングにより、両端を厳密化している。
            float DirectionalWipeAlpha(float2 uv)
            {
                float t = DirectionalAxisCoord(uv, _WipeAngle);

                // soft band の半幅。0 だと smoothstep の edge が一致して未定義になるため下限を設ける。
                float soft = max(_WipeSoftness, 1e-4);

                // progress=0 → boundary=1+soft（全面表示）、progress=1 → boundary=-soft（全面透明）。
                // soft band が [0,1] の外まで完全に掃けるので、両端は厳密に no-op / 全透明になる。
                float boundary = lerp(1.0 + soft, -soft, saturate(_WipeProgress));

                // t が boundary より方向側(大きい)なら透明、手前なら表示。1-wipe を alpha 係数として返す。
                float wipe = smoothstep(boundary - soft, boundary + soft, t);
                return 1.0 - wipe;
            }

            // 光源(_SunCenter)からの放射状の減衰を返す共通ヘルパ。1 = 中心、0 = radius 外。
            // 照明(乗算)とコア(加算)の両方が、radius/falloff を変えてこれを使う。
            // _SunAspect で UV 円の縦横比を補正できる（既定 (1,1) は補正なし。UI 矩形が非正方形だと
            // UV 円は楕円に見えるため、必要なら aspect で縦横を合わせる）。
            float SunRadialGlow(float2 uv, float radius, float falloff)
            {
                float2 delta = (uv - _SunCenter.xy) * _SunAspect.xy;
                float dist = length(delta);
                float glow = saturate(1.0 - dist / max(radius, 1e-4));
                return pow(glow, max(falloff, 1e-4));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float blurredValue = BlurCombinedNoise(input.uv);

                float lower = min(_ValueMin, _ValueMax);
                float upper = max(_ValueMin, _ValueMax);
                upper = max(upper, lower + 0.0001);

                float normalized = smoothstep(lower, upper, saturate(blurredValue));
                normalized = pow(saturate(normalized), max(0.0001, _ValuePower));

                // LowColor / HighColor をそれぞれ _GradientAngle 方向に沿って Start→End へグラデーションする。
                // _GradientStrength=0 のとき係数 0 で、従来どおり _LowColor / _HighColor のまま（後方互換）。
                // Low と High は別々の Start/End 色対を持つので、独立したグラデーションになる。
                float gradientT = saturate(DirectionalAxisCoord(input.uv, _GradientAngle) * _GradientStrength);
                half4 lowColor = lerp(_LowColor, _LowColorEnd, gradientT);
                half4 highColor = lerp(_HighColor, _HighColorEnd, gradientT);

                half4 color = lerp(lowColor, highColor, normalized);

                // 月/太陽ライト。夜空に月が雲を照らす絵を作る:
                // (1) 照明(乗算): 光源近傍だけ雲を残し、遠方を黒へ落とす。これが無いと全面が明るく飛ぶ
                //     (加算のみだと「近くは雲、遠くは黒」が両立しない)。_SunIntensity=0 で乗算係数 1=従来どおり。
                float illum = SunRadialGlow(input.uv, _SunRadius, _SunFalloff);
                color.rgb *= lerp(1.0, illum, _SunIntensity);

                // (2) コア(加算): 月本体の小さく強い輝点。"ライトなので重なる点は加算" はここが担う。
                //     _SunCoreIntensity=0 で無加算（後方互換）。
                float core = SunRadialGlow(input.uv, _SunCoreRadius, _SunCoreFalloff);
                color.rgb += _SunColor.rgb * (_SunCoreIntensity * core);

                color.a *= SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                color *= input.color;

                // 方向ワイプ: 指定角度の方向側から進行度に応じて alpha を fade out する。
                color.a *= DirectionalWipeAlpha(input.uv);

#ifdef UNITY_UI_CLIP_RECT
                color.a *= Get2DClipping(input.worldPosition.xy, _ClipRect);
#endif
#ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
#endif
                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
