using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BC.Rendering
{
    public sealed class ParticleDistortionShaderGUI : ShaderGUI
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
            DrawDistortionSection();
            DrawFadeSection();
            DrawNoiseSection();
            DrawOptionalSection();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(materials, "Edit Particle Distortion Material");
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
                if (ParticleDistortionMaterialValidator.NeedsNormalization(material))
                {
                    requiresNormalization = true;
                    break;
                }
            }

            if (!requiresNormalization)
            {
                return false;
            }

            Undo.RecordObjects(materials, "Auto Normalize Particle Distortion Materials");

            bool changed = false;
            foreach (Material material in materials)
            {
                if (!ParticleDistortionMaterialValidator.Normalize(material))
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
                if (ParticleDistortionMaterialValidator.TryGetOpaqueTextureDependencyWarning(material, out string opaqueTextureWarning))
                {
                    EditorGUILayout.HelpBox(FormatWarning(material, opaqueTextureWarning, materials.Length), MessageType.Warning);
                }

                if (ParticleDistortionMaterialValidator.TryGetPerformanceWarning(material, out string performanceWarning))
                {
                    EditorGUILayout.HelpBox(FormatWarning(material, performanceWarning, materials.Length), MessageType.Warning);
                }
            }
        }

        private void DrawPresetSection(Material[] materials)
        {
            DrawSectionHeader("Preset");

            string[] presetNames = ParticleDistortionPresetUtility.GetPresetNames();
            if (presetNames.Length == 0)
            {
                EditorGUILayout.HelpBox("No ParticleDistortion presets are registered.", MessageType.Info);
                return;
            }

            selectedPresetIndex = Mathf.Clamp(selectedPresetIndex, 0, presetNames.Length - 1);
            selectedPresetIndex = EditorGUILayout.Popup("Preset", selectedPresetIndex, presetNames);

            string description = ParticleDistortionPresetUtility.GetPresetDescription(selectedPresetIndex);
            if (!string.IsNullOrWhiteSpace(description))
            {
                EditorGUILayout.HelpBox(description, MessageType.None);
            }

            using (new EditorGUI.DisabledScope(materials.Length == 0))
            {
                if (GUILayout.Button("Apply Selected Preset"))
                {
                    Undo.RecordObjects(materials, "Apply Particle Distortion Preset");
                    foreach (Material material in materials)
                    {
                        ParticleDistortionPresetUtility.ApplyPreset(material, selectedPresetIndex);
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

        private void DrawDistortionSection()
        {
            DrawSectionHeader("Distortion");
            DrawTextureWithFloat("_DistortionMap", "_DistortionStrength", "Distortion Map");
            DrawProperty("_DistortionScale");
            DrawProperty("_DistortionScrollSpeed");
            DrawProperty("_Alpha");
            DrawProperty("_UseVertexColor");
        }

        private void DrawFadeSection()
        {
            DrawSectionHeader("Fade");
            DrawProperty("_EdgeFadePower");
            DrawProperty("_EdgeFadeStrength");
        }

        private void DrawNoiseSection()
        {
            DrawSectionHeader("Noise");
            DrawTexture("_NoiseMap", "Noise Map");
            DrawProperty("_NoiseStrength");
            if (GetFloatValue("_NoiseStrength") > 0.001f)
            {
                DrawProperty("_NoiseScale");
            }
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
                if (!ParticleDistortionMaterialValidator.Normalize(material))
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

        private void DrawTexture(string texturePropertyName, string label)
        {
            if (!propertyLookup.TryGetValue(texturePropertyName, out MaterialProperty textureProperty))
            {
                return;
            }

            materialEditor.TexturePropertySingleLine(new GUIContent(label), textureProperty);
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

        private float GetFloatValue(string propertyName)
        {
            if (!propertyLookup.TryGetValue(propertyName, out MaterialProperty property))
            {
                return 0f;
            }

            return property.floatValue;
        }

        private static string FormatWarning(Material material, string warningMessage, int materialCount)
        {
            if (materialCount <= 1 || material == null)
            {
                return warningMessage;
            }

            return material.name + ": " + warningMessage;
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