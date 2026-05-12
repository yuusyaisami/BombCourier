以下を **ToyDioramaPostProcess Milestone Spec** として定義します。

これは既存の `PS2PostProcess` を拡張する計画ではなく、**別物の本命ポストプロセスを段階的に作るための実装順序**です。
最重要方針は、**Bloom や Haze で誤魔化す前に、まず色設計を固めること**です。

---

# ToyDioramaPostProcess Milestone Spec

## 0. 基本方針

このポストプロセスは、以下の順で作る。

```text
M0  Project Scaffold / File Layout
M1  Passthrough Composite Pass
M2  Settings Binding / Debug View Foundation
M3  Core Color Grade
M4  Pastel Compression / Cream Highlight
M5  Edge Tone / Lens Shading
M6  Depth Haze
M7  Clean Grain / Blue Noise
M8  Soft Bloom / Halation Pipeline
M9  Quality Tier / Preset System
M10 Camera / UI / Injection Policy
M11 Performance / RT Lifetime / Cleanup
M12 Production Validation / Authoring Guide
```

最初から全部作らない。
特に以下は後回し。

```text
- Bloom
- Halation
- Depth Haze
- Grain
- Miniature DOF
- Lens Distortion
- Chromatic Aberration
```

理由は明確です。
最初に Bloom や Haze を入れると、**色設計が弱いのに、それっぽく見えてしまう**からです。
この PostProcess の芯は、エフェクト量ではなく **色・明部・暗部・彩度の整理** です。

---

# M0 Project Scaffold / File Layout

## 目的

実装前に、フォルダ構成・ファイル分割・テストシーン・命名規則を確定する。

この段階では、まだ見た目を作らない。

## 作成物

```text
Assets/
  BC/
    Rendering/
      PostProcess/
        ToyDiorama/
          Shaders/
            ToyDioramaComposite.shader
            ToyDioramaBloom.shader

            HLSL/
              ToyDiorama_Input.hlsl
              ToyDiorama_Common.hlsl
              ToyDiorama_ColorGrade.hlsl
              ToyDiorama_Pastel.hlsl
              ToyDiorama_Highlight.hlsl
              ToyDiorama_DepthHaze.hlsl
              ToyDiorama_EdgeTone.hlsl
              ToyDiorama_Grain.hlsl
              ToyDiorama_Debug.hlsl

          Runtime/
            ToyDioramaPostProcessSettings.cs
            ToyDioramaPostProcessFeature.cs
            ToyDioramaCompositePass.cs
            ToyDioramaBloomPass.cs
            ToyDioramaRenderTargets.cs

          Editor/
            ToyDioramaPostProcessEditor.cs
            ToyDioramaPostProcessValidator.cs
            ToyDioramaPresetUtility.cs

          Presets/
            SoftToy.asset
            ClayDiorama.asset
            MattePlastic.asset
            PictureBook.asset
            CleanDebug.asset

          Samples/
            ToyDiorama_ColorLab.unity
            ToyDiorama_DepthLab.unity
            ToyDiorama_BloomLab.unity
            ToyDiorama_GameplayLab.unity

          Documentation/
            ToyDioramaPostProcessSpec.md
            ToyDioramaPostProcessMilestones.md
            ToyDioramaPostProcessPropertyReference.md
            ToyDioramaPostProcessAuthoringGuide.md
```

## 完了条件

```text
- 全ファイルの配置が確定している
- HLSLファイルに include guard がある
- shader / runtime / editor / presets / samples / docs が分かれている
- 既存の PS2PostProcess とファイル・名前空間・責務が混ざっていない
```

## 禁止事項

```text
- まだ色補正を実装しない
- まだBloomを作らない
- まだDepthを読む処理を書かない
- まだShaderGUIを作り込まない
```

---

# M1 Passthrough Composite Pass

## 目的

URP の RendererFeature / RenderPass として、画面を壊さずに取得・出力できる最小パスを作る。

ここでは絵作りをしない。
**正しく Blit / Composite できるかだけを見る。**

## 実装内容

```text
- ToyDioramaPostProcessFeature
- ToyDioramaCompositePass
- ToyDioramaComposite.shader
- Source Color を取得
- そのまま Destination に出力
```

## 使用ファイル

```text
Runtime/
  ToyDioramaPostProcessFeature.cs
  ToyDioramaCompositePass.cs
  ToyDioramaPostProcessSettings.cs

Shaders/
  ToyDioramaComposite.shader

HLSL/
  ToyDiorama_Input.hlsl
  ToyDiorama_Common.hlsl
```

## 完了条件

