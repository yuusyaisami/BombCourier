# ToyDiorama PostProcess Authoring Guide

## Goal

ToyDioramaPostProcess は cheap retro filter を足すための effect ではありません。
色、明部、暗部、奥行き、レンズ感を整えて、玩具 / ジオラマ / 粘土っぽい画面に寄せるための最終ルックです。

## Canonical Setup

1. canonical renderer は PC_Renderer を使います。
2. Mobile_Renderer を使う場合は selected preset を MobileOptimized にし、Force Low Quality Tier を on にします。
3. PC_Renderer では Force Low Quality Tier を off に保ちます。canonical desktop path で on にすると build validator が停止します。
4. ToyDioramaPostProcessFeature は enabled のまま使います。
5. legacy FullScreenPassRendererFeature は canonical renderer 上で inactive に保ちます。
6. Assets/Settings 配下の VolumeProfile では URP Bloom と ColorAdjustments を実効状態で併用しません。
7. Screen Space UI は Screen Space Overlay を優先し、必要なら ToyDiorama 後段の UI camera に分離します。

## Camera Policy

- Game Camera: 常に対象です。
- Scene View: authoring 用 toggle で opt-in します。
- Preview / Reflection / utility camera: 対象外です。
- World Space UI: camera path に含まれてもよい前提です。
- Screen Space UI: ToyDiorama の後段で描くか、Overlay で外します。

## Recommended Workflow

1. preset を選ぶ。
2. Apply Selected Preset を押して visual values を current settings に展開する。
3. QualityTier を決める。まずは Medium を基準にします。
4. Core Color Grade を固める。
5. Tints と Pastel Compression で素材の方向性を作る。
6. Cream Highlight と Edge Tone で質感を整える。
7. Depth Haze、Bloom、Halation、Grain は最後に足す。
8. Screen Space UI と gameplay readability を確認する。

## Tuning Order

### 1. Core Color Grade

- Exposure で基準露出を決めます。
- Contrast は ESL の面の読みやすさを保てる範囲で上げます。
- BlackLift は暗部の色を殺さない最低限にします。
- WhiteSoftClamp は highlight を痛くしない程度に入れます。

### 2. Tints

- ShadowTint は冷たく、HighlightTint は少し暖かく寄せると安定します。
- MidTint はまず 0 から始め、必要な scene だけ加えます。
- tint strength は色味を足すためであって、白 balance を壊すために使いません。

### 3. Pastel Compression

- Saturation を先に極端に上げず、PastelStrength と HighSaturationCompress で整理します。
- PastelLuminanceBias は塗装っぽさと紙っぽさの分岐に使います。

### 4. Cream Highlight

- 強い発光を作る用途ではなく、硬い白を柔らかく丸める用途です。
- threshold を下げすぎると全体が薄く見えるので、明部だけに効かせます。

### 5. Edge Tone

- edge が見えることより、edge 近傍の彩度と明度がうるさくならないことを優先します。
- radius を広げすぎると線が太って見えます。

### 6. Depth Haze

- 遠景に空気を足す effect です。
- indoor scene では薄く、outdoor scene では near / far の差が見える程度に入れます。
- Low では無効なので、見えない時は QualityTier を先に確認します。

### 7. Bloom And Halation

- bloom は soft に寄せ、threshold で対象を絞ります。
- halation は High 以上でだけ使い、常時強くしません。
- URP Bloom が別で有効なら必ず切ります。

### 8. Grain

- grain は画を汚すためではなく、表面の均一さを少し崩すために使います。
- 現行実装は procedural film grain で、BlueNoiseTex の見た目に依存しません。
- strength は小さく始め、scale と response で見え方を調整します。
- ちらつきが気になるときは temporal strength を下げます。

## Quality Tier Use

QualityTier は effect を強制的に on にするスイッチではありません。
authoring 側で off の effect は、tier を上げても off のままです。

M14 以降、Mobile_Renderer 上の feature instance は Force Low Quality Tier により resolved runtime tier を Low に固定します。
authored settings は mutation されないため、mobile path の確認は runtime resolved tier と topology で見ます。
inspector は authored Quality Tier と resolved runtime tier を別表示するので、mobile path では両方を確認します。

M13 以降、ToyDiorama shader は zero-variant policy を維持します。
perf 差は shader keyword の増減ではなく、tier に応じた runtime pass topology と RT workload で出します。

### Low

- 使う場面: 最低コスト確認、effect 切り分け、低負荷 fallback。
- 期待する見た目: color grade ベースの最低限の ToyDiorama 感。
- 使わない effect: Depth Haze、Bloom、Halation、Grain。
- runtime topology: Bloom raster pass 0、total raster pass 2。

### Medium

- 使う場面: 通常 gameplay の基準。
- 期待する見た目: color grade に空気感と軽い bloom を足した標準品質。
- bloom contract: downsample divisor 4、blur pass pair 1。
- runtime topology: Bloom raster pass 4、total raster pass 6。

### High

- 使う場面: 演出を強めたい gameplay、撮影、見せ場。
- 期待する見た目: bloom の質と halation を含む上位品質。
- bloom contract: downsample divisor 2、blur pass pair 2。
- runtime topology: Bloom raster pass 6、total raster pass 8。

