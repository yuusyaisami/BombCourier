using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace BC.UI.Components
{
    /// <summary>
    /// ポインター操作とナビゲーション操作を同じ EventSystem 選択経路へ統一するための共有ブートストラップ。
    /// EventSystem / InputSystemUIInputModule を必ず用意し、プロジェクト全体(project-wide)の
    /// InputSystem_Actions を使うように揃えて UI アクションマップを有効化する。
    /// 各 UI 画面は表示時にこれを呼ぶことで、入力アセットのズレ(モジュールが package 既定アセットを使い、
    /// カスタムナビが project-wide を読む)による「ナビが効かない/SE が鳴らない」を解消する。
    /// </summary>
    public static class UINavigationBootstrap
    {
        private const string UiActionMapName = "UI";

        /// <summary>EventSystem と UI 入力モジュールを生成/設定し、project-wide アクションへ統一する。</summary>
        public static EventSystem EnsureConfigured()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
                eventSystem = Object.FindAnyObjectByType<EventSystem>();

            if (eventSystem == null)
            {
                var eventSystemObject = new GameObject("EventSystem");
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
            }

            eventSystem.sendNavigationEvents = true;

            InputSystemUIInputModule uiInputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (uiInputModule == null)
                uiInputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

            ApplyProjectWideActions(uiInputModule);

            if (!uiInputModule.enabled)
                uiInputModule.enabled = true;

            EnableUiActionMap();

            return eventSystem;
        }

        /// <summary>有効な選択が無い(null/非アクティブ)場合に fallback を選択し、ナビの起点を保証する。</summary>
        public static void EnsureSelection(GameObject fallback)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null || fallback == null || !fallback.activeInHierarchy)
                return;

            GameObject current = eventSystem.currentSelectedGameObject;
            if (current != null && current.activeInHierarchy)
                return;

            eventSystem.SetSelectedGameObject(fallback);
        }

        // モジュールの入力アセットを project-wide の InputSystem_Actions に統一する。
        // actionsAsset が既に正しい場合は AssignDefaultActions() を呼ばない。
        // 不必要な AssignDefaultActions() 呼び出しはモジュールの再初期化を引き起こし、
        // 既に正しくシリアライズされた Submit アクション購読を壊す原因になる。
        private static void ApplyProjectWideActions(InputSystemUIInputModule module)
        {
            if (module == null)
                return;

            InputActionAsset actions = InputSystem.actions;
            if (actions == null)
                return;

            if (module.actionsAsset != actions)
            {
                // actionsAsset が異なる場合のみ差し替えて標準参照を再バインドする。
                module.actionsAsset = actions;
                module.AssignDefaultActions();
            }
            // actionsAsset が既に正しい場合はシリアライズ済みの参照をそのまま使う。
        }

        private static void EnableUiActionMap()
        {
            InputActionAsset actions = InputSystem.actions;
            InputActionMap uiMap = actions != null ? actions.FindActionMap(UiActionMapName, throwIfNotFound: false) : null;
            if (uiMap != null && !uiMap.enabled)
                uiMap.Enable();
        }
    }
}
