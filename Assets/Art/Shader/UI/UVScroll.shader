// BC/UI/UVScroll
// UI 用の UV スクロール shader。_MainTex を _ScrollSpeed (UV/sec) で時間スクロールし、_Tiling でタイル数を指定する。
// シームレスにループさせるには、テクスチャの Wrap Mode を Repeat に設定すること
// （Sprite atlas に入れると端で隣の絵を拾うため、スクロールには非アトラスの Repeat テクスチャを使う）。
// UI Image の Material として使えるよう、Mask/RectMask2D(Stencil/ClipRect) と AlphaClip に対応する。
Shader "BC/UI/UVScroll"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color       ("Tint", Color) = (1, 1, 1, 1)

        // ---- Scroll ----
        _ScrollSpeed ("Scroll Speed (UV/sec)", Vector) = (0.1, 0, 0, 0)
        _Tiling      ("Tiling",                 Vector) = (1, 1, 0, 0)

        // ---- Stencil (UI Mask 対応) ----
        _StencilComp      ("Stencil Comparison", Float) = 8
        _Stencil          ("Stencil ID",         Float) = 0
        _StencilOp        ("Stencil Operation",  Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask  ("Stencil Read Mask",  Float) = 255
        _ColorMask        ("Color Mask",         Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"    = "UniversalPipeline"
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
            "UniversalMaterialType" = "Unlit"
        }

        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull     Off
        Lighting Off
        ZWrite   Off
        ZTest    [unity_GUIZTestMode]
        Blend    SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target 2.0

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : COLOR;
                float2 uv          : TEXCOORD0;
                float4 worldPos    : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float4    _Color;
            float4    _ScrollSpeed;
            float4    _Tiling;
            float4    _ClipRect;

            // RectMask2D 用の矩形クリップ。clipRect 内なら 1、外なら 0 を返す。
            // EndingBackground と同じく外部依存を避けるためローカル定義する。
            float Get2DClipping(float2 position, float4 clipRect)
            {
                float2 inside = step(clipRect.xy, position) * step(position, clipRect.zw);
                return inside.x * inside.y;
            }

            Varyings vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.worldPos    = IN.positionOS;
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // タイル数を掛け、時間で UV をスクロールする。シームレスなループには
                // テクスチャの Wrap Mode = Repeat が必要（sampler の wrap がタイリングを担う）。
                // _Time.y は秒なので、_ScrollSpeed は「UV/秒」になる。
                float2 uv = IN.uv * _Tiling.xy + _Time.y * _ScrollSpeed.xy;

                half4 col = tex2D(_MainTex, uv) * IN.color;

#ifdef UNITY_UI_CLIP_RECT
                col.a *= Get2DClipping(IN.worldPos.xy, _ClipRect);
#endif
#ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
#endif
                return col;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
