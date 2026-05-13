以下を **EnvironmentStylizedLit Milestone Spec v0.1** として定義します。
目的は、いきなり完成版を作らず、**常にコンパイル可能・比較可能・破綻箇所を特定可能な単位**で Shader を育てることです。

---

# EnvironmentStylizedLit Milestone Spec v0.1

## 現在の進捗棚卸し

この節は planned scope ではなく、2026-05-13 時点の実装実体ベースの進捗です。

### 進捗サマリー

```text
- 現在の実装到達点: M14 Production Validation / Authoring Guide
- 直列完了点: M14 Production Validation / Authoring Guide
- 順番評価: M0 -> M1 -> M2 -> M3 -> M4 -> M5 -> M6 -> M7 -> M8 -> M9 -> M10 -> M11 -> M12 -> M13 -> M14 の順で進んでいます
```

### Milestone Progress

| Milestone | Status | Progress | Notes |
| --- | --- | --- | --- |
| M0 Project Scaffold / File Layout | Complete | 100% | Shader / HLSL / Editor / Presets / Documentation / Test material / validation scene の受け皿が存在します。 |
| M1 Minimal URP Forward Shader | Complete | 100% | ForwardLit pass で BaseColor を描画する最小 shader は成立しています。 |
| M2 SurfaceData Foundation | Complete | 100% | BaseMap / BaseColor / AlphaClip / NormalMap / Occlusion / Emission / Metallic / Smoothness の SurfaceData build は成立しており、M2 validation material / scene と EditMode contract test まで閉じています。 |
| M3 Stylized Main Light Diffuse | Complete | 100% | Main Light の stepped diffuse、Wrap Lighting、Band color controls、M3 debug views を runtime HLSL と EditMode contract tests まで閉じています。 |
| M4 Shadow / Cull / Interior Room Stability | Complete | 100% | Main Light の receive shadow、ShadowInfluence / ShadowSoftFill / ShadowColorBlend、AlphaClip 対応 ShadowCaster、ESL_TestRoom の室内箱 / Cull / cast-shadow validation を runtime と EditMode contract tests まで閉じています。 |
| M5 Ambient / Bounce / Shadow Color System | Complete | 100% | Directional ambient、疑似 bounce light、IndirectShadowColor、M5 validation material / scene / EditMode contract tests まで閉じています。 |
| M6 Stylized Specular / Edge Sheen | Complete | 100% | SpecularMode Off / Soft / Quantized / Ceramic / Plastic、EdgeSheen、M6 validation material / scene / EditMode contract tests まで閉じています。 |
| M7 World Noise / Band Noise | Complete | 100% | Noise.hlsl に軽量 world/object noise と distance fade を実装し、noise source を World/ObjectSpace から選択できるようにした上で、Surface / StylizedDiffuse / Debug と M7 validation material / scene / EditMode contract tests まで閉じています。 |
| M8 Required Render Pass Completion | Complete | 100% | ShadowCaster / DepthOnly / DepthNormalsOnly / Meta pass を実装し、AlphaClip を Shadow / Depth / DepthNormals に反映、Meta では bake 用 Albedo / Emission のみを出力し、M8 validation scene anchor と EditMode contract tests まで閉じています。 |
| M9 Baked GI / Light Probe / SSAO Compatibility | Complete | 100% | ForwardLit が baked GI / Light Probe SH / SSAO AO factor / shadow mask を transport し、Lighting が MixRealtimeAndBakedGI と cavity tint を owner として扱い、M9 validation material / scene / EditMode contract tests まで閉じています。 |
| M10 Additional Lights | Complete | 100% | AdditionalLightMode Off / FillOnly / Quantized / Continuous、AdditionalLightIntensity、AdditionalLightShadowInfluence、AdditionalLightColorInfluence、point/spot light validation materials / scene anchors / EditMode contract tests まで閉じています。 |
| M11 Triplanar / Vertex Color / World Gradient | Complete | 100% | Triplanar BaseMap / NormalMap / Noise、vertex color mask、world Y gradient、validation materials / scene anchors / EditMode contract tests まで閉じています。 |
| M12 ShaderGUI / Validator / Presets | Complete | 100% | CustomEditor 接続、用途別 section を持つ ShaderGUI、DebugView / Triplanar warning と値正規化を行う validator、5 種 preset のワンクリック適用、M12 EditMode contract tests まで閉じています。 |
| M13 Performance / Variant Cleanup | Complete | 100% | triplanar-only local keyword policy、DebugView の non-development build validator、Low / Medium / High tier utility、M13 validation material / scene anchor、EditMode contract tests まで閉じています。 |
| M14 Production Validation / Authoring Guide | Complete | 100% | module-local guide、production validation anchor、preset review material、M14 EditMode contract が同期済みです。 |

