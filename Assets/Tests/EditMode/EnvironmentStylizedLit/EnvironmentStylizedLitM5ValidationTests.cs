using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BC.Rendering.Tests
{
    public sealed class EnvironmentStylizedLitM5ValidationTests
    {
        private const string ShaderPath = "Assets/Art/Shader/EnvironmentStylizedLit/EnvironmentStylizedLit.shader";
        private const string AmbientPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_Ambient.hlsl";
        private const string LightingPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_Lighting.hlsl";
        private const string StylizedDiffusePath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_StylizedDiffuse.hlsl";
        private const string ForwardLitPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/Passes/EnvironmentStylizedLit_ForwardLitPass.hlsl";
        private const string TestRoomScenePath = "Assets/Scenes/EnvironmentStylizedLit/ESL_TestRoom.unity";
        private const string LightingLabScenePath = "Assets/Scenes/EnvironmentStylizedLit/ESL_LightingLab.unity";
        private const string AmbientRoomMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_AmbientRoom.mat";
        private const string AmbientBounceMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_AmbientBounce.mat";

        [Test]
        public void M5ShaderDeclaresAmbientAndBounceProperties()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);

            Assert.IsNotNull(shader, "EnvironmentStylizedLit shader asset must load.");
            AssertShaderHasProperty(shader, "_AmbientTopColor");
            AssertShaderHasProperty(shader, "_AmbientSideColor");
            AssertShaderHasProperty(shader, "_AmbientBottomColor");
            AssertShaderHasProperty(shader, "_AmbientStrength");
            AssertShaderHasProperty(shader, "_BounceColor");
            AssertShaderHasProperty(shader, "_BounceStrength");
            AssertShaderHasProperty(shader, "_BounceDirection");
            AssertShaderHasProperty(shader, "_BounceWrap");
            AssertShaderHasProperty(shader, "_IndirectShadowColor");
        }

        [Test]
        public void M5TemporaryMaterialSupportsAmbientAndBounceOverrides()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Material material = new Material(shader);
            Vector4 expectedBounceDirection = new Vector4(0f, 1f, 0f, 0f);

            try
            {
                material.SetColor("_AmbientTopColor", new Color(0.6f, 0.7f, 0.8f, 1f));
                material.SetColor("_AmbientSideColor", new Color(0.4f, 0.45f, 0.5f, 1f));
                material.SetColor("_AmbientBottomColor", new Color(0.2f, 0.18f, 0.16f, 1f));
                material.SetFloat("_AmbientStrength", 0.55f);
                material.SetColor("_BounceColor", new Color(0.95f, 0.78f, 0.6f, 1f));
                material.SetFloat("_BounceStrength", 0.3f);
                material.SetVector("_BounceDirection", expectedBounceDirection);
                material.SetFloat("_BounceWrap", 0.5f);
                material.SetColor("_IndirectShadowColor", new Color(0.74f, 0.8f, 0.9f, 1f));

                Assert.AreEqual(0.55f, material.GetFloat("_AmbientStrength"));
                Assert.AreEqual(0.3f, material.GetFloat("_BounceStrength"));
                Assert.AreEqual(0.5f, material.GetFloat("_BounceWrap"));
                Assert.That(material.GetVector("_BounceDirection"), Is.EqualTo(expectedBounceDirection));
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void M5SourceFilesKeepAmbientAndBounceResponsibilitiesSeparated()
        {
            string ambientSource = File.ReadAllText(AmbientPath);
            string lightingSource = File.ReadAllText(LightingPath);
            string stylizedDiffuseSource = File.ReadAllText(StylizedDiffusePath);
            string forwardLitSource = File.ReadAllText(ForwardLitPath);

            StringAssert.Contains("ESL_EvaluateAmbientData", ambientSource);
            StringAssert.Contains("ESL_EvaluateDirectionalAmbient", ambientSource);
            StringAssert.Contains("ESL_EvaluateBounceFactor", ambientSource);

            StringAssert.Contains("ESL_EvaluateIndirectLighting", lightingSource);
            StringAssert.Contains("ESL_EvaluateIndirectMask", lightingSource);
            StringAssert.Contains("ESL_AmbientData", lightingSource);

            StringAssert.Contains("indirectLightingData", stylizedDiffuseSource);
            StringAssert.Contains("diffuseData.ambientColor", stylizedDiffuseSource);
            StringAssert.Contains("diffuseData.indirectColor", stylizedDiffuseSource);

            StringAssert.DoesNotContain("_AmbientTopColor", forwardLitSource);
            StringAssert.DoesNotContain("ESL_EvaluateAmbientData", forwardLitSource);
            StringAssert.DoesNotContain("GetMainLight(", forwardLitSource);
        }

        [Test]
        public void M5ValidationAssetsContainAmbientAndBounceAnchors()
        {
            Material ambientRoomMaterial = AssetDatabase.LoadAssetAtPath<Material>(AmbientRoomMaterialPath);
            Material ambientBounceMaterial = AssetDatabase.LoadAssetAtPath<Material>(AmbientBounceMaterialPath);

            Assert.IsNotNull(ambientRoomMaterial, "Expected M5 ambient room material asset.");
            Assert.IsNotNull(ambientBounceMaterial, "Expected M5 ambient bounce material asset.");

            Scene testRoomScene = EditorSceneManager.OpenScene(TestRoomScenePath, OpenSceneMode.Single);

            Assert.IsTrue(testRoomScene.IsValid(), "ESL_TestRoom must open successfully.");
            AssertRendererUsesMaterial("M5AmbientRoom", ambientRoomMaterial);
            AssertRendererUsesMaterial("M5BounceProbeSphere", ambientBounceMaterial);
            AssertRendererUsesMaterial("M5AmbientReferenceCube", ambientBounceMaterial);
            AssertViewpointInsideRoom("M5AmbientRoomViewpoint", "M5AmbientRoom");
            Assert.Greater(ambientRoomMaterial.GetFloat("_AmbientStrength"), 0f);
            Assert.Greater(ambientRoomMaterial.GetFloat("_BounceStrength"), 0f);

            Scene lightingLabScene = EditorSceneManager.OpenScene(LightingLabScenePath, OpenSceneMode.Single);

            Assert.IsTrue(lightingLabScene.IsValid(), "ESL_LightingLab must open successfully.");
            AssertRendererUsesMaterial("M5AmbientTopQuad", ambientBounceMaterial);
            AssertRendererUsesMaterial("M5AmbientSideQuad", ambientBounceMaterial);
            AssertRendererUsesMaterial("M5AmbientBottomQuad", ambientBounceMaterial);
            AssertRendererUsesMaterial("M5BounceFacingQuad", ambientBounceMaterial);
            AssertRendererUsesMaterial("M5BounceOpposingQuad", ambientBounceMaterial);
            AssertQuadNormalY("M5AmbientTopQuad", 1f);
            AssertQuadNormalY("M5AmbientSideQuad", 0f);
            AssertQuadNormalY("M5AmbientBottomQuad", -1f);
            AssertQuadNormalY("M5BounceFacingQuad", 1f);
            AssertQuadNormalY("M5BounceOpposingQuad", -1f);
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

        private static void AssertRendererUsesMaterial(string objectName, Material expectedMaterial)
        {
            GameObject targetObject = GameObject.Find(objectName);
            Assert.IsNotNull(targetObject, $"Expected validation marker object: {objectName}");

            Renderer renderer = targetObject.GetComponent<Renderer>();
            Assert.IsNotNull(renderer, $"Expected renderer on validation marker object: {objectName}");
            Assert.AreSame(expectedMaterial, renderer.sharedMaterial, $"Unexpected material on validation marker object: {objectName}");
        }

        private static void AssertViewpointInsideRoom(string viewpointObjectName, string roomObjectName)
        {
            GameObject viewpointObject = GameObject.Find(viewpointObjectName);
            GameObject roomObject = GameObject.Find(roomObjectName);

            Assert.IsNotNull(viewpointObject, $"Expected viewpoint marker object: {viewpointObjectName}");
            Assert.IsNotNull(roomObject, $"Expected room object: {roomObjectName}");

            Renderer renderer = roomObject.GetComponent<Renderer>();
            Assert.IsNotNull(renderer, $"Expected renderer on room object: {roomObjectName}");
            Assert.IsTrue(renderer.bounds.Contains(viewpointObject.transform.position), $"Expected {viewpointObjectName} to remain inside {roomObjectName}.");
        }

        private static void AssertQuadNormalY(string objectName, float expectedY)
        {
            GameObject targetObject = GameObject.Find(objectName);
            Assert.IsNotNull(targetObject, $"Expected validation marker object: {objectName}");

            float actualY = targetObject.transform.forward.y;
            Assert.That(actualY, Is.EqualTo(expectedY).Within(1e-4f), $"Unexpected quad normal orientation: {objectName}");
        }
    }
}