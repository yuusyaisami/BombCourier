# Screen Transition Shader System Spec

## 1. 目的
現在の ImageA / ImageB のアルファ制御によるクロスフェードを廃止し、単一のフルスクリーン合成シェーダーによる画面遷移システムを構築する。

従来方式では、ImageA と ImageB を別々に透明化するため、次の状況で背後の生レンダー結果や Camera Solid Color が露出する。

- A/B の片方または両方に透明ピクセルがある
- Canvas の描画順が崩れる
- 遷移中に Scene / Camera / UI が一時的に未初期化になる
- 黒背景を置いた場合、黒が実際の遷移色として見えてしまう
- UI Image のアルファブレンドが最終画面同士の合成ではなく、背景の上に 2 枚の半透明板を重ねる処理になる

新システムでは、最終的な画面 A と最終的な画面 B をテクスチャとして扱い、1 つのシェーダー内で合成する。

```hlsl
FinalColor = TransitionComposite(FromTexture, ToTexture, Progress, Mode);
FinalAlpha = 1.0;
```

遷移中の出力は常に不透明な最終色であり、背景色、Solid Color、未初期化画面に依存しない。

## 2. 基本方針

### 2.1 やらないこと
以下は禁止する。

- ImageA.color.a と ImageB.color.a を逆方向に動かすだけのクロスフェード
- 黒背景 Image を安全網として置く方式
- Canvas の描画順に依存した遷移
- カメラ背景色を隠すための場当たり的な Solid Color 調整
- 遷移中だけ大量の GameObject を生成・破棄する方式
- Texture が無ければ黒で代用する無音フォールバック

本仕様は AGENTS.md の方針である、正しさ、保守性、明示的設計、静かなフォールバック回避に従う。

### 2.2 採用方式
採用するのは次の 2 層構成。

1. Core Composite Shader
- _TextureFrom
- _TextureTo
- _Progress
- _Mode
- _NoiseTex
- _MaskTex
- その他パラメータを受け取り、最終画面を 1 枚で合成する HLSL シェーダー

2. Screen Transition Runtime
- 現在画面を RenderTexture にキャプチャ
- 新しい画面または指定 Texture を遷移先として扱う
- Shader へ値を渡し、進行度を制御
- Scene 切り替え中も FromTexture を保持して破綻を防止

## 3. システム名と推奨配置
システム名:

```text
BC Screen Transition System
```

推奨配置:

```text
Assets/Docs/ScreenTransitionShaderSpec.md
Assets/Scripts/Rendering/Transition/
Assets/Art/Shader/Transition/
Assets/Art/Materials/Transition/
Assets/Art/Textures/Noise/
Assets/Art/SO/Transition/
```

## 4. 最終アーキテクチャ

```text
ScreenTransitionServiceMB
  ├─ owns transition state
  ├─ owns RenderTexture pool
  ├─ owns active transition request
  ├─ drives progress / easing / timing
  └─ exposes public API

ScreenTransitionRendererFeature
  ├─ captures current camera color into From RT
  ├─ composites FromTexture + active camera color
  ├─ writes final color back into camera color target
  └─ uses RenderGraph / URP pattern

ScreenTransitionProfileSO
  ├─ default duration
  ├─ transition mode
  ├─ easing
  ├─ noise settings
  ├─ distortion settings
  └─ color correction settings

BC_ScreenTransition.shader
  ├─ HLSL direct-written shader
  ├─ texture-to-texture composite
  ├─ camera-color composite
  ├─ dissolve / wipe / glitch / mask modes
  └─ debug visualization modes
```

BombCourier には既に BC.Rendering で ScriptableRendererFeature と RenderGraph を使った実装パターンがある。本機能も activeColorTexture を読み取り、別ターゲットへ書き戻し、cameraColor を差し替える構造を踏襲する。

## 5. レンダリング方式

### 5.1 基本式
最小のクロスフェードは以下。

```hlsl
float t = saturate(_Progress);
float4 from = SampleFrom(uv);
float4 to   = SampleTo(uv);
float4 col  = lerp(from, to, t);
col.a = 1.0;
return col;
```

重要点は、From と To を半透明 Image として描くのではなく、1 ピクセルごとに shader 内で合成して最終色を不透明で出すこと。これにより背後のカメラ背景は原理的に露出しない。

## 6. 状態遷移

```text
Idle
  ↓
CaptureFrom
  ↓
HoldFrom
  ↓
LoadOrPrepareTo
  ↓
Transitioning
  ↓
Complete
  ↓
Idle
```