### 判断メモ

```text
- この milestone line は PostProcess とは別物です。
- 現在 repo にある実装から見る限り、EnvironmentStylizedLit は M14 の production validation / authoring guide 段階まで到達しています。
- DebugView の本番禁止は ShaderGUI warning だけでなく build validator で固定し、tier 定義は preset から分離しています。
```

## 0. 基本方針

この Shader は、以下の順で作る。

```text
1. URPで正しく描画される最小Shader
2. Surface入力の整理
3. Main Lightによる段階化Diffuse
4. Shadow / Ambient / Bounce
5. Specular / Edge Sheen
6. Noise / Band Noise
7. DepthNormals / SSAO / Meta / Lightmap
8. Additional Lights
9. Triplanar / Vertex Color / World Gradient
10. ShaderGUI / Preset / Validation
11. 最適化・Variant整理・実制作検証
```

最初から全部入れない。
特に以下は後回しにする。

```text
- Triplanar
- Additional Lightsの高度対応
- Baked GIの高度Stylize
- ShaderGUI
- Preset大量作成
- 複雑なNoise
- Vertex Color Mask
```

理由は単純で、初期段階で機能を増やすと、**ライト計算の問題なのか、Passの問題なのか、Normalの問題なのか、Shader Variantの問題なのか切り分け不能になる**からです。

---

# 1. Milestone一覧

```text
M0  Project Scaffold / File Layout
M1  Minimal URP Forward Shader
M2  SurfaceData Foundation
M3  Stylized Main Light Diffuse
M4  Shadow / Cull / Interior Room Stability
M5  Ambient / Bounce / Shadow Color System
M6  Stylized Specular / Edge Sheen
M7  World Noise / Band Noise
M8  Required Render Pass Completion
M9  Baked GI / Light Probe / SSAO Compatibility
M10 Additional Lights
M11 Triplanar / Vertex Color / World Gradient
M12 ShaderGUI / Validator / Presets
M13 Performance / Variant Cleanup
M14 Production Validation / Authoring Guide
```

この順番を崩さない方がいいです。

---

# M0 Project Scaffold / File Layout

## 目的

Shader 実装前に、**ファイル構成・命名・空ファイル・テスト用Scene** を作る。

この段階では Shader の見た目を作らない。

## 作成物

```text
Assets/Art/Shader/EnvironmentStylizedLit/
  EnvironmentStylizedLit.shader

  HLSL/
    EnvironmentStylizedLit_Input.hlsl
    EnvironmentStylizedLit_Common.hlsl
    EnvironmentStylizedLit_Surface.hlsl
    EnvironmentStylizedLit_Lighting.hlsl
    EnvironmentStylizedLit_StylizedDiffuse.hlsl
    EnvironmentStylizedLit_Ambient.hlsl
    EnvironmentStylizedLit_Specular.hlsl
    EnvironmentStylizedLit_Noise.hlsl
    EnvironmentStylizedLit_Triplanar.hlsl
    EnvironmentStylizedLit_Debug.hlsl

    Passes/
      EnvironmentStylizedLit_ForwardLitPass.hlsl
      EnvironmentStylizedLit_ShadowCasterPass.hlsl
      EnvironmentStylizedLit_DepthOnlyPass.hlsl
      EnvironmentStylizedLit_DepthNormalsPass.hlsl
      EnvironmentStylizedLit_MetaPass.hlsl

  Editor/
    EnvironmentStylizedLitShaderGUI.cs
    EnvironmentStylizedLitMaterialValidator.cs
    EnvironmentStylizedLitPresetUtility.cs

  Presets/

  Documentation/
    README.md

Assets/Art/Materials/EnvironmentStylizedLit/
  ESL_Test_Default.mat
  ESL_Test_Interior.mat

Assets/Scenes/EnvironmentStylizedLit/
  ESL_TestRoom.unity
  ESL_LightingLab.unity
```

