# BombCourier

> 爆弾を拾うとジャンプ力が落ちる。だから、拾う前に脱出ルートを作る。

**BombCourier** は、爆弾を運んでゲートを破壊し、施設からの脱出を目指す3Dパズルアクションゲームです。

プレイヤーは先にステージを探索し、足場や移動可能なオブジェクトを配置して運搬ルートを構築します。爆弾に触れた瞬間からカウントダウンが始まり、所持中はジャンプ力も低下するため、計画段階の思考と運搬段階の緊張感を交互に味わう設計になっています。

- 全12ステージ
- 日本語・英語対応
- WebGL向けに制作
- マウス・キーボード推奨
- 開発期間：約1か月

---

## ゲームの目的

各ステージでは、ゲートを塞いでいる段ボールを爆弾で破壊し、その先へ進むとクリアになります。

基本的な流れは次のとおりです。

1. ステージを探索する
2. 爆弾からゲートまでのルートを考える
3. 押せる物体や各種ギミックを利用して足場・経路を作る
4. 爆弾を拾い、制限時間内に運ぶ
5. 爆弾をゲートへ投げて破壊する

爆弾を持っている間はジャンプ力が低下します。通常時に通れる場所でも通れなくなるため、爆弾を拾う前の準備が重要です。

ダッシュ、ジャンプ、移動床などの運動量を利用すると、爆弾を通常より遠くへ投げられる場合があります。

---

## 操作方法

| 操作 | キー |
| --- | --- |
| 移動 | `WASD` |
| ジャンプ | `Space` |
| ダッシュ | `Shift` |
| インタラクト | `E` |
| リロード / リセット | `R` |

ゲームコントローラーは一部対応していますが、UI操作などが正常に動作しない場合があります。マウスとキーボードでのプレイを推奨します。

---

## スター評価

各ステージには3種類のスターがあり、全12ステージで合計36個です。

| スター | 条件 |
| --- | --- |
| Clear Star | ステージをクリアする |
| Banapan Star | Banana Pancake Starを持ったままゲートを通過する |
| Time Star | 規定時間以内にステージをクリアする |

36個すべて集めても大きな追加要素は解放されませんが、小さなメッセージが表示されます。

---

## キャラクター

一部のステージには会話可能なキャラクターが登場します。会話は攻略に必須ではありませんが、世界観やキャラクター同士の関係を知ることができます。

---

# Technical Overview

## 開発環境

| 項目 | 内容 |
| --- | --- |
| Engine | Unity `6000.4.6f1` |
| Language | C# |
| Render Pipeline | Universal Render Pipeline `17.4.0` |
| Primary Target | WebGL |
| Input | Unity Input System |
| Localization | Unity Localization |
| Async | UniTask |
| Camera | Cinemachine |
| UI | uGUI / TextMesh Pro |
| Animation | DOTween / Text Animator for Unity |
| Editor Authoring | Odin Inspector / 独自Editor拡張 |
| Platform Integration | unityroom client library |

主要なUnity Packageの正確なバージョンは [`Packages/manifest.json`](Packages/manifest.json) を参照してください。

## アーキテクチャ

本プロジェクトでは、UnityのSceneやMonoBehaviourへすべての責務を集中させず、ライフタイムと機能単位でサービスを分離しています。

### Application Kernel / Scene Kernel

独自のKernel構成により、アプリケーション全体と各Sceneのサービスを分けて管理します。

- **ApplicationKernel**
  - Scene遷移
  - Loading
  - Application単位のEvent
  - Application単位の共有状態
- **SceneKernel**
  - Entity登録・解決
  - Entity Lifecycle
  - Action実行
  - ReactiveValue
  - Camera制御
  - Tutorial進行
  - Scene単位のEvent / ValueStore

`IKernelInstaller` を実装したComponentを `Order` 順に組み立てることで、初期化順序と所有関係を明示しています。Scene終了時にはKernelが保持するサービスを明示的に破棄します。

関連コード：

- [`Assets/Scripts/Base/Kernel/`](Assets/Scripts/Base/Kernel/)
- [`Assets/Scripts/Base/Entity/`](Assets/Scripts/Base/Entity/)
- [`Assets/Scripts/Base/Event/`](Assets/Scripts/Base/Event/)

### Action System

