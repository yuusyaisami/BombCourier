using System.Collections.Generic;
using BC.Base;
using BC.Inputs;
using BC.Manager;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BC.UI
{
    // 現在プレイヤーが行える操作の一覧を HUD に表示するコンポーネント。
    // baseEntries でベース定義を持ち、UIOperationGuideExtenderMB で追加定義を受け付ける。
    // Update ごとに各エントリの表示条件を評価してアイテムを表示/非表示にする。
    public sealed class UIOperationGuideMB : MonoBehaviour
    {
        [SerializeField] private PlayerInputCatalogMB inputCatalog;

        [Tooltip("常に表示候補に入るベースエントリ。")]
        [SerializeField] private List<OperationGuideEntryDefinition> baseEntries = new();

        [Tooltip("エントリを並べる親 RectTransform。")]
        [SerializeField] private RectTransform container;

        [Tooltip("各エントリに使うプレハブ。UIOperationGuideItemMB を持つ必要がある。")]
        [SerializeField] private UIOperationGuideItemMB itemPrefab;

        // ------------------------------------------------------------------
        // Runtime state
        // ------------------------------------------------------------------

        private readonly List<RuntimeGuideItem> runtimeItems = new();
        private readonly List<UIOperationGuideExtenderMB> extenders = new();

        private EntityMB currentEntityMB;
        private ValueStoreService currentStore;

        private bool isBoundToGameLogic;
        private bool isBoundToInputPrompt;

        // ------------------------------------------------------------------
        // Unity lifecycle
        // ------------------------------------------------------------------

        private void Start()
        {
            TryBindGameLogic();
            TryBindInputPrompt();
            RebuildItems();
        }

        private void OnEnable()
        {
            TryBindInputPrompt();
        }

        private void OnDisable()
        {
            UnbindInputPrompt();
        }

        private void OnDestroy()
        {
            UnbindInputPrompt();
            UnbindGameLogic();
            ClearRuntimeItems();
        }

        private void Update()
        {
            TryBindGameLogic();
            TryBindInputPrompt();
            EvaluateVisibility();
        }

        // ------------------------------------------------------------------
        // Extender API
        // ------------------------------------------------------------------

        /// <summary>
        /// UIOperationGuideExtenderMB から追加エントリを登録する。
        /// 登録後にアイテムリストを再構築する。
        /// </summary>
        public void RegisterExtender(UIOperationGuideExtenderMB extender)
        {
            if (extender == null || extenders.Contains(extender)) return;
            extenders.Add(extender);
            RebuildItems();
        }

        /// <summary>
        /// UIOperationGuideExtenderMB の登録を解除する。
        /// 解除後にアイテムリストを再構築する。
        /// </summary>
        public void UnregisterExtender(UIOperationGuideExtenderMB extender)
        {
            if (extender == null) return;
            if (!extenders.Remove(extender)) return;
            RebuildItems();
        }

        // ------------------------------------------------------------------
        // Bind / Unbind helpers
        // ------------------------------------------------------------------

        private void TryBindGameLogic()
        {
            if (isBoundToGameLogic) return;
            GameLogicManagerMB gm = GameLogicManagerMB.Instance;
            if (gm == null) return;

            gm.OnPlayerSpawned += HandlePlayerSpawned;
            HandlePlayerSpawned(gm.PlayerInstance);
            isBoundToGameLogic = true;
        }

        private void UnbindGameLogic()
        {
            if (!isBoundToGameLogic) return;
            if (GameLogicManagerMB.Instance != null)
                GameLogicManagerMB.Instance.OnPlayerSpawned -= HandlePlayerSpawned;
            isBoundToGameLogic = false;
        }

        private void TryBindInputPrompt()
        {
            if (isBoundToInputPrompt || InputManagerMB.Instance == null) return;
            InputManagerMB.Instance.PromptDeviceKindChanged += HandlePromptDeviceKindChanged;
            isBoundToInputPrompt = true;
        }

        private void UnbindInputPrompt()
        {
            if (!isBoundToInputPrompt) return;
            if (InputManagerMB.Instance != null)
                InputManagerMB.Instance.PromptDeviceKindChanged -= HandlePromptDeviceKindChanged;
            isBoundToInputPrompt = false;
        }

        // ------------------------------------------------------------------
        // Event handlers
        // ------------------------------------------------------------------

        private void HandlePlayerSpawned(PlayerMB player)
        {
            currentEntityMB = null;
            currentStore = null;
            if (player == null) return;

            EntityMB entityMB = player.GetComponent<EntityMB>();
            SceneKernelMB kernel = player.GetComponentInParent<SceneKernelMB>();
            if (entityMB == null || !entityMB.HasEntity || kernel?.Kernel?.ValueStore == null) return;

            currentEntityMB = entityMB;
            currentStore = kernel.Kernel.ValueStore;
        }

        private void HandlePromptDeviceKindChanged(InputPromptDeviceKind _)
        {
            foreach (RuntimeGuideItem item in runtimeItems)
                item.GuideItem?.RefreshIcon(force: true);
        }

        // ------------------------------------------------------------------
        // Item lifecycle
        // ------------------------------------------------------------------

        private void RebuildItems()
        {
            ClearRuntimeItems();
            if (itemPrefab == null || container == null) return;

            foreach (OperationGuideEntryDefinition entry in baseEntries)
                AddItem(entry);

            foreach (UIOperationGuideExtenderMB ext in extenders)
            {
                foreach (OperationGuideEntryDefinition entry in ext.ExtraEntries)
                    AddItem(entry);
            }
        }

        private void AddItem(OperationGuideEntryDefinition entry)
        {
            if (entry == null) return;

            UIOperationGuideItemMB item = Object.Instantiate(itemPrefab, container);
            item.gameObject.SetActive(false);

            InputActionReference actionRef = null;
            inputCatalog?.TryGetAction(entry.ActionCatalogId, out actionRef);

            item.Bind(actionRef, entry.LabelText);
            runtimeItems.Add(new RuntimeGuideItem(item, entry));
        }

        private void ClearRuntimeItems()
        {
            foreach (RuntimeGuideItem item in runtimeItems)
            {
                if (item.GuideItem != null)
                    Destroy(item.GuideItem.gameObject);
            }
            runtimeItems.Clear();
        }

        // ------------------------------------------------------------------
        // Visibility evaluation
        // ------------------------------------------------------------------

        private void EvaluateVisibility()
        {
            bool hasValidPlayer = currentStore != null
                               && currentEntityMB != null
                               && currentEntityMB.HasEntity;
            EntityRef entity = hasValidPlayer ? currentEntityMB.Entity : default;

            foreach (RuntimeGuideItem item in runtimeItems)
            {
                bool visible;
                if (!hasValidPlayer)
                {
                    visible = false;
                }
                else if (item.Entry.VisibilityCondition == null)
                {
                    visible = true;
                }
                else
                {
                    visible = item.Entry.VisibilityCondition.Evaluate(currentStore, entity);
                }

                item.GuideItem.SetVisible(visible);
            }
        }

        // ------------------------------------------------------------------
        // Inner types
        // ------------------------------------------------------------------

        private sealed class RuntimeGuideItem
        {
            public UIOperationGuideItemMB GuideItem { get; }
            public OperationGuideEntryDefinition Entry { get; }

            public RuntimeGuideItem(UIOperationGuideItemMB guideItem, OperationGuideEntryDefinition entry)
            {
                GuideItem = guideItem;
                Entry = entry;
            }
        }
    }
}
