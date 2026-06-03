using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BC.UI
{
    /// <summary>
    /// settings 画面内の Selectable navigation を 1 箇所で明示配線する。
    /// 自動距離判定に依存せず、Scene ごとの差分は entry の並びと接続先だけに閉じ込める。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UISelectableNavigationMapMB : MonoBehaviour
    {
        [Serializable]
        public sealed class NavigationEntry
        {
            public Selectable selectable;
            public Selectable up;
            public Selectable down;
            public Selectable left;
            public Selectable right;
        }

        [SerializeField] private List<NavigationEntry> entries = new();

        public IReadOnlyList<NavigationEntry> Entries => entries;

        public void SetEntries(IEnumerable<NavigationEntry> newEntries)
        {
            entries.Clear();
            if (newEntries == null)
                return;

            entries.AddRange(newEntries);
        }

        public void Apply()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                NavigationEntry entry = entries[i];
                if (entry == null || entry.selectable == null)
                    continue;

                Navigation navigation = entry.selectable.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnUp = entry.up != null ? entry.up : navigation.selectOnUp;
                navigation.selectOnDown = entry.down != null ? entry.down : navigation.selectOnDown;
                navigation.selectOnLeft = entry.left != null ? entry.left : navigation.selectOnLeft;
                navigation.selectOnRight = entry.right != null ? entry.right : navigation.selectOnRight;
                entry.selectable.navigation = navigation;
            }
        }
    }
}
