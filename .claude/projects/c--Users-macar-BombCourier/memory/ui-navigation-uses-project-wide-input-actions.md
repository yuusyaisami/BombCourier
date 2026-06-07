---
name: ui-navigation-uses-project-wide-input-actions
description: UI nav/focus/SE は EventSystem 選択経路に統一。EventSystem の UI モジュールは project-wide の InputSystem_Actions を使うこと
metadata:
  type: project
---

UI のフォーカス表示と Focus/Select SE は **EventSystem の選択（`OnSelect`/`OnDeselect`）** に収束する設計。ポインターは `OnPointerEnter`→`Select()` で、ナビゲーションは Move 入力で、どちらも `SetSelectedGameObject` 経由で `OnSelect` を発火させる。

落とし穴（2026-06 に修正）:
- project-wide 入力アセットは `Assets/InputSystem_Actions.inputactions`（guid `052faaac586de48259a63d0c4782560b`、`InputSystem.actions` が返す）。シーンの EventSystem `InputSystemUIInputModule` が **package 既定 `DefaultInputActions`（guid `ca9f...`）** を参照しているとアセットが二重になり、ナビ Move/SE が発火しない（マウスだけ動く）。両アセットの UI マップのアクション id は完全一致するので fileID を保ったまま guid 差し替えで repoint 可能。
- 実行時は [UINavigationBootstrap.cs](../../../Assets/Scripts/UI/Components/UINavigationBootstrap.cs)（`BC.UI.Components`）の `EnsureConfigured()` が、モジュールを `InputSystem.actions` に統一・UI マップ Enable・`sendNavigationEvents=true` を保証する。**新規 UI 画面は表示時にこれを呼ぶ**。`EnsureSelection(fallback)` で初期選択も保証する。
- フォーカス SE は [UIButtonMB](../../../Assets/Scripts/UI/Components/UIButtonMB.cs) だけでなく [UISelectableFocusMB](../../../Assets/Scripts/UI/Components/UISelectableFocusMB.cs) でも鳴る（`GameSoundDataManagerMB.PlayUIFocus(override)`）。スライダー/トグル/ドロップダウン/ステージ項目に付与される。

**Why:** Title/Settings でナビ操作と Focus/Select SE が効かずマウスだけ動く不具合の原因が、UI モジュールの入力アセット不一致だった。
**How to apply:** UI ナビを追加する画面では `UINavigationBootstrap.EnsureConfigured()` を表示時に呼び、初期選択を入れる。カスタム入力で `InputSystem.actions.FindAction("UI/...")` を使うなら UI マップが Enable 済みか確認。[[gamelogic-keys-require-scenekernel-scope]] と同様、プロジェクト固有の入力配線の前提。
