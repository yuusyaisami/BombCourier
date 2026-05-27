# EnvironmentStylizedLit Property Reference

This reference is the module-local index for the material properties exposed by BC/EnvironmentStylizedLit.

Recommended values assume the M14 validation flow in ESL_TestRoom and ESL_LightingLab.

## Surface

| Property | Range / Type | Default | Recommended | Notes |
| --- | --- | --- | --- | --- |
| _Cull | Enum | Front | Front for normal walls, Back for inward rooms, Both only when required | Matches URP Render Face semantics so ProBuilder and standard URP Lit materials behave consistently. |
| _AlphaClip | 0 or 1 | 0 | Off for most opaque stage surfaces | Enable only when the silhouette must clip. |
| _Cutoff | 0-1 | 0.5 | 0.4-0.6 when Alpha Clip is enabled | ShadowCaster and depth passes follow this value. |

## Base

| Property | Range / Type | Default | Recommended | Notes |
| --- | --- | --- | --- | --- |
| _BaseMap | Texture2D | white | Use when UVs are stable | Prefer authored UVs before enabling triplanar. |
| _BaseColor | Color | white | Drive the broad material family here first | Presets treat this as the main look anchor. |
| _NormalMap | Texture2D | bump | Optional | Add only when the surface needs directional breakup. |
| _NormalScale | 0-2 | 1 | 0.3-1.2 | Large values can make stylized bands unstable. |
| _OcclusionMap | Texture2D | white | Optional | Use subtly on stage surfaces. |
| _OcclusionStrength | 0-1 | 1 | 0-0.5 for broad walls and floors | Heavy AO can flatten the stylized lighting. |
| _EmissionMap | Texture2D | black | Optional | Keep off for general environment materials. |
| _EmissionColor | Color | black | Low-intensity accents only | Meta pass exports bake-facing emission. |
| _EmissionStrength | 0-10 | 0 | 0-1 for environment use | High values dominate the stylized shading. |

## Main Lighting

| Property | Range / Type | Default | Recommended | Notes |
| --- | --- | --- | --- | --- |
| _LightStepCount | 1-5 | 3 | 3-4 | Fewer steps increase toon contrast. |
| _LightStepSmoothness | 0-0.5 | 0.08 | 0.04-0.16 | Lower values produce harder band transitions. |
| _WrapLighting | 0-1 | 0.15 | 0.1-0.3 | Use to keep broad walls from collapsing to black. |
| _BandContrast | 0.25-2 | 1 | 0.7-1.3 | Push gently before changing all band colors. |
| _BandOffset | -1 to 1 | 0 | -0.1 to 0.1 | Prefer small offsets. |
| _MainLightColorInfluence | 0-1 | 0.2 | 0.1-0.25 | Keep low so the palette drives the look more than the raw Unity light color. |
| _MainLightIntensityResponse | 0.25-8 | 1 | 0.8-1.5 | Higher values compress the bright core of the main light. |
| _DeepShadowColor | Color | cool shadow | Preset-owned | Lowest band color. |
| _ShadowColor | Color | cool shadow | Preset-owned | Main shadow tint. |
| _MidColor | Color | pale cool | Preset-owned | Middle band color. |
| _LightColor | Color | warm light | Preset-owned | Lit band color. |
| _HighlightColor | Color | warm highlight | Preset-owned | Top band color and specular pairing hint. |

## Shadow

| Property | Range / Type | Default | Recommended | Notes |
| --- | --- | --- | --- | --- |
| _ShadowInfluence | 0-1 | 1 | 0.75-1 | Keep near 1 for stage readability. |
| _ShadowSoftFill | 0-1 | 0.2 | 0.15-0.35 | Use before lifting all ambient colors. |
| _ShadowColorBlend | 0-1 | 0.6 | 0.45-0.8 | Tunes how strongly the shadow palette overrides the lit palette. |

## Ambient / Bounce / Indirect

