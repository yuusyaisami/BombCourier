Shader "TextMeshPro/UI Bitmap"
{
    Properties
    {
        [MainTexture] _MainTex ("Font Atlas", 2D) = "white" {}
        _FaceTex ("Font Texture", 2D) = "white" {}
        [MainColor] _FaceColor ("Text Color", Color) = (1, 1, 1, 1)
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth ("Outline Width", Range(0, 1)) = 0
        [Enum(In, 0, Out, 1, Both, 2)] _OutlineMode ("Outline Mode", Float) = 1

        _VertexOffsetX ("Vertex OffsetX", float) = 0
        _VertexOffsetY ("Vertex OffsetY", float) = 0
        _MaskSoftnessX ("Mask SoftnessX", float) = 0
        _MaskSoftnessY ("Mask SoftnessY", float) = 0

        _ClipRect ("Clip Rect", vector) = (-32767, -32767, 32767, 32767)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _CullMode ("Cull Mode", Float) = 0
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }

        Stencil
        {
            Ref[_Stencil]
            Comp[_StencilComp]
            Pass[_StencilOp]
            ReadMask[_StencilReadMask]
            WriteMask[_StencilWriteMask]
        }

        Lighting Off
        Cull [_CullMode]
        ZTest [unity_GUIZTestMode]
        ZWrite Off
        Fog { Mode Off }
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float4 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float4 mask : TEXCOORD2;
            };

            uniform sampler2D _MainTex;
            uniform float4 _MainTex_TexelSize;
            uniform sampler2D _FaceTex;
            uniform float4 _FaceTex_ST;
            uniform fixed4 _FaceColor;

            uniform fixed4 _OutlineColor;
            uniform float _OutlineWidth;
            uniform float _OutlineMode;

            uniform float _VertexOffsetX;
            uniform float _VertexOffsetY;
            uniform float4 _ClipRect;
            uniform float _MaskSoftnessX;
            uniform float _MaskSoftnessY;
            uniform float _UIMaskSoftnessX;
            uniform float _UIMaskSoftnessY;
            uniform int _UIVertexColorAlwaysGammaSpace;

            v2f vert (appdata_t v)
            {
                float4 vert = v.vertex;
                vert.x += _VertexOffsetX;
                vert.y += _VertexOffsetY;

                vert.xy += (vert.w * 0.5) / _ScreenParams.xy;

                float4 vPosition = UnityPixelSnap(UnityObjectToClipPos(vert));

                if (_UIVertexColorAlwaysGammaSpace && !IsGammaSpace())
                {
                    v.color.rgb = UIGammaToLinear(v.color.rgb);
                }
                fixed4 faceColor = v.color;
                faceColor *= _FaceColor;

                v2f OUT;
                OUT.vertex = vPosition;
                OUT.color = faceColor;
                OUT.texcoord0 = v.texcoord0;
                OUT.texcoord1 = TRANSFORM_TEX(v.texcoord1, _FaceTex);
                float2 pixelSize = vPosition.w;
                pixelSize /= abs(float2(_ScreenParams.x * UNITY_MATRIX_P[0][0], _ScreenParams.y * UNITY_MATRIX_P[1][1]));

                // Clamp _ClipRect to 16bit.
                const float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                const half2 maskSoftness = half2(max(_UIMaskSoftnessX, _MaskSoftnessX), max(_UIMaskSoftnessY, _MaskSoftnessY));
                OUT.mask = float4(vert.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * maskSoftness + pixelSize.xy));

                return OUT;
            }

            void SampleOutlineNeighborhood(float2 uv, half centerAlpha, out half dilatedAlpha, out half erodedAlpha)
            {
                float2 offset = _MainTex_TexelSize.xy * (_OutlineWidth * 4.0);
                dilatedAlpha = centerAlpha;
                erodedAlpha = centerAlpha;

                half sampleAlpha = tex2D(_MainTex, uv + float2( offset.x, 0.0)).a;
                dilatedAlpha = max(dilatedAlpha, sampleAlpha);
                erodedAlpha = min(erodedAlpha, sampleAlpha);

                sampleAlpha = tex2D(_MainTex, uv + float2(-offset.x, 0.0)).a;
                dilatedAlpha = max(dilatedAlpha, sampleAlpha);
                erodedAlpha = min(erodedAlpha, sampleAlpha);

                sampleAlpha = tex2D(_MainTex, uv + float2(0.0,  offset.y)).a;
                dilatedAlpha = max(dilatedAlpha, sampleAlpha);
                erodedAlpha = min(erodedAlpha, sampleAlpha);

                sampleAlpha = tex2D(_MainTex, uv + float2(0.0, -offset.y)).a;
                dilatedAlpha = max(dilatedAlpha, sampleAlpha);
                erodedAlpha = min(erodedAlpha, sampleAlpha);

                sampleAlpha = tex2D(_MainTex, uv + float2( offset.x,  offset.y)).a;
                dilatedAlpha = max(dilatedAlpha, sampleAlpha);
                erodedAlpha = min(erodedAlpha, sampleAlpha);

                sampleAlpha = tex2D(_MainTex, uv + float2( offset.x, -offset.y)).a;
                dilatedAlpha = max(dilatedAlpha, sampleAlpha);
                erodedAlpha = min(erodedAlpha, sampleAlpha);

                sampleAlpha = tex2D(_MainTex, uv + float2(-offset.x,  offset.y)).a;
                dilatedAlpha = max(dilatedAlpha, sampleAlpha);
                erodedAlpha = min(erodedAlpha, sampleAlpha);

                sampleAlpha = tex2D(_MainTex, uv + float2(-offset.x, -offset.y)).a;
                dilatedAlpha = max(dilatedAlpha, sampleAlpha);
                erodedAlpha = min(erodedAlpha, sampleAlpha);
            }

            fixed4 frag (v2f IN) : SV_Target
            {
                half faceCoverage = tex2D(_MainTex, IN.texcoord0).a;
                half dilatedCoverage;
                half erodedCoverage;
                SampleOutlineNeighborhood(IN.texcoord0, faceCoverage, dilatedCoverage, erodedCoverage);

                fixed4 faceTex = tex2D(_FaceTex, IN.texcoord1);

                half faceMask = faceCoverage;
                half outlineMask = 0.0;

                // In = inner edge, Out = outer edge, Both = centered stroke.
                if (_OutlineMode < 0.5)
                {
                    faceMask = erodedCoverage;
                    outlineMask = saturate(faceCoverage - erodedCoverage);
                }
                else if (_OutlineMode < 1.5)
                {
                    faceMask = faceCoverage;
                    outlineMask = saturate(dilatedCoverage - faceCoverage);
                }
                else
                {
                    faceMask = erodedCoverage;
                    outlineMask = saturate(dilatedCoverage - erodedCoverage);
                }

                fixed3 faceRgb = faceTex.rgb * IN.color.rgb;
                fixed3 outlineRgb = _OutlineColor.rgb;
                half faceAlpha = IN.color.a * faceMask;
                half outlineAlpha = IN.color.a * _OutlineColor.a * outlineMask;
                fixed4 color = fixed4(lerp(outlineRgb, faceRgb, saturate(faceMask)), max(faceAlpha, outlineAlpha));

                // Alternative implementation to UnityGet2DClipping with support for softness.
                #if UNITY_UI_CLIP_RECT
                    half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                    color *= m.x * m.y;
                #endif

                #if UNITY_UI_ALPHACLIP
                    clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
