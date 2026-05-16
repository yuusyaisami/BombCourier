using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BC.Rendering
{
    public sealed class ParticleUnlitShaderGUI : ShaderGUI
    {
        private static int selectedPresetIndex;
        private static int selectedTierIndex = 1;
        private static readonly string[] debugModeOptions =
        {
            "Final",
            "Base RGB",
            "Base Alpha",
            "Vertex Color",
            "Vertex Alpha",
            "MaskMap R / Dissolve",
            "MaskMap G / Emission",
            "MaskMap B / Variation",
            "MaskMap A / Shape",
            "Noise",
            "Dissolve Result",
            "Emission Result",
            "Soft Circle",
            "Custom1",
            "Custom2",
            "UV"
        };

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
            DrawQualityTierSection(materials);

            EditorGUI.BeginChangeCheck();

            DrawRenderingSection();
            DrawBaseSection();
            DrawShapeSection();
            DrawMaskSection();
            DrawNoiseSection();
            DrawDissolveSection();
            DrawEmissionSection();
            DrawDepthSection();
            DrawDebugSection();
            DrawOptionalSection();

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
                if (ParticleUnlitMaterialValidator.TryGetDebugViewAuthoringWarning(material, out string debugWarning))
                {
                    EditorGUILayout.HelpBox(FormatWarning(material, debugWarning, materials.Length), MessageType.Warning);
                }

                if (ParticleUnlitMaterialValidator.TryGetPerformanceWarning(material, out string warningMessage))
                {
                    EditorGUILayout.HelpBox(FormatWarning(material, warningMessage, materials.Length), MessageType.Warning);
                }

                if (ParticleUnlitMaterialValidator.TryGetTierMismatchWarning(material, out string tierMismatchWarning))
                {
                    EditorGUILayout.HelpBox(FormatWarning(material, tierMismatchWarning, materials.Length), MessageType.Warning);
                }

                if (ParticleUnlitMaterialValidator.TryGetWebGlTierWarning(material, out string webGlTierWarning))
                {
                    EditorGUILayout.HelpBox(FormatWarning(material, webGlTierWarning, materials.Length), MessageType.Warning);
                }

                if (ParticleUnlitMaterialValidator.TryGetDepthInteractionWarning(material, out string depthWarning))
                {
                    EditorGUILayout.HelpBox(FormatWarning(material, depthWarning, materials.Length), MessageType.Warning);
                }
            }
        }

        private void DrawQualityTierSection(Material[] materials)
        {
            DrawSectionHeader("Quality Tier");

            string[] tierNames = ParticleUnlitQualityTierUtility.GetTierNames();
            if (tierNames.Length == 0)
            {
                EditorGUILayout.HelpBox("No ParticleUnlit quality tiers are registered.", MessageType.Info);
                return;
            }

            selectedTierIndex = Mathf.Clamp(selectedTierIndex, 0, tierNames.Length - 1);
            selectedTierIndex = EditorGUILayout.Popup("Tier", selectedTierIndex, tierNames);

            string description = ParticleUnlitQualityTierUtility.GetTierDescription(selectedTierIndex);
            if (!string.IsNullOrWhiteSpace(description))
            {
                EditorGUILayout.HelpBox(description, MessageType.None);
            }

            if (materials != null && materials.Length > 0 && ParticleUnlitMaterialValidator.TryGetTierSummary(materials[0], out string summary))
            {
                EditorGUILayout.HelpBox(summary, MessageType.None);
            }

            using (new EditorGUI.DisabledScope(materials == null || materials.Length == 0))
            {
                if (GUILayout.Button("Apply Selected Tier"))
                {
                    Undo.RecordObjects(materials, "Apply Particle Quality Tier");
                    foreach (Material material in materials)
                    {
                        ParticleUnlitQualityTierUtility.ApplyTier(material, selectedTierIndex);
                        ParticleUnlitMaterialValidator.Normalize(material);
                        EditorUtility.SetDirty(material);
                    }
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

        private void DrawShapeSection()
        {
            DrawSectionHeader("Shape");
            DrawProperty("_SoftCircleStrength");
            DrawProperty("_EdgeFadePower");
            DrawProperty("_EdgeFadeStrength");
        }

        private void DrawMaskSection()
        {
            DrawSectionHeader("Mask");
            DrawTexture("_MaskMap", "Mask Map");
            DrawProperty("_MaskStrength");
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

        private void DrawEmissionSection()
        {
            DrawSectionHeader("Emission");
            DrawProperty("_EmissionColor");
            DrawProperty("_EmissionStrength");
            DrawProperty("_EmissionAlphaInfluence");
        }

        private void DrawDepthSection()
        {
            DrawSectionHeader("Depth");
            DrawProperty("_UseSoftParticles");

            if (GetFloatValue("_UseSoftParticles") > 0.5f)
            {
                DrawProperty("_SoftParticleDistance");
            }

            DrawProperty("_UseCameraFade");

            if (GetFloatValue("_UseCameraFade") > 0.5f)
            {
                DrawProperty("_CameraFadeNear");
                DrawProperty("_CameraFadeFar");
            }
        }

        private void DrawDebugSection()
        {
            DrawSectionHeader("Debug");
            DrawDebugModeProperty();

            if (GetFloatValue("_DebugMode") > 0.5f)
            {
                EditorGUILayout.HelpBox("Debug Mode overrides the final particle shading for troubleshooting. Reset it to Final for regular authoring review.", MessageType.None);
            }
        }

        private void DrawOptionalSection()
        {
            DrawSectionHeader("Optional");
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

        private void DrawDebugModeProperty()
        {
            if (!propertyLookup.TryGetValue("_DebugMode", out MaterialProperty property))
            {
                return;
            }

            int currentMode = Mathf.Clamp(Mathf.RoundToInt(property.floatValue), 0, debugModeOptions.Length - 1);
            EditorGUI.showMixedValue = property.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            int selectedMode = EditorGUILayout.Popup(property.displayName, currentMode, debugModeOptions);
            EditorGUI.showMixedValue = false;

            if (EditorGUI.EndChangeCheck())
            {
                property.floatValue = selectedMode;
            }
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

        private void DrawTexture(string texturePropertyName, string label)
        {
            if (!propertyLookup.TryGetValue(texturePropertyName, out MaterialProperty textureProperty))
            {
                return;
            }

            materialEditor.TexturePropertySingleLine(new GUIContent(label), textureProperty);
        }

        private float GetFloatValue(string propertyName)
        {
            if (!propertyLookup.TryGetValue(propertyName, out MaterialProperty property))
            {
                return 0f;
            }

            return property.floatValue;
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