# BombCourier Editor Foundation Spec v0.1

## 1. 目的

BombCourier にはすでに複数の editor authoring 実装が存在するが、現状は次の種類ごとに作法が分かれている。

- IMGUI `PropertyDrawer`
- IMGUI `CustomEditor`
- `AdvancedDropdown` / `GenericMenu` ベースの picker
- `OnSceneGUI` / `OnDrawGizmos*` ベースの scene visualization
- 将来導入する UIToolkit `EditorWindow`

その結果、レイアウト規約、Undo 適用、Prefab override、状態保持、見た目、ラベル文言、missing/incompatible 表示が各所で重複している。

本仕様の目的は、BombCourier の editor 実装を支える共通基盤を定義し、今後の `ActionInline`、独自 Window、picker、scene visualization を同じ設計ルールの上に載せることにある。

## 2. 対象

本仕様は phase 1 として、以下を対象にする。

- `Assets/Scripts/Editor/` 配下の既存 editor 実装
- runtime component 内の `OnDrawGizmos()` / `OnDrawGizmosSelected()` による authoring visualization
- 今後追加する IMGUI inspector / drawer
- 今後追加する UIToolkit editor window
- 今後追加する検索付き picker / dropdown

代表的な既存対象は以下。

- `ValueKeyReferenceDrawer`
- `SignalReferenceDrawer`
- `EntityTagReferenceDrawer`
- `MovingPlatformNodePathDropdownDrawer`
- `ReactiveValueDrawerBase` とその派生
- `ValueStoreWriteAuthoringDrawer`
- `CameraPathSequenceAuthoringMBEditor`
- `CameraPathSequenceAuthoringMB` / `CameraPathPointAuthoringMB` の gizmo
- `MovingPlatformMB` の gizmo
- `BreakableGateObjectMB` / `PlayerItemHandleStateMB` / `UIFallEffectMB` の gizmo

## 3. 非目的

phase 1 では以下はやらない。

- 全 editor 実装の一括全面移行
- Odin を基底システムの前提にすること
- UXML / USS 前提の UIToolkit framework 構築
- graph editor 基盤
- runtime 全般に共通の universal visualization interface の導入
- 既存 runtime authoring data の大規模な再設計

## 4. 基本方針

### 4.1 依存方針

- foundation 自体は Unity 標準 API のみを前提とする。
- IMGUI は `UnityEditor.EditorGUI` / `EditorGUILayout` / `SerializedProperty` を使う。
- picker は `AdvancedDropdown` または `GenericMenu` を使う。
- scene visualization は `Handles` / `Gizmos` を使う。
- UIToolkit は code-first を前提に `VisualElement` を組む。
- Odin は既存 authoring 属性との共存対象であり、foundation の必須依存にはしない。

### 4.2 レイヤ分割

foundation は次の 5 層に分ける。

1. `BC.Editor.Foundation`
2. `BC.Editor.Foundation.IMGUI`
3. `BC.Editor.Foundation.Pickers`
4. `BC.Editor.Foundation.Scene`
5. `BC.Editor.Foundation.UIToolkit`

`ActionInline` や将来の独自 Window は、上記レイヤを直接組み合わせて実装する。

### 4.3 pilot migration 優先

phase 1 のゴールは「全移行」ではなく「共通化の核を完成させ、代表機能を移行してテンプレート化すること」とする。

pilot migration は以下の順で行う。

1. picker 群
2. IMGUI drawer 群
3. scene editor / visualization 群
4. `ActionInline` inspector
5. `ActionInline` window

## 5. 物理配置

editor foundation の配置は以下を正とする。

```text
Assets/Scripts/Editor/Foundation/
Assets/Scripts/Editor/Foundation/IMGUI/
Assets/Scripts/Editor/Foundation/Pickers/
Assets/Scripts/Editor/Foundation/Scene/
Assets/Scripts/Editor/Foundation/UIToolkit/
```

テストは以下を正とする。

```text
Assets/Tests/EditMode/EditorFoundation/
```

editor docs の起点は以下。

```text
Assets/Docs/EditorFoundationSpec.md
Assets/Docs/EditorAuthoringStyleGuide.md
```

## 6. namespace と assembly

### 6.1 namespace

新規 editor code の namespace root は `BC.Editor` に統一する。

例:

- `BC.Editor.Foundation`
- `BC.Editor.Foundation.IMGUI`
- `BC.Editor.Foundation.Pickers`
- `BC.Editor.Foundation.Scene`
- `BC.Editor.Foundation.UIToolkit`

既存の `BC.CameraEditor` は移行対象とし、将来的に `BC.Editor.Camera` もしくは `BC.Editor` 配下へ寄せる。

### 6.2 asmdef

phase 1 で以下の assembly を追加する。

- `BombCourier.Editor`
- `BombCourier.EditorTests`

方針:

