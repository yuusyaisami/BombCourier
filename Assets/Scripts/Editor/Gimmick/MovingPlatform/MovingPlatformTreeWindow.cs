using System;
using System.Collections.Generic;
using BC.Editor.Foundation;
using BC.Editor.Foundation.UIToolkit;
using BC.Gimmick.MovingPlatform;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BC.Editor.Gimmick.MovingPlatformTools
{
    public sealed class MovingPlatformTreeWindow : SplitViewWindowBase
    {
        private const string WindowTitle = "MovingPlatform Tree";
        private const string RootPropertyPath = "treeAuthoring";
        private const string SessionPrefix = "BC.Editor.MovingPlatformTree";
        private const string SessionSelectedPathKey = "selected-path";

        private enum TreeRowKind
        {
            SectionHeader = 0,
            TreeRoot = 1,
            RailNode = 2,
            SelectorNode = 3,
            SelectorStep = 4,
        }

        private readonly SerializedObjectBridge serializedObjectBridge = new();
        private readonly SessionStateViewModel sessionState = new(SessionPrefix);
        private readonly List<TreeRow> rows = new();

        private IMGUIContainerBridge detailBridge;
        private ScrollView detailScrollView;
        private ScrollView treeScrollView;
        private Label targetLabel;
        private Label footerLabel;
        private string selectedPropertyPath = RootPropertyPath;

        private readonly struct TreeRow
        {
            public TreeRow(
                string title,
                string propertyPath,
                int depth,
                bool isSelectable,
                bool isRailNode,
                int railNodeIndex,
                TreeRowKind kind = TreeRowKind.SectionHeader,
                int selectorIndex = -1,
                int stepIndex = -1)
            {
                Title = title;
                PropertyPath = propertyPath;
                Depth = depth;
                IsSelectable = isSelectable;
                IsRailNode = isRailNode;
                RailNodeIndex = railNodeIndex;
                Kind = kind;
                SelectorIndex = selectorIndex;
                StepIndex = stepIndex;
            }

            public string Title { get; }
            public string PropertyPath { get; }
            public int Depth { get; }
            public bool IsSelectable { get; }
            public bool IsRailNode { get; }
            public int RailNodeIndex { get; }
            public TreeRowKind Kind { get; }
            public int SelectorIndex { get; }
            public int StepIndex { get; }
        }

        public static void Open(MovingPlatformMB target)
        {
            MovingPlatformTreeWindow window = GetWindow<MovingPlatformTreeWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(760f, 480f);
            window.Show();
            window.Focus();
            if (target != null)
                window.Bind(target);
        }

        public override void CreateGUI()
        {
            detailBridge ??= new IMGUIContainerBridge("Select a tree item to edit.");
            base.CreateGUI();
            detailBridge.Applied -= RefreshWindow;
            detailBridge.Applied += RefreshWindow;
        }

        protected override void BuildToolbar(VisualElement root)
        {
            targetLabel = new Label("No target");
            targetLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            targetLabel.style.flexGrow = 1f;
            root.Add(targetLabel);

            Button addRailButton = new Button();
            addRailButton.text = "Add Rail Root";
            addRailButton.clicked += CreateRailRoot;
            root.Add(addRailButton);

            Button addSelectorButton = new Button();
            addSelectorButton.text = "Add Selector";
            addSelectorButton.clicked += () => CreateSelectorNode();
            root.Add(addSelectorButton);

            Button refreshButton = new(RefreshWindow) { text = "Refresh" };
            root.Add(refreshButton);

            Button migrateButton = new(TryMigrate) { text = "Migrate" };
            root.Add(migrateButton);
        }

        protected override void BuildLeftPane(VisualElement root)
        {
            treeScrollView = new ScrollView();
            treeScrollView.style.flexGrow = 1f;
            treeScrollView.focusable = true;
            root.Add(treeScrollView);
        }

        protected override void BuildRightPane(VisualElement root)
        {
            detailScrollView = new ScrollView();
            detailScrollView.style.flexGrow = 1f;

            detailBridge.Root.style.flexGrow = 1f;
            detailScrollView.Add(detailBridge.Root);
            root.Add(detailScrollView);
        }

        protected override void BuildFooter(VisualElement root)
        {
            footerLabel = new Label();
            footerLabel.style.flexGrow = 1f;
            root.Add(footerLabel);
        }

        private void Bind(MovingPlatformMB target)
        {
            if (target == null)
                return;

            serializedObjectBridge.Bind(target, RootPropertyPath);
            selectedPropertyPath = NormalizeSelectedPropertyPath(sessionState.GetString(SessionSelectedPathKey, RootPropertyPath));
            RefreshWindow();
        }

        private void RefreshWindow()
        {
            if (!serializedObjectBridge.TryResolveTarget(out UnityEngine.Object resolvedTarget) || resolvedTarget is not MovingPlatformMB movingPlatform)
            {
                rows.Clear();
                treeScrollView?.Clear();
                detailBridge.Clear("Bind a MovingPlatform to edit its tree.");
                if (targetLabel != null)
                    targetLabel.text = "No target";
                if (footerLabel != null)
                    footerLabel.text = "No target";
                return;
            }

            targetLabel.text = movingPlatform.name;
            rows.Clear();
            RebuildRows(movingPlatform);
            RebuildTreeList();
            BindDetailPane();
            UpdateFooter(movingPlatform);
        }

        private void RebuildRows(MovingPlatformMB movingPlatform)
        {
            MovingPlatformTreeAuthoring authoring = movingPlatform.TreeAuthoring ?? new MovingPlatformTreeAuthoring();
            rows.Add(new TreeRow("Tree Root", RootPropertyPath, 0, true, false, -1, TreeRowKind.TreeRoot));
            rows.Add(new TreeRow("Rails", string.Empty, 0, false, false, -1, TreeRowKind.SectionHeader));

            IReadOnlyList<MovingPlatformRailNodeAuthoring> railNodes = authoring.RailNodes ?? System.Array.Empty<MovingPlatformRailNodeAuthoring>();
            var childrenByParent = new Dictionary<string, List<int>>(System.StringComparer.Ordinal);
            for (int i = 0; i < railNodes.Count; i++)
            {
                MovingPlatformRailNodeAuthoring railNode = railNodes[i];
                if (railNode == null)
                    continue;

                string parentId = railNode.ParentRailNodeId;
                if (!childrenByParent.TryGetValue(parentId, out List<int> children))
                {
                    children = new List<int>();
                    childrenByParent.Add(parentId, children);
                }

                children.Add(i);
            }

            string rootRailId = authoring.RootRailNodeId;
            if (!string.IsNullOrWhiteSpace(rootRailId))
            {
                for (int i = 0; i < railNodes.Count; i++)
                {
                    if (railNodes[i] != null && railNodes[i].StableId == rootRailId)
                    {
                        AddRailRows(railNodes, childrenByParent, i, 1);
                        break;
                    }
                }
            }

            if (rows.Count == 2)
            {
                for (int i = 0; i < railNodes.Count; i++)
                    AddRailRows(railNodes, childrenByParent, i, 1);
            }

            rows.Add(new TreeRow("Selectors", string.Empty, 0, false, false, -1, TreeRowKind.SectionHeader));
            IReadOnlyList<MovingPlatformSelectorNodeAuthoring> selectors = authoring.Selectors ?? System.Array.Empty<MovingPlatformSelectorNodeAuthoring>();
            for (int selectorIndex = 0; selectorIndex < selectors.Count; selectorIndex++)
            {
                MovingPlatformSelectorNodeAuthoring selector = selectors[selectorIndex];
                string selectorPath = $"treeAuthoring.selectors.Array.data[{selectorIndex}]";
                rows.Add(new TreeRow(
                    selector != null ? selector.Label : $"Selector {selectorIndex + 1}",
                    selectorPath,
                    1,
                    true,
                    false,
                    -1,
                    TreeRowKind.SelectorNode,
                    selectorIndex));

                IReadOnlyList<MovingPlatformControlNodeAuthoring> steps = selector?.OrderedChildren ?? System.Array.Empty<MovingPlatformControlNodeAuthoring>();
                for (int stepIndex = 0; stepIndex < steps.Count; stepIndex++)
                {
                    MovingPlatformControlNodeAuthoring step = steps[stepIndex];
                    rows.Add(new TreeRow(
                        step != null ? step.Label : $"Step {stepIndex + 1}",
                        $"{selectorPath}.orderedChildren.Array.data[{stepIndex}]",
                        2,
                        true,
                        false,
                        -1,
                        TreeRowKind.SelectorStep,
                        selectorIndex,
                        stepIndex));
                }
            }
        }

        private void AddRailRows(
            IReadOnlyList<MovingPlatformRailNodeAuthoring> railNodes,
            Dictionary<string, List<int>> childrenByParent,
            int railNodeIndex,
            int depth)
        {
            if (railNodeIndex < 0 || railNodeIndex >= railNodes.Count)
                return;

            MovingPlatformRailNodeAuthoring railNode = railNodes[railNodeIndex];
            if (railNode == null)
                return;

            rows.Add(new TreeRow(
                railNode.Label,
                $"treeAuthoring.railNodes.Array.data[{railNodeIndex}]",
                depth,
                true,
                true,
                railNodeIndex,
                TreeRowKind.RailNode));

            if (!childrenByParent.TryGetValue(railNode.StableId, out List<int> children))
                return;

            for (int i = 0; i < children.Count; i++)
                AddRailRows(railNodes, childrenByParent, children[i], depth + 1);
        }

        private void RebuildTreeList()
        {
            if (treeScrollView == null)
                return;

            treeScrollView.Clear();
            for (int i = 0; i < rows.Count; i++)
            {
                TreeRow row = rows[i];
                if (!row.IsSelectable)
                {
                    Label label = new(row.Title);
                    label.style.unityFontStyleAndWeight = FontStyle.Bold;
                    label.style.marginTop = 6f;
                    treeScrollView.Add(label);
                    continue;
                }

                Button button = new(() => SelectRow(row))
                {
                    text = row.Title,
                };
                button.style.unityTextAlign = TextAnchor.MiddleLeft;
                button.style.marginLeft = row.Depth * 14f;
                button.style.marginBottom = 2f;
                if (row.PropertyPath == selectedPropertyPath)
                    button.style.backgroundColor = new StyleColor(new Color(0.22f, 0.42f, 0.65f, 0.95f));

                button.AddManipulator(new ContextualMenuManipulator(evt => PopulateRowMenu(evt, row)));

                treeScrollView.Add(button);
            }
        }

        private void PopulateRowMenu(ContextualMenuPopulateEvent evt, TreeRow row)
        {
            if (serializedObjectBridge.SerializedObject == null)
                return;

            switch (row.Kind)
            {
                case TreeRowKind.TreeRoot:
                    evt.menu.AppendAction("Create/Rail Root", _ => CreateRailRoot());
                    evt.menu.AppendAction("Create/Selector", _ => CreateSelectorNode());
                    break;
                case TreeRowKind.RailNode:
                    evt.menu.AppendAction("Create/Child Rail", _ => CreateChildRail(row.RailNodeIndex));
                    evt.menu.AppendAction("Create/Selector", _ => CreateSelectorNode(row.RailNodeIndex));
                    evt.menu.AppendSeparator();
                    evt.menu.AppendAction("Move/Make Root", _ => MakeRailRoot(row.RailNodeIndex));
                    evt.menu.AppendAction("Delete/Rail Node", _ => DeleteRailNode(row.RailNodeIndex));
                    break;
                case TreeRowKind.SelectorNode:
                    evt.menu.AppendAction("Create/Step/Move", _ => CreateSelectorStep(row.SelectorIndex, typeof(MovingPlatformMoveNodeAuthoring)));
                    evt.menu.AppendAction("Create/Step/Wait", _ => CreateSelectorStep(row.SelectorIndex, typeof(MovingPlatformWaitNodeAuthoring)));
                    evt.menu.AppendAction("Create/Step/Action", _ => CreateSelectorStep(row.SelectorIndex, typeof(MovingPlatformInlineActionNodeAuthoring)));
                    evt.menu.AppendAction("Create/Step/Rotate", _ => CreateSelectorStep(row.SelectorIndex, typeof(MovingPlatformRotationNodeAuthoring)));
                    evt.menu.AppendAction("Create/Step/Scale", _ => CreateSelectorStep(row.SelectorIndex, typeof(MovingPlatformScaleNodeAuthoring)));
                    evt.menu.AppendSeparator();
                    AppendRailAnchorMenu(evt, row.SelectorIndex);
                    evt.menu.AppendSeparator();
                    evt.menu.AppendAction("Delete/Selector", _ => DeleteSelectorNode(row.SelectorIndex));
                    break;
                case TreeRowKind.SelectorStep:
                    AppendStepMoveMenu(evt, row);
                    evt.menu.AppendSeparator();
                    evt.menu.AppendAction("Delete/Step", _ => DeleteSelectorStep(row.SelectorIndex, row.StepIndex));
                    break;
            }
        }

        private void AppendRailAnchorMenu(ContextualMenuPopulateEvent evt, int selectorIndex)
        {
            if (!TryResolveMovingPlatform(out MovingPlatformMB movingPlatform))
                return;

            IReadOnlyList<MovingPlatformRailNodeAuthoring> railNodes = movingPlatform.TreeAuthoring?.RailNodes ?? System.Array.Empty<MovingPlatformRailNodeAuthoring>();
            if (railNodes.Count == 0)
                return;

            for (int i = 0; i < railNodes.Count; i++)
            {
                int railIndex = i;
                string title = railNodes[i] != null ? railNodes[i].Label : $"Rail {i + 1}";
                evt.menu.AppendAction($"Move Anchor/{title}", _ => SetSelectorAnchor(selectorIndex, railIndex));
            }
        }

        private void AppendStepMoveMenu(ContextualMenuPopulateEvent evt, TreeRow row)
        {
            if (!TryResolveMovingPlatform(out MovingPlatformMB movingPlatform))
                return;

            IReadOnlyList<MovingPlatformSelectorNodeAuthoring> selectors = movingPlatform.TreeAuthoring?.Selectors ?? System.Array.Empty<MovingPlatformSelectorNodeAuthoring>();
            if (selectors.Count <= 1)
                return;

            for (int i = 0; i < selectors.Count; i++)
            {
                if (i == row.SelectorIndex)
                    continue;

                int targetSelectorIndex = i;
                string title = selectors[i] != null ? selectors[i].Label : $"Selector {i + 1}";
                evt.menu.AppendAction($"Move To Selector/{title}", _ => MoveSelectorStep(row.SelectorIndex, row.StepIndex, targetSelectorIndex));
            }
        }

        private void SelectRow(TreeRow row)
        {
            selectedPropertyPath = NormalizeSelectedPropertyPath(string.IsNullOrWhiteSpace(row.PropertyPath) ? RootPropertyPath : row.PropertyPath);
            sessionState.SetString(SessionSelectedPathKey, selectedPropertyPath);
            if (row.IsRailNode)
                MovingPlatformTreeEditorSelection.SelectedRailNodeIndex = row.RailNodeIndex;

            BindDetailPane();
            RebuildTreeList();
        }

        private void CreateRailRoot()
        {
            ExecuteTreeMutation("Create MovingPlatform Rail Root", movingPlatform =>
            {
                MovingPlatformTreeAuthoring tree = movingPlatform.TreeAuthoring;
                if (tree == null || tree.RailNodes.Count > 0)
                    return false;

                tree.AddRailNode("Root Rail", string.Empty);
                return true;
            });
        }

        private void CreateChildRail(int parentRailNodeIndex)
        {
            ExecuteTreeMutation("Create MovingPlatform Rail Node", movingPlatform =>
            {
                MovingPlatformTreeAuthoring tree = movingPlatform.TreeAuthoring;
                IReadOnlyList<MovingPlatformRailNodeAuthoring> railNodes = tree?.RailNodes ?? System.Array.Empty<MovingPlatformRailNodeAuthoring>();
                if (tree == null || parentRailNodeIndex < 0 || parentRailNodeIndex >= railNodes.Count || railNodes[parentRailNodeIndex] == null)
                    return false;

                tree.AddRailNode($"Rail {railNodes.Count + 1}", railNodes[parentRailNodeIndex].StableId);
                return true;
            });
        }

        private void MakeRailRoot(int railNodeIndex)
        {
            ExecuteTreeMutation("Promote MovingPlatform Rail Root", movingPlatform =>
            {
                MovingPlatformTreeAuthoring tree = movingPlatform.TreeAuthoring;
                IReadOnlyList<MovingPlatformRailNodeAuthoring> railNodes = tree?.RailNodes ?? System.Array.Empty<MovingPlatformRailNodeAuthoring>();
                if (tree == null || railNodeIndex < 0 || railNodeIndex >= railNodes.Count || railNodes[railNodeIndex] == null)
                    return false;

                return tree.ReparentRailNode(railNodes[railNodeIndex].StableId, string.Empty);
            });
        }

        private void DeleteRailNode(int railNodeIndex)
        {
            ExecuteTreeMutation("Delete MovingPlatform Rail Node", movingPlatform =>
            {
                MovingPlatformTreeAuthoring tree = movingPlatform.TreeAuthoring;
                IReadOnlyList<MovingPlatformRailNodeAuthoring> railNodes = tree?.RailNodes ?? System.Array.Empty<MovingPlatformRailNodeAuthoring>();
                if (tree == null || railNodeIndex < 0 || railNodeIndex >= railNodes.Count || railNodes[railNodeIndex] == null)
                    return false;

                return tree.RemoveRailNode(railNodes[railNodeIndex].StableId);
            });
        }

        private void CreateSelectorNode(int anchorRailNodeIndex = -1)
        {
            ExecuteTreeMutation("Create MovingPlatform Selector", movingPlatform =>
            {
                MovingPlatformTreeAuthoring tree = movingPlatform.TreeAuthoring;
                IReadOnlyList<MovingPlatformRailNodeAuthoring> railNodes = tree?.RailNodes ?? System.Array.Empty<MovingPlatformRailNodeAuthoring>();
                if (tree == null)
                    return false;

                string anchorRailNodeId = string.Empty;
                if (anchorRailNodeIndex >= 0 && anchorRailNodeIndex < railNodes.Count && railNodes[anchorRailNodeIndex] != null)
                    anchorRailNodeId = railNodes[anchorRailNodeIndex].StableId;
                else if (railNodes.Count > 0 && railNodes[0] != null)
                    anchorRailNodeId = railNodes[0].StableId;

                if (string.IsNullOrWhiteSpace(anchorRailNodeId) && railNodes.Count == 0)
                {
                    MovingPlatformRailNodeAuthoring rootRail = tree.AddRailNode("Root Rail", string.Empty);
                    anchorRailNodeId = rootRail != null ? rootRail.StableId : string.Empty;
                }

                if (string.IsNullOrWhiteSpace(anchorRailNodeId))
                    return false;

                tree.AddSelectorNode("Selector", anchorRailNodeId);
                return true;
            });
        }

        private void DeleteSelectorNode(int selectorIndex)
        {
            ExecuteTreeMutation("Delete MovingPlatform Selector", movingPlatform =>
            {
                MovingPlatformTreeAuthoring tree = movingPlatform.TreeAuthoring;
                IReadOnlyList<MovingPlatformSelectorNodeAuthoring> selectors = tree?.Selectors ?? System.Array.Empty<MovingPlatformSelectorNodeAuthoring>();
                if (tree == null || selectorIndex < 0 || selectorIndex >= selectors.Count || selectors[selectorIndex] == null)
                    return false;

                return tree.RemoveSelectorNode(selectors[selectorIndex].StableId);
            });
        }

        private void CreateSelectorStep(int selectorIndex, Type stepType)
        {
            ExecuteTreeMutation("Create MovingPlatform Step", movingPlatform =>
            {
                MovingPlatformTreeAuthoring tree = movingPlatform.TreeAuthoring;
                IReadOnlyList<MovingPlatformSelectorNodeAuthoring> selectors = tree?.Selectors ?? System.Array.Empty<MovingPlatformSelectorNodeAuthoring>();
                if (tree == null || selectorIndex < 0 || selectorIndex >= selectors.Count || selectors[selectorIndex] == null)
                    return false;

                tree.AddSelectorStep(selectors[selectorIndex].StableId, stepType);
                return true;
            });
        }

        private void DeleteSelectorStep(int selectorIndex, int stepIndex)
        {
            ExecuteTreeMutation("Delete MovingPlatform Step", movingPlatform =>
            {
                MovingPlatformTreeAuthoring tree = movingPlatform.TreeAuthoring;
                IReadOnlyList<MovingPlatformSelectorNodeAuthoring> selectors = tree?.Selectors ?? System.Array.Empty<MovingPlatformSelectorNodeAuthoring>();
                if (tree == null || selectorIndex < 0 || selectorIndex >= selectors.Count || selectors[selectorIndex] == null)
                    return false;

                IReadOnlyList<MovingPlatformControlNodeAuthoring> steps = selectors[selectorIndex].OrderedChildren;
                if (stepIndex < 0 || stepIndex >= steps.Count || steps[stepIndex] == null)
                    return false;

                return tree.RemoveSelectorStep(selectors[selectorIndex].StableId, steps[stepIndex].StableId);
            });
        }

        private void SetSelectorAnchor(int selectorIndex, int railNodeIndex)
        {
            ExecuteTreeMutation("Change MovingPlatform Selector Anchor", movingPlatform =>
            {
                MovingPlatformTreeAuthoring tree = movingPlatform.TreeAuthoring;
                IReadOnlyList<MovingPlatformSelectorNodeAuthoring> selectors = tree?.Selectors ?? System.Array.Empty<MovingPlatformSelectorNodeAuthoring>();
                IReadOnlyList<MovingPlatformRailNodeAuthoring> railNodes = tree?.RailNodes ?? System.Array.Empty<MovingPlatformRailNodeAuthoring>();
                if (tree == null || selectorIndex < 0 || selectorIndex >= selectors.Count || selectors[selectorIndex] == null)
                    return false;

                if (railNodeIndex < 0 || railNodeIndex >= railNodes.Count || railNodes[railNodeIndex] == null)
                    return false;

                return tree.SetSelectorAnchorRailNodeId(selectors[selectorIndex].StableId, railNodes[railNodeIndex].StableId);
            });
        }

        private void MoveSelectorStep(int sourceSelectorIndex, int stepIndex, int targetSelectorIndex)
        {
            ExecuteTreeMutation("Move MovingPlatform Step", movingPlatform =>
            {
                MovingPlatformTreeAuthoring tree = movingPlatform.TreeAuthoring;
                IReadOnlyList<MovingPlatformSelectorNodeAuthoring> selectors = tree?.Selectors ?? System.Array.Empty<MovingPlatformSelectorNodeAuthoring>();
                if (tree == null || sourceSelectorIndex < 0 || sourceSelectorIndex >= selectors.Count || targetSelectorIndex < 0 || targetSelectorIndex >= selectors.Count)
                    return false;

                MovingPlatformSelectorNodeAuthoring sourceSelector = selectors[sourceSelectorIndex];
                MovingPlatformSelectorNodeAuthoring targetSelector = selectors[targetSelectorIndex];
                if (sourceSelector == null || targetSelector == null)
                    return false;

                IReadOnlyList<MovingPlatformControlNodeAuthoring> steps = sourceSelector.OrderedChildren;
                if (stepIndex < 0 || stepIndex >= steps.Count || steps[stepIndex] == null)
                    return false;

                return tree.MoveSelectorStep(sourceSelector.StableId, steps[stepIndex].StableId, targetSelector.StableId, targetSelector.OrderedChildren.Count);
            });
        }

        private void ExecuteTreeMutation(string undoName, Func<MovingPlatformMB, bool> mutation)
        {
            if (!TryResolveMovingPlatform(out MovingPlatformMB movingPlatform))
                return;

            Undo.RecordObject(movingPlatform, undoName);
            if (!mutation(movingPlatform))
                return;

            PrefabUtility.RecordPrefabInstancePropertyModifications(movingPlatform);
            EditorUtility.SetDirty(movingPlatform);
            RefreshWindow();
        }

        private bool TryResolveMovingPlatform(out MovingPlatformMB movingPlatform)
        {
            movingPlatform = null;
            if (!serializedObjectBridge.TryResolveTarget(out UnityEngine.Object resolvedTarget) || resolvedTarget is not MovingPlatformMB typedTarget)
                return false;

            movingPlatform = typedTarget;
            return true;
        }

        private void BindDetailPane()
        {
            if (!serializedObjectBridge.TryResolveTarget(out UnityEngine.Object resolvedTarget))
            {
                detailBridge.Clear("Bind a MovingPlatform to edit its tree.");
                return;
            }

            detailBridge.Bind(new SerializedObject(resolvedTarget), selectedPropertyPath);
        }

        private static string NormalizeSelectedPropertyPath(string propertyPath)
        {
            if (string.IsNullOrWhiteSpace(propertyPath))
                return RootPropertyPath;

            if (propertyPath.EndsWith(".rule"))
                return propertyPath.Substring(0, propertyPath.Length - ".rule".Length);

            return propertyPath;
        }

        private void UpdateFooter(MovingPlatformMB movingPlatform)
        {
            if (footerLabel == null)
                return;

            IReadOnlyList<MovingPlatformTreeValidationIssue> issues = movingPlatform.ValidateTreeAuthoring();
            int errorCount = 0;
            int warningCount = 0;
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == MovingPlatformTreeValidationSeverity.Error)
                    errorCount++;
                else if (issues[i].Severity == MovingPlatformTreeValidationSeverity.Warning)
                    warningCount++;
            }

            footerLabel.text = $"Rails: {movingPlatform.EffectiveTreeRailNodeCount}  Selectors: {movingPlatform.EffectiveTreeSelectorCount}  Errors: {errorCount}  Warnings: {warningCount}";
        }

        private void TryMigrate()
        {
            if (!serializedObjectBridge.TryResolveTarget(out UnityEngine.Object resolvedTarget) || resolvedTarget is not MovingPlatformMB movingPlatform)
                return;

            Undo.RecordObject(movingPlatform, "Migrate MovingPlatform Tree");
            if (!movingPlatform.TryApplyLegacyMigration(out string failureReason))
            {
                EditorUtility.DisplayDialog(
                    "MovingPlatform Migration Failed",
                    string.IsNullOrWhiteSpace(failureReason) ? "Migration failed." : failureReason,
                    "Close");
                return;
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(movingPlatform);
            EditorUtility.SetDirty(movingPlatform);
            RefreshWindow();
        }
    }
}
