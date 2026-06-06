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
        private static void ApplyProjectWideActions(InputSystemUIInputModule module)
        {
            if (module == null)
                return;

            InputActionAsset actions = InputSystem.actions;
            if (actions == null)
            {
                // project-wide 未設定環境のフォールバック(package 既定)。
                if (module.actionsAsset == null)
                    module.AssignDefaultActions();
                return;
            }

            if (module.actionsAsset == actions)
                return;

            module.actionsAsset = actions;
            module.point = ResolveActionReference(actions, "UI/Point");
            module.leftClick = ResolveActionReference(actions, "UI/Click");
            module.rightClick = ResolveActionReference(actions, "UI/RightClick");
            module.middleClick = ResolveActionReference(actions, "UI/MiddleClick");
            module.scrollWheel = ResolveActionReference(actions, "UI/ScrollWheel");
            module.move = ResolveActionReference(actions, "UI/Navigate");
            module.submit = ResolveActionReference(actions, "UI/Submit");
            module.cancel = ResolveActionReference(actions, "UI/Cancel");

            // TrackedDevice 系は存在すれば貼る(未使用でも害なし)。
            InputActionReference trackedPosition = ResolveActionReference(actions, "UI/TrackedDevicePosition");
            if (trackedPosition != null)
                module.trackedDevicePosition = trackedPosition;

            InputActionReference trackedOrientation = ResolveActionReference(actions, "UI/TrackedDeviceOrientation");
            if (trackedOrientation != null)
                module.trackedDeviceOrientation = trackedOrientation;
        }

        private static InputActionReference ResolveActionReference(InputActionAsset actions, string actionPath)
        {
            InputAction action = actions.FindAction(actionPath);
            return action != null ? InputActionReference.Create(action) : null;
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
