# Particle Material System Spec

## 0. この仕様書の位置づけ

本仕様書は、Unity 6 + URP 環境における Particle System 本体粒子用 Material 基盤の全体設計を定義する。

対象は Trail ではなく、Particle System Renderer で描画される粒子本体である。Trail は既存の [Assets/Docs/ParticleTrailMaterialSpec.md](Assets/Docs/ParticleTrailMaterialSpec.md) と [Assets/Art/Shader/Particles/TrailUnlit/BC_Particles_TrailUnlit.shader](Assets/Art/Shader/Particles/TrailUnlit/BC_Particles_TrailUnlit.shader) で別契約として管理されているため、本仕様には統合しない。

正式な Shader Family 名は以下に統一する。

```text
BC/Particles/ParticleUnlit
BC/Particles/ParticleLit
BC/Particles/ParticleDistortion
```

M14 設計予約として、以下の future family 名も canonical 候補として固定する。

```text
BC/Particles/ParticleRingUnlit
BC/Particles/ParticleGroundUnlit
```

`PartcleDistortion` は誤字として扱い、以後は `ParticleDistortion` に統一する。

---

## 1. リポジトリ整合方針

このリポジトリでは、Particle 系 Shader はすでに Trail 側で以下の実装規約を持っている。


M10 実装注記:

```text
- material property は増やさず、ParticleSystem.customData の Custom1 と active vertex streams で制御する
- shader は Custom1.xyzw を TEXCOORD1 で受け取り、Custom1.x/y を additive delta、Custom1.z を noise UV offset として扱う
- Custom1.w は variant index 予約のまま 0 default を維持する
- Debug View は 13=Custom1、15=UV まで実装し、14=Custom2 は予約のままとする
```
```text
- Shader 名は BC/ 接頭辞で統一する
- アセット配置は Assets/Art/... を使う
- 仕様書は Assets/Docs/... に置く
- ShaderGUI と MaterialValidator で hidden render-state property を同期する
- Bootstrapper で検証用 Texture / Material / Prefab を再生成できるようにする
- EditMode test で Shader / Spec / Editor code / 生成物契約を監査する
- WebGL 向け軽量契約として不要な shader_feature を増やさない
```

Particle Material System も同じ方針に従う。

つまり、本仕様は単に見た目の要件を列挙するだけではなく、以下の実装単位まで含めて固定する。

```text
- Shader
- HLSL 分割
- ShaderGUI
- MaterialValidator
- PresetUtility
- ValidationBootstrapper
- EditMode validation tests
- Spec document
```

コード上の実装はまだ TrailUnlit が先行しており、Particle 本体用 Shader 群は未着手である。そのため本仕様では、現行実装と将来予約を明確に分離する。

---

## 2. 目的

この基盤の目的は、埃や火花を表示する単発 Shader を増やすことではない。
将来的に以下を段階的に追加しても、Shader・Material・Prefab・Texture 規約を根本から作り直さず拡張できる土台を作ることを目的とする。

```text
- 埃
- 煙
- 霧
- 光粒
- 火花
- 魔法粒子
- 雪
- 雨
- 泡
- 葉っぱ
- 破片
- 地面煙
- 爆発煙
- 熱気
- 空気の歪み
- 水中の屈折
- 魔法の空間歪み
- 衝撃波
- Ring / Radial Particle
- Mesh Particle
- Flipbook Particle
- Custom Data 制御 Particle
```

---

## 3. 最優先方針

```text
- WebGL でも破綻しにくい
- URP で扱いやすい
- Particle System と相性がよい
- Material 設定だけで用途を分けられる
- 用途別 Shader を乱立させない
- 後から Lit / Distortion / Flipbook / Custom Data を追加できる
- Texture 規約と Custom Data 規約を最初に固定する
- Debug と Quality Tier を設計段階から考慮する
```

ParticleUnlit の初期実装では、TrailUnlit と同様に WebGL を意識した軽量契約を優先する。

```text
- #pragma target 2.0 を基準候補とする
- Depth Texture / Opaque Texture 依存は初期実装に入れない
- Blend Mode は keyword ではなく hidden render-state property で切り替える
- 不要な shader_feature を増やさない
- 高機能版は別 Family または Advanced 側に分離する
```

