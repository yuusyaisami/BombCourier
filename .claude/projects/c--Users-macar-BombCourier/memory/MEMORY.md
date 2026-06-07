# Memory Index

- [GameLogic.* keys: SceneKernel or Entity scope](gamelogic-keys-require-scenekernel-scope.md) — GameLogic.* は共有(SceneKernel)/エンティティ単位(Entity)どちらも可。ただし起動 Writer は Entity を SceneKernel に自動補正する落とし穴。
- [UI nav uses project-wide input actions](ui-navigation-uses-project-wide-input-actions.md) — UI フォーカス/SE は EventSystem 選択経路に統一。UI モジュールは project-wide InputSystem_Actions を使い、UINavigationBootstrap.EnsureConfigured() を表示時に呼ぶ。
