# Particle Trail Material Spec

## 目的

Particle System Trails、TrailRenderer、LineRenderer で共通利用できる、URP/WebGL 向けの軽量 Transparent Unlit Trail Shader を提供する。風、砂ぼこり、光の残像、Particle Effect の Trail 表現を同じ authoring contract で扱えるようにする。

## Shader

- Shader name: `BC/Particles/TrailUnlit`
- Path: `Assets/Art/Shader/Particles/TrailUnlit/BC_Particles_TrailUnlit.shader`
- Render Pipeline: Universal Render Pipeline
- Surface: Transparent / Unlit
- Target: `#pragma target 2.0`
- Base sample: 1
- Noise sample: 1
- 初期実装では Depth Texture、Opaque Texture、Compute、VFX Graph、Lit lighting、heavy distortion を使わない。

## HLSL 分割

- `TrailUnlit_Input.hlsl`: texture、sampler、material CBUFFER
- `TrailUnlit_Common.hlsl`: safe math、UV scroll、edge fade、dissolve helper
- `TrailUnlit_Sampling.hlsl`: base/noise sampling
- `TrailUnlit_Surface.hlsl`: final color、alpha、noise、dissolve、emission composition
- `Passes/TrailUnlit_ForwardPass.hlsl`: vertex/fragment pass

単一の巨大な HLSL にはしない。各ファイルには、なぜその分割があるのか分かる短いコメントを残す。

## Material Properties

### Rendering

- `_BlendMode`: Alpha / Additive / Premultiply / Multiply
- `_SrcBlend`, `_DstBlend`: hidden render-state properties
- `_Cull`: Off / Front / Back
- `_ZTest`: CompareFunction
- `_ZWrite`: hidden、初期実装では常に Off
- `_QueueOffset`: Transparent queue offset

### Base

- `_BaseMap`
- `_BaseColor`
- `_Alpha`
- `_Brightness`
- `_UseVertexColor`

### UV / Noise / Dissolve

- `_UVScrollSpeed`
- `_NoiseMap`
- `_NoiseStrength`
- `_NoiseScale`
- `_NoiseScrollSpeed`
- `_DissolveAmount`
- `_DissolveSoftness`

### Edge Fade / Emission

- `_EdgeFadeAxis`: U/V。Trail の texture mode や renderer による横幅UV差を吸収する。
- `_EdgeFadePower`
- `_EdgeFadeStrength`
- `_EmissionColor`
- `_EmissionStrength`

## Blend Mode Contract

- Alpha: `SrcAlpha`, `OneMinusSrcAlpha`
- Additive: `SrcAlpha`, `One`
- Premultiply: `One`, `OneMinusSrcAlpha`。Fragment 側で RGB を alpha 乗算する。
- Multiply: `DstColor`, `OneMinusSrcAlpha`

Blend Mode は shader keyword では増やさず、ShaderGUI/Validator が hidden render-state properties を同期する。

## Generated Assets

### Textures

- `T_Trail_SoftLine`
- `T_Trail_DustNoiseLine`
- `T_Trail_WindStreak`
- `T_Trail_LightBeam`
- `T_Noise_SoftCloud`
- `T_Noise_Streak`
- `T_Noise_Dissolve`

### Materials

- `M_Trail_Dust_Alpha`
- `M_Trail_Wind_AlphaScroll`
- `M_Trail_Light_Additive`
- `M_Trail_Magic_Additive`
- `M_Trail_Smoke_Alpha`
- `M_Trail_SpeedLine_Alpha`

### Prefabs

- `FX_Dust_Trail`
- `FX_Wind_Trail`
- `FX_LightBeam_Trail`
- `FX_Wind_LineRenderer`
- `FX_LightBeam_TrailRenderer`

## Validation

- Shader/HLSL/Editor/Spec の存在を EditMode test で確認する。
- Material が `BC/Particles/TrailUnlit` を参照していることを確認する。
- Blend Mode から `_SrcBlend` / `_DstBlend` / renderQueue が同期されることを確認する。
- Prefab の Particle System Trails module と trail material assignment を確認する。
- LineRenderer / TrailRenderer prefab が同じ material contract で使えることを確認する。
- shader source に不要な `shader_feature` が増えていないことを確認する。
