---
name: gamelogic-keys-require-scenekernel-scope
description: GameLogic.* ValueStore keys can be SceneKernel(shared) OR Entity(per-entity) scope; startup writer still auto-remaps Entity->SceneKernel
metadata:
  type: project
---

ValueStore のキーとスコープ対応（`IsKeyCompatible` in [SetValueStoreValueStepAuthoring.cs](../../../Assets/Scripts/Action/Authoring/SetValueStoreValueStepAuthoring.cs)）:
- `Local.*` → Local スコープのみ。
- `Kernel.*`（アプリ永続）→ SceneKernel / ApplicationKernel のみ（Entity 不可）。
- `GameLogic.*` → **SceneKernel（共有）でも Entity（エンティティ単位）でも可**。2026-06 に Entity 許可へ緩和（`IsApplicationKernelDescriptor` で Kernel.* だけ除外）。NPC ごとの会話回数（GameLogic.TalkCount）などに使える。
- その他 → Entity。

落とし穴（重要）:
- `ValueStoreStartupWriterMB` は `GameLogic.*` の Entity スコープを **SceneKernel に自動補正**する（`ResolveEffectiveStoreScope`）。SetValueStoreValue ランタイムやリアクティブ読み取りは補正しない。
- そのため「起動 Writer で GameLogic.X を Entity 指定」しても実際は SceneKernel に書かれる。エンティティ単位で初期化したいなら起動 Writer に頼らず、talk アクション内の SetValueStoreValue(Entity) で初期化するか、ValueKey の既定値(0)に任せる。
- Entity 単位で使うときは、書き込み(SetValueStoreValue の Target)と読み取り(ReactiveSnapshot の EntityValueStore ソース)が **同じエンティティ** を指すこと。

**Why:** 会話分岐（GameLogic.TalkCount で初回/2回目判定）が「ずっと初回」になっていた原因＝増分が Entity スコープで `IsKeyCompatible` に弾かれ黙って失敗していた。ユーザー要望でエンティティ単位の保持を許可した。
**How to apply:** GameLogic.* を per-entity で使うなら増分・If 読み取りを Entity スコープ＋同一ターゲットに統一。増分→判定の順序にも注意（`+= 1` を先にやると初回で `== 0` が偽になる。判定を先にして増分は後）。
