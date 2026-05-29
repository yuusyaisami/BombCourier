# BombCourier Action Inline Inspector Spec v0.2

## 1. 目的

BombCourier の InlineAction は Talk / NPC / Camera / Action authoring の多くの箇所で繰り返し使われる。

現状の `SerializeReference` ベース既定表示は、以下の点で常用に向いていない。

1. step 一覧の視認性が低い。
2. 型の見分けと内容把握に縦スクロールが必要になる。
3. `DisplayName` が契約にある一方で、日常の authoring 体験に統合されていない。
4. step の追加、複製、削除、並べ替え、名前変更の導線が弱い。

本仕様の目的は、InlineAction を高頻度 authoring に耐える compact かつ readable な inspector UI に再設計することにある。

Inspector は短い Action をその場で編集するための簡易 UI とする。深い nested structure の全体像と branch 単位編集は `ActionInlineWindowSpec.md` の別 Window に委譲する。

## 2. 対象範囲

Phase 1 の対象は、InlineAction を露出する全使用箇所とする。

- `TalkRequestData.onStartTalkAction`
- `TalkRequestData.onCompleteTalkAction`
- `NPCObjectMB.interactionAction`
- `CameraPathPointAuthoringMB.onArriveAction`
- `CameraPathContracts` 内の `onArriveAction`
- `SubActionStepAuthoring.action`
- `IfStepAuthoring.whenTrue`
- `IfStepAuthoring.whenFalse`
- `ShowTalkChoiceStepAuthoring` 内 option の `inlineAction`
- 今後追加される nested InlineAction

対象契約の起点は以下。

- `Assets/Scripts/Action/Core/ActionStepAuthoringContracts.cs`
- `Assets/Docs/ActionInlineWindowSpec.md`

## 3. 非目的

初期段階では以下をやらない。

- multi-object editing の完全最適化
- node graph 化
- step ごとの専用 editor window
- `DisplayName` の常時表示
- 全 step への個別 editor 実装の先行投入

別 Window での本格的な InlineAction 編集は `ActionInlineWindowSpec.md` の責務とする。本仕様では Inspector 側の簡易表示と Window 起動導線を扱う。

## 4. 現状コード前提

### 4.1 InlineAction 契約

`InlineAction` は `SerializeReference` な `List<ActionStepAuthoring>` を持つ。

```csharp
[Serializable]
public sealed class InlineAction
{
    [SerializeReference]
    private List<ActionStepAuthoring> _steps;
}

[Serializable]
public abstract class ActionStepAuthoring
{
    public string DisplayName;
}
```

Window 対応後は、`ActionStepAuthoring` を authoring tree の node として扱う。

Inspector は child action の具体 field path を step type ごとに直接判定しない。child count / branch count / has child badge は、`ActionStepAuthoring` の child slot descriptor 契約から得る。

### 4.2 Editor 側の再利用元

以下の既存 editor 実装を再利用パターンとする。

- `Assets/Scripts/Editor/Action/ValueStoreWriteAuthoringDrawer.cs`
  - 行レイアウト
  - 条件分岐表示
  - 高さ計算
- `Assets/Scripts/Editor/ValueKeyReferenceDrawer.cs`
  - `AdvancedDropdown`
  - 検索付き選択 UI
  - `Undo` / `PrefabUtility.RecordPrefabInstancePropertyModifications()` 適用
- `Assets/Scripts/Editor/ReactiveValue/ReactiveValueDrawerBase.cs`
  - compact な `PropertyDrawer` 構造
  - 行送り規約

### 4.3 詳細表示の前提

step の詳細表示は既存 `SerializedProperty` 描画を優先し、以下の既存 drawer を再利用する。

- Reactive 系 drawer
- `ValueStoreWriteAuthoringDrawer`
- `ValueKeyReferenceDrawer`
- `EntityTagReferenceDrawer`

## 5. 設計原則

### 5.1 一覧性優先

普段の作業は step 一覧の把握と順序変更が中心であるため、常時 1 行ヘッダを基本表示とする。

### 5.2 詳細は foldout 時だけ出す

