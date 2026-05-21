# BombCourier Action Inline Window Spec v0.1

## 1. 目的

本仕様は、`InlineAction` を Inspector 内の compact list だけでは把握しづらい入れ子構造まで含めて編集するための別 Window を定義する。

Window は Scratch 風の structured block tree として表示し、Action 全体の流れ、分岐、子 Action の位置関係を一画面で把握できることを目的とする。

Inspector は日常的な短い編集と Window 起動導線を担当し、深い nested structure の把握と編集は本 Window が担当する。

## 2. 前提文書

本仕様は以下を前提にする。

- `Assets/Docs/EditorFoundationSpec.md`
- `Assets/Docs/EditorAuthoringStyleGuide.md`　 
- `Assets/Docs/ActionInlineInspectorSpec.md`

Window 実装は editor foundation の上に構築する。

- shell は `BC.Editor.Foundation.UIToolkit`
- detail pane は `IMGUIContainer` bridge
- binding / Undo / Prefab override は foundation utility
- theme / spacing / badge / status 表示は global style token

## 3. 対象範囲

Phase 1 の対象は、property-bound な単一 `InlineAction` root を開いて編集する Window とする。

対象 nested Action は最低限以下を含む。

- `SubActionStepAuthoring.action`
- `IfStepAuthoring.whenTrue`
- `IfStepAuthoring.whenFalse`
- `ShowTalkStepAuthoring.talkRequestData.onStartTalkAction`
- `ShowTalkStepAuthoring.talkRequestData.onCompleteTalkAction`
- `ShowTalkChoiceStepAuthoring.options[*].inlineAction`
- 今後 `ActionStepAuthoring` child slot 契約で公開される child `InlineAction`

## 4. 非目的

Phase 1 では以下をやらない。

- freeform node graph
- 任意座標配置
- cross-branch drag move
- timeline editor
- step ごとの専用 Window
- detached copy editing
- selection-follow window
- runtime visual scripting VM の再設計
- UXML / USS 前提の Window framework

今回の node 化は authoring tree の構造整理であり、自由配置 graph editor への移行ではない。

## 5. 表示モデル

表示モデルは以下に固定する。

- `InlineAction`: sequence block
- `ActionStepAuthoring`: node
- child `InlineAction`: named branch slot
- branch slot の表示: branch lane

Window 左ペインは step card と branch lane のみで構成する。枝は親 step card の直下へ表示し、自由な edge / port / canvas connection は持たない。

例:

```text
Root Action
  [0] ShowTalk
      Start Action
        [0] SetValueStoreValue
      Complete Action
        [0] SubAction
            Action
              [0] WaitFrames
  [1] If
      True
        [0] ShowToast
      False
        Empty
```

## 6. ActionSystem 契約

### 6.1 child slot descriptor

Window は step type 名や private field path を個別判定して nested Action を探さない。

`BC.ActionSystem` 側に child slot descriptor 契約を追加し、各 step が自分の child branch を明示的に申告する。

descriptor は最低限以下を持つ。

- `slot id`
- `label`
- `order`
- `InlineAction` reference
- `is present`
- `metadata badge`

`slot id` は同じ step type 内で stable な英数字 identifier とする。

例:

- `action`
- `whenTrue`
- `whenFalse`
- `talkStart`
- `talkComplete`
- `choice:<stableOptionId>`

### 6.2 direct child と indirect child

direct child と indirect child は Window 上で同じ first-class branch として扱う。

`ShowTalkStepAuthoring` は `TalkRequestData` 内の `onStartTalkAction` / `onCompleteTalkAction` を child slot として公開する。`TalkRequestData` の内部に hidden nested Action が残る状態にはしない。

`ShowTalkChoiceStepAuthoring` は option outcome が `InlineAction` の場合だけ child slot を公開する。`None` / `ValueStoreWrite` では branch lane を表示しない。

### 6.3 stable option id

`ShowTalkChoiceStepAuthoring` の option branch には stable key が必要である。

`TalkChoiceOptionAuthoring` は hidden stable id を持つ。option の rename / reorder / duplicate 後も Window state が誤った branch に移らないことを保証する。

