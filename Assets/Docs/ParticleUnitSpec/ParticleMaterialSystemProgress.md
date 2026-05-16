# Particle Material System Progress

## 0. 位置づけ

本ファイルは、Particle Material System の現在進捗を保存するためのスナップショット置き場である。

役割は以下の通りに分ける。

```text
ParticleMaterialSystemSpec.md
  設計の正本

ParticleMaterialSystemMilestones.md
  実装計画の正本

ParticleMaterialSystemProgress.md
  実装進捗と現時点ステータスの正本
```

Milestone 文書にも進捗棚卸しはあるが、そちらは計画文書の一部である。本ファイルは、現在地を短く更新し続けるための保存先として使う。

---

## 1. 更新ルール

```text
- 実装の区切りごとに更新する
- planned scope ではなく repo 実体ベースで書く
- 進んだ milestone だけでなく、未着手 / 保留 / block も明記する
- 進捗率は見積もりではなく、成果物ベースで更新する
- milestone 文書の計画を勝手に書き換えず、実体との差分はここに残す
```

---

## 2. 現在の進捗スナップショット

更新日:

```text
2026-05-16
```

進捗サマリー:

```text
- 現在の実装到達点: M15 Quality Tier code path を ParticleUnlit に追加済み
- 既存の参照実装: TrailUnlit 系のみ
- ParticleUnlit の最小 shader source は追加済み
- docs 状態: spec / milestones / progress の 3 点が作成済み
- M1 bootstrapper と scaffold test は追加済み
- folder scaffold は作成済み
- M2 bootstrapper と validation test は追加済み
- bootstrapper の scene 復元漏れを修正済み
- generated material / texture の contract test を強化済み
- M3 ShaderGUI / MaterialValidator / PresetUtility は追加済み
- M3 bootstrapper 拡張と blend preset validation test は追加済み
- ParticleUnlit shader / HLSL に MaskMap / Noise / Dissolve を追加済み
- ParticleUnlit bootstrapper に M4 noise / mask texture と Smoke / Magic material 生成を追加済み
- ParticleUnlit validation test を M4 generated texture / material / scene anchor 契約まで拡張済み
- ParticleUnlit shader / HLSL に EmissionColor / EmissionStrength / EmissionAlphaInfluence を追加済み
- ParticleUnlit bootstrapper に M5 emission material 正規化を追加済み
- ParticleUnlit validation test を M5 emission source / material 契約まで拡張済み
- ParticleUnlit bootstrapper に M6 prefab / preview 再生成を追加済み
- ParticleUnlit bootstrapper に M7 WebGL load-test case 再生成を追加済み
- ParticleUnlit validation test を M7 WebGL load-test / build entry 契約まで拡張済み
- ParticleUnlitWebGlBuildUtility を追加済み
- ParticleUnlit shader / HLSL に `_DebugMode` と M8 debug output helper を追加済み
- ParticleUnlit ShaderGUI / validator / preset / bootstrapper / validation test を M8 debug contract まで拡張済み
- validation scene / generated material / generated texture の実生成は Unity project lock により batch 実行待ち
- ParticleUnlit shader / HLSL に `_UseSoftParticles` / `_SoftParticleDistance` / `_UseCameraFade` / `_CameraFadeNear` / `_CameraFadeFar` と depth-based alpha fade path を追加済み
- ParticleUnlit ShaderGUI / validator / preset / bootstrapper / build utility / validation test を M11 depth interaction contract まで拡張済み
- BombCourier.Particles.EditorTests.csproj のビルドで M11 code path の compile を確認済み
- ParticleLit shader / HLSL に BaseMap / BaseColor / Alpha / Vertex Color / NormalMap / NormalStrength / Smoothness / Metallic / LightInfluence / Emission を持つ最小 lit path を追加済み
- ParticleLitShaderGUI / ParticleLitMaterialValidator / ParticleLitPresetUtility を追加済み
- ParticleUnlitValidationBootstrapper.cs を M12 lit material / texture / Future Lit Test Area anchor 再生成まで拡張済み
- ParticleLitValidationTests.cs を追加し、M12 shader source / validator / preset / generated material / texture importer / scene anchor 契約を監査するようにした
- BombCourier.Particles.EditorTests.csproj のビルドで M12 code path の compile を確認済み
- ParticleDistortion shader / HLSL に Opaque Texture ベースの DistortionMap / Strength / Scale / Scroll / Alpha / Edge Fade / optional Noise path を追加済み
- ParticleDistortionShaderGUI / ParticleDistortionMaterialValidator / ParticleDistortionPresetUtility を追加済み
- ParticleUnlitValidationBootstrapper.cs を M13 distortion texture / material / prefab / Future Distortion Test Area anchor / preview 再生成まで拡張済み
- ParticleDistortionValidationTests.cs を追加し、M13 shader source / validator / preset / generated material / texture importer / prefab / scene anchor 契約と WebGL 対象外境界を監査するようにした
- BombCourier.Particles.EditorTests.csproj のビルドで M13 code path の compile を確認済み
- M14 の docs 正本を Ring / Ground design + scaffold/contract milestone として更新済み
- ParticleUnlitValidationBootstrapper.cs を M14 future Ring / Ground folder depth と prefab candidate naming の source contract まで拡張済み
- ParticleMaterialSystemM1ScaffoldTests.cs を M14 Ring / Ground HLSL / Passes / Editor scaffold 監査まで拡張済み
- ParticleMaterialSystemM14DesignTests.cs を追加し、M14 docs / bootstrapper source / scaffold README 契約を監査するようにした
- ParticleRingUnlit / ParticleGroundUnlit の HLSL / Passes / Editor README scaffold を追加し、tracked repo 実体として残るようにした
- ParticleUnlit shader に hidden property `_QualityTier` を追加済み
- ParticleUnlitQualityTierUtility.cs を追加し、Low / Medium / High の authored / inferred tier contract を実装済み
- ParticleUnlitPresetUtility.cs / ParticleUnlitMaterialValidator.cs / ParticleUnlitShaderGUI.cs を M15 tier authoring / warning / summary 契約まで拡張済み
- ParticleUnlitBuildValidator.cs / ParticleUnlitWebGlBuildUtility.cs を M15 standard WebGL tier policy と quality tier validation asset contract まで拡張済み
- ParticleUnlitValidationBootstrapper.cs を M15 quality tier validation material / scene anchor 再生成まで拡張済み
- ParticleUnlitQualityTierValidationTests.cs を追加し、M15 source / validator / build / generated asset / scene anchor 契約を監査するようにした
- BombCourier.Particles.EditorTests.csproj のビルドで M15 code path の compile を確認済み
- ParticleUnlitValidationBootstrapper.cs に BootstrapM16ReviewHarness を追加し、ParticleMaterialReviewHarness と Ring / Ground placeholder、bright / dark backdrop を scene root standalone harness として再生成できるようにした
- ParticleUnlitBuildValidator.cs / ParticleUnlitWebGlBuildUtility.cs に M16 alias entry を追加し、RunM16WebGlValidationBuild を canonical entry に寄せた
- ParticleMaterialSystemM16HardeningTests.cs を追加し、M16 review harness source / scene contract を監査するようにした
- Assets/Tests/PlayMode/Particles/ に最小 PlayMode smoke asmdef と ParticleMaterialSystemPlayModeSmokeTests.cs を追加し、validation scene の runtime load と representative particle object を確認できるようにした
- ParticleMaterialUsageGuide.md / ParticleMaterialPresetGuide.md / ParticleMaterialPerformanceGuide.md を追加し、usage / preset / performance の運用ガイドを spec から分離した
```

