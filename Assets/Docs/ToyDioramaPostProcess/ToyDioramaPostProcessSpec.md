了解です。
これは既存の `PS2PostProcess` の延長ではなく、**完全に別系統の本命ポストプロセス**として設計します。

以下を **ToyDioramaPostProcess 仕様書 v0.1** とします。

---

# ToyDioramaPostProcess 仕様書 v0.1

## 1. 名称

正式仮称：

```text
BC/PostProcess/ToyDioramaPostProcess
```

内部名：

```text
ToyDioramaPostProcess
```

目的が分かりやすく、今後 `ClayCameraPostProcess` や `SoftMiniaturePostProcess` などに分岐する場合も整理しやすい名前にする。

---

# 2. 目的

`ToyDioramaPostProcess` は、ゲーム画面全体に対して以下の印象を与えるためのポストプロセスである。

```text
小さな玩具・粘土・石膏・マットプラスチックで作られたジオラマを、
柔らかいレンズで撮影したような画面に整える。
```

主目的は以下。

```text
- 画面全体の色味を統一する
- 暗部を黒く潰さない
- 明部を硬い白飛びではなく、柔らかいクリーム色に寄せる
- 強すぎる彩度を丸め、玩具的だが安っぽくない色にする
- 奥行きを薄い空気感で整理する
- 画面端を黒くするのではなく、柔らかくトーン調整する
- 弱いBloom / Halationで明部を少しだけにじませる
```

---

# 3. 非目的

この PostProcess では以下を主目的にしない。

```text
- PS1 / PS2 / レトロゲーム風
- 低解像度化
- 強いディザ
- RGB量子化
- 黒いビネット
- 強い色収差
- 強いTilt-Shift
- 全画面Blurによるごまかし
- Horror / VHS / Glitch 表現
```

既存の `PS2PostProcess` は完全に別枠として残す。
この PostProcess に `PixelSnap` や `Bayer Dither` を標準機能として持ち込まない。

---

# 4. 表現コンセプト

目指す画面は以下。

```text
- 明るいが、白飛びしない
- カラフルだが、彩度が暴れない
- 影は黒ではなく、青灰・紫灰・赤茶に寄る
- 明部は少しクリーム色に柔らかく寄る
- 遠景は少し空気に溶ける
- 画面端は黒く沈めず、少し柔らかくなる
- 全体として「フィルターを乗せた感」ではなく、最初からそういう絵作りに見える
```

方向性としては、

```text
Retro Filter ではなく、
Stylized Camera Grade
```

である。

---

# 5. EnvironmentStylizedLit との役割分担

前に設計した `EnvironmentStylizedLit` とは明確に責務を分ける。

## EnvironmentStylizedLit の責務

```text
- 物体ごとのライト段階化
- 色付き影
- 粘土・石膏・マットプラスチック質感
- World Noise
- Surface Normal
- Specular / Edge Sheen
- AO / Cavity
```

## ToyDioramaPostProcess の責務

```text
- 画面全体の色統一
- 明部の柔らかさ
- 暗部の色味
- 奥行きの空気感
- レンズ的な周辺処理
- 弱いBloom / Halation
- 最終的な画面の完成度調整
```

重要な方針：

```text
質感はMaterial Shaderで作る。
画面の統一感はPostProcessで作る。
```

PostProcessに質感表現まで背負わせない。

---

# 6. 基本構成

`ToyDioramaPostProcess` は単一の1パスShaderだけで完結させない。
本格的に作る場合、少なくとも以下の2系統に分ける。

```text
1. Composite Pass
   色補正、Pastel化、Depth Haze、Edge Tone、Grainを行う

2. Soft Bloom / Halation Pass
   明部抽出、Downsample、Blur、Upsample、Compositeを行う
```

最小版では Composite のみでよい。
本命版では Bloom / Halation を別パスで持つ。

---

# 7. Render Mode

## 7.1 CompositeOnly

軽量モード。

```text
- Color Grade
- Pastel Compression
- Cream Highlight
- Depth Haze
- Edge Tone
- Grain
```

Bloom / Blur 系は使わない。

用途：

```text
- 開発初期
- 低負荷環境
- Debug
- 携帯機向け設定
```

---

## 7.2 Full