---

## 4. Scope

### 4.1 対象

```text
- Particle System Renderer で描画される粒子本体
- Billboard Particle
- Stretched Billboard Particle
- Horizontal Billboard Particle
- Vertical Billboard Particle
- Mesh Particle
- Texture Sheet Animation
- Custom Data Module
- Custom Vertex Streams
- URP 向け Particle Shader
```

### 4.2 非対象

```text
- Particle System Trails 専用 Material
- TrailRenderer / LineRenderer 専用 Shader
- VFX Graph 専用 Shader
- Post Process 全画面 Effect
- Compute Shader 前提の GPU Particle
- Simulation そのものを制御する仕組み
```

Trail 用は次の既存仕様で管理する。

```text
BC/Particles/TrailUnlit
```

Particle 本体と Trail は統合しない。理由は、UV 構造・用途・必要なフェード・Prefab 設計が異なるためである。

---

## 5. Shader Family

### 5.1 中核 Family

```text
BC/Particles/ParticleUnlit
  軽量な透明 Unlit 粒子用。
  初期実装の中心。

BC/Particles/ParticleLit
  ライト影響を受ける粒子用。
  Mesh Particle を主対象とする。

BC/Particles/ParticleDistortion
  背景歪み・熱気・空気の揺らぎ用。
  Opaque Texture / Camera Color Texture 依存のため後続拡張。
```

### 5.2 将来候補

```text
BC/Particles/ParticleRingUnlit
BC/Particles/ParticleGroundUnlit
BC/Particles/ParticleUnlitLite
BC/Particles/ParticleUnlitAdvanced
```

M14 実装注記:

```text
- ParticleRingUnlit は Shockwave / Magic Circle / Water Ripple / Landing Ring / Explosion Ring の ownership を持つ設計専用 family とする
- ParticleGroundUnlit は Ground Smoke / Floor Mist / Dust Cloud / Magic Ground Aura / Creeping Fog の ownership を持つ設計専用 family とする
- M14 では shader root や generated material を完成させず、docs / scaffold / source contract までを fixed scope とする
```

### 5.3 分離方針

以下を 1 つの巨大 Shader に統合しない。

```text
- Unlit
- Lit
- Distortion
- Ring
- Ground
- Trail
```

理由は以下。

```text
- 不要な分岐が増える
- Variant と Inspector が肥大化する
- WebGL で重くなりやすい
- Material 作成時の事故が増える
- 用途ごとの最適化が難しくなる
```

---

## 6. 実装フェーズ定義

### 6.1 現在の実装状況

このリポジトリで現時点で存在する Particle 関連の本実装は TrailUnlit 系のみである。

```text
Exists today:
  Assets/Art/Shader/Particles/TrailUnlit/
  Assets/Art/Materials/Particles/Trails/
  Assets/Art/Textures/Particles/Trails/
  Assets/Art/Prefab/Particles/Trails/
```

### 6.2 初期実装対象

本仕様で最初に実装する対象は以下に限定する。

```text
BC/Particles/ParticleUnlit
```

初期 Material:

```text
M_Particle_Dust_Alpha
M_Particle_Smoke_Alpha
M_Particle_Glow_Premultiply
M_Particle_Spark_Additive
M_Particle_Magic_Additive
```

初期 Prefab:

```text
FX_Particle_Dust
FX_Particle_Smoke
FX_Particle_Glow
FX_Particle_Spark
FX_Particle_Magic
```

### 6.3 予約のみ

以下は初期実装では作らないが、命名とデータ構造だけ先に固定する。

```text
- ParticleLit
- ParticleDistortion
- Flipbook Blend
- Custom Data
- Flow Map
- Depth Interaction
- Fake Lighting / Rim
- Ring / Ground 系 Shader
- Advanced Debug
- Quality Tier 分離
```

---

## 7. 実装アセット構成

現行リポジトリの Assets/Art 規約に合わせ、Particle 本体用の目標配置を以下に固定する。