```text
- RendererFeatureを追加しても画面が変わらない
- Game View が正常表示される
- Scene View / Game View が magenta にならない
- Frame Debugger上でToyDioramaCompositePassを確認できる
- Game Viewの解像度変更で破綻しない
- 一時RTの確保・解放が最低限正しく動く
```

## 禁止事項

```text
- まだ色補正を入れない
- まだDepthを要求しない
- まだBloom用RTを作らない
- まだDebugViewを作り込まない
```

M1で失敗している状態で機能を足すと、原因切り分けが終わります。
まずは **何もしないPostProcessが安全に動く** ことを確認します。

---

# M2 Settings Binding / Debug View Foundation

## 目的

C# 側の設定値を Shader に正しく渡し、Debug View の土台を作る。

ここもまだ本格的な絵作りには入らない。

## 実装内容

```text
- ToyDioramaPostProcessSettings
- Material Property binding
- DebugView enum
- DebugView: SourceColor
- DebugView: Luminance
- DebugView: UV
```

## Settings 初期項目

```text
Enable
QualityTier
DebugView

Exposure
Contrast
Saturation

ShadowTint
MidTint
HighlightTint
```

## DebugView

```text
Off
SourceColor
Luminance
UV
```

## 完了条件

```text
- C#側の値変更がShaderに反映される
- DebugViewを切り替えられる
- SourceColor / Luminance / UV を確認できる
- DebugView中はInspector側で警告を出せる準備がある
- 設定値が未設定でも例外を出さずに安全に動く
```

## 禁止事項

```text
- まだPastel処理を入れない
- まだCream Highlightを入れない
- まだBloomを入れない
```

---

# M3 Core Color Grade

## 目的

この PostProcess の基礎である、**暗部・中間・明部の色設計** を実装する。

ここが弱いと、後から何を足しても安っぽくなる。

## 実装内容

```text
- Exposure
- Contrast
- BlackLift
- WhiteSoftClamp
- Shadow/Mid/Highlight Mask
- ShadowTint
- MidTint
- HighlightTint
- ShadowTintStrength
- MidTintStrength
- HighlightTintStrength
```

## 使用ファイル

```text
HLSL/
  ToyDiorama_ColorGrade.hlsl
  ToyDiorama_Common.hlsl
  ToyDiorama_Debug.hlsl
```

## 必須 DebugView

```text
Luminance
ShadowMask
MidMask
HighlightMask
BeforeColorGrade
AfterColorGrade
```

## 完了条件

```text
- 暗部を黒ではなく色付きに寄せられる
- 明部を真っ白ではなく柔らかい色に寄せられる
- 中間色を過剰に壊さず維持できる
- BlackLiftで暗部の潰れを制御できる
- WhiteSoftClampで白飛びの硬さを抑えられる
- DebugViewでShadow/Mid/HighlightのMaskを確認できる
```

## 禁止事項

```text
- 彩度圧縮をまだ入れない
- Cream Highlightをまだ入れない
- Bloomをまだ入れない
- Vignette的な黒落としを入れない
```

この時点で既存 `PS2PostProcess` より方向性が変わっている必要があります。
ただし、まだ派手な効果は不要です。

---

# M4 Pastel Compression / Cream Highlight

## 目的

玩具・粘土・ジオラマ向けの色に寄せる。
ここで「安い色補正」から一段上げる。

## 実装内容

```text
- Saturation
- PastelStrength
- HighSaturationCompress
- PastelLuminanceBias

- CreamHighlightColor
- CreamHighlightStrength
- CreamHighlightThreshold
- CreamHighlightSoftness
```

## 使用ファイル

```text
HLSL/
  ToyDiorama_Pastel.hlsl
  ToyDiorama_Highlight.hlsl
  ToyDiorama_ColorGrade.hlsl
  ToyDiorama_Debug.hlsl
```

## 必須 DebugView

```text
PastelMask
HighSaturationMask
CreamHighlightMask
BeforePastel
AfterPastel
```

## 完了条件

```text
- 高彩度の色だけを自然に丸められる
- 全体を単純に灰色っぽくしない
- 明るい部分をクリーム色に寄せられる
- 白飛びが柔らかくなる
- 赤・青・緑などの強い色が浮きすぎない
- EnvironmentStylizedLitの影色や質感を壊さない
```

## 禁止事項

```text
- RGBを直接量子化しない
- Bayer Ditherを入れない
- PixelSnapを入れない
- Bloomで明部をごまかさない
```

ここで **ToyDioramaPostProcess の基本の絵** が見えてくるべきです。

---

# M5 Edge Tone / Lens Shading

## 目的

黒いビネットではなく、画面端の硬さを取る。
画面端を少し柔らかく、少し色味を整える。