方針:

- 新規 option 生成時に stable id を割り当てる。
- 既存 data で stable id が空の場合は editor utility が補完する。
- duplicate 時は複製先に新しい stable id を割り当てる。
- reorder 時は stable id を維持する。

## 7. Window 構成

### 7.1 root

Window root は code-first UIToolkit で構築する。

基本構成:

1. toolbar
2. split content
3. status footer

split content は左 `block tree`、右 `detail pane` に分ける。

### 7.2 toolbar

toolbar は root Action の context を示し、最小限の操作だけを持つ。

- root label
- bound target name
- root property path
- refresh
- expand all
- collapse all
- focus selection

phase 1 では save button を置かない。編集は `SerializedObject` / `SerializedProperty` と Undo によって即時反映する。

### 7.3 left pane

left pane は structured block tree を表示する。

表示単位:

- root block header
- step card
- branch lane
- empty branch placeholder

step card の構成:

- index
- type badge
- summary
- state badge
- child badge

branch lane の構成:

- branch label
- step count
- branch status
- empty placeholder

### 7.4 right pane

right pane は選択対象の設定画面を表示する。

選択対象:

- step card
- branch lane
- root block

step card 選択時:

- step payload を既存 `PropertyDrawer` / `SerializedProperty` ベースで描画する。
- detail pane は `IMGUIContainer` bridge を使う。
- `DisplayName` は summary override として編集できる。

branch lane 選択時:

- branch block の step list 操作を表示する。
- add / duplicate / delete / reorder は branch 内だけを対象にする。

root block 選択時:

- root block の step list 操作を表示する。
- root metadata が増えた場合はここに表示する。

### 7.5 status footer

status footer には軽量診断のみを表示する。

- selected path
- validation count
- missing branch count
- dirty state

full validation を repaint ごとに実行しない。必要な場合は snapshot 更新時に限定する。

## 8. 操作仕様

### 8.1 selection

left pane の step card / branch lane / root block をクリックすると right pane の detail が切り替わる。

selection key は以下を組み合わせる。

- target object instance id
- root property path
- managed reference id
- child slot id
- stable option id

managed reference id が取得できる step では reorder 後も selection を維持する。

### 8.2 edit operations

Phase 1 の編集操作は以下に固定する。

- block 内 reorder
- add step
- duplicate step
- delete step
- rename label

cross-branch drag move は扱わない。必要になった場合は cut / paste operation として別仕様で追加する。

### 8.3 context menu

step card の context menu:

1. Rename Label
2. Clear Label
3. Duplicate Step
4. Delete Step
5. Open Branch if child exists

branch lane の context menu:

1. Add Step
2. Duplicate Branch Steps
3. Clear Branch
4. Expand
5. Collapse

dangerous operation は Undo 対応を必須にする。

### 8.4 branch visibility

branch lane は child `InlineAction` が存在する場合に表示する。

ただし `If` の `whenTrue` / `whenFalse` のように semantic branch として重要な slot は、空でも placeholder として表示してよい。

`ShowTalkChoice` は outcome が `InlineAction` でない option の branch lane は表示しない。

### 8.5 outcome switch

`ShowTalkChoice` option の outcome を変更した場合:

- `None` / `ValueStoreWrite` -> `InlineAction`: empty branch を生成または復元する。
- `InlineAction` -> `None` / `ValueStoreWrite`: branch lane を非表示にする。
- branch data を完全削除するか保持するかは implementation phase で一貫させる。Phase 1 の推奨は orphan を残さず clear すること。

Undo / Redo で tree 表示が正しく復元されることを必須とする。

## 9. Inspector との関係

Inspector は compact-plus list を維持する。

Inspector 側で深い nested tree を全面展開しない。Inspector では以下だけを表示する。

- current block の step list
- foldout detail
- child count / branch count / has child badge
- `Open in Window`

Window は nested structure の全体像と branch 単位編集を担当する。

これにより、Inspector は短い Action を素早く編集する場所、Window は複雑な Action を設計する場所として役割を分ける。

## 10. Editor foundation 連携

### 10.1 namespace