標準モード。

```text
- CompositeOnly の全機能
- Soft Bloom
- Halation
- 明部にじみ
```

用途：

```text
- PC版
- スクリーンショット
- 最終絵作り
```

---

## 7.3 Cinematic

演出用。

```text
- Full より Bloom / Haze / Edge Softness を強める
- Miniature DOF を許可
- 操作中ではなく、演出・リザルト・タイトル向け
```

通常プレイ中のデフォルトにはしない。

---

# 8. 処理順

基本処理順は以下。

```text
1. Source Color取得
2. Exposure / Black Lift / White Soft Clamp
3. Shadow / Mid / Highlight Tint
4. Pastel Compression
5. Cream Highlight
6. Depth Haze
7. Soft Bloom / Halation 合成
8. Edge Tone / Lens Shading
9. Grain
10. Final Clamp
11. Debug View
```

重要：

```text
RGB量子化やPixelSnapは処理順に含めない。
```

---

# 9. Color Grade

## 9.1 目的

画面全体の色を、玩具・ジオラマ・粘土系の方向に整える。

単純な `Contrast` と `Saturation` だけでは不足。
暗部・中間・明部を分けて制御する。

## 9.2 Properties

```text
_Exposure
_Contrast
_BlackLift
_WhiteSoftClamp

_ShadowTint
_MidTint
_HighlightTint

_ShadowTintStrength
_MidTintStrength
_HighlightTintStrength
```

## 9.3 基本方針

暗部：

```text
黒に落とさない。
青灰 / 紫灰 / 赤茶に寄せる。
```

中間：

```text
Base Colorの印象を保つ。
```

明部：

```text
真っ白にしない。
クリーム色 / 淡い黄色に寄せる。
```

---

# 10. Pastel Compression

## 10.1 目的

画面を「派手な原色」から「玩具・絵本・粘土的な色」に寄せる。

単純な Saturation Down ではなく、**強すぎる彩度だけを丸める**。

## 10.2 Properties

```text
_Saturation
_PastelStrength
_HighSaturationCompress
_PastelLuminanceBias
```

## 10.3 挙動

```text
低彩度の色:
  ほぼ維持

高彩度の色:
  少し抑える

明るい色:
  パステル寄りにする

暗い色:
  彩度を完全には消さない
```

この処理により、赤・青・緑などの強い色が浮きすぎるのを防ぐ。

---

# 11. Cream Highlight

## 11.1 目的

明部を硬い白飛びにせず、玩具・陶器・粘土のような柔らかい明部にする。

## 11.2 Properties

```text
_CreamHighlightColor
_CreamHighlightStrength
_CreamHighlightThreshold
_CreamHighlightSoftness
```

## 11.3 挙動

明るい部分だけを検出し、少しクリーム色に寄せる。

```text
白:
  完全な白ではなく、少し温度のある白へ

黄色・オレンジ系:
  暖かさを保ちつつ柔らかくする

青・緑系:
  彩度が暴れないように丸める
```

強くしすぎると画面全体が黄ばんで見えるため、デフォルトは弱め。

---

# 12. Soft Bloom

## 12.1 目的

明るい部分をわずかににじませ、硬いCG感を減らす。

通常の派手なBloomではなく、**弱く、広く、柔らかいBloom**を目指す。

## 12.2 Properties

```text
_SoftBloomEnabled
_SoftBloomThreshold
_SoftBloomSoftKnee
_SoftBloomIntensity
_SoftBloomRadius
_SoftBloomTint
```

## 12.3 実装方針

Bloomは1パスで雑に作らない。

本格版では以下の構成にする。

```text
1. Bright Prefilter
2. Downsample
3. Blur
4. Upsample
5. Composite
```

## 12.4 推奨見た目

```text
- 発光しているようには見えすぎない
- 明るい角や床が少し柔らかくなる
- 白い面が少しだけ空気に溶ける
- ソシャゲ的な強いBloomにはしない
```

---

# 13. Halation

## 13.1 目的

強い明部の周囲に、わずかな暖色のにじみを入れる。

Bloomよりも色味寄りの効果。

## 13.2 Properties

```text
_HalationEnabled
_HalationStrength
_HalationColor
_HalationThreshold
_HalationRadius
```

