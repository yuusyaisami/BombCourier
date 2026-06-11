# Release Code Review Milestone

## 0. 目的

この文書は、リリース前にゲーム全体のコードを妥協なくレビューするためのマイルストーン定義である。

目的は単なるコード確認ではない。以下を、セクション単位で検出し、修正し、証跡を残す。

```text
- 潜在的なバグ
- 仕様と実装のずれ
- ライフサイクル、非同期、リソース解放、シーン遷移の破綻
- 例外や失敗の握りつぶし
- コメント不足、誤解を招くコメント、説明不足の複雑ロジック
- 可読性の低い命名、巨大メソッド、責務混在
- コード規約、命名規則、フォルダ規約、テスト方針の不統一
- public GitHub repository として読まれたときに迷う箇所
- リリースビルド、Addressables、Localization、URP、Input、Audio、Scene 設定の事故要因
```

このマイルストーンは、レビューそのものの実施計画である。
各セクションの完了時は、本書の `Milestone Progress` と対象セクションの `完了通知` を更新する。

---

## 0.1 現在の棚卸し

この節は、2026-06-11 時点でリポジトリを走査した結果に基づく。

```text
- Unity Editor: 6000.4.6f1
- 主コード範囲: Assets/Scripts, Assets/BC, Assets/Art/Shader, Assets/Tests
- 対象コード/シェーダーファイル数: 約637
- C# 行数: 約124,594
- shader / hlsl / cginc 行数: 約7,134
- 合計行数: 約131,728
- 主要テスト: Assets/Tests/EditMode, Assets/Tests/PlayMode
- テスト実行補助: Tools/Run-UnityTests.ps1
```

主要なコード分布は以下である。

| Area | Approx. files | Notes |
| --- | ---: | --- |
| Assets/Scripts/Editor | 80 | Editor foundation、Action editor、Gizmo、Drawer、migration/validator。 |
| Assets/Scripts/Utility | 61 | ReactiveValue、ValueStore、Wiring、SpriteAnimation、shared utility。 |
| Assets/Scripts/UI | 57 | HUD、Title、Setting、Talk、Toast、Tooltip、UI effects。 |
| Assets/Scripts/Action | 48 | Action authoring/runtime、talk/action orchestration。 |
| Assets/Scripts/Base | 38 | Kernel、Entity、Event、Loading、Localization。 |
| Assets/Scripts/Movement | 35 | Motor、ground/step/contact/support/cushion pipeline。 |
| Assets/Scripts/Gimmick | 24 | MovingPlatform、BreakableGate、PressurePlate、Cushion など。 |
| Assets/Scripts/Player | 15 | Player data、movement、camera、interaction、audio。 |
| Assets/Scripts/Managers | 15 | Game flow、UI、Talk、Input、Settings、Stage など。 |
| Assets/BC/Rendering | 26 | ToyDiorama post process runtime/editor/shader。 |
| Assets/Art/Shader | 85 | EnvironmentStylizedLit、Particles、Transition、UI、Hologram、EndingBackground。 |
| Assets/Tests/EditMode | 49 | Editor/contract/validation tests。 |
| Assets/Tests/PlayMode | 33 | Gameplay/runtime smoke/regression tests。 |

既知のローカル確認制約 (2026-06-11 M0 で再確認):

```text
- Git LFS 3.7.0 が利用可能で、git status / LFS 差分確認は正常に動作する。
  本書初版の「git-lfs が見つからず git status が失敗」は本環境では解消済み (当該記述は非規範)。
- Unity Editor 6000.4.6f1 がローカルに存在し、Tools/Run-UnityTests.ps1 でのバッチ実行が可能。
  ただし PlayMode は -RunSynchronously 不可、実行前に Unity Editor を閉じる必要がある。
- LFS は主にテクスチャ等の大容量資産に使用。Player ビルド成果物は M1 で追跡対象から除外した。
```

---

## 0.2 レビューの基本方針

レビューは「フォルダを眺める」ではなく、各セクションで以下を必ず行う。

```text
1. 関連仕様、Docs、既存テスト、近傍コードを読む
2. 実装の所有者、ライフタイム、失敗時挙動、データ所有を明確にする
3. 潜在バグを優先して読む
4. 失敗を握りつぶす fallback、catch、null skip、Debug.Log だけの継続を確認する
5. async / cancellation / lifecycle / scene unload / destroy 後参照を確認する
6. コメントが必要な複雑ロジック、逆に不要または誤解を招くコメントを洗い出す
7. public repository として読める命名・コメント・Docs になっているか確認する
8. 既存テストの有無を確認し、不足するテストを追加または明記する
9. 修正後に最小の関連テストを実行する
10. セクション完了時に本書へ結果を記録する
```

コメント方針:

```text
- コメントは多めに入れてよいが、処理の逐語説明ではなく、設計理由・不変条件・失敗時の意図を書く
- 複雑なゲームロジック、物理補正、状態遷移、Action step、Editor migration には読み手向けの文脈を残す
- コメントで曖昧なコードを隠さない。命名や分割で読めるものはコード側を直す
- public GitHub 上で、初見の開発者が「なぜそうするか」を追える状態を完了条件にする
```

---

## 0.3 共通チェックリスト

各セクションは、最低限このチェックリストを通過してから Complete にする。

```text
- Active Docs / Spec と実装が矛盾していない
- 失敗時に silent success / silent skip になっていない
- null、未設定参照、破棄済み Object、scene unload の扱いが明確
- async 処理が cancellation と例外を伝播または診断している
- Unity lifecycle の Awake / OnEnable / Start / Update / OnDisable / OnDestroy の順序依存が明確
- public contract、serialized field、ScriptableObject、scene/prefab 参照の互換性を壊していない
- Update / FixedUpdate / rendering path に不要な allocation、探索、文字列処理がない
- Debug.Log 系が本番で過剰または必要な診断不足になっていない
- TODO / FIXME / HACK / NotImplemented / 一時実装が残っていない、または release blocker として記録済み
- コメントが必要なロジックに rationale / invariant がある
- 既存テストで守られている範囲と、追加すべきテスト範囲が明確
- 関連 EditMode / PlayMode / validation / build check の実行結果を記録した
```

推奨する機械的な初期検索:

```text
rg -n "TODO|FIXME|HACK|XXX|NotImplemented|throw new NotImplementedException" Assets/Scripts Assets/BC Assets/Art/Shader Assets/Tests
rg -n "catch \\(|catch\\(" Assets/Scripts Assets/BC Assets/Art/Shader Assets/Tests
rg -n "Debug\\.Log|Debug\\.LogWarning|Debug\\.LogError|Debug\\.LogException" Assets/Scripts Assets/BC Assets/Art/Shader
rg -n "async void|UniTaskVoid|Forget\\(|CancellationToken.None" Assets/Scripts Assets/BC
rg -n "FindObjectOfType|FindFirstObjectByType|GameObject\\.Find|Resources\\.Load|DontDestroyOnLoad" Assets/Scripts Assets/BC
rg -n "Update\\(|FixedUpdate\\(|LateUpdate\\(" Assets/Scripts Assets/BC
```

---

## 0.4 Milestone Progress