## 実装内容

```text
- EdgeToneEnabled
- EdgeToneColor
- EdgeToneStrength
- EdgeToneRadius
- EdgeToneSoftness
- EdgeSaturationFade
- EdgeBrightnessOffset
```

## 使用ファイル

```text
HLSL/
  ToyDiorama_EdgeTone.hlsl
  ToyDiorama_Debug.hlsl
```

## 必須 DebugView

```text
EdgeMask
BeforeEdgeTone
AfterEdgeTone
```

## 完了条件

```text
- 画面端を黒く潰さない
- 画面端の彩度を弱く落とせる
- 画面端を少し暖色/寒色に寄せられる
- 効果を0にすると完全に無効化される
- 通常プレイで邪魔にならない
```

## 禁止事項

```text
- color *= vignette のような黒乗算を標準にしない
- 画面中央の視認性を落とさない
- 端を過剰に暗くしない
```

## MVP到達点

M5完了時点で、最初の実用最小版として成立する。

```text
MVP:
  - Color Grade
  - Pastel Compression
  - Cream Highlight
  - Edge Tone
  - DebugView
```

この段階で「方向性が違う」と感じるなら、Bloom以降に進む前に必ず調整する。

---

# M6 Depth Haze

## 目的

奥行きを整理する。
遠景を少しだけ空気に溶かし、ジオラマ・模型感を出す。

## 実装内容

```text
- Depth Texture 要求
- Linear Depth 取得
- DepthHazeEnabled
- DepthHazeColor
- DepthHazeStrength
- DepthHazeStart
- DepthHazeEnd
- DepthHazeSaturationFade
- DepthHazeBrightnessLift
```

## 使用ファイル

```text
HLSL/
  ToyDiorama_DepthHaze.hlsl
  ToyDiorama_Debug.hlsl

Runtime/
  ToyDioramaCompositePass.cs
```

## 必須 DebugView

```text
RawDepth
LinearDepth
DepthHazeMask
BeforeDepthHaze
AfterDepthHaze
```

## 完了条件

```text
- Depth Textureがある場合、DepthHazeが効く
- Depth Textureがない場合、安全に無効化される
- 近景はほぼ変わらない
- 遠景だけHazeColorに自然に寄る
- 彩度低下と明度持ち上げを制御できる
- 室内では弱め、屋外では強めに調整できる
```

## 禁止事項

```text
- 霧のように強くしすぎない
- プレイヤー周辺の視認性を落とさない
- Depthがない環境で壊れない
```

Depth Haze は強い武器ですが、やりすぎると一気に「霧フィルター」になります。
標準値は弱めにするべきです。

---

# M7 Clean Grain / Blue Noise

## 目的

画面の硬さを少し取り、バンディングを隠す。
ただし、レトロ風のディザではなく、かなり薄いカメラ由来の粒子感にする。

## 実装内容

```text
- BlueNoiseTex
- GrainEnabled
- GrainStrength
- GrainScale
- GrainResponse
- GrainTemporalStrength
```

## 使用ファイル

```text
HLSL/
  ToyDiorama_Grain.hlsl
  ToyDiorama_Debug.hlsl
```

## 必須 DebugView

```text
Grain
BeforeGrain
AfterGrain
```

## 完了条件

```text
- Bayer Ditherではない
- 粒子が目立ちすぎない
- 暗部・明部でGrain量を制御できる
- Strength 0で完全に無効化される
- 静止画で汚く見えない
- カメラ移動時にチラつきすぎない
```

## 禁止事項

```text
- PS2PostProcessのBayer Ditherを流用しない
- 強いNoiseで質感をごまかさない
- Material側の粘土質感の代わりにしない
```

Grain は最後の薄化粧です。
効果が見えすぎたら失敗です。

---

# M8 Soft Bloom / Halation Pipeline

## 目的

明部をわずかに柔らかくにじませる。
ただし、派手な Bloom ではなく、**弱く広い、おもちゃ撮影的な明部処理** にする。

## 実装内容

```text
- Bright Prefilter
- Downsample
- Blur
- Upsample
- Composite
- SoftBloomEnabled
- SoftBloomThreshold
- SoftBloomSoftKnee
- SoftBloomIntensity
- SoftBloomRadius
- SoftBloomTint

- HalationEnabled
- HalationStrength
- HalationColor
- HalationThreshold
- HalationRadius
```

## 使用ファイル

```text
Shaders/
  ToyDioramaBloom.shader

Runtime/
  ToyDioramaBloomPass.cs
  ToyDioramaRenderTargets.cs
  ToyDioramaCompositePass.cs
```

