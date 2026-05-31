Shader "Hidden/BC/UI/ScreenTransitionImage"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _TextureFrom ("From Texture", 2D) = "white" {}
        _TextureTo ("To Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _Progress ("Progress", Range(0, 1)) = 0
        _Mode ("Mode", Int) = 0
        _FromUvScaleOffset ("From UV Scale Offset", Vector) = (1, 1, 0, 0)
        _ToUvScaleOffset ("To UV Scale Offset", Vector) = (1, 1, 0, 0)
        _Feather ("Feather", Range(0.0001, 0.5)) = 0.04
        _NoiseScale ("Noise Scale", Float) = 8
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 1
        _Seed ("Seed", Float) = 0
        _Direction ("Direction", Vector) = (1, 0, 0, 0)
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
        _Aspect ("Aspect", Float) = 1
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "CanUseSpriteAtlas" = "True"
            "PreviewType" = "Plane"
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
            Name "Composite"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Art/Shader/Transition/HLSL/BC_ScreenTransitionModes.hlsl"

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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_TextureFrom);
            SAMPLER(sampler_TextureFrom);
            TEXTURE2D(_TextureTo);
            SAMPLER(sampler_TextureTo);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float _Progress;
                int _Mode;
                float4 _FromUvScaleOffset;
                float4 _ToUvScaleOffset;
                float _Feather;
                float _NoiseScale;
                float _NoiseStrength;
                float _Seed;
                float4 _Direction;
                float4 _Center;
                float _Aspect;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            float2 TransformUv(float2 uv, float4 scaleOffset)
            {
                return uv * scaleOffset.xy + scaleOffset.zw;
            }

            half4 SampleFrom(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_TextureFrom, sampler_TextureFrom, TransformUv(uv, _FromUvScaleOffset));
            }

            half4 SampleTo(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_TextureTo, sampler_TextureTo, TransformUv(uv, _ToUvScaleOffset));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float t = BC_EvaluateTransitionT(_Progress, _Mode);

                half4 fromColor = SampleFrom(uv);
                half4 toColor = SampleTo(uv);

                float toWeight = t;
                if (_Mode == 2)
                {
                    toWeight = BC_EvaluateDirectionalWipe(uv, t, _Direction.xy, _Feather);
                }
                else if (_Mode == 3)
                {
                    toWeight = BC_EvaluateRadialWipe(uv, t, _Center.xy, _Aspect, _Feather);
                }
                else if (_Mode == 4)
                {
                    float2 noiseUv = uv * max(0.01, _NoiseScale) + _Seed * float2(0.618, 0.382);
                    float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUv).r;
                    toWeight = BC_EvaluateBlueNoiseDissolve(uv, t, _Feather, noise, _NoiseStrength);
                }

                half blend = saturate(toWeight);
                half4 color = lerp(fromColor, toColor, blend);
                color *= input.color;
                return color;
            }
            ENDHLSL
        }
    }
}