- Idle: 非遷移状態
- CaptureFrom: 現在の最終画面を RT に保存
- HoldFrom: Scene Load や UI 構築中、From を全画面保持
- LoadOrPrepareTo: 遷移先 Scene/UI/Texture/Camera 準備
- Transitioning: Progress を 0 から 1 に進行
- Complete: 後片付けして Idle へ戻る

CaptureFrom の必須条件:

- キャプチャ対象 Camera が有効
- RT サイズが現在解像度と一致
- キャプチャ完了まで遷移開始しない

## 7. 入力ソース

### 7.1 FromTexture
種類:

```text
RenderTexture capturedCurrentFrame
Texture2D staticTexture
RenderTexture externalSource
```

基本は RenderTexture を使う。

### 7.2 ToTexture
使用モード:

- CameraColor
  - URP activeColorTexture を遷移先として扱う
  - Scene 遷移、ゲーム画面遷移向き
- ExplicitTexture
  - 指定 Texture を遷移先として扱う
  - タイトル背景、メニュー画像、カード演出向き
- CapturedToTexture
  - 構築後 UI/Camera を RT に焼き、遷移先にする
  - 完全な UI 間遷移向き

### 7.3 優先運用
通常の Scene 遷移:

```text
From = 前フレーム最終画面キャプチャ
To   = 新 Scene の active camera color
```

UI 画像切り替え:

```text
From = 画像 A
To   = 画像 B
```

## 8. Shader 仕様

### 8.1 ファイル

```text
Assets/Art/Shader/Transition/BC_ScreenTransition.shader
Assets/Art/Shader/Transition/HLSL/BC_ScreenTransitionCommon.hlsl
Assets/Art/Shader/Transition/HLSL/BC_ScreenTransitionModes.hlsl
```

### 8.2 Shader 名

```text
Hidden/BC/Transition/ScreenTransition
```

ランタイム管理を基本とし、必要ならデバッグ用 Material を作成する。

### 8.3 Properties

```text
_TextureFrom           ("From Texture", 2D)
_TextureTo             ("To Texture", 2D)
_Progress              ("Progress", Range(0, 1))
_Mode                  ("Mode", Int)
_EaseStrength          ("Ease Strength", Range(0, 1))
_Feather               ("Feather", Range(0, 1))
_NoiseTex              ("Noise Texture", 2D)
_NoiseScale            ("Noise Scale", Float)
_NoiseStrength         ("Noise Strength", Range(0, 1))
_Seed                  ("Seed", Float)
_Direction             ("Direction", Vector)
_Center                ("Center", Vector)
_Aspect                ("Aspect", Float)
_DistortionStrength    ("Distortion Strength", Range(0, 1))
_ChromaticAberration   ("Chromatic Aberration", Range(0, 1))
_PixelSize             ("Pixel Size", Float)
_VignetteStrength      ("Vignette Strength", Range(0, 1))
_DebugView             ("Debug View", Int)
```

### 8.4 出力契約
全モード共通で原則:

```hlsl
return float4(finalRgb, 1.0);
```

例外は Material Preview や Debug View のみ。

## 9. 遷移モード

### 9.1 Practical
- 0 LinearCrossFade
- 1 SmoothCrossFade
- 2 GammaCorrectCrossFade
- 3 DirectionalWipe
- 4 RadialWipe
- 5 MaskTextureWipe

### 9.2 Stylized
- 6 BlueNoiseDissolve
- 7 LuminanceDissolve
- 8 DistortionCrossFade
- 9 GlitchSlice
- 10 ChromaticSplit
- 11 PixelateTransition
- 12 VortexTransition

## 10. C# API 仕様

### 10.1 ScreenTransitionServiceMB

```csharp
namespace BC.Rendering.Transition
{
    public sealed class ScreenTransitionServiceMB : MonoBehaviour
    {
        public bool IsTransitioning { get; }
        public float Progress { get; }

        public UniTask PlayAsync(ScreenTransitionRequest request, CancellationToken ct = default);

        public UniTask CaptureCurrentFrameAsync(CancellationToken ct = default);

        public void SetDefaultProfile(ScreenTransitionProfileSO profile);

        public void SkipToEnd();

        public void CancelCurrentTransition(ScreenTransitionCancelMode mode);
    }
}
```

### 10.2 ScreenTransitionRequest

