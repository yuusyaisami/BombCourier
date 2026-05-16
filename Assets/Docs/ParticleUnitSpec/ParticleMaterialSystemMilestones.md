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
- 現在の実装到達点: M12 ParticleLit 最小実装の code path まで追加済み
- 既存の参照実装: TrailUnlit 系のみ
- ParticleUnlit の最小 shader source は追加済み
- M1 bootstrapper と scaffold test は追加済み
- M2 bootstrapper と validation test は追加済み
- bootstrapper の scene 復元漏れは修正済み
- generated material / texture の contract test は強化済み
- M3 ShaderGUI / MaterialValidator / PresetUtility は追加済み
- M3 bootstrapper と validation test 拡張は追加済み
- M4 MaskMap / Noise / Dissolve の shader / bootstrapper / validation test 拡張は追加済み
- M5 Emission / Glow / Spark の shader / bootstrapper / validation test 拡張は追加済み
- M6 prefab / preview 再生成と prefab source link validation は追加済み
- M7 WebGL load-test case 再生成、EditMode contract、WebGL build utility は追加済み
- M8 DebugMode 0-12、ShaderGUI section 再編、debug warning、debug source audit は追加済み
- ParticleLit の `.shader + HLSL` 最小 lit 実装、ShaderGUI / MaterialValidator / PresetUtility、M12 bootstrapper 拡張、EditMode contract test は追加済み
- ParticleDistortion の `.shader + HLSL` 最小 distortion 実装、ShaderGUI / MaterialValidator / PresetUtility、M13 bootstrapper 拡張、EditMode contract test は追加済み
```

### 現在 repo に存在する参照資産

```text
Assets/Docs/ParticleTrailMaterialSpec.md
Assets/Art/Shader/Particles/TrailUnlit/
Assets/Art/Shader/Particles/ParticleUnlit/BC_Particles_ParticleUnlit.shader
Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Debug.hlsl
Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs
Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitWebGlBuildUtility.cs
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
| M3 Blend Mode / Material Preset | In Progress | 80% | ParticleUnlit の ShaderGUI / MaterialValidator / PresetUtility、blend state 契約、M3 bootstrapper、validation test 拡張は追加済み。generated material / scene anchor の Unity 実生成確認だけが未完了。 |
| M4 MaskMap / Noise / Dissolve | In Progress | 80% | ParticleUnlit に MaskMap / Noise / Dissolve property と surface logic、M4 bootstrapper、shared noise / mask texture 生成、Smoke / Magic material 生成、validation test 拡張まで追加済み。Unity editor 上での実生成確認と Test Runner 実行が未完了。 |
| M5 Emission / Glow / Spark | In Progress | 65% | ParticleUnlit に EmissionColor / EmissionStrength / EmissionAlphaInfluence と MaskMap.g による emission mask を追加済み。Glow / Spark / Magic preset、M5 bootstrapper、validation test 拡張も追加済み。Unity editor 上での実生成確認と Test Runner 実行が未完了。 |
| M6 初期 Prefab 整備 | In Progress | 72% | 5 prefab 生成、preview scene 再生成、prefab instance 化、prefab source link validation まで追加済み。Unity editor 上での実生成確認と Test Runner 実行が未完了。 |
| M7 WebGL / Lightweight 検証 | In Progress | 68% | BootstrapM7WebGlValidationAssets、WebGL Load Test Area の drift cleanup、EditMode contract test、ParticleUnlitWebGlBuildUtility、ParticleUnlitBuildValidator / prebuild validator は追加済み。Unity editor 上での bootstrap 実行、Unity Test Runner、実 WebGL build 実行が未完了。 |
| M8 Debug View / Inspector 整理 | In Progress | 69% | `_DebugMode` 0-12、ParticleUnlit_Debug.hlsl、ShaderGUI の section 再編、debug authoring warning、BootstrapM8DebugValidationAssets、non-development build 向け debug guard、EditMode contract 拡張は追加済み。Unity editor 上での debug view 目視確認と Unity Test Runner 実行が未完了。 |
| M9 Flipbook / Atlas 基盤 | In Progress | 72% | 4x4 flipbook atlas 生成、M_Particle_SmokeFlipbook_Alpha / M_Particle_MagicBurst_Additive、Texture Sheet Animation prefab 契約、flipbook preview、tile bleed を抑える atlas padding、Smoke/Magic marker の drift cleanup、EnsureM9ValidationAssetsReady、EditMode contract 拡張は追加済み。Unity editor 上の再生確認と Unity Test Runner / WebGL build 実行が未完了。 |
| M10-M11 | In Progress | 72% | M10 / M11 の code path、bootstrapper、EditMode contract、narrow compile は追加済み。Unity editor 上の bootstrap / visual / Test Runner / WebGL 実行が未完了。 |
| M12 ParticleLit 設計・最小実装 | In Progress | 62% | ParticleLit shader / HLSL、editor authoring surface、M12 bootstrapper、Future Lit Test Area validation anchor、EditMode contract test、narrow compile は追加済み。Unity editor 上の bootstrap 実行、目視確認、Unity Test Runner 実行が未完了。 |
| M13-M16 | In Progress | 15% | M13 ParticleDistortion の code path、generated asset bootstrapper、EditMode contract は追加済み。M14-M16 は未着手。 |

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