すべての step payload を常時展開しない。詳細編集は foldout 展開時に限定する。

### 5.3 DisplayName は補助情報

`DisplayName` はユーザーが任意に付ける label override と位置付ける。主 UI では hidden-by-default とし、必要なときだけ右クリック rename で編集する。

### 5.4 低頻度操作はコンテキストメニューへ寄せる

duplicate / delete / rename / clear label のような低頻度操作は、常設ボタンを増やさず右クリックコンテキストメニューへ寄せる。

### 5.5 summary は deterministic に生成する

summary は「毎回同じ入力から同じ 1 行文字列が出る」ことを重視する。気まぐれな可変表現は避ける。

### 5.6 Window へ委譲する

Inspector は nested tree を全面展開する場所ではない。

Inspector が扱うのは現在 block の compact list、foldout detail、軽量な child badge、`Open in Window` 導線までとする。

深い nested structure、branch lane、Action 全体の俯瞰、branch 単位の編集は `ActionInlineWindowSpec.md` の責務とする。

## 6. 採用 UI 密度

行ヘッダ密度の候補は以下の 3 つ。

1. ultra-compact
2. compact-plus
3. rich-row

採用は `compact-plus` とする。

理由:

1. ultra-compact は summary 情報が不足し、If / ValueStore / TalkChoice の見分けが悪い。
2. rich-row は情報は多いが、横幅と視線移動が増え、高頻度編集に不利。
3. compact-plus は summary を残しつつ、常設ボタンを抑えられる。

## 7. 行ヘッダ仕様

### 7.1 行構成

各 step 行の構成は原則以下に固定する。

1. drag handle
2. foldout
3. index
4. type badge
5. summary
6. state area

右端に大きな常設ボタン群は置かない。右クリックは行全体で受け付けてよい。

### 7.2 役割

- drag handle: 並べ替え用
- foldout: 詳細編集の開閉
- index: 順序把握用
- type badge: step 種別の即時判別用
- summary: 内容把握用の主要情報
- state area: error / custom label / step-specific metadata の小型表示

### 7.3 幅が足りない場合の優先順位

幅不足時は次の順で圧縮する。

1. summary を pixel ベースで省略表示する
2. index 表示を短縮する
3. type badge を短縮ラベルへ切り替える
4. state area は最後まで残す

行ヘッダ内での折り返しは行わない。

### 7.4 state area

Phase 1 の state area は次の情報を持てばよい。

- error / incomplete indicator
- custom label indicator
- step-specific metadata badge

step-specific metadata badge は、常にすべて出すのではなく「一覧で判断価値が高い binary 情報」に限る。

例:

- ShowTalk: start action / complete action / wait
- nested step: child が存在すること

## 8. DisplayName 仕様

### 8.1 基本方針

- `DisplayName` は通常 inspector では表示しない
- foldout 展開後の詳細内にも表示しない
- summary の override 用 label としてだけ扱う

### 8.2 右クリックメニュー

各 step 行の右クリックメニューには最低限以下を含める。

1. Rename Label
2. Clear Label
3. Duplicate Step
4. Delete Step

将来的に以下を追加してもよい。

- Move Up
- Move Down
- Expand Children
- Collapse Children

### 8.3 Rename Label の挙動

`Rename Label` 実行時は別 modal を出さず、同じ行の summary 領域を一時的な text field に差し替える `inline rename mode` を採用する。

挙動は以下。

1. 行の summary 表示を text field に置き換える。
2. 初期値は現在の `DisplayName` を使う。
3. `Enter` で確定する。
4. `Escape` で元値へ戻してキャンセルする。
5. focus out 時は内容が変更されていれば確定する。
6. 空文字または whitespace only で確定した場合は `DisplayName` をクリア扱いにする。
7. `Undo` / `Redo` と prefab override を壊さない形で保存する。

### 8.4 Clear Label の挙動

- `DisplayName` が未設定なら disabled にする。
- 実行時は `DisplayName` を空に戻す。
- summary は自動生成ルールへ即座にフォールバックする。

## 9. summary 自動生成ルール

### 9.1 優先順位

