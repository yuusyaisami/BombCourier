using System;
using System.Collections.Generic;
using BC.Base;
using BC.Character;
using BC.Editor.Foundation;
using BC.Editor.Foundation.UIToolkit;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BC.Editor.Character
{
    public sealed class FaceExpressionUvSetWindow : SplitViewWindowBase
    {
        private const string WindowTitle = "Face Expression UV Set";
        private const float LeftPaneWidth = 280f;
        private const float AtlasPaneHeight = 420f;
        private const float ListRowHeight = 30f;
        private const float HandleVisualSize = 10f;
        private const float HandleHitSize = 16f;
        private const float MinRectSize = 1f;
        private const float AtlasPadding = 10f;
        private static readonly Color SelectedRowColorPro = new Color(0.76f, 0.66f, 0.28f, 0.55f);
        private static readonly Color SelectedRowColorLight = new Color(0.92f, 0.78f, 0.30f, 0.52f);

        private enum AtlasDragMode
        {
            None = 0,
            Create = 1,
            Move = 2,
            Resize = 3,
        }

        private enum ResizeHandle
        {
            None = 0,
            TopLeft = 1,
            TopRight = 2,
            BottomLeft = 3,
            BottomRight = 4,
        }

        private FaceExpressionUvSet boundAsset;
        private SerializedObject serializedObject;
        private SerializedProperty atlasTextureProperty;
        private SerializedProperty pixelRectUsesTopLeftOriginProperty;
        private SerializedProperty entriesProperty;

        private IMGUIContainer listContainer;
        private IMGUIContainer atlasContainer;
        private IMGUIContainer detailContainer;
        private Label footerLabel;
        private ObjectField assetField;

        private readonly List<FaceExpressionId> enumBuffer = new();
        private Vector2 listScroll;
        private int selectedIndex = -1;

        private AtlasDragMode dragMode;
        private ResizeHandle activeHandle;
        private Rect dragStartRectTopLeft;
        private Vector2 dragStartPixel;
        private Vector2 dragOffsetPixel;
        private Rect atlasDrawRect;
        private Rect? lastTemplatePixelRect;

        protected override float InitialLeftPaneWidth => LeftPaneWidth;

        [MenuItem("Window/BombCourier/Face Expression UV Set")]
        private static void OpenWindow()
        {
            OpenForAsset(Selection.activeObject as FaceExpressionUvSet);
        }

        internal static void OpenForAsset(FaceExpressionUvSet asset)
        {
            FaceExpressionUvSetWindow window = GetWindow<FaceExpressionUvSetWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(900f, 560f);
            window.Show();
            window.Focus();

            if (asset != null)
                window.Bind(asset);
            else
                window.TryBindFromSelection();

            window.RefreshAll();
        }

        public override void CreateGUI()
        {
            titleContent = new GUIContent(WindowTitle);
            base.CreateGUI();
            TryBindFromSelection();
            RefreshAll();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent(WindowTitle);
            TryBindFromSelection();
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is not FaceExpressionUvSet selected)
                return;

            if (boundAsset == selected)
                return;

            Bind(selected);
            RefreshAll();
        }

        protected override void BuildToolbar(VisualElement root)
        {
            root.style.justifyContent = Justify.SpaceBetween;

            VisualElement left = new VisualElement();
            left.style.flexDirection = FlexDirection.Row;
            left.style.alignItems = Align.Center;
            left.style.flexGrow = 1f;

            Label assetLabel = new Label("Target");
            assetLabel.style.minWidth = 46f;
            assetLabel.style.marginRight = 6f;
            left.Add(assetLabel);

            assetField = new ObjectField
            {
                objectType = typeof(FaceExpressionUvSet),
                allowSceneObjects = false,
            };

            assetField.style.flexGrow = 1f;
            assetField.RegisterValueChangedCallback(evt =>
            {
                Bind(evt.newValue as FaceExpressionUvSet);
                RefreshAll();
            });

            left.Add(assetField);
            root.Add(left);

            Button bindSelectionButton = new Button(() =>
            {
                TryBindFromSelection();
                RefreshAll();
            })
            {
                text = "Use Selection"
            };

            root.Add(bindSelectionButton);
        }

        protected override void BuildLeftPane(VisualElement root)
        {
            listContainer = new IMGUIContainer(DrawEntriesPane)
            {
                style =
                {
                    flexGrow = 1f,
                }
            };

            root.Add(listContainer);
        }

        protected override void BuildRightPane(VisualElement root)
        {
            TwoPaneSplitView vertical = new TwoPaneSplitView(0, AtlasPaneHeight, TwoPaneSplitViewOrientation.Vertical)
            {
                style =
                {
                    flexGrow = 1f,
                }
            };

            atlasContainer = new IMGUIContainer(DrawAtlasPane)
            {
                style =
                {
                    flexGrow = 1f,
                }
            };

            detailContainer = new IMGUIContainer(DrawDetailPane)
            {
                style =
                {
                    flexGrow = 1f,
                }
            };

            vertical.Add(atlasContainer);
            vertical.Add(detailContainer);
            root.Add(vertical);
        }

        protected override void BuildFooter(VisualElement root)
        {
            footerLabel = new Label();
            footerLabel.style.flexGrow = 1f;
            root.Add(footerLabel);
        }

        private void Bind(FaceExpressionUvSet asset)
        {
            boundAsset = asset;
            serializedObject = asset != null ? new SerializedObject(asset) : null;
            CacheRootProperties();
            selectedIndex = ResolveSafeSelectionIndex(selectedIndex);

            if (assetField != null)
                assetField.value = boundAsset;

            CancelAtlasInteraction();
        }

        private void TryBindFromSelection()
        {
            if (Selection.activeObject is FaceExpressionUvSet selected)
            {
                Bind(selected);
            }
            else if (boundAsset == null)
            {
                Bind(null);
            }
        }

        private void CacheRootProperties()
        {
            atlasTextureProperty = null;
            pixelRectUsesTopLeftOriginProperty = null;
            entriesProperty = null;

            if (serializedObject == null)
                return;

            atlasTextureProperty = serializedObject.FindProperty("atlasTexture");
            pixelRectUsesTopLeftOriginProperty = serializedObject.FindProperty("pixelRectUsesTopLeftOrigin");
            entriesProperty = serializedObject.FindProperty("entries");
        }

        private void RefreshAll()
        {
            selectedIndex = ResolveSafeSelectionIndex(selectedIndex);
            UpdateFooter();
            listContainer?.MarkDirtyRepaint();
            atlasContainer?.MarkDirtyRepaint();
            detailContainer?.MarkDirtyRepaint();
            RequestRepaint();
        }

        private void UpdateFooter()
        {
            if (footerLabel == null)
                return;

            if (!IsBound)
            {
                footerLabel.text = "FaceExpressionUvSet を選択してください。";
                return;
            }

            int count = EntryCount;
            string selectedLabel = selectedIndex >= 0 ? $"Selected: {selectedIndex}" : "Selected: none";
            footerLabel.text = $"Entries: {count} | {selectedLabel} | Shortcuts: Ctrl+N(Create), Ctrl+D(Duplicate), Del(Delete), Up/Down(Select)";
        }

        private void DrawEntriesPane()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField("Expressions", EditorStyles.boldLabel);

                if (!IsBound)
                {
                    EditorGUILayout.HelpBox("Project で FaceExpressionUvSet を選択してから編集してください。", MessageType.Info);
                    return;
                }

                serializedObject.Update();
                selectedIndex = ResolveSafeSelectionIndex(selectedIndex);
                HandleEntryListShortcuts(Event.current);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("+", GUILayout.Width(26f)))
                    {
                        CreateEntryFromTemplate();
                    }
                }

                Rect scrollRect = GUILayoutUtility.GetRect(10f, 20000f, 10f, 20000f, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                Rect viewRect = new Rect(0f, 0f, scrollRect.width - 16f, Mathf.Max(scrollRect.height, EntryCount * (ListRowHeight + 2f) + 8f));

                listScroll = GUI.BeginScrollView(scrollRect, listScroll, viewRect);
                DrawEntryRows(viewRect.width);
                GUI.EndScrollView();

                Event evt = Event.current;
                if (evt.type == EventType.ContextClick && scrollRect.Contains(evt.mousePosition))
                {
                    ShowEntryContextMenu();
                    evt.Use();
                }
            }
        }

        private void DrawEntryRows(float width)
        {
            Event evt = Event.current;
            bool anyHovered = false;

            for (int i = 0; i < EntryCount; i++)
            {
                SerializedProperty entry = entriesProperty.GetArrayElementAtIndex(i);
                Rect rowRect = new Rect(2f, 4f + i * (ListRowHeight + 2f), Mathf.Max(20f, width - 4f), ListRowHeight);
                bool selected = i == selectedIndex;

                Color rowColor = selected
                    ? ResolveSelectedRowColor()
                    : EditorGUIUtility.isProSkin
                        ? (i % 2 == 0 ? new Color(0.20f, 0.20f, 0.20f, 0.85f) : new Color(0.23f, 0.23f, 0.23f, 0.85f))
                        : (i % 2 == 0 ? new Color(0.90f, 0.90f, 0.90f, 1f) : new Color(0.86f, 0.86f, 0.86f, 1f));

                EditorGUI.DrawRect(rowRect, rowColor);
                DrawRectOutline(rowRect, new Color(0f, 0f, 0f, 0.18f), 1f);

                Rect inner = new Rect(rowRect.x + 6f, rowRect.y + 4f, rowRect.width - 12f, rowRect.height - 8f);
                string expressionLabel = ((FaceExpressionId)entry.FindPropertyRelative("expression").intValue).ToString();
                Rect pixelRect = entry.FindPropertyRelative("pixelRect").rectValue;
                bool blinkEnabled = entry.FindPropertyRelative("blink").FindPropertyRelative("enabled").boolValue;
                string blinkLabel = blinkEnabled ? " (Blink)" : string.Empty;
                string summary = $"#{i:00} {expressionLabel}{blinkLabel}  [{pixelRect.x:0},{pixelRect.y:0},{pixelRect.width:0},{pixelRect.height:0}]";
                GUI.Label(inner, summary, EditorStyles.label);

                if (!rowRect.Contains(evt.mousePosition))
                    continue;

                anyHovered = true;

                if (evt.type == EventType.MouseDown && evt.button == 0)
                {
                    selectedIndex = i;
                    UpdateFooter();
                    RefreshAll();
                    evt.Use();
                    return;
                }

                if (evt.type == EventType.ContextClick)
                {
                    selectedIndex = i;
                    ShowEntryContextMenu();
                    evt.Use();
                    return;
                }
            }

            if (anyHovered)
                return;

            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                selectedIndex = -1;
                UpdateFooter();
            }
        }

        private void HandleEntryListShortcuts(Event evt)
        {
            if (!IsBound || evt == null || evt.type != EventType.KeyDown)
                return;

            if (EditorGUIUtility.editingTextField)
                return;

            if (evt.control && evt.keyCode == KeyCode.N)
            {
                CreateEntryFromTemplate();
                evt.Use();
                return;
            }

            if (evt.control && evt.keyCode == KeyCode.D)
            {
                DuplicateSelectedEntry();
                evt.Use();
                return;
            }

            if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
            {
                DeleteSelectedEntry();
                evt.Use();
                return;
            }

            if (evt.keyCode == KeyCode.UpArrow)
            {
                if (EntryCount <= 0)
                    return;

                selectedIndex = Mathf.Clamp(selectedIndex <= 0 ? 0 : selectedIndex - 1, 0, EntryCount - 1);
                RefreshAll();
                evt.Use();
                return;
            }

            if (evt.keyCode == KeyCode.DownArrow)
            {
                if (EntryCount <= 0)
                    return;

                selectedIndex = Mathf.Clamp(selectedIndex < 0 ? 0 : selectedIndex + 1, 0, EntryCount - 1);
                RefreshAll();
                evt.Use();
            }
        }

        private void ShowEntryContextMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("新規作成"), false, CreateEntryFromTemplate);

            bool canEditSelected = selectedIndex >= 0 && selectedIndex < EntryCount;
            if (canEditSelected)
            {
                menu.AddItem(new GUIContent("複製"), false, DuplicateSelectedEntry);
                menu.AddItem(new GUIContent("削除"), false, DeleteSelectedEntry);
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("複製"));
                menu.AddDisabledItem(new GUIContent("削除"));
            }

            menu.ShowAsContext();
        }

        private void CreateEntryFromTemplate()
        {
            if (!IsBound)
                return;

            if (!TryResolveUnusedExpression(-1, out FaceExpressionId expression))
            {
                EditorUtility.DisplayDialog(WindowTitle, "利用可能な FaceExpressionId が残っていません。", "OK");
                return;
            }

            Rect templateRect = ResolveTemplateRect();

            Undo.RecordObject(boundAsset, "Create Face Expression Entry");
            serializedObject.Update();
            int newIndex = entriesProperty.arraySize;
            entriesProperty.InsertArrayElementAtIndex(newIndex);

            SerializedProperty newEntry = entriesProperty.GetArrayElementAtIndex(newIndex);
            WriteEntry(newEntry, expression, templateRect, blinkEnabled: false, FaceExpressionId.Neutral);
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(boundAsset);

            selectedIndex = newIndex;
            lastTemplatePixelRect = templateRect;
            RefreshAll();
        }

        private void DuplicateSelectedEntry()
        {
            if (!IsBound || selectedIndex < 0 || selectedIndex >= EntryCount)
                return;

            if (!TryResolveUnusedExpression(selectedIndex, out FaceExpressionId expression))
            {
                EditorUtility.DisplayDialog(WindowTitle, "重複不可のため複製できません。利用可能な FaceExpressionId がありません。", "OK");
                return;
            }

            serializedObject.Update();
            SerializedProperty source = entriesProperty.GetArrayElementAtIndex(selectedIndex);
            Rect sourcePixelRect = source.FindPropertyRelative("pixelRect").rectValue;
            bool blinkEnabled = source.FindPropertyRelative("blink").FindPropertyRelative("enabled").boolValue;
            FaceExpressionId blinkExpression = (FaceExpressionId)source.FindPropertyRelative("blink").FindPropertyRelative("blinkExpression").intValue;

            Undo.RecordObject(boundAsset, "Duplicate Face Expression Entry");
            int newIndex = selectedIndex + 1;
            entriesProperty.InsertArrayElementAtIndex(newIndex);
            SerializedProperty newEntry = entriesProperty.GetArrayElementAtIndex(newIndex);
            WriteEntry(newEntry, expression, sourcePixelRect, blinkEnabled, blinkExpression);
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(boundAsset);

            selectedIndex = newIndex;
            lastTemplatePixelRect = sourcePixelRect;
            RefreshAll();
        }

        private void DeleteSelectedEntry()
        {
            if (!IsBound || selectedIndex < 0 || selectedIndex >= EntryCount)
                return;

            Undo.RecordObject(boundAsset, "Delete Face Expression Entry");
            serializedObject.Update();
            entriesProperty.DeleteArrayElementAtIndex(selectedIndex);
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(boundAsset);

            selectedIndex = Mathf.Clamp(selectedIndex, 0, EntryCount - 1);
            if (EntryCount <= 0)
                selectedIndex = -1;

            RefreshAll();
        }

        private void DrawAtlasPane()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField("Atlas Work Panel", EditorStyles.boldLabel);

                if (!IsBound)
                {
                    EditorGUILayout.HelpBox("FaceExpressionUvSet が未選択です。", MessageType.Info);
                    return;
                }

                serializedObject.Update();

                Texture2D atlasTexture = atlasTextureProperty.objectReferenceValue as Texture2D;
                Rect hostRect = GUILayoutUtility.GetRect(10f, 20000f, 10f, 20000f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                if (atlasTexture == null)
                {
                    EditorGUI.DrawRect(hostRect, EditorThemeTokens.BlockBackground);
                    EditorGUI.HelpBox(hostRect, "atlasTexture を設定すると編集できます。", MessageType.Info);
                    return;
                }

                atlasDrawRect = FitRect(hostRect, atlasTexture.width, atlasTexture.height, AtlasPadding);
                EditorGUI.DrawRect(hostRect, EditorThemeTokens.BlockBackground);
                // Alpha を有効にして描画し、透過を含む atlas が正しい見え方になるようにする。
                GUI.DrawTexture(atlasDrawRect, atlasTexture, ScaleMode.ScaleToFit, true);
                DrawAtlasBorder(atlasDrawRect);

                DrawEntryOverlays(atlasTexture.width, atlasTexture.height);
                HandleAtlasInteraction(atlasTexture.width, atlasTexture.height);
            }
        }

        private void DrawDetailPane()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField("Entry Detail", EditorStyles.boldLabel);

                if (!IsBound)
                {
                    EditorGUILayout.HelpBox("FaceExpressionUvSet が未選択です。", MessageType.Info);
                    return;
                }

                serializedObject.Update();

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(atlasTextureProperty);
                EditorGUILayout.PropertyField(pixelRectUsesTopLeftOriginProperty);

                selectedIndex = ResolveSafeSelectionIndex(selectedIndex);
                if (selectedIndex < 0)
                {
                    EditorGUILayout.Space(4f);
                    EditorGUILayout.HelpBox("左の一覧から Entry を選択してください。", MessageType.Info);

                    if (EditorGUI.EndChangeCheck())
                        ApplyPropertyChanges();

                    return;
                }

                SerializedProperty entry = entriesProperty.GetArrayElementAtIndex(selectedIndex);
                SerializedProperty expressionProperty = entry.FindPropertyRelative("expression");
                SerializedProperty pixelRectProperty = entry.FindPropertyRelative("pixelRect");
                SerializedProperty blinkProperty = entry.FindPropertyRelative("blink");
                SerializedProperty blinkEnabledProperty = blinkProperty.FindPropertyRelative("enabled");
                SerializedProperty blinkExpressionProperty = blinkProperty.FindPropertyRelative("blinkExpression");

                EditorGUILayout.Space(4f);
                DrawExpressionPopup(expressionProperty);
                EditorGUILayout.PropertyField(pixelRectProperty);
                EditorGUILayout.Space(2f);
                EditorGUILayout.PropertyField(blinkEnabledProperty);
                if (blinkEnabledProperty.boolValue)
                    EditorGUILayout.PropertyField(blinkExpressionProperty);

                if (EditorGUI.EndChangeCheck())
                {
                    Rect rect = pixelRectProperty.rectValue;
                    pixelRectProperty.rectValue = SanitizeStoredRect(rect, ResolveAtlasTextureWidth(), ResolveAtlasTextureHeight());
                    lastTemplatePixelRect = pixelRectProperty.rectValue;
                    ApplyPropertyChanges();
                }
            }
        }

        private void DrawExpressionPopup(SerializedProperty expressionProperty)
        {
            enumBuffer.Clear();
            FaceExpressionId current = (FaceExpressionId)expressionProperty.intValue;

            FaceExpressionId[] values = (FaceExpressionId[])Enum.GetValues(typeof(FaceExpressionId));
            for (int i = 0; i < values.Length; i++)
            {
                FaceExpressionId candidate = values[i];
                if (candidate == current || !IsExpressionUsed(candidate, selectedIndex))
                    enumBuffer.Add(candidate);
            }

            if (enumBuffer.Count <= 0)
            {
                EditorGUILayout.HelpBox("利用可能な expression 値がありません。", MessageType.Warning);
                return;
            }

            int popupIndex = Mathf.Max(0, enumBuffer.IndexOf(current));
            string[] labels = new string[enumBuffer.Count];
            for (int i = 0; i < enumBuffer.Count; i++)
                labels[i] = enumBuffer[i].ToString();

            int nextIndex = EditorGUILayout.Popup("Expression", popupIndex, labels);
            FaceExpressionId next = enumBuffer[Mathf.Clamp(nextIndex, 0, enumBuffer.Count - 1)];
            expressionProperty.intValue = (int)next;
        }

        private void HandleAtlasInteraction(int atlasWidth, int atlasHeight)
        {
            if (EntryCount <= 0)
                return;

            Event evt = Event.current;
            if (evt == null)
                return;

            if (evt.type == EventType.MouseDown && evt.button == 0 && atlasDrawRect.Contains(evt.mousePosition))
            {
                if (TryPickEntryAtMouse(evt.mousePosition, atlasWidth, atlasHeight, out int pickedIndex) && pickedIndex != selectedIndex)
                {
                    selectedIndex = pickedIndex;
                    UpdateFooter();
                    listContainer?.MarkDirtyRepaint();
                    detailContainer?.MarkDirtyRepaint();
                }

                if (!TryGetSelectedRectTopLeft(atlasWidth, atlasHeight, out Rect selectedRectTopLeft))
                    return;

                Rect selectedGuiRect = AtlasToGuiRect(selectedRectTopLeft, atlasDrawRect, atlasWidth, atlasHeight);
                ResizeHandle hitHandle = ResolveHitHandle(evt.mousePosition, selectedGuiRect);
                Vector2 mousePixel = GuiToAtlasPixel(evt.mousePosition, atlasDrawRect, atlasWidth, atlasHeight, clampToBounds: true);

                Undo.RecordObject(boundAsset, "Edit Face Expression PixelRect");

                if (hitHandle != ResizeHandle.None)
                {
                    dragMode = AtlasDragMode.Resize;
                    activeHandle = hitHandle;
                    dragStartRectTopLeft = selectedRectTopLeft;
                }
                else if (selectedGuiRect.Contains(evt.mousePosition))
                {
                    dragMode = AtlasDragMode.Move;
                    activeHandle = ResizeHandle.None;
                    dragStartRectTopLeft = selectedRectTopLeft;
                    dragOffsetPixel = mousePixel - selectedRectTopLeft.position;
                }
                else
                {
                    dragMode = AtlasDragMode.Create;
                    activeHandle = ResizeHandle.None;
                    dragStartPixel = mousePixel;
                    dragStartRectTopLeft = Rect.MinMaxRect(mousePixel.x, mousePixel.y, mousePixel.x + MinRectSize, mousePixel.y + MinRectSize);
                }

                evt.Use();
                return;
            }

            if (evt.type == EventType.MouseDrag && dragMode != AtlasDragMode.None)
            {
                Vector2 mousePixel = GuiToAtlasPixel(evt.mousePosition, atlasDrawRect, atlasWidth, atlasHeight, clampToBounds: true);
                Rect nextTopLeft = dragStartRectTopLeft;

                switch (dragMode)
                {
                    case AtlasDragMode.Create:
                        nextTopLeft = RectFromPoints(dragStartPixel, mousePixel);
                        break;

                    case AtlasDragMode.Move:
                        nextTopLeft.position = mousePixel - dragOffsetPixel;
                        nextTopLeft.x = Mathf.Clamp(nextTopLeft.x, 0f, Mathf.Max(0f, atlasWidth - nextTopLeft.width));
                        nextTopLeft.y = Mathf.Clamp(nextTopLeft.y, 0f, Mathf.Max(0f, atlasHeight - nextTopLeft.height));
                        break;

                    case AtlasDragMode.Resize:
                        nextTopLeft = ResizeRect(dragStartRectTopLeft, mousePixel, activeHandle, atlasWidth, atlasHeight);
                        break;
                }

                nextTopLeft = ClampTopLeftRect(nextTopLeft, atlasWidth, atlasHeight);
                ApplySelectedRectFromTopLeft(nextTopLeft);
                evt.Use();
                return;
            }

            if (evt.type == EventType.MouseUp && evt.button == 0 && dragMode != AtlasDragMode.None)
            {
                CancelAtlasInteraction();
                evt.Use();
            }
        }

        private void DrawEntryOverlays(int atlasWidth, int atlasHeight)
        {
            for (int i = 0; i < EntryCount; i++)
            {
                SerializedProperty entry = entriesProperty.GetArrayElementAtIndex(i);
                Rect storedRect = entry.FindPropertyRelative("pixelRect").rectValue;
                Rect topLeftRect = StoredRectToTopLeftRect(storedRect, atlasHeight);
                Rect guiRect = AtlasToGuiRect(topLeftRect, atlasDrawRect, atlasWidth, atlasHeight);

                bool selected = i == selectedIndex;
                Color line = selected
                    ? EditorThemeTokens.SelectedColor
                    : new Color(EditorThemeTokens.SelectedColor.r, EditorThemeTokens.SelectedColor.g, EditorThemeTokens.SelectedColor.b, 0.45f);
                Color fill = selected
                    ? new Color(line.r, line.g, line.b, 0.20f)
                    : new Color(line.r, line.g, line.b, 0.08f);

                EditorGUI.DrawRect(guiRect, fill);
                DrawRectOutline(guiRect, line, 2f);

                if (!selected)
                    continue;

                EditorGUIUtility.AddCursorRect(guiRect, MouseCursor.MoveArrow);

                DrawCornerHandle(guiRect, ResizeHandle.TopLeft);
                DrawCornerHandle(guiRect, ResizeHandle.TopRight);
                DrawCornerHandle(guiRect, ResizeHandle.BottomLeft);
                DrawCornerHandle(guiRect, ResizeHandle.BottomRight);
            }
        }

        private bool TryPickEntryAtMouse(Vector2 mousePosition, int atlasWidth, int atlasHeight, out int pickedIndex)
        {
            pickedIndex = -1;

            if (EntryCount <= 0)
                return false;

            // 重なり時は現在選択中を優先。
            if (selectedIndex >= 0 && selectedIndex < EntryCount)
            {
                SerializedProperty selectedEntry = entriesProperty.GetArrayElementAtIndex(selectedIndex);
                Rect selectedStoredRect = selectedEntry.FindPropertyRelative("pixelRect").rectValue;
                Rect selectedTopLeftRect = StoredRectToTopLeftRect(selectedStoredRect, atlasHeight);
                Rect selectedGuiRect = AtlasToGuiRect(selectedTopLeftRect, atlasDrawRect, atlasWidth, atlasHeight);

                if (selectedGuiRect.Contains(mousePosition))
                {
                    pickedIndex = selectedIndex;
                    return true;
                }
            }

            for (int i = EntryCount - 1; i >= 0; i--)
            {
                if (i == selectedIndex)
                    continue;

                SerializedProperty entry = entriesProperty.GetArrayElementAtIndex(i);
                Rect storedRect = entry.FindPropertyRelative("pixelRect").rectValue;
                Rect topLeftRect = StoredRectToTopLeftRect(storedRect, atlasHeight);
                Rect guiRect = AtlasToGuiRect(topLeftRect, atlasDrawRect, atlasWidth, atlasHeight);

                if (!guiRect.Contains(mousePosition))
                    continue;

                pickedIndex = i;
                return true;
            }

            return false;
        }

        private void DrawCornerHandle(Rect selectedGuiRect, ResizeHandle handle)
        {
            Rect hitRect = GetHandleHitRect(selectedGuiRect, handle);
            Rect handleRect = GetHandleRect(selectedGuiRect, handle);
            EditorGUI.DrawRect(handleRect, EditorThemeTokens.SelectedColor);
            DrawRectOutline(handleRect, Color.black, 1f);

            MouseCursor cursor = handle switch
            {
                ResizeHandle.TopLeft => MouseCursor.ResizeUpLeft,
                ResizeHandle.BottomRight => MouseCursor.ResizeUpLeft,
                ResizeHandle.TopRight => MouseCursor.ResizeUpRight,
                ResizeHandle.BottomLeft => MouseCursor.ResizeUpRight,
                _ => MouseCursor.Arrow,
            };

            EditorGUIUtility.AddCursorRect(hitRect, cursor);
        }

        private ResizeHandle ResolveHitHandle(Vector2 mousePosition, Rect selectedGuiRect)
        {
            if (GetHandleHitRect(selectedGuiRect, ResizeHandle.TopLeft).Contains(mousePosition))
                return ResizeHandle.TopLeft;
            if (GetHandleHitRect(selectedGuiRect, ResizeHandle.TopRight).Contains(mousePosition))
                return ResizeHandle.TopRight;
            if (GetHandleHitRect(selectedGuiRect, ResizeHandle.BottomLeft).Contains(mousePosition))
                return ResizeHandle.BottomLeft;
            if (GetHandleHitRect(selectedGuiRect, ResizeHandle.BottomRight).Contains(mousePosition))
                return ResizeHandle.BottomRight;
            return ResizeHandle.None;
        }

        private Rect GetHandleRect(Rect selectedGuiRect, ResizeHandle handle)
        {
            return handle switch
            {
                ResizeHandle.TopLeft => new Rect(selectedGuiRect.xMin - HandleVisualSize * 0.5f, selectedGuiRect.yMin - HandleVisualSize * 0.5f, HandleVisualSize, HandleVisualSize),
                ResizeHandle.TopRight => new Rect(selectedGuiRect.xMax - HandleVisualSize * 0.5f, selectedGuiRect.yMin - HandleVisualSize * 0.5f, HandleVisualSize, HandleVisualSize),
                ResizeHandle.BottomLeft => new Rect(selectedGuiRect.xMin - HandleVisualSize * 0.5f, selectedGuiRect.yMax - HandleVisualSize * 0.5f, HandleVisualSize, HandleVisualSize),
                ResizeHandle.BottomRight => new Rect(selectedGuiRect.xMax - HandleVisualSize * 0.5f, selectedGuiRect.yMax - HandleVisualSize * 0.5f, HandleVisualSize, HandleVisualSize),
                _ => Rect.zero,
            };
        }

        private Rect GetHandleHitRect(Rect selectedGuiRect, ResizeHandle handle)
        {
            Rect visualRect = GetHandleRect(selectedGuiRect, handle);
            float expand = Mathf.Max(0f, (HandleHitSize - HandleVisualSize) * 0.5f);
            visualRect.xMin -= expand;
            visualRect.xMax += expand;
            visualRect.yMin -= expand;
            visualRect.yMax += expand;
            return visualRect;
        }

        private void ApplySelectedRectFromTopLeft(Rect topLeftRect)
        {
            if (!IsBound || selectedIndex < 0 || selectedIndex >= EntryCount)
                return;

            serializedObject.Update();
            SerializedProperty entry = entriesProperty.GetArrayElementAtIndex(selectedIndex);
            SerializedProperty pixelRectProperty = entry.FindPropertyRelative("pixelRect");
            Rect storedRect = TopLeftRectToStoredRect(topLeftRect, ResolveAtlasTextureHeight());
            storedRect = SanitizeStoredRect(storedRect, ResolveAtlasTextureWidth(), ResolveAtlasTextureHeight());
            pixelRectProperty.rectValue = storedRect;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(boundAsset);

            lastTemplatePixelRect = storedRect;
            detailContainer?.MarkDirtyRepaint();
            listContainer?.MarkDirtyRepaint();
        }

        private bool TryGetSelectedRectTopLeft(int atlasWidth, int atlasHeight, out Rect topLeftRect)
        {
            topLeftRect = default;
            if (selectedIndex < 0 || selectedIndex >= EntryCount)
                return false;

            serializedObject.Update();
            SerializedProperty entry = entriesProperty.GetArrayElementAtIndex(selectedIndex);
            Rect storedRect = entry.FindPropertyRelative("pixelRect").rectValue;
            topLeftRect = ClampTopLeftRect(StoredRectToTopLeftRect(storedRect, atlasHeight), atlasWidth, atlasHeight);
            return true;
        }

        private Rect ResolveTemplateRect()
        {
            if (selectedIndex >= 0 && selectedIndex < EntryCount)
            {
                serializedObject.Update();
                Rect selectedRect = entriesProperty.GetArrayElementAtIndex(selectedIndex).FindPropertyRelative("pixelRect").rectValue;
                return SanitizeStoredRect(selectedRect, ResolveAtlasTextureWidth(), ResolveAtlasTextureHeight());
            }

            if (lastTemplatePixelRect.HasValue)
                return SanitizeStoredRect(lastTemplatePixelRect.Value, ResolveAtlasTextureWidth(), ResolveAtlasTextureHeight());

            int width = ResolveAtlasTextureWidth();
            int height = ResolveAtlasTextureHeight();
            if (width <= 0 || height <= 0)
                return new Rect(0f, 0f, 32f, 32f);

            float rectWidth = Mathf.Min(64f, width);
            float rectHeight = Mathf.Min(64f, height);
            return new Rect(0f, 0f, rectWidth, rectHeight);
        }

        private int ResolveSafeSelectionIndex(int candidate)
        {
            if (!IsBound || EntryCount <= 0)
                return -1;

            return Mathf.Clamp(candidate, 0, EntryCount - 1);
        }

        private void CancelAtlasInteraction()
        {
            dragMode = AtlasDragMode.None;
            activeHandle = ResizeHandle.None;
        }

        private bool TryResolveUnusedExpression(int exceptIndex, out FaceExpressionId result)
        {
            FaceExpressionId[] all = (FaceExpressionId[])Enum.GetValues(typeof(FaceExpressionId));
            for (int i = 0; i < all.Length; i++)
            {
                FaceExpressionId candidate = all[i];
                if (IsExpressionUsed(candidate, exceptIndex))
                    continue;

                result = candidate;
                return true;
            }

            result = default;
            return false;
        }

        private bool IsExpressionUsed(FaceExpressionId candidate, int exceptIndex)
        {
            if (!IsBound)
                return false;

            serializedObject.Update();
            for (int i = 0; i < EntryCount; i++)
            {
                if (i == exceptIndex)
                    continue;

                int value = entriesProperty.GetArrayElementAtIndex(i).FindPropertyRelative("expression").intValue;
                if (value == (int)candidate)
                    return true;
            }

            return false;
        }

        private void WriteEntry(
            SerializedProperty entry,
            FaceExpressionId expression,
            Rect pixelRect,
            bool blinkEnabled,
            FaceExpressionId blinkExpression)
        {
            entry.FindPropertyRelative("expression").intValue = (int)expression;
            entry.FindPropertyRelative("pixelRect").rectValue = pixelRect;
            SerializedProperty blink = entry.FindPropertyRelative("blink");
            blink.FindPropertyRelative("enabled").boolValue = blinkEnabled;
            blink.FindPropertyRelative("blinkExpression").intValue = (int)blinkExpression;
        }

        private void ApplyPropertyChanges()
        {
            Undo.RecordObject(boundAsset, "Edit Face Expression UV Set");
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(boundAsset);
            listContainer?.MarkDirtyRepaint();
            atlasContainer?.MarkDirtyRepaint();
            UpdateFooter();
        }

        private static Rect FitRect(Rect hostRect, int textureWidth, int textureHeight, float padding)
        {
            Rect inner = new Rect(
                hostRect.x + padding,
                hostRect.y + padding,
                Mathf.Max(1f, hostRect.width - padding * 2f),
                Mathf.Max(1f, hostRect.height - padding * 2f));

            if (textureWidth <= 0 || textureHeight <= 0)
                return inner;

            float textureAspect = (float)textureWidth / textureHeight;
            float hostAspect = inner.width / inner.height;

            if (hostAspect > textureAspect)
            {
                float width = inner.height * textureAspect;
                float x = inner.x + (inner.width - width) * 0.5f;
                return new Rect(x, inner.y, width, inner.height);
            }
            else
            {
                float height = inner.width / textureAspect;
                float y = inner.y + (inner.height - height) * 0.5f;
                return new Rect(inner.x, y, inner.width, height);
            }
        }

        private static void DrawRectOutline(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
        }

        private static void DrawAtlasBorder(Rect rect)
        {
            DrawRectOutline(rect, new Color(0f, 0f, 0f, 0.55f), 1f);
        }

        private static Rect AtlasToGuiRect(Rect topLeftRect, Rect drawRect, int atlasWidth, int atlasHeight)
        {
            float x = drawRect.x + topLeftRect.x / atlasWidth * drawRect.width;
            float y = drawRect.y + topLeftRect.y / atlasHeight * drawRect.height;
            float width = topLeftRect.width / atlasWidth * drawRect.width;
            float height = topLeftRect.height / atlasHeight * drawRect.height;
            return new Rect(x, y, width, height);
        }

        private static Vector2 GuiToAtlasPixel(Vector2 guiPoint, Rect drawRect, int atlasWidth, int atlasHeight, bool clampToBounds)
        {
            float u = (guiPoint.x - drawRect.x) / Mathf.Max(1f, drawRect.width);
            float v = (guiPoint.y - drawRect.y) / Mathf.Max(1f, drawRect.height);

            if (clampToBounds)
            {
                u = Mathf.Clamp01(u);
                v = Mathf.Clamp01(v);
            }

            return new Vector2(u * atlasWidth, v * atlasHeight);
        }

        private Rect StoredRectToTopLeftRect(Rect storedRect, int atlasHeight)
        {
            if (pixelRectUsesTopLeftOriginProperty == null || pixelRectUsesTopLeftOriginProperty.boolValue)
                return storedRect;

            return new Rect(
                storedRect.x,
                atlasHeight - storedRect.y - storedRect.height,
                storedRect.width,
                storedRect.height);
        }

        private Rect TopLeftRectToStoredRect(Rect topLeftRect, int atlasHeight)
        {
            if (pixelRectUsesTopLeftOriginProperty == null || pixelRectUsesTopLeftOriginProperty.boolValue)
                return topLeftRect;

            return new Rect(
                topLeftRect.x,
                atlasHeight - topLeftRect.y - topLeftRect.height,
                topLeftRect.width,
                topLeftRect.height);
        }

        private static Rect RectFromPoints(Vector2 a, Vector2 b)
        {
            float xMin = Mathf.Min(a.x, b.x);
            float yMin = Mathf.Min(a.y, b.y);
            float xMax = Mathf.Max(a.x, b.x);
            float yMax = Mathf.Max(a.y, b.y);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static Rect ResizeRect(Rect source, Vector2 mousePixel, ResizeHandle handle, int atlasWidth, int atlasHeight)
        {
            float xMin = source.xMin;
            float yMin = source.yMin;
            float xMax = source.xMax;
            float yMax = source.yMax;

            switch (handle)
            {
                case ResizeHandle.TopLeft:
                    xMin = Mathf.Clamp(mousePixel.x, 0f, xMax - MinRectSize);
                    yMin = Mathf.Clamp(mousePixel.y, 0f, yMax - MinRectSize);
                    break;

                case ResizeHandle.TopRight:
                    xMax = Mathf.Clamp(mousePixel.x, xMin + MinRectSize, atlasWidth);
                    yMin = Mathf.Clamp(mousePixel.y, 0f, yMax - MinRectSize);
                    break;

                case ResizeHandle.BottomLeft:
                    xMin = Mathf.Clamp(mousePixel.x, 0f, xMax - MinRectSize);
                    yMax = Mathf.Clamp(mousePixel.y, yMin + MinRectSize, atlasHeight);
                    break;

                case ResizeHandle.BottomRight:
                    xMax = Mathf.Clamp(mousePixel.x, xMin + MinRectSize, atlasWidth);
                    yMax = Mathf.Clamp(mousePixel.y, yMin + MinRectSize, atlasHeight);
                    break;
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static Rect ClampTopLeftRect(Rect rect, int atlasWidth, int atlasHeight)
        {
            float width = Mathf.Clamp(rect.width, MinRectSize, atlasWidth);
            float height = Mathf.Clamp(rect.height, MinRectSize, atlasHeight);
            float x = Mathf.Clamp(rect.x, 0f, Mathf.Max(0f, atlasWidth - width));
            float y = Mathf.Clamp(rect.y, 0f, Mathf.Max(0f, atlasHeight - height));
            return new Rect(x, y, width, height);
        }

        private static Rect SanitizeStoredRect(Rect rect, int atlasWidth, int atlasHeight)
        {
            if (atlasWidth <= 0 || atlasHeight <= 0)
                return new Rect(Mathf.Max(0f, rect.x), Mathf.Max(0f, rect.y), Mathf.Max(MinRectSize, rect.width), Mathf.Max(MinRectSize, rect.height));

            float width = Mathf.Clamp(rect.width, MinRectSize, atlasWidth);
            float height = Mathf.Clamp(rect.height, MinRectSize, atlasHeight);
            float x = Mathf.Clamp(rect.x, 0f, Mathf.Max(0f, atlasWidth - width));
            float y = Mathf.Clamp(rect.y, 0f, Mathf.Max(0f, atlasHeight - height));

            return new Rect(
                Mathf.Round(x),
                Mathf.Round(y),
                Mathf.Round(width),
                Mathf.Round(height));
        }

        private int ResolveAtlasTextureWidth()
        {
            Texture2D atlasTexture = atlasTextureProperty?.objectReferenceValue as Texture2D;
            return atlasTexture != null ? atlasTexture.width : 0;
        }

        private int ResolveAtlasTextureHeight()
        {
            Texture2D atlasTexture = atlasTextureProperty?.objectReferenceValue as Texture2D;
            return atlasTexture != null ? atlasTexture.height : 0;
        }

        private static Color ResolveSelectedRowColor()
        {
            return EditorGUIUtility.isProSkin ? SelectedRowColorPro : SelectedRowColorLight;
        }

        private bool IsBound => boundAsset != null && serializedObject != null && entriesProperty != null;

        private int EntryCount => entriesProperty != null && entriesProperty.isArray ? entriesProperty.arraySize : 0;
    }
}
