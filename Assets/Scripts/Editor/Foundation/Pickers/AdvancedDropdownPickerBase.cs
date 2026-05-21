using System;
using System.Collections.Generic;
using BC.Editor.Foundation;
using UnityEditor.IMGUI.Controls;

namespace BC.Editor.Foundation.Pickers
{
    public abstract class AdvancedDropdownPickerBase<TDescriptor> : AdvancedDropdown
    {
        private readonly IReadOnlyList<TDescriptor> descriptors;
        private readonly Action<TDescriptor> onSelected;

        protected AdvancedDropdownPickerBase(
            AdvancedDropdownState state,
            IReadOnlyList<TDescriptor> descriptors,
            Action<TDescriptor> onSelected)
            : base(state)
        {
            this.descriptors = descriptors ?? Array.Empty<TDescriptor>();
            this.onSelected = onSelected;
            minimumSize = new UnityEngine.Vector2(EditorThemeTokens.MinimumPickerWidth, 240f);
        }

        protected IReadOnlyList<TDescriptor> Descriptors => descriptors;

        protected sealed override AdvancedDropdownItem BuildRoot()
        {
            AdvancedDropdownItem root = new(GetRootName());

            if (descriptors.Count == 0)
            {
                root.AddChild(new AdvancedDropdownItem(GetEmptyLabel()));
                return root;
            }

            Dictionary<string, AdvancedDropdownItem> groups = new(StringComparer.Ordinal);

            for (int i = 0; i < descriptors.Count; i++)
            {
                TDescriptor descriptor = descriptors[i];
                string path = GetPath(descriptor);
                AdvancedDropdownItem parent = root;

                if (!string.IsNullOrWhiteSpace(path))
                    parent = BuildGroups(root, groups, path);

                parent.AddChild(new DescriptorItem(GetDisplayName(descriptor), descriptor));
            }

            return root;
        }

        protected sealed override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is DescriptorItem descriptorItem)
                onSelected?.Invoke(descriptorItem.Descriptor);
        }

        protected virtual string GetRootName()
        {
            return "Select";
        }

        protected virtual string GetEmptyLabel()
        {
            return "No matching items";
        }

        protected abstract string GetDisplayName(TDescriptor descriptor);

        protected virtual string GetPath(TDescriptor descriptor)
        {
            return string.Empty;
        }

        private static AdvancedDropdownItem BuildGroups(
            AdvancedDropdownItem root,
            IDictionary<string, AdvancedDropdownItem> groups,
            string path)
        {
            string[] segments = path.Split(new[] { '/', '.' }, StringSplitOptions.RemoveEmptyEntries);
            AdvancedDropdownItem parent = root;
            string currentPath = string.Empty;

            for (int i = 0; i < segments.Length; i++)
            {
                currentPath = string.IsNullOrEmpty(currentPath)
                    ? segments[i]
                    : $"{currentPath}/{segments[i]}";

                if (!groups.TryGetValue(currentPath, out AdvancedDropdownItem group))
                {
                    group = new AdvancedDropdownItem(segments[i]);
                    groups.Add(currentPath, group);
                    parent.AddChild(group);
                }

                parent = group;
            }

            return parent;
        }

        private sealed class DescriptorItem : AdvancedDropdownItem
        {
            public DescriptorItem(string name, TDescriptor descriptor)
                : base(string.IsNullOrWhiteSpace(name) ? "(Unnamed)" : name)
            {
                Descriptor = descriptor;
            }

            public TDescriptor Descriptor { get; }
        }
    }
}
