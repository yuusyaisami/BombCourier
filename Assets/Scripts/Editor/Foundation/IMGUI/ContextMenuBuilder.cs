using System;
using UnityEditor;
using UnityEngine;

namespace BC.Editor.Foundation.IMGUI
{
    public sealed class ContextMenuBuilder
    {
        private readonly GenericMenu menu = new();
        private readonly Action repaint;

        public ContextMenuBuilder(Action repaint = null)
        {
            this.repaint = repaint;
        }

        public ContextMenuBuilder AddItem(string label, bool enabled, Action action)
        {
            if (enabled)
            {
                menu.AddItem(new GUIContent(label), false, () =>
                {
                    action?.Invoke();
                    repaint?.Invoke();
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent(label));
            }

            return this;
        }

        public ContextMenuBuilder AddCheckedItem(string label, bool enabled, bool checkedValue, Action action)
        {
            if (enabled)
            {
                menu.AddItem(new GUIContent(label), checkedValue, () =>
                {
                    action?.Invoke();
                    repaint?.Invoke();
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent(label), checkedValue);
            }

            return this;
        }

        public ContextMenuBuilder AddSeparator(string path = "")
        {
            menu.AddSeparator(path ?? string.Empty);
            return this;
        }

        public void ShowAsContext()
        {
            menu.ShowAsContext();
        }

        public void DropDown(UnityEngine.Rect position)
        {
            menu.DropDown(position);
        }
    }
}