会話、演出、カメラ、ギミックなどの逐次処理は、直書きのCoroutineを増やすのではなく、Authoring DataとRuntime実行を分離したAction Systemで管理しています。

- Actionの直列・条件分岐・待機
- 会話やカメラ演出との連携
- 実行ContextによるActor / Trigger Entityの参照
- UniTaskを利用した非同期処理とCancellation
- Inspector向けの独自Authoring UI

関連コード：

- [`Assets/Scripts/Action/`](Assets/Scripts/Action/)
- [`Assets/Scripts/Editor/Action/`](Assets/Scripts/Editor/Action/)

### ReactiveValue / ValueStore

Actionやギミックのパラメーターには、固定値だけでなく実行時状態を参照できるReactiveValue層を使用しています。

- Literalと動的値を同じAuthoring Dataとして扱う
- Entity単位とScene Kernel単位の状態を分離
- Value変更をWatch Handleで追跡
- Transformや距離など、継続評価が必要な値に対応
- 解決失敗を暗黙の既定値として握りつぶさない
- Bindingや監視の寿命をAction実行単位へ閉じ込める

詳細は [`Assets/Docs/ReactiveValueSystemSpec.md`](Assets/Docs/ReactiveValueSystemSpec.md) を参照してください。

### Player Movement / Physics

プレイヤー移動は、入力・意図・接地・段差補助・支持物体・物理補正などを分割した構成です。

爆弾やアイテムの運搬では、次の要素を扱います。

- 所持重量によるジャンプ性能の変化
- Rigidbodyベースの投擲
- ダッシュ・ジャンプ・移動床の運動量継承
- 爆弾の起爆状態とUI表示
- Carry中の衝突および復帰処理

関連コード：

- [`Assets/Scripts/Movement/`](Assets/Scripts/Movement/)
- [`Assets/Scripts/Player/`](Assets/Scripts/Player/)
- [`Assets/Scripts/Item/`](Assets/Scripts/Item/)
- [`Assets/Scripts/Gimmick/`](Assets/Scripts/Gimmick/)

### Rendering

URPを基盤として、ゲーム固有のシェーダーとポストプロセスを実装しています。

- Stylized Environment Shader
- Particle Shader
- Hologram表現
- UI / Screen Transition Shader
- Ground Decal Shadow
- Toy Diorama Post Process
- PC / Mobileの2種類の描画プロファイル

`Mobile` は軽量描画プロファイルの名称であり、スマートフォン対応を意味するものではありません。Mobile設定は処理負荷を抑える代わりに、コントラストが弱く柔らかい表示になります。PC設定は負荷がやや高い代わりに、コントラストと描画品質を高めています。

関連コード・仕様：

- [`Assets/BC/Rendering/`](Assets/BC/Rendering/)
- [`Assets/Art/Shader/`](Assets/Art/Shader/)
- [`Assets/Docs/`](Assets/Docs/)

### Localization / Input

- 日本語・英語のLocalization Table
- 接続中の入力デバイスに応じた操作アイコン表示
- マウス・キーボードと一部ゲームパッド入力
- unityroom向けランキング・スコア連携

---

## Scene構成

Build Settingsには次のSceneが登録されています。

```text
Assets/Scenes/TitleScene.unity
Assets/Scenes/GameScene.unity
```

通常は `TitleScene.unity` から実行してください。

---

## ディレクトリ構成

```text
Assets/
├─ BC/Rendering/          # 独自ポストプロセスと描画機能
├─ Art/Shader/            # 環境・Particle・UI・Hologram等のShader
├─ Docs/                  # 設計仕様・実装方針・マイルストーン
├─ Scenes/                # Title / Game Scene
├─ Scripts/
│  ├─ Action/             # Action authoring / runtime
│  ├─ Base/               # Kernel・Entity・Event・Loading等
│  ├─ Camera/             # Camera制御・Camera Path
│  ├─ Editor/             # 独自Editor拡張・Validator・Migration
│  ├─ Gimmick/            # Moving Platform・Gate・Pressure Plate等
│  ├─ Item/               # Bomb・Carry対象・Bonus Item
│  ├─ Managers/           # Game Flow・Stage・Input・Settings等
│  ├─ Movement/           # Motor・Ground・Step・Contact等
│  ├─ Player/             # Player制御
│  ├─ Tutorial/           # Tutorial runtime / condition
│  ├─ UI/                 # HUD・Title・Talk・Toast・Settings等
│  └─ Utility/            # ReactiveValue・ValueStore・共通機能
└─ Tests/
   ├─ EditMode/
   └─ PlayMode/

Packages/                 # Unity Package定義・埋め込みPackage
ProjectSettings/          # Unity Project設定
Tools/                    # Test実行補助Script
```

