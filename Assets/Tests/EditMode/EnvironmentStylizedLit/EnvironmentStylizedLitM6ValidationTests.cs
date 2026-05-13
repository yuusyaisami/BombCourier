using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BC.Rendering.Tests
{
    public sealed class EnvironmentStylizedLitM6ValidationTests
    {
        private const string ShaderPath = "Assets/Art/Shader/EnvironmentStylizedLit/EnvironmentStylizedLit.shader";
        private const string SpecularPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_Specular.hlsl";
        private const string LightingPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_Lighting.hlsl";
        private const string StylizedDiffusePath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_StylizedDiffuse.hlsl";
        private const string ForwardLitPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/Passes/EnvironmentStylizedLit_ForwardLitPass.hlsl";
        private const string LightingLabScenePath = "Assets/Scenes/EnvironmentStylizedLit/ESL_LightingLab.unity";
        private const string TestRoomScenePath = "Assets/Scenes/EnvironmentStylizedLit/ESL_TestRoom.unity";
        private const string SoftSpecularMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_SoftSpecular.mat";
        private const string QuantizedMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_Quantized.mat";
        private const string CeramicMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_Ceramic.mat";
        private const string PlasticMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_Plastic.mat";
        private const string EdgeSheenMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_EdgeSheen.mat";

        [Test]
        public void M6ValidationAssetsExist()
        {
            Assert.IsTrue(File.Exists(ShaderPath), $"Expected EnvironmentStylizedLit shader asset: {ShaderPath}");
            Assert.IsTrue(File.Exists(LightingLabScenePath), $"Expected M6 lighting validation scene: {LightingLabScenePath}");
            Assert.IsTrue(File.Exists(TestRoomScenePath), $"Expected M6 edge sheen validation scene: {TestRoomScenePath}");
            Assert.IsTrue(File.Exists(SoftSpecularMaterialPath), $"Expected M6 validation material: {SoftSpecularMaterialPath}");
            Assert.IsTrue(File.Exists(QuantizedMaterialPath), $"Expected M6 validation material: {QuantizedMaterialPath}");
            Assert.IsTrue(File.Exists(CeramicMaterialPath), $"Expected M6 validation material: {CeramicMaterialPath}");
            Assert.IsTrue(File.Exists(PlasticMaterialPath), $"Expected M6 validation material: {PlasticMaterialPath}");
            Assert.IsTrue(File.Exists(EdgeSheenMaterialPath), $"Expected M6 validation material: {EdgeSheenMaterialPath}");
        }

        [Test]
        public void M6ShaderDeclaresSpecularAndEdgeSheenProperties()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);

            Assert.IsNotNull(shader, "EnvironmentStylizedLit shader asset must load.");
            AssertShaderHasProperty(shader, "_SpecularMode");
            AssertShaderHasProperty(shader, "_SpecularStrength");
            AssertShaderHasProperty(shader, "_SpecularStepCount");
            AssertShaderHasProperty(shader, "_SpecularStepSmoothness");
            AssertShaderHasProperty(shader, "_EdgeSheenStrength");
            AssertShaderHasProperty(shader, "_EdgeSheenPower");
            AssertShaderHasProperty(shader, "_EdgeSheenColor");
        }

        [Test]
        public void M6TemporaryMaterialSupportsSpecularAndEdgeSheenOverrides()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Material material = new Material(shader);

            try
            {
                material.SetFloat("_SpecularMode", 2f);
                material.SetFloat("_SpecularStrength", 0.42f);
                material.SetFloat("_Smoothness", 0.61f);
                material.SetFloat("_SpecularStepCount", 4f);
                material.SetFloat("_SpecularStepSmoothness", 0.16f);
                material.SetColor("_SpecularColor", new Color(0.88f, 0.81f, 0.74f, 1f));
                material.SetFloat("_EdgeSheenStrength", 0.24f);
                material.SetFloat("_EdgeSheenPower", 3.2f);
                material.SetColor("_EdgeSheenColor", new Color(1f, 0.95f, 0.9f, 1f));

                Assert.AreEqual(2f, material.GetFloat("_SpecularMode"));
                Assert.AreEqual(0.42f, material.GetFloat("_SpecularStrength"));
                Assert.AreEqual(0.61f, material.GetFloat("_Smoothness"));
                Assert.AreEqual(4f, material.GetFloat("_SpecularStepCount"));
                Assert.AreEqual(0.16f, material.GetFloat("_SpecularStepSmoothness"));
                Assert.AreEqual(0.24f, material.GetFloat("_EdgeSheenStrength"));
                Assert.AreEqual(3.2f, material.GetFloat("_EdgeSheenPower"));
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void M6SourceFilesKeepSpecularResponsibilitiesSeparated()
        {
            string specularSource = File.ReadAllText(SpecularPath);
            string lightingSource = File.ReadAllText(LightingPath);
            string stylizedDiffuseSource = File.ReadAllText(StylizedDiffusePath);
            string forwardLitSource = File.ReadAllText(ForwardLitPath);

            StringAssert.Contains("ESL_EvaluateSpecularData", specularSource);
            StringAssert.Contains("ESL_EvaluateEdgeSheenTerm", specularSource);
            StringAssert.Contains("ESL_SPECULAR_MODE_QUANTIZED", specularSource);
            StringAssert.Contains("ESL_SPECULAR_MODE_PLASTIC", specularSource);
            StringAssert.Contains("lightFacing", specularSource);
            StringAssert.Contains("shadowAttenuation", specularSource);

            StringAssert.Contains("EnvironmentStylizedLit_Specular.hlsl", lightingSource);
            StringAssert.Contains("ESL_EvaluateSpecularLighting", lightingSource);

            StringAssert.Contains("specularData.combinedSpecular", stylizedDiffuseSource);
            StringAssert.Contains("diffuseData.edgeSheenColor", stylizedDiffuseSource);

            StringAssert.DoesNotContain("_SpecularStrength", forwardLitSource);
            StringAssert.DoesNotContain("halfDirectionWS", forwardLitSource);
            StringAssert.DoesNotContain("ESL_EvaluateSpecularLighting", forwardLitSource);
        }

        [Test]
        public void M6ValidationAssetsContainSpecularAndEdgeSheenAnchors()
        {
            Material softSpecularMaterial = AssetDatabase.LoadAssetAtPath<Material>(SoftSpecularMaterialPath);
            Material quantizedMaterial = AssetDatabase.LoadAssetAtPath<Material>(QuantizedMaterialPath);
            Material ceramicMaterial = AssetDatabase.LoadAssetAtPath<Material>(CeramicMaterialPath);
            Material plasticMaterial = AssetDatabase.LoadAssetAtPath<Material>(PlasticMaterialPath);
            Material edgeSheenMaterial = AssetDatabase.LoadAssetAtPath<Material>(EdgeSheenMaterialPath);

            Assert.IsNotNull(softSpecularMaterial, "Expected M6 soft specular material asset.");
            Assert.IsNotNull(quantizedMaterial, "Expected M6 quantized material asset.");
            Assert.IsNotNull(ceramicMaterial, "Expected M6 ceramic material asset.");
            Assert.IsNotNull(plasticMaterial, "Expected M6 plastic material asset.");
            Assert.IsNotNull(edgeSheenMaterial, "Expected M6 edge sheen material asset.");

            Scene lightingLabScene = EditorSceneManager.OpenScene(LightingLabScenePath, OpenSceneMode.Single);

            Assert.IsTrue(lightingLabScene.IsValid(), "ESL_LightingLab must open successfully.");
            AssertRendererUsesMaterial("M6SoftSpecularSphere", softSpecularMaterial);
            AssertRendererUsesMaterial("M6QuantizedSpecularSphere", quantizedMaterial);
            AssertRendererUsesMaterial("M6CeramicSpecularSphere", ceramicMaterial);
            AssertRendererUsesMaterial("M6PlasticSpecularSphere", plasticMaterial);
            Assert.AreEqual(1f, softSpecularMaterial.GetFloat("_SpecularMode"));
            Assert.AreEqual(2f, quantizedMaterial.GetFloat("_SpecularMode"));
            Assert.AreEqual(3f, ceramicMaterial.GetFloat("_SpecularMode"));
            Assert.AreEqual(4f, plasticMaterial.GetFloat("_SpecularMode"));
            Assert.AreEqual(4f, quantizedMaterial.GetFloat("_SpecularStepCount"));
            AssertSpherePosition("M6SoftSpecularSphere", new Vector3(-4.8f, 0.95f, 8.2f));
            AssertSpherePosition("M6QuantizedSpecularSphere", new Vector3(-1.6f, 0.95f, 8.2f));
            AssertSpherePosition("M6CeramicSpecularSphere", new Vector3(1.6f, 0.95f, 8.2f));
            AssertSpherePosition("M6PlasticSpecularSphere", new Vector3(4.8f, 0.95f, 8.2f));
            AssertHorizontalSpacing("M6SoftSpecularSphere", "M6QuantizedSpecularSphere", 3.2f);
            AssertHorizontalSpacing("M6QuantizedSpecularSphere", "M6CeramicSpecularSphere", 3.2f);
            AssertHorizontalSpacing("M6CeramicSpecularSphere", "M6PlasticSpecularSphere", 3.2f);

            Scene testRoomScene = EditorSceneManager.OpenScene(TestRoomScenePath, OpenSceneMode.Single);

            Assert.IsTrue(testRoomScene.IsValid(), "ESL_TestRoom must open successfully.");
            AssertRendererUsesMaterial("M6EdgeSheenQuad", edgeSheenMaterial);
            Assert.AreEqual(0f, edgeSheenMaterial.GetFloat("_SpecularMode"));
            Assert.AreEqual(0f, edgeSheenMaterial.GetFloat("_SpecularStrength"));
            Assert.Greater(edgeSheenMaterial.GetFloat("_EdgeSheenStrength"), 0f);
            AssertQuadRotationY("M6EdgeSheenQuad", 75f);
            AssertGrazingView("M6EdgeSheenViewpoint", "M6EdgeSheenQuad", 0.35f);
            AssertEdgeSheenSceneUsesFrontLight("M6EdgeSheenQuad", "Directional Light", 0.2f);
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

            string expectedMaterialPath = AssetDatabase.GetAssetPath(expectedMaterial);
            string actualMaterialPath = AssetDatabase.GetAssetPath(renderer.sharedMaterial);
            Assert.AreEqual(expectedMaterialPath, actualMaterialPath, $"Unexpected material on validation marker object: {objectName}");
        }

        private static void AssertGrazingView(string viewpointObjectName, string targetObjectName, float maxAbsDot)
        {
            GameObject viewpointObject = GameObject.Find(viewpointObjectName);
            GameObject targetObject = GameObject.Find(targetObjectName);

            Assert.IsNotNull(viewpointObject, $"Expected viewpoint marker object: {viewpointObjectName}");
            Assert.IsNotNull(targetObject, $"Expected target object: {targetObjectName}");

            Vector3 viewDirection = (targetObject.transform.position - viewpointObject.transform.position).normalized;
            float absoluteDot = Mathf.Abs(Vector3.Dot(targetObject.transform.forward.normalized, viewDirection));
            Assert.LessOrEqual(absoluteDot, maxAbsDot, $"Expected grazing view for {targetObjectName}, but dot was {absoluteDot}.");
        }

        private static void AssertSpherePosition(string objectName, Vector3 expectedPosition)
        {
            GameObject targetObject = GameObject.Find(objectName);
            Assert.IsNotNull(targetObject, $"Expected validation marker object: {objectName}");

            Vector3 actualPosition = targetObject.transform.position;
            Assert.That(actualPosition.x, Is.EqualTo(expectedPosition.x).Within(1e-4f));
            Assert.That(actualPosition.y, Is.EqualTo(expectedPosition.y).Within(1e-4f));
            Assert.That(actualPosition.z, Is.EqualTo(expectedPosition.z).Within(1e-4f));
        }

        private static void AssertHorizontalSpacing(string leftObjectName, string rightObjectName, float expectedSpacing)
        {
            GameObject leftObject = GameObject.Find(leftObjectName);
            GameObject rightObject = GameObject.Find(rightObjectName);

            Assert.IsNotNull(leftObject, $"Expected validation marker object: {leftObjectName}");
            Assert.IsNotNull(rightObject, $"Expected validation marker object: {rightObjectName}");
            Assert.That(rightObject.transform.position.x - leftObject.transform.position.x, Is.EqualTo(expectedSpacing).Within(1e-4f));
        }

        private static void AssertQuadRotationY(string objectName, float expectedYDegrees)
        {
            GameObject targetObject = GameObject.Find(objectName);
            Assert.IsNotNull(targetObject, $"Expected validation marker object: {objectName}");
            Assert.That(targetObject.transform.eulerAngles.y, Is.EqualTo(expectedYDegrees).Within(1e-3f));
        }

        private static void AssertEdgeSheenSceneUsesFrontLight(string targetObjectName, string lightObjectName, float minimumWrappedFacing)
        {
            GameObject targetObject = GameObject.Find(targetObjectName);
            GameObject lightObject = GameObject.Find(lightObjectName);

            Assert.IsNotNull(targetObject, $"Expected validation marker object: {targetObjectName}");
            Assert.IsNotNull(lightObject, $"Expected light object: {lightObjectName}");

            Light light = lightObject.GetComponent<Light>();
            Assert.IsNotNull(light, $"Expected Light component on: {lightObjectName}");
            Assert.AreEqual(LightType.Directional, light.type);

            Vector3 normalWS = targetObject.transform.forward.normalized;
            Vector3 lightDirectionWS = -lightObject.transform.forward.normalized;
            float wrappedFacing = Mathf.Clamp01((Vector3.Dot(normalWS, lightDirectionWS) + 0.45f) / 1.45f);
            Assert.GreaterOrEqual(wrappedFacing, minimumWrappedFacing, $"Expected {targetObjectName} to remain light-facing enough for edge sheen validation.");
        }
    }
}