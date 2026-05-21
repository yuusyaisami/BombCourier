using BC.Editor.Foundation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BC.Editor.Foundation.UIToolkit
{
    public abstract class EditorWindowBase : EditorWindow
    {
        private VisualElement toolbar;
        private VisualElement content;
        private VisualElement footer;

        public VisualElement Toolbar => toolbar;
        public VisualElement Content => content;
        public VisualElement Footer => footer;

        public virtual void CreateGUI()
        {
            rootVisualElement.Clear();
            ApplyRootStyle(rootVisualElement);

            toolbar = new VisualElement { name = "toolbar" };
            content = new VisualElement { name = "content" };
            footer = new VisualElement { name = "footer" };

            ApplyToolbarStyle(toolbar);
            ApplyContentStyle(content);
            ApplyFooterStyle(footer);

            rootVisualElement.Add(toolbar);
            rootVisualElement.Add(content);
            rootVisualElement.Add(footer);

            BuildToolbar(toolbar);
            BuildContent(content);
            BuildFooter(footer);
        }

        protected virtual void BuildToolbar(VisualElement root)
        {
        }

        protected abstract void BuildContent(VisualElement root);

        protected virtual void BuildFooter(VisualElement root)
        {
        }

        protected void RequestRepaint()
        {
            Repaint();
        }

        private static void ApplyRootStyle(VisualElement root)
        {
            root.style.flexGrow = 1f;
            root.style.backgroundColor = EditorThemeTokens.WindowBackground;
        }

        private static void ApplyToolbarStyle(VisualElement element)
        {
            element.style.flexDirection = FlexDirection.Row;
            element.style.alignItems = Align.Center;
            element.style.minHeight = 24f;
            element.style.paddingLeft = EditorThemeTokens.PanePadding;
            element.style.paddingRight = EditorThemeTokens.PanePadding;
            element.style.backgroundColor = EditorThemeTokens.PanelBackground;
        }

        private static void ApplyContentStyle(VisualElement element)
        {
            element.style.flexGrow = 1f;
        }

        private static void ApplyFooterStyle(VisualElement element)
        {
            element.style.flexDirection = FlexDirection.Row;
            element.style.alignItems = Align.Center;
            element.style.minHeight = 20f;
            element.style.paddingLeft = EditorThemeTokens.PanePadding;
            element.style.paddingRight = EditorThemeTokens.PanePadding;
            element.style.backgroundColor = EditorThemeTokens.PanelBackground;
        }
    }
}