| Property | Range / Type | Default | Recommended | Notes |
| --- | --- | --- | --- | --- |
| _AmbientTopColor | Color | cool blue | Preset-owned | Sky-facing ambient tint. |
| _AmbientSideColor | Color | muted cool | Preset-owned | Wall-facing ambient tint. |
| _AmbientBottomColor | Color | warm dark | Preset-owned | Down-facing ambient tint. |
| _AmbientStrength | 0-1 | 0.35 | 0.25-0.5 | Main indoor fill control. |
| _BounceColor | Color | warm bounce | Preset-owned | Use warm bounce for room readability. |
| _BounceStrength | 0-1 | 0.2 | 0.1-0.35 | Too high makes floors glow unnaturally. |
| _BounceDirection | Vector | up | Usually world up | Keep aligned to the intended bounce source. |
| _BounceWrap | 0-1 | 0.35 | 0.25-0.6 | Softens bounce falloff. |
| _IndirectShadowColor | Color | cool tint | Preset-owned | Indirect shadow tint used on baked / probe response. |
| _IndirectStrength | 0-1 | 1 | 0.8-1 | Main indirect lighting multiplier. |
| _IndirectStylizeStrength | 0-1 | 0.35 | 0.2-0.5 | Push only as far as readability allows. |

## Specular / Edge Sheen

| Property | Range / Type | Default | Recommended | Notes |
| --- | --- | --- | --- | --- |
| _Metallic | 0-1 | 0 | 0 for most diorama surfaces | Present for surface completeness, not physically-based targets. |
| _Smoothness | 0-1 | 0.35 | 0.1-0.65 | Higher values need matching specular intent. |
| _SpecularColor | Color | gray | Preset-owned | Keep close to the highlight palette. |
| _SpecularMode | Enum Off/Soft/Quantized/Ceramic/Plastic | Soft | Match surface family | Ceramic and Plastic are intentionally more stylized. |
| _SpecularStrength | 0-1 | 0.25 | 0.02-0.35 | Raise with care on large walls. |
| _SpecularStepCount | 1-5 | 3 | 3-4 when quantized | Lower values sharpen the stylized response. |
| _SpecularStepSmoothness | 0-0.5 | 0.1 | 0.04-0.16 | Keep small when using quantized highlights. |
| _EdgeSheenStrength | 0-1 | 0.15 | 0-0.2 | Best for toys and ceramic accents. |
| _EdgeSheenPower | 0.5-8 | 2.5 | 2-4 | Higher values narrow the rim. |
| _EdgeSheenColor | Color | warm highlight | Preset-owned | Keep consistent with main highlights. |

## Additional Lights

| Property | Range / Type | Default | Recommended | Notes |
| --- | --- | --- | --- | --- |
| _AdditionalLightMode | Enum Off/FillOnly/Quantized/Continuous | FillOnly | Off or FillOnly for broad stage use | Quantized and Continuous cost more and are best reserved for deliberate accents. |
| _AdditionalLightIntensity | 0-1 | 0.5 | 0-0.6 | Lower values keep point and spot lights supportive rather than dominant. |
| _AdditionalLightShadowInfluence | 0-1 | 0.65 | 0.4-0.8 | Preserve stylized readability when point / spot lights cast shadows. |
| _AdditionalLightColorInfluence | 0-1 | 0.75 | 0.5-0.85 | Lower values neutralize colored fill lights. |
| _AdditionalLightAttenuationPower | 0.25-8 | 1.8 | 1.5-2.5 | Higher values tighten the lit core and reduce round light blobs. |
| _AdditionalLightAttenuationStepCount | 1-5 | 3 | 3-4 | Controls how many attenuation steps each local light uses. |
| _AdditionalLightAttenuationSmoothness | 0-0.5 | 0.08 | 0.04-0.12 | Lower values make the attenuation bands harder. |
| _AdditionalLightPaletteBlend | 0-1 | 0.65 | 0.35-0.7 | Higher values bias local lights back toward the environment palette. |
| _AdditionalFillMaxMask | 0-1 | 0.45 | 0.25-0.5 | Caps FillOnly so it lifts shadows without washing the room. |

## Triplanar