```csharp
public readonly struct ScreenTransitionRequest
{
    public ScreenTransitionProfileSO Profile { get; }
    public Texture ExplicitToTexture { get; }
    public float? OverrideDuration { get; }
    public bool CaptureFromCurrentFrame { get; }
    public bool WaitUntilToReady { get; }
}
```

### 10.3 Cancel Mode

```csharp
public enum ScreenTransitionCancelMode
{
    CompleteImmediately,
    HoldCurrentVisual,
    ReturnToFrom,
    HardStop
}
```

基本は CompleteImmediately。Scene Load 中キャンセルは HoldCurrentVisual を使う。

## 11. Profile 仕様

### 11.1 ScreenTransitionProfileSO

```csharp
[CreateAssetMenu(
    fileName = "ScreenTransitionProfile",
    menuName = "BC/Rendering/Transition/Screen Transition Profile")]
public sealed class ScreenTransitionProfileSO : ScriptableObject
{
    public ScreenTransitionMode mode;
    public float duration;
    public AnimationCurve easing;

    public float feather;
    public Texture2D noiseTexture;
    public float noiseScale;
    public float noiseStrength;

    public Vector2 direction;
    public Vector2 center;

    public float distortionStrength;
    public float chromaticAberration;
    public float pixelSize;
    public float vignetteStrength;

    public bool gammaCorrectBlend;
}
```

### 11.2 Profile 例

```text
Transition_DefaultSmooth.asset
Transition_StageStartRadial.asset
Transition_StageClearWhiteNoise.asset
Transition_GameOverVignette.asset
Transition_GlitchDanger.asset
Transition_PixelRetro.asset
Transition_MemoryDistortion.asset
```

## 12. RenderTexture 管理

### 12.1 原則
毎回 new せず、ScreenTransitionRenderTexturePool で再利用する。
一致キー:

```text
width
height
graphicsFormat
msaa
depthBufferBits
useMipMap
sRGB
```

### 12.2 再作成条件
- GameView 解像度変更
- Fullscreen 切り替え
- Render Scale 変更
- URP Asset の Color Format 変更
- HDR 有効/無効変更

### 12.3 解放タイミング
- OnDisable
- OnDestroy
- Service shutdown
- Graphics device reset

## 13. UI との関係

### 13.1 重要制約
Screen Space - Overlay Canvas は Camera 後描画のため、RendererFeature 合成対象外になりうる。

遷移対象 UI は次を推奨:

```text
Screen Space - Camera
または
World Space UI
```

代替案:
Transition 用最上位 Canvas に RawImage + BC_ScreenTransition.shader を置き、From と To の両方を明示テクスチャで渡す。

### 13.2 結論
- ゲーム画面、ステージ画面遷移: RendererFeature 方式
- タイトル背景、メニュー背景、立ち絵差し替え: Texture-to-Texture UI Composite 方式

## 14. 失敗時挙動
黒表示はフォールバックではなく事故として扱う。

### 14.1 FromTexture が無い
- Editor / Development: Error log を出して transition aborted
- Release: last valid frame があれば使用。無ければ transition を行わず即時表示

### 14.2 ToTexture 未準備
- Progress を 0 固定
- FromTexture を保持
- ToReady 後に進行開始

### 14.3 Shader / Material が無い
- Error log
- 遷移を実行しない

## 15. 品質要件

### 15.1 視覚品質
必須:

- 背景 Solid Color が一瞬も見えない
- 黒背景不要
- 透明ピクセルを含む画像でも破綻しない
- 画面全体を常に覆う
- 解像度変更で伸びやズレが出ない
- Linear / Gamma 差で極端な暗化が出ない
- Scene Load タイミング差でちらつかない

### 15.2 パフォーマンス目標

```text
Full HD: 1 full-screen pass
通常: from 1 sample + to 1 sample
Noise mode: +1 sample
Distortion mode: +2 から 4 samples
Glitch mode: 分岐を抑える
```

禁止:

- 毎フレーム RenderTexture 作成
- 毎フレーム Material 作成
- 毎フレーム Texture2D 生成
- CPU ピクセル処理
- 大量 UI Image 重ね

## 16. デバッグ機能

### 16.1 Debug View

```text
0 Final
1 FromTexture
2 ToTexture
3 TransitionMask
4 Noise
5 UV
6 Difference
```

### 16.2 Runtime Debug Overlay
任意実装: ScreenTransitionDebugOverlayMB
表示項目:

```text
state
progress
mode
from texture size
to texture source
duration
renderer feature active
last capture frame
```

## 17. テスト項目

### 17.1 Visual Test