## 13.3 方針

デフォルトではかなり弱くする。

```text
推奨:
  0.02 ～ 0.10

非推奨:
  0.2以上の常時使用
```

強くするとフィルム風・夢風に寄る。
通常プレイでは控えめにする。

---

# 14. Depth Haze

## 14.1 目的

奥行きを整理する。
遠くにあるものを少しだけ空気に溶かし、ジオラマ感を出す。

## 14.2 Properties

```text
_DepthHazeEnabled
_DepthHazeColor
_DepthHazeStrength
_DepthHazeStart
_DepthHazeEnd
_DepthHazeSaturationFade
_DepthHazeBrightnessLift
```

## 14.3 挙動

```text
近景:
  ほぼそのまま

中景:
  少し色が馴染む

遠景:
  HazeColorに寄る
  彩度が少し落ちる
  明度がわずかに持ち上がる
```

## 14.4 注意

狭い室内マップでは弱くする。
強すぎると単に霧っぽくなり、ゲームプレイの視認性が落ちる。

---

# 15. Edge Tone / Lens Shading

## 15.1 目的

黒いビネットではなく、画面端の硬さを取る。

既存のような処理は避ける。

```hlsl
color *= vignette;
```

これは安っぽく見えやすい。

## 15.2 Properties

```text
_EdgeToneEnabled
_EdgeToneColor
_EdgeToneStrength
_EdgeToneRadius
_EdgeToneSoftness
_EdgeSaturationFade
_EdgeBrightnessOffset
```

## 15.3 挙動

画面端を黒くするのではなく、以下を弱く行う。

```text
- 少し彩度を落とす
- 少し暖色または寒色に寄せる
- 少し明度を調整する
```

デフォルトでは、ほぼ気づかない程度にする。

---

# 16. Miniature Lens Softness

## 16.1 目的

ミニチュア撮影のような柔らかさを演出する。

ただし、ゲームプレイ中に強いDOFやTilt-Shiftをかけると操作性が悪くなるため、標準では弱めまたは無効。

## 16.2 Properties

```text
_MiniatureLensEnabled
_EdgeBlurStrength
_CenterSharpness
_FocusDistance
_FocusRange
_DepthBlurStrength
```

## 16.3 方針

通常プレイ：

```text
OFF または 極弱
```

演出・タイトル・リザルト：

```text
ON
```

最初の実装では後回しでよい。

---

# 17. Grain

## 17.1 目的

画面の硬さを取り、微妙な階調のバンディングを隠す。

ただし、表面の粘土感は Material Shader 側で作る。
PostProcess の Grain はカメラ由来の微粒子感として扱う。

## 17.2 Properties

```text
_GrainEnabled
_GrainStrength
_GrainScale
_GrainResponse
_GrainTemporalStrength
_BlueNoiseTex
```

## 17.3 方針

Bayer Dither は使わない。
Blue Noise Texture を使う。

推奨：

```text
GrainStrength = 0.01 ～ 0.04
```

強すぎると汚くなる。

---

# 18. Optional: Luminance Quantize

## 18.1 目的

レトロなRGB量子化ではなく、明度だけを弱く整理する。

## 18.2 Properties

```text
_LuminanceQuantizeEnabled
_LuminanceSteps
_LuminanceQuantizeStrength
_LuminanceQuantizeSmoothness
```

## 18.3 方針

デフォルトOFF。

使う場合も、RGBを直接潰さない。
画面全体を安いフィルターにしないため、明度だけを軽く丸める。

---

# 19. Avoided Features

この PostProcess では標準搭載しない。

```text
_PixelSnap
_BayerDither
_RGBColorSteps
_StrongChromaticAberration
_BlackVignette
_Glitch
_VHSNoise
```

必要な場合は別PostProcessとして作る。

---

# 20. Debug View

Debug View は必須。
ポストプロセスは原因切り分けが難しいため、必ず中間結果を見られるようにする。

## 20.1 Debug Modes

```text
Off
SourceColor
Luminance
ShadowMask
MidMask
HighlightMask
PastelMask
CreamHighlightMask
BloomMask
HalationMask
DepthHazeMask
EdgeMask
Grain
BeforeFinalClamp
FinalColor
```

