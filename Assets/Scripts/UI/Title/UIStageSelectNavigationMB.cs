using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace BC.UI.Title
{
    // ステージセレクトページのカスタムフォーカスナビゲーションを担当する。
    // InputSystem の Navigate アクションを購読し、ページを跨いだ方向移動も処理する。
    //
    // レイアウト想定:
    //   Page 1: Index 0-3 (上段) / Index 4-7 (下段)   合計 8 ステージ
    //   Page 2: Index 8-11 (中央 4)
    //
    //   上段: [0][1][2][3]
    //   下段: [4][5][6][7]
    //   → Right from 3 → 4, Right from 7 → page2 item 8
    //   → Left  from 4 → 3, Left  from 8 → page1 item 7
    public sealed class UIStageSelectNavigationMB : MonoBehaviour
    {
        [SerializeField] private UIStageSelectPageMB stageSelectPage;

        // Page1: items[0..7], Page2: items[8..11]
        // 外部から UIStageSelectPageMB が設定する。
        private UIStageSelectItemMB[] items;
        private int focusedIndex = 0;

        // Navigate アクション参照
        private InputAction navigateAction;
        private InputAction submitAction;
        private InputAction cancelAction;

        public void Initialize(UIStageSelectItemMB[] allItems)
        {
            items = allItems;
        }

        private void OnEnable()
        {
            // project-wide UI マップが無効だと performed が発火しないため、念のため有効化しておく。
            InputSystem.actions?.FindActionMap("UI")?.Enable();

            navigateAction = InputSystem.actions?.FindAction("UI/Navigate");
            submitAction = InputSystem.actions?.FindAction("UI/Submit");
            cancelAction = InputSystem.actions?.FindAction("UI/Cancel");

            if (navigateAction != null) navigateAction.performed += OnNavigate;
            if (cancelAction != null) cancelAction.performed += OnCancel;
        }

        private void OnDisable()
        {
            if (navigateAction != null) navigateAction.performed -= OnNavigate;
            if (cancelAction != null) cancelAction.performed -= OnCancel;
        }

        private void Update()
        {
            if (stageSelectPage == null || !stageSelectPage.IsShowing || stageSelectPage.IsModalInputActive)
                return;

            SyncFocusWithEventSystemSelection();
        }

        /// <summary>外部からフォーカスを設定する（ページ切り替え時など）。</summary>
        public void SetFocus(int index, bool fireEvent = true)
        {
            if (items == null || index < 0 || index >= items.Length) return;

            if (items[index] == null || !items[index].CanReceiveNavigationFocus())
                return;

            focusedIndex = index;

            // 常に 1 件だけ focused を有効にして、表示ズレを自己修復する。
            for (int i = 0; i < items.Length; i++)
            {
                UIStageSelectItemMB item = items[i];
                if (item == null)
                    continue;

                item.SetFocused(i == focusedIndex);
            }

            GameObject selectionObject = items[focusedIndex].GetSelectionObject();
            if (selectionObject != null && EventSystem.current != null && EventSystem.current.currentSelectedGameObject != selectionObject)
                EventSystem.current?.SetSelectedGameObject(selectionObject);
        }

        private void OnNavigate(InputAction.CallbackContext ctx)
        {
            if (stageSelectPage == null || !stageSelectPage.IsShowing || stageSelectPage.IsModalInputActive) return;

            Vector2 dir = ctx.ReadValue<Vector2>();

            int dx = 0;
            int dy = 0;
            if (dir.x > 0.5f) dx = 1;
            if (dir.x < -0.5f) dx = -1;
            if (dir.y > 0.5f) dy = -1; // UI では Y 軸反転 (上=前の行)
            if (dir.y < -0.5f) dy = 1;

            if (dx == 0 && dy == 0) return;

            MoveFocus(dx, dy);
        }

        private void OnCancel(InputAction.CallbackContext ctx)
        {
            if (stageSelectPage == null || !stageSelectPage.IsShowing || stageSelectPage.IsModalInputActive) return;
            TitleSceneManagerMB.Instance?.GoToMainPageAsync(false, stageSelectPage.destroyCancellationToken).Forget();
        }

        private void MoveFocus(int dx, int dy)
        {
            if (items == null || items.Length == 0) return;

            int current = focusedIndex;
            int page1Count = 8; // Page1 は index 0-7
            int rowSize = 4; // 1 行あたり 4 ステージ

            int next = current;

            // 横移動
            if (dx != 0)
            {
                next = GetHorizontalNext(current, dx, page1Count, rowSize);
            }
            // 縦移動
            else if (dy != 0)
            {
                next = GetVerticalNext(current, dy, page1Count, rowSize);
            }

            if (next == current) return;

            if (items[next] == null || !items[next].CanReceiveNavigationFocus())
                return;

            // ページをまたぐかどうか
            int currentPage = (current < page1Count) ? 0 : 1;
            int nextPage = (next < page1Count) ? 0 : 1;

            if (nextPage != currentPage)
            {
                stageSelectPage.SwitchToPageAsync(nextPage, stageSelectPage.destroyCancellationToken)
                    .ContinueWith(() => SetFocus(next))
                    .Forget();
            }
            else
            {
                SetFocus(next);
            }
        }

        private int GetHorizontalNext(int current, int dx, int page1Count, int rowSize)
        {
            int totalCount = items.Length;

            // Page1 上段 (0-3)
            if (current < rowSize)
            {
                int candidate = current + dx;
                if (candidate >= 0 && candidate < rowSize) return candidate;
                if (dx > 0 && current == rowSize - 1) return current + rowSize; // 上段右端 → 下段左端(4)
                return current;
            }
            // Page1 下段 (4-7)
            if (current < page1Count)
            {
                int rowIndex = current - rowSize; // 0-3
                int candidate = rowIndex + dx;
                if (candidate >= 0 && candidate < rowSize) return rowSize + candidate; // 同行内
                if (dx > 0 && rowIndex == rowSize - 1) return page1Count; // 下段右端 → Page2 先頭 (8)
                if (dx < 0 && rowIndex == 0) return rowSize - 1; // 下段左端 → 上段右端 (3)
                return current;
            }
            // Page2 (8-11): 1 行 4 ステージ
            {
                int rowIndex = current - page1Count; // 0-3
                int candidate = rowIndex + dx;
                if (candidate >= 0 && candidate < rowSize)
                    return page1Count + candidate;
                if (dx < 0 && rowIndex == 0)
                    return page1Count - 1; // Page2 左端 → Page1 下段右端 (7)
                return current;
            }
        }

        private int GetVerticalNext(int current, int dy, int page1Count, int rowSize)
        {
            // Page1 上段 ↓ → 下段
            if (current < rowSize && dy > 0)
                return current + rowSize;

            // Page1 下段 ↑ → 上段
            if (current >= rowSize && current < page1Count && dy < 0)
                return current - rowSize;

            return current; // Page2 は 1 行のみなので縦移動なし
        }

        private void SyncFocusWithEventSystemSelection()
        {
            if (items == null || items.Length == 0)
                return;

            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected == null)
                return;

            int selectedIndex = -1;
            for (int i = 0; i < items.Length; i++)
            {
                UIStageSelectItemMB item = items[i];
                if (item == null)
                    continue;

                if (item.IsSelectionTarget(selected))
                {
                    selectedIndex = i;
                    break;
                }
            }

            if (selectedIndex < 0 || selectedIndex == focusedIndex)
                return;

            SetFocus(selectedIndex);
        }
    }
}
