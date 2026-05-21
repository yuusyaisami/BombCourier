using BC.Editor.Foundation;
using UnityEngine.UIElements;

namespace BC.Editor.Foundation.UIToolkit
{
    public abstract class SplitViewWindowBase : EditorWindowBase
    {
        private TwoPaneSplitView splitView;
        private VisualElement leftPane;
        private VisualElement rightPane;

        protected VisualElement LeftPane => leftPane;
        protected VisualElement RightPane => rightPane;

        protected virtual float InitialLeftPaneWidth => 320f;

        protected override void BuildContent(VisualElement root)
        {
            splitView = new TwoPaneSplitView(
                0,
                InitialLeftPaneWidth,
                TwoPaneSplitViewOrientation.Horizontal);

            splitView.style.flexGrow = 1f;

            leftPane = new VisualElement { name = "left-pane" };
            rightPane = new VisualElement { name = "right-pane" };

            leftPane.style.flexGrow = 1f;
            rightPane.style.flexGrow = 1f;
            leftPane.style.paddingLeft = EditorThemeTokens.PanePadding;
            leftPane.style.paddingRight = EditorThemeTokens.PanePadding;
            rightPane.style.paddingLeft = EditorThemeTokens.PanePadding;
            rightPane.style.paddingRight = EditorThemeTokens.PanePadding;

            splitView.Add(leftPane);
            splitView.Add(rightPane);
            root.Add(splitView);

            BuildLeftPane(leftPane);
            BuildRightPane(rightPane);
        }

        protected abstract void BuildLeftPane(VisualElement root);

        protected abstract void BuildRightPane(VisualElement root);
    }
}