## 必須 DebugView

```text
BloomPrefilter
BloomBlur
BloomComposite
HalationMask
BeforeBloom
AfterBloom
```

## 完了条件

```text
- BloomをON/OFFできる
- HalationをON/OFFできる
- Bloomが明部を柔らかくするが、画面全体を白くしない
- Halationが暖色にじみとして弱く機能する
- Bloom用RTが解像度変更に追従する
- RTリークがない
- QualityTierでBloom解像度や回数を変えられる
```

## 禁止事項

```text
- 1パスで雑なBlurを作って済ませない
- Bloomを強くしすぎない
- ソシャゲ的なギラギラBloomにしない
- 色設計の弱さをBloomで誤魔化さない
```

M8は大きいMilestoneです。
実装量が増えるため、必要なら以下のように分割してもよい。

```text
M8.1 Bright Prefilter
M8.2 Downsample / Blur
M8.3 Upsample / Composite
M8.4 Halation
M8.5 RT Lifetime / Quality
```

---

# M9 Quality Tier / Preset System

## 目的

実制作で使えるように、品質段階とプリセットを整備する。

## 実装内容

```text
QualityTier:
  Low
  Medium
  High
  Cinematic

Preset:
  SoftToy
  ClayDiorama
  MattePlastic
  PictureBook
  CleanDebug
```

## QualityTier 方針

```text
Low:
  Color Grade
  Pastel
  Cream Highlight
  Edge Tone
  No DepthHaze
  No Bloom
  No Grain

Medium:
  Color Grade
  Pastel
  Cream Highlight
  Edge Tone
  DepthHaze
  Grain
  Simple Bloom

High:
  Medium +
  Better Bloom
  Halation
  More precise masks

Cinematic:
  High +
  Stronger Haze
  Stronger Bloom
  Optional lens softness
```

## 完了条件

```text
- QualityTierを切り替えられる
- Presetを適用できる
- Preset適用後も手動調整できる
- CleanDebugで余計な演出を消せる
- SoftToyが標準として使える
- ClayDioramaがEnvironmentStylizedLitと自然に合う
```

## 禁止事項

```text
- Preset値をRendererFeature内に直書きしない
- Inspectorに大量の未整理プロパティを並べない
- QualityTierと個別設定の関係を曖昧にしない
```

---

# M10 Camera / UI / Injection Policy

## 目的

実際のゲーム画面で、どこに適用するかを確定する。

特に UI への影響を曖昧にすると、後で確実に問題になります。

## 実装内容

```text
- Injection Timing整理
- CameraType判定
- SceneView適用可否
- GameView適用
- UI適用方針
- Preview Camera適用可否
```

## 基本方針

```text
World Space UI:
  適用されてもよい

Screen Space UI:
  原則適用しない

Scene View:
  開発設定でON/OFF可能

Preview Camera:
  原則OFF
```

## 完了条件

```text
- Main Cameraで正しく動く
- SceneViewでON/OFFできる
- UIが読みにくくならない運用方針がある
- Screen Space UIを後段描画するか、Camera分離する方針が決まっている
- URP標準Bloom/ColorAdjustmentsとの二重適用方針が決まっている
```

## 禁止事項

```text
- UI文字をPostProcessで潰す
- URP標準ColorAdjustmentsとToyDioramaのColorGradeを無秩序に併用する
- Cameraごとの適用条件を曖昧にする
```

原則として、ToyDiorama側で ColorGrade / Bloom を持つなら、URP側の同種エフェクトは切るべきです。
二重にかけると調整不能になります。

---

# M11 Performance / RT Lifetime / Cleanup

## 目的

負荷・一時RT・Shader分岐を整理する。

見た目が固まってから最適化する。
最初から最適化を優先しすぎると、表現実験が遅くなります。

## 実装内容

```text
- RT確保/解放の整理
- 解像度変更対応
- Camera変更対応
- Low/Medium/Highの負荷確認
- Bloom downsample回数の制御
- DebugViewの本番無効化方針
- 不要shader keyword整理
```

## 計測対象

```text
- CompositeOnly
- DepthHazeあり
- Grainあり
- Bloomあり
- Halationあり
- High / Cinematic
```

## 完了条件

```text
- 一時RTリークがない
- 解像度変更で破綻しない
- Scene切り替えで破綻しない
- QualityTierごとの負荷差が明確
- Lowは十分軽い
- Highは使用箇所を限定できる
- DebugViewが本番用設定に残りにくい
```

## 禁止事項

```text
- すべての機能を常時ONにしない
- Bloom用RTを無駄に高解像度で持ち続けない
- Debug用分岐を本番で放置しない
```