### Cinematic

- 使う場面: final capture、比較検証、限定的な演出。
- bloom contract: downsample divisor 2、blur pass pair 3。
- runtime topology: Bloom raster pass 8、total raster pass 10。
- 注意: strongest blur chain を使うため、常時使用前提ではありません。

## Preset Starting Points

| Preset | Use Case |
| --- | --- |
| SoftToy | 標準の starting point。迷ったらここから始めます。 |
| ClayDiorama | ESL と合わせた彩度寄りの look を作りたい時。 |
| MattePlastic | 表面を少しドライに見せたい gameplay 向け。 |
| PictureBook | コントラスト強めの比較用。常用前提ではなく基準差を見る用途。 |
| CleanDebug | effect を抑えて切り分けたい時。ship 用 preset ではありません。 |
| MobileOptimized | Mobile_Renderer 向け。core stylization を残しつつ secondary runtime effects を切った starting point。 |

## Validation Scenes

| Scene | Path | Primary Goal | Recommended Start |
| --- | --- | --- | --- |
| ColorLab | Assets/Scenes/ToyDiorama/ToyDiorama_ColorLab.unity | preset / tint / pastel balance | SoftToy, Medium |
| DepthLab | Assets/Scenes/ToyDiorama/ToyDiorama_DepthLab.unity | depth haze near / far separation | ClayDiorama, Medium |
| BloomLab | Assets/Scenes/ToyDiorama/ToyDiorama_BloomLab.unity | bloom / halation threshold and radius | SoftToy, High |
| GameplayLab | Assets/Scenes/ToyDiorama/ToyDiorama_GameplayLab.unity | canonical gameplay camera, UI, readability | MattePlastic, Medium |

scene が欠けている場合は、editor utility で再生成します。

```powershell
BC.Rendering.Editor.ToyDioramaValidationSceneGenerator.GenerateAllBatch
```

## Scene-Specific Notes

### Indoor

- ESL_TestRoom 系では BlackLift と shadow tint を先に詰めます。
- bloom を足す前に明部の cream highlight だけで成立するか確認します。

### Outdoor

- ESL_LightingLab 系では Depth Haze と highlight tint のバランスを先に取ります。
- 遠景 haze と bloom が同時に強いと安っぽく見えやすいので、どちらかを主役にします。

### Gameplay

- SampleScene 系では UI readability を最優先に見ます。
- Screen Space Overlay UI は ToyDiorama の外側に保ちます。
- World Space UI は scene 側の重要度を見て許容します。

## Validation Checklist

- Game Camera でのみ effect が出る。
- Scene View は必要時だけ opt-in している。
- Screen Space UI の文字が濁っていない。
- URP Bloom / ColorAdjustments が project settings volume で重なっていない。
- DebugView が Off に戻っている。
- ESL の材質感が flat になっていない。
- cheap retro filter に見える要素が残っていない。

## Automated Checks

最低限の PlayMode smoke は以下で回せます。

この smoke は Assets/Scenes/ToyDiorama/ToyDiorama_GameplayLab.unity を additive load し、Main Camera / UIScreenCanvas / WorldSpaceCanvas / CharacterProxy と canonical renderer path を確認します。
同じ suite で Mobile_RPAsset override を使った mobile renderer path も確認します。

```powershell
./Tools/Run-UnityTests.ps1 -Platform PlayMode -TestFilter "BC.Rendering.PlayModeTests.ToyDioramaPostProcessPlayModeSmokeTests" -NoGraphics -TimeoutSeconds 900
```

validation scene の contract は以下で固定します。

```powershell
./Tools/Run-UnityTests.ps1 -Platform EditMode -TestFilter "BC.Rendering.Tests.ToyDioramaValidationSceneTests" -RunSynchronously -NoGraphics -TimeoutSeconds 900
```

QualityTier の perf policy contract は以下で固定します。

```powershell
./Tools/Run-UnityTests.ps1 -Platform EditMode -TestFilter "BC.Rendering.Tests.ToyDioramaPostProcessSettingsTests" -RunSynchronously -NoGraphics -TimeoutSeconds 900
```

この suite は tier policy だけでなく、ToyDiorama shader source の zero-variant audit も兼ねます。

EditMode の asset / validator contract は既存の ToyDiorama suite も併用します。

```powershell
./Tools/Run-UnityTests.ps1 -Platform EditMode -TestFilter "BC.Rendering.Tests.ToyDioramaPostProcessFeatureTests" -RunSynchronously -NoGraphics -TimeoutSeconds 900
./Tools/Run-UnityTests.ps1 -Platform EditMode -TestFilter "BC.Rendering.Tests.ToyDioramaPostProcessBuildValidatorTests" -RunSynchronously -NoGraphics -TimeoutSeconds 900
./Tools/Run-UnityTests.ps1 -Platform EditMode -TestFilter "BC.Rendering.Tests.ToyDioramaPostProcessPresetAssetTests" -RunSynchronously -NoGraphics -TimeoutSeconds 900
```