現在 repo に存在する関連資産:

```text
Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemSpec.md
Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemMilestones.md
Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemProgress.md
Assets/Docs/ParticleUnitSpec/ParticleMaterialUsageGuide.md
Assets/Docs/ParticleUnitSpec/ParticleMaterialPresetGuide.md
Assets/Docs/ParticleUnitSpec/ParticleMaterialPerformanceGuide.md
Assets/Docs/ParticleTrailMaterialSpec.md
Assets/Art/Shader/Particles/TrailUnlit/
Assets/Art/Shader/Particles/ParticleUnlit/BC_Particles_ParticleUnlit.shader
Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Input.hlsl
Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Common.hlsl
Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Sampling.hlsl
Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Debug.hlsl
Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Surface.hlsl
Assets/Art/Shader/Particles/ParticleUnlit/HLSL/Passes/ParticleUnlit_ForwardPass.hlsl
Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitQualityTierUtility.cs
Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs
Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitWebGlBuildUtility.cs
Assets/Art/Shader/Particles/ParticleLit/BC_Particles_ParticleLit.shader
Assets/Art/Shader/Particles/ParticleLit/HLSL/ParticleLit_Input.hlsl
Assets/Art/Shader/Particles/ParticleLit/HLSL/ParticleLit_Common.hlsl
Assets/Art/Shader/Particles/ParticleLit/HLSL/ParticleLit_Sampling.hlsl
Assets/Art/Shader/Particles/ParticleLit/HLSL/ParticleLit_Lighting.hlsl
Assets/Art/Shader/Particles/ParticleLit/HLSL/ParticleLit_Surface.hlsl
Assets/Art/Shader/Particles/ParticleLit/HLSL/Passes/ParticleLit_ForwardPass.hlsl
Assets/Art/Shader/Particles/ParticleLit/Editor/ParticleLitShaderGUI.cs
Assets/Art/Shader/Particles/ParticleLit/Editor/ParticleLitMaterialValidator.cs
Assets/Art/Shader/Particles/ParticleLit/Editor/ParticleLitPresetUtility.cs
Assets/Art/Shader/Particles/ParticleDistortion/BC_Particles_ParticleDistortion.shader
Assets/Art/Shader/Particles/ParticleDistortion/HLSL/ParticleDistortion_Input.hlsl
Assets/Art/Shader/Particles/ParticleDistortion/HLSL/ParticleDistortion_Common.hlsl
Assets/Art/Shader/Particles/ParticleDistortion/HLSL/ParticleDistortion_Sampling.hlsl
Assets/Art/Shader/Particles/ParticleDistortion/HLSL/ParticleDistortion_Surface.hlsl
Assets/Art/Shader/Particles/ParticleDistortion/HLSL/Passes/ParticleDistortion_ForwardPass.hlsl
Assets/Art/Shader/Particles/ParticleDistortion/Editor/ParticleDistortionShaderGUI.cs
Assets/Art/Shader/Particles/ParticleDistortion/Editor/ParticleDistortionMaterialValidator.cs
Assets/Art/Shader/Particles/ParticleDistortion/Editor/ParticleDistortionPresetUtility.cs
Assets/Art/Shader/Particles/ParticleRingUnlit/README.md
Assets/Art/Shader/Particles/ParticleRingUnlit/HLSL/README.md
Assets/Art/Shader/Particles/ParticleRingUnlit/HLSL/Passes/README.md
Assets/Art/Shader/Particles/ParticleRingUnlit/Editor/README.md
Assets/Art/Shader/Particles/ParticleGroundUnlit/README.md
Assets/Art/Shader/Particles/ParticleGroundUnlit/HLSL/README.md
Assets/Art/Shader/Particles/ParticleGroundUnlit/HLSL/Passes/README.md
Assets/Art/Shader/Particles/ParticleGroundUnlit/Editor/README.md
Assets/Art/Materials/Particles/Trails/
Assets/Art/Materials/Particles/Lit/
Assets/Art/Materials/Particles/Distortion/
Assets/Art/Textures/Particles/Trails/
Assets/Art/Textures/Particles/Lit/
Assets/Art/Textures/Particles/Distortion/
Assets/Art/Prefab/Particles/Trails/
Assets/Art/Prefab/Particles/Distortion/
Assets/Tests/EditMode/Particles/TrailUnlitValidationTests.cs
Assets/Tests/EditMode/Particles/ParticleMaterialSystemM1ScaffoldTests.cs
Assets/Tests/EditMode/Particles/ParticleMaterialSystemM16HardeningTests.cs
Assets/Tests/EditMode/Particles/ParticleMaterialSystemM14DesignTests.cs
Assets/Tests/EditMode/Particles/ParticleUnlitValidationTests.cs
Assets/Tests/EditMode/Particles/ParticleUnlitQualityTierValidationTests.cs
Assets/Tests/EditMode/Particles/ParticleLitValidationTests.cs
Assets/Tests/EditMode/Particles/ParticleDistortionValidationTests.cs
Assets/Tests/PlayMode/Particles/BombCourier.Particles.PlayModeTests.asmdef
Assets/Tests/PlayMode/Particles/ParticleMaterialSystemPlayModeSmokeTests.cs
```

