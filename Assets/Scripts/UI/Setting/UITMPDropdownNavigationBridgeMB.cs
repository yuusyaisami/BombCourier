using System.Collections.Generic;
using System.Reflection;
using BC.UI.Components;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BC.UI
{
    /// <summary>
    /// TMP_Dropdown の展開時フォーカスを安定化し、閉じた後に root dropdown へ選択を戻す。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UITMPDropdownNavigationBridgeMB : MonoBehaviour
    {
        private static readonly FieldInfo RuntimeDropdownField =
            typeof(TMP_Dropdown).GetField("m_Dropdown", BindingFlags.Instance | BindingFlags.NonPublic);

        [SerializeField] private TMP_Dropdown dropdown;

        private bool wasExpanded;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
            wasExpanded = dropdown != null && dropdown.IsExpanded;
        }

        private void Update()
        {
            if (dropdown == null)
                return;

            bool isExpanded = dropdown.IsExpanded;
            if (!wasExpanded && isExpanded)
                HandleExpandedAsync(destroyCancellationToken).Forget();
            else if (wasExpanded && !isExpanded)
                RestoreRootSelectionAsync(destroyCancellationToken).Forget();

            wasExpanded = isExpanded;
        }

        private void EnsureInitialized()
        {
            dropdown ??= GetComponent<TMP_Dropdown>();
        }

        private async UniTaskVoid HandleExpandedAsync(System.Threading.CancellationToken ct)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
            if (dropdown == null || !dropdown.IsExpanded)
                return;

            GameObject runtimeDropdownList = ResolveRuntimeDropdownList();
            if (runtimeDropdownList == null)
                return;

            IReadOnlyList<Toggle> itemToggles = ResolveActiveItemToggles(runtimeDropdownList);
            ApplyItemNavigation(itemToggles);
            SelectCurrentValueItem(itemToggles);
        }

        private async UniTaskVoid RestoreRootSelectionAsync(System.Threading.CancellationToken ct)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
            if (dropdown == null || dropdown.IsExpanded || EventSystem.current == null)
                return;

            EventSystem.current.SetSelectedGameObject(dropdown.gameObject);
        }

        private GameObject ResolveRuntimeDropdownList()
        {
            return RuntimeDropdownField?.GetValue(dropdown) as GameObject;
        }

        private static IReadOnlyList<Toggle> ResolveActiveItemToggles(GameObject runtimeDropdownList)
        {
            Toggle[] toggles = runtimeDropdownList != null
                ? runtimeDropdownList.GetComponentsInChildren<Toggle>(includeInactive: false)
                : new Toggle[0];

            return toggles;
        }

        private static void ApplyItemNavigation(IReadOnlyList<Toggle> itemToggles)
        {
            for (int i = 0; i < itemToggles.Count; i++)
            {
                Toggle toggle = itemToggles[i];
                if (toggle == null)
                    continue;

                Navigation navigation = toggle.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnUp = i > 0 ? itemToggles[i - 1] : null;
                navigation.selectOnDown = i + 1 < itemToggles.Count ? itemToggles[i + 1] : null;
                navigation.selectOnLeft = null;
                navigation.selectOnRight = null;
                toggle.navigation = navigation;
            }
        }

        private void SelectCurrentValueItem(IReadOnlyList<Toggle> itemToggles)
        {
            if (itemToggles.Count == 0 || EventSystem.current == null)
                return;

            int selectedIndex = Mathf.Clamp(dropdown.value, 0, itemToggles.Count - 1);
            Toggle selectedToggle = itemToggles[selectedIndex];
            if (selectedToggle == null)
                return;

            EventSystem.current.SetSelectedGameObject(selectedToggle.gameObject);

            UISelectableFocusMB itemFocus = selectedToggle.GetComponent<UISelectableFocusMB>();
            itemFocus?.SetFocusedImmediate(true);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            EnsureInitialized();
        }

        private void OnValidate()
        {
            EnsureInitialized();
        }
#endif
    }
}