このプロジェクトでは `GameLib` 配下は使わない。
Shader本体は既存規約に合わせて `Assets/Art/Shader`、Material は `Assets/Art/Materials`、Scene は `Assets/Scenes` に置く。
`Editor` / `Presets` / `Documentation` は M12 以降の受け皿として M0 で作るが、この段階では実処理を書かない。

## 完了条件

```text
- ファイル構成が確定している
- すべてのHLSLにinclude guardがある
- 関数prefixは ESL_ に統一されている
- .shader は存在するが、まだ高度な描画はしない
- TestRoom / LightingLab が作成されている
```

## 禁止事項

```text
- まだStylized Lightingを書かない
- まだNoiseを書かない
- まだShaderGUIを書かない
- まだTriplanarを書かない
```

---

# M1 Minimal URP Forward Shader

## 目的

まず、URPで **magentaにならず、OpaqueなBaseColorが描画される最小Shader** を作る。

ここでの目的は「絵作り」ではなく、**URP Pass接続の成功確認**。

## 実装内容

```text
- ForwardLit Pass
- BaseColorのみ描画
- Cull設定
- ZWrite On
- ZTest LEqual
- Fog対応は最低限
```

## 使用ファイル

```text
EnvironmentStylizedLit.shader
EnvironmentStylizedLit_Input.hlsl
EnvironmentStylizedLit_Common.hlsl
EnvironmentStylizedLit_ForwardLitPass.hlsl
```

## 完了条件

```text
- Materialを割り当てたCube / Plane / ProBuilder Meshが表示される
- Cull Front / Back / Both がUnity標準的な意味で動く
- 内向き法線の箱で、Front設定時に内側だけ描画できる
- Scene View / Game Viewでmagentaにならない
```

## 禁止事項

```text
- Main Light計算を入れない
- NormalMapを入れない
- Shadowを入れない
- Specularを入れない
```

この時点で Cull が正しくない場合、以降全部が壊れます。
M1は地味ですが、かなり重要です。

---

# M2 SurfaceData Foundation

## 目的

Lit系Shaderとして必要な Material 入力を整理する。

この段階では、まだ高度なライト表現はしない。
**SurfaceDataを正しく作ること** が目的。

## 実装内容

```text
- BaseMap
- BaseColor
- Alpha
- AlphaClip
- NormalMap
- NormalScale
- OcclusionMap
- EmissionMap
- Metallic
- Smoothness
- SpecularColor
```

## 使用ファイル

```text
EnvironmentStylizedLit_Input.hlsl
EnvironmentStylizedLit_Surface.hlsl
EnvironmentStylizedLit_ForwardLitPass.hlsl
```

## 作る構造体

```hlsl
struct ESL_SurfaceData
{
    float3 albedo;
    float alpha;

    float3 normalTS;
    float metallic;
    float smoothness;
    float occlusion;

    float3 emission;

    float cavity;
    float colorVariationMask;
    float bandOffsetMask;
    float specialMask;
};
```

## 完了条件

```text
- BaseMapが正しく反映される
- BaseColor tintが効く
- AlphaClipが動く
- NormalMapをON/OFFできる
- Emissionが最低限出る
- SurfaceData構築処理がForwardLitPassに直書きされていない
```

## 禁止事項

```text
- StylizedDiffuseをまだ完成させない
- ShadowColorやRampをまだ入れすぎない
- Additional Lightsを入れない
```

---

# M3 Stylized Main Light Diffuse

## 目的

この Shader の核である **x段階のライト表現** を実装する。

ここで初めて「普通のLitではない」見た目になる。

## 実装内容

```text
- Main Light取得
- NdotL計算
- Wrap Lighting
- LightStepCount
- LightStepSmoothness
- BandContrast
- BandOffset
- DeepShadowColor
- ShadowColor
- MidColor
- LightColor
- HighlightColor
```

## 使用ファイル

```text
EnvironmentStylizedLit_StylizedDiffuse.hlsl
EnvironmentStylizedLit_Lighting.hlsl
EnvironmentStylizedLit_Debug.hlsl
EnvironmentStylizedLit_ForwardLitPass.hlsl
```

## 必須Debug View

```text
- NdotL
- WrappedLight
- SteppedLight
- BandColor
```

## 完了条件

```text
- LightStepCountを変えると明暗段階が変わる
- StepSmoothnessで境界の硬さを調整できる
- WrapLightingで影側の明るさが変わる
- ShadowColor / LightColor が正しく影響する
- DebugViewでNdotL / SteppedLightを確認できる
```

