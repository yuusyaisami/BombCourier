# Particle Material System Milestones

## 0. 目的

本 Milestone は、[Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemSpec.md](Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemSpec.md) を実装可能な単位へ分解するための実装計画である。

進捗の時点スナップショットは、計画本文とは分離して [Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemProgress.md](Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemProgress.md) に保存する。

対象は Unity 6 + URP + Particle System 本体粒子用 Material 基盤であり、中心となる Shader Family は以下に固定する。

```text
BC/Particles/ParticleUnlit
BC/Particles/ParticleLit
BC/Particles/ParticleDistortion
```

初期実装では `ParticleUnlit` を最優先する。`ParticleLit` と `ParticleDistortion` は責務と予約 Property を先に固定し、後続 Milestone で実装する。

---

## 0.1 現在の進捗棚卸し

この節は planned scope ではなく、現時点の repo 実体ベースの進捗である。

### 進捗サマリー

```text
- 現在の実装到達点: M2 最小 shader 実装中
- 既存の参照実装: TrailUnlit 系のみ
- ParticleUnlit の最小 shader source は追加済み
- M1 bootstrapper と scaffold test は追加済み
- M2 bootstrapper と validation test は追加済み
- bootstrapper の scene 復元漏れは修正済み
- generated material / texture の contract test は強化済み
```

### 現在 repo に存在する参照資産

```text
Assets/Docs/ParticleTrailMaterialSpec.md
Assets/Art/Shader/Particles/TrailUnlit/
Assets/Art/Shader/Particles/ParticleUnlit/BC_Particles_ParticleUnlit.shader
Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs
Assets/Art/Materials/Particles/Trails/
Assets/Art/Textures/Particles/Trails/
Assets/Art/Prefab/Particles/Trails/
Assets/Tests/EditMode/Particles/TrailUnlitValidationTests.cs
Assets/Tests/EditMode/Particles/ParticleMaterialSystemM1ScaffoldTests.cs
Assets/Tests/EditMode/Particles/ParticleUnlitValidationTests.cs
```

### Milestone Progress

| Milestone | Status | Progress | Notes |
| --- | --- | --- | --- |
| M0 基盤仕様確定 | Complete | 100% | [Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemSpec.md](Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemSpec.md) が存在し、Trail 分離、Property 命名、Texture Packing、Custom Data Layout、Folder 方針が固定済み。 |
| M1 Folder / Naming / Sample Scene | In Progress | 80% | Assets/Art / Assets/Scenes / Assets/Tests への配置方針で実装済み。bootstrapper と scaffold test は追加済みで、scene 正本の実生成確認だけが Unity project lock により保留。 |
| M2 ParticleUnlit 最小 Shader | In Progress | 72% | TrailUnlit と同系統の `.shader + HLSL` 最小実装、M2 bootstrapper 拡張、validation test、scene 復元漏れ修正、generated material / texture contract 強化まで追加済み。generated asset 実生成の確認だけが Unity project lock により保留。 |
| M3-M16 | Planned | 0% | 基盤未着手のため後続も未着手。 |

### 判断メモ

```text
- Trail 用 Shader と Particle 本体用 Shader は統合しない
- フォルダ規約は Assets/Art / Assets/Scenes / Assets/Docs / Assets/Tests に揃える
- Blend state は TrailUnlit と同じく hidden render-state property で同期する
- Bootstrapper と EditMode contract test を早い段階から入れる
```

---

## 1. Milestone 全体方針

### 1.1 実装順序

```text
M0  基盤仕様確定
M1  Folder / Naming / Sample Scene 基盤
M2  ParticleUnlit 最小 Shader
M3  Blend Mode / Material Preset 基盤
M4  MaskMap / Noise / Dissolve
M5  Emission / Glow / Spark 表現
M6  初期 Prefab 整備
M7  WebGL / Lightweight 検証
M8  Debug View / Inspector 整理
M9  Flipbook / Atlas 基盤
M10 Custom Data / Vertex Stream 基盤
M11 Depth Interaction 基盤
M12 ParticleLit 設計・最小実装
M13 ParticleDistortion 設計・最小実装
M14 Ring / Ground 系拡張設計
M15 Quality Tier / Shader 分離方針
M16 Optimization / Validation / Documentation
```

### 1.2 絶対に守ること

```text
- ParticleUnlit に Lit や Distortion を混ぜない
- Trail 用 Shader と統合しない
- 用途ごとに Shader を乱立しない
- Material だけ作って Prefab 化しない
- MaskMap のチャンネル意味を途中で変えない
- Custom Data Layout を場当たり的に変更しない
- WebGL を後付け対応にしない
- Debug / Validation を最後まで放置しない
```

### 1.3 完成の定義

```text
- ParticleUnlit 基盤が実用可能
- Dust / Smoke / Glow / Spark / Magic の Prefab が存在する
- MaskMap / Noise / Dissolve / Emission が機能する
- Flipbook / Custom Data / Depth / Lit / Distortion の拡張方針が実装上破綻しない
- WebGL で最低限表示できる
- Sample Scene で全体を検証できる
- 将来 ParticleLit / ParticleDistortion / Ring / Ground へ拡張可能
```

---

## M0: Particle Material System 基盤仕様確定

### 目的

Particle Material System 全体の設計方針を固定する。ここでは実装よりも、後から作り替えになりやすい設計要素を先に確定する。

### 対象

```text
- Shader Family 分類
- Property 命名規則
- Texture Packing 仕様
- Custom Data Layout
- Vertex Stream 方針
- Material 命名規則
- Prefab 命名規則
- Folder 構成
- Quality Tier 方針
- Future Extension 方針
```

### 作業内容

```text
- ParticleMaterialSystemSpec.md を確定する
- ParticleUnlit / ParticleLit / ParticleDistortion の責務を確定する
- TrailUnlit と分離する方針を明記する
- MaskMap RGBA の意味を固定する
- Custom1 / Custom2 のスロット意味を固定する
- 初期実装範囲を ParticleUnlit に限定する
- 後続拡張を仕様上予約する
```

### 完了条件

```text
- ParticleUnlit / ParticleLit / ParticleDistortion の役割が明確
- 初期実装対象が ParticleUnlit であることが明確
- MaskMap Layout が確定している
- Custom Data Layout が確定している
- Shader を乱立しない方針が明確
- WebGL を意識した制限が明記されている
```

### 成果物

```text
Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemSpec.md
Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemMilestones.md
Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemProgress.md
```

---

## M1: Folder / Naming / Sample Scene 基盤作成

### 目的

Particle Material System を実装するためのプロジェクト構造を先に整える。ここを曖昧にすると、Material、Texture、Prefab、validation code が散らかる。

### 現行 repo に合わせた作成フォルダ

この repo では `Assets/Game/Effects/...` は使わず、既存の TrailUnlit と EnvironmentStylizedLit の配置規約に合わせる。

```text
Assets/
  Art/
    Shader/
      Particles/
        ParticleUnlit/
          HLSL/
          Editor/
        ParticleLit/
          HLSL/
          Editor/
        ParticleDistortion/
          HLSL/
          Editor/
        ParticleRingUnlit/
        ParticleGroundUnlit/

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

  Scenes/
    Particles/
      ParticleMaterialTestScene.unity

  Tests/
    EditMode/
      Particles/
        ParticleUnlitValidationTests.cs

  Docs/
    ParticleUnitSpec/
      ParticleMaterialSystemSpec.md
      ParticleMaterialSystemMilestones.md
```

### 補足

```text
- Prefabs ではなく、既存資産に合わせて Prefab フォルダを使う
- Samples/Scenes ではなく、既存 scene 規約に合わせて Assets/Scenes 配下へ置く
- validation test も最初から Assets/Tests/EditMode/Particles に受け皿を作る
```

### 作成する Scene

```text
Assets/Scenes/Particles/ParticleMaterialTestScene.unity
```

初期段階では空 Scene でよい。ただし、以下の検証アンカー名を先に固定する。

```text
- Dust Test Area
- Smoke Test Area
- Glow Test Area
- Spark Test Area
- Magic Test Area
- WebGL Load Test Area
- Future Lit Test Area
- Future Distortion Test Area
```

### 完了条件

```text
- Folder 構成が仕様通り作成されている
- Sample Scene が存在する
- Material / Texture / Prefab / Test の配置先が明確
- 命名規則が Spec と矛盾していない
```

### 成果物

```text
Assets/Art/Shader/Particles/ParticleUnlit/
Assets/Art/Shader/Particles/ParticleLit/
Assets/Art/Shader/Particles/ParticleDistortion/
Assets/Art/Materials/Particles/Unlit/
Assets/Art/Textures/Particles/Unlit/
Assets/Art/Textures/Particles/Shared/
Assets/Art/Prefab/Particles/Unlit/
Assets/Scenes/Particles/ParticleMaterialTestScene.unity
Assets/Tests/EditMode/Particles/ParticleUnlitValidationTests.cs
```

---

## M2: ParticleUnlit 最小 Shader 実装

### 目的

Particle System Renderer で表示可能な、最小限の `ParticleUnlit` Shader を作成する。この Milestone ではまだ Noise や Dissolve は不要で、正しく表示され、Particle Color / Alpha が反映されることを最優先する。

### 実装対象

```text
Shader:
BC/Particles/ParticleUnlit
```

### 実装方式

元案の Shader Graph ではなく、既存の [Assets/Art/Shader/Particles/TrailUnlit/BC_Particles_TrailUnlit.shader](Assets/Art/Shader/Particles/TrailUnlit/BC_Particles_TrailUnlit.shader) と同系統の手書き `.shader + HLSL` 構成を採用する。

```text
Assets/Art/Shader/Particles/ParticleUnlit/BC_Particles_ParticleUnlit.shader
Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Input.hlsl
Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Common.hlsl
Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Sampling.hlsl
Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Surface.hlsl
Assets/Art/Shader/Particles/ParticleUnlit/HLSL/Passes/ParticleUnlit_ForwardPass.hlsl
```

### 必須機能

```text
- Transparent
- ZWrite Off
- ZTest LEqual
- Cull Off
- BaseMap
- BaseColor
- Alpha
- Brightness
- Vertex Color
- Particle Alpha
- Soft Circle
```

### Properties

```csharp
_BaseMap
_BaseColor
_Alpha
_Brightness
_UseVertexColor
_SoftCircleStrength
_EdgeFadePower
_EdgeFadeStrength
```

### 基本合成

```text
FinalRGB =
  BaseMap.rgb
  * BaseColor.rgb
  * VertexColor.rgb
  * Brightness

FinalAlpha =
  BaseMap.a
  * BaseColor.a
  * VertexColor.a
  * Alpha
  * SoftCircle
```

### 検証項目

```text
- Particle System Renderer に Material を設定して表示できる
- BaseMap が表示される
- BaseColor が反映される
- Start Color が反映される
- Color over Lifetime が反映される
- Alpha over Lifetime が反映される
- Soft Circle で四角い Billboard 感が軽減される
```

### 完了条件

```text
- 最小 ParticleUnlit Material で粒子が表示される
- Particle 側の Color / Alpha が効く
- Soft Circle が効く
- Shader Compile Error がない
```

### 成果物

```text
Assets/Art/Shader/Particles/ParticleUnlit/BC_Particles_ParticleUnlit.shader
Assets/Art/Shader/Particles/ParticleUnlit/HLSL/...
Assets/Art/Materials/Particles/Unlit/M_Particle_Test_Alpha.mat
```

---

## M3: Blend Mode / Material Preset 基盤

### 目的

ParticleUnlit で主要 Blend Mode を使い分けられるようにする。Particle 表現では Blend Mode が見た目を大きく左右するため、早い段階で契約を固定する。

### 対応 Blend Mode

```text
Alpha
Additive
Premultiply
```

`Multiply` は後続対応でよい。

### repo 流儀で同時に入れるもの

TrailUnlit と同様に、Blend state は keyword ではなく Material の hidden render-state property で管理する。そのため、この Milestone では Material preset だけでなく以下も同時に揃える。

```text
- ParticleUnlitShaderGUI.cs
- ParticleUnlitMaterialValidator.cs
- ParticleUnlitPresetUtility.cs
```

### 主要 Property

```csharp
_BlendMode
_SrcBlend
_DstBlend
_ZWrite
_ColorMask
_QueueOffset
```

### 作成 Material

```text
M_Particle_Dust_Alpha
M_Particle_Glow_Premultiply
M_Particle_Spark_Additive
```

### 用途

```text
Alpha:
  Dust / Smoke / Mist / Snow

Premultiply:
  Glow / Soft Light / Bloom なし光粒

Additive:
  Spark / Magic / Energy / 強い発光
```

### 検証項目

```text
- Alpha で埃が自然に見える
- Premultiply で柔らかい光粒が見える
- Additive で火花が明るく見える
- Additive が白飛びしすぎない
- Material 切り替えで Shader が破綻しない
- _BlendMode と _SrcBlend / _DstBlend / renderQueue が同期する
```

### 完了条件

```text
- Alpha / Additive / Premultiply の Material が存在する
- 各 Blend Mode が Particle System 上で確認できる
- 同じ Shader から用途別 Material を作れる
- ShaderGUI / Validator で状態同期ができる
```

### 成果物

```text
Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitShaderGUI.cs
Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitMaterialValidator.cs
Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitPresetUtility.cs
Assets/Art/Materials/Particles/Unlit/M_Particle_Dust_Alpha.mat
Assets/Art/Materials/Particles/Unlit/M_Particle_Glow_Premultiply.mat
Assets/Art/Materials/Particles/Unlit/M_Particle_Spark_Additive.mat
```

---

## M4: MaskMap / Noise / Dissolve 実装

### 目的

ParticleUnlit に粒子の不均一さ、欠け、消滅表現を追加する。将来拡張に耐えるために `_MaskMap` 仕様を実装へ反映する。

### 追加機能

```text
- MaskMap
- NoiseMap
- NoiseStrength
- NoiseScale
- NoiseScrollSpeed
- DissolveAmount
- DissolveSoftness
```

### MaskMap Layout

```text
R = Dissolve / Erosion Mask
G = Emission Mask
B = Variation / Noise Influence
A = Secondary Alpha / Shape Mask
```

### Properties

```csharp
_MaskMap
_MaskStrength

_NoiseMap
_NoiseStrength
_NoiseScale
_NoiseScrollSpeed
_NoiseSpace

_DissolveAmount
_DissolveSoftness
```

### 初期 NoiseSpace

```text
ParticleUV
```

World / Screen 系は後続対応。

### 作成 Material

```text
M_Particle_Smoke_Alpha
M_Particle_Magic_Additive
```

### 検証項目

```text
- Noise が Alpha に影響する
- Dissolve で粒子が欠ける
- MaskMap.r が Dissolve に使われる
- MaskMap.a が Alpha 補助として使える
- Smoke が単純な丸に見えにくくなる
- Magic 粒子が Dissolve で動きを持つ
```

### 完了条件

```text
- MaskMap が機能する
- NoiseMap が機能する
- Dissolve が機能する
- Smoke / Magic Material が作成されている
```

### 成果物

```text
Assets/Art/Materials/Particles/Unlit/M_Particle_Smoke_Alpha.mat
Assets/Art/Materials/Particles/Unlit/M_Particle_Magic_Additive.mat
Assets/Art/Textures/Particles/Shared/T_Noise_SoftCloud.png
Assets/Art/Textures/Particles/Shared/T_Noise_Dissolve.png
Assets/Art/Textures/Particles/Shared/T_Mask_Particle_Test_RGBA.png
```

---

## M5: Emission / Glow / Spark 表現強化

### 目的

発光系 Particle の基盤を整える。Glow、Spark、Magic は頻出するため、Bloom なしでも最低限見える発光制御を作る。

### 追加機能

```text
- EmissionColor
- EmissionStrength
- EmissionAlphaInfluence
- MaskMap.g による Emission Mask
```

### Properties

```csharp
_EmissionColor
_EmissionStrength
_EmissionAlphaInfluence
```

### 対象 Material

```text
M_Particle_Glow_Premultiply
M_Particle_Spark_Additive
M_Particle_Magic_Additive
```

### 検証項目

```text
- Glow が柔らかく表示される
- Spark が短寿命で明るく表示される
- Magic が Dissolve + Emission で表現できる
- EmissionStrength で明るさ調整できる
- Bloom なしでも見える
- Bloom ありでも白飛びしすぎない
```

### 完了条件

```text
- Emission が Material から制御できる
- MaskMap.g が Emission Mask として機能する
- Glow / Spark / Magic の基本表現が成立する
```

---

## M6: 初期 Particle Prefab 整備

### 目的

Material を実際に使える Particle Prefab として整備する。Particle 表現は Material 単体では成立しないため、Particle System 設定とセットで管理する。

### 作成 Prefab

```text
FX_Particle_Dust
FX_Particle_Smoke
FX_Particle_Glow
FX_Particle_Spark
FX_Particle_Magic
```

### 配置先

```text
Assets/Art/Prefab/Particles/Unlit/
```

### Prefab 共通構成

```text
FX_Particle_xxx
  - Particle System
  - Particle System Renderer
```

### Prefab ごとの基準

FX_Particle_Dust:

```text
Material:
M_Particle_Dust_Alpha

Renderer:
Billboard

Simulation Space:
World

Start Lifetime:
4-12

Start Speed:
0.01-0.15

Start Size:
0.005-0.04

Max Particles:
100-300
```

FX_Particle_Smoke:

```text
Material:
M_Particle_Smoke_Alpha

Renderer:
Billboard

Start Lifetime:
1-5

Start Speed:
0.1-1.0

Start Size:
0.2-2.0

Size over Lifetime:
拡大

Color over Lifetime:
徐々に消える
```

FX_Particle_Glow:

```text
Material:
M_Particle_Glow_Premultiply

Renderer:
Billboard

Start Lifetime:
1-4

Start Speed:
0-0.5

Start Size:
0.05-0.5
```

FX_Particle_Spark:

```text
Material:
M_Particle_Spark_Additive

Renderer:
Billboard or Stretched Billboard

Start Lifetime:
0.1-0.8

Start Speed:
2-10

Start Size:
0.01-0.08
```

FX_Particle_Magic:

```text
Material:
M_Particle_Magic_Additive

Renderer:
Billboard

Start Lifetime:
0.5-3

Start Speed:
0.2-2.0

Start Size:
0.05-0.4
```

### 完了条件

```text
- 5 種類の Prefab が存在する
- Scene に置くだけで表示できる
- Material と Particle System 設定が用途に合っている
- Sample Scene で比較できる
```

---

## M7: WebGL / Lightweight 検証

### 目的

初期 ParticleUnlit 基盤が WebGL で破綻しないことを確認する。WebGL 対応を後付けにすると、後で Shader 機能を削る作業が発生しやすい。

### 検証対象

```text
FX_Particle_Dust
FX_Particle_Smoke
FX_Particle_Glow
FX_Particle_Spark
FX_Particle_Magic
```

### 検証条件

```text
- WebGL Build で Shader Compile Error が出ない
- Dust 100 particles
- Dust 300 particles
- Smoke 50 particles
- Spark 200 particles
- Glow 100 particles
- Magic 100 particles
- 複数 Prefab 同時表示
```

### repo 流儀で行う補助検証

TrailUnlit と同様に、source audit と generated asset validation を入れる。

```text
- ParticleUnlitValidationBootstrapper で検証資産を再生成できる
- EditMode test で不要な shader_feature を監査する
- Depth / Opaque Texture が初期実装へ紛れ込んでいないことを確認する
```

### 完了条件

```text
- WebGL Build で初期 Prefab が表示される
- Shader Error がない
- 最低限の負荷目安が確認できている
- WebGL で重い機能が明確になっている
```

---

## M8: Debug View / Inspector 整理

### 目的

Particle Shader の調整とトラブルシュートをしやすくする。表示されない、Alpha が効かない、Dissolve で消えた、Vertex Color が 0 などの原因切り分けを早くする。

### DebugMode

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
```

Custom Data は M10 以降で追加する。

### Inspector 表示順

```text
1. Rendering
2. Base
3. Shape
4. Mask
5. Noise
6. Dissolve
7. Emission
8. Debug
9. Optional
```

### 完了条件

```text
- DebugMode で主要中間値を確認できる
- Material Inspector の項目順が整理されている
- 調整時に原因切り分けが可能
```

---

## M9: Flipbook / Atlas 基盤

### 目的

Smoke、Fire、Explosion、Magic Burst など、アニメーション粒子の基盤を作る。この Milestone では本格的な高度補間までは不要で、まずは Unity Particle System の Texture Sheet Animation と整合する構成を作る。

### 対象

```text
- Smoke Flipbook
- Magic Burst Flipbook
- Explosion placeholder
```

### 実装内容

```text
- Flipbook Texture Import 規則
- Texture Sheet Animation 対応確認
- Random Start Frame 検証
- Frame over Time 検証
- Flipbook 用 Material 作成
```

### 作成 Material

```text
M_Particle_SmokeFlipbook_Alpha
M_Particle_MagicBurst_Additive
```

### 将来予約

```text
- UV2
- AnimBlend
- Flipbook Blend
- Variant Atlas
- Custom1.w Variant Index
```

### 完了条件

```text
- Smoke Flipbook が再生できる
- 粒子ごとに開始フレームを変えられる
- 静止 Smoke より自然に見える
- Flipbook 用 Texture 規約が明確
```

---

## M10: Custom Data / Vertex Stream 基盤

### 目的

Particle System 側から Shader の見た目を粒子ごとに制御できるようにする。これは後から入れると作り替えになりやすいため、基盤として重要である。

### Custom Data Layout

```text
Custom1.x = Dissolve Amount Override
Custom1.y = Emission Strength Override
Custom1.z = Noise Offset / Random01
Custom1.w = Variant Index

Custom2.x = Distortion Strength Override
Custom2.y = Fake Light / Rim Influence
Custom2.z = Flow Strength
Custom2.w = Reserved
```

### 初期実装対象

まずは `ParticleUnlit` で以下のみ対応する。

```text
Custom1.x:
  Dissolve Override

Custom1.y:
  Emission Override

Custom1.z:
  Noise Offset
```

### 必要 Vertex Streams

```text
Position
Color
UV
Custom1.xyzw
```

### 検証項目

```text
- Custom1.x で Dissolve が変わる
- Custom1.y で Emission が変わる
- Custom1.z で Noise Offset が変わる
- Custom Data 未設定でも破綻しない
```

### 完了条件

```text
- Custom Data で粒子ごとの差分が作れる
- Magic / Spark で表現差を出せる
- 既存 Material が壊れない
```

---

## M11: Depth Interaction 基盤

### 目的

3D 空間で Particle が壁・床・カメラと交差した時の破綻を減らす。

### 対象機能

```text
- Soft Particles
- Camera Fade
```

以下は将来候補。

```text
- Depth Fade
- Intersection Highlight
- Ground Contact Fade
```

### Properties

```csharp
_UseSoftParticles
_SoftParticleDistance

_UseCameraFade
_CameraFadeNear
_CameraFadeFar
```

### 対象 Prefab

```text
FX_Particle_Smoke
FX_Particle_Dust
FX_Particle_Glow
```

### 注意点

```text
- Depth Texture が必要になる可能性がある
- WebGL では Optional 扱い
- 初期 Prefab では OFF を標準にしてよい
- ON / OFF で見た目が破綻しないようにする
```

### 完了条件

```text
- Soft Particle ON で壁・床との交差が目立ちにくくなる
- Camera Fade ON でカメラ近接時の巨大板感が減る
- OFF でも従来通り表示できる
```

---

## M12: ParticleLit 設計・最小実装

### 目的

ライト影響を受ける物理的な粒子用 Shader を作成する。ただし ParticleLit は初期基盤ではなく後続拡張であり、この Milestone に入る前に ParticleUnlit が安定している必要がある。

### 対象

```text
- 雨粒
- 泡
- 葉っぱ
- 破片
- Mesh Particle
```

### 実装対象

```text
Shader:
BC/Particles/ParticleLit
```

### 必須機能

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
- Emission
```

### Optional

```text
- Rim Light
- Alpha Clip
- Dissolve
```

### 作成 Material

```text
M_Particle_Raindrop_Lit
M_Particle_Bubble_Lit
M_Particle_Debris_Lit
```

### Renderer Mode

```text
Billboard:
  Raindrop / Bubble で検証

Mesh:
  Debris で検証
```

### 完了条件

```text
- ParticleLit Material がライト影響を受ける
- NormalMap が機能する
- Mesh Particle に適用できる
- ParticleUnlit と責務が混ざっていない
```

### 成果物

```text
Assets/Art/Shader/Particles/ParticleLit/BC_Particles_ParticleLit.shader
Assets/Art/Materials/Particles/Lit/M_Particle_Raindrop_Lit.mat
Assets/Art/Materials/Particles/Lit/M_Particle_Bubble_Lit.mat
Assets/Art/Materials/Particles/Lit/M_Particle_Debris_Lit.mat
```

---

## M13: ParticleDistortion 設計・最小実装

### 目的

熱気・空気の揺らぎ・魔法歪みなど、背景歪み Particle の基盤を作る。Distortion は高負荷で URP 設定依存が強いため、通常 Particle と分離する。

### 実装対象

```text
Shader:
BC/Particles/ParticleDistortion
```

### 必須機能

```text
- DistortionMap
- DistortionStrength
- DistortionScale
- DistortionScrollSpeed
- Alpha
- Edge Fade
- Noise optional
```

### 作成 Material

```text
M_Particle_HeatHaze_Distortion
M_Particle_AirWarp_Distortion
M_Particle_MagicWarp_Distortion
```

### 作成 Prefab

```text
FX_Particle_HeatHaze
FX_Particle_AirWarp
FX_Particle_MagicWarp
```

### 注意点

```text
- Opaque Texture / Camera Color Texture 依存を明記する
- WebGL では標準使用しない
- 大量発生禁止
- 画面全体を覆う歪みは禁止
```

### 完了条件

```text
- HeatHaze が背景を歪ませる
- MagicWarp が短時間 Effect として使える
- Distortion を ParticleUnlit に混ぜていない
- 負荷上の注意が明文化されている
```

---

## M14: Ring / Ground 系拡張設計

### 目的

通常 Billboard では表現しにくい Ring / Ground 系 Particle の拡張方針を決める。この Milestone では実装よりも設計分離を重視する。

### Ring 系対象

```text
- Shockwave
- Magic Circle
- Water Ripple
- Landing Effect
- Explosion Ring
```

### Ground 系対象

```text
- Ground Smoke
- Floor Mist
- Dust Cloud
- Magic Ground Aura
- 地面を這う霧
```

### 候補 Shader

```text
BC/Particles/ParticleRingUnlit
BC/Particles/ParticleGroundUnlit
```

### 必要機能候補

Ring:

```text
- Ring Mask
- Radial Dissolve
- Polar UV
- Edge Emission
```

Ground:

```text
- Horizontal Billboard
- World Space UV
- Height Fade
- Ground Contact Fade
- Directional Noise
```

### 完了条件

```text
- Ring / Ground を ParticleUnlit 本体へ無理に詰め込まない方針が明確
- 専用 Shader 化する条件が明確
- 将来作る Prefab 候補が明確
```

---

## M15: Quality Tier / Shader 分離方針

### 目的

WebGL / Mobile / PC 高品質で無理なく使い分けられる品質階層を設計する。

### Quality Tier

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

### 将来 Shader 候補

```text
BC/Particles/ParticleUnlitLite
BC/Particles/ParticleUnlit
BC/Particles/ParticleUnlitAdvanced
```

### 判断基準

```text
- Shader 内分岐で済むか
- Keyword Variant が増えすぎないか
- WebGL で不要処理を削れるか
- Material 数が増えすぎないか
- Prefab 管理が破綻しないか
```

### 完了条件

```text
- WebGL 用軽量版の方針が決まっている
- 高品質版の方針が決まっている
- どの機能をどの Tier に入れるか明確
- Shader 分離が必要になる条件が明確
```

---

## M16: Optimization / Validation / Documentation

### 目的

Particle Material System を実運用できる状態に仕上げる。最後にまとめて整えるのではなく、M1-M15 で積み上げたものを検証・整理する。

### 最適化項目

```text
- 不要 Property 削除
- 不要 Keyword 削除
- Texture Sample 数確認
- MaskMap 使用状況確認
- Material 重複整理
- Prefab 設定統一
- WebGL 負荷確認
- Overdraw 確認
- HLSL 分割の責務整理
```

### Validation Scene

```text
Assets/Scenes/Particles/ParticleMaterialTestScene.unity
```

配置するもの:

```text
- Dust
- Smoke
- Glow
- Spark
- Magic
- Smoke Flipbook
- Custom Data Magic
- Soft Particle Smoke
- Lit Bubble
- Lit Debris
- HeatHaze
- MagicWarp
- Ring placeholder
- GroundSmoke placeholder
```

### 検証条件

```text
- Editor
- PlayMode
- WebGL Build
- Bloom ON
- Bloom OFF
- 明るい背景
- 暗い背景
- 複数 Particle 同時表示
- 低粒子数
- 高粒子数
```

### Documentation

作成するドキュメント:

```text
Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemSpec.md
Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemMilestones.md
Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemProgress.md
Assets/Docs/ParticleUnitSpec/ParticleMaterialUsageGuide.md
Assets/Docs/ParticleUnitSpec/ParticleMaterialPresetGuide.md
Assets/Docs/ParticleUnitSpec/ParticleMaterialPerformanceGuide.md
```

### 完了条件

```text
- Sample Scene で主要 Effect を確認できる
- WebGL で初期 Prefab が表示できる
- Material / Prefab / Texture の命名が整理されている
- 仕様書と実装が矛盾していない
- 次に ParticleLit / ParticleDistortion / Ring / Ground へ進める状態
```

---

## 2. 推奨実装順

実際に作業する順番は以下で固定してよい。

```text
1. M0  基盤仕様確定
2. M1  Folder / Sample Scene
3. M2  ParticleUnlit 最小 Shader
4. M3  Blend Mode
5. M4  Mask / Noise / Dissolve
6. M5  Emission
7. M6  Prefab 化
8. M7  WebGL 検証
9. M8  Debug View
10. M9  Flipbook
11. M10 Custom Data
12. M11 Depth Interaction
13. M12 ParticleLit
14. M13 ParticleDistortion
15. M14 Ring / Ground 設計
16. M15 Quality Tier
17. M16 Optimization / Documentation
```

---

## 3. 最初に着手すべき実装単位

最初に実装するべき最小セットはこれである。

```text
M1:
- Folder 作成
- Sample Scene 作成
- Validation test の受け皿作成

M2:
- ParticleUnlit Shader 作成
- BaseMap
- BaseColor
- Alpha
- VertexColor
- SoftCircle

M3:
- Dust Alpha
- Glow Premultiply
- Spark Additive
- ShaderGUI / Validator / PresetUtility の最小実装
```

ここまでできれば、最低限の Particle Material 基盤として成立する。

---

## 4. 実装を急がない方がいいもの

以下は魅力的だが、初期で触ると基盤が散らかる。

```text
- ParticleLit
- ParticleDistortion
- Flow Map
- Ring Shader
- Ground Shader
- Custom Data の全対応
- Flipbook Blend
- Advanced Debug
- WebGL 用 Lite Shader 分離
```

これらは仕様上は予約するが、初期実装では後回しが正解である。

---

## 5. 厳しめの判断

この Milestone で一番重要なのは、`ParticleUnlit` を早く作ることではない。正確には、将来拡張で壊れない `ParticleUnlit` を作ることである。

特に以下は最初に固定する。

```text
- MaskMap の RGBA 割り当て
- Custom1 / Custom2 の意味
- Property 名
- Folder 構成
- Material 命名
- Prefab 命名
- Trail と Particle 本体の分離
- Lit / Distortion を別 Shader にする方針
- Blend state を hidden property で同期する方針
- Bootstrapper / EditMode validation を入れる方針
```

ここを曖昧にしたまま実装すると、後で高確率で作り直しになる。逆に、ここを固定しておけば最初の実装は小さくて構わない。

最初の完成形はこれで十分である。

```text
BC/Particles/ParticleUnlit

M_Particle_Dust_Alpha
M_Particle_Smoke_Alpha
M_Particle_Glow_Premultiply
M_Particle_Spark_Additive
M_Particle_Magic_Additive

FX_Particle_Dust
FX_Particle_Smoke
FX_Particle_Glow
FX_Particle_Spark
FX_Particle_Magic
```

その後に、

```text
Flipbook
Custom Data
Depth Interaction
ParticleLit
ParticleDistortion
Ring
Ground
Quality Tier
```

へ進めるのが、一番破綻しにくい順番である。