## 20.2 Properties

```text
_DebugView
_DebugExposure
```

DebugView 中は Inspector に警告を出す。

---

# 21. Quality Tier

## 21.1 Low

```text
- Color Grade
- Pastel Compression
- Cream Highlight
- Edge Tone
- No Bloom
- No Halation
- No Depth Haze
- No Grain
```

用途：

```text
- 低負荷
- 開発中
- 遠景確認
```

---

## 21.2 Medium

```text
- Color Grade
- Pastel Compression
- Cream Highlight
- Depth Haze
- Edge Tone
- Grain
- Simple Bloom
```

標準推奨。

---

## 21.3 High

```text
- Full Color Grade
- Pastel Compression
- Cream Highlight
- Depth Haze
- Soft Bloom
- Halation
- Grain
- Optional Miniature Lens
```

スクリーンショットや最終品質向け。

---

# 22. Preset

## 22.1 Soft Toy

標準プリセット。

```text
用途:
  通常ゲームプレイ

特徴:
  色は明るい
  彩度は少し抑える
  明部は柔らかい
  Bloomは弱め
```

---

## 22.2 Clay Diorama

粘土・石膏寄り。

```text
特徴:
  ShadowTintを青灰・紫灰に寄せる
  CreamHighlightを少し強め
  Saturationは低め
  Grainは極弱
```

---

## 22.3 Matte Plastic

玩具・プラスチック寄り。

```text
特徴:
  彩度はやや高め
  PastelCompressionで原色を丸める
  Bloomは少し強め
  Shadowは黒くしない
```

---

## 22.4 Picture Book

絵本・柔らかい背景向け。

```text
特徴:
  DepthHaze強め
  Saturation低め
  EdgeTone強め
  Halation弱め
```

---

## 22.5 Clean Debug

開発用。

```text
特徴:
  余計な演出を切る
  Color Gradeのみ
  Bloomなし
  Grainなし
```

---

## 22.6 Mobile Optimized

Mobile_Renderer 向け。

```text
特徴:
  core color grade は残す
  EdgeTone は維持する
  DepthHaze / Bloom / Halation / Grain は切る
  runtime は Low tier 前提
```

---

# 23. 実装ファイル構成

推奨構成：

```text
Assets/
  BC/
    Rendering/
      PostProcess/
        ToyDiorama/
          Shaders/
            ToyDioramaPostProcess.shader
            ToyDioramaBloom.shader
            ToyDioramaComposite.shader

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
            ToyDioramaPostProcessPass.cs
            ToyDioramaBloomPass.cs

          Editor/
            ToyDioramaPostProcessEditor.cs
            ToyDioramaPostProcessValidator.cs

          Presets/
            SoftToy.asset
            ClayDiorama.asset
            MattePlastic.asset
            PictureBook.asset
            CleanDebug.asset

          Documentation/
            ToyDioramaPostProcessSpec.md
            ToyDioramaPostProcessPropertyReference.md
            ToyDioramaPostProcessAuthoringGuide.md
```

---

# 24. HLSL分割方針

## ToyDiorama_Input.hlsl

```text
- Material / Settings パラメータ
- Texture / Sampler定義
```

## ToyDiorama_Common.hlsl

```text
- Luminance計算
- Remap
- SafeNormalize
- SmoothMask
- Color Utility
```

## ToyDiorama_ColorGrade.hlsl

```text
- Exposure
- Contrast
- BlackLift
- WhiteSoftClamp
- Shadow/Mid/Highlight Tint
```

## ToyDiorama_Pastel.hlsl

```text
- Saturation調整
- High Saturation Compression
- Pastel化
```

## ToyDiorama_Highlight.hlsl

```text
- Cream Highlight
- Highlight Mask
- Soft White
```

## ToyDiorama_DepthHaze.hlsl

```text
- Depth取得
- Linear Depth化
- Haze Mask
- 遠景色補正
```

## ToyDiorama_EdgeTone.hlsl

```text
- 画面端Mask
- EdgeTone
- Edge Saturation Fade
```

## ToyDiorama_Grain.hlsl

```text
- BlueNoise sampling
- Grain合成
```

## ToyDiorama_Debug.hlsl

```text
- DebugView切り替え
```