## 禁止事項

```text
- まだBand Noiseを入れない
- まだAdditional Lightsを入れない
- まだTriplanarを入れない
```

M3がこのShaderの最初の山です。
ここが微妙なら、後の機能を増やしても良くなりません。

---

# M4 Shadow / Cull / Interior Room Stability

## 目的

影、Cull、内向き法線、室内箱で破綻しないようにする。

あなたの用途ではかなり重要です。
地面・壁・天井に使うため、普通のキャラクターShaderより **CullとShadowの挙動が重要** です。

## 実装内容

```text
- Receive Shadows
- Main Light Shadow Attenuation
- ShadowInfluence
- ShadowSoftFill
- ShadowColorBlend
- ShadowCaster Passの最小実装
- AlphaClip対応ShadowCaster
```

## 使用ファイル

```text
EnvironmentStylizedLit_Lighting.hlsl
EnvironmentStylizedLit_ShadowCasterPass.hlsl
EnvironmentStylizedLit_Surface.hlsl
```

## 検証Scene

```text
ESL_TestRoom.unity
```

検証内容：

```text
- 内向き法線の部屋
- 床・壁・天井
- Point Light
- Directional Light
- Cast Shadows On/Off
- Cull Front / Back / Both
```

## 完了条件

```text
- 内向き法線の箱で正しく描画される
- ToonShaderのようにFront/Backの意味が逆転しない
- ShadowCasterが動く
- AlphaClipされた面が影でも反映される
- 影が完全な黒に潰れない調整項目がある
```

## 禁止事項

```text
- まだSSAO対応を入れない
- まだDepthNormalsを深く作り込まない
- まだBaked GI対応に進まない
```

---

# M5 Ambient / Bounce / Shadow Color System

## 目的

箱の中や室内で見た目が死なないように、**方向性Ambientと疑似Bounce Light** を実装する。

ここで「粘土ジオラマ感」がかなり出る。

## 実装内容

```text
- AmbientTopColor
- AmbientSideColor
- AmbientBottomColor
- AmbientStrength
- BounceColor
- BounceStrength
- BounceDirection
- BounceWrap
- IndirectShadowColor
```

## 使用ファイル

```text
EnvironmentStylizedLit_Ambient.hlsl
EnvironmentStylizedLit_Lighting.hlsl
```

## 完了条件

```text
- 上向き面、横向き面、下向き面でAmbient色が変わる
- 室内箱の中が真っ黒にならない
- BounceStrengthで影側に色を足せる
- Directional Lightが弱くても形が読める
```

## 禁止事項

```text
- 本物のGIを再現しようとしない
- BakedGIをまだ複雑にStylizeしない
- 複数Bounce方向をまだ作らない
```

ここでは「物理的正確さ」より「絵として読めること」を優先します。

---

# M6 Stylized Specular / Edge Sheen

## 目的

粘土、石膏、マットプラスチック、陶器っぽさを出すために、**弱く整理された反射** を追加する。

## 実装内容

```text
- SpecularMode
  - Off
  - Soft
  - Quantized
  - Ceramic
  - Plastic

- SpecularStrength
- Smoothness
- SpecularStepCount
- SpecularStepSmoothness
- SpecularColor
- EdgeSheenStrength
- EdgeSheenPower
- EdgeSheenColor
```

## 使用ファイル

```text
EnvironmentStylizedLit_Specular.hlsl
EnvironmentStylizedLit_Lighting.hlsl
```

## 完了条件

```text
- Soft Specularで粘土/石膏風になる
- Ceramic設定で少し硬いハイライトが出る
- Plastic設定で玩具っぽい広めの反射が出る
- EdgeSheenはOutlineではなく、軽い縁の光として機能する
```

## 禁止事項

```text
- 強すぎる白ハイライトにしない
- キャラToonのRim Lightのようにしない
- Outline的な見た目に寄せない
```

---

# M7 World Noise / Band Noise

## 目的

広い壁・床が単調にならないようにする。
また、段階影の境界を完全なセル境界にせず、**ざらついた粘土・石膏感** を出す。

## 実装内容

```text
- WorldNoise
- ObjectSpaceNoise
- AlbedoNoiseStrength
- WorldNoiseScale
- WorldNoiseStrength
- WorldNoiseContrast
- LightBandNoiseStrength
- LightBandNoiseScale
- NoiseDistanceFadeStart
- NoiseDistanceFadeEnd
```

