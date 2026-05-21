# BombCourier Editor Authoring Style Guide v0.1

## 1. 目的

本ガイドは、BombCourier の editor 実装で日常的に守るべきコーディング規約、UI 規約、状態管理規約、移行規約を定義する。

foundation spec が構造を定める文書であるのに対し、本ガイドは「新規実装時にどう書くか」を定める文書である。

## 2. 基本原則

### 2.1 foundation first

- 新しい editor code は既存コードのコピーから始めない。
- まず foundation に寄せられる概念が無いかを確認する。
- 2 箇所以上で再利用しそうな処理は local utility に閉じず foundation へ切り出す。

### 2.2 authoring safety first

- Undo が壊れる実装は採用しない。
- Prefab override が壊れる実装は採用しない。
- `SerializedProperty` で編集できるものは direct field 書き換えより `SerializedProperty` を優先する。
- error / missing / incompatible は blank のまま黙って通さない。

### 2.3 compact but readable

- editor UI は常設ボタンを増やし過ぎない。
- 低頻度操作は context menu に寄せる。
- 一覧性が重要な UI は 1 行 summary を優先する。

### 2.4 Odin-agnostic

- foundation 自体は Odin 必須にしない。
- Odin attribute が付いている runtime authoring component と共存してよい。
- 新しい editor foundation API を Odin type に依存させない。

## 3. folder と namespace

### 3.1 folder

新規 editor code は以下を正とする。

```text
Assets/Scripts/Editor/Foundation/
Assets/Scripts/Editor/Foundation/IMGUI/
Assets/Scripts/Editor/Foundation/Pickers/
Assets/Scripts/Editor/Foundation/Scene/
Assets/Scripts/Editor/Foundation/UIToolkit/
```

feature 固有の editor 実装は以下を正とする。

```text
Assets/Scripts/Editor/<Feature>/
```

例:

- `Assets/Scripts/Editor/Action/`
- `Assets/Scripts/Editor/Camera/`

### 3.2 namespace

- namespace root は `BC.Editor` に統一する。
- feature namespace は `BC.Editor.<Feature>` を使う。
- foundation namespace は `BC.Editor.Foundation.*` を使う。

禁止:

- `BC.CameraEditor` のような feature 個別 root namespace の増殖
- 同じ責務なのに namespace だけ別系統になること

## 4. 命名規則

### 4.1 type

- base class は `...Base`
- helper utility は `...Utility`
- state holder は `...State` または `...StateKey`
- adapter は `...Adapter`
- bridge は `...Bridge`
- snapshot は `...Snapshot`
- context menu builder は `...ContextMenuBuilder`

### 4.2 file

- public / internal top-level type とファイル名を一致させる。
- 例外は同系統の極小派生 class をまとめる場合だけに留める。

### 4.3 method

- side-effect がある apply 系は `Apply...`
- `SerializedProperty` 解決は `TryResolve...` / `Find...`
- button content 構築は `Build...Content`
- dropdown root 構築は `BuildRoot`
- snapshot 生成は `Build...Snapshot`

## 5. IMGUI 規約

### 5.1 rect 計算

- 手組み `Rect` は `RectLayoutUtility` を優先する。
- `LineHeight` と `Spacing` の定数は foundation token を使う。
- `EditorGUIUtility.singleLineHeight` の直書きを散らさない。

### 5.2 property drawer

- 共通責務は `PropertyDrawerBase` に寄せる。
- missing field 時は明示的な fallback label / help box を出す。
- `EditorGUI.BeginProperty()` / `EndProperty()` を忘れない。

### 5.3 serialized editing

- `SerializedProperty` がある場合はそちらを使う。
- reflection で private field を直接書き換えない。
- `managedReferenceValue` の操作は専用 controller / utility 経由にする。

### 5.4 inline list

- row UI は index、type、summary、state の責務を分ける。
- 常設ボタンは最小限に抑える。
- rename / duplicate / delete / move は context menu に寄せるのが基本。

### 5.5 help box

- setup 不足は `MessageType.Info`
- 互換性不整合は `MessageType.Warning`
- editor code の構造破損や registry failure は `MessageType.Error`

## 6. Picker / Dropdown 規約

### 6.1 generic 化優先

- `ValueKey`、`Signal`、`EntityTag` のような tree picker は個別に再実装しない。
- `AdvancedDropdown` 基底と adapter を使う。

### 6.2 button content

button content の状態は次に固定する。

1. current
2. incompatible current
3. missing stored value
4. none

blank button title は禁止する。

### 6.3 apply