summary の生成優先順位は固定する。

1. `DisplayName` が trim 後に非空なら、その文字列を summary とする。
2. そうでなければ step 固有テンプレートを使う。
3. step 固有テンプレートが成立しない場合は nicified type 名または `Unconfigured` を返す。

### 9.2 共通整形ルール

すべての自動 summary は以下に従う。

1. 1 行のみ。
2. 改行、タブ、連続空白は単一スペースへ正規化する。
3. type 名は summary に重複して含めない。type badge が別に存在するためである。
4. 低情報量の既定値は省略する。
5. 欠落や未設定は blank にせず短い placeholder を返す。
6. テキストは pixel ベースで省略表示してよいが、生成段階では概ね 48-64 文字に収まる内容を目標にする。
7. glue word は短く保つ。既存 inspector 文脈に合わせて英語寄りの短縮表現を許可する。

### 9.3 共通 helper ルール

#### 9.3.1 Text snippet

- 改行を空白へ変換する。
- 前後空白を trim する。
- 空なら `Empty text` を返す。
- 先頭の意味あるテキストを優先し、描画時に省略記号で切る。

#### 9.3.2 Duration

- `0` または既定値相当なら省略してよい。
- 表示する場合は `0.3s` のような短い秒表記にする。

#### 9.3.3 Count

- `1 step` / `n steps`
- `1 option` / `n options`
- If branch count は `T:n` / `F:n`

#### 9.3.4 Target summary

`EntityTargetReference` の要約は以下。

- `Self`
- `Trigger`
- `Tag:<name>`
- `Tag:<name> (All)`

`Self` が既定で文脈的に明らかな場合は省略してよいが、意味が曖昧になる場合は残す。

#### 9.3.5 ValueKey summary

ValueKey は compact path として表示する。

ルール:

1. `Kernel.` / `Local.` のような大域 prefix は短縮してよい。
2. 最後の 1 segment だけで曖昧な場合は末尾 2 segment を残す。
3. Local key は `L:<short-path>`、Kernel key は `K:<short-path>` の表記を許可する。
4. 未設定時は `No key` を返す。

例:

- `Local.Values.Int0` -> `L:Int0`
- `Kernel.Gimmick.SequenceIndex` -> `K:SequenceIndex`
- `Local.Choice.SelectedIndex` -> `L:Choice.SelectedIndex`

#### 9.3.6 ReactiveBool summary for If

If の condition summary は以下の優先で短くまとめる。

- Literal: `True` / `False`
- EntityValueStore: `<entity>:<key>`
- LocalValueStore: `L:<key>`
- EntityAlive: `Alive(<entity>)`
- CompareNumber: `<left> <op> <right>`

`<op>` は以下へ圧縮する。

- `Equal` -> `==`
- `NotEqual` -> `!=`
- `Greater` -> `>`
- `GreaterOrEqual` -> `>=`
- `Less` -> `<`
- `LessOrEqual` -> `<=`

左辺 / 右辺が長すぎる場合は source kind の短い表記へフォールバックしてよい。

## 10. step ごとの summary テンプレート

### 10.1 WaitFramesStepAuthoring

テンプレート:

- `1 frame`
- `n frames`

### 10.2 SetActiveStepAuthoring

テンプレート:

- 既定 target が `Self` の場合: `On` / `Off`
- 既定以外の target を含む場合: `<target> -> On` / `<target> -> Off`

### 10.3 SubActionStepAuthoring

テンプレート:

- `n steps`
- `Empty` if action is null or zero steps

nested child の中身は summary に展開しない。

### 10.4 IfStepAuthoring

テンプレート:

- `if <condition> | T:n F:n`

例:

- `if True | T:1 F:0`
- `if L:Flag0 | T:2 F:1`
- `if Alive(Trigger) | T:1 F:1`

branch の中身は count のみを出し、branch 内全文は展開しない。

### 10.5 ShowToastStepAuthoring

テンプレート:

- text がある場合: `<text snippet>`
- text が無く icon のみある場合: `Icon only`
- 両方無い場合: `Empty toast`

duration は既定から外れる場合のみ metadata か suffix で補う。