## 使用ファイル

```text
EnvironmentStylizedLit_Noise.hlsl
EnvironmentStylizedLit_StylizedDiffuse.hlsl
EnvironmentStylizedLit_Surface.hlsl
EnvironmentStylizedLit_Debug.hlsl
```

## 必須Debug View

```text
- WorldNoise
- BandNoise
```

## 完了条件

```text
- WorldNoiseでUVに依存しない色ムラが出る
- BandNoiseで明暗境界が少し揺らぐ
- 距離でNoiseを弱められる
- Noiseを0にすると完全に無効化される
```

## 禁止事項

```text
- まだTriplanar Texture Samplingを入れない
- 重いNoiseを大量に入れない
- 動くNoiseを標準にしない
```

地面・壁用なので、ノイズが強すぎるとゲーム画面全体が汚くなります。
このMilestoneでは「質感があるが邪魔ではない」範囲に収める。

---

# M8 Required Render Pass Completion

## 目的

URPで実運用するために必要なPassを揃える。

## 実装内容

```text
- ShadowCaster Pass 完成
- DepthOnly Pass
- DepthNormals Pass
- Meta Pass
```

## 使用ファイル

```text
EnvironmentStylizedLit_ShadowCasterPass.hlsl
EnvironmentStylizedLit_DepthOnlyPass.hlsl
EnvironmentStylizedLit_DepthNormalsPass.hlsl
EnvironmentStylizedLit_MetaPass.hlsl
```

## 完了条件

```text
- Depth Texture使用時に破綻しない
- SSAOを使う準備ができている
- DepthNormalsでNormalが正しく出る
- AlphaClipがDepth / Shadow / DepthNormalsに反映される
- MetaPassでLightmap bake用のAlbedo / Emissionが出る
```

## 禁止事項

```text
- MetaPassにStylized Lightingを焼き込みすぎない
- DepthOnlyに不要なLighting処理を入れない
- ShadowCasterにNoiseやSpecularを入れない
```

Passごとの責務を混ぜると後で壊れます。

---

# M9 Baked GI / Light Probe / SSAO Compatibility

## 目的

環境用Shaderとして、Lightmap / Light Probe / SSAO と共存できるようにする。

## 実装内容

```text
- BakedGI取得
- LightProbe / SH対応
- IndirectStrength
- IndirectStylizeStrength
- SSAO反映
- Occlusionとの統合
```

## 使用ファイル

```text
EnvironmentStylizedLit_Lighting.hlsl
EnvironmentStylizedLit_Ambient.hlsl
EnvironmentStylizedLit_DepthNormalsPass.hlsl
```

## 完了条件

```text
- Lightmapあり/なしで破綻しない
- Light Probeあり/なしで破綻しない
- SSAOが効く
- AOが黒乗算だけにならず、色付きCavityとして扱える
```

## 禁止事項

```text
- BakedGIをそのまま強く足しすぎない
- Stylized Lightingの段階感をBakedGIで潰さない
- AOを真っ黒乗算にしない
```

---

# M10 Additional Lights

## 目的

Point Light / Spot Light などの追加ライトに対応する。

ただし、全部をMain Lightと同じ段階化にすると画面がうるさくなるため、モード制御を持つ。

## 実装内容

```text
AdditionalLightMode:
  Off
  FillOnly
  Quantized
  Continuous

AdditionalLightIntensity
AdditionalLightShadowInfluence
AdditionalLightColorInfluence
```

## 使用ファイル

```text
EnvironmentStylizedLit_Lighting.hlsl
EnvironmentStylizedLit_StylizedDiffuse.hlsl
```

## 完了条件

```text
- Point Lightが効く
- Spot Lightが効く
- FillOnlyで影側を自然に持ち上げられる
- Quantizedで追加ライトも段階化できる
- Offにすると完全に無効化される
```

## 禁止事項

```text
- 初期設定をQuantizedにしない
- 複数ライトで画面をバンドだらけにしない
- Additional LightをMain Lightより目立たせすぎない
```

推奨デフォルトはこれです。

```text
AdditionalLightMode = FillOnly
```

---

# M11 Triplanar / Vertex Color / World Gradient

## 目的

ProBuilderやモジュール式レベル制作で使いやすくする。