```text
Assets/
  Art/
    Shader/
      Particles/
        ParticleUnlit/
          BC_Particles_ParticleUnlit.shader
          HLSL/
            ParticleUnlit_Input.hlsl
            ParticleUnlit_Common.hlsl
            ParticleUnlit_Sampling.hlsl
            ParticleUnlit_Surface.hlsl
            Passes/
              ParticleUnlit_ForwardPass.hlsl
          Editor/
            ParticleUnlitShaderGUI.cs
            ParticleUnlitMaterialValidator.cs
            ParticleUnlitPresetUtility.cs
            ParticleUnlitValidationBootstrapper.cs

        ParticleLit/
          BC_Particles_ParticleLit.shader

        ParticleDistortion/
          BC_Particles_ParticleDistortion.shader

    Materials/
      Particles/
        Unlit/
        Lit/
        Distortion/

    Textures/
      Particles/
        Unlit/
        Lit/
        Distortion/
        Shared/

    Prefab/
      Particles/
        Unlit/
        Lit/
        Distortion/

  Docs/
    ParticleUnitSpec/
      ParticleMaterialSystemSpec.md

  Tests/
    EditMode/
      Particles/
        ParticleUnlitValidationTests.cs
```

Trail 系が `Trails` フォルダを使っているのに対し、本仕様では粒子本体を Family ベースで `Unlit` / `Lit` / `Distortion` に分離する。Trail と本体粒子は別資産群として管理する。

---

## 8. 実装パターン契約

TrailUnlit との整合のため、ParticleUnlit 以降も以下のパターンで実装する。

### 8.1 HLSL 分割

```text
Input      : texture, sampler, material CBUFFER
Common     : safe math, blend helper, noise/dissolve helper
Sampling   : texture sampling と UV 組み立て
Surface    : final color / alpha / emission composition
Passes     : vertex / fragment pass
```

単一の巨大 HLSL にはしない。複雑な境界には短いコメントを残し、意図が分かるようにする。

### 8.2 ShaderGUI + Validator

Blend state や queue は Material の hidden property を ShaderGUI / MaterialValidator が同期する。

最低限予約する hidden property は以下。

```csharp
_SrcBlend
_DstBlend
_ZWrite
_ColorMask
_QueueOffset
```

### 8.3 PresetUtility

Material preset は ScriptableObject ではなく、少なくとも初期段階では TrailUnlit と同様に Utility で値を適用する方式を許容する。

### 8.4 Bootstrapper

検証用 Texture / Material / Prefab は、手作業だけに依存せず Bootstrapper で再生成できるようにする。

### 8.5 EditMode Validation

最低限以下を EditMode test で監査する。

```text
- Shader / HLSL / Editor code / Spec の存在
- Material が正しい Shader を参照していること
- BlendMode と hidden render-state property の同期
- 生成物の命名と配置
- WebGL 向け軽量契約に反する記述が紛れ込んでいないこと
```

---

## 9. 共通 Property 命名規則

Particle Shader Family 全体で、同じ意味の Property 名は統一する。

### 9.1 Rendering

```csharp
_BlendMode
_Cull
_ZTest
_SrcBlend
_DstBlend
_ZWrite
_ColorMask
_QueueOffset
_DebugMode
```

`_BlendMode` は project-wide の authoring property とし、実際の Blend state は Validator が hidden property に同期する。

### 9.2 Base

```csharp
_BaseMap
_BaseColor
_Alpha
_Brightness
_UseVertexColor
```

### 9.3 Mask

```csharp
_MaskMap
_MaskStrength
```

### 9.4 Noise

```csharp
_NoiseMap
_NoiseStrength
_NoiseScale
_NoiseScrollSpeed
_NoiseSpace
```

### 9.5 Dissolve / Erosion

```csharp
_DissolveAmount
_DissolveSoftness
_DissolveEdgeColor
_DissolveEdgeStrength
```

### 9.6 Emission

```csharp
_EmissionColor
_EmissionStrength
_EmissionAlphaInfluence
```

### 9.7 Shape

```csharp
_SoftCircleStrength
_EdgeFadePower
_EdgeFadeStrength
_ShapeMode
```

### 9.8 UV / Flipbook / Flow / Lit / Distortion