| Milestone | Section | Status | Progress | 完了通知 |
| --- | --- | --- | ---: | --- |
| M0 | Review Protocol / Baseline | Complete | 100% | 2026-06-11: protocol+baseline 確定 / 環境再確認 (M0 完了通知) |
| M1 | Build, Repository, Project Settings | Complete | 100% | 2026-06-11: settings 検証 + 約410MB 生成物を untrack/gitignore (M1 完了通知) |
| M2 | Kernel / Service Wiring / Managers | Complete | 100% | 2026-06-11: 8 ファイル修正、batchmode は WIP 安定後 (M2 完了通知) |
| M3 | Action / Talk / Tutorial Orchestration | Complete | 100% | 2026-06-11: Talk cancellation propagation fixed + PlayMode 18/18 (M3 完了通知) |
| M4 | ValueStore / ReactiveValue / Events / Registries | Complete | 100% | 2026-06-11: invalid EntityRef/event/kernel value fixes + EditMode 52/52 + PlayMode 4/4 (M4 完了通知) |
| M5 | Player Movement / Physics / Camera Coupling | Complete | 100% | 2026-06-11: camera source/motor correction/ragdoll order fixes + PlayMode 18/18 (M5 完了通知) |
| M6 | Carry, Bomb, Items, Interaction | Complete | 100% | 2026-06-11: bomb/carry/UI/interaction lifetime fixes + PlayMode 18/18 (M6 完了通知) |
| M7 | Gimmicks / Stage Objects | Planned | 0% | 未通知 |
| M8 | Entity / NPC / Animation / Face / Effects | Planned | 0% | 未通知 |
| M9 | Stage Progression / Snapshot / Loading / Save-like State | Planned | 0% | 未通知 |
| M10 | UI / HUD / Title / Settings / Navigation | Planned | 0% | 未通知 |
| M11 | Audio / Input / Localization Runtime | Planned | 0% | 未通知 |
| M12 | Rendering / Shaders / Post Process / Particles | Planned | 0% | 未通知 |
| M13 | Editor Tools / Authoring / Validation | Planned | 0% | 未通知 |
| M14 | Scenes / Prefabs / ScriptableObjects / Addressables / Localization Assets | Planned | 0% | 未通知 |
| M15 | Test Coverage / Test Quality / CI Commands | Planned | 0% | 未通知 |
| M16 | Performance / Diagnostics / Release Hardening | Planned | 0% | 未通知 |
| M17 | Documentation / Comments / Public Readability | Planned | 0% | 未通知 |
| M18 | Final Release Gate / Triage Closure | Planned | 0% | 未通知 |

Status は `Planned`, `In Progress`, `Blocked`, `Complete` のいずれかにする。
完了通知には、完了日、担当、実行コマンド、残リスクへのリンクまたは要約を記録する。

---

# M0 Review Protocol / Baseline

## 目的

レビュー全体の基準を固定する。
後続セクションで判断がぶれないように、分類、証跡、完了条件、修正方針を先に決める。

## 対象

```text
Agents.md
Assets/Docs/**
Tools/Run-UnityTests.ps1
Packages/manifest.json
ProjectSettings/ProjectVersion.txt
既存の TestResults_*.xml / Logs / build logs
```

## 作業内容

```text
- Active Docs と historical / generated / stale log を分ける
- Finding の severity を定義する
- セクションごとの review note / fix note の書式を決める
- 既存テストコマンドと Unity 実行制約を確認する
- Git LFS、Unity Editor、platform build、CI など環境前提を明記する
```

## Finding 分類

| Severity | 意味 | リリース判断 |
| --- | --- | --- |
| Release Blocker | data loss、進行不能、起動不能、ビルド不能、重大な state corruption。 | 修正必須。 |
| Must Fix | 高確率のバグ、仕様違反、例外握りつぶし、重要テスト不足。 | 原則修正必須。 |
| Should Fix | 可読性、診断性、保守性、将来バグの温床。 | リリース前修正を推奨。 |
| Comment / Docs | public repo として説明不足、仕様と実装の導線不足。 | 原則セクション内で改善。 |
| Optional | 明確な改善だがリリース安全性に直結しない。 | backlog 化可。 |

## 完了条件

```text
- レビュー記録の書式が確定している
- 共通チェックリストが最新化されている
- 実行可能なテストコマンドが確認されている
- 環境制約が明記されている
```

## 完了通知

```text
Status: Complete
Completed at: 2026-06-11
担当: Claude (Opus 4.8)
Evidence:
- Finding 分類 (Release Blocker / Must Fix / Should Fix / Comment-Docs / Optional) と記録書式 (付録A) を採用・運用。
- 一次規範として Agents.md を確認 (clear failure over silent fallback / no swallowed exceptions / explicit lifetime & cancellation 等)。
- テストコマンド: Tools/Run-UnityTests.ps1 を読解。zero-tests-collected を failed 扱いし、timeout / 欠落 XML / 非0 exit を分類する実装を確認 (M15 前提を満たす)。
- 環境再確認 (0.1 の stale claim を修正): Git LFS 3.7.0 利用可・git status 正常、Unity 6000.4.6f1 ローカル存在。
Remaining risk:
- バッチテストは Unity Editor を閉じる必要があり、PlayMode は -RunSynchronously 不可という実行制約。
- 本セッション中、利用者が同一アセンブリのソースを IDE で同時編集していたため、確定的な test 実行は WIP 安定後に行う。
```

---

# M1 Build, Repository, Project Settings

## 目的

リリース前に、プロジェクトが再現可能に開き、テストでき、ビルドできる状態か確認する。

## 対象

```text
Packages/manifest.json
Packages/packages-lock.json
ProjectSettings/**
Assets/Settings/**
Assets/AddressableAssetsData/**
Assets/InputSystem_Actions.inputactions
BombCourier.slnx
Tools/**
*.csproj.lscache
build_*.txt / UnityTestLog.txt / TestResults_*.xml
Windows/ / Windows.zip / _BombCourier.zip
```

## レビュー観点

```text
- Unity version と package version が Docs / CI / local script と一致しているか
- packages-lock.json が manifest と整合しているか
- Git LFS が必要な資産、zip、build output、generated cache の扱いが明確か
- EditorBuildSettings に release 対象 scene が正しく入っているか
- Quality / URP / Renderer / Input / Audio / Time / Physics 設定が意図通りか
- Addressables group と Localization group が release に必要な資産を含むか
- generated / cache / build output が repository に残る理由が明確か
```

## 完了条件

```text
- プロジェクト設定の release blocker がない
- 不要な生成物や配布物が tracked されている場合は方針が決まっている
- Git LFS と大容量資産の扱いが明記されている
- Build / Test 実行手順が再現可能である
```

## 完了通知

```text
Status: Complete
Completed at: 2026-06-11
担当: Claude (Opus 4.8)
Evidence:
- Version 整合: ProjectVersion 6000.4.6f1 = manifest 前提 = Run-UnityTests.ps1 の UnityPath。packages-lock.json は manifest と整合 (URP 17.4.0 / inputsystem / localization を確認)。
- EditorBuildSettings: TitleScene + GameScene の2本が enabled。ToyDiorama / EnvironmentStylizedLit の lab scene 6本は build 除外 (正しい)。addressables / input / localization の configObjects 配線済み。
- Repository hygiene 修正: 約410MB の生成/配布物が git 追跡されていた問題を是正。
  内訳: Windows/ (217 files, 247MB)、Windows.zip (81MB) + _BombCourier.zip (83MB)、*.csproj.lscache (20)、BombCourier.slnx、stale TestResults_*.xml (18)、build_*.txt / detailed_build_output.txt / UnityTestLog.txt。
  対応: .gitignore に再発防止パターンを追加 + git rm --cached で index から除去 (264 staged deletions、ワークツリーのファイルは保持)。
Remaining risk:
- 追跡解除は staged・未コミット。利用者がコミットして確定する必要あり。意図せぬ巻き込みを避けるなら untracking を単独コミット推奨。
- git 履歴からの完全削除 (リポジトリ肥大の解消) は filter-repo 等の別作業が必要 (本作業では未実施)。
- Quality / URP / Physics / Audio の詳細設定監査は M12 / M14 / M16 に委譲 (M1 では build 含有・version 整合・release blocker 不在を確認)。
```

---

# M2 Kernel / Service Wiring / Managers

## 目的

ゲーム全体の所有権、サービスライフタイム、scene/application 境界を確認する。
ここが曖昧だと、ほぼ全セクションのバグが起きる。

## 対象

```text
Assets/Scripts/Base/Kernel/**
Assets/Scripts/Base/Loading/**
Assets/Scripts/Base/Entity/**
Assets/Scripts/Base/Event/**
Assets/Scripts/Managers/**
Assets/Scripts/Camera/SceneCameraService.cs
Assets/Scripts/Utility/Wiring/**
```

## レビュー観点

```text
- ApplicationKernel と SceneKernel の責務境界
- MB bootstrap の順序、重複生成、scene unload 後参照
- Manager singleton / static Instance の初期化失敗と破棄
- targetObjects wiring、fallback store、service auto-create の妥当性
- EventService / registry の購読解除、メモリリーク、stale handle
- LoadingSceneService / SceneManagerService の cancellation と失敗診断
```

