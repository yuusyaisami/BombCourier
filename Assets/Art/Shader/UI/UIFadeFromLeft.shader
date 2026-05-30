// BC/UI/FadeFromLeft
// ステージセレクト背景用。左側を固定色、右側をテクスチャ画像にグラデーション合成するシェーダー。
// _FadeEdge (0-1) でグラデーション開始 X 位置を、_FadeSmooth で滑らかさを制御する。
Shader "BC/UI/FadeFromLeft"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color          ("Tint",         Color)          = (1,1,1,1)

        // ---- Stencil (UI Mask 対応) ----
        _StencilComp      ("Stencil Comparison", Float) = 8
        _Stencil          ("Stencil ID",         Float) = 0
        _StencilOp        ("Stencil Operation",  Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask  ("Stencil Read Mask",  Float) = 255
        _ColorMask        ("Color Mask",         Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        // ---- Fade ----
        _LeftColor  ("Left Solid Color",         Color)       = (0.08,0.08,0.08,1)
        _FadeEdge   ("Fade Edge (0=left, 1=right)", Range(0,1)) = 0.3
        _FadeSmooth ("Fade Smoothness",          Range(0.001,0.5)) = 0.15
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
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
        ColorMask[_ColorMask]

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
            float4    _LeftColor;
            float     _FadeEdge;
            float     _FadeSmooth;
            float4    _ClipRect;

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
                half4 texColor  = tex2D(_MainTex, IN.uv) * IN.color;
                // smoothstep で左固定色 → テクスチャ に滑らかにブレンド
                float blend = smoothstep(_FadeEdge - _FadeSmooth, _FadeEdge + _FadeSmooth, IN.uv.x);
                half4 col;
                col.rgb = lerp(_LeftColor.rgb, texColor.rgb, blend);
                col.a   = lerp(_LeftColor.a,   texColor.a,   blend);

#ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPos.xy, _ClipRect);
#endif
#ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
#endif
                return col;
            }
            ENDHLSL
        }
    }
}