---

# 25. Runtime構成

## 25.1 Settings

`ToyDioramaPostProcessSettings` は、全パラメータを保持する。

```text
- Color設定
- Pastel設定
- Highlight設定
- Bloom設定
- DepthHaze設定
- EdgeTone設定
- Grain設定
- Debug設定
- Quality Tier
```

---

## 25.2 Renderer Feature / Pass

PostProcess は RendererFeature / RenderPass として実装する。

責務：

```text
- 必要なTextureを確保
- CameraColorを取得
- Depthが必要ならDepthTextureを要求
- Bloom用一時RTを確保
- Composite Passを実行
- 後片付け
```

注意：

```text
Bloomをやる場合、1枚のShaderだけで無理に済ませない。
```

---

# 26. Injection Timing

描画タイミングは明確にする。

## 標準

```text
Scene rendering後
Built-in PostProcessと二重に色補正しない位置
```

方針：

```text
ToyDioramaPostProcess が Color Grade と Bloom を持つ場合、
URP側のBloom / ColorAdjustmentsは原則OFFにする。
```

二重に Bloom や Color Grade がかかると、調整不能になる。

---

# 27. Alpha / UI 方針

## 27.1 UIへの適用

基本方針：

```text
World Space UI:
  適用されてもよい

Screen Space UI:
  原則適用しない
```

理由：

```text
Screen Space UIまでPostProcessで変色すると、
文字の視認性や色設計が崩れる。
```

## 27.2 対応方針

最初はゲーム画面全体に適用でよい。
最終的には UI Layer を後段描画するか、Camera分離で対応する。

BombCourier の現行実装では以下を採用する。

```text
- PC_Renderer は ToyDiorama を有効化した renderer を canonical とする
- PC_Renderer では Force Low Quality Tier を無効のまま維持する
- Mobile_Renderer は ToyDiorama を有効化した supported fallback renderer とする
- Mobile_Renderer では Force Low Quality Tier を有効にし、MobileOptimized preset を基準にする
- renderer policy 違反は build validator で停止する
- legacy PS2 FullScreenPass は canonical renderer では無効のまま保持する
- SampleScene の Screen Space UI は Screen Space Overlay を維持する
- Screen Space Camera UI が必要になった場合は ToyDiorama 後段の UI camera に分離する
```

---

# 28. 受け入れ基準

## 28.1 見た目

以下を満たすこと。

```text
- 既存PS2PostProcessより明確に高品質
- 低解像度フィルターに見えない
- 黒いビネット感がない
- RGB量子化の安っぽさがない
- 明部が柔らかい
- 暗部が色を持っている
- EnvironmentStylizedLitの質感を壊さない
- ゲーム画面として視認性が落ちない
```

## 28.2 技術

```text
- CompositeOnlyで動く
- FullでBloom / Halationが動く
- DepthHazeはDepthがない場合に安全に無効化できる
- DebugViewで各Maskを確認できる
- Quality Tierを切り替えられる
- Screen Space UIへの影響方針が明確
```

---

# 29. 最初の実装範囲

最初からFullを作らない。
まずは以下だけでよい。

```text
MVP:
  - SourceColor取得
  - Exposure
  - Contrast
  - BlackLift
  - Shadow/Mid/Highlight Tint
  - Pastel Compression
  - Cream Highlight
  - Edge Tone
  - DebugView
```

まだ入れないもの：

```text
- Bloom
- Halation
- DepthHaze
- Grain
- Miniature Lens
```

理由：

```text
最初に色設計を固めないと、
BloomやHazeで良く見えているだけの雑なPostProcessになる。
```

---

# 30. 結論

`ToyDioramaPostProcess` は、既存の `PS2PostProcess` の置き換えではなく、完全に別の本命ポストプロセスとして作る。

方向性はこれ。

```text
Pixel化・ディザ・RGB量子化で古く見せるのではなく、
色・明部・暗部・奥行き・レンズ感で、
玩具・粘土・ジオラマ的な画面に整える。
```

初期実装では、まず **Color Grade / Pastel / Cream Highlight / Edge Tone** だけを作るべきです。
ここが弱いまま Bloom や Depth Haze を足しても、最終的に安っぽいフィルターになります。
