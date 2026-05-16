using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BC.Rendering
{
    public sealed class ParticleUnlitShaderGUI : ShaderGUI
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
            DrawEdgeFadeSection();
            DrawAdvancedSection();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(materials, "Edit Particle Unlit Material");
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
                if (ParticleUnlitMaterialValidator.NeedsNormalization(material))
                {
                    requiresNormalization = true;
                    break;
                }
            }

            if (!requiresNormalization)
            {
                return false;
            }

            Undo.RecordObjects(materials, "Auto Normalize Particle Materials");
            bool changed = false;
            foreach (Material material in materials)
            {
                if (!ParticleUnlitMaterialValidator.Normalize(material))
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
                if (ParticleUnlitMaterialValidator.TryGetPerformanceWarning(material, out string warningMessage))
                {
                    EditorGUILayout.HelpBox(FormatWarning(material, warningMessage, materials.Length), MessageType.Warning);
                }
            }
        }

        private void DrawPresetSection(Material[] materials)
        {
            DrawSectionHeader("Preset");

            string[] presetNames = ParticleUnlitPresetUtility.GetPresetNames();
            if (presetNames.Length == 0)
            {
                EditorGUILayout.HelpBox("No ParticleUnlit presets are registered.", MessageType.Info);
                return;
            }

            selectedPresetIndex = Mathf.Clamp(selectedPresetIndex, 0, presetNames.Length - 1);
            selectedPresetIndex = EditorGUILayout.Popup("Preset", selectedPresetIndex, presetNames);

            string description = ParticleUnlitPresetUtility.GetPresetDescription(selectedPresetIndex);
            if (!string.IsNullOrWhiteSpace(description))
            {
                EditorGUILayout.HelpBox(description, MessageType.None);
            }

            using (new EditorGUI.DisabledScope(materials.Length == 0))
            {
                if (GUILayout.Button("Apply Selected Preset"))
                {
                    Undo.RecordObjects(materials, "Apply Particle Preset");
                    foreach (Material material in materials)
                    {
                        ParticleUnlitPresetUtility.ApplyPreset(material, selectedPresetIndex);
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

        private void DrawEdgeFadeSection()
        {
            DrawSectionHeader("Edge Fade");
            DrawProperty("_SoftCircleStrength");
            DrawProperty("_EdgeFadePower");
            DrawProperty("_EdgeFadeStrength");
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

        private void DrawTextureWithColor(string texturePropertyName, string colorPropertyName, string label)
        {
            if (!propertyLookup.TryGetValue(texturePropertyName, out MaterialProperty textureProperty))
            {
                return;
            }

            MaterialProperty colorProperty = null;
            propertyLookup.TryGetValue(colorPropertyName, out colorProperty);
            materialEditor.TexturePropertySingleLine(new GUIContent(label), textureProperty, colorProperty);
        }

        private static void NormalizeMaterials(Material[] materials)
        {
            if (materials == null)
            {
                return;
            }

            foreach (Material material in materials)
            {
                if (ParticleUnlitMaterialValidator.Normalize(material))
                {
                    EditorUtility.SetDirty(material);
                }
            }
        }

        private static Material[] GetSelectedMaterials(Object[] targets)
        {
            List<Material> materials = new List<Material>();
            if (targets == null)
            {
                return materials.ToArray();
            }

            foreach (Object target in targets)
            {
                Material material = target as Material;
                if (material != null)
                {
                    materials.Add(material);
                }
            }

            return materials.ToArray();
        }

        private static string FormatWarning(Material material, string warningMessage, int selectionCount)
        {
            if (selectionCount <= 1 || material == null)
            {
                return warningMessage;
            }

            return material.name + ": " + warningMessage;
        }

        private static void DrawSectionHeader(string title)
        {
            GUILayout.Space(6f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }
    }
}