## 重点的に疑うバグ

```text
- scene 切り替え直後に古い manager / registry を参照する
- 未設定参照を warning だけ出して続行し、後段で不整合になる
- fallback store が specification violation を隠す
- async scene load 中に cancellation / destroy が入り、状態だけ進む
```

## 完了条件

```text
- service lifetime と failure behavior がコードまたはコメントで追える
- silent fallback が domain contract として正当化されている、または修正済み
- manager 間の循環依存と初期化順序依存が把握されている
- 関連テストまたは検証手順が記録されている
```

## 完了通知

```text
Status: Complete (review + fixes)
Completed at: 2026-06-11
担当: Claude (Opus 4.8) + read-only verifier subagents 2 本 (全 finding を実コードで再検証)

Reviewed:
- Base/Kernel/** (BaseKernel, KernelContracts, ApplicationKernel(MB), SceneKernel(MB))
- Base/Loading/** (LoadingSceneService(MB), SceneManagerService(MB))
- Base/Event/** (EventService, contracts)
- Base/Entity/** (Registry, Lifecycle, Spawner, Resolver, Bootstrapper)
- Managers/** (15 本), Utility/Wiring/**, Camera/SceneCameraService.cs

Findings & Fixes (本 milestone で修正、8 ファイル):
- [Must Fix] EntityLifecycleService.cs:18 — ApplicationKernelMB.Instance を無ガード dereference。単体起動/一部 PlayMode で NRE になり SceneKernel 構築全体が崩れる。→ null 許容+明示診断、ApplicationRegistry 使用2箇所をガード (DDOL は scene 登録へ明示フォールバック)。他参照箇所と挙動統一。
- [Must Fix] StageManagerMB.LoadStage — stageData ([SerializeField]) 無ガードで .StageData 参照、prefab も無ガード Instantiate。未配線時 NRE。→ stageData / data / stagePrefab を明示診断。prefab 検証を「前ステージ破棄」より前へ移動し、無効 prefab 時に現ステージを壊さない順序へ修正。
- [Must Fix/限定実害] GameLogicManagerMB.Start — SceneKernelMB 取得が無ガードで NRE 余地。→ null 診断+中断。GetComponent<EntityMB> の二重呼び出しを単一化。
- [Should Fix] GameStateManagerMB / StageManagerMB — singleton Instance を OnDestroy で畳んでおらず、scene reload 後に破棄済み参照が残り fake-null 依存になる (他の正しい singleton と非対称)。→ OnDestroy で Instance=null。GameStateManagerMB の空 Update も除去。
- [Should Fix] KernelBuilder — List.Sort が不安定で同一 Order (EventMB=-5, ScopedEntityRegistryMB=-5) の初期化順が不定。installers が instance field で複数 Build 時に累積。targetObjects の null スロットで NRE。→ OrderBy (安定ソート) + ローカル化 + null スロット明示スキップ。
- [Should Fix] ApplicationKernelMB / SceneKernelMB Update — 重複生成/破棄競合時に kernel.Tick で NRE 余地。→ kernel?.Tick。
- [Comment/Docs] ValueStore auto-create fallback (Application/Scene) — 「失敗隠蔽」でなく警告付き degraded mode という domain contract である旨の rationale を明記。

初期化順 (把握済み・循環依存なし):
- ApplicationKernel: Event(-5), Registry(-5) → ValueStore(0) → LoadingScene(5) → SceneManager(10)
- SceneKernel: Event(-5), Registry(-5) → EntityLifecycle(-1) → ValueStore(0) → ComponentResolver(1) → ActionRunner(2) → Spawner(10)
- ApplicationKernelMB は [DefaultExecutionOrder(-10000)] で SceneKernel より先に常駐。

Tests / checks:
- Command (WIP 安定後に実行): pwsh -File Tools/Run-UnityTests.ps1 -Platform EditMode -TestFilter BombCourier -RunSynchronously
- Result: NOT RUN — レビュー中、利用者が同一アセンブリのソース (EventService / ValueStore / Talk*) を IDE で同時編集していたため。Unity batchmode は当該 WIP を巻き込んでコンパイルし結果が誤りやすく、プロジェクトをロックして作業を妨げるため見送り。
- 変更は最小・整形済み C# で既存型/API のみ使用、静的レビュー済み。

Remaining risk:
- 上記 batchmode regression が未実行。WIP 確定後に EditMode(BombCourier)、必要なら関連 PlayMode を実行のこと。
- 範囲外 (別 milestone へ委譲、未修正):
  - GameLogicManagerMB の状態遷移 .Forget() 群は手動 IsDestroyedOrShuttingDown ガード依存で構造的 cancellation でない → M3。
  - EntityRef.Version が常に1で stale-handle 検査が tautology (ID 単調・非再利用のため現状安全) → M4。
  - ScopedEntityRegistry.GetEntitiesByTag が内部 List を IReadOnlyList で返す (現状 action 実行前に results へコピーされ安全) → M4。
  - SceneKernel teardown が scene-root entity の unregister event を発火しない (GC 依存、cross-scene/DDOL listener 追加時に顕在化) → M4/M9。
```

---

# M3 Action / Talk / Tutorial Orchestration

## 目的

会話、チュートリアル、演出、カメラ、UI、ValueStore 書き込みをつなぐ Action 系の安全性を確認する。

## 対象

```text
Assets/Scripts/Action/Authoring/**
Assets/Scripts/Action/Core/**
Assets/Scripts/Tutorial/**
Assets/Scripts/Managers/TalkAdapterMB.cs
Assets/Scripts/Managers/TalkSystemManagerMB.cs
Assets/Scripts/UI/UITalkSystem/**
Assets/Docs/ActionInlineInspectorSpec.md
Assets/Docs/ActionInlineWindowSpec.md
Assets/Docs/TalkData.md
```

## レビュー観点

```text
- IActionStepRuntime の契約、ActionResult、skip/fail の意味
- Authoring と Runtime のペアが全 step で整合しているか
- SubAction / If / Wait / Talk / Choice / Camera / Audio / ValueStore step の cancellation
- Action 中の actor entity 解決、destroy、scene reload、選択肢範囲外
- Talk localization key、speaker、choice、local value write の失敗診断
- Tutorial progression が重複開始、途中 cancellation、UI未設定で壊れないか
```

## コメント重点

```text
- step が失敗ではなく skip になる条件
- authoring data が runtime contract に変換される理由
- Action sequence の中断、再入、選択肢保存の不変条件
```

## 完了条件

```text
- 各 Action step の失敗/skip/complete の意味が明確
- catch と Debug.LogException 後の戻り値が仕様に合っている
- Tutorial / Talk / Choice の分岐が test または手順で検証されている
- コメント不足の複雑 step に rationale が入っている
```

## レビュー結果 2026-06-11

Status: Complete

Must Fix:

- `ShowTalkStepRuntime` の cancellation が `TalkSystemManagerMB` まで届いていない。`ShowTalkStepRuntime` は `CancellationTokenSource` を生成し、`Cancel()` で cancel しているが、`TalkAdapterMB.TryShowTalkAsyncInternal()` は token を受け取ったあと `talkSystemManager.ShowTalk(...)` へ渡していない。そのため Action 側がキャンセル済みになっても、会話 UI / Talk manager 側の待機と入力ロックが継続し得る。
  - Evidence: `Assets/Scripts/Action/Core/ShowTalkStepRuntime.cs:76`, `Assets/Scripts/Action/Core/ShowTalkStepRuntime.cs:83`, `Assets/Scripts/Managers/TalkAdapterMB.cs:223`, `Assets/Scripts/Managers/TalkAdapterMB.cs:290`, `Assets/Scripts/Managers/TalkSystemManagerMB.cs:388`
  - Required fix: `TalkSystemManagerMB.ShowTalk(...)` に外部 `CancellationToken` を受け取る overload / parameter を追加し、manager 内部 CTS と linked token にする。`UITalkSystemMB.ShowTalk(...)` と start action には linked token を渡す。
  - Required test: `ShowTalkStepRuntime` 実行中に Action を cancel し、Talk UI が閉じる、待機 task が残らない、入力/カメラ lock が残らないことを PlayMode で確認する。

Should Fix:

- `UITalkSystemMB.ShowTalk()` の表示 tween は `AsyncWaitForCompletion()` を cancellation なしで待っている。会話キャンセルが tween 中に来ると、UI close が 1 frame 以上遅延する可能性がある。
  - Evidence: `Assets/Scripts/UI/UITalkSystem/UITalkSystemMB.cs:181`
  - Required fix: DOTween 待機に cancellation を渡す、または hide/cancel 時に該当 tween を kill する。

Review note:

- `TalkSystemManagerMB` の complete action が `CancellationToken.None` を使う箇所は、`HideTalk` 自身のキャンセルと完了 action が競合しないための意図がコメントされている。これは現時点では不具合扱いにしない。
  - Evidence: `Assets/Scripts/Managers/TalkSystemManagerMB.cs:459`, `Assets/Scripts/Managers/TalkSystemManagerMB.cs:467`

修正結果:

- `TalkAdapterMB` から `TalkSystemManagerMB.ShowTalk(...)` へ外部 cancellation token を伝播し、manager 内部 CTS と linked token で統合した。
- 外部 cancellation 時は Talk UI、選択肢、presentation adapter、camera focus、input lock、active actor/adapter state を即時 cleanup し、`OperationCanceledException` を呼び出し元へ返す契約にした。
- `UITalkSystemMB` の表示 tween は cancellation で kill され、`HideTalk(0)` は即時非表示にすることで tween 待機中の残留を防いだ。
- PlayMode tests で、直接 `ShowTalk` cancellation と `TalkAdapterMB.TryShowTalkAsync` 経由 cancellation の両方を固定した。

## 完了通知

```text
Status: Complete
Completed at: 2026-06-11
Evidence:
- Fixed: Assets/Scripts/Managers/TalkSystemManagerMB.cs, Assets/Scripts/Managers/TalkAdapterMB.cs, Assets/Scripts/UI/UITalkSystem/UITalkSystemMB.cs
- Tests: Logs/TestRuns/20260611-140649_PlayMode_M3M5M6RelatedClasses (Unity PlayMode, 18/18 passed)
- Tests: Logs/TestRuns/20260611-135005_PlayMode_M3M5M6Contracts (Unity PlayMode, 7/7 passed)
Remaining risk: No known open M3 release blocker from this pass.
```

---

# M4 ValueStore / ReactiveValue / Events / Registries

## 目的

状態管理、Reactive binding、typed key、entity/tag/signal registry の一貫性を確認する。

## 対象

```text
Assets/Scripts/Utility/ValueStore/**
Assets/Scripts/Utility/ReactiveValue/**
Assets/Scripts/Base/Event/**
Assets/Scripts/Base/Entity/Registry/**
Assets/Scripts/Base/Character/**
Assets/Scripts/Editor/ReactiveValue/**
Assets/Scripts/Editor/ValueKeyReferenceDrawer.cs
Assets/Scripts/Editor/SignalReferenceDrawer.cs
Assets/Docs/ReactiveValueSystemSpec.md
```

## レビュー観点

```text
- ValueKey / EntityTag / Signal / CharacterId の public contract
- scope 解決、local/entity/kernel/snapshot/watched source の一貫性
- missing key、unsupported type、type mismatch の failure behavior
- watch handle の解除、重複購読、destroy 後通知
- Editor drawer が不正 authoring data を安全に正規化するか
- Action integration と startup writer の write failure が隠れないか
```

## 完了条件

```text
- key registry と binding の仕様が Docs と一致
- runtime と editor drawer の正規化ルールが一致
- watch/unwatch の resource lifetime が明確
- 関連 EditMode tests が確認済み
```

## レビュー結果 2026-06-11

Status: Complete

Must Fix:

- `ValueStoreService` が invalid `EntityRef` を拒否せず、`EntityId` だけで store を作成する。`default(EntityRef)` / 未解決 Entity に対する `Set/Get/GetHandle` が成功してしまい、authoring や runtime のターゲット解決ミスを `EntityId=0` の状態書き込みとして隠す可能性がある。`ReactiveValueSystemSpec.md` の「失敗を明示する」方針とも矛盾する。
  - Evidence: `Assets/Scripts/Utility/ValueStore/ValueStoreService.cs:142`, `Assets/Scripts/Utility/ValueStore/ValueStoreService.cs:153`
  - Required fix: `GetRequiredStore(EntityRef entity)` で `!entity.IsValid` を明示的に拒否する。既存 API が throw 方針なら `InvalidOperationException`、非throw方針なら `Try*` API を分ける。
  - Required test: invalid `EntityRef` に対する `Get/Set/GetHandle/SetModifier` が silent success にならないことを EditMode で固定する。

Must Fix:

- `KernelEventService` / `EntityEventService` は publish 中に live handler list をそのまま走査している。handler が自分または他 handler を unsubscribe すると次の handler を skip し得る。handler が subscribe すると同じ publish 中に新 handler が呼ばれる可能性もある。イベント順序と通知対象が購読変更に依存するため、状態遷移やUI更新で再現困難な欠落が起きる。
  - Evidence: `Assets/Scripts/Base/Event/EventService.cs:45`, `Assets/Scripts/Base/Event/EventService.cs:108`
  - Required fix: publish 開始時に snapshot を取る、または versioned iteration policy を明文化して実装する。新規購読は次回 publish から、解除済み handler は現在 publish で呼ぶ/呼ばないのどちらかを仕様化する。
  - Required test: self-unsubscribe、他 handler unsubscribe、publish 中 subscribe の3パターンを EditMode で固定する。

Should Fix:

- `ValueWatchHandle` は `ValueStoreScope.Clear()` 後も handle 自体が有効な見た目で残る。現在値の stale read と listener clear の関係がコードコメントだけでは追いにくい。
  - Evidence: `Assets/Scripts/Utility/ValueStore/ValueStoreScope.cs:11`, `Assets/Scripts/Utility/ValueStore/ValueWatchHandle.cs:12`
  - Required fix: scope clear 後の handle の契約を docs/test に明記する。可能なら disposed/version を持たせ、古い handle の subscribe/read が診断可能になるようにする。

修正結果:

- `ValueStoreService.GetRequiredStore(EntityRef)` は invalid `EntityRef` を明示的に拒否し、`EntityId=0` への silent write/read を防ぐ契約にした。
- `KernelEventService` / `EntityEventService` は publish 開始時の snapshot を走査し、新規購読は次回 publish から、解除済み handler は現在 publish でも呼ばない policy をテストで固定した。
- `SceneKernel` に `KernelValueStoreService` を持たせ、SceneKernel scope の kernel value を SceneKernel entity の `EntityValueStore` へ誤って逃がさないようにした。
- `SetValueStoreValueStepRuntime` は SceneKernel/ApplicationKernel scope の書き込みを `KernelValueStoreService` へ明示的に行う。
- ReactiveValue の既存 EditMode tests を現在の runtime contract に合わせ、watch/unsupported/fallback/kernel write の期待値を再固定した。

## 完了通知

```text
Status: Complete
Completed at: 2026-06-11
Evidence:
- Fixed: Assets/Scripts/Utility/ValueStore/ValueStoreService.cs, Assets/Scripts/Base/Event/EventService.cs, Assets/Scripts/Base/Kernel/SceneKernel.cs, Assets/Scripts/Base/Kernel/SceneKernelMB.cs, Assets/Scripts/Utility/ValueStore/ValueStoreMB.cs
- Fixed: Assets/Scripts/Utility/ReactiveValue/ReactiveValueContracts.cs, Assets/Scripts/Utility/ReactiveValue/ReactiveValueResolverService.cs, Assets/Scripts/Action/Core/SetValueStoreValueStepRuntime.cs
- Tests: Logs/TestRuns/20260611-140550_EditMode_M4ReactiveValueFull (Unity EditMode, 52/52 passed)
- Tests: Logs/TestRuns/20260611-140959_PlayMode_M4ConditionDrivenCollider (Unity PlayMode, 4/4 passed)
- Tests: Logs/TestRuns/20260611-134921_EditMode_M4NewContracts (Unity EditMode, 4/4 passed)
Remaining risk: `ValueWatchHandle` の Clear 後 contract は should-fix として文書化/テスト補強余地あり。今回の release blocker は解消済み。
```