このMilestoneで、Shaderはかなり実用寄りになる。

## 実装内容

```text
- Triplanar BaseMap
- Triplanar NormalMap
- Triplanar Noise
- VertexColor Mask
- WorldYGradient
```

## Vertex Color 用途

```text
R = Cavity Mask
G = Band Offset Mask
B = Color Variation Mask
A = Special Effect Mask
```

M11ではAを予約チャンネルとして保持する。
具体的なSpecial Effectの消費処理は、後続Milestoneで用途を明文化してから追加する。

## 使用ファイル

```text
EnvironmentStylizedLit_Triplanar.hlsl
EnvironmentStylizedLit_Surface.hlsl
EnvironmentStylizedLit_Noise.hlsl
EnvironmentStylizedLit_Lighting.hlsl
```

## 完了条件

```text
- UVが雑なProBuilder壁でも質感が成立する
- TriplanarをOFFにすると通常UVに戻る
- VertexColorで汚れ/色ムラ/明暗補正ができる
- WorldYGradientで壁の上下差を出せる
```

## 禁止事項

```text
- TriplanarをデフォルトONにしない
- VertexColorの意味を途中で変えない
- WorldGradientをLighting計算と密結合しない
```

---

# M12 ShaderGUI / Validator / Presets

## 目的

Material Inspectorを実用可能にする。

このShaderはプロパティが多いため、標準Inspectorに全部並べると使い物にならない。

## 実装内容

```text
Editor/
  EnvironmentStylizedLitShaderGUI.cs
  EnvironmentStylizedLitMaterialValidator.cs
  EnvironmentStylizedLitPresetUtility.cs
```

## Inspector Section

```text
Surface
Base
Lighting
Shadow
Ambient / Bounce
Specular
Noise
Cavity / AO
Vertex / Gradient
Advanced
Debug
```

## Preset

```text
ClayDiorama
PaintedPlaster
MatteToyPlastic
CeramicToy
ChalkPastel
```

## 完了条件

```text
- Inspectorが用途別に整理されている
- DebugView中は警告が出る
- Triplanar ON時に負荷警告が出る
- Presetをワンクリックで適用できる
- 不正値がValidatorで補正される
```

## 禁止事項

```text
- ShaderGUIに計算ロジックを書かない
- Preset値をShaderGUIに直書きしない
- ValidatorなしでKeywordを放置しない
```

---

# M13 Performance / Variant Cleanup

## 目的

見た目が完成してから、Shader Variantと負荷を整理する。

最初から最適化しすぎると、表現実験が遅くなる。
最適化はこの段階でまとめてやる。

## 実装内容

```text
- shader_feature_local整理
- 不要multi_compile削減
- DebugViewのビルド時扱い確認
- Low / Medium / High Tier整理
- Triplanar負荷計測
- Additional Lights負荷計測
- Noise負荷計測
```

## Performance Tier

```text
Low:
  Main Lightのみ
  No Triplanar
  No Band Noise
  No Additional Lights
  Simple Specular

Medium:
  Main Light
  FillOnly Additional Lights
  World Noise
  Soft Specular
  AO

High:
  Triplanar
  Band Noise
  Stylized Specular
  Vertex Color
  SSAO
```

## 完了条件

```text
- 不要なShader Variantが削減されている
- Materialごとのkeywordが正しく切り替わる
- Low / Medium / High の使い分けが明確
- 大きな壁・床に複数枚使っても極端に重くない
```

## 禁止事項

```text
- 見た目が固まる前に最適化で表現を潰さない
- すべての機能を常時ONにしない
- DebugViewを本番用Materialに残さない
```

---

# M14 Production Validation / Authoring Guide

## 目的

実制作で使えるか検証し、運用ルールを固める。

## 作成物

```text
Documentation/
  EnvironmentStylizedLitPropertyReference.md
  EnvironmentStylizedLitAuthoringGuide.md
  EnvironmentStylizedLitTroubleshooting.md
```

## 検証項目

```text
- 室内箱
- ProBuilder床
- ProBuilder壁
- 階段
- 柱
- 角をベベルした壁
- Lightmapあり
- Lightmapなし
- Point Lightあり
- Spot Lightあり
- Directional Lightのみ
- SSAOあり
- SSAOなし
```

## 完了条件