### 10.6 ShowTalkStepAuthoring

テンプレート:

- speaker と text の両方がある場合: `<speaker>: <text snippet>`
- speaker のみある場合: `<speaker>`
- text のみある場合: `<text snippet>`
- 両方無い場合: `Empty talk`

`isWaitingActionCompleted`、start action、complete action は summary 本文へ詰め込み過ぎず、必要なら state area の metadata で示す。

### 10.7 HideTalkStepAuthoring

テンプレート:

- `Instant` if duration is `0`
- `<duration>s` otherwise

### 10.8 ShowTalkChoiceStepAuthoring

テンプレート:

- `1 option`
- `n options`

追加規則:

- `defaultSelectionIndex` が既定値から意味を持つ場合は `, default <index>` を付けてよい。
- `wrapSelection == false` の場合は `, no wrap` を付けてよい。
- option の全文列挙はしない。

例:

- `2 options`
- `4 options, default 2`
- `3 options, no wrap`

### 10.9 SetValueStoreValueStepAuthoring

テンプレート:

- `<key> = <value>`
- target や scope を明示した方が情報価値が高い場合は `<scope> <key> = <value>`
- entity scope かつ target が既定以外なら `<target>: <key> = <value>`

値 summary は active kind に従う。

- Bool: `True` / `False`
- Int: 数値そのまま
- Float: 過剰な桁を落とした短い数値
- String: text snippet
- EntityRef: `Self` / `Trigger` / target summary
- FaceExpressionId: enum 名
- EntityMoveState: enum 名

例:

- `L:Int0 = 42`
- `DisplayName = Bomb`
- `Trigger: FaceExpression = Happy`

### 10.10 SetSceneCameraStepAuthoring

テンプレート:

- camera がある場合: `<camera name>`
- camera 未設定時: `No camera`

### 10.11 ClearSceneCameraStepAuthoring

テンプレート:

- `Clear action camera`

### 10.12 SetEntityFacingTargetStepAuthoring

テンプレート:

- 既定 target が `Self` の場合: `face <faceTarget>`
- 既定以外の target を含む場合: `<target> face <faceTarget>`
- channel が既定でない場合: ` @ <channel>` を suffix 追加

例:

- `face Trigger`
- `Tag:NPC face Trigger`
- `face Trigger @ Talk`

### 10.13 ClearEntityFacingStepAuthoring

テンプレート:

- 既定 target が `Self` の場合: `Self`
- 既定以外: `<target>`
- channel が既定でない場合: ` @ <channel>` を suffix 追加

### 10.14 SetEntityAnimationParameterStepAuthoring

テンプレート:

- `SetBool`: `<parameter> = True/False`
- `SetFloat`: `<parameter> = <float>`
- `SetInteger`: `<parameter> = <int>`
- `SetTrigger`: `SetTrigger <parameter>`
- `ResetTrigger`: `ResetTrigger <parameter>`

target が既定以外なら prefix で `<target>:` を付けてよい。

### 10.15 SetEntityAnimationLayerWeightStepAuthoring

テンプレート:

- `<layer> = <weight>`
- duration が 0 より大きい場合は ` in <duration>s` を suffix 追加
- target が既定以外なら prefix で `<target>:` を付けてよい

例:

- `UpperBody = 1`
- `UpperBody = 0.5 in 0.2s`

## 11. 不完全設定時の placeholder ルール

summary は空文字を返さない。代表的な placeholder は以下。

- `Empty`
- `Empty talk`
- `Empty toast`
- `No camera`
- `No key`
- `No options`
- `No action`
- `Unconfigured`

行に error indicator を出す場合でも summary 自体は placeholder を返す。

## 12. nested InlineAction ルール

nested InlineAction は同じ drawer を再帰利用する。

ただし一覧 summary は親行に子の全文を展開しない。

許可するのは以下まで。

- child count
- branch count
- has child indicator

これにより If / SubAction / TalkChoice が縦に膨れ上がるのを防ぐ。

深い nested structure の全体像は `ActionInlineWindowSpec.md` の structured block tree で扱う。Inspector は現在 block の compact list を主表示とし、必要に応じて root / nested block から `Open in Window` できる導線を置く。

