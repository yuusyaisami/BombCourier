// BC/UI/AnimatedUI
// A general-purpose URP UI shader that replicates Unity's default UI/Default behaviour
// while adding an optional striped-overlay animation feature.
// The stripe logic lives in UIAnimated.hlsl.
Shader "BC/UI/AnimatedUI"
{
    Properties
    {
        // ---- Standard UI properties ----
        [PerRendererData] _MainTex      ("Sprite Texture", 2D)    = "white" {}
        _Color                          ("Tint",           Color)  = (1,1,1,1)

        // Stencil (required for UI Mask components)
        _StencilComp     ("Stencil Comparison", Float) = 8
        _Stencil         ("Stencil ID",         Float) = 0
        _StencilOp       ("Stencil Operation",  Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask",  Float) = 255
        _ColorMask       ("Color Mask",         Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        // ---- Striped Overlay feature ----
        [Toggle] _StripedOverlayEnabled  ("Striped Overlay Enabled",  Float)        = 0
        _StripedOverlayHue               ("Overlay Hue",              Range(0, 1))  = 0
        _StripedOverlaySaturation        ("Overlay Saturation",       Range(0, 1))  = 1
        _StripedOverlayValue             ("Overlay Value",            Range(0, 1))  = 1
        _StripedOverlayAlpha             ("Overlay Blend Alpha",      Range(0, 1))  = 0.5
        _StripedOverlayAngle             ("Stripe Angle (degrees)",   Range(0, 360))= 0
        _StripedOverlayFrequency         ("Stripe Frequency",         Range(1, 50)) = 10
        _StripedOverlayThickness         ("Stripe Thickness",         Range(0, 1))  = 0.5
        _StripedOverlaySpeed             ("Animation Speed",          Range(0, 20)) = 2
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
            #pragma vertex   UIAnimatedVert
            #pragma fragment UIAnimatedFrag
            #pragma target 2.0

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "UIAnimated.hlsl"

            // ---- Vertex input / output ----
            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS    : SV_POSITION;
                float4 color         : COLOR;
                float2 uv            : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;   // xy used by UI clip rect
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ---- Uniforms ----
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _MainTex_ST;
                float4 _ClipRect;

                float  _StripedOverlayEnabled;
                float  _StripedOverlayHue;
                float  _StripedOverlaySaturation;
                float  _StripedOverlayValue;
                float  _StripedOverlayAlpha;
                float  _StripedOverlayAngle;
                float  _StripedOverlayFrequency;
                float  _StripedOverlayThickness;
                float  _StripedOverlaySpeed;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // ---- Vertex shader ----
            Varyings UIAnimatedVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.worldPosition = input.positionOS;
                output.positionCS    = TransformObjectToHClip(input.positionOS.xyz);
                output.uv            = TRANSFORM_TEX(input.uv, _MainTex);
                output.color         = input.color * _Color;
                return output;
            }

            // ---- Fragment shader ----
            half4 UIAnimatedFrag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;

                // Striped overlay (enabled via material property at runtime)
                UNITY_BRANCH
                if (_StripedOverlayEnabled > 0.5)
                {
                    color = (half4)BC_ApplyStripedOverlay(
                        (float4)color,
                        input.uv,
                        _StripedOverlayAngle,
                        _StripedOverlayFrequency,
                        _StripedOverlayThickness,
                        _StripedOverlaySpeed,
                        _Time.y,
                        _StripedOverlayHue,
                        _StripedOverlaySaturation,
                        _StripedOverlayValue,
                        _StripedOverlayAlpha
                    );
                }

#ifdef UNITY_UI_CLIP_RECT
                color.a *= BC_Get2DClipping(input.worldPosition.xy, _ClipRect);
#endif

#ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
#endif

                return color;
            }
            ENDHLSL
        }
    }
}