```text
- 実際のステージで使える
- Authoring Guideがある
- よくある問題と対処が整理されている
- Presetの見た目が安定している
- Shaderの用途と非用途が明確になっている
```

---

# 2. 実装の流れ

実装順は以下で固定する。

```text
M0
↓
M1
↓
M2
↓
M3
↓
M4
↓
M5
↓
M6
↓
M7
↓
M8
↓
M9
↓
M10
↓
M11
↓
M12
↓
M13
↓
M14
```

特に重要なのは、**M3より前にNoiseやSpecularを入れないこと**です。

理由は、ライト段階化の品質が確認できなくなるからです。

---

# 3. AI実装向けのタスク分割

AIに投げるなら、Milestoneごとに以下の粒度にする。

## 悪い依頼

```text
EnvironmentStylizedLit Shaderを全部実装してください。
```

これは高確率で破綻します。

## 良い依頼

```text
M1のみ実装してください。
目的はURPでmagentaにならず、BaseColorだけが表示される最小ForwardLit Shaderです。
Lighting、NormalMap、Shadow、Noise、Specularは実装しないでください。
```

次：

```text
M2のみ実装してください。
SurfaceDataの構築を追加します。
BaseMap / BaseColor / AlphaClip / NormalMap / Occlusion / Emissionを読み取ります。
Stylized Lightingはまだ変更しないでください。
```

次：

```text
M3のみ実装してください。
Main LightのNdotL、WrapLighting、LightStepCount、StepSmoothness、Light Ramp Color、DebugViewを実装します。
Shadow、Noise、Specular、Additional Lightsはまだ入れないでください。
```

このように、**禁止事項まで明示する**のが重要です。

---

# 4. 各Milestoneの完了レビュー観点

各Milestone完了時には、必ず以下を見る。

```text
1. コンパイルエラーがないか
2. magentaになっていないか
3. Shader Variantが無駄に増えていないか
4. .shader本体にロジックが増えていないか
5. PassファイルにLightingロジックが混ざっていないか
6. DebugViewで中間値を確認できるか
7. 内向き法線の箱で壊れていないか
8. ProBuilder Meshで壊れていないか
```

---

# 5. 最重要チェックポイント

## Checkpoint A: M1完了時

```text
Cull / 法線 / 内向き箱が正しい
```

ここが壊れていると全部ダメです。

---

## Checkpoint B: M3完了時

```text
x段階ライト表現が魅力的か
```

ここが微妙なら、後の機能で誤魔化しても微妙です。

---

## Checkpoint C: M5完了時

```text
室内が黒潰れせず、粘土・ジオラマ感が出ているか
```

このShaderの個性はM5でほぼ決まります。

---

## Checkpoint D: M8完了時

```text
URPの基本Passとして破綻していないか
```

ここで初めて「実用Shader」に近づきます。

---

## Checkpoint E: M12完了時

```text
制作者がMaterialを調整できる状態か
```

Shaderが高機能でも、Inspectorが崩壊していたら運用不能です。

---

# 6. 優先順位

最優先はこれです。

```text
1. Cull / 法線 / 室内箱で正しい
2. x段階ライトが美しい
3. 影が黒く潰れない
4. 壁・床が単調にならない
5. URP Passとして破綻しない
6. Inspectorで運用できる
7. 負荷を制御できる
```

逆に、初期段階で優先しないもの。

```text
- 完璧なPBR互換
- 金属表現
- 透明表現
- 水表現
- キャラクター表現
- アウトライン
- 高度なGI再現
```

---

# 7. 実装開始時の推奨ブランチ

```text
feature/environment-stylized-lit-m0-scaffold
feature/environment-stylized-lit-m1-forward
feature/environment-stylized-lit-m2-surface
feature/environment-stylized-lit-m3-stylized-diffuse
...
```

Milestoneごとにブランチを切るべきです。
Shaderは一度壊れると原因が追いにくいため、細かく戻れる状態にした方がいいです。

---

# 8. 最初に作るべきMilestone

次にやるべきは **M0** です。

```text
M0 Project Scaffold / File Layout
```

ここでは、まだShaderの見た目は作らず、以下だけを完成させます。

```text
- フォルダ構成
- 空HLSL
- include guard
- .shaderの最低限
- テスト用Material
- テスト用Scene
```

M0を雑にすると、後の実装でファイルが散らかります。
このShaderは規模が大きいので、最初に構成を固めるべきです。
