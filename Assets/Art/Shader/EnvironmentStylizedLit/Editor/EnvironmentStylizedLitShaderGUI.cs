using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BC.Rendering
{
    public sealed class EnvironmentStylizedLitShaderGUI : ShaderGUI
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

            DrawSurfaceSection();
            DrawBaseSection();
            DrawLightingSection();
            DrawShadowSection();
            DrawAmbientBounceSection();
            DrawSpecularSection();
            DrawNoiseSection();
            DrawCavityAoSection();
            DrawVertexGradientSection();
            DrawAdvancedSection(materials);
            DrawDebugSection();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(materials, "Edit Environment Stylized Lit Material");
                NormalizeMaterials(materials);
            }
        }

        private void DrawWarnings(Material[] materials)
        {
            foreach (string warningMessage in CollectWarningMessages(materials))
            {
                EditorGUILayout.HelpBox(warningMessage, MessageType.Warning);
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
                if (EnvironmentStylizedLitMaterialValidator.NeedsNormalization(material))
                {
                    requiresNormalization = true;
                    break;
                }
            }

            if (!requiresNormalization)
            {
                return false;
            }

            Undo.RecordObjects(materials, "Auto Normalize ESL Materials");

            bool changed = false;
            foreach (Material material in materials)
            {
                if (!EnvironmentStylizedLitMaterialValidator.Normalize(material))
                {
                    continue;
                }

                EditorUtility.SetDirty(material);
                changed = true;
            }

            return changed;
        }

        internal static string[] CollectWarningMessages(Material[] materials)
        {
            List<string> warningMessages = new List<string>();
            if (materials == null)
            {
                return warningMessages.ToArray();
            }

            foreach (Material material in materials)
            {
                if (material == null)
                {
                    continue;
                }

                if (EnvironmentStylizedLitMaterialValidator.TryGetDebugViewAuthoringWarning(material, out string debugWarning))
                {
                    warningMessages.Add(FormatWarning(material, debugWarning, materials.Length));
                }

                if (EnvironmentStylizedLitMaterialValidator.TryGetTriplanarPerformanceWarning(material, out string triplanarWarning))
                {
                    warningMessages.Add(FormatWarning(material, triplanarWarning, materials.Length));
                }
            }

            return warningMessages.ToArray();
        }

        private void DrawPresetSection(Material[] materials)
        {
            DrawSectionHeader("Preset");

            string[] presetNames = EnvironmentStylizedLitPresetUtility.GetPresetNames();
            if (presetNames.Length == 0)
            {
                EditorGUILayout.HelpBox("No EnvironmentStylizedLit presets are registered.", MessageType.Info);
                return;
            }

            selectedPresetIndex = Mathf.Clamp(selectedPresetIndex, 0, presetNames.Length - 1);
            selectedPresetIndex = EditorGUILayout.Popup("Preset", selectedPresetIndex, presetNames);

            string description = EnvironmentStylizedLitPresetUtility.GetPresetDescription(selectedPresetIndex);
            if (!string.IsNullOrWhiteSpace(description))
            {
                EditorGUILayout.HelpBox(description, MessageType.None);
            }

            using (new EditorGUI.DisabledScope(materials.Length == 0))
            {
                if (GUILayout.Button("Apply Selected Preset"))
                {
                    Undo.RecordObjects(materials, "Apply ESL Preset");

                    foreach (Material material in materials)
                    {
                        EnvironmentStylizedLitPresetUtility.ApplyPreset(material, selectedPresetIndex);
                        EditorUtility.SetDirty(material);
                    }
                }
            }
        }

        private void DrawSurfaceSection()
        {
            DrawSectionHeader("Surface");
            DrawProperty("_Cull");
            DrawProperty("_AlphaClip");

            if (GetFloatValue("_AlphaClip") > 0.5f)
            {
                DrawProperty("_Cutoff");
            }
        }

        private void DrawBaseSection()
        {
            DrawSectionHeader("Base");
            DrawTextureWithColor("_BaseMap", "_BaseColor", "Base Map");
            DrawTextureWithFloat("_NormalMap", "_NormalScale", "Normal Map");
            DrawTextureWithFloat("_OcclusionMap", "_OcclusionStrength", "Occlusion Map");
            DrawTexture("_EmissionMap", "Emission Map");
            DrawProperty("_EmissionColor");
            DrawProperty("_EmissionStrength");
        }

        private void DrawLightingSection()
        {
            DrawSectionHeader("Lighting");
            DrawProperty("_LightStepCount");
            DrawProperty("_LightStepSmoothness");
            DrawProperty("_WrapLighting");
            DrawProperty("_BandContrast");
            DrawProperty("_BandOffset");
            DrawProperty("_AdditionalLightMode");
            DrawProperty("_AdditionalLightIntensity");
            DrawProperty("_AdditionalLightShadowInfluence");
            DrawProperty("_AdditionalLightColorInfluence");
        }

        private void DrawShadowSection()
        {
            DrawSectionHeader("Shadow");
            DrawProperty("_DeepShadowColor");
            DrawProperty("_ShadowColor");
            DrawProperty("_MidColor");
            DrawProperty("_LightColor");
            DrawProperty("_HighlightColor");
            DrawProperty("_ShadowInfluence");
            DrawProperty("_ShadowSoftFill");
            DrawProperty("_ShadowColorBlend");
        }

        private void DrawAmbientBounceSection()
        {
            DrawSectionHeader("Ambient / Bounce");
            DrawProperty("_AmbientTopColor");
            DrawProperty("_AmbientSideColor");
            DrawProperty("_AmbientBottomColor");
            DrawProperty("_AmbientStrength");
            DrawProperty("_BounceColor");
            DrawProperty("_BounceStrength");
            DrawProperty("_BounceDirection");
            DrawProperty("_BounceWrap");
            DrawProperty("_IndirectShadowColor");
            DrawProperty("_IndirectStrength");
            DrawProperty("_IndirectStylizeStrength");
        }

        private void DrawSpecularSection()
        {
            DrawSectionHeader("Specular");
            DrawProperty("_Metallic");
            DrawProperty("_Smoothness");
            DrawProperty("_SpecularColor");
            DrawProperty("_SpecularMode");
            DrawProperty("_SpecularStrength");
            DrawProperty("_SpecularStepCount");
            DrawProperty("_SpecularStepSmoothness");
            DrawProperty("_EdgeSheenStrength");
            DrawProperty("_EdgeSheenPower");
            DrawProperty("_EdgeSheenColor");
        }

        private void DrawNoiseSection()
        {
            DrawSectionHeader("Noise");
            DrawProperty("_NoiseSpace");
            DrawProperty("_AlbedoNoiseStrength");
            DrawProperty("_WorldNoiseScale");
            DrawProperty("_WorldNoiseStrength");
            DrawProperty("_WorldNoiseContrast");
            DrawProperty("_LightBandNoiseStrength");
            DrawProperty("_LightBandNoiseScale");
            DrawProperty("_NoiseDistanceFadeStart");
            DrawProperty("_NoiseDistanceFadeEnd");
        }

        private void DrawCavityAoSection()
        {
            DrawSectionHeader("Cavity / AO");
            DrawProperty("_OcclusionStrength");
            DrawProperty("_CavityStrength");
            DrawProperty("_CavityColor");
        }

        private void DrawVertexGradientSection()
        {
            DrawSectionHeader("Vertex / Gradient");
            DrawProperty("_VertexColorEnabled");
            DrawProperty("_VertexColorCavityStrength");
            DrawProperty("_VertexColorBandOffsetStrength");
            DrawProperty("_VertexColorColorVariationStrength");
            DrawProperty("_WorldYGradientEnabled");
            DrawProperty("_WorldYGradientTopColor");
            DrawProperty("_WorldYGradientBottomColor");
            DrawProperty("_WorldYGradientMin");
            DrawProperty("_WorldYGradientMax");
            DrawProperty("_WorldYGradientStrength");
        }

        private void DrawAdvancedSection(Material[] materials)
        {
            DrawSectionHeader("Advanced");
            DrawProperty("_TriplanarBaseMapEnabled");
            DrawProperty("_TriplanarNormalMapEnabled");
            DrawProperty("_TriplanarNoiseEnabled");
            DrawProperty("_TriplanarScale");
            DrawProperty("_TriplanarBlendSharpness");

            using (new EditorGUI.DisabledScope(materials.Length == 0))
            {
                if (GUILayout.Button("Normalize Selected Materials"))
                {
                    Undo.RecordObjects(materials, "Normalize ESL Materials");
                    NormalizeMaterials(materials);
                }
            }
        }

        private void DrawDebugSection()
        {
            DrawSectionHeader("Debug");
            DrawProperty("_DebugView");
        }

        private void RebuildPropertyLookup(MaterialProperty[] properties)
        {
            propertyLookup.Clear();

            foreach (MaterialProperty property in properties)
            {
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
            if (TryGetProperty(propertyName, out MaterialProperty property))
            {
                materialEditor.ShaderProperty(property, property.displayName);
            }
        }

        private void DrawTexture(string texturePropertyName, string label)
        {
            if (TryGetProperty(texturePropertyName, out MaterialProperty textureProperty))
            {
                materialEditor.TexturePropertySingleLine(new GUIContent(label), textureProperty);
            }
        }

        private void DrawTextureWithColor(string texturePropertyName, string colorPropertyName, string label)
        {
            if (TryGetProperty(texturePropertyName, out MaterialProperty textureProperty) &&
                TryGetProperty(colorPropertyName, out MaterialProperty colorProperty))
            {
                materialEditor.TexturePropertySingleLine(new GUIContent(label), textureProperty, colorProperty);
                return;
            }

            DrawTexture(texturePropertyName, label);
            DrawProperty(colorPropertyName);
        }

        private void DrawTextureWithFloat(string texturePropertyName, string floatPropertyName, string label)
        {
            if (TryGetProperty(texturePropertyName, out MaterialProperty textureProperty) &&
                TryGetProperty(floatPropertyName, out MaterialProperty floatProperty))
            {
                materialEditor.TexturePropertySingleLine(new GUIContent(label), textureProperty, floatProperty);
                return;
            }

            DrawTexture(texturePropertyName, label);
            DrawProperty(floatPropertyName);
        }

        private bool TryGetProperty(string propertyName, out MaterialProperty property)
        {
            return propertyLookup.TryGetValue(propertyName, out property);
        }

        private float GetFloatValue(string propertyName)
        {
            return TryGetProperty(propertyName, out MaterialProperty property) ? property.floatValue : 0f;
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

        private static void NormalizeMaterials(Material[] materials)
        {
            foreach (Material material in materials)
            {
                if (EnvironmentStylizedLitMaterialValidator.Normalize(material))
                {
                    EditorUtility.SetDirty(material);
                }
            }
        }

        private static string FormatWarning(Material material, string warningMessage, int selectionCount)
        {
            return selectionCount > 1 ? material.name + ": " + warningMessage : warningMessage;
        }
    }
}