using System;
using System.Collections.Generic;
using BC.Base;
using UnityEngine;

namespace BC.UI
{
    // Tutorial ToDo の表示を受け持つ UI コンポーネント。
    // 新しいチュートリアル runtime では条件監視を持たず、現在 step の表示状態だけを反映する。
    public sealed class UITutorialToDoListMB : MonoBehaviour
    {
        [SerializeField] private TutorialToDoListSO defaultData;
        [SerializeField] private RectTransform itemContainer;
        [SerializeField] private UITutorialToDoItemMB itemPrefab;

        // ------------------------------------------------------------------
        // Runtime state
        // ------------------------------------------------------------------

        private TutorialToDoListSO         currentData;
        private ValueStoreService          currentStore;
        private EntityRef                  currentPlayerEntity;

        private readonly List<UITutorialToDoItemMB> runtimeItems = new();
        private bool[] completedFlags = Array.Empty<bool>();

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        public bool AllCompleted
        {
            get
            {
                if (completedFlags.Length == 0) return false;
                foreach (bool flag in completedFlags)
                    if (!flag) return false;
                return true;
            }
        }

        /// <summary>全アイテム達成時に発火する。</summary>
        public event Action OnAllCompleted;

        // ------------------------------------------------------------------
        // Unity lifecycle
        // ------------------------------------------------------------------

        private void OnDestroy()
        {
            UnsubscribeAll();
        }

        // ------------------------------------------------------------------
        // Public methods
        // ------------------------------------------------------------------

        /// <summary>
        /// 指定データでリストを初期化し、条件監視を開始する。
        /// data が null の場合は defaultData を使用する。
        /// </summary>
        public void Initialize(TutorialToDoListSO data, EntityRef playerEntity, ValueStoreService store)
        {
            UnsubscribeAll();
            ClearItems();

            currentData         = data != null ? data : defaultData;
            currentPlayerEntity = playerEntity;
            currentStore        = store;

            if (currentData == null) return;

            int count = currentData.Items.Count;
            completedFlags = new bool[count];

            for (int i = 0; i < count; i++)
            {
                TutorialToDoItemDefinition definition = currentData.Items[i];

                UITutorialToDoItemMB item = null;
                if (itemPrefab != null && itemContainer != null)
                {
                    item = UnityEngine.Object.Instantiate(itemPrefab, itemContainer);
                    item.Setup(definition.LabelText);
                }
                runtimeItems.Add(item);

                int capturedIndex = i;
                definition.Condition?.Subscribe(
                    currentStore, currentPlayerEntity,
                    completedIndex => SetItemCompleted(completedIndex, true),
                    capturedIndex);
            }
        }

        public void ShowChecklist(IReadOnlyList<string> labels, IReadOnlyList<bool> completedStates = null)
        {
            UnsubscribeAll();
            ClearItems();

            if (labels == null || labels.Count == 0)
            {
                Hide();
                return;
            }

            gameObject.SetActive(true);
            completedFlags = new bool[labels.Count];

            for (int i = 0; i < labels.Count; i++)
            {
                bool completed = completedStates != null && i < completedStates.Count && completedStates[i];
                completedFlags[i] = completed;

                UITutorialToDoItemMB item = null;
                if (itemPrefab != null && itemContainer != null)
                {
                    item = UnityEngine.Object.Instantiate(itemPrefab, itemContainer);
                    item.Setup(labels[i]);
                    item.SetCompleted(completed);
                }

                runtimeItems.Add(item);
            }
        }

        public void Hide()
        {
            Clear();
            gameObject.SetActive(false);
        }

        public void Clear()
        {
            UnsubscribeAll();
            ClearItems();
            currentData = null;
            currentStore = null;
            currentPlayerEntity = default;
        }

        /// <summary>指定インデックスのアイテムの達成状態を手動で設定する。</summary>
        public void SetItemCompleted(int index, bool completed)
        {
            if (index < 0 || index >= completedFlags.Length) return;
            if (completedFlags[index] == completed) return;

            completedFlags[index] = completed;
            if (index < runtimeItems.Count)
                runtimeItems[index]?.SetCompleted(completed);

            if (completed && AllCompleted)
                OnAllCompleted?.Invoke();
        }

        /// <summary>全アイテムをリセットし、条件監視を再開する。</summary>
        public void ResetAll()
        {
            UnsubscribeAll();

            for (int i = 0; i < completedFlags.Length; i++)
            {
                completedFlags[i] = false;
                if (i < runtimeItems.Count)
                    runtimeItems[i]?.SetCompleted(false);
            }

            if (currentData == null) return;
            int count = Mathf.Min(currentData.Items.Count, completedFlags.Length);
            for (int i = 0; i < count; i++)
            {
                int capturedIndex = i;
                currentData.Items[i].Condition?.Subscribe(
                    currentStore, currentPlayerEntity,
                    completedIndex => SetItemCompleted(completedIndex, true),
                    capturedIndex);
            }
        }

        // ------------------------------------------------------------------
        // Private helpers
        // ------------------------------------------------------------------

        private void UnsubscribeAll()
        {
            if (currentData == null) return;
            foreach (TutorialToDoItemDefinition def in currentData.Items)
                def.Condition?.Unsubscribe();
        }

        private void ClearItems()
        {
            foreach (UITutorialToDoItemMB item in runtimeItems)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            runtimeItems.Clear();
            completedFlags = Array.Empty<bool>();
        }
    }
}