---

# M5 Player Movement / Physics / Camera Coupling

## 目的

プレイヤー移動、物理接触、地面補正、段差補助、足場追従、カメラ連携を重点レビューする。
リリース時の体感品質と進行不能リスクに直結する。

## 対象

```text
Assets/Scripts/Movement/**
Assets/Scripts/Player/Movement/**
Assets/Scripts/Player/Camera/**
Assets/Scripts/Player/Data/PlayerMB.cs
Assets/Scripts/Camera/**
Assets/Docs/BombCourier_PlayerMoveSystem_ReworkSpec_JA.md
```

## レビュー観点

```text
- Update / FixedUpdate / physics query の責務分離
- GroundProbe / GroundSnap / StepAssist / ContactSolver / SupportMotion の順序
- Rigidbody / Collider policy、scale、layer、trigger、kinematic support
- 持ち物状態、cushion、moving platform、camera forward との結合
- cancellation / destroy / scene reload 中の motor state
- debug log、debug draw、hot path allocation、GetComponent 探索
```

## 重点的に疑うバグ

```text
- support motion と player input が二重加算される
- ground snap と step assist が斜面/段差/移動足場で競合する
- Rigidbody state が snapshot restore 後に破綻する
- camera 未設定 fallback がプレイフィールを silently 変える
```

## 完了条件

```text
- 移動 pipeline の順序がコードコメントまたは Docs で追える
- 物理補正の不変条件が明文化されている
- PlayMode movement tests が関連範囲で実行済み
- hot path の診断ログと allocation が確認済み
```

## レビュー結果 2026-06-11

Status: Complete

Should Fix:

- `ThirdPersonCameraController` は `Look` 入力の種類を `Mouse.current.delta.ReadValue() == look` の値比較で判定している。InputActions 側は `<Pointer>/delta` と `<Gamepad>/rightStick` を同じ `Look` action に束ねており、値一致で device/source を推定するのは不安定。Pointer/Touch、processor 追加、複数 device 同時入力で mouse path と gamepad path が入れ替わると、`gamepadSensitivity * Time.deltaTime` が誤適用され、カメラ感度が大きく変わる。
  - Evidence: `Assets/Scripts/Player/Camera/ThirdPersonCameraController.cs:95`, `Assets/Scripts/Player/Camera/ThirdPersonCameraController.cs:97`, `Assets/Scripts/Player/Camera/ThirdPersonCameraController.cs:109`, `Assets/Scripts/Player/Camera/ThirdPersonCameraController.cs:116`, `Assets/InputSystem_Actions.inputactions:223`, `Assets/InputSystem_Actions.inputactions:236`
  - Required fix: `InputAction.CallbackContext.control.device` か control scheme を保持して device 種別を判断する。もしくは mouse/pointer と gamepad を別 action/path として明示的に分ける。
  - Required test: pointer delta と gamepad rightStick の両経路で yaw/pitch の適用係数が期待通りになることを EditMode または PlayMode で固定する。

Should Fix:

- Player prefab は `EntityMoveMotorMB` 配下に enabled non-trigger の ragdoll colliders を保持している。一方、`MovementColliderPolicyValidator` は body/foot 以外の non-trigger collider を違反として扱う。現状は `PlayerRagdollControllerMB.Awake()` が先に ragdoll collider を disable する前提に見えるが、この順序依存が明示的に固定されていない。Prefab編集、execution order変更、別Player派生Prefabで、移動collider policy違反と物理干渉が再発し得る。
  - Evidence: `Assets/Scripts/Movement/Body/MovementColliderPolicyValidator.cs:27`, `Assets/Scripts/Movement/Body/MovementColliderPolicyValidator.cs:42`, `Assets/Scripts/Movement/EntityMoveMotorMB.cs:228`, `Assets/Scripts/Movement/EntityMoveMotorMB.cs:256`, `Assets/Scripts/Movement/EntityMoveMotorMB.cs:1469`, `Assets/Scripts/Player/Movement/PlayerRagdollControllerMB.cs:91`, `Assets/Scripts/Player/Movement/PlayerRagdollControllerMB.cs:225`, `Assets/Art/Prefab/Player/Player.prefab:805`, `Assets/Art/Prefab/Player/Player.prefab:815`
  - Required fix: `PlayerRagdollControllerMB` の execution order または `EntityMoveMotorMB` 側の ragdoll exclusion contract を明文化する。Prefab validation test で、Awake/OnEnable 後のPlayer collider policyが違反しないことを固定する。

Should Fix:

- `EntityMoveMotorMB.ApplyPositionCorrection()` は `FixedUpdate` pipeline 内で `bodyRigidbody.position += correction.Delta` を直接書き換える。StepAssist solver 単体は Rigidbody を直接動かさないテストで守られているが、統合側の直接 position 書き込みは interpolation/contact 解決との関係が未文書化で、段差・移動足場・cushion 付近の瞬間補正で副作用が出やすい。
  - Evidence: `Assets/Scripts/Movement/EntityMoveMotorMB.cs:409`, `Assets/Scripts/Movement/EntityMoveMotorMB.cs:751`, `Assets/Scripts/Movement/EntityMoveMotorMB.cs:756`, `Assets/Tests/PlayMode/Gameplay/PlayerMoveM6StepAssistPlayModeTests.cs:115`
  - Required fix: `MovePosition` を使うべきか、直接 position 書き込みを許容する不変条件を明文化する。統合PlayModeで step assist correction 後の接触安定性と support velocity 二重加算なしを確認する。

Review note:

- Movement solver 群は M0-M9 系 PlayMode tests が存在し、単体の基礎保護は比較的厚い。今回の残リスクは、主に実Prefab・入力device判定・統合時の物理補正である。

修正結果:

- `ThirdPersonCameraController` は `InputAction.CallbackContext.control.device` を保持し、Pointer 系と Gamepad 系を device 種別で判定するようにした。Pointer path は delta を frame time で再スケールせず、Gamepad path のみ `Time.deltaTime` を適用する。
- `EntityMoveMotorMB.ApplyPositionCorrection()` は Rigidbody の直接 `position += delta` ではなく `MovePosition(bodyRigidbody.position + delta)` を使う。
- `PlayerRagdollControllerMB` に execution order を固定し、ragdoll collider disable が movement collider policy validation より先に走る契約を明示した。
- PlayMode tests で pointer/gamepad 判定、ragdoll execution order、関連 movement/camera class 群を確認した。

## 完了通知

```text
Status: Complete
Completed at: 2026-06-11
Evidence:
- Fixed: Assets/Scripts/Player/Camera/ThirdPersonCameraController.cs, Assets/Scripts/Movement/EntityMoveMotorMB.cs, Assets/Scripts/Player/Movement/PlayerRagdollControllerMB.cs
- Tests: Logs/TestRuns/20260611-140649_PlayMode_M3M5M6RelatedClasses (Unity PlayMode, 18/18 passed)
- Tests: Logs/TestRuns/20260611-135005_PlayMode_M3M5M6Contracts (Unity PlayMode, 7/7 passed)
Remaining risk: No known open M5 release blocker from this pass. Broader movement regression remains in M15/M16 full-suite hardening.
```

---

# M6 Carry, Bomb, Items, Interaction

## 目的

ゲームの中心である爆弾運搬、持ち上げ、投げ、離す、安全判定、相互作用を確認する。

## 対象

```text
Assets/Scripts/Item/Bomb/**
Assets/Scripts/Item/Carry/**
Assets/Scripts/Item/BonusItem/**
Assets/Scripts/Player/Interaction/**
Assets/Scripts/Player/Data/PlayerItemHandleStateMB.cs
Assets/Scripts/UI/Bomb/**
Assets/Scripts/UI/Carry/**
Assets/Scripts/UI/Interaction/**
```

## レビュー観点

```text
- carry/release/throw/drop の state machine
- interactable scoring、highlight、prompt、input race
- 爆発、連鎖爆発、障害物破壊、物理衝突の境界条件
- 持ち物中の移動能力低下、animation、UI 表示
- release safety と collision avoidance が不正位置を作らないか
- item destroy / scene reload / checkpoint restore との整合
```

## 完了条件

