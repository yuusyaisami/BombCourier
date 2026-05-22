using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BC.Rendering.Tests
{
    public sealed class EnvironmentStylizedLitM12ValidationTests
    {
        private const string ShaderPath = "Assets/Art/Shader/EnvironmentStylizedLit/EnvironmentStylizedLit.shader";
        private const string ShaderGuiPath = "Assets/Art/Shader/EnvironmentStylizedLit/Editor/EnvironmentStylizedLitShaderGUI.cs";
        private const string ValidatorPath = "Assets/Art/Shader/EnvironmentStylizedLit/Editor/EnvironmentStylizedLitMaterialValidator.cs";
        private const string PresetUtilityPath = "Assets/Art/Shader/EnvironmentStylizedLit/Editor/EnvironmentStylizedLitPresetUtility.cs";

        [Test]
        public void M12EditorFilesExist()
        {
            Assert.IsTrue(File.Exists(ShaderPath), $"Expected shader asset: {ShaderPath}");
            Assert.IsTrue(File.Exists(ShaderGuiPath), $"Expected M12 ShaderGUI source: {ShaderGuiPath}");
            Assert.IsTrue(File.Exists(ValidatorPath), $"Expected M12 validator source: {ValidatorPath}");
            Assert.IsTrue(File.Exists(PresetUtilityPath), $"Expected M12 preset utility source: {PresetUtilityPath}");
        }

        [Test]
        public void M12ShaderAndGuiDeclareInspectorSectionsAndCustomEditor()
        {
            string shaderSource = File.ReadAllText(ShaderPath);
            string shaderGuiSource = File.ReadAllText(ShaderGuiPath);

            StringAssert.Contains("CustomEditor \"BC.Rendering.EnvironmentStylizedLitShaderGUI\"", shaderSource);
            StringAssert.Contains("DrawSectionHeader(\"Surface\")", shaderGuiSource);
            StringAssert.Contains("DrawSectionHeader(\"Base\")", shaderGuiSource);
            StringAssert.Contains("DrawSectionHeader(\"Lighting\")", shaderGuiSource);
            StringAssert.Contains("DrawSectionHeader(\"Shadow\")", shaderGuiSource);
            StringAssert.Contains("DrawSectionHeader(\"Ambient / Bounce\")", shaderGuiSource);
            StringAssert.Contains("DrawSectionHeader(\"Specular\")", shaderGuiSource);
            StringAssert.Contains("DrawSectionHeader(\"Noise\")", shaderGuiSource);
            StringAssert.Contains("DrawSectionHeader(\"Cavity / AO\")", shaderGuiSource);
            StringAssert.Contains("DrawSectionHeader(\"Vertex / Gradient\")", shaderGuiSource);
            StringAssert.Contains("DrawSectionHeader(\"Advanced\")", shaderGuiSource);
            StringAssert.Contains("DrawSectionHeader(\"Debug\")", shaderGuiSource);
            StringAssert.Contains("Apply Selected Preset", shaderGuiSource);
        }

        [Test]
        public void M12ValidatorNormalizesInvalidValuesAndSurfacesWarnings()
        {
            Material material = CreateTemporaryMaterial();

            try
            {
                material.SetFloat("_DebugView", 99f);
                material.SetFloat("_TriplanarBaseMapEnabled", 1.8f);
                material.SetFloat("_TriplanarNormalMapEnabled", -2f);
                material.SetFloat("_TriplanarNoiseEnabled", 1f);
                material.SetFloat("_WorldYGradientMin", 6f);
                material.SetFloat("_WorldYGradientMax", 2f);
                material.SetFloat("_NoiseDistanceFadeStart", 30f);
                material.SetFloat("_NoiseDistanceFadeEnd", 10f);

                Type validatorType = GetEditorAssemblyType("BC.Rendering.EnvironmentStylizedLitMaterialValidator");
                MethodInfo normalizeMethod = GetStaticMethod(validatorType, "Normalize", typeof(Material));
                Assert.IsTrue((bool)normalizeMethod.Invoke(null, new object[] { material }));

                Assert.AreEqual(9f, material.GetFloat("_DebugView"));
                Assert.AreEqual(1f, material.GetFloat("_TriplanarBaseMapEnabled"));
                Assert.AreEqual(0f, material.GetFloat("_TriplanarNormalMapEnabled"));
                Assert.AreEqual(1f, material.GetFloat("_TriplanarNoiseEnabled"));
                Assert.AreEqual(2f, material.GetFloat("_WorldYGradientMin"));
                Assert.AreEqual(6f, material.GetFloat("_WorldYGradientMax"));
                Assert.AreEqual(10f, material.GetFloat("_NoiseDistanceFadeStart"));
                Assert.AreEqual(30f, material.GetFloat("_NoiseDistanceFadeEnd"));
                Assert.IsTrue(material.IsKeywordEnabled("_ESL_TRIPLANAR_BASEMAP"));
                Assert.IsFalse(material.IsKeywordEnabled("_ESL_TRIPLANAR_NORMALMAP"));
                Assert.IsTrue(material.IsKeywordEnabled("_ESL_TRIPLANAR_NOISE"));

                AssertWarningContains(validatorType, "TryGetDebugViewAuthoringWarning", material, "SimpleBoostFresnel");
                AssertWarningContains(validatorType, "TryGetTriplanarPerformanceWarning", material, "Base Map");
                AssertWarningContains(validatorType, "TryGetTriplanarPerformanceWarning", material, "Noise");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void M12PresetUtilityDeclaresFivePresetsAndAppliesDistinctLooks()
        {
            Material material = CreateTemporaryMaterial();

            try
            {
                Type presetUtilityType = GetEditorAssemblyType("BC.Rendering.EnvironmentStylizedLitPresetUtility");
                MethodInfo getPresetNamesMethod = GetStaticMethod(presetUtilityType, "GetPresetNames");
                MethodInfo applyPresetMethod = GetStaticMethod(presetUtilityType, "ApplyPreset", typeof(Material), typeof(int));

                string[] presetNames = (string[])getPresetNamesMethod.Invoke(null, null);
                CollectionAssert.AreEqual(
                    new[]
                    {
                        "ClayDiorama",
                        "PaintedPlaster",
                        "MatteToyPlastic",
                        "CeramicToy",
                        "ChalkPastel"
                    },
                    presetNames);

                applyPresetMethod.Invoke(null, new object[] { material, 0 });
                float claySpecularStrength = material.GetFloat("_SpecularStrength");
                Color clayShadowColor = material.GetColor("_ShadowColor");

                applyPresetMethod.Invoke(null, new object[] { material, 3 });
                Assert.AreEqual(3f, material.GetFloat("_SpecularMode"));
                Assert.Greater(material.GetFloat("_Smoothness"), claySpecularStrength);

                applyPresetMethod.Invoke(null, new object[] { material, 4 });
                Assert.AreEqual(1f, material.GetFloat("_WorldYGradientEnabled"));
                Assert.Greater(material.GetFloat("_WorldYGradientStrength"), 0f);
                Assert.AreNotEqual(claySpecularStrength, material.GetFloat("_SpecularStrength"));
                Assert.AreNotEqual(clayShadowColor, material.GetColor("_WorldYGradientBottomColor"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void M12PresetApplicationIsDeterministicAcrossPriorState()
        {
            Material carryOverMaterial = CreateTemporaryMaterial();
            Material freshMaterial = CreateTemporaryMaterial();

            try
            {
                Type presetUtilityType = GetEditorAssemblyType("BC.Rendering.EnvironmentStylizedLitPresetUtility");
                MethodInfo applyPresetMethod = GetStaticMethod(presetUtilityType, "ApplyPreset", typeof(Material), typeof(int));

                applyPresetMethod.Invoke(null, new object[] { carryOverMaterial, 0 });
                applyPresetMethod.Invoke(null, new object[] { carryOverMaterial, 4 });
                applyPresetMethod.Invoke(null, new object[] { freshMaterial, 4 });

                AssertMaterialsMatchForPreset(freshMaterial, carryOverMaterial);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(carryOverMaterial);
                UnityEngine.Object.DestroyImmediate(freshMaterial);
            }
        }

        [Test]
        public void M12ShaderGuiHelpersNormalizeOnOpenAndCollectWarningsAcrossSelection()
        {
            Material primaryMaterial = CreateTemporaryMaterial();
            Material secondaryMaterial = CreateTemporaryMaterial();

            try
            {
                primaryMaterial.SetFloat("_WorldYGradientMin", 8f);
                primaryMaterial.SetFloat("_WorldYGradientMax", 2f);
                primaryMaterial.SetFloat("_TriplanarBaseMapEnabled", 1f);
                secondaryMaterial.SetFloat("_DebugView", 3f);

                Type shaderGuiType = GetEditorAssemblyType("BC.Rendering.EnvironmentStylizedLitShaderGUI");
                MethodInfo autoNormalizeMethod = GetStaticMethod(shaderGuiType, "AutoNormalizeMaterials", typeof(Material[]));
                MethodInfo collectWarningsMethod = GetStaticMethod(shaderGuiType, "CollectWarningMessages", typeof(Material[]));

                Assert.IsTrue((bool)autoNormalizeMethod.Invoke(null, new object[] { new[] { primaryMaterial, secondaryMaterial } }));
                Assert.AreEqual(2f, primaryMaterial.GetFloat("_WorldYGradientMin"));
                Assert.AreEqual(8f, primaryMaterial.GetFloat("_WorldYGradientMax"));
                Assert.IsTrue(primaryMaterial.IsKeywordEnabled("_ESL_TRIPLANAR_BASEMAP"));

                string[] warningMessages = (string[])collectWarningsMethod.Invoke(null, new object[] { new[] { primaryMaterial, secondaryMaterial } });
                Assert.That(warningMessages.Length, Is.GreaterThanOrEqualTo(2));
                CollectionAssert.Contains(warningMessages, primaryMaterial.name + ": Triplanar sampling increases texture and math cost. Enabled: Base Map. Keep it off on materials that already have stable UVs.");
                CollectionAssert.Contains(warningMessages, secondaryMaterial.name + ": Debug View is set to SteppedLight. Reset Debug View to Off before regular authoring review.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(primaryMaterial);
                UnityEngine.Object.DestroyImmediate(secondaryMaterial);
            }
        }

        private static Material CreateTemporaryMaterial()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Assert.IsNotNull(shader, "EnvironmentStylizedLit shader asset must load.");
            return new Material(shader);
        }

        private static Type GetEditorAssemblyType(string fullTypeName)
        {
            Type type = null;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullTypeName);
                if (type != null)
                {
                    break;
                }
            }

            Assert.IsNotNull(type, $"Expected editor type: {fullTypeName}");
            return type;
        }

        private static MethodInfo GetStaticMethod(Type declaringType, string methodName, params System.Type[] parameterTypes)
        {
            MethodInfo method = parameterTypes.Length == 0
                ? declaringType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                : declaringType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, parameterTypes, null);

            Assert.IsNotNull(method, $"Expected static method: {declaringType.FullName}.{methodName}");
            return method;
        }

        private static void AssertWarningContains(Type validatorType, string methodName, Material material, string expectedText)
        {
            MethodInfo method = GetStaticMethod(validatorType, methodName);
            object[] arguments = { material, null };
            Assert.IsTrue((bool)method.Invoke(null, arguments), $"Expected warning from {methodName}.");
            StringAssert.Contains(expectedText, arguments[1] as string);
        }

        private static void AssertMaterialsMatchForPreset(Material expected, Material actual)
        {
            Assert.AreEqual(expected.GetFloat("_SpecularStrength"), actual.GetFloat("_SpecularStrength"));
            Assert.AreEqual(expected.GetFloat("_AmbientStrength"), actual.GetFloat("_AmbientStrength"));
            Assert.AreEqual(expected.GetFloat("_WorldYGradientEnabled"), actual.GetFloat("_WorldYGradientEnabled"));
            Assert.AreEqual(expected.GetFloat("_WorldYGradientStrength"), actual.GetFloat("_WorldYGradientStrength"));
            Assert.AreEqual(expected.GetVector("_BounceDirection"), actual.GetVector("_BounceDirection"));
            Assert.AreEqual(expected.GetColor("_BaseColor"), actual.GetColor("_BaseColor"));
            Assert.AreEqual(expected.GetColor("_ShadowColor"), actual.GetColor("_ShadowColor"));
            Assert.AreEqual(expected.GetColor("_AmbientTopColor"), actual.GetColor("_AmbientTopColor"));
            Assert.AreEqual(expected.GetColor("_WorldYGradientTopColor"), actual.GetColor("_WorldYGradientTopColor"));
            Assert.AreEqual(expected.GetColor("_WorldYGradientBottomColor"), actual.GetColor("_WorldYGradientBottomColor"));
        }
    }
}