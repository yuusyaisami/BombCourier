Shader "Hidden/BC/Transition/ScreenTransition"
{
    Properties
    {
        _TextureFrom ("From Texture", 2D) = "black" {}
        _TextureTo ("To Texture", 2D) = "black" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _Progress ("Progress", Range(0, 1)) = 0
        _Mode ("Mode", Int) = 0
        _UseExplicitTo ("Use Explicit To", Float) = 0
        _FromUvScaleOffset ("From UV Scale Offset", Vector) = (1, 1, 0, 0)
        _ToUvScaleOffset ("To UV Scale Offset", Vector) = (1, 1, 0, 0)
        _Feather ("Feather", Range(0.0001, 0.5)) = 0.04
        _NoiseScale ("Noise Scale", Float) = 8
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 1
        _Seed ("Seed", Float) = 0
        _Direction ("Direction", Vector) = (1, 0, 0, 0)
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
        _Aspect ("Aspect", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "CaptureFrom"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragCapture

            #include "Assets/Art/Shader/Transition/HLSL/BC_ScreenTransitionCommon.hlsl"

            half4 FragCapture(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 toColor = SampleTo(uv);
                return half4(toColor.rgb, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Composite"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragComposite

            #include "Assets/Art/Shader/Transition/HLSL/BC_ScreenTransitionCommon.hlsl"
            #include "Assets/Art/Shader/Transition/HLSL/BC_ScreenTransitionModes.hlsl"

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            half4 FragComposite(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
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

                half3 finalRgb = lerp(fromColor.rgb, toColor.rgb, saturate(toWeight));
                return half4(finalRgb, 1.0h);
            }
            ENDHLSL
        }
    }
}