---

## 3. Milestone Progress

| Milestone | Status | Progress | Notes |
| --- | --- | --- | --- |
| M0 基盤仕様確定 | Complete | 100% | [Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemSpec.md](Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemSpec.md) と [Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemMilestones.md](Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemMilestones.md) が存在し、命名、分離方針、Packing、Custom Data Layout が固定済み。 |
| M1 Folder / Naming / Sample Scene | In Progress | 80% | scaffold folder 群と M1 bootstrapper/test は追加済み。scene 正本は M2 bootstrapper でも生成できる状態だが、Unity project lock により batch 実行は未完了。 |
| M2 ParticleUnlit 最小 Shader | In Progress | 72% | [Assets/Art/Shader/Particles/ParticleUnlit/BC_Particles_ParticleUnlit.shader](Assets/Art/Shader/Particles/ParticleUnlit/BC_Particles_ParticleUnlit.shader) と最小 HLSL 群、M2 bootstrapper 拡張、[Assets/Tests/EditMode/Particles/ParticleUnlitValidationTests.cs](Assets/Tests/EditMode/Particles/ParticleUnlitValidationTests.cs) は追加済み。scene 復元漏れ修正と generated material/texture contract test 強化も完了。generated texture/material/scene anchor の実生成確認だけが Unity project lock により保留。 |
| M3 Blend Mode / Material Preset | In Progress | 80% | [Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitShaderGUI.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitShaderGUI.cs)、[Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitMaterialValidator.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitMaterialValidator.cs)、[Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitPresetUtility.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitPresetUtility.cs)、M3 bootstrapper 拡張、[Assets/Tests/EditMode/Particles/ParticleUnlitValidationTests.cs](Assets/Tests/EditMode/Particles/ParticleUnlitValidationTests.cs) の M3 contract 拡張は追加済み。generated material / scene anchor の Unity 実生成確認だけが未完了。 |
| M4 MaskMap / Noise / Dissolve | In Progress | 80% | ParticleUnlit shader / HLSL に MaskMap / Noise / Dissolve を追加済み。Smoke / Magic 用 generated material、shared noise / mask texture、M4 bootstrapper、M4 validation test も追加済み。Unity editor 上での実生成確認と Test Runner 実行が未完了。 |
| M5 Emission / Glow / Spark | In Progress | 65% | ParticleUnlit shader / HLSL に emission property と MaskMap.g による emission mask を追加済み。Glow / Spark / Magic preset、M5 bootstrapper、M5 validation test 拡張も追加済み。Unity editor 上での実生成確認と Test Runner 実行が未完了。 |
| M6 初期 Prefab 整備 | In Progress | 72% | [Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs) に BootstrapM6PrefabAssets、5 prefab 生成、scene preview 再生成を追加済み。[Assets/Tests/EditMode/Particles/ParticleUnlitValidationTests.cs](Assets/Tests/EditMode/Particles/ParticleUnlitValidationTests.cs) も prefab asset / preview contract まで拡張済み。Unity editor 上での実生成確認と Test Runner 実行が未完了。 |
| M7 WebGL / Lightweight 検証 | In Progress | 68% | [Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs) に BootstrapM7WebGlValidationAssets、WebGL load-test case 再生成、marker 配下の drift cleanup を追加済み。[Assets/Tests/EditMode/Particles/ParticleUnlitValidationTests.cs](Assets/Tests/EditMode/Particles/ParticleUnlitValidationTests.cs) は M7 load-test / build entry / case set 契約まで拡張済み。[Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitWebGlBuildUtility.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitWebGlBuildUtility.cs) と [Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitBuildValidator.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitBuildValidator.cs) も追加済み。Unity editor 上での bootstrap 実行、Unity Test Runner、実 WebGL build 実行が未完了。 |
| M8 Debug View / Inspector 整理 | In Progress | 69% | [Assets/Art/Shader/Particles/ParticleUnlit/BC_Particles_ParticleUnlit.shader](Assets/Art/Shader/Particles/ParticleUnlit/BC_Particles_ParticleUnlit.shader) に `_DebugMode` を追加済み。[Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Debug.hlsl](Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Debug.hlsl) と [Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Surface.hlsl](Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Surface.hlsl) で debug output path を追加済み。[Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitShaderGUI.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitShaderGUI.cs)、[Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitMaterialValidator.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitMaterialValidator.cs)、[Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs)、[Assets/Tests/EditMode/Particles/ParticleUnlitValidationTests.cs](Assets/Tests/EditMode/Particles/ParticleUnlitValidationTests.cs) を M8 契約まで拡張済み。[Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitBuildValidator.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitBuildValidator.cs) には non-development build 向け debug guard も追加済み。Unity editor 上での debug view 目視確認と Unity Test Runner 実行が未完了。 |
| M9 Flipbook / Atlas 基盤 | In Progress | 72% | [Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs) に BootstrapM9FlipbookValidationAssets、4x4 flipbook atlas 生成、tile bleed を抑える padding、Smoke / Magic marker の drift cleanup、flipbook material / prefab / preview 再生成を追加済み。[Assets/Tests/EditMode/Particles/ParticleUnlitValidationTests.cs](Assets/Tests/EditMode/Particles/ParticleUnlitValidationTests.cs) も M9 atlas importer / Texture Sheet Animation / scene preview / padding / marker child set 契約まで拡張済み。[Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitBuildValidator.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitBuildValidator.cs) と [Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitWebGlBuildUtility.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitWebGlBuildUtility.cs) は EnsureM9ValidationAssetsReady へ更新済み。Unity editor 上の flipbook 再生確認と Unity Test Runner / WebGL build 実行が未完了。 |
| M10 Custom Data / Vertex Stream 基盤 | In Progress | 70% | [Assets/Art/Shader/Particles/ParticleUnlit/HLSL/Passes/ParticleUnlit_ForwardPass.hlsl](Assets/Art/Shader/Particles/ParticleUnlit/HLSL/Passes/ParticleUnlit_ForwardPass.hlsl) で Custom1.xyzw を TEXCOORD1 経由で routing し、[Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Surface.hlsl](Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Surface.hlsl) と [Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Debug.hlsl](Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Debug.hlsl) で Dissolve / Emission / Noise Offset の additive delta と Debug 13=Custom1 / 15=UV を実装済み。[Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs) には M10 custom-data prefab / preview、CustomDataModule と active vertex streams 契約、renderer baseline reset を追加済み。[Assets/Tests/EditMode/Particles/ParticleUnlitValidationTests.cs](Assets/Tests/EditMode/Particles/ParticleUnlitValidationTests.cs)、[Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitBuildValidator.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitBuildValidator.cs)、[Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitWebGlBuildUtility.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitWebGlBuildUtility.cs) も EnsureM10ValidationAssetsReady / prefab contract まで更新済み。Unity editor 上の custom-data 再生確認、Unity Test Runner、実 WebGL build 実行は未完了。 |
| M11 Depth Interaction 基盤 | In Progress | 74% | [Assets/Art/Shader/Particles/ParticleUnlit/BC_Particles_ParticleUnlit.shader](Assets/Art/Shader/Particles/ParticleUnlit/BC_Particles_ParticleUnlit.shader)、[Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Common.hlsl](Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Common.hlsl)、[Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Surface.hlsl](Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Surface.hlsl) に Soft Particles / Camera Fade path を追加済み。[Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitShaderGUI.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitShaderGUI.cs)、[Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitMaterialValidator.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitMaterialValidator.cs)、[Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitPresetUtility.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitPresetUtility.cs)、[Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs)、[Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitBuildValidator.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitBuildValidator.cs)、[Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitWebGlBuildUtility.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitWebGlBuildUtility.cs)、[Assets/Tests/EditMode/Particles/ParticleUnlitValidationTests.cs](Assets/Tests/EditMode/Particles/ParticleUnlitValidationTests.cs) も M11 契約まで更新済み。Unity editor 上の bootstrap 実行、ParticleMaterialTestScene 目視確認、Unity Test Runner、実 WebGL build 実行が未完了。 |
| M12 ParticleLit 設計・最小実装 | In Progress | 62% | [Assets/Art/Shader/Particles/ParticleLit/BC_Particles_ParticleLit.shader](Assets/Art/Shader/Particles/ParticleLit/BC_Particles_ParticleLit.shader) と HLSL 分割、[Assets/Art/Shader/Particles/ParticleLit/Editor/ParticleLitShaderGUI.cs](Assets/Art/Shader/Particles/ParticleLit/Editor/ParticleLitShaderGUI.cs)、[Assets/Art/Shader/Particles/ParticleLit/Editor/ParticleLitMaterialValidator.cs](Assets/Art/Shader/Particles/ParticleLit/Editor/ParticleLitMaterialValidator.cs)、[Assets/Art/Shader/Particles/ParticleLit/Editor/ParticleLitPresetUtility.cs](Assets/Art/Shader/Particles/ParticleLit/Editor/ParticleLitPresetUtility.cs)、[Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs) の M12 拡張、[Assets/Tests/EditMode/Particles/ParticleLitValidationTests.cs](Assets/Tests/EditMode/Particles/ParticleLitValidationTests.cs) を追加済み。Unity editor 上の bootstrap 実行、ParticleMaterialTestScene の目視確認、Unity Test Runner 実行は未完了。 |
| M13 ParticleDistortion 設計・最小実装 | In Progress | 60% | [Assets/Art/Shader/Particles/ParticleDistortion/BC_Particles_ParticleDistortion.shader](Assets/Art/Shader/Particles/ParticleDistortion/BC_Particles_ParticleDistortion.shader) と HLSL 分割、[Assets/Art/Shader/Particles/ParticleDistortion/Editor/ParticleDistortionShaderGUI.cs](Assets/Art/Shader/Particles/ParticleDistortion/Editor/ParticleDistortionShaderGUI.cs)、[Assets/Art/Shader/Particles/ParticleDistortion/Editor/ParticleDistortionMaterialValidator.cs](Assets/Art/Shader/Particles/ParticleDistortion/Editor/ParticleDistortionMaterialValidator.cs)、[Assets/Art/Shader/Particles/ParticleDistortion/Editor/ParticleDistortionPresetUtility.cs](Assets/Art/Shader/Particles/ParticleDistortion/Editor/ParticleDistortionPresetUtility.cs)、[Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs) の M13 拡張、[Assets/Tests/EditMode/Particles/ParticleDistortionValidationTests.cs](Assets/Tests/EditMode/Particles/ParticleDistortionValidationTests.cs) を追加済み。Unity editor 上の bootstrap 実行、ParticleMaterialTestScene の目視確認、Unity Test Runner 実行は未完了。 |
| M14 Ring / Ground 系拡張設計 | In Progress | 78% | [Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemMilestones.md](Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemMilestones.md) と [Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemSpec.md](Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemSpec.md) を design + scaffold/contract milestone として更新済み。[Assets/Art/Shader/Particles/ParticleRingUnlit/README.md](Assets/Art/Shader/Particles/ParticleRingUnlit/README.md) と [Assets/Art/Shader/Particles/ParticleGroundUnlit/README.md](Assets/Art/Shader/Particles/ParticleGroundUnlit/README.md)、各 HLSL / Passes / Editor README によって tracked scaffold を repo 実体として固定済み。[Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs) と [Assets/Tests/EditMode/Particles/ParticleMaterialSystemM1ScaffoldTests.cs](Assets/Tests/EditMode/Particles/ParticleMaterialSystemM1ScaffoldTests.cs)、[Assets/Tests/EditMode/Particles/ParticleMaterialSystemM14DesignTests.cs](Assets/Tests/EditMode/Particles/ParticleMaterialSystemM14DesignTests.cs) で future Ring / Ground scaffold 契約を監査する状態まで追加済み。functional shader / generated asset / scene marker は未着手。 |
| M15 Quality Tier / Shader 分離方針 | In Progress | 62% | [Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitQualityTierUtility.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitQualityTierUtility.cs)、[Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitShaderGUI.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitShaderGUI.cs)、[Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitMaterialValidator.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitMaterialValidator.cs)、[Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitBuildValidator.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitBuildValidator.cs)、[Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs)、[Assets/Tests/EditMode/Particles/ParticleUnlitQualityTierValidationTests.cs](Assets/Tests/EditMode/Particles/ParticleUnlitQualityTierValidationTests.cs) を追加し、Low / Medium / High tier authoring と standard WebGL build policy まで code path を固定済み。ParticleUnlitLite / ParticleUnlitAdvanced への shader split と Unity 実行系確認は未完了。 |
| M16 Optimization / Validation / Documentation | In Progress | 36% | [Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs](Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs) に M16 review harness bootstrap を追加済み。[Assets/Tests/EditMode/Particles/ParticleMaterialSystemM16HardeningTests.cs](Assets/Tests/EditMode/Particles/ParticleMaterialSystemM16HardeningTests.cs) と [Assets/Tests/PlayMode/Particles/ParticleMaterialSystemPlayModeSmokeTests.cs](Assets/Tests/PlayMode/Particles/ParticleMaterialSystemPlayModeSmokeTests.cs)、guide docs 3 本を追加済み。Unity Test Runner 実行、actual WebGL build、manual review checklist の消化は未完了。 |