| Property | Range / Type | Default | Recommended | Notes |
| --- | --- | --- | --- | --- |
| _TriplanarBaseMapEnabled | 0 or 1 | 0 | Off unless UVs are unstable | Local keyword owned. |
| _TriplanarNormalMapEnabled | 0 or 1 | 0 | Off unless the normal map also needs UV-independent projection | Local keyword owned. |
| _TriplanarNoiseEnabled | 0 or 1 | 0 | Off unless the noise path must match triplanar projection | Local keyword owned. |
| _TriplanarScale | 0.01-8 | 1 | Tune from world size, not texture guesswork | Revisit from the M13 rough-UV walls. |
| _TriplanarBlendSharpness | 1-8 | 4 | 3-6 | Higher values harden axis transitions. |

## Vertex / World Gradient

| Property | Range / Type | Default | Recommended | Notes |
| --- | --- | --- | --- | --- |
| _VertexColorEnabled | 0 or 1 | 0 | Enable only when authoring data exists | M11 keeps vertex color A reserved as a special-mask contract. |
| _VertexColorCavityStrength | 0-1 | 1 | 0.2-1 | Scales cavity tint from vertex data. |
| _VertexColorBandOffsetStrength | 0-1 | 1 | 0.1-0.8 | Avoid large offsets on broad surfaces. |
| _VertexColorColorVariationStrength | 0-1 | 1 | 0.1-0.8 | Use to break up repeated modules. |
| _WorldYGradientEnabled | 0 or 1 | 0 | Enable for plaster, chalk, and broad walls | Uniform branch, not a shader keyword. |
| _WorldYGradientTopColor | Color | white | Match the lit-side palette | Keeps upper surfaces airy. |
| _WorldYGradientBottomColor | Color | white | Slightly darker or warmer than the top color | Good for chalk and plaster looks. |
| _WorldYGradientMin | Float | 0 | Anchor to the bottom of the surface family | Validator normalizes Min/Max ordering. |
| _WorldYGradientMax | Float | 3 | Anchor to the top of the surface family | Use scene scale, not object local scale. |
| _WorldYGradientStrength | 0-1 | 0 | 0.15-0.4 when enabled | Keep subtle on large stage surfaces. |

## Noise / Distance Fade

| Property | Range / Type | Default | Recommended | Notes |
| --- | --- | --- | --- | --- |
| _NoiseSpace | Enum WorldNoise/ObjectSpaceNoise | WorldNoise | WorldNoise for modular stage surfaces | ObjectSpaceNoise is for stable local patterns. |
| _AlbedoNoiseStrength | 0-1 | 0.18 | 0.05-0.24 | Use sparingly before increasing band noise. |
| _WorldNoiseScale | 0.01-8 | 0.4 | 0.25-1.2 | Match world scale across modular pieces. |
| _WorldNoiseStrength | 0-1 | 0.35 | 0.1-0.35 | More is not automatically better. |
| _WorldNoiseContrast | 0.25-4 | 1.25 | 0.8-1.6 | Raise only after scale feels right. |
| _LightBandNoiseStrength | 0-1 | 0.08 | 0-0.14 on broad stage surfaces | Keep low to avoid dirty band edges. |
| _LightBandNoiseScale | 0.01-8 | 0.75 | 0.4-1.2 | Pair with low strength. |
| _NoiseDistanceFadeStart | 0-100 | 12 | Scene-dependent | Use M13 near / far viewpoints as the baseline. |
| _NoiseDistanceFadeEnd | 0-100 | 32 | Scene-dependent | Keep End above Start; validator normalizes this. |

## Cavity / AO / Debug

| Property | Range / Type | Default | Recommended | Notes |
| --- | --- | --- | --- | --- |
| _CavityStrength | 0-1 | 0.35 | 0.1-0.45 | Use to recover form, not to fake baked AO. |
| _CavityColor | Color | cool cavity | Preset-owned | Keep near the indirect palette. |
| _DebugView | Enum Off/NdotL/WrappedLight/SteppedLight/BandColor/WorldNoise/BandNoise | Off | Off outside focused review | M13 build validation blocks checked-in shipping materials from keeping this enabled. |

## Notes

- Properties are grouped to match the custom inspector in EnvironmentStylizedLitShaderGUI.
- Validation scenes are the canonical review surface for M14. Re-check large walls, rough-UV surfaces, point lights, spot lights, and preset strips there before touching production materials.
- If a property starts fighting readability, step back to the relevant preset first instead of stacking more overrides on a single material.