```text
- 爆弾運搬の主ループが仕様と一致
- release safety の理由と限界がコメントで追える
- Interaction prompt と highlight が stale state を残さない
- 関連 PlayMode tests が確認済み
```

## レビュー結果 2026-06-11

Status: Complete

Must Fix:

- 手持ち爆弾が爆発して `Destroy(gameObject)` された場合、`PlayerItemHandleStateMB` が保持している `ICarryableItem` interface 参照は自動で fake-null にならない。`TickThrow()` は `currentlyHandledItem == null` を interface 比較で見たあと `currentlyHandledItem.ItemTransform` にアクセスするため、破棄済み `BombMB` へのアクセスで `MissingReferenceException` が出る可能性がある。`BombMB.Exploded` を Player 側で購読しておらず、爆発時に `ClearHeldState()` する経路もない。
  - Evidence: `Assets/Scripts/Player/Data/PlayerItemHandleStateMB.cs:317`, `Assets/Scripts/Player/Data/PlayerItemHandleStateMB.cs:321`, `Assets/Scripts/Player/Data/PlayerItemHandleStateMB.cs:837`, `Assets/Scripts/Item/Bomb/BombMB.cs:772`, `Assets/Scripts/Item/Bomb/BombMB.cs:786`, `Assets/Scripts/Item/Bomb/BombMB.cs:789`
  - Required fix: `SetCurrentHandledItem()` で `BombMB.Exploded` を subscribe/unsubscribe し、現在所持中の爆弾が爆発したら即 `ClearHeldState()` する。加えて、`ICarryableItem` が `UnityEngine.Object` / `Component` の場合は Unity fake-null を考慮した生存確認 helper を使う。
  - Required test: 爆弾を所持中に爆発させ、1 frame 後に `IsHandlingItem == false`、jump penalty解除、`CurrentHandledItemChanged(null)` 発火、MissingReferenceなしを PlayMode で確認する。

Must Fix:

- `CarryableObjectMB` は所持中に collider を disable するが、Stage restore では collider enabled 状態を復元していない。Stage snapshot は Rigidbody/Transform は復元するが Collider.enabled は共通保存対象ではないため、generic carryable を持ったまま Reload/Reset すると、復元後も collider disabled のままになり、再取得不能・物理干渉なし・prompt消失につながる。
  - Evidence: `Assets/Scripts/Item/Carry/CarryableObjectMB.cs:91`, `Assets/Scripts/Item/Carry/CarryableObjectMB.cs:131`, `Assets/Scripts/Item/Carry/CarryableObjectMB.cs:136`, `Assets/Scripts/Stage/Snapshot/StageRestorableMB.cs:97`, `Assets/Scripts/Stage/Snapshot/StageSnapshotServiceMB.cs:146`
  - Required fix: `CarryableObjectMB.RestoreStageState()` で `objectCollider.enabled = !isHandled` を明示する。必要なら `rb.isKinematic/useGravity/detectCollisions` も item contract と同期する。`BombMB.RestoreStageState()` と同等の復元明示性に揃える。
  - Required test: carryable を所持した状態から checkpoint/baseline restore し、collider enabled、CanBeCarried、prompt候補復帰、Rigidbody状態を PlayMode で確認する。

Should Fix:

- `UICarryThrowMB` は `Update()` で毎frame slider を同期しているにもかかわらず、`async void UpdateThrowPowerSlider()` でも同じ値を `Task.Yield()` ループで更新している。`EndThrowCharge()` は `CancellationTokenSource` を cancel したあと dispose せず `null` にしており、`DOFade` tween も kill していない。表示の責務が二重化し、destroy/unbind 時の非同期・tween lifetime が追いにくい。
  - Evidence: `Assets/Scripts/UI/Carry/UICarryThrowMB.cs:52`, `Assets/Scripts/UI/Carry/UICarryThrowMB.cs:78`, `Assets/Scripts/UI/Carry/UICarryThrowMB.cs:91`, `Assets/Scripts/UI/Carry/UICarryThrowMB.cs:99`, `Assets/Scripts/UI/Carry/UICarryThrowMB.cs:109`
  - Required fix: slider同期を `Update()` に一本化し、`async void` と CTS を削除する。DOTween は field に保持して `OnDisable/OnDestroy` で kill する。
  - Required test: Player差し替え、throw charge start/end、UI破棄で listener/tween/task が残らないことを PlayMode または smoke test で確認する。

Should Fix:

- `PlayerInteractionController` の `carryableAdapters` は一度見つけた `MonoBehaviour` key を保持し続ける。destroy/reload 後のUnity fake-null keyを掃除しないため、長時間プレイやステージ切替で stale adapter が蓄積し、候補解決の診断が難しくなる。
  - Evidence: `Assets/Scripts/Player/Interaction/PlayerInteractionController.cs:24`, `Assets/Scripts/Player/Interaction/PlayerInteractionController.cs:361`, `Assets/Scripts/Player/Interaction/PlayerInteractionController.cs:363`
  - Required fix: `RefreshCandidates()` 前後または stage restore/unbind 時に destroyed key を prune する。`MonoBehaviour` key は Unity fake-null 判定で扱う。

Review note:

- release safety と owner collision ignore は、投擲後の押し出し事故を避ける意図がコメントされている。今回の主な未解決リスクは、destroy/restore/lifetime の境界である。

修正結果:

- `PlayerItemHandleStateMB` は現在所持中の `BombMB.Exploded` を subscribe/unsubscribe し、手持ち爆弾の爆発時に即 `ClearHeldState()` する。`ICarryableItem` interface 参照でも Unity fake-null / `MissingReferenceException` を考慮した生存確認を使う。
- `CarryableObjectMB.RestoreStageState()` は collider enabled、Rigidbody kinematic/detectCollisions/useGravity、速度、所持中の holder collision ignore を item state と同期する。
- `UICarryThrowMB` は slider 同期を `Update()` に一本化し、`async void` ループと CTS を削除した。表示 tween は field で保持し、destroy 時に kill する。
- `PlayerInteractionController` は候補更新前に destroyed carryable adapter key を prune し、stage reload/destroy 後の stale adapter 蓄積を防ぐ。
- PlayMode tests で爆発時の held state cleanup、restore 後 collider/physics、interaction adapter prune、UI tween lifecycle を固定した。

## 完了通知

```text
Status: Complete
Completed at: 2026-06-11
Evidence:
- Fixed: Assets/Scripts/Player/Data/PlayerItemHandleStateMB.cs, Assets/Scripts/Item/Carry/CarryableObjectMB.cs, Assets/Scripts/UI/Carry/UICarryThrowMB.cs, Assets/Scripts/Player/Interaction/PlayerInteractionController.cs
- Tests: Logs/TestRuns/20260611-140649_PlayMode_M3M5M6RelatedClasses (Unity PlayMode, 18/18 passed)
- Tests: Logs/TestRuns/20260611-135005_PlayMode_M3M5M6Contracts (Unity PlayMode, 7/7 passed)
Remaining risk: No known open M6 release blocker from this pass. Stage progression and full interaction sweep continue in M9/M15.
```

---

# M7 Gimmicks / Stage Objects

## 目的

各ステージギミックが、単体でも組み合わせでも破綻しないか確認する。

## 対象

```text
Assets/Scripts/Gimmick/**
Assets/Scripts/Editor/Gimmick/**
Assets/Scripts/Utility/Motion/**
Assets/Tests/EditMode/Gimmick/**
Assets/Tests/PlayMode/Gameplay/*Gate*
Assets/Tests/PlayMode/Gameplay/*Platform*
Assets/Tests/PlayMode/Gameplay/*Cushion*
```

## レビュー観点

```text
- MovingPlatform tree / runtime path / editor migration
- BreakableGate / GateObstacle / ExplosionResponse の破壊順序
- Cushion / PressurePlate / Lever / GodHand / Filter / ConditionDrivenCollider の state
- Trigger enter/exit の重複、destroy 中コールバック、layer mask
- Editor authoring と runtime data の整合
- StageSnapshot restore との相性
```

## 完了条件

```text
- 各ギミックの state transition と failure behavior が確認済み
- 複数ギミック連携時の依存が把握されている
- Editor migration が release 前に安全である
- PlayMode / EditMode regression が実行済み
```