---

## セットアップ

### 必要環境

- Unity Hub
- Unity `6000.4.6f1`
- Git
- Git LFS
- Windows PowerShell（付属のTest Runnerを使用する場合）

### Clone

```bash
git lfs install
git clone https://github.com/yuusyaisami/BombCourier.git
cd BombCourier
git lfs pull
```

Unity Hubへリポジトリのルートディレクトリを追加し、指定バージョンのUnityで開いてください。初回起動時はPackageの解決とAsset Importに時間がかかる場合があります。

### 実行

1. `Assets/Scenes/TitleScene.unity` を開く
2. Unity EditorのPlayを実行する

WebGL Buildでは `TitleScene` と `GameScene` をBuild対象に含めてください。現在のBuild Settingsには両Sceneが登録されています。

---

## テスト

EditMode / PlayModeのTestを `Assets/Tests/` 以下に配置しています。

Unity Test Runnerから実行できるほか、Windowsでは付属のPowerShell Scriptを利用できます。Scriptを実行する前にUnity Editorを終了してください。

### EditMode

```powershell
.\Tools\Run-UnityTests.ps1 `
  -Platform EditMode `
  -TestFilter "BC.Base.Tests" `
  -RunSynchronously
```

### PlayMode

```powershell
.\Tools\Run-UnityTests.ps1 `
  -Platform PlayMode `
  -TestFilter "BC.Gameplay.PlayModeTests.BombCarryCollisionPlayModeTests"
```

PlayModeでは `-RunSynchronously` を使用できません。実行結果、Unity Log、NUnit XML、Summaryは `Logs/TestRuns/` 以下へ出力されます。

---

## トラブルシューティング

### アイテムが地面へ埋まり、消えてしまった

状況によってはステージを続行できません。`R` キーからステージをResetしてください。

### ReloadとResetの違い

- **Reload**: 最初に爆弾を取得した時点の状態へ戻します。複数の爆弾へ触れている場合は、保存された状態を順に戻ります。
- **Reset**: ステージ全体を最初からやり直します。

アイテム消失、進行不能、明らかな挙動不良が発生した場合はResetを推奨します。

### 爆弾がゲートまで届かない

ダッシュ中、ジャンプ中、または移動中のオブジェクト上から投げると、運動量が加わって飛距離が伸びる場合があります。ルートと投擲タイミングを調整してください。

### ゲームが英語で起動する

`Setting` → `Language` から日本語を選択してください。環境によっては英語が初期言語になる場合があります。

### 3D酔いが発生する

ブラウザの表示領域を小さくすると症状が軽減する場合があります。体調に異変を感じた場合はプレイを中断してください。

### スマートフォンで遊べるか

現在、スマートフォンはサポートしていません。

---

## AI利用について

本プロジェクトでは、次の用途でAI支援を利用しています。

- 日本語から英語へのLocalization補助
- 一部のプログラミング、調査、レビュー補助
- ドキュメント作成補助

LocalizationにはChatGPTおよびGeminiを使用した箇所があります。日本語固有の表現やニュアンスがあるため、可能であれば日本語版でのプレイを推奨します。

AI Coding Agent向けの作業規約は [`Agents.md`](Agents.md) を参照してください。

---

## Known Limitations

- スマートフォン非対応
- ゲームパッドでは一部UIを操作できない場合がある
- WebGLの制約により描画品質を抑えている
- 物理演算の状況によってアイテムが地面へ入り込む場合がある
- 3Dゲームのため、プレイヤーによっては画面酔いが発生する

---

## License

現在、このリポジトリにはプロジェクト全体へ適用される `LICENSE` ファイルがありません。

明示的な許諾がない限り、本リポジトリのソースコード、ゲームデータ、画像、音声、モデルその他の制作物について、再利用・改変・再配布を許可するものではありません。

また、本プロジェクトには複数の第三者製Package・Plugin・Assetが含まれています。それらには各提供元のライセンスおよび利用規約が適用されます。

---

## Author

- GitHub: [@yuusyaisami](https://github.com/yuusyaisami)