Window 実装の namespace は `BC.Editor.Action` とする。

Window 用の汎用 helper が foundation に属する場合は以下へ置く。

- `BC.Editor.Foundation.UIToolkit`
- `BC.Editor.Foundation.IMGUI`

feature 固有の view model は `BC.Editor.Action` に置く。

### 10.2 view model

Window は以下の view model / key を使う。

- `ActionBlockTreeViewModel`
- `ActionBranchKey`
- `ActionWindowSelectionState`
- `ActionDetailPaneBridge`

`ActionBlockTreeViewModel` は `SerializedObject` と root property path から snapshot を構築する。

### 10.3 style

色、余白、badge、line height、status 表示は global style token を使う。

Window 固有の magic number は避ける。必要な token は foundation 側へ追加する。

## 11. validation / diagnostics

Window の tree snapshot は軽量診断を持つ。

診断対象:

- missing step
- missing child action
- empty semantic branch
- validation error count
- unsupported managed reference
- stale option stable id

runtime `Validate()` を高頻度 repaint で回さない。snapshot rebuild 時か明示 refresh 時に限定する。

## 12. data migration

破壊的変更は許可するが、既存 authoring data を不用意に壊さない。

移行方針:

- `InlineAction` の `_steps` は root block として維持する。
- `ActionStepAuthoring` は tree node 契約を持つ。
- 既存 field 名は可能な限り維持する。
- `TalkChoiceOptionAuthoring` には hidden stable id を追加する。
- stable id 未設定の既存 option は editor migration utility で補完する。

## 13. 実装フェーズ

### Phase 1: spec / contract

- 本仕様の作成
- Inspector spec との責務分離
- child slot descriptor 契約の追加
- stable option id 方針の追加

### Phase 2: foundation skeleton

- `BC.Editor.Foundation.UIToolkit` の window base
- `SerializedObject` bridge
- `IMGUIContainer` detail bridge
- style token 連携

### Phase 3: Action tree model

- `ActionBlockTreeViewModel`
- `ActionBranchKey`
- tree snapshot builder
- child slot traversal
- selection state

### Phase 4: Window UI

- toolbar
- block tree left pane
- detail pane
- status footer
- context menu

### Phase 5: Inspector integration

- `Open in Window`
- child badge
- branch count summary
- Window と Inspector の selection / refresh 連携

## 14. 検証項目

### 14.1 EditMode test

- `If` child slot 列挙
- `SubAction` child slot 列挙
- `ShowTalk` start / complete child slot 列挙
- `ShowTalkChoice` option child slot 列挙
- empty branch 表示
- missing branch 表示
- outcome switch 後の branch 表示更新
- option stable id の維持
- duplicate option 時の stable id 再割り当て
- reorder 後の selection key 維持

### 14.2 compile / validate parity

- `If` の compile 結果が既存意味と一致すること
- `SubAction` の compile 結果が既存意味と一致すること
- `ShowTalkChoice` の `InlineAction` outcome が既存意味と一致すること
- `ShowTalk` の start / complete action が Window 表示と runtime 実行で一致すること

### 14.3 manual check

- `TalkRequestData`
- `NPCObjectMB.interactionAction`
- `CameraPathPointAuthoringMB.onArriveAction`
- `CameraPathContracts.onArriveAction`
- `If -> SubAction -> TalkChoice` の深い入れ子

確認観点:

- 左ペインで全体像が把握できる
- step 選択で右 pane が正しく切り替わる
- branch lane 選択で branch step list を編集できる
- Inspector は compact なまま読みやすい
- global style token 変更が Window に反映される

## 15. 最終決定事項

- Window は structured block tree とする。
- freeform graph は採用しない。
- `InlineAction` は sequence block とする。
- `ActionStepAuthoring` は tree node とする。
- child `InlineAction` は named branch slot とする。
- child slot detection は `ActionStepAuthoring` 契約で行う。
- direct / indirect child は同じ branch lane として扱う。
- Inspector は compact-plus list を維持する。
- 深い nested structure は Window が担当する。
- Window は property-bound とし、detached copy editing は採用しない。