---

# M12 Production Validation / Authoring Guide

## 目的

実際のゲーム制作で使えるかを検証し、運用ルールを文書化する。

## 作成物

```text
Documentation/
  ToyDioramaPostProcessPropertyReference.md
  ToyDioramaPostProcessAuthoringGuide.md
  ToyDioramaPostProcessTroubleshooting.md
```

## 検証シーン

```text
ToyDiorama_ColorLab
ToyDiorama_DepthLab
ToyDiorama_BloomLab
ToyDiorama_GameplayLab
```

## 検証項目

```text
- 屋内マップ
- 屋外マップ
- ProBuilder壁/床
- EnvironmentStylizedLit使用環境
- 通常URP Lit使用環境
- キャラクター表示
- Screen Space UI
- World Space UI
- Point Lightあり
- Directional Lightあり
- Bloom対象あり
- DepthHazeが効く遠景あり
```

## 完了条件

```text
- 既存PS2PostProcessとは明確に別物の見た目になっている
- 安いレトロフィルター感がない
- EnvironmentStylizedLitの質感を壊さない
- 通常プレイで視認性が落ちない
- Authoring Guideがある
- Troubleshootingがある
- Presetの用途が明確
```

---

# 実装順序

基本はこの順番で固定します。

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
```

特に重要なのは、**M8 Bloom / Halation を M3〜M5 より前にやらないこと**です。

理由：

```text
Bloomは画面を良く見せる力が強い。
しかし、色設計が弱い状態でBloomを入れると、
根本の弱さが見えなくなる。
```

まず M5 までで、Bloomなしでも成立する絵を作るべきです。

---

# 最重要チェックポイント

## Checkpoint A: M1完了時

```text
何もしないPostProcessとして安全に動いているか
```

ここが壊れていると、以後の全てが不安定になります。

---

## Checkpoint B: M3完了時

```text
暗部・中間・明部の色分離が美しいか
```

ToyDioramaPostProcessの基礎品質はここで決まります。

---

## Checkpoint C: M5完了時

```text
Bloomなしでも、玩具・粘土・ジオラマ方向の画面に見えるか
```

ここが弱いなら、Bloomに進まず M3〜M5 をやり直すべきです。

---

## Checkpoint D: M8完了時

```text
Bloom / Halationが上品に効いているか
```

強すぎるBloomは失敗です。
このPostProcessに必要なのは、派手さではなく柔らかさです。

---

## Checkpoint E: M10完了時

```text
UIとCamera適用方針が破綻していないか
```

UIまでポストプロセスで変色すると、ゲームとしての品質が落ちます。
ここは必ず明文化します。

---

# AI実装向けタスク分割

悪い依頼：

```text
ToyDioramaPostProcessを全部実装してください。
```

これは高確率で破綻します。

良い依頼：

```text
M1のみ実装してください。
目的はURP RendererFeature / RenderPassとして、SourceColorをそのままDestinationへ出力するPassthrough PostProcessです。
色補正、Depth、Bloom、DebugViewはまだ実装しないでください。
```

次：

```text
M2のみ実装してください。
Settings BindingとDebugViewの基礎を作ります。
SourceColor / Luminance / UV のDebugViewだけを実装してください。
ColorGrade本体はまだ実装しないでください。
```

次：

```text
M3のみ実装してください。
Exposure / Contrast / BlackLift / WhiteSoftClamp / Shadow-Mid-Highlight Tint を実装します。
Pastel、CreamHighlight、Bloom、DepthHazeはまだ実装しないでください。
```

このように **Milestoneごとに禁止事項まで指定する** のが重要です。

---

# 優先順位

最優先：

```text
1. 安全なRenderPass
2. 色設計
3. 暗部を黒く潰さない
4. 明部を柔らかくする
5. 彩度を上品に丸める
6. 画面端を黒くしない
7. DepthHazeで奥行きを整理する
8. Bloom/Halationを弱く上品に使う
9. UIを壊さない
10. 負荷を制御する
```

優先しないもの：

```text
- PixelSnap
- Bayer Dither
- RGB Quantization
- VHS
- Glitch
- 強いChromatic Aberration
- 強いTilt-Shift
- 強いLens Distortion
```

---

# 最初に作るべきもの

次に実装するなら、まず **M0 → M1** です。

```text
M0:
  ファイル構成と空実装を作る

M1:
  何もしないPassthrough PostProcessをURP上で動かす
```

ここを雑にすると、後の M3 ColorGrade や M8 Bloom で地獄になります。
PostProcessは「画面全部を触る処理」なので、最初に安全な土台を作るべきです。