```csharp
_UVScrollSpeed

_FlipbookBlend
_FlipbookRows
_FlipbookColumns
_FlipbookMode

_FlowMap
_FlowStrength
_FlowScrollSpeed
_FlowAlphaInfluence

_NormalMap
_NormalStrength
_Smoothness
_Metallic
_LightInfluence

_DistortionMap
_DistortionStrength
_DistortionScale
_DistortionScrollSpeed
_DistortionAlphaInfluence

_RingInnerRadius
_RingOuterRadius
_RingThickness
_PolarScrollSpeed
_RadialDissolveAmount
_EdgeEmissionColor
_EdgeEmissionStrength

_WorldUvScale
_WorldUvScroll
_HeightFadeStart
_HeightFadeEnd
_GroundContactFade
_DirectionalNoiseMap
_DirectionalNoiseStrength
_DirectionalNoiseScroll
```

M14 実装注記:

```text
- Ring / Ground 系の reserved property は命名だけ先に固定し、M14 では shader 実装しない
- Ring shape mode を ParticleUnlit の `_ShapeMode` に押し戻さず、専用 family で扱う前提にする
- Ground の world-space / contact fade は一般 billboard variation と切り分ける
```

---

## 10. Texture Packing Specification

後から作り替えになりやすい Texture 構造を先に固定する。

### 10.1 MaskMap Layout

`_MaskMap` の RGBA を以下に固定する。

```text
R = Dissolve / Erosion Mask
G = Emission Mask
B = Variation / Noise Influence
A = Secondary Alpha / Shape Mask
```

### 10.2 Import ルール

BaseMap:

```text
sRGB: ON
Alpha: Input Texture Alpha
Wrap: Clamp 推奨
Filter: Bilinear
```

MaskMap:

```text
sRGB: OFF
Wrap: Clamp or Repeat
Filter: Bilinear
低品質圧縮は禁止
```

NoiseMap:

```text
sRGB: OFF
Wrap: Repeat
Filter: Bilinear
低品質圧縮は禁止
```

NormalMap:

```text
Texture Type: Normal Map
sRGB: OFF
```

DistortionMap:

```text
sRGB: OFF
Wrap: Repeat
Filter: Bilinear
```

---

## 11. Custom Data Layout

初期実装では必須にしない。ただし、将来の作り替えを避けるためレイアウトは固定する。

### 11.1 Custom1

```text
Custom1.x = Dissolve Amount Override
Custom1.y = Emission Strength Override
Custom1.z = Noise Offset / Random01
Custom1.w = Variant Index
```

### 11.2 Custom2

```text
Custom2.x = Distortion Strength Override
Custom2.y = Fake Light / Rim Influence
Custom2.z = Flow Strength
Custom2.w = Reserved
```

### 11.3 ルール

```text
- Custom1 / Custom2 の意味を途中で変えない
- Shader ごとに別の意味を持たせない
- 予約済みスロットを場当たり的に再利用しない
- Custom Data 前提の Material / Prefab は命名で識別できるようにする
```

---

## 12. Vertex Stream Specification

### 12.1 初期 ParticleUnlit 必須

```text
Position
Color
UV
```

### 12.2 Flipbook 用予約

```text
UV
UV2
AnimBlend
```

### 12.3 Custom Data 用予約

```text
Custom1.xyzw
Custom2.xyzw
```

### 12.4 将来候補

```text
Velocity
```

---

## 13. ParticleUnlit Specification

### 13.1 目的

`BC/Particles/ParticleUnlit` は、最も使用頻度の高い軽量 Particle Shader である。

対象:

```text
- 埃
- 煙
- 霧
- 光粒
- 火花
- 魔法粒子
- 雪
- 爆発の残り粒子
- 空気中の浮遊粒子
```

### 13.2 Render State

```text
Surface: Transparent
ZWrite: Off
ZTest: LEqual
Cull: Off
Lighting: None
Queue: Transparent
```

### 13.3 対応 Blend Mode

```text
Alpha
Additive
Premultiply
Multiply
```

初期優先度:

```text
1. Alpha
2. Additive
3. Premultiply
4. Multiply
```

### 13.4 初期必須機能

```text
- BaseMap
- BaseColor
- Alpha
- Brightness
- Vertex Color
- Particle Alpha
- Soft Circle
- MaskMap
- Noise
- Dissolve / Erosion
- Emission
- UV Scroll
```

### 13.5 基本合成式