- `Assets/Scripts/Editor` 配下は `BombCourier.Editor` に収める。
- foundation 用 EditMode test は `BombCourier.EditorTests` に分離する。
- runtime assembly への依存は最小限に保つ。
- editor-only test host は production runtime assembly に置かない。

## 7. Core foundation

`BC.Editor.Foundation` には以下を置く。

### 7.1 `EditorBindingContext`

役割:

- `SerializedObject`
- root `propertyPath`
- `targetObjects`
- optional owner label
- optional host editor state key

を一つに束ねる。

用途:

- picker apply
- window binding
- nested property targeting
- `ActionInline` root binding

### 7.2 `EditorStateKey`

役割:

- session-local state の stable key を生成する。
- foldout、rename mode、selected row、window session state のキーとして使う。

入力の基準:

- target object instance id
- root property path
- optional managed reference id
- optional local suffix

要求:

- reorder 後も同じ step を追跡できる場合は managed reference id を優先する。
- array index だけに依存しない。

### 7.3 `SerializedPropertyPathUtility`

役割:

- relative path 組み立て
- parent / child property 解決
- managed reference property 判定
- array element 判定
- `propertyPath` ベースの human-readable label 補助

### 7.4 `UndoApplyUtility`

役割:

- `Undo.RecordObject(s)`
- `SerializedObject.ApplyModifiedProperties()`
- `PrefabUtility.RecordPrefabInstancePropertyModifications()`
- `EditorUtility.SetDirty()`

を一貫した順で適用する。

要求:

- single target と multi-target の両方に対応する。
- picker / context menu / inline rename / window edit の共通 apply 経路とする。

### 7.5 `EditorThemeTokens`

役割:

- 行高さ
- 標準 spacing
- type badge 色
- warning / error / missing / incompatible 色
- minimum picker size
- scene label offset

などの共通 token を一箇所に集約する。

phase 1 では IMGUI / UIToolkit / Scene で共通概念だけを持ち、実 API 依存の wrapper は各層に置く。

## 8. IMGUI foundation

`BC.Editor.Foundation.IMGUI` には以下を置く。

### 8.1 `RectLayoutUtility`

役割:

- 単純な縦積みレイアウト
- prefix label 付き field rect
- help box rect
- 省略表示用 label rect
- compact row 分割

既存 `ReactiveValueDrawerBase` や `ValueStoreWriteAuthoringDrawer` の手組み `Rect` 計算をここへ寄せる。

### 8.2 `PropertyDrawerBase`

役割:

- `LineHeight` / `Spacing`
- missing field fallback
- `BeginProperty` / `EndProperty`
- `GetControlDelta()`
- child property height helper

などを共通化する。

`ReactiveValueDrawerBase` は将来的にこの基底を継承する。

### 8.3 `InlineListController`

役割:

- 一覧 row の描画順序
- row state
- add / insert / delete / duplicate / reorder
- row height 取得
- context menu 起動

`ActionInline`、将来の compact inspector list、custom editor 内の軽量 row list で使う。

### 8.4 `ManagedReferenceListController`

役割:

- `SerializeReference` list への step 追加
- 型生成
- duplicate
- delete
- reorder 後の state 維持

要求:

- 追加時の型生成責務を list owner から分離する。
- duplicate は shallow copy ではなく serialized round-trip で壊れにくくする。

### 8.5 `ContextMenuBuilder`

役割:

- `GenericMenu` 構築
- enabled / disabled 制御
- separator 位置の統一
- apply 後の repaint hook

## 9. Picker foundation

`BC.Editor.Foundation.Pickers` には generic picker 基盤を置く。

### 9.1 目的

現在の `ValueKey`、`Signal`、`EntityTag` picker はほぼ同じ構造を個別実装している。

共通化対象は以下。

- registry error 表示
- current / missing / incompatible の button content
- `AdvancedDropdownState` の管理
- path segment ごとの tree 生成
- selection apply と Undo
- `None` 表示
- minimum size

### 9.2 共有要素

- `PickerItemDescriptor`
- `PickerButtonContentBuilder`
- `AdvancedDropdownPickerBase<TDescriptor>`
- `PickerApplyUtility`
- `PickerStateCache`

### 9.3 adapter

phase 1 の adapter は以下。

- `ValueKeyPickerAdapter`
- `SignalPickerAdapter`
- `EntityTagPickerAdapter`
- `MovingPlatformNodePickerAdapter`

既存 drawer は adapter を使う薄い wrapper にする。

### 9.4 `MovingPlatformNode`

`MovingPlatformNodePathDropdownDrawer` は registry ベースではなく owner `SerializedObject` ベースの picker なので、phase 1 では同じ generic base を使いつつ descriptor source だけ差し替える。

## 10. Scene foundation

`BC.Editor.Foundation.Scene` には scene editing / visualization の基底を置く。

### 10.1 `SceneToolEditorBase<TTarget>`

役割:

- `serializedObject.Update()` / `ApplyModifiedProperties()`
- selected sub-index の保持
- common repaint
- `Undo.RecordObject`
- scene handle draw の雛形

`CameraPathSequenceAuthoringMBEditor` のような `CustomEditor + OnSceneGUI` をここへ寄せる。

### 10.2 `GizmoAdapterBase<TTarget>`

役割:

- runtime component から読み取り専用 snapshot を取得する
- `Gizmos` 描画ロジックを editor 側へ移せるものは移す
- runtime と editor の責務境界を明確にする

phase 1 の方針:

- runtime 側に必要最小限の read-only helper や snapshot builder を追加してよい。
- ただし universal interface はまだ作らない。

### 10.3 `SceneHandleStyleTokens`

役割:

- line color
- selected color
- handle size multiplier
- label offset
- wire alpha

を統一する。

### 10.4 `SceneUndoScope`

役割:

- `BeginChangeCheck()` / `EndChangeCheck()`
- `Undo.RecordObject()`
- change label

を包む。

## 11. UIToolkit foundation

`BC.Editor.Foundation.UIToolkit` には code-first の window 基底を置く。

### 11.1 phase 1 方針

- UXML / USS は必須にしない。
- `CreateGUI()` 内で code-first に組む。
- 既存 IMGUI drawer の再利用が必要な箇所は `IMGUIContainer` bridge を使う。

### 11.2 `EditorWindowBase`

役割:

- root element 初期化
- title / minimum size / toolbar 基本構成
- domain reload 後の再接続
- repaint hook

### 11.3 `SplitViewWindowBase`

役割:

- list/detail 2 pane
- optional inspector pane
- pane width state 記録

`ActionInline` の window はこの基底に乗る前提で設計する。

### 11.4 `SerializedObjectBridge`

役割:

- specific target object と `propertyPath` に window を bind する
- current `SerializedObject` の再作成
- domain reload 後の再解決
- `target` 消滅時の graceful failure

### 11.5 `SessionStateViewModel`

役割:

- window-local state の保存
- selected row
- expanded set
- pane width
- sort / filter

### 11.6 `IMGUIContainer` bridge

役割:

- detail pane だけ既存 IMGUI drawer を再利用する
- phase 1 の UIToolkit window 実装コストを抑える

## 12. 既存実装の移行先

phase 1 での代表移行先は以下。

- `ValueKeyReferenceDrawer` -> picker foundation
- `SignalReferenceDrawer` -> picker foundation
- `EntityTagReferenceDrawer` -> picker foundation
- `MovingPlatformNodePathDropdownDrawer` -> picker foundation
- `ReactiveValueDrawerBase` -> IMGUI foundation
- `ValueStoreWriteAuthoringDrawer` -> IMGUI foundation
- `CameraPathSequenceAuthoringMBEditor` -> scene foundation

runtime visualization は以下を順次移行候補とする。

- `CameraPathSequenceAuthoringMB`
- `CameraPathPointAuthoringMB`
- `MovingPlatformMB`
- `BreakableGateObjectMB`
- `PlayerItemHandleStateMB`
- `UIFallEffectMB`

## 13. 検証項目

### 13.1 foundation test

- picker の missing / incompatible / current 表示
- picker apply 時の Undo / Prefab override
- multi-target apply
- IMGUI layout height helper
- managed reference list 操作
- editor state key の安定性
- scene snapshot helper の計算

### 13.2 pilot regression

- 既存 ReactiveValue editor tests が green
- picker 移行後に `ValueKey` / `Signal` / `EntityTag` が既存と同等動作
- `CameraPathSequenceAuthoringMBEditor` の scene 操作が既存と同等動作

### 13.3 manual check

- `CameraPath`
- `MovingPlatform`
- `BreakableGate`
- `PlayerItemHandle`
- `UIFallEffect`

の inspector / scene view を確認する。

## 14. フェーズ計画

### Phase 1

- foundation spec
- authoring style guide
- asmdef
- folder / namespace rule

### Phase 2

- picker foundation 抽出
- picker pilot migration

### Phase 3

- IMGUI foundation 抽出
- drawer pilot migration

### Phase 4

- scene foundation 抽出
- visualization pilot migration

### Phase 5

- `ActionInline` inspector を IMGUI foundation 上で実装

### Phase 6

- UIToolkit foundation を前提に `ActionInlineWindowSpec.md` を作成
- その後に `ActionInline` window を実装

## 15. 最終決定事項

- foundation の依存方針は `Odin-agnostic` とする。
- UI Toolkit は phase 1 では code-first を採用する。
- editor namespace root は `BC.Editor` に統一する。
- picker は generic `AdvancedDropdown` 基盤へ寄せる。
- runtime 側 `OnDrawGizmos*` は可能なものから editor adapter へ寄せる。
- 一括全面移行は行わず、pilot migration を先に完成させる。
- `ActionInlineWindowSpec.md` は foundation spec 確定後に作成する。
