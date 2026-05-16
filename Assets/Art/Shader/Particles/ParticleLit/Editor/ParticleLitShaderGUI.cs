using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BC.Rendering
{
    public sealed class ParticleLitShaderGUI : ShaderGUI
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
            DrawPresetSection(materials);

            EditorGUI.BeginChangeCheck();

            DrawRenderingSection();
            DrawBaseSection();
            DrawLightingSection();
            DrawEmissionSection();
            DrawOptionalSection();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(materials, "Edit Particle Lit Material");
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
                if (ParticleLitMaterialValidator.NeedsNormalization(material))
                {
                    requiresNormalization = true;
                    break;
                }
            }

            if (!requiresNormalization)
            {
                return false;
            }

            Undo.RecordObjects(materials, "Auto Normalize Particle Lit Materials");

            bool changed = false;
            foreach (Material material in materials)
            {
                if (!ParticleLitMaterialValidator.Normalize(material))
                {
                    continue;
                }

                EditorUtility.SetDirty(material);
                changed = true;
            }

            return changed;
        }

        private void DrawPresetSection(Material[] materials)
        {
            DrawSectionHeader("Preset");

            string[] presetNames = ParticleLitPresetUtility.GetPresetNames();
            if (presetNames.Length == 0)
            {
                EditorGUILayout.HelpBox("No ParticleLit presets are registered.", MessageType.Info);
                return;
            }

            selectedPresetIndex = Mathf.Clamp(selectedPresetIndex, 0, presetNames.Length - 1);
            selectedPresetIndex = EditorGUILayout.Popup("Preset", selectedPresetIndex, presetNames);

            string description = ParticleLitPresetUtility.GetPresetDescription(selectedPresetIndex);
            if (!string.IsNullOrWhiteSpace(description))
            {
                EditorGUILayout.HelpBox(description, MessageType.None);
            }

            using (new EditorGUI.DisabledScope(materials.Length == 0))
            {
                if (GUILayout.Button("Apply Selected Preset"))
                {
                    Undo.RecordObjects(materials, "Apply Particle Lit Preset");
                    foreach (Material material in materials)
                    {
                        ParticleLitPresetUtility.ApplyPreset(material, selectedPresetIndex);
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
            DrawProperty("_UseVertexColor");
            DrawTextureWithFloat("_NormalMap", "_NormalScale", "Normal Map");
        }

        private void DrawLightingSection()
        {
            DrawSectionHeader("Lighting");
            DrawProperty("_Smoothness");
            DrawProperty("_Metallic");
            DrawProperty("_LightInfluence");
        }

        private void DrawEmissionSection()
        {
            DrawSectionHeader("Emission");
            DrawProperty("_EmissionColor");
            DrawProperty("_EmissionStrength");
        }

        private void DrawOptionalSection()
        {
            DrawSectionHeader("Optional");
            DrawProperty("_QueueOffset");
        }

        private void NormalizeMaterials(Material[] materials)
        {
            if (materials == null)
            {
                return;
            }

            foreach (Material material in materials)
            {
                if (!ParticleLitMaterialValidator.Normalize(material))
                {
                    continue;
                }

                EditorUtility.SetDirty(material);
            }
        }

        private void RebuildPropertyLookup(MaterialProperty[] properties)
        {
            propertyLookup.Clear();
            if (properties == null)
            {
                return;
            }

            foreach (MaterialProperty property in properties)
            {
                if (property == null || string.IsNullOrEmpty(property.name))
                {
                    continue;
                }

                propertyLookup[property.name] = property;
            }
        }

        private void DrawSectionHeader(string title)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
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
            if (!propertyLookup.TryGetValue(texturePropertyName, out MaterialProperty textureProperty) ||
                !propertyLookup.TryGetValue(colorPropertyName, out MaterialProperty colorProperty))
            {
                return;
            }

            materialEditor.TexturePropertySingleLine(new GUIContent(label), textureProperty, colorProperty);
        }

        private void DrawTextureWithFloat(string texturePropertyName, string floatPropertyName, string label)
        {
            if (!propertyLookup.TryGetValue(texturePropertyName, out MaterialProperty textureProperty) ||
                !propertyLookup.TryGetValue(floatPropertyName, out MaterialProperty floatProperty))
            {
                return;
            }

            materialEditor.TexturePropertySingleLine(new GUIContent(label), textureProperty, floatProperty);
        }

        private static Material[] GetSelectedMaterials(Object[] targets)
        {
            if (targets == null || targets.Length == 0)
            {
                return System.Array.Empty<Material>();
            }

            List<Material> materials = new List<Material>(targets.Length);
            foreach (Object target in targets)
            {
                if (target is Material material)
                {
                    materials.Add(material);
                }
            }

            return materials.ToArray();
        }
    }
}