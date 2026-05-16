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
- 現在の実装到達点: M2 最小 shader 実装中
- 既存の参照実装: TrailUnlit 系のみ
- ParticleUnlit の最小 shader source は追加済み
- docs 状態: spec / milestones / progress の 3 点が作成済み
- M1 bootstrapper と scaffold test は追加済み
- folder scaffold は作成済み
- M2 bootstrapper と validation test は追加済み
- bootstrapper の scene 復元漏れを修正済み
- generated material / texture の contract test を強化済み
- validation scene / generated material / generated texture の実生成は Unity project lock により batch 実行待ち
```

現在 repo に存在する関連資産:

```text
Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemSpec.md
Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemMilestones.md
Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemProgress.md
Assets/Docs/ParticleTrailMaterialSpec.md
Assets/Art/Shader/Particles/TrailUnlit/
Assets/Art/Shader/Particles/ParticleUnlit/BC_Particles_ParticleUnlit.shader
Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Input.hlsl
Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Common.hlsl
Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Sampling.hlsl
Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Surface.hlsl
Assets/Art/Shader/Particles/ParticleUnlit/HLSL/Passes/ParticleUnlit_ForwardPass.hlsl
Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs
Assets/Art/Materials/Particles/Trails/
Assets/Art/Textures/Particles/Trails/
Assets/Art/Prefab/Particles/Trails/
Assets/Tests/EditMode/Particles/TrailUnlitValidationTests.cs
Assets/Tests/EditMode/Particles/ParticleMaterialSystemM1ScaffoldTests.cs
Assets/Tests/EditMode/Particles/ParticleUnlitValidationTests.cs
```

---

## 3. Milestone Progress

| Milestone | Status | Progress | Notes |
| --- | --- | --- | --- |
| M0 基盤仕様確定 | Complete | 100% | [Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemSpec.md](Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemSpec.md) と [Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemMilestones.md](Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemMilestones.md) が存在し、命名、分離方針、Packing、Custom Data Layout が固定済み。 |
| M1 Folder / Naming / Sample Scene | In Progress | 80% | scaffold folder 群と M1 bootstrapper/test は追加済み。scene 正本は M2 bootstrapper でも生成できる状態だが、Unity project lock により batch 実行は未完了。 |
| M2 ParticleUnlit 最小 Shader | In Progress | 72% | [Assets/Art/Shader/Particles/ParticleUnlit/BC_Particles_ParticleUnlit.shader](Assets/Art/Shader/Particles/ParticleUnlit/BC_Particles_ParticleUnlit.shader) と最小 HLSL 群、M2 bootstrapper 拡張、[Assets/Tests/EditMode/Particles/ParticleUnlitValidationTests.cs](Assets/Tests/EditMode/Particles/ParticleUnlitValidationTests.cs) は追加済み。scene 復元漏れ修正と generated material/texture contract test 強化も完了。generated texture/material/scene anchor の実生成確認だけが Unity project lock により保留。 |
| M3 Blend Mode / Material Preset | Planned | 0% | ParticleUnlit 用 ShaderGUI / Validator / PresetUtility は未実装。 |
| M4 MaskMap / Noise / Dissolve | Planned | 0% | 粒子本体用の Mask / Noise / Material 群は未作成。 |
| M5 Emission / Glow / Spark | Planned | 0% | 未着手。 |
| M6 初期 Prefab 整備 | Planned | 0% | FX_Particle_* Prefab は未作成。 |
| M7 WebGL / Lightweight 検証 | Planned | 0% | 未着手。 |
| M8 Debug View / Inspector 整理 | Planned | 0% | 未着手。 |
| M9 Flipbook / Atlas 基盤 | Planned | 0% | 未着手。 |
| M10 Custom Data / Vertex Stream 基盤 | Planned | 0% | 未着手。 |
| M11 Depth Interaction 基盤 | Planned | 0% | 未着手。 |
| M12 ParticleLit 設計・最小実装 | Planned | 0% | 未着手。 |
| M13 ParticleDistortion 設計・最小実装 | Planned | 0% | 未着手。 |
| M14 Ring / Ground 系拡張設計 | Planned | 0% | 未着手。 |
| M15 Quality Tier / Shader 分離方針 | Planned | 0% | 未着手。 |
| M16 Optimization / Validation / Documentation | Planned | 0% | 未着手。 |

---

## 4. 現在の次アクション

```text
最優先:
  M1/M2 の generated asset 実生成完了

具体的には:
  - Unity editor 側で ParticleUnlitValidationBootstrapper.BootstrapM2ValidationAssets を実行する
  - ParticleMaterialTestScene.unity と M_Particle_Test_Alpha.mat と T_Particle_TestSoftSprite.png を生成する
  - ParticleUnlitValidationTests を Unity Test Runner で通す
  - 完了後に M1 と M2 の進捗率を更新する
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
```