---

## 4. 現在の次アクション

```text
最優先:
  M16 review harness の Unity 実行確認
  particle EditMode / PlayMode / WebGL validation の一巡
  M16 guide docs と actual scene review の差分解消

具体的には:
  - Unity editor 側で ParticleUnlitValidationBootstrapper.BootstrapM16ReviewHarness を実行する
  - ParticleMaterialTestScene.unity の ParticleMaterialReviewHarness に bright / dark backdrop と Ring / Ground placeholder が再生成されることを確認する
  - ParticleUnlitValidationTests / ParticleUnlitQualityTierValidationTests / ParticleLitValidationTests / ParticleDistortionValidationTests / ParticleMaterialSystemM16HardeningTests を Unity Test Runner で通す
  - ParticleMaterialSystemPlayModeSmokeTests を Unity PlayMode Test Runner で通す
  - ParticleUnlitWebGlBuildUtility.RunM16WebGlValidationBuild で standard WebGL build を通す
  - Quality Tier Test Area、Future Lit Test Area、Future Distortion Test Area、ParticleMaterialReviewHarness を横断して manual review checklist を消化する
  - guide docs 3 本の手順が actual scene layout と一致しているか確認し、必要なら wording を詰める
  - ParticleUnlitLite / ParticleUnlitAdvanced split を次 milestone に送るか判断する
```