- 透明 PNG A から 透明 PNG B
- 黒背景なし Scene から 別 Scene
- Solid Color 赤 Scene から 青 Scene
- ロード中 To が 30 frame 遅延
- 解像度変更後遷移
- Time.timeScale = 0
- 連続遷移
- 遷移中キャンセル
- 遷移中 Scene Load 失敗

### 17.2 Pixel Validation
可能なら Editor Test で検証:

- 遷移中最終出力 alpha = 1.0
- 禁止色露出なし (例: Solid Color Magenta)
- Progress 0 で From 一致
- Progress 1 で To 一致

### 17.3 Performance Validation

- Profiler で GC Alloc 0
- Material instance 増殖なし
- RenderTexture leak なし
- Frame Debugger で pass 数確認

## 18. 実装フェーズ

### Phase 1: 最小完成版
- BC_ScreenTransition.shader
- ScreenTransitionServiceMB
- ScreenTransitionRendererFeature
- ScreenTransitionProfileSO
- LinearCrossFade
- SmoothCrossFade
- DirectionalWipe
- RadialWipe
- BlueNoiseDissolve

### Phase 2: Scene Load 対応
- CaptureFrom
- HoldFromDuringLoad
- ToReady 判定
- SceneManager integration
- UniTask async API

### Phase 3: 高品質演出
- LuminanceDissolve
- MaskTextureWipe
- DistortionCrossFade
- GlitchSlice
- ChromaticSplit
- PixelateTransition

### Phase 4: デバッグと検証
- Debug View
- Debug Overlay
- Editor test scene
- Profile presets
- Frame Debugger validation guide

### Phase 5: 旧方式撤去
削除対象:

- ImageA/ImageB 直接クロスフェード制御
- 黒背景による誤魔化し
- Canvas 依存の場当たり Fade Controller

## 19. 受け入れ条件
以下を満たしたら完成:

1. 黒背景なしで自然なクロスフェードができる
2. Camera Solid Color が遷移中に見えない
3. 透明画像同士でも背景漏れしない
4. Scene Load 中も FromTexture を保持できる
5. RendererFeature と UI Texture Composite 両方で使える
6. HLSL 直書き shader で複数モードを持つ
7. GC Alloc が発生しない
8. Material / RenderTexture leak がない
9. ProfileSO で演出をデータ化できる
10. 旧 ImageA/ImageB 方式を置き換え可能

## 20. 最終判断
今回の解は、UI Image を 2 枚重ねる設計の延命ではない。

BombCourier は URP / RenderGraph ベースの描画拡張を既に持つため、ScreenTransitionRendererFeature を中心に据える。
1 枚の最終合成 shader が FromTexture と ToTexture を読み、Progress と Mode に応じて最終画面を不透明で出力する。

この方針により、黒背景、背後の生背景、Camera Solid Color の混入を原理的に排除できる。

---

## 21. 小さな Milestone

### M0: 仕様固定とアセット土台
目標:
- 仕様書の確定
- フォルダと命名の統一

完了条件:
- 本ドキュメントをレビュー合意
- Assets/Art/Shader/Transition 配下の初期構成を作成
- Assets/Art/SO/Transition 配下の配置規則を決定

### M1: 最小レンダリング動作
目標:
- LinearCrossFade だけで遷移を成立

完了条件:
- ScreenTransitionRendererFeature が cameraColor を置換できる
- Progress 0 は From、1 は To で一致
- Solid Color 露出なし

### M2: ランタイム API 最小版
目標:
- ScreenTransitionServiceMB から 1 本の遷移を再生

完了条件:
- PlayAsync が単発遷移を実行
- IsTransitioning と Progress が正しく更新
- キャンセル CompleteImmediately が動作

### M3: Scene Load 耐性
目標:
- CaptureFrom と HoldFrom を実装

完了条件:
- To 未準備時に Progress を 0 固定
- Scene 切替中に黒や未初期化画面が出ない
- ToReady 後に遷移再開

### M4: 実運用の最低演出
目標:
- SmoothCrossFade、DirectionalWipe、RadialWipe、BlueNoiseDissolve を追加

完了条件:
- ProfileSO でモード切替可能
- 各モードで alpha=1.0 契約を維持
- Profiler で明確な GC Alloc 増加なし

### M5: 検証と旧方式の置換準備
目標:
- 旧 ImageA/ImageB 方式を差し替える判断材料をそろえる

完了条件:
- Visual Test の必須ケースを通過
- 既存 Fade Controller 依存箇所を一覧化
- 置換手順ドラフトを別ドキュメント化
