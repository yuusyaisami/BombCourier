Shader "BC/Hologram/HologramWireMap"
{
    Properties
    {
        // Basic colours and opacity
        _LineColor ("Line Color", Color) = (0, 1, 0.2, 1)
        _FillColor ("Fill Color", Color) = (0, 0.8, 0.4, 1)
        _FillAlpha ("Fill Alpha", Float) = 0.06
        _EmissionStrength ("Emission Strength", Float) = 2.0

        // Wireframe control
        _LineWidth ("Line Width", Float) = 1.2
        _LineFeather ("Line Feather", Float) = 1.0
        _LineAlpha ("Line Alpha", Float) = 1.0

        // Fresnel rim
        _FresnelPower ("Fresnel Power", Float) = 3.0
        _FresnelStrength ("Fresnel Strength", Float) = 0.6

        // Scan lines
        _ScanScale ("Scan Scale", Float) = 12.0
        _ScanSpeed ("Scan Speed", Float) = 1.0
        _ScanStrength ("Scan Strength", Float) = 0.35
        _ScanDirection ("Scan Direction", Vector) = (0, 1, 0, 0)

        // Radar pulse
        _PulseCenter ("Pulse Center", Vector) = (0, 0, 0, 0)
        _PulseSpeed ("Pulse Speed", Float) = 4.0
        _PulseWidth ("Pulse Width", Float) = 0.6
        _PulseStrength ("Pulse Strength", Float) = 0.8
        _PulseInterval ("Pulse Interval", Float) = 8.0

        // Reveal animation
        _Reveal ("Reveal", Float) = 1.0
        _RevealMinY ("Reveal Min Y", Float) = 0.0
        _RevealMaxY ("Reveal Max Y", Float) = 10.0
        _RevealSoftness ("Reveal Softness", Float) = 0.5
        _RevealGlowStrength ("Reveal Glow Strength", Float) = 2.0

        // Optional glitch noise
        _GlitchStrength ("Glitch Strength", Float) = 0.0
        _GlitchScale ("Glitch Scale", Float) = 20.0
        _GlitchSpeed ("Glitch Speed", Float) = 8.0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200

        // Transparent blend state
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 bary       : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _LineColor;
                float4 _FillColor;
                float _FillAlpha;
                float _EmissionStrength;
                float _LineWidth;
                float _LineFeather;
                float _LineAlpha;
                float _FresnelPower;
                float _FresnelStrength;
                float _ScanScale;
                float _ScanSpeed;
                float _ScanStrength;
                float4 _ScanDirection;
                float4 _PulseCenter;
                float _PulseSpeed;
                float _PulseWidth;
                float _PulseStrength;
                float _PulseInterval;
                float _Reveal;
                float _RevealMinY;
                float _RevealMaxY;
                float _RevealSoftness;
                float _RevealGlowStrength;
                float _GlitchStrength;
                float _GlitchScale;
                float _GlitchSpeed;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                // Object to world
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.positionWS = worldPos;
                output.positionCS = TransformWorldToHClip(worldPos);
                // Normal to world (handles scaling)
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                // Pass barycentric coordinates encoded in vertex color
                output.bary = input.color.rgb;
                return output;
            }

            // Simple hash-based noise function for glitch effect
            float hash21(float2 p)
            {
                // fract(sin(...)) noise; cheap and deterministic
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 positionWS = input.positionWS;
                float3 normalWS = normalize(input.normalWS);

                // Compute wireframe line factor from barycentric coordinates
                float3 bary = input.bary;
                float3 d = fwidth(bary);
                float3 s = smoothstep(d * _LineWidth, d * (_LineWidth + _LineFeather), bary);
                float wireEdge = 1.0 - min(min(s.x, s.y), s.z);

                // Base fill opacity
                float fill = _FillAlpha;

                // Fresnel rim lighting based on view direction
                float3 viewDirWS = normalize(_WorldSpaceCameraPos - positionWS);
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower) * _FresnelStrength;

                // Scan line effect along arbitrary direction
                float scanCoord = dot(positionWS, normalize(_ScanDirection.xyz));
                float scan = sin(scanCoord * _ScanScale + _Time.y * _ScanSpeed) * 0.5 + 0.5;
                scan *= _ScanStrength;

                // Radar pulse expanding from centre on XZ plane
                float dist = distance(positionWS.xz, _PulseCenter.xz);
                float t = fmod(_Time.y * _PulseSpeed, _PulseInterval);
                float pulse = 1.0 - smoothstep(0.0, _PulseWidth, abs(dist - t));
                pulse *= _PulseStrength;

                // Reveal animation mask based on world Y
                float revealHeight = lerp(_RevealMinY, _RevealMaxY, _Reveal);
                float revealMask = 1.0 - smoothstep(revealHeight, revealHeight + _RevealSoftness, positionWS.y);
                float revealEdge = 1.0 - abs(positionWS.y - revealHeight) / _RevealSoftness;
                revealEdge = saturate(revealEdge);

                // Optional glitch: modulate effect and alpha by noise
                float glitchValue = 0.0;
                if (_GlitchStrength > 0.0)
                {
                    float2 noisePos = positionWS.xy * _GlitchScale + _Time.y * _GlitchSpeed;
                    float noise = hash21(noisePos);
                    // Create sporadic blackouts by thresholding noise
                    glitchValue = step(1.0 - _GlitchStrength, noise);
                }

                // Combine secondary effects into a multiplier for emission
                float effect = 1.0 + fresnel + scan + pulse + revealEdge * _RevealGlowStrength;
                // Reduce effect in glitch regions
                effect = lerp(effect, effect * (1.0 - glitchValue), _GlitchStrength);

                // Colour contributions from fill and line
                float3 fillColor = _FillColor.rgb * _EmissionStrength * effect;
                float3 lineColor = _LineColor.rgb * _EmissionStrength * effect;
                float3 finalColor = lerp(fillColor, lineColor, wireEdge);

                // Alpha: use max of fill or line, scaled by reveal mask and glitch
                float alpha = max(_FillAlpha, wireEdge * _LineAlpha);
                alpha *= revealMask;
                alpha *= 1.0 - glitchValue;

                return float4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
    // Use a fallback that displays an error if URP is not available
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}