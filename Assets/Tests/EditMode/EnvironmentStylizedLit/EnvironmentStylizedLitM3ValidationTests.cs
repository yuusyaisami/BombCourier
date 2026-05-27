using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BC.Rendering.Tests
{
    public sealed class EnvironmentStylizedLitM3ValidationTests
    {
        private const string ShaderPath = "Assets/Art/Shader/EnvironmentStylizedLit/EnvironmentStylizedLit.shader";
        private const string LightingPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_Lighting.hlsl";
        private const string StylizedDiffusePath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_StylizedDiffuse.hlsl";
        private const string DebugPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_Debug.hlsl";
        private const string ForwardLitPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/Passes/EnvironmentStylizedLit_ForwardLitPass.hlsl";
        private const string TestRoomScenePath = "Assets/Scenes/EnvironmentStylizedLit/ESL_TestRoom.unity";
        private const string LightingLabScenePath = "Assets/Scenes/EnvironmentStylizedLit/ESL_LightingLab.unity";

        [Test]
        public void M3ShaderDeclaresStylizedMainLightProperties()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);

            Assert.IsNotNull(shader, "EnvironmentStylizedLit shader asset must load.");
            AssertShaderHasProperty(shader, "_LightStepCount");
            AssertShaderHasProperty(shader, "_LightStepSmoothness");
            AssertShaderHasProperty(shader, "_WrapLighting");
            AssertShaderHasProperty(shader, "_BandContrast");
            AssertShaderHasProperty(shader, "_BandOffset");
            AssertShaderHasProperty(shader, "_DeepShadowColor");
            AssertShaderHasProperty(shader, "_ShadowColor");
            AssertShaderHasProperty(shader, "_MidColor");
            AssertShaderHasProperty(shader, "_LightColor");
            AssertShaderHasProperty(shader, "_HighlightColor");
            AssertShaderHasProperty(shader, "_DebugView");
        }

        [Test]
        public void M3TemporaryMaterialSupportsStylizedDiffuseOverrides()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Material material = new Material(shader);
            Color expectedShadowColor = new Color(0.3f, 0.35f, 0.5f, 1f);
            Color expectedLightColor = new Color(0.95f, 0.9f, 0.82f, 1f);

            try
            {
                material.SetFloat("_LightStepCount", 5f);
                material.SetFloat("_LightStepSmoothness", 0.2f);
                material.SetFloat("_WrapLighting", 0.35f);
                material.SetFloat("_BandContrast", 1.6f);
                material.SetFloat("_BandOffset", -0.15f);
                material.SetFloat("_DebugView", 3f);
                material.SetColor("_ShadowColor", expectedShadowColor);
                material.SetColor("_LightColor", expectedLightColor);

                Assert.AreEqual(5f, material.GetFloat("_LightStepCount"));
                Assert.AreEqual(0.2f, material.GetFloat("_LightStepSmoothness"));
                Assert.AreEqual(0.35f, material.GetFloat("_WrapLighting"));
                Assert.AreEqual(1.6f, material.GetFloat("_BandContrast"));
                Assert.AreEqual(-0.15f, material.GetFloat("_BandOffset"));
                Assert.AreEqual(3f, material.GetFloat("_DebugView"));
                AssertColorApproximately(expectedShadowColor, material.GetColor("_ShadowColor"));
                AssertColorApproximately(expectedLightColor, material.GetColor("_LightColor"));
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void M3ValidationScenesContainDirectionalMainLightAnchors()
        {
            AssertSceneContainsDirectionalLight(TestRoomScenePath);
            AssertSceneContainsDirectionalLight(LightingLabScenePath);
        }

        [Test]
        public void M3SourceFilesKeepLightingResponsibilitiesSeparated()
        {
            string lightingSource = File.ReadAllText(LightingPath);
            string stylizedDiffuseSource = File.ReadAllText(StylizedDiffusePath);
            string debugSource = File.ReadAllText(DebugPath);
            string forwardLitSource = File.ReadAllText(ForwardLitPath);

            StringAssert.Contains("ESL_GetMainLightData", lightingSource);
            StringAssert.Contains("GetMainLight()", lightingSource);
            StringAssert.Contains("ESL_EvaluateBandColor", lightingSource);
            StringAssert.Contains("ESL_EvaluateLightBandAttenuation", lightingSource);
            StringAssert.Contains("ESL_EvaluateMainLightBandAttenuation", lightingSource);
            StringAssert.Contains("float bandIndex = floor(clampedLight * 4.0 + 0.5);", lightingSource);
            StringAssert.Contains("return max(0.25, steppedAttenuation);", lightingSource);
            StringAssert.Contains("return _HighlightColor.rgb;", lightingSource);
            StringAssert.DoesNotContain("smoothstep(0.0, 0.25, clampedLight)", lightingSource);
            StringAssert.DoesNotContain("smoothstep(0.25, 0.5, clampedLight)", lightingSource);
            StringAssert.DoesNotContain("smoothstep(0.5, 0.75, clampedLight)", lightingSource);
            StringAssert.DoesNotContain("smoothstep(0.75, 1.0, clampedLight)", lightingSource);

            StringAssert.Contains("ESL_EvaluateStylizedDiffuse", stylizedDiffuseSource);
            StringAssert.Contains("diffuseData.wrappedLight", stylizedDiffuseSource);
            StringAssert.Contains("diffuseData.steppedLight", stylizedDiffuseSource);
            StringAssert.Contains("diffuseData.mainLightAttenuation", stylizedDiffuseSource);
            StringAssert.Contains("ESL_EvaluateSpecularLighting(inputData, mainLight, diffuseData.shadowAttenuation, diffuseData.mainLightAttenuation)", stylizedDiffuseSource);

            StringAssert.Contains("ESL_ApplyDebugView", debugSource);
            StringAssert.Contains("ESL_DEBUG_STEPPED_LIGHT", debugSource);

            StringAssert.Contains("ESL_EvaluateStylizedDiffuse", forwardLitSource);
            StringAssert.Contains("ESL_ApplyDebugView", forwardLitSource);
            StringAssert.DoesNotContain("GetMainLight(", forwardLitSource);
        }

        private static void AssertShaderHasProperty(Shader shader, string propertyName)
        {
            int propertyCount = shader.GetPropertyCount();

            for (int index = 0; index < propertyCount; index++)
            {
                if (shader.GetPropertyName(index) == propertyName)
                {
                    return;
                }
            }

            Assert.Fail($"Expected shader property: {propertyName}");
        }

        private static void AssertSceneContainsDirectionalLight(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid(), $"Scene must open successfully: {scenePath}");

            foreach (Light light in Object.FindObjectsByType<Light>())
            {
                if (light.type == LightType.Directional)
                {
                    return;
                }
            }

            Assert.Fail($"Expected a directional main light in validation scene: {scenePath}");
        }

        private static void AssertColorApproximately(Color expected, Color actual)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(1e-4f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(1e-4f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(1e-4f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(1e-4f));
        }
    }
}