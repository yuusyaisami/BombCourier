# ToyDiorama PostProcess Troubleshooting

## Before Debugging

最初に次の 4 点を確認します。

1. renderer 上で ToyDiorama が active か。
2. settings.Enabled が on か。
3. DebugView が Off か。
4. URP Bloom / ColorAdjustments が project volume で重なっていないか。

validation scene 自体がおかしい時は、まず generator を再実行します。

## No Effect In Game View

### No Effect Symptoms

- Scene View では見えるが Game View で出ない。
- まったく look が変わらない。

### No Effect Typical Causes

- feature が inactive。
- settings.Enabled が off。
- camera type が Game 以外。
- renderer ownership が別の full-screen path に取られている。

### No Effect Fix

- feature inspector の renderer ownership warning を確認します。
- PC_Renderer 上で legacy FullScreenPassRendererFeature が inactive か確認します。
- camera が Preview / Reflection / utility camera になっていないか確認します。

## Build Fails Because Of Debug View

### Debug View Build Symptoms

- non-development build で build preprocessor が止める。

### Debug View Build Cause

- DebugView が Off 以外のまま ship build しようとしています。

### Debug View Build Fix

- feature の DebugView を Off に戻します。
- enabled のまま debug view を残さないよう、build 前 checklist に入れます。

## Screen Space UI Looks Processed

### Screen Space UI Symptoms

- UI text が濁る。
- HUD の色が変わる。

### Screen Space UI Cause

- Screen Space UI が ToyDiorama camera path に入っています。

### Screen Space UI Fix

- Screen Space Overlay を優先します。
- 必要なら ToyDiorama 後段の UI camera に分離します。
- World Space UI は許容対象ですが、重要 UI は overlay 側に寄せます。

## Bloom Is Too Strong Or Too Soft

### Bloom Symptoms

- 画面全体が白っぽい。
- 明部以外まで glow する。

### Bloom Typical Causes

- SoftBloomThreshold が低すぎる。
- SoftBloomIntensity / Radius が高すぎる。
- URP Bloom が別に有効。

### Bloom Fix

- threshold を先に上げます。
- intensity より前に threshold と soft knee を調整します。
- Assets/Settings 配下の VolumeProfile で URP Bloom overlap がないか確認します。

## Halation Does Not Appear

### Halation Symptoms

- bloom はあるのに halation だけ見えない。

### Halation Cause

- QualityTier が High 未満。
- HalationEnabled が off。
- HalationStrength が小さすぎる。

### Halation Fix

- High か Cinematic に上げて再確認します。
- threshold と radius も一緒に見直します。

## Depth Haze Does Not Appear

### Depth Haze Symptoms

- 遠景が変わらない。
- DebugView を切ると効果がわからない。

### Depth Haze Typical Causes

- QualityTier が Low。
- depth texture が不要条件になっている。
- near / far の差が scene 側で足りない。

### Depth Haze Fix

- Low を避けて Medium 以上で確認します。
- 遠景差が出る camera position で確認します。
- haze start / end を scene の奥行きレンジに合わせます。

## Grain Does Not Appear

### Grain Symptoms

- grain を on にしても変化がない。

### Grain Typical Causes

- QualityTier が Low。
- GrainStrength が 0 に近い。
- BlueNoiseTex が null で fallback も取れていない。

### Grain Fix

- Medium 以上で確認します。
- grain strength と scale を一時的に上げて切り分けます。
- fallback blue noise resource が読めているかを確認します。

## Effect Looks Like Cheap Retro Filter

### Cheap Retro Symptoms

- 低解像度フィルターに見える。
- 彩度圧縮より前に glow と haze でごまかしている印象になる。

### Cheap Retro Cause

- Core Color Grade と Tints が固まる前に bloom / haze を盛っています。

### Cheap Retro Fix

- Exposure、Contrast、BlackLift、WhiteSoftClamp を先に決めます。
- Tints、Pastel、Cream Highlight、Edge Tone の順で画の骨格を整えます。
- optional effects は最後に追加します。

## Renderer Ownership Warning Appears

### Renderer Ownership Symptoms

- inspector に renderer ownership warning が出る。

### Renderer Ownership Meaning

- feature 自体が inactive。
- settings.Enabled が off。
- active FullScreenPassRendererFeature が同じ renderer に残っている。

### Renderer Ownership Fix

- canonical renderer は ToyDiorama を single final-look owner にします。
- dormant registration のままにしません。

## Mobile Quality Policy Warning Appears

### Mobile Warning Symptoms

- Mobile_Renderer の inspector で warning が出る。

### Mobile Warning Cause

- Mobile_Renderer 上の ToyDiorama feature で Force Low Quality Tier が off。

### Mobile Warning Fix

- Mobile_Renderer.asset では Force Low Quality Tier を on にします。
- selected preset を MobileOptimized に戻します。
- mobile path は Low runtime budget 固定が前提なので、Medium 以上を authoring で期待しません。

## Project PostProcess Overlap Warning Appears

### Project Overlap Symptoms

- inspector に project overlap warning が出る。

### Project Overlap Cause

- Assets/Settings 配下の VolumeProfile で URP Bloom または ColorAdjustments が active です。

### Project Overlap Fix

- ToyDiorama が Color Grade / Bloom を持つ前提なので、重複側を切ります。

## Useful Commands

validation scene regenerate:

```text
BC.Rendering.Editor.ToyDioramaValidationSceneGenerator.GenerateAllBatch
```

PlayMode smoke:

```powershell
./Tools/Run-UnityTests.ps1 -Platform PlayMode -TestFilter "BC.Rendering.PlayModeTests.ToyDioramaPostProcessPlayModeSmokeTests" -NoGraphics -TimeoutSeconds 900
```

この suite は GameplayLab canonical path に加えて、Mobile_RPAsset override の mobile renderer path も確認します。

EditMode contracts:

```powershell
./Tools/Run-UnityTests.ps1 -Platform EditMode -TestFilter "BC.Rendering.Tests.ToyDioramaPostProcessFeatureTests" -RunSynchronously -NoGraphics -TimeoutSeconds 600
./Tools/Run-UnityTests.ps1 -Platform EditMode -TestFilter "BC.Rendering.Tests.ToyDioramaPostProcessBuildValidatorTests" -RunSynchronously -NoGraphics -TimeoutSeconds 600
./Tools/Run-UnityTests.ps1 -Platform EditMode -TestFilter "BC.Rendering.Tests.ToyDioramaPostProcessPresetAssetTests" -RunSynchronously -NoGraphics -TimeoutSeconds 600
./Tools/Run-UnityTests.ps1 -Platform EditMode -TestFilter "BC.Rendering.Tests.ToyDioramaPostProcessSettingsTests" -RunSynchronously -NoGraphics -TimeoutSeconds 900
```

Settings suite は tier policy regression だけでなく、ToyDiorama shader source の zero-variant audit も兼ねます。