child action の検出は editor 側の step type 判定ではなく、`ActionStepAuthoring` の child slot descriptor 契約を使う。

child slot descriptor は direct child と indirect child を同じ branch として扱う。

対象例:

- `SubActionStepAuthoring.action`
- `IfStepAuthoring.whenTrue`
- `IfStepAuthoring.whenFalse`
- `ShowTalkStepAuthoring.talkRequestData.onStartTalkAction`
- `ShowTalkStepAuthoring.talkRequestData.onCompleteTalkAction`
- `ShowTalkChoiceStepAuthoring.options[*].inlineAction`

Inspector ではこれらを深く展開せず、count / has child / missing branch のような軽量 metadata として表示する。

`ShowTalkChoiceStepAuthoring` の option branch は stable option id を前提にする。reorder / duplicate 後も child badge と Window selection が別 option に移らないことを要求する。

## 13. コンテキストメニュー仕様

最低限のコマンド構成は以下。

1. `Rename Label`
2. `Clear Label`
3. separator
4. `Duplicate Step`
5. `Delete Step`

将来的に追加可。

1. `Move Up`
2. `Move Down`
3. `Expand All Children`
4. `Collapse All Children`

## 14. 実装配置方針

editor-only 実装は以下へ置く。

```text
Assets/Scripts/Editor/Action/
```

想定 utility:

- `InlineActionDrawer`
- `InlineActionEditorState`
- `ActionStepSummaryUtility`
- `ActionStepChildSlotUtility`
- `ActionStepManagedReferenceUtility`
- `ActionStepPickerDropdown`
- `ActionStepContextMenuUtility`
- `ActionInlineWindowLauncher`

## 15. 検証項目

### 15.1 手動確認

1. TalkRequestData の start / complete action
2. NPCObjectMB の interactionAction
3. CameraPathPointAuthoringMB の onArriveAction
4. CameraPathContracts 内 onArriveAction
5. If / SubAction / TalkChoice の nested InlineAction

確認観点:

1. 一覧性
2. summary の妥当性
3. DisplayName 非表示
4. 右クリック rename
5. Clear Label
6. Add / Duplicate / Delete / Reorder
7. Undo / Redo
8. Prefab override

### 15.2 editor test

可能なら以下を edit mode test で押さえる。

1. empty InlineAction の高さと empty state
2. `DisplayName` があるとき summary が override されること
3. `DisplayName` が空のとき step テンプレートへ戻ること
4. rename 確定 / キャンセル
5. managed reference step 追加時の型設定
6. nested InlineAction の foldout 高さ
7. child slot descriptor 由来の child badge
8. `Open in Window` 導線の property-bound target

既存パターンの再利用元:

- `Assets/Tests/EditMode/ReactiveValue/ReactiveValueM7EditorDrawerTests.cs`
- `Assets/Tests/EditMode/ReactiveValue/ReactiveValueTestUtility.cs`

## 16. フェーズ計画

1. 対象面の固定
2. drawer / utility 基盤追加
3. compact-plus 行ヘッダ実装
4. DisplayName の右クリック rename 実装
5. 既存 drawer 再利用による詳細編集統合
6. 検索付き step picker と追加操作改善
7. editor test と仕上げ

## 17. 最終決定事項

- InlineAction inspector は共通 drawer で統一する。
- DisplayName は通常非表示とする。
- DisplayName は summary override としてのみ使う。
- DisplayName の編集は右クリック `Rename Label` に統一する。
- rename UI は inline rename mode とする。
- 行ヘッダ密度は compact-plus を採用する。
- summary は DisplayName 優先、未設定時は deterministic な自動生成を行う。
- 低頻度操作はコンテキストメニューへ寄せる。
- 深い nested structure の把握と編集は `ActionInlineWindowSpec.md` の Window へ委譲する。
- Inspector には `Open in Window` 導線を置く。
- child action の検出は child slot descriptor 契約へ寄せる。
- direct / indirect child は Inspector 上では軽量 metadata として扱う。