```text
Base = Sample(_BaseMap, uv)

Color.rgb =
    Base.rgb
  * _BaseColor.rgb
  * VertexColor.rgb
  * _Brightness

Alpha =
    Base.a
  * _BaseColor.a
  * VertexColor.a
  * _Alpha
```

MaskMap:

```text
DissolveMask = MaskMap.r
EmissionMask = MaskMap.g
Variation    = MaskMap.b
ShapeMask    = MaskMap.a
```

Emission:

```text
Emission =
    Base.rgb
  * _EmissionColor.rgb
  * _EmissionStrength
  * EmissionMask
```

Final:

```text
FinalRGB = Color.rgb + Emission
FinalA   = Alpha
```

Premultiply 時は Fragment 側で `FinalRGB *= FinalA` を適用する。これは既存 TrailUnlit の設計と揃える。

### 13.6 Soft Circle

```text
centeredUV = uv * 2 - 1
distance = length(centeredUV)
circleMask = 1 - saturate(distance)
softCircle = pow(circleMask, _EdgeFadePower)

Alpha *= lerp(1, softCircle, _SoftCircleStrength)
```

### 13.7 Noise

初期は `ParticleUV` 基準とする。

```text
0 = ParticleUV
1 = WorldXZ
2 = WorldXYZ
3 = ScreenUV
```

初期実装対象は `ParticleUV` のみ。その他は予約値とする。

### 13.8 Dissolve / Erosion

```text
DissolveMask = smoothstep(
    _DissolveAmount,
    _DissolveAmount + _DissolveSoftness,
    NoiseOrMask)

Alpha *= DissolveMask
```

### 13.9 初期 Material Preset

```text
M_Particle_Dust_Alpha
M_Particle_Smoke_Alpha
M_Particle_Glow_Premultiply
M_Particle_Spark_Additive
M_Particle_Magic_Additive
```

---

## 14. ParticleLit Specification

`BC/Particles/ParticleLit` はライト影響を受ける粒子用 Shader である。

対象:

```text
- 雨粒
- 泡
- 葉っぱ
- 紙片
- 石片
- 木片
- 金属片
- 破片
- Mesh Particle
```

初期実装で後回しにする理由:

```text
- 埃、煙、火花、光粒には不要
- Unlit より重い
- WebGL では負荷が高くなりやすい
- Mesh Particle 設計とセットで考えるべき
```

将来機能:

```text
- BaseMap
- BaseColor
- Alpha
- Vertex Color
- NormalMap
- NormalStrength
- Smoothness
- Metallic
- LightInfluence
- Rim Light
- Emission
- Dissolve
- Alpha Clip
```

M12 実装注記:

```text
- 初回実装では BaseMap / BaseColor / Alpha / Vertex Color / NormalMap / NormalStrength / Smoothness / Metallic / LightInfluence / Emission に限定する
- lighting は main light + minimal ambient + simple specular のみとし、Additional Lights、Rim Light、Alpha Clip、Dissolve はこの段階では入れない
- Billboard validation は Raindrop / Bubble、Mesh validation は Debris で行い、validation scene は ParticleMaterialTestScene の Future Lit Test Area を再利用する
- mesh particle 向け validation では ParticleSystemRenderer.renderMode = Mesh と cube mesh assignment を使い、ParticleUnlit family へ機能を逆流させない
- normal map 用 validation texture は generated asset とし、editor validator は hidden blend/render-state property を含めて deterministic に正規化する
```

---

## 15. ParticleDistortion Specification

`BC/Particles/ParticleDistortion` は背景歪みを伴う粒子表現用 Shader である。

対象:

```text
- 熱気
- 空気の揺らぎ
- 水中屈折
- 魔法の空間歪み
- 爆発後の衝撃波
- 強風の歪み
```

後回しにする理由:

```text
- Opaque Texture / Camera Color Texture 依存になりやすい
- WebGL では重い
- URP 設定依存が強い
- 通常 Particle 表現には不要
```

将来機能:

```text
- DistortionMap
- DistortionStrength
- DistortionScale
- DistortionScrollSpeed
- DistortionAlphaInfluence
- Noise
- Edge Fade
- Depth Fade
- Flow Map
```

M13 実装注記:

```text
- ParticleDistortion は URP Opaque Texture を正規経路とし、DeclareOpaqueTexture.hlsl + SampleSceneColor で scene color をサンプリングする
- M13 の実装範囲は DistortionMap / DistortionStrength / DistortionScale / DistortionScrollSpeed / Alpha / Edge Fade / Noise optional までに限定し、Depth Fade / Flow Map / GrabPass は入れない
- editor authoring は ParticleDistortionShaderGUI / ParticleDistortionMaterialValidator / ParticleDistortionPresetUtility で構成し、Opaque Texture 依存 warning と高負荷 warning を inspector で出す
- validation bootstrapper は generated distortion vector/noise texture、M_Particle_HeatHaze_Distortion / M_Particle_AirWarp_Distortion / M_Particle_MagicWarp_Distortion、FX_Particle_HeatHaze / FX_Particle_AirWarp / FX_Particle_MagicWarp を再生成し、ParticleMaterialTestScene の Future Distortion Test Area に validation anchor と prefab preview を置く
- ParticleDistortion は標準 WebGL required set に入れず、WebGL standard unsupported の境界を docs / tests の両方で明示する
```

---

## 16. Flipbook / Atlas Specification

初期は Unity Particle System の Texture Sheet Animation Module による通常再生に対応する。

```text
- Grid Flipbook
- Random Start Frame
- Frame over Time
```

M9 実装注記:

```text
- ParticleUnlit shader は UV0 sampling のまま維持し、flipbook frame の切替は Particle System の Texture Sheet Animation module 側で行う
- `_FlipbookBlend` / `_FlipbookRows` / `_FlipbookColumns` / `_FlipbookMode` はこの時点では material property として未実装の予約値とする
- generated validation atlas は 4x4 grid を基準にし、Smoke / Magic Burst / Explosion placeholder を用意する
- flipbook atlas texture importer は sRGB on、Clamp、Bilinear、No Mipmap、Uncompressed を基準にする
```

将来対応:

```text
- Flipbook Blend
- UV2
- AnimBlend
- Variant Atlas
- Custom1.w による Variant Index 選択
```

---

## 17. Shape / Depth / Fake Lighting 予約

### 17.1 ShapeMode

```text
0 = Texture Alpha Only
1 = Soft Circle
2 = Ellipse
3 = Ring
4 = Streak
5 = Box Soft
6 = Procedural Star
```

初期対応は `Texture Alpha Only` と `Soft Circle` のみ。

### 17.2 Depth Interaction 予約

```csharp
_UseSoftParticles
_SoftParticleDistance
_UseCameraFade
_CameraFadeNear
_CameraFadeFar
_IntersectionStrength
_IntersectionColor
```

M11 実装では Soft Particles と Camera Fade のみ先行対応し、shader keyword は増やさず runtime toggle で制御する。shipping material / prefab は default-off を維持し、validation scene では dedicated material で ON 状態を確認する。

### 17.3 Fake Lighting / Rim 予約

```csharp
_RimColor
_RimPower
_RimStrength
_FakeLightDirection
_FakeLightStrength
```

---

## 18. Renderer Mode Compatibility

```text
ParticleUnlit:
  Billboard
  Stretched Billboard
  Horizontal Billboard
  Vertical Billboard

ParticleLit:
  Mesh Particle
  Billboard optional

ParticleDistortion:
  Billboard
  Horizontal Billboard optional

ParticleRingUnlit:
  Billboard
  Horizontal Billboard
  Mesh optional in future milestone

ParticleGroundUnlit:
  Horizontal Billboard
  Billboard optional
  Mesh optional in future milestone
```

M14 実装注記:

```text
- Ring / Ground は renderer mode 前提が強いため、ParticleUnlit の generic mode 拡張ではなく family 分離で扱う
- Ground family は flat placement を前提にするため、Horizontal Billboard を primary に据える
```

---

## 19. Sorting / Transparency Policy

Transparent Particle は描画順破綻が起きやすい。Material だけで解決しようとしない。

```text
- Blend Mode
- Render Queue Offset
- ParticleSystemRenderer Sort Mode
- Sorting Fudge
- Particle サイズ
- 発生位置
- ZWrite
```

原則:

```text
ZWrite Off を基本とする
```

`_QueueOffset` は既存 TrailUnlit と同じく project-wide property とし、Validator が renderQueue と同期する。

---

## 20. Quality Tier Policy

```text
ParticleQuality.Low
  BaseMap
  Vertex Color
  Alpha
  No Noise
  No Dissolve
  No Depth
  No Distortion

ParticleQuality.Medium
  BaseMap
  MaskMap
  Noise
  Dissolve
  Emission

ParticleQuality.High
  Medium +
  Flipbook Blend
  Soft Particles
  Camera Fade
  Custom Data

ParticleQuality.Ultra
  High +
  ParticleLit
  ParticleDistortion
  Flow Map
  Advanced Depth Interaction
```

初期では `ParticleUnlit` のみでよい。ただし Quality Tier の概念は仕様として固定する。

M14 実装注記:

```text
- ParticleRingUnlit / ParticleGroundUnlit は M14 時点では quality tier へ未配属の reserved family とする
- 最初の機能 milestone で ParticleUnlit 相当か High 以上かを再評価する
```

M15 実装注記:

```text
- ParticleUnlit は hidden property `_QualityTier` を持ち、Low / Medium / High の authored tier を material に保持する
- ParticleUnlitQualityTierUtility により authored / inferred tier を共通化し、ShaderGUI / MaterialValidator / BuildValidator / bootstrapper が同一 contract を参照する
- standard WebGL path は Low / Medium までを許容し、High tier ParticleUnlit material は build validator で reject する
- Ultra は family-level boundary として ParticleLit / ParticleDistortion 側に残し、ParticleUnlitLite / ParticleUnlitAdvanced の shader split は reserve のままとする
```

---

## 21. Debug View Specification

```text
0 = Final
1 = Base RGB
2 = Base Alpha
3 = Vertex Color
4 = Vertex Alpha
5 = MaskMap R / Dissolve
6 = MaskMap G / Emission
7 = MaskMap B / Variation
8 = MaskMap A / Shape
9 = Noise
10 = Dissolve Result
11 = Emission Result
12 = Soft Circle
13 = Custom1
14 = Custom2
15 = UV
```

初期実装で全 Debug を作る必要はないが、`_DebugMode` は予約済み Property とする。

---

## 22. Material / Prefab Policy

### 22.1 Material 命名規則

```text
M_Particle_{UseCase}_{BlendOrType}
```

例:

```text
M_Particle_Dust_Alpha
M_Particle_Glow_Premultiply
M_Particle_Spark_Additive
M_Particle_HeatHaze_Distortion
M_Particle_Bubble_Lit
```

### 22.2 Prefab 命名規則

```text
FX_Particle_{UseCase}
```

### 22.3 Prefab 責務

Prefab は Material だけでは決まらない Particle の見た目をまとめる。

```text
- Emission
- Shape
- Lifetime
- Speed
- Size
- Rotation
- Color over Lifetime
- Size over Lifetime
- Texture Sheet Animation
- Renderer Mode
- Sorting
- Custom Data
```

Material だけ作って Prefab を作らない運用は禁止とする。

---

## 23. 初期実装受け入れ条件

```text
- BC/Particles/ParticleUnlit が存在する
- M_Particle_Dust_Alpha が存在する
- M_Particle_Smoke_Alpha が存在する
- M_Particle_Glow_Premultiply が存在する
- M_Particle_Spark_Additive が存在する
- M_Particle_Magic_Additive が存在する
- 各 Material が Prefab 化されている
- Particle System 側の Color over Lifetime が反映される
- Particle Alpha が反映される
- Alpha / Additive / Premultiply が使える
- Noise / Dissolve / Emission が使える
- WebGL Build で Shader Error が出ない
```

---

## 24. Validation Policy

### 24.1 Test Scene

検証 Scene の目標名:

```text
ParticleMaterialTestScene.unity
```

### 24.2 EditMode test で確認する項目

```text
- Shader / HLSL / Editor / Spec の存在
- Material が `BC/Particles/ParticleUnlit` を参照している
- Blend Mode から `_SrcBlend` / `_DstBlend` / renderQueue が同期される
- generated prefab が既定 Material を参照している
- shader source に不要な `shader_feature` が増えていない
- Depth / Opaque Texture が初期実装へ紛れ込んでいない
```

### 24.3 見た目検証項目

```text
- BaseMap が表示される
- Vertex Color が反映される
- Particle Alpha が反映される
- Blend Mode が正しく見える
- MaskMap が正しく機能する
- Noise が正しく機能する
- Dissolve が正しく機能する
- Emission が正しく機能する
- Soft Circle で四角い板感が軽減される
- Bloom なしでも Glow / Spark が視認できる
- 明るい背景・暗い背景の両方で破綻しない
```

### 24.4 M16 Review Harness

M16 では `ParticleMaterialTestScene` を manual review harness として使う。

```text
- Dust / Smoke / Glow / Spark / Magic preview は既存 marker を使う
- Quality Tier Test Area は standalone review area として維持する
- Future Lit Test Area / Future Distortion Test Area は family boundary review に使う
- ParticleMaterialReviewHarness は bright/dark backdrop と Ring/Ground placeholder を置く standalone harness とする
- M1 eight-marker contract を壊す scene object 追加は行わない
```

---

## 25. Performance Policy

### 25.1 ParticleUnlit 標準予算

```text
BaseMap Sample: 1
MaskMap Sample: 0-1
NoiseMap Sample: 0-1
Depth Sample: 0
Opaque Texture Sample: 0
Lighting: なし
```

基本は Texture Sample 2 回以内を目標にする。

### 25.2 禁止事項

```text
- 画面全体を覆う半透明 Particle を大量に重ねる
- Additive を大量に重ねて白飛びさせる
- Distortion を常時大量発生させる
- WebGL 標準で ParticleLit を大量使用する
- Texture Sample を無制限に増やす
- Shader Keyword を無秩序に増やす
```

M16 実装注記:

```text
- standard WebGL path は Low / Medium までを canonical とし、High-tier boundary feature は build validator で reject する
- performance guide は `ParticleMaterialPerformanceGuide.md` に分離し、spec 側には contract のみ残す
- optimization は property 削除より、source audit と drift prevention を優先する
```

---

## 26. Future Extension Roadmap

```text
A. Flipbook 強化
  Flipbook Blend / UV2 / AnimBlend / Variant Atlas

B. Custom Data 制御
  Custom1.x Dissolve / Custom1.y Emission / Custom1.z Noise / Custom1.w Variant
  Custom2.x Distortion / Custom2.y Fake Light / Custom2.z Flow

C. Depth Interaction
  Soft Particles / Camera Fade / Intersection Highlight / Ground Contact Fade

D. ParticleLit
  NormalMap / Smoothness / Metallic / LightInfluence / Mesh Particle

E. ParticleDistortion
  DistortionMap / DistortionStrength / Opaque Texture / Flow Map

F. Ring / Radial Particle
  Ring Mask / Radial Dissolve / Polar UV / Edge Emission

G. Ground Particle
  Horizontal Billboard / World Space UV / Height Fade / Ground Contact Fade

H. Quality 分離
  ParticleUnlitLite / ParticleUnlitAdvanced / WebGL 用 Preset
```

---

## 27. 最終設計判断

この基盤で最も重要なのは、最初から全部の機能を実装することではない。
重要なのは、以下を先に固定することである。

```text
- Shader Family
- Property 命名
- Texture Packing
- Custom Data Layout
- Vertex Stream 方針
- Material / Prefab 命名
- Quality Tier
- Debug View
- Future Extension 分類
- Validator / Bootstrapper / Test を含む実装パターン
```

この設計にしておけば、最初は軽量な `ParticleUnlit` だけで始められる。
その後、必要に応じて `ParticleLit`、`ParticleDistortion`、Flipbook、Custom Data、Flow Map、Depth Interaction、Ring、Ground、Lite / Advanced 系へ安全に拡張できる。

逆に、ここを決めずに `BaseMap + Alpha + Emission` だけで作り始めると、後から以下で詰まりやすい。

```text
- Custom Data を後から無計画に足す
- MaskMap のチャンネル意味を後から変える
- Flipbook と Atlas の扱いを場当たりで決める
- Lit と Distortion を ParticleUnlit に混ぜる
- WebGL 用軽量版を後から無理やり作る
```

したがって、本仕様では「初期実装は小さく、拡張点は広く固定する」を採用する。