## 完了通知

```text
Status: Planned
Completed at:
Evidence:
Remaining risk:
```

---

# M8 Entity / NPC / Animation / Face / Effects

## 目的

Entity 基盤、NPC、表情、アニメーション、影、着地/衝撃エフェクトの安全性と可読性を確認する。

## 対象

```text
Assets/Scripts/Entity/**
Assets/Scripts/Base/Entity/**
Assets/Scripts/Effects/**
Assets/Scripts/Rendering/EntityMaterialControllerMB.cs
Assets/Scripts/Managers/EntityMaterialDatasetServiceMB.cs
Assets/Scripts/Editor/Character/**
Assets/Scripts/Editor/CharacterIdReferenceDrawer.cs
Assets/Tests/PlayMode/Gameplay/*Entity*
Assets/Tests/PlayMode/Gameplay/*Face*
```

## レビュー観点

```text
- Entity binding/unbinding、registry 登録解除、duplicate entity
- Facing target、animation parameter/layer、face UV、blend shape の失敗
- ImpactEffect pool、landing impact cancellation、material dataset 切替
- NPC look controller と gameplay state の独立性
- visible effect が gameplay state を隠さないか
```

## 完了条件

```text
- Entity lifecycle と visual component の ownership が明確
- animation / face / material 失敗時の診断が十分
- effect pool と async cancellation が安全
- 関連 tests が確認済み
```

## 完了通知

```text
Status: Planned
Completed at:
Evidence:
Remaining risk:
```

---

# M9 Stage Progression / Snapshot / Loading / Save-like State

## 目的

ステージ進行、チェックポイント、リトライ、snapshot restore、タイトル進捗を確認する。

## 対象

```text
Assets/Scripts/Stage/**
Assets/Scripts/Managers/StageManagerMB.cs
Assets/Scripts/Managers/GameStateManagerMB.cs
Assets/Scripts/Managers/GameLogicManagerMB.cs
Assets/Scripts/Base/Loading/**
Assets/Scripts/UI/GameLogic/**
Assets/Scripts/UI/Title/TitleStageProgressServiceMB.cs
Assets/Tests/PlayMode/Gameplay/*Snapshot*
Assets/Tests/PlayMode/Gameplay/*Checkpoint*
```

## レビュー観点

```text
- checkpoint と snapshot の保存対象/復元対象
- StableObjectId、registry、restorable の重複/欠落
- リトライ時に物理、UI、Action、ValueStore、Entity state が戻るか
- scene load/unload 中の async operation と cancellation
- title progress と stage unlock の永続化方針
```

## 完了条件

```text
- retry / checkpoint / stage clear / reload の state contract が明確
- restore 対象外の状態が意図的である
- 進行不能につながる復元漏れがない
- 関連 PlayMode tests が確認済み
```

## 完了通知

```text
Status: Planned
Completed at:
Evidence:
Remaining risk:
```

---

# M10 UI / HUD / Title / Settings / Navigation

## 目的

ユーザー操作面の不具合、入力競合、表示状態の stale 化、設定反映漏れを確認する。

## 対象

```text
Assets/Scripts/UI/**
Assets/Scripts/Managers/UIManagerMB.cs
Assets/Scripts/Managers/UIGameSceneManagerMB.cs
Assets/Scripts/Managers/ToastSystemManagerMB.cs
Assets/Scripts/Managers/ScreenOverlaySystemManagerMB.cs
Assets/Scripts/Managers/SettingManagerMB.cs
Assets/Scripts/Input/**
Assets/Art/Shader/UI/**
Assets/Tests/EditMode/UI/**
Assets/Tests/PlayMode/Gameplay/UI*
```

## レビュー観点

```text
- Title / StageSelect / Settings / Modal / HUD の state transition
- uGUI navigation、selected object、keyboard/gamepad/mouse 併用
- UI animation の cancellation、destroy、再入
- settings 変更が Audio / Language / SceneManager / ValueStore と同期するか
- Talk / Toast / Tooltip / TutorialToDo の表示優先順位
- UI shader/material instance の lifetime
```

## 完了条件

```text
- UI state machine が初見で追える
- modal / transition / settings の再入防止が明確
- UI navigation tests と PlayMode smoke が確認済み
- コメント不足の複雑 UI flow が補強済み
```

## 完了通知

```text
Status: Planned
Completed at:
Evidence:
Remaining risk:
```

---

# M11 Audio / Input / Localization Runtime

## 目的

音、入力、言語切替、文字列解決の release quality を確認する。

## 対象

```text
Assets/Scripts/Audio/**
Assets/Scripts/Input/**
Assets/Scripts/Base/Localization/**
Assets/Scripts/Managers/LanguageManagerMB.cs
Assets/Scripts/Managers/InputManagerMB.cs
Assets/Scripts/Managers/GameSoundDataManagerMB.cs
Assets/Scripts/UI/Setting/**
Assets/Settings/Localization/**
Assets/InputSystem_Actions.inputactions
```

## レビュー観点

```text
- Audio pool exhaustion、BGM transition、volume sync、cancellation
- InputAction enable/disable、device prompt、rebinding 風設定、UI input conflict
- Localization key missing、table missing、locale switch 中の UI 更新
- 日本語/英語の混在が仕様通りか
- release build で過剰な warning が出ないか
```

## 完了条件

```text
- audio/input/localization の失敗時挙動が明確
- locale/table/key の不足が検出可能
- settings UI と runtime service の同期が確認済み
- 必要なコメントと Docs 導線がある
```

## 完了通知

```text
Status: Planned
Completed at:
Evidence:
Remaining risk:
```

---

# M12 Rendering / Shaders / Post Process / Particles

## 目的

見た目を支える shader、post process、renderer feature、particle material の安定性と release build 互換性を確認する。

## 対象

```text
Assets/BC/Rendering/PostProcess/ToyDiorama/**
Assets/Scripts/Rendering/**
Assets/Art/Shader/EnvironmentStylizedLit/**
Assets/Art/Shader/Particles/**
Assets/Art/Shader/Transition/**
Assets/Art/Shader/UI/**
Assets/Art/Shader/Hologram/**
Assets/Art/Shader/EndingBackground/EndingBackground.shader
Assets/Art/Shader/PS2PostProcess.shader
Assets/Art/Shader/OutlineInvertedHull.shader
Assets/Docs/ShaderSpec.md
Assets/Docs/ShaderMilestoneSpec.md
Assets/Docs/ToyDioramaPostProcess/**
Assets/Docs/ParticleUnitSpec/**
```

## レビュー観点

```text
- URP 17.4.0 / Unity 6000.4.6f1 での API 互換
- RendererFeature lifecycle、RTHandle 確保/解放、camera type、UI overlay
- shader property 名、material validator、preset、quality tier
- debug view / keyword / variant / mobile renderer / WebGL 制約
- HLSL include guard、pass 分割、fallback shader、magenta risk
- ParticleUnlit/Lit/Distortion/Trail の責務分離
- EndingBackground shader を含む単体 shader の property と material 使用箇所
```

## 完了条件

```text
- production build で禁止すべき debug view / keyword が guard されている
- shader と material validator の契約が一致
- post process の RT lifetime と no-op path が安全
- active scenes で使用される shader/material が破綻しない
- 関連 EditMode / PlayMode / visual validation が確認済み
```

## 完了通知

```text
Status: Planned
Completed at:
Evidence:
Remaining risk:
```

---

# M13 Editor Tools / Authoring / Validation

## 目的

制作効率を支える Editor tools が、データ破壊や silent migration を起こさないか確認する。

## 対象

```text
Assets/Scripts/Editor/**
Assets/BC/Rendering/PostProcess/ToyDiorama/Editor/**
Assets/Art/Shader/**/Editor/**
Assets/Tests/EditMode/EditorFoundation/**
Assets/Docs/EditorFoundationSpec.md
Assets/Docs/EditorAuthoringStyleGuide.md
```

## レビュー観点

```text
- SerializedProperty / ManagedReference / Undo / Prefab override の扱い
- inline action editor、picker、drawer、window の state persistence
- migration command が明示操作であり、勝手に data churn しないか
- validation/bootstrapper が deterministic で scene/asset を壊さないか
- Editor-only code が runtime assembly に漏れていないか
```