- selection apply は `UndoApplyUtility` を通す。
- multi-target apply を考慮する。
- `PrefabUtility.RecordPrefabInstancePropertyModifications()` を忘れない。

### 6.4 state

- dropdown state key は `propertyPath` だけに依存し過ぎない。
- filter 条件や allow-none 条件も state key に含める。

### 6.5 owner-local picker

`MovingPlatformNode` のような owner-local picker でも、button content と apply の作法は共通化する。

## 7. Scene / Gizmo 規約

### 7.1 runtime と editor の責務

- 重い可視化ロジックを runtime component の `OnDrawGizmos*` に溜め込み過ぎない。
- editor 側へ寄せられる描画は `GizmoAdapterBase<TTarget>` へ移す。
- runtime 側には read-only snapshot helper だけを残してよい。

### 7.2 handle 操作

- `OnSceneGUI` の変更適用は `SceneUndoScope` を使う。
- `BeginChangeCheck()` / `EndChangeCheck()` を裸で重複させない。

### 7.3 color と size

- scene handle / gizmo の色は `SceneHandleStyleTokens` を使う。
- magic number の色や半径を各 file に散らさない。

### 7.4 label

- scene label は短く保つ。
- index と human label を併記するときは `"1: Point A"` 形式を基本とする。

## 8. UIToolkit 規約

### 8.1 phase 1 方針

- editor window は code-first を基本とする。
- UXML / USS の導入は phase 1 では必須にしない。

### 8.2 window 構成

- root toolbar
- primary content
- optional status/footer

の 3 段を基本構成とする。

### 8.3 state

- selected row
- split ratio
- filter text
- expanded rows

は `SessionStateViewModel` または同等の state object に集約する。

### 8.4 IMGUI bridge

- detail pane のみ既存 IMGUI drawer を使うのは許可する。
- ただし window 全体を IMGUI の寄せ集めにはしない。

## 9. Undo / Prefab / Dirty 規約

### 9.1 共通 apply

以下の責務は `UndoApplyUtility` を通す。

- picker apply
- inline rename
- context menu 操作
- list add / duplicate / delete / reorder
- scene handle edit

### 9.2 direct mutation

以下は禁止する。

- `target` へ直接 field 代入して終える
- `Undo.RecordObject()` だけで `PrefabUtility.RecordPrefabInstancePropertyModifications()` を省略する
- `SerializedObject` を作ったのに `ApplyModifiedProperties()` をしない

## 10. 文言と見た目

### 10.1 文言

- UI ラベルは短く、意味が一意に分かるものにする。
- summary は 1 行で deterministic にする。
- missing / incompatible / no matching は current 状態と明確に区別する。

### 10.2 言語

- code symbol は英語
- user-facing label は既存 inspector 文脈に合わせた英語寄りの短語を許可する
- doc / comment は日本語でよい

### 10.3 表示状態

- current: 通常表示
- incompatible: `Incompatible: ...`
- missing: `Missing: ...`
- none: `None`
- empty collection: `No matching ...`

を基本フォーマットとする。

## 11. テスト規約

### 11.1 editor test

- foundation helper は EditMode test を書く。
- UI automation ではなく contract test を優先する。
- `SerializedObject` / `SerializedProperty` / `PropertyDrawer` の高さや state 変化を検証する。

### 11.2 test host

- editor-only test host は editor assembly 配下に置く。
- production runtime component を test host に流用しない。

### 11.3 回帰ゲート

- 既存 ReactiveValue editor tests は回帰ゲートとして維持する。
- foundation 移行後も green であることを条件にする。

## 12. 移行規約

### 12.1 copy-paste 禁止

- 既存 drawer を複製して別 feature 用に改造しない。
- 先に foundation へ抽出し、既存実装を薄くする。

### 12.2 pilot migration

- まず代表機能を 1 つ移行する。
- その結果 stable になってから横展開する。

### 12.3 namespace 正規化

- 新しい file を `BC.CameraEditor` に追加しない。
- 既存の別 root namespace は移行時に `BC.Editor` 配下へ寄せる。

## 13. ActionInline への適用方針

- `ActionInline` inspector は IMGUI foundation を使う。
- `ActionInline` window は UIToolkit foundation と IMGUI bridge を使う。
- `ActionInlineWindowSpec.md` は foundation spec と本 style guide を前提文書とする。

## 14. 最終ルール

- foundation に置ける概念は feature file に閉じ込めない。
- Undo / Prefab override / missing state を壊す実装は採用しない。
- 新規 editor window は code-first UIToolkit を基本にする。
- scene visualization は editor adapter への移行を前提に増築する。
- 新しい editor code は `BC.Editor` に統一する。
