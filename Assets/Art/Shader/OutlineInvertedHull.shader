Shader "BC/PickupOutlineScreenSpace"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 1, 0, 1)
        _OutlineWidth ("Outline Width Pixels", Range(1, 8)) = 4
        _OutlineSoftness ("Outline Softness", Range(0, 4)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Name "PickupOutlineMask"

            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend Off

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineSoftness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(1.0h, 1.0h, 1.0h, 1.0h);
            }

            ENDHLSL
        }

        Pass
        {
            Name "PickupOutlineComposite"

            Cull Off
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #define PICKUP_OUTLINE_MAX_RADIUS 8

            TEXTURE2D_X(_PickupOutlineMaskTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineSoftness;
            CBUFFER_END

            float4 _PickupOutlineMaskTexelSize;

            float SampleMask(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_PickupOutlineMaskTex, sampler_LinearClamp, uv).r;
            }

            float EvaluateOutlineAlpha(float2 uv)
            {
                float centerMask = SampleMask(uv);

                // maskの外側だけに色を乗せることで、複雑形状でも塗りつぶしではなく輪郭として見せる。
                float outsideMask = 1.0 - smoothstep(0.35, 0.65, centerMask);

                if (outsideMask <= 0.0)
                {
                    return 0.0;
                }

                float outlineWidth = clamp(_OutlineWidth, 0.5, PICKUP_OUTLINE_MAX_RADIUS);
                float outlineSoftness = max(_OutlineSoftness, 0.001);
                float searchRadius = min((float)PICKUP_OUTLINE_MAX_RADIUS, ceil(outlineWidth + outlineSoftness));
                float nearestDistance = searchRadius + 1.0;

                [loop]
                for (int y = -PICKUP_OUTLINE_MAX_RADIUS; y <= PICKUP_OUTLINE_MAX_RADIUS; y++)
                {
                    [loop]
                    for (int x = -PICKUP_OUTLINE_MAX_RADIUS; x <= PICKUP_OUTLINE_MAX_RADIUS; x++)
                    {
                        float2 offsetPixels = float2(x, y);
                        float distancePixels = length(offsetPixels);

                        if (distancePixels <= 0.001 || distancePixels > searchRadius)
                        {
                            continue;
                        }

                        float2 sampleUv = uv + offsetPixels * _PickupOutlineMaskTexelSize.xy;
                        float sampleMask = SampleMask(sampleUv);

                        if (sampleMask > 0.001)
                        {
                            nearestDistance = min(nearestDistance, distancePixels);
                        }
                    }
                }

                float edgeAlpha = 1.0 - smoothstep(outlineWidth, outlineWidth + outlineSoftness, nearestDistance);
                return saturate(edgeAlpha * outsideMask);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float alpha = EvaluateOutlineAlpha(input.texcoord);
                return half4(_OutlineColor.rgb, _OutlineColor.a * alpha);
            }

            ENDHLSL
        }
    }

    FallBack Off
}