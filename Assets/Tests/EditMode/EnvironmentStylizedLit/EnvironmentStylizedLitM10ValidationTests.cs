using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BC.Rendering.Tests
{
    public sealed class EnvironmentStylizedLitM10ValidationTests
    {
        private const string ShaderPath = "Assets/Art/Shader/EnvironmentStylizedLit/EnvironmentStylizedLit.shader";
        private const string LightingPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_Lighting.hlsl";
        private const string StylizedDiffusePath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_StylizedDiffuse.hlsl";
        private const string ForwardLitPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/Passes/EnvironmentStylizedLit_ForwardLitPass.hlsl";
        private const string TestRoomScenePath = "Assets/Scenes/EnvironmentStylizedLit/ESL_TestRoom.unity";
        private const string LightingLabScenePath = "Assets/Scenes/EnvironmentStylizedLit/ESL_LightingLab.unity";
        private const string AdditionalOffMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_AdditionalOff.mat";
        private const string AdditionalFillOnlyMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_AdditionalFillOnly.mat";
        private const string AdditionalQuantizedMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_AdditionalQuantized.mat";
        private const string AdditionalContinuousMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_AdditionalContinuous.mat";

        [Test]
        public void M10ValidationAssetsExist()
        {
            Assert.IsTrue(File.Exists(ShaderPath), $"Expected EnvironmentStylizedLit shader asset: {ShaderPath}");
            Assert.IsTrue(File.Exists(LightingPath), $"Expected lighting source: {LightingPath}");
            Assert.IsTrue(File.Exists(StylizedDiffusePath), $"Expected stylized diffuse source: {StylizedDiffusePath}");
            Assert.IsTrue(File.Exists(ForwardLitPath), $"Expected forward pass source: {ForwardLitPath}");
            Assert.IsTrue(File.Exists(TestRoomScenePath), $"Expected M10 validation scene: {TestRoomScenePath}");
            Assert.IsTrue(File.Exists(LightingLabScenePath), $"Expected M10 validation scene: {LightingLabScenePath}");
            Assert.IsTrue(File.Exists(AdditionalOffMaterialPath), $"Expected M10 validation material: {AdditionalOffMaterialPath}");
            Assert.IsTrue(File.Exists(AdditionalFillOnlyMaterialPath), $"Expected M10 validation material: {AdditionalFillOnlyMaterialPath}");
            Assert.IsTrue(File.Exists(AdditionalQuantizedMaterialPath), $"Expected M10 validation material: {AdditionalQuantizedMaterialPath}");
            Assert.IsTrue(File.Exists(AdditionalContinuousMaterialPath), $"Expected M10 validation material: {AdditionalContinuousMaterialPath}");
        }

        [Test]
        public void M10ShaderDeclaresAdditionalLightProperties()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Material material = new Material(shader);

            try
            {
                Assert.IsNotNull(shader, "EnvironmentStylizedLit shader asset must load.");
                AssertShaderHasProperty(shader, "_AdditionalLightMode");
                AssertShaderHasProperty(shader, "_AdditionalLightIntensity");
                AssertShaderHasProperty(shader, "_AdditionalLightShadowInfluence");
                AssertShaderHasProperty(shader, "_AdditionalLightColorInfluence");

                material.SetFloat("_AdditionalLightMode", 2f);
                material.SetFloat("_AdditionalLightIntensity", 0.4f);
                material.SetFloat("_AdditionalLightShadowInfluence", 0.7f);
                material.SetFloat("_AdditionalLightColorInfluence", 0.8f);

                Assert.AreEqual(2f, material.GetFloat("_AdditionalLightMode"));
                Assert.AreEqual(0.4f, material.GetFloat("_AdditionalLightIntensity"));
                Assert.AreEqual(0.7f, material.GetFloat("_AdditionalLightShadowInfluence"));
                Assert.AreEqual(0.8f, material.GetFloat("_AdditionalLightColorInfluence"));
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void M10SourceFilesKeepAdditionalLightResponsibilitiesSeparated()
        {
            string shaderSource = File.ReadAllText(ShaderPath);
            string lightingSource = File.ReadAllText(LightingPath);
            string stylizedDiffuseSource = File.ReadAllText(StylizedDiffusePath);
            string forwardLitSource = File.ReadAllText(ForwardLitPath);

            StringAssert.Contains("#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS", shaderSource);
            StringAssert.Contains("#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS", shaderSource);
            StringAssert.Contains("#pragma multi_compile _ _LIGHT_LAYERS", shaderSource);
            StringAssert.Contains("#pragma multi_compile _ _CLUSTER_LIGHT_LOOP", shaderSource);
            StringAssert.Contains("#pragma instancing_options renderinglayer", shaderSource);

            StringAssert.Contains("ESL_ADDITIONAL_LIGHT_MODE_FILL_ONLY", lightingSource);
            StringAssert.Contains("ESL_EvaluateAdditionalLighting", lightingSource);
            StringAssert.Contains("ESL_EvaluateAdditionalDominanceMask", lightingSource);
            StringAssert.Contains("ESL_AccumulateAdditionalLight", lightingSource);
            StringAssert.Contains("GetAdditionalLight(", lightingSource);
            StringAssert.Contains("LIGHT_LOOP_BEGIN", lightingSource);
            StringAssert.Contains("inputData.vertexLighting", lightingSource);
            StringAssert.Contains("_AdditionalLightIntensity", lightingSource);
            StringAssert.Contains("_AdditionalLightShadowInfluence", lightingSource);
            StringAssert.Contains("_AdditionalLightColorInfluence", lightingSource);
            StringAssert.DoesNotContain("VertexLighting(", lightingSource);
            StringAssert.DoesNotContain("fillColor += contribution", lightingSource);
            StringAssert.DoesNotContain("quantizedColor += contribution", lightingSource);
            StringAssert.DoesNotContain("continuousColor += contribution", lightingSource);

            StringAssert.Contains("additionalLightingData", stylizedDiffuseSource);
            StringAssert.Contains("diffuseData.additionalLightColor", stylizedDiffuseSource);
            StringAssert.Contains("ESL_EvaluateAdditionalLighting(inputData", stylizedDiffuseSource);

            StringAssert.DoesNotContain("GetAdditionalLight(", forwardLitSource);
            StringAssert.DoesNotContain("LIGHT_LOOP_BEGIN", forwardLitSource);
            StringAssert.Contains("fogFactorAndVertexLight", forwardLitSource);
            StringAssert.Contains("VertexLighting(positionInputs.positionWS, normalInputs.normalWS)", forwardLitSource);
        }

        [Test]
        public void M10ValidationAssetsContainAdditionalLightAnchors()
        {
            Material additionalOffMaterial = AssetDatabase.LoadAssetAtPath<Material>(AdditionalOffMaterialPath);
            Material additionalFillOnlyMaterial = AssetDatabase.LoadAssetAtPath<Material>(AdditionalFillOnlyMaterialPath);
            Material additionalQuantizedMaterial = AssetDatabase.LoadAssetAtPath<Material>(AdditionalQuantizedMaterialPath);
            Material additionalContinuousMaterial = AssetDatabase.LoadAssetAtPath<Material>(AdditionalContinuousMaterialPath);

            Assert.IsNotNull(additionalOffMaterial, "Expected M10 off material asset.");
            Assert.IsNotNull(additionalFillOnlyMaterial, "Expected M10 fill-only material asset.");
            Assert.IsNotNull(additionalQuantizedMaterial, "Expected M10 quantized material asset.");
            Assert.IsNotNull(additionalContinuousMaterial, "Expected M10 continuous material asset.");

            Scene testRoomScene = EditorSceneManager.OpenScene(TestRoomScenePath, OpenSceneMode.Single);
            Assert.IsTrue(testRoomScene.IsValid(), "ESL_TestRoom must open successfully.");
            Assert.IsNotNull(GameObject.Find("M10AdditionalLightViewpoint"), "Expected M10 viewpoint marker.");
            AssertRendererUsesMaterial("M10AdditionalOffSphere_Room", additionalOffMaterial);
            AssertRendererUsesMaterial("M10FillOnlySphere_Room", additionalFillOnlyMaterial);
            AssertRendererUsesMaterial("M10QuantizedSphere_Room", additionalQuantizedMaterial);
            AssertRendererUsesMaterial("M10ContinuousSphere_Room", additionalContinuousMaterial);
            AssertLight("M10PointLight", LightType.Point);
            AssertLight("M10SpotLight", LightType.Spot);
            Assert.AreEqual(0f, additionalOffMaterial.GetFloat("_AdditionalLightMode"));
            Assert.AreEqual(1f, additionalFillOnlyMaterial.GetFloat("_AdditionalLightMode"));
            Assert.AreEqual(2f, additionalQuantizedMaterial.GetFloat("_AdditionalLightMode"));
            Assert.AreEqual(3f, additionalContinuousMaterial.GetFloat("_AdditionalLightMode"));
            Assert.Greater(additionalContinuousMaterial.GetFloat("_AdditionalLightColorInfluence"), additionalFillOnlyMaterial.GetFloat("_AdditionalLightColorInfluence"));

            Scene lightingLabScene = EditorSceneManager.OpenScene(LightingLabScenePath, OpenSceneMode.Single);
            Assert.IsTrue(lightingLabScene.IsValid(), "ESL_LightingLab must open successfully.");
            Assert.IsNotNull(GameObject.Find("M10LightingLabViewpoint"), "Expected M10 lighting-lab viewpoint marker.");
            AssertRendererUsesMaterial("M10AdditionalOffSphere_Lab", additionalOffMaterial);
            AssertRendererUsesMaterial("M10FillOnlySphere_Lab", additionalFillOnlyMaterial);
            AssertRendererUsesMaterial("M10QuantizedSphere_Lab", additionalQuantizedMaterial);
            AssertRendererUsesMaterial("M10ContinuousSphere_Lab", additionalContinuousMaterial);
            AssertLight("M10PointLight_Lab", LightType.Point);
            AssertLight("M10SpotLight_Lab", LightType.Spot);
            AssertHorizontalSpacing("M10AdditionalOffSphere_Lab", "M10FillOnlySphere_Lab", 3.2f);
            AssertHorizontalSpacing("M10FillOnlySphere_Lab", "M10QuantizedSphere_Lab", 3.2f);
            AssertHorizontalSpacing("M10QuantizedSphere_Lab", "M10ContinuousSphere_Lab", 3.2f);
        }

        private static void AssertShaderHasProperty(Shader shader, string propertyName)
        {
            int propertyCount = ShaderUtil.GetPropertyCount(shader);

            for (int index = 0; index < propertyCount; index++)
            {
                if (ShaderUtil.GetPropertyName(shader, index) == propertyName)
                {
                    return;
                }
            }

            Assert.Fail($"Expected shader property: {propertyName}");
        }

        private static void AssertRendererUsesMaterial(string objectName, Material expectedMaterial)
        {
            GameObject targetObject = GameObject.Find(objectName);
            Assert.IsNotNull(targetObject, $"Expected validation marker object: {objectName}");

            Renderer renderer = targetObject.GetComponent<Renderer>();
            Assert.IsNotNull(renderer, $"Expected renderer on validation marker object: {objectName}");
            Assert.AreSame(expectedMaterial, renderer.sharedMaterial, $"Unexpected material on validation marker object: {objectName}");
        }

        private static void AssertLight(string objectName, LightType expectedType)
        {
            GameObject lightObject = GameObject.Find(objectName);
            Assert.IsNotNull(lightObject, $"Expected validation light object: {objectName}");

            Light light = lightObject.GetComponent<Light>();
            Assert.IsNotNull(light, $"Expected Light component on validation light object: {objectName}");
            Assert.AreEqual(expectedType, light.type);
            Assert.AreNotEqual(LightShadows.None, light.shadows);
            Assert.Greater(light.range, 0f);
            Assert.Greater(light.intensity, 0f);
        }

        private static void AssertHorizontalSpacing(string leftObjectName, string rightObjectName, float expectedSpacing)
        {
            GameObject leftObject = GameObject.Find(leftObjectName);
            GameObject rightObject = GameObject.Find(rightObjectName);

            Assert.IsNotNull(leftObject, $"Expected validation marker object: {leftObjectName}");
            Assert.IsNotNull(rightObject, $"Expected validation marker object: {rightObjectName}");
            Assert.That(rightObject.transform.position.x - leftObject.transform.position.x, Is.EqualTo(expectedSpacing).Within(1e-4f));
        }
    }
}