### 実装方針

```text
- ParticleUnlitValidationBootstrapper.BootstrapM6PrefabAssets で 5 prefab を再生成する
- 既存の M5 material を prefab の唯一の参照元として使う
- ParticleMaterialTestScene.unity に prefab preview object を追加し、material validation anchor とは責務分離する
- EditMode test で prefab asset と scene preview の両方を監査する
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

Bootstrap baseline:
Lifetime 8.0 / Speed 0.08 / Size 0.03 / Max 180 / EmissionRate 22
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

Bootstrap baseline:
Lifetime 3.4 / Speed 0.42 / Size 0.85 / Max 96 / EmissionRate 10
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

Bootstrap baseline:
Lifetime 2.2 / Speed 0.12 / Size 0.24 / Max 72 / EmissionRate 7
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

Bootstrap baseline:
Renderer = Stretched Billboard
Lifetime 0.35 / Speed 6.0 / Size 0.04 / Max 180 / EmissionRate 36
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

Bootstrap baseline:
Lifetime 1.6 / Speed 0.45 / Size 0.18 / Max 84 / EmissionRate 11
Color/Size over Lifetime を有効化
```

### Scene 比較

```text
- 新規 scene は作らず Assets/Scenes/Particles/ParticleMaterialTestScene.unity を継続利用する
- Dust / Smoke / Glow / Spark / Magic Test Area の各 marker 配下に preview object を追加する
- preview 名は FX_Particle_Dust_Preview など prefab 名ベースで固定する
- 既存の ParticleUnlit_* validation anchor は M2-M5 回帰監査のため残す
```

### 完了条件

```text
- 5 種類の Prefab が存在する
- Scene に置くだけで表示できる
- Material と Particle System 設定が用途に合っている
- Sample Scene で比較できる
- BootstrapM6PrefabAssets を連続実行しても prefab / preview が重複しない
- ParticleUnlitValidationTests で prefab asset と scene preview の契約が監査される
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

### 現時点の repo 実装

```text
- ParticleUnlitValidationBootstrapper.BootstrapM7WebGlValidationAssets を追加済み
- WebGL Load Test Area に Dust100 / Dust300 / Smoke50 / Spark200 / Glow100 / Magic100 / Mixed を prefab instance ベースで再生成し、未知の child を残さない
- ParticleUnlitValidationTests を M7 setup と WebGL load-test contract まで拡張済み
- ParticleUnlitWebGlBuildUtility から validation scene 単体の WebGL build を実行できる
- ParticleUnlitBuildValidator / ParticleUnlitWebGlBuildPreprocessor で WebGL build 前に M7 validation asset を強制できる
- 実 WebGL build 成否と editor 上の見た目確認は未検証
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

### 現時点の repo 実装

