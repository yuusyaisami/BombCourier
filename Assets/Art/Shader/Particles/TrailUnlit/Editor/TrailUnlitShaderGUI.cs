using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BC.Rendering
{
    public sealed class TrailUnlitShaderGUI : ShaderGUI
    {
        private static int selectedPresetIndex;

        private readonly Dictionary<string, MaterialProperty> propertyLookup = new Dictionary<string, MaterialProperty>();

        private MaterialEditor materialEditor;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            this.materialEditor = materialEditor;
            RebuildPropertyLookup(properties);

            Material[] materials = GetSelectedMaterials(materialEditor.targets);
            AutoNormalizeMaterials(materials);
            DrawWarnings(materials);
            DrawPresetSection(materials);

            EditorGUI.BeginChangeCheck();

            DrawRenderingSection();
            DrawBaseSection();
            DrawUVSection();
            DrawNoiseSection();
            DrawDissolveSection();
            DrawEdgeFadeSection();
            DrawEmissionSection();
            DrawAdvancedSection();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(materials, "Edit Trail Unlit Material");
                NormalizeMaterials(materials);
            }
        }

        internal static bool AutoNormalizeMaterials(Material[] materials)
        {
            if (materials == null || materials.Length == 0)
            {
                return false;
            }

            bool requiresNormalization = false;
            foreach (Material material in materials)
            {
                if (TrailUnlitMaterialValidator.NeedsNormalization(material))
                {
                    requiresNormalization = true;
                    break;
                }
            }

            if (!requiresNormalization)
            {
                return false;
            }

            Undo.RecordObjects(materials, "Auto Normalize Trail Materials");

            bool changed = false;
            foreach (Material material in materials)
            {
                if (!TrailUnlitMaterialValidator.Normalize(material))
                {
                    continue;
                }

                EditorUtility.SetDirty(material);
                changed = true;
            }

            return changed;
        }

        private void DrawWarnings(Material[] materials)
        {
            if (materials == null)
            {
                return;
            }

            foreach (Material material in materials)
            {
                if (TrailUnlitMaterialValidator.TryGetPerformanceWarning(material, out string warningMessage))
                {
                    EditorGUILayout.HelpBox(FormatWarning(material, warningMessage, materials.Length), MessageType.Warning);
                }
            }
        }

        private void DrawPresetSection(Material[] materials)
        {
            DrawSectionHeader("Preset");

            string[] presetNames = TrailUnlitPresetUtility.GetPresetNames();
            if (presetNames.Length == 0)
            {
                EditorGUILayout.HelpBox("No TrailUnlit presets are registered.", MessageType.Info);
                return;
            }

            selectedPresetIndex = Mathf.Clamp(selectedPresetIndex, 0, presetNames.Length - 1);
            selectedPresetIndex = EditorGUILayout.Popup("Preset", selectedPresetIndex, presetNames);

            string description = TrailUnlitPresetUtility.GetPresetDescription(selectedPresetIndex);
            if (!string.IsNullOrWhiteSpace(description))
            {
                EditorGUILayout.HelpBox(description, MessageType.None);
            }

            using (new EditorGUI.DisabledScope(materials.Length == 0))
            {
                if (GUILayout.Button("Apply Selected Preset"))
                {
                    Undo.RecordObjects(materials, "Apply Trail Preset");

                    foreach (Material material in materials)
                    {
                        TrailUnlitPresetUtility.ApplyPreset(material, selectedPresetIndex);
                        EditorUtility.SetDirty(material);
                    }
                }
            }
        }

        private void DrawRenderingSection()
        {
            DrawSectionHeader("Rendering");
            DrawProperty("_BlendMode");
            DrawProperty("_Cull");
            DrawProperty("_ZTest");
        }

        private void DrawBaseSection()
        {
            DrawSectionHeader("Base");
            DrawTextureWithColor("_BaseMap", "_BaseColor", "Base Map");
            DrawProperty("_Alpha");
            DrawProperty("_Brightness");
            DrawProperty("_UseVertexColor");
        }

        private void DrawUVSection()
        {
            DrawSectionHeader("UV");
            DrawProperty("_UVScrollSpeed");
        }

        private void DrawNoiseSection()
        {
            DrawSectionHeader("Noise");
            DrawTexture("_NoiseMap", "Noise Map");
            DrawProperty("_NoiseStrength");

            if (GetFloatValue("_NoiseStrength") > 0.001f || GetFloatValue("_DissolveAmount") > 0.001f)
            {
                DrawProperty("_NoiseScale");
                DrawProperty("_NoiseScrollSpeed");
            }
        }

        private void DrawDissolveSection()
        {
            DrawSectionHeader("Dissolve");
            DrawProperty("_DissolveAmount");
            DrawProperty("_DissolveSoftness");
        }

        private void DrawEdgeFadeSection()
        {
            DrawSectionHeader("Edge Fade");
            DrawProperty("_EdgeFadeAxis");
            DrawProperty("_EdgeFadePower");
            DrawProperty("_EdgeFadeStrength");
        }

        private void DrawEmissionSection()
        {
            DrawSectionHeader("Emission");
            DrawProperty("_EmissionColor");
            DrawProperty("_EmissionStrength");
        }

        private void DrawAdvancedSection()
        {
            DrawSectionHeader("Advanced");
            DrawProperty("_QueueOffset");
        }

        private void RebuildPropertyLookup(MaterialProperty[] properties)
        {
            propertyLookup.Clear();

            foreach (MaterialProperty property in properties)
            {
                propertyLookup[property.name] = property;
            }
        }

        private void DrawProperty(string propertyName)
        {
            if (!propertyLookup.TryGetValue(propertyName, out MaterialProperty property))
            {
                return;
            }

            materialEditor.ShaderProperty(property, property.displayName);
        }

        private void DrawTexture(string texturePropertyName, string label)
        {
            if (!propertyLookup.TryGetValue(texturePropertyName, out MaterialProperty textureProperty))
            {
                return;
            }

            materialEditor.TexturePropertySingleLine(new GUIContent(label), textureProperty);
            materialEditor.TextureScaleOffsetProperty(textureProperty);
        }

        private void DrawTextureWithColor(string texturePropertyName, string colorPropertyName, string label)
        {
            if (!propertyLookup.TryGetValue(texturePropertyName, out MaterialProperty textureProperty))
            {
                return;
            }

            if (propertyLookup.TryGetValue(colorPropertyName, out MaterialProperty colorProperty))
            {
                materialEditor.TexturePropertySingleLine(new GUIContent(label), textureProperty, colorProperty);
            }
            else
            {
                materialEditor.TexturePropertySingleLine(new GUIContent(label), textureProperty);
            }

            materialEditor.TextureScaleOffsetProperty(textureProperty);
        }

        private float GetFloatValue(string propertyName)
        {
            return propertyLookup.TryGetValue(propertyName, out MaterialProperty property) ? property.floatValue : 0f;
        }

        private static void NormalizeMaterials(Material[] materials)
        {
            foreach (Material material in materials)
            {
                if (TrailUnlitMaterialValidator.Normalize(material))
                {
                    EditorUtility.SetDirty(material);
                }
            }
        }

        private static Material[] GetSelectedMaterials(Object[] targets)
        {
            List<Material> materials = new List<Material>();
            foreach (Object target in targets)
            {
                if (target is Material material)
                {
                    materials.Add(material);
                }
            }

            return materials.ToArray();
        }

        private static void DrawSectionHeader(string title)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        private static string FormatWarning(Material material, string warningMessage, int materialCount)
        {
            if (materialCount <= 1 || material == null)
            {
                return warningMessage;
            }

            return material.name + ": " + warningMessage;
        }
    }
}
