using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BC.Rendering.Tests
{
    public sealed class EnvironmentStylizedLitM4ValidationTests
    {
        private const string ShaderPath = "Assets/Art/Shader/EnvironmentStylizedLit/EnvironmentStylizedLit.shader";
        private const string LightingPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_Lighting.hlsl";
        private const string SamplingPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_Sampling.hlsl";
        private const string SurfacePath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_Surface.hlsl";
        private const string ForwardLitPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/Passes/EnvironmentStylizedLit_ForwardLitPass.hlsl";
        private const string ShadowCasterPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/Passes/EnvironmentStylizedLit_ShadowCasterPass.hlsl";
        private const string TestRoomScenePath = "Assets/Scenes/EnvironmentStylizedLit/ESL_TestRoom.unity";
        private const string DefaultMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_Default.mat";
        private const string InteriorMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_Interior.mat";
        private const string RoomMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_Room.mat";
        private const string DoubleSidedMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_DoubleSided.mat";

        [Test]
        public void M4ValidationAssetsExist()
        {
            Assert.IsTrue(File.Exists(ShaderPath), $"Expected EnvironmentStylizedLit shader asset: {ShaderPath}");
            Assert.IsTrue(File.Exists(TestRoomScenePath), $"Expected M4 validation scene: {TestRoomScenePath}");
            Assert.IsTrue(File.Exists(DefaultMaterialPath), $"Expected M4 validation material: {DefaultMaterialPath}");
            Assert.IsTrue(File.Exists(InteriorMaterialPath), $"Expected M4 validation material: {InteriorMaterialPath}");
            Assert.IsTrue(File.Exists(RoomMaterialPath), $"Expected M4 validation material: {RoomMaterialPath}");
            Assert.IsTrue(File.Exists(DoubleSidedMaterialPath), $"Expected M4 validation material: {DoubleSidedMaterialPath}");
        }

        [Test]
        public void M4ShaderDeclaresShadowPropertiesAndShadowCasterPass()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            string shaderSource = File.ReadAllText(ShaderPath);

            Assert.IsNotNull(shader, "EnvironmentStylizedLit shader asset must load.");
            AssertShaderHasProperty(shader, "_ShadowInfluence");
            AssertShaderHasProperty(shader, "_ShadowSoftFill");
            AssertShaderHasProperty(shader, "_ShadowColorBlend");
            StringAssert.Contains("Name \"ShadowCaster\"", shaderSource);
            StringAssert.Contains("\"LightMode\" = \"ShadowCaster\"", shaderSource);
        }

        [Test]
        public void M4TemporaryMaterialSupportsShadowOverrides()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Material material = new Material(shader);

            try
            {
                material.SetFloat("_ShadowInfluence", 0.65f);
                material.SetFloat("_ShadowSoftFill", 0.25f);
                material.SetFloat("_ShadowColorBlend", 0.8f);

                Assert.AreEqual(0.65f, material.GetFloat("_ShadowInfluence"));
                Assert.AreEqual(0.25f, material.GetFloat("_ShadowSoftFill"));
                Assert.AreEqual(0.8f, material.GetFloat("_ShadowColorBlend"));
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void M4SourceFilesKeepShadowResponsibilitiesSeparated()
        {
            string lightingSource = File.ReadAllText(LightingPath);
            string samplingSource = File.ReadAllText(SamplingPath);
            string surfaceSource = File.ReadAllText(SurfacePath);
            string forwardLitSource = File.ReadAllText(ForwardLitPath);
            string shadowCasterSource = File.ReadAllText(ShadowCasterPath);

            StringAssert.Contains("ESL_EvaluateShadowAttenuation", lightingSource);
            StringAssert.Contains("GetMainLight(inputData.shadowCoord", lightingSource);
            StringAssert.Contains("ESL_ApplyShadowColorBlend", lightingSource);
            StringAssert.Contains("ESL_EvaluateShadowedBandColor", lightingSource);

            StringAssert.Contains("ESL_ApplyAlphaClipFromUV", samplingSource);
            StringAssert.Contains("ESL_SampleBaseAlpha", samplingSource);
            StringAssert.Contains("EnvironmentStylizedLit_Sampling.hlsl", surfaceSource);

            StringAssert.Contains("GetShadowCoord(positionInputs)", forwardLitSource);
            StringAssert.DoesNotContain("ApplyShadowBias(", forwardLitSource);

            StringAssert.Contains("ApplyShadowBias", shadowCasterSource);
            StringAssert.Contains("ESL_ApplyAlphaClipFromUV", shadowCasterSource);
        }

        [Test]
        public void TestRoomContainsM4ShadowAndCullValidationAnchors()
        {
            Material defaultMaterial = AssetDatabase.LoadAssetAtPath<Material>(DefaultMaterialPath);
            Material interiorMaterial = AssetDatabase.LoadAssetAtPath<Material>(InteriorMaterialPath);
            Material roomMaterial = AssetDatabase.LoadAssetAtPath<Material>(RoomMaterialPath);
            Material doubleSidedMaterial = AssetDatabase.LoadAssetAtPath<Material>(DoubleSidedMaterialPath);
            Scene scene = EditorSceneManager.OpenScene(TestRoomScenePath, OpenSceneMode.Single);
            GameObject directionalLightObject = GameObject.Find("Directional Light");
            GameObject pointLightObject = GameObject.Find("M4PointLight");
            GameObject viewpointObject = GameObject.Find("M4RoomViewpoint");
            GameObject interiorRoomObject = GameObject.Find("M4InteriorRoom");

            Assert.IsTrue(scene.IsValid(), "ESL_TestRoom must open successfully.");
            Assert.IsNotNull(directionalLightObject, "M4 validation requires a directional light anchor.");
            AssertPointLightExists(pointLightObject);
            Assert.IsNotNull(viewpointObject, "Expected M4 room viewpoint marker.");
            Assert.IsNotNull(interiorRoomObject, "Expected M4 interior room marker.");

            AssertRendererUsesMaterial("M4InteriorRoom", roomMaterial);
            AssertRendererUsesMaterial("M4CullBackCube", defaultMaterial);
            AssertRendererUsesMaterial("M4CullFrontCube", roomMaterial);
            AssertRendererUsesMaterial("M4CullBothCube", doubleSidedMaterial);
            AssertRendererUsesMaterial("M4AlphaClipCasterQuad", interiorMaterial);

            AssertRendererShadowCastingMode("M4ShadowCasterOnSphere", ShadowCastingMode.On);
            AssertRendererShadowCastingMode("M4ShadowCasterOffSphere", ShadowCastingMode.Off);
            AssertRendererShadowCastingMode("M4AlphaClipCasterQuad", ShadowCastingMode.On);
            AssertLightCastsRealtimeShadows(directionalLightObject, LightType.Directional);
            AssertLightCastsRealtimeShadows(pointLightObject, LightType.Point);
            AssertViewpointInsideRoom(viewpointObject, interiorRoomObject);
            Assert.AreEqual(2f, defaultMaterial.GetFloat("_Cull"));
            Assert.AreEqual(1f, roomMaterial.GetFloat("_Cull"));
            Assert.AreEqual(0f, doubleSidedMaterial.GetFloat("_Cull"));
            Assert.AreEqual(1f, interiorMaterial.GetFloat("_AlphaClip"));
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

        private static void AssertPointLightExists(GameObject pointLightObject)
        {
            Assert.IsNotNull(pointLightObject, "Expected point light anchor: M4PointLight");

            Light light = pointLightObject.GetComponent<Light>();
            Assert.IsNotNull(light, "Expected Light component on M4PointLight.");
            Assert.AreEqual(LightType.Point, light.type);
        }

        private static void AssertLightCastsRealtimeShadows(GameObject lightObject, LightType expectedType)
        {
            Light light = lightObject.GetComponent<Light>();
            Assert.IsNotNull(light, $"Expected Light component on: {lightObject.name}");
            Assert.AreEqual(expectedType, light.type);
            Assert.AreNotEqual(LightShadows.None, light.shadows, $"Expected realtime shadows on: {lightObject.name}");
        }

        private static void AssertViewpointInsideRoom(GameObject viewpointObject, GameObject interiorRoomObject)
        {
            Renderer renderer = interiorRoomObject.GetComponent<Renderer>();
            Assert.IsNotNull(renderer, "Expected renderer on M4InteriorRoom.");
            Assert.IsTrue(renderer.bounds.Contains(viewpointObject.transform.position), "M4 room viewpoint must remain inside the interior room bounds.");
        }

        private static void AssertRendererUsesMaterial(string objectName, Material expectedMaterial)
        {
            GameObject targetObject = GameObject.Find(objectName);
            Assert.IsNotNull(targetObject, $"Expected validation marker object: {objectName}");

            Renderer renderer = targetObject.GetComponent<Renderer>();
            Assert.IsNotNull(renderer, $"Expected renderer on validation marker object: {objectName}");
            Assert.AreSame(expectedMaterial, renderer.sharedMaterial, $"Unexpected material on validation marker object: {objectName}");
        }

        private static void AssertRendererShadowCastingMode(string objectName, ShadowCastingMode expectedMode)
        {
            GameObject targetObject = GameObject.Find(objectName);
            Assert.IsNotNull(targetObject, $"Expected validation marker object: {objectName}");

            Renderer renderer = targetObject.GetComponent<Renderer>();
            Assert.IsNotNull(renderer, $"Expected renderer on validation marker object: {objectName}");
            Assert.AreEqual(expectedMode, renderer.shadowCastingMode);
        }
    }
}