```text
- BC_Particles_ParticleUnlit.shader に `_DebugMode` を追加済み
- ParticleUnlit_Debug.hlsl を追加し、0-12 の debug output を既存 sampled/intermediate values から返す
- ParticleUnlitShaderGUI を Rendering / Base / Shape / Mask / Noise / Dissolve / Emission / Debug / Optional 順へ再編済み
- ParticleUnlitMaterialValidator に `_DebugMode` clamp と debug authoring warning を追加済み
- ParticleUnlitPresetUtility と ParticleUnlitValidationBootstrapper で generated materials の debug default-off を固定済み
- ParticleUnlitBuildValidator に non-development build 向け active debug material guard を追加済み
- ParticleUnlitValidationTests を M8 source audit / ShaderGUI order / debug default-off 契約まで拡張済み
- Unity editor 上の debug view 目視確認と Unity Test Runner 実行は未完了
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

### 現時点の repo 実装

```text
- ParticleUnlit shader / HLSL は UV0 sampling の lean path を維持し、M9 では `_FlipbookRows` などの material property を追加していない
- ParticleUnlitValidationBootstrapper に `BootstrapM9FlipbookValidationAssets` を追加し、4x4 の Smoke / Magic Burst / Explosion placeholder atlas を生成する
- M_Particle_SmokeFlipbook_Alpha と M_Particle_MagicBurst_Additive を追加し、既存 ParticleUnlit preset / mask / noise 契約の上で flipbook 用 BaseMap へ差し替え済み
- FX_Particle_SmokeFlipbook と FX_Particle_MagicBurst prefab を追加し、Texture Sheet Animation の Whole Sheet / Random Start Frame / Frame over Time 契約を固定済み
- ParticleMaterialTestScene に Smoke / Magic marker 配下の flipbook preview prefab instance を追加し、marker child drift cleanup も追加済み
- generated flipbook atlas は tile bleed を抑える safe padding を含むよう修正済み
- ParticleUnlitBuildValidator / ParticleUnlitWebGlBuildUtility を EnsureM9ValidationAssetsReady 経由へ更新済み
- ParticleUnlitValidationTests を M9 source audit / atlas importer / material / prefab / scene preview / padding / marker child set 契約まで拡張済み
- Unity editor 上の flipbook 再生確認、Unity Test Runner、実 WebGL build 実行は未完了
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

### repo 実装

```text
- ParticleUnlit_ForwardPass.hlsl で Custom1.xyzw を TEXCOORD1 として vertex/fragment 間で routing するよう更新済み
- ParticleUnlit_Surface.hlsl で Custom1.x を Dissolve Amount additive delta、Custom1.y を Emission Strength additive delta、Custom1.z を Noise UV offset として適用済み
- ParticleUnlit_Debug.hlsl で Debug 13=Custom1、15=UV を実装済み
- ParticleUnlitValidationBootstrapper.cs に BootstrapM10CustomDataValidationAssets、FX_Particle_SparkCustomData / FX_Particle_MagicCustomData prefab、scene preview 再生成、CustomDataModule 設定、Custom1XYZW vertex stream 設定を追加済み
- ResetValidationRenderer で default vertex streams を Position / Color / UV に戻し、M10 prefab だけ Custom1XYZW を opt-in するよう固定済み
- ParticleUnlitValidationTests.cs を M10 source audit / custom-data prefab / active vertex stream / preview contract まで拡張済み
- ParticleUnlitBuildValidator / ParticleUnlitWebGlBuildUtility を EnsureM10ValidationAssetsReady / RunM10WebGlValidationBuild 経由へ更新済み
- Unity editor 上の custom-data 再生確認、Unity Test Runner、実 WebGL build 実行は未完了
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

### Repo 実装メモ

```text
- BC_Particles_ParticleUnlit.shader / ParticleUnlit_Input.hlsl に _UseSoftParticles / _SoftParticleDistance / _UseCameraFade / _CameraFadeNear / _CameraFadeFar を追加
- ParticleUnlit_Common.hlsl で DeclareDepthTexture.hlsl, SampleSceneDepth, LinearEyeDepth を使う Soft Particles / Camera Fade helper を追加
- ParticleUnlit_ForwardPass.hlsl で positionWS を fragment へ渡し、ParticleUnlit_Surface.hlsl で alpha に softParticleFade / cameraFade を乗算
- ParticleUnlitShaderGUI.cs に Depth section を追加し、ParticleUnlitMaterialValidator.cs で depth toggle clamp と near/far 正規化、authoring warning を追加
- ParticleUnlitPresetUtility.cs で shipping material の depth interaction default-off を固定
- ParticleUnlitValidationBootstrapper.cs に BootstrapM11DepthInteractionValidationAssets、Dust/Smoke/Glow 用 validation material、Smoke marker 配下の depth wall、scene anchor を追加
- ParticleUnlitBuildValidator.cs / ParticleUnlitWebGlBuildUtility.cs / ParticleUnlitValidationTests.cs を EnsureM11ValidationAssetsReady / RunM11WebGlValidationBuild 契約へ更新
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

### repo 実装メモ

```text
- shader root は Assets/Art/Shader/Particles/ParticleLit/BC_Particles_ParticleLit.shader とし、Input / Common / Sampling / Lighting / Surface / ForwardPass へ分割する
- editor authoring は ParticleLitShaderGUI / ParticleLitMaterialValidator / ParticleLitPresetUtility で構成し、BlendMode / QueueOffset / Normal / Lighting parameter を hidden render-state と合わせて正規化する
- preset は Raindrop / Bubble / Debris の 3 種とし、Bubble は Premultiply、Debris は Mesh particle を前提に metallic を持たせる
- validation bootstrapper は BootstrapM12ParticleLitValidationAssets を追加し、Future Lit Test Area 配下へ Billboard 2種と Mesh 1種の anchor、generated lit base/normal texture、3 material を再生成する
- Debris validation は ParticleSystemRenderer.renderMode = Mesh と cube mesh assignment で確認し、Raindrop / Bubble は Billboard のまま main light 応答を確認する
- EditMode contract は ParticleLitValidationTests.cs で shader source audit、validator / preset determinism、generated material / texture importer、scene anchor / renderer mode を監査する
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

### repo 実装メモ

```text
- shader root は Assets/Art/Shader/Particles/ParticleDistortion/BC_Particles_ParticleDistortion.shader とし、Input / Common / Sampling / Surface / ForwardPass へ分割する
- URP Opaque Texture を正規経路とし、DeclareOpaqueTexture.hlsl + SampleSceneColor で scene color を参照する。GrabPass / Depth Fade / Flow Map はこの段階では入れない
- editor authoring は ParticleDistortionShaderGUI / ParticleDistortionMaterialValidator / ParticleDistortionPresetUtility で構成し、Opaque Texture 依存 warning と heavy distortion warning を inspector に出す
- validation bootstrapper は BootstrapM13ParticleDistortionValidationAssets を追加し、generated distortion vector/noise texture、3 material、3 prefab、Future Distortion Test Area の validation anchor / prefab preview / reference wall を再生成する
- EditMode contract は ParticleDistortionValidationTests.cs で shader source audit、validator / preset determinism、generated material / texture importer / prefab / scene anchor 契約と、M13 を標準 WebGL required set に入れていない境界を監査する
```

---

## M14: Ring / Ground 系拡張設計

### 目的

通常 Billboard では表現しにくい Ring / Ground 系 Particle の拡張方針を決める。この Milestone では完成した visual shader を作るのではなく、ParticleUnlit に無理に詰め込まないための design + scaffold/contract を repo 上で固定する。

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

### Milestone 境界

```text
M14 に含める:
  - docs 正本の更新
  - Ring / Ground の family ownership と採用条件の明文化
  - reserved property contract の固定
  - canonical prefab candidate naming の固定
  - folder / bootstrapper / EditMode test の scaffold contract 追加

M14 に含めない:
  - shipping-ready shader 実装
  - generated material / prefab 生成
  - validation scene marker 追加
  - WebGL build contract 追加
  - visual tuning と look-dev
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

### Family 分離条件

```text
Ring family を使う条件:
  - 中心からの半径制御が見た目の主成分
  - Polar / Radial Dissolve が必要
  - 輪郭発光や円環厚み制御が必要

Ground family を使う条件:
  - 地面接触前提で使う
  - Horizontal Billboard または flat projection が前提
  - World Space UV continuity と Height Fade が見た目の主成分

上記に当てはまらないもの:
  - 一般 billboard は ParticleUnlit に残す
  - 背景歪みは ParticleDistortion に残す
  - 線状の移動表現は TrailUnlit に残す
```

### reserved property contract

Ring:

```csharp
_BlendMode
_Cull
_ZTest
_BaseMap
_BaseColor
_Alpha
_UseVertexColor
_RingInnerRadius
_RingOuterRadius
_RingThickness
_PolarScrollSpeed
_RadialDissolveAmount
_EdgeEmissionColor
_EdgeEmissionStrength
_QueueOffset
```

Ground:

```csharp
_BlendMode
_Cull
_ZTest
_BaseMap
_BaseColor
_Alpha
_UseVertexColor
_WorldUvScale
_WorldUvScroll
_HeightFadeStart
_HeightFadeEnd
_GroundContactFade
_DirectionalNoiseMap
_DirectionalNoiseStrength
_DirectionalNoiseScroll
_QueueOffset
```

### canonical prefab 候補

Ring:

```text
FX_Particle_ShockwaveRing
FX_Particle_MagicCircle
FX_Particle_WaterRipple
FX_Particle_LandingRing
FX_Particle_ExplosionRing
```

Ground:

```text
FX_Particle_GroundSmoke
FX_Particle_FloorMist
FX_Particle_DustCloud
FX_Particle_MagicGroundAura
FX_Particle_CreepingFog
```

### renderer mode 方針

```text
ParticleRingUnlit:
  Billboard
  Horizontal Billboard
  Mesh support は後続

ParticleGroundUnlit:
  Horizontal Billboard
  Billboard optional
  Mesh support は後続
```

### 完了条件

```text
- Ring / Ground を ParticleUnlit 本体へ無理に詰め込まない方針が明確
- 専用 Shader 化する条件が明確
- 将来作る Prefab 候補が明確
- repo 上で Ring / Ground scaffold contract が固定されている
```

### repo 実装メモ

```text
- Assets/Art/Shader/Particles/ParticleRingUnlit と ParticleGroundUnlit は、M14 で root README と HLSL / HLSL/Passes / Editor scaffold contract を持つ
- ParticleUnlitValidationBootstrapper.cs は M14 で future family 用 directory constant と prefab candidate / marker naming を source contract として保持する
- ParticleMaterialSystemM1ScaffoldTests.cs は Ring / Ground の HLSL / Passes / Editor folder depth を監査する
- M14 専用の EditMode contract test は docs、bootstrapper source、scaffold README の整合を監査する
- shader root、ShaderGUI、validator、preset、generated material/prefab は M14 では追加しない
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

### M15 実装注記

```text
- ParticleUnlit shader に hidden property `_QualityTier` を追加し、Low / Medium / High の authored tier を material に保持する
- ParticleUnlitQualityTierUtility を追加し、tier 名称、description、ApplyTier、authored / inferred tier 判定を共有化する
- ParticleUnlitShaderGUI / ParticleUnlitMaterialValidator に Quality Tier section、tier mismatch warning、WebGL standard-path warning を追加する
- ParticleUnlitBuildValidator / ParticleUnlitWebGlBuildUtility は M15 quality tier validation assets を前提にし、High tier ParticleUnlit material を standard WebGL path から reject する
- ParticleUnlitValidationBootstrapper は scene root の standalone な Quality Tier Test Area と 3 つの validation material / anchor を再生成する
- ParticleUnlitLite / ParticleUnlitAdvanced への shader split はまだ行わず、split 判断基準だけを固定した状態で止める
```

---

## M16: Optimization / Validation / Documentation

### 目的

Particle Material System を実運用できる状態に仕上げる。最後にまとめて整えるのではなく、M1-M15 で積み上げたものを検証・整理する。

### M16 実装方針

```text
- M16 は feature expansion ではなく hardening milestone とする
- 最優先は M7-M15 で追加した validation の Unity-side 実行回収
- ParticleMaterialTestScene を M16 review harness として整理する
- guide docs を spec から分離し、usage / preset / performance を別文書にする
- ParticleUnlitLite / ParticleUnlitAdvanced や Ring / Ground functional shader は M16 では実装しない
```

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

M16 実装注記:

```text
- 既存の Dust / Smoke / Glow / WebGL marker child-set contract は崩さない
- M16 固有の review object は `ParticleMaterialReviewHarness` 配下の standalone object として管理する
- Quality Tier Test Area も scene root standalone area として維持する
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

### M16 実装注記

```text
- ParticleMaterialUsageGuide は family selection、validation scene workflow、shipping checklist を扱う
- ParticleMaterialPresetGuide は preset と quality tier の starting point を扱う
- ParticleMaterialPerformanceGuide は sample budget、WebGL boundary、overdraw / split trigger を扱う
- spec は contract の正本に留め、guide 相当の説明は別文書へ分離する
```

### 完了条件

```text
- Sample Scene で主要 Effect を確認できる
- WebGL で初期 Prefab が表示できる
- Material / Prefab / Texture の命名が整理されている
- 仕様書と実装が矛盾していない
- 次に ParticleLit / ParticleDistortion / Ring / Ground へ進める状態
```

### Non-Goals

```text
- ParticleUnlitLite / ParticleUnlitAdvanced の shader root 新設
- ParticleRingUnlit / ParticleGroundUnlit の functional shader 実装
- automated overdraw measurement の導入
- Lit / Distortion family の look-dev 深掘り
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
