using BC.Editor.Foundation.IMGUI;
using BC.Gimmick.MovingPlatform;
using UnityEditor;
using UnityEngine;

namespace BC.Editor.Gimmick.MovingPlatformTools
{
    [CustomPropertyDrawer(typeof(MovingPlatformRailNodeAuthoring))]
    public sealed class MovingPlatformRailNodeAuthoringDrawer : PropertyDrawerBase
    {
        protected override float GetPropertyHeightCore(SerializedProperty property, GUIContent label)
        {
            SerializedProperty parentRailNodeId = property.FindPropertyRelative("parentRailNodeId");
            SerializedProperty localPosition = property.FindPropertyRelative("localPosition");
            SerializedProperty overrideIncomingTiming = property.FindPropertyRelative("overrideIncomingTiming");
            SerializedProperty incomingTimingControl = property.FindPropertyRelative("incomingTimingControl");
            SerializedProperty incomingDuration = property.FindPropertyRelative("incomingDuration");
            SerializedProperty incomingSpeed = property.FindPropertyRelative("incomingSpeed");
            SerializedProperty incomingEasingMode = property.FindPropertyRelative("incomingEasingMode");

            float height = 0f;
            height += GetChildHeight(parentRailNodeId) + Spacing;
            height += GetChildHeight(localPosition) + Spacing;
            height += GetChildHeight(overrideIncomingTiming) + Spacing;
            height += GetChildHeight(incomingTimingControl) + Spacing;
            height += GetChildHeight(incomingDuration) + Spacing;
            height += GetChildHeight(incomingSpeed) + Spacing;
            height += GetChildHeight(incomingEasingMode);
            return height;
        }

        protected override void DrawProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty parentRailNodeId = property.FindPropertyRelative("parentRailNodeId");
            SerializedProperty localPosition = property.FindPropertyRelative("localPosition");
            SerializedProperty overrideIncomingTiming = property.FindPropertyRelative("overrideIncomingTiming");
            SerializedProperty incomingTimingControl = property.FindPropertyRelative("incomingTimingControl");
            SerializedProperty incomingDuration = property.FindPropertyRelative("incomingDuration");
            SerializedProperty incomingSpeed = property.FindPropertyRelative("incomingSpeed");
            SerializedProperty incomingEasingMode = property.FindPropertyRelative("incomingEasingMode");

            Rect cursor = EditorGUI.IndentedRect(position);
            DrawChild(ref cursor, parentRailNodeId, new GUIContent("Parent Rail Node"));
            DrawChild(ref cursor, localPosition, new GUIContent("Local Position"));
            DrawChild(ref cursor, overrideIncomingTiming, new GUIContent("Override Timing"));
            DrawChild(ref cursor, incomingTimingControl, new GUIContent("Timing Control"));
            DrawChild(ref cursor, incomingDuration, new GUIContent("Duration"));
            DrawChild(ref cursor, incomingSpeed, new GUIContent("Speed"));
            DrawChild(ref cursor, incomingEasingMode, new GUIContent("Easing Mode"));
        }
    }

    [CustomPropertyDrawer(typeof(MovingPlatformSelectorNodeAuthoring))]
    public sealed class MovingPlatformSelectorNodeAuthoringDrawer : PropertyDrawerBase
    {
        protected override float GetPropertyHeightCore(SerializedProperty property, GUIContent label)
        {
            SerializedProperty anchorRailNodeId = property.FindPropertyRelative("anchorRailNodeId");
            SerializedProperty rule = property.FindPropertyRelative("rule");
            SerializedProperty orderedChildren = property.FindPropertyRelative("orderedChildren");

            float height = 0f;
            height += GetChildHeight(anchorRailNodeId) + Spacing;
            height += GetChildHeight(rule) + Spacing;
            height += GetChildHeight(orderedChildren);
            return height;
        }

        protected override void DrawProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty anchorRailNodeId = property.FindPropertyRelative("anchorRailNodeId");
            SerializedProperty rule = property.FindPropertyRelative("rule");
            SerializedProperty orderedChildren = property.FindPropertyRelative("orderedChildren");

            Rect cursor = EditorGUI.IndentedRect(position);
            DrawChild(ref cursor, anchorRailNodeId, new GUIContent("Anchor Rail Node"));
            DrawChild(ref cursor, rule, new GUIContent("Layer Settings"));
            DrawChild(ref cursor, orderedChildren, new GUIContent("Step Data"));
        }
    }
}