## 完了条件

```text
- Undo / dirty / prefab override / serialized object update が正しい
- migration と bootstrap の実行条件が明確
- Editor tests が確認済み
- public repo 読者が authoring tool の目的を追えるコメントがある
```

## 完了通知

```text
Status: Planned
Completed at:
Evidence:
Remaining risk:
```

---

# M14 Scenes / Prefabs / ScriptableObjects / Addressables / Localization Assets

## 目的

コードだけでは検出できない、scene/prefab/SO/string table 参照の欠落や release packaging の事故を確認する。

## 対象

```text
Assets/Scenes/**
Assets/Settings/**
Assets/AddressableAssetsData/**
Assets/Resources/**
Assets/**.asset
Assets/**.prefab
Assets/Settings/Localization/**
ProjectSettings/EditorBuildSettings.asset
ProjectSettings/GraphicsSettings.asset
ProjectSettings/QualitySettings.asset
ProjectSettings/TagManager.asset
```

## レビュー観点

```text
- GameScene / TitleScene / validation scenes の build inclusion
- Prefab missing script、missing reference、duplicate singleton
- ScriptableObject の初期値、release preset、debug preset 混入
- Addressables group、Localization table、locale asset の不足
- Resources / Addressables / direct reference の責務分離
- .meta GUID の欠落や重複
```

## 完了条件

```text
- release scenes と addressable/localization assets が確認済み
- missing reference / missing script / debug asset 混入がない
- scene/prefab/SO 変更の必要がある場合は差分理由が明確
- Unity 上の validation または CI 証跡がある
```

## 完了通知

```text
Status: Planned
Completed at:
Evidence:
Remaining risk:
```

---

# M15 Test Coverage / Test Quality / CI Commands

## 目的

レビューで見つけたバグを再発させないため、テスト範囲、テスト品質、実行コマンドを整理する。

## 対象

```text
Assets/Tests/EditMode/**
Assets/Tests/PlayMode/**
Tools/Run-UnityTests.ps1
TestResults_*.xml
UnityTestLog.txt
Logs/TestRuns/**
```

## レビュー観点

```text
- EditMode と PlayMode の責務分離
- zero tests collected を成功扱いしていないか
- stale XML / old logs を現在の証跡として扱っていないか
- flaky timing、scene state、shared static state、order dependence
- major gameplay loop に回帰テストがあるか
- review fix に対して最小の regression test が追加されているか
```

## 推奨テスト実行例

```powershell
pwsh -File Tools/Run-UnityTests.ps1 -Platform EditMode -TestFilter BombCourier -RunSynchronously
pwsh -File Tools/Run-UnityTests.ps1 -Platform PlayMode -TestFilter BombCourier
```

個別修正では、対象 namespace / class の filter を優先する。

## 事前観測 2026-06-11

M3-M6 修正後に full-suite の現状確認を行い、M15 以降で扱うべき既存 gate failure を確認した。

```text
- EditMode full: Logs/TestRuns/20260611-141524_EditMode_FullAfterM3M6
  Result: Failed(Child), total 265, passed 185, failed 80
  主な失敗群: InlineActionDrawer editor tests、EnvironmentStylizedLit validation、ParticleLit/ParticleDistortion validation
- PlayMode full: Logs/TestRuns/20260611-141733_PlayMode_FullAfterM3M6
  Result: RunError before result XML
  主原因: ParticleUnlitValidationBootstrapper.EnsureValidationScene が保存済み Scene.name を変更し、Unity 6000.4.6f1 で InvalidOperationException
```

上記は M3-M6 の targeted regression tests では再現していないが、release gate としては M12/M13/M15 で閉じる必要がある。

## 完了条件

```text
- 各セクションの関連 test が明確
- release blocker 修正には regression test がある、または追加できない理由が記録済み
- テスト実行コマンド、結果、ログ場所が記録されている
- stale artifacts と current evidence が分離されている
```

## 完了通知

```text
Status: Planned
Completed at:
Evidence:
Remaining risk:
```

---

# M16 Performance / Diagnostics / Release Hardening

## 目的

リリース前に、hot path、ログ、診断、build target 固有の破綻を横断確認する。

## 対象

```text
Assets/Scripts/**
Assets/BC/**
Assets/Art/Shader/**
ProjectSettings/**
Assets/Settings/**
```

## レビュー観点

```text
- Update / FixedUpdate / LateUpdate / render pass / shader hot path
- per-frame allocation、LINQ、string interpolation、GetComponent、Find 系
- Debug.Log 系の release build 影響
- exception path が診断可能で、必要なら fail-fast するか
- mobile/WebGL/PC renderer、quality tier、shader variant
- memory leak、event subscription leak、material/RT/mesh instance leak
```

## 完了条件

```text
- hot path の obvious performance regressions がない
- release build で不要な verbose log がない
- diagnostics が不足している critical failure がない
- platform 固有リスクが triage 済み
```

## 完了通知

```text
Status: Planned
Completed at:
Evidence:
Remaining risk:
```

---

# M17 Documentation / Comments / Public Readability

## 目的

public GitHub repository として、他の開発者が意図を追える状態にする。
コメント量を増やすこと自体ではなく、迷う箇所を残さないことを完了条件にする。

## 対象

```text
Assets/Docs/**
Agents.md
Assets/Scripts/**
Assets/BC/**
Assets/Art/Shader/**
```

## レビュー観点

```text
- active spec と実装がずれていないか
- milestone docs が現状を誤って Complete と書いていないか
- complex gameplay logic、physics、rendering、editor migration に rationale があるか
- public API / serialized data / ScriptableObject / shader property の意味が追えるか
- 日本語/英語コメントの使い分けが不自然でないか
- README 相当の導線が不足していないか
```

## コメントを追加すべき代表例

```text
- 物理補正の順序が逆だと破綻する箇所
- fallback が domain contract として許される箇所
- Action step が skip を返す理由
- shader keyword / variant / debug mode の production guard
- Editor migration が対象 asset をどう守るか
- snapshot restore の対象外状態と理由
```

## 完了条件

```text
- コメント不足の high-risk logic が解消されている
- Docs が現在の実装を正しく説明している
- public repo 読者が subsystem の入口を見つけられる
- 誤解を招く古いコメントや古い Docs が修正または非規範扱いになっている
```

## 完了通知

```text
Status: Planned
Completed at:
Evidence:
Remaining risk:
```

---

# M18 Final Release Gate / Triage Closure

## 目的

全セクションのレビュー結果を統合し、リリースしてよい状態か判断する。

## 対象

```text
本書の M0-M17
レビューで追加された issue / fix / tests / docs
最新の test run summaries
release build artifacts
```

## 作業内容

```text
- Release Blocker / Must Fix が残っていないことを確認
- Should Fix を release 前対応と backlog に分ける
- Optional は release 後 backlog に移す
- test evidence と build evidence を最新化する
- 残リスクを release note または internal note に明記する
- 本書の Milestone Progress を Complete / Blocked に更新する
```

## 最終完了条件

```text
- 全セクションが Complete または明示的な Blocked
- Blocked がある場合、リリース判断者が内容を理解できる
- Release Blocker / Must Fix が 0 件
- 関連 EditMode / PlayMode / build check の最新証跡がある
- Docs / comments / tests が修正内容と同期している
```

## 完了通知

```text
Status: Planned
Completed at:
Evidence:
Remaining risk:
Release decision:
```

---

## 付録A: セクション完了時の記録テンプレート

```text
Section:
Status:
Completed at:

Reviewed files:
- ...

Findings:
- [Severity] file:line - summary

Fixes:
- ...

Comments / Docs added:
- ...

Tests / checks:
- Command:
- Result:
- Notes:

Remaining risks:
- ...
```

## 付録B: レビュー修正時の最小ルール

```text
- unrelated cleanup を混ぜない
- public contract を変える場合は Docs と tests を同時に更新する
- scene/prefab/SO を変える場合は Unity 上の差分理由を記録する
- generated asset は generator/source を優先して直す
- 失敗を隠す fallback を追加しない
- コメントは「なぜ」「不変条件」「失敗時の意図」を書く
- 修正ごとに最小の関連 test を実行する
```
