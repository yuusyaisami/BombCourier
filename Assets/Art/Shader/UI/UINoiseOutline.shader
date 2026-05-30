// BC/UI/NoiseOutline
// UI Image に適用するグラデーションアウトラインシェーダー。
// UINoiseOutlineMB が子 Image にこのシェーダーのマテリアルを設定して使用する。
// _Intensity を 0-1 で切り替えることでフォーカス状態を表現する。
Shader "BC/UI/NoiseOutline"
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

        // ---- Outline ----
        _OutlineColor     ("Outline Color",       Color)         = (1,0.8,0,1)
        _OutlineInnerColor ("Outline Inner Color", Color)        = (1,0.95,0.55,0.35)
        _OutlineWidth     ("Outline Width (0-1)", Range(0,0.15)) = 0.05
        _NoiseScale       ("Noise Scale",         Range(1,30))   = 8
        _NoiseSpeed       ("Noise Speed",         Range(0,5))    = 1.5
        _Intensity        ("Intensity (0=off, 1=full)", Range(0,1)) = 0
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

            float4 _Color;
            float4 _OutlineColor;
            float4 _OutlineInnerColor;
            float  _OutlineWidth;
            float  _NoiseScale;
            float  _NoiseSpeed;
            float  _Intensity;
            float4 _RectSize;

            float4 _ClipRect;

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
                half4 texColor = tex2D(_MainTex, IN.uv) * IN.color;

                // ボーダー領域: UV が端に近い矩形リング
                float2 uv = saturate(IN.uv);
                float width = max(_RectSize.x, 1.0);
                float height = max(_RectSize.y, 1.0);
                float aspect = width / height;

                float wX = aspect >= 1.0 ? (_OutlineWidth / aspect) : _OutlineWidth;
                float wY = aspect >= 1.0 ? _OutlineWidth : (_OutlineWidth * aspect);

                float edgeDistX = min(uv.x, 1.0 - uv.x);
                float edgeDistY = min(uv.y, 1.0 - uv.y);
                float normalizedEdgeDist = min(edgeDistX / max(wX, 0.0001), edgeDistY / max(wY, 0.0001));
                float inBorder = 1.0 - step(1.0, normalizedEdgeDist);
                float gradientT = saturate(normalizedEdgeDist);

                float outlineAlpha = inBorder * _Intensity;
                half4 outlineColor = lerp(_OutlineColor, _OutlineInnerColor, gradientT);
                outlineColor.a    *= outlineAlpha * texColor.a;

                // 中央は透明のまま、アウトライン部分だけ描画する。
                half4 col = outlineColor;

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