---

## 5. 更新ログ

### 2026-05-16

```text
- ParticleMaterialSystemSpec.md を追加
- ParticleMaterialSystemMilestones.md を追加
- ParticleMaterialSystemProgress.md を追加
- 命名規約を現行 repo の Assets/Art / Assets/Scenes / Assets/Docs / Assets/Tests に補正
- TrailUnlit を既存参照実装として明記
- Particle 本体用の専用 progress 保存先を新設
- ParticleUnlitValidationBootstrapper.cs を追加
- ParticleMaterialSystemM1ScaffoldTests.cs を追加
- Particle M1 の scaffold folder 群を作成
- Unity project lock により ParticleMaterialTestScene.unity の batch 生成は未完了
- BC_Particles_ParticleUnlit.shader と最小 HLSL 群を追加
- ParticleUnlitValidationBootstrapper.cs を M2 validation asset 生成まで拡張
- ParticleUnlitValidationTests.cs を追加
- BombCourier.Particles.EditorTests.csproj のビルドで M2 code path の compile を確認
- BootstrapM2ValidationAssets 実行後に開いていた scene setup を復元するよう修正
- generated material の render-state と generated texture importer を validation test で検証するよう強化
- ParticleUnlitShaderGUI.cs / ParticleUnlitMaterialValidator.cs / ParticleUnlitPresetUtility.cs を追加
- ParticleUnlit shader に _BlendMode / _QueueOffset と CustomEditor 接続を追加
- ParticleUnlitValidationBootstrapper.cs を M3 blend preset asset 生成まで拡張
- ParticleUnlitValidationTests.cs を M3 blend mode / preset / validator contract まで拡張
- BombCourier.Particles.EditorTests.csproj の再ビルドで M3 code path の compile を確認
- ParticleUnlit shader / HLSL に _MaskMap / _NoiseMap / _DissolveAmount と M4 surface logic を追加
- ParticleUnlitValidationBootstrapper.cs に BootstrapM4NoiseDissolveAssets と M4 generated texture / material / scene anchor 生成を追加
- ParticleUnlitValidationTests.cs を M4 generated texture / material / scene anchor 契約まで拡張
- ParticleUnlit shader / HLSL に _EmissionColor / _EmissionStrength / _EmissionAlphaInfluence と M5 emission surface logic を追加
- ParticleUnlitValidationBootstrapper.cs に BootstrapM5EmissionAssets と Glow / Spark / Magic material の emission 再生成を追加
- ParticleUnlitValidationTests.cs を M5 emission source / material 契約まで拡張
- ParticleUnlitValidationBootstrapper.cs に BootstrapM6PrefabAssets と FX_Particle_* prefab / preview 再生成を追加
- ParticleUnlitValidationTests.cs を M6 prefab asset / preview contract まで拡張
- ParticleUnlitValidationBootstrapper.cs に BootstrapM7WebGlValidationAssets と WebGL load-test case 再生成を追加
- ParticleUnlitValidationTests.cs を M7 WebGL load-test / build entry 契約まで拡張
- ParticleUnlitWebGlBuildUtility.cs を追加
- ParticleUnlitMaterialValidator.cs の lightweight warning を M7 WebGL load-test 前提へ更新
- ParticleUnlitBuildValidator.cs を追加し、WebGL build 前に M7 validation asset を強制する prebuild validator を追加
- ParticleUnlitValidationBootstrapper.cs の WebGL Load Test Area で未知の child を削除する drift cleanup を追加
- ParticleUnlitValidationTests.cs で WebGL Load Test Area の case set と GPU instancing 無効化も監査するよう強化
- ParticleUnlit_Debug.hlsl を追加し、ParticleUnlit に `_DebugMode` 0-12 の debug output path を追加
- ParticleUnlitShaderGUI.cs を Shape / Debug / Optional を含む M8 section order へ再編
- ParticleUnlitMaterialValidator.cs に `_DebugMode` clamp と debug authoring warning を追加
- ParticleUnlitPresetUtility.cs と ParticleUnlitValidationBootstrapper.cs で generated materials の debug default-off を固定
- ParticleUnlitValidationTests.cs を M8 debug source audit / ShaderGUI order / default-off 契約まで拡張
- ParticleUnlitBuildValidator.cs を M8 bootstrap と non-development build 向け debug guard へ拡張
- ParticleUnlitWebGlBuildUtility.cs を EnsureM8ValidationAssetsReady 経由へ更新
- ParticleUnlitValidationBootstrapper.cs に BootstrapM9FlipbookValidationAssets と 4x4 flipbook atlas / material / prefab / preview 再生成を追加
- ParticleUnlitValidationTests.cs を M9 atlas importer / Texture Sheet Animation / scene preview 契約まで拡張
- ParticleUnlitBuildValidator.cs と ParticleUnlitWebGlBuildUtility.cs を EnsureM9ValidationAssetsReady 経由へ更新
- ParticleUnlitValidationBootstrapper.cs の flipbook atlas に tile bleed を抑える padding を追加
- ParticleUnlitValidationBootstrapper.cs の Smoke / Magic marker に preview drift cleanup を追加
- ParticleUnlitValidationTests.cs を flipbook padding と marker child set 契約まで拡張
- ParticleUnlit_ForwardPass.hlsl / ParticleUnlit_Surface.hlsl / ParticleUnlit_Debug.hlsl に Custom1.xyzw routing、additive dissolve / emission delta、noise offset、Debug 13=Custom1 / 15=UV を追加
- ParticleUnlitValidationBootstrapper.cs に BootstrapM10CustomDataValidationAssets、Spark / Magic custom-data prefab・preview、CustomDataModule 設定、Custom1XYZW vertex stream opt-in、renderer baseline stream reset を追加
- ParticleUnlitValidationTests.cs を M10 shader source audit / custom-data prefab / preview / active vertex stream 契約まで拡張
- ParticleUnlitBuildValidator.cs と ParticleUnlitWebGlBuildUtility.cs を EnsureM10ValidationAssetsReady / RunM10WebGlValidationBuild 経由へ更新
- ParticleUnlit shader / HLSL に `_UseSoftParticles` / `_SoftParticleDistance` / `_UseCameraFade` / `_CameraFadeNear` / `_CameraFadeFar` と scene depth / camera distance を使う alpha fade path を追加
- ParticleUnlitShaderGUI.cs に Depth section を追加し、ParticleUnlitMaterialValidator.cs に depth interaction warning、toggle clamp、camera fade near/far 正規化を追加
- ParticleUnlitPresetUtility.cs で shipping material の depth interaction default-off を固定
- ParticleUnlitValidationBootstrapper.cs に BootstrapM11DepthInteractionValidationAssets、Dust / Smoke / Glow validation material、Smoke marker 配下の depth wall、M11 scene anchor を追加
- ParticleUnlitBuildValidator.cs / ParticleUnlitWebGlBuildUtility.cs / ParticleUnlitValidationTests.cs を EnsureM11ValidationAssetsReady / RunM11WebGlValidationBuild / M11 source and scene contract へ更新
- BombCourier.Particles.EditorTests.csproj のビルドで M11 code path の compile を確認
- BC_Particles_ParticleLit.shader と ParticleLit HLSL 分割、ParticleLitShaderGUI.cs / ParticleLitMaterialValidator.cs / ParticleLitPresetUtility.cs を追加
- ParticleUnlitValidationBootstrapper.cs に BootstrapM12ParticleLitValidationAssets、generated lit base/normal texture、Raindrop / Bubble / Debris material、Future Lit Test Area anchor を追加
- ParticleLitValidationTests.cs を追加し、M12 source audit / validator / preset determinism / generated material / scene contract を監査するようにした
- BombCourier.Particles.EditorTests.csproj のビルドで M12 code path の compile を確認
- ParticleMaterialSystemMilestones.md の M14 を design + scaffold/contract milestone として拡張し、family split、reserved property、canonical prefab candidate を明文化
- ParticleMaterialSystemSpec.md に ParticleRingUnlit / ParticleGroundUnlit の canonical future family 名、reserved property、renderer mode、quality tier 注記を追加
- ParticleUnlitValidationBootstrapper.cs に M14 future Ring / Ground HLSL / Passes / Editor scaffold path と source contract comment を追加
- ParticleMaterialSystemM1ScaffoldTests.cs を M14 Ring / Ground subfolder scaffold 監査まで拡張
- ParticleMaterialSystemM14DesignTests.cs を追加し、M14 docs / bootstrapper source / scaffold README 契約を監査するようにした
- ParticleRingUnlit / ParticleGroundUnlit に README scaffold を追加
- ParticleMaterialSystemSpec.md に _EdgeEmissionColor / _EdgeEmissionStrength の reserved property を追加し、M14 milestone との naming drift を解消
- ParticleMaterialSystemM14DesignTests.cs を tracked scaffold README 監査まで強化
- ParticleMaterialSystemProgress.md の M14 進捗率と次アクションを review 後の状態へ更新
- ParticleUnlit の M15 Quality Tier code path、quality tier validation assets、EditMode contract test を追加
- M16 review harness、guide docs、minimal PlayMode smoke を追加
```
