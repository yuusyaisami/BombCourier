using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BC.Rendering.Tests
{
    public sealed class EnvironmentStylizedLitM7ValidationTests
    {
        private const string ShaderPath = "Assets/Art/Shader/EnvironmentStylizedLit/EnvironmentStylizedLit.shader";
        private const string NoisePath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_Noise.hlsl";
        private const string SurfacePath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_Surface.hlsl";
        private const string StylizedDiffusePath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_StylizedDiffuse.hlsl";
        private const string DebugPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_Debug.hlsl";
        private const string ForwardLitPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/Passes/EnvironmentStylizedLit_ForwardLitPass.hlsl";
        private const string TestRoomScenePath = "Assets/Scenes/EnvironmentStylizedLit/ESL_TestRoom.unity";
        private const string LightingLabScenePath = "Assets/Scenes/EnvironmentStylizedLit/ESL_LightingLab.unity";
        private const string WorldNoiseMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_WorldNoise.mat";
        private const string ObjectSpaceNoiseMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_ObjectSpaceNoise.mat";
        private const string BandNoiseMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_BandNoise.mat";
        private const string NoiseOffMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_NoiseOff.mat";

        [Test]
        public void M7ValidationAssetsExist()
        {
            Assert.IsTrue(File.Exists(ShaderPath), $"Expected EnvironmentStylizedLit shader asset: {ShaderPath}");
            Assert.IsTrue(File.Exists(TestRoomScenePath), $"Expected M7 validation scene: {TestRoomScenePath}");
            Assert.IsTrue(File.Exists(LightingLabScenePath), $"Expected M7 lighting validation scene: {LightingLabScenePath}");
            Assert.IsTrue(File.Exists(WorldNoiseMaterialPath), $"Expected M7 validation material: {WorldNoiseMaterialPath}");
            Assert.IsTrue(File.Exists(ObjectSpaceNoiseMaterialPath), $"Expected M7 validation material: {ObjectSpaceNoiseMaterialPath}");
            Assert.IsTrue(File.Exists(BandNoiseMaterialPath), $"Expected M7 validation material: {BandNoiseMaterialPath}");
            Assert.IsTrue(File.Exists(NoiseOffMaterialPath), $"Expected M7 validation material: {NoiseOffMaterialPath}");
        }

        [Test]
        public void M7ShaderDeclaresNoisePropertiesAndDebugViews()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            string shaderSource = File.ReadAllText(ShaderPath);

            Assert.IsNotNull(shader, "EnvironmentStylizedLit shader asset must load.");
            AssertShaderHasProperty(shader, "_NoiseSpace");
            AssertShaderHasProperty(shader, "_AlbedoNoiseStrength");
            AssertShaderHasProperty(shader, "_WorldNoiseScale");
            AssertShaderHasProperty(shader, "_WorldNoiseStrength");
            AssertShaderHasProperty(shader, "_WorldNoiseContrast");
            AssertShaderHasProperty(shader, "_LightBandNoiseStrength");
            AssertShaderHasProperty(shader, "_LightBandNoiseScale");
            AssertShaderHasProperty(shader, "_NoiseDistanceFadeStart");
            AssertShaderHasProperty(shader, "_NoiseDistanceFadeEnd");
            StringAssert.Contains("WorldNoise,0,ObjectSpaceNoise,1", shaderSource);
            StringAssert.Contains("WorldNoise,5", shaderSource);
            StringAssert.Contains("BandNoise,6", shaderSource);
        }

        [Test]
        public void M7TemporaryMaterialSupportsNoiseOverrides()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Material material = new Material(shader);

            try
            {
                material.SetFloat("_NoiseSpace", 1f);
                material.SetFloat("_AlbedoNoiseStrength", 0.34f);
                material.SetFloat("_WorldNoiseScale", 0.58f);
                material.SetFloat("_WorldNoiseStrength", 0.46f);
                material.SetFloat("_WorldNoiseContrast", 1.6f);
                material.SetFloat("_LightBandNoiseStrength", 0.22f);
                material.SetFloat("_LightBandNoiseScale", 1.05f);
                material.SetFloat("_NoiseDistanceFadeStart", 7f);
                material.SetFloat("_NoiseDistanceFadeEnd", 25f);
                material.SetFloat("_DebugView", 6f);

                Assert.AreEqual(1f, material.GetFloat("_NoiseSpace"));
                Assert.AreEqual(0.34f, material.GetFloat("_AlbedoNoiseStrength"));
                Assert.AreEqual(0.58f, material.GetFloat("_WorldNoiseScale"));
                Assert.AreEqual(0.46f, material.GetFloat("_WorldNoiseStrength"));
                Assert.AreEqual(1.6f, material.GetFloat("_WorldNoiseContrast"));
                Assert.AreEqual(0.22f, material.GetFloat("_LightBandNoiseStrength"));
                Assert.AreEqual(1.05f, material.GetFloat("_LightBandNoiseScale"));
                Assert.AreEqual(7f, material.GetFloat("_NoiseDistanceFadeStart"));
                Assert.AreEqual(25f, material.GetFloat("_NoiseDistanceFadeEnd"));
                Assert.AreEqual(6f, material.GetFloat("_DebugView"));
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void M7SourceFilesKeepNoiseResponsibilitiesSeparated()
        {
            string noiseSource = File.ReadAllText(NoisePath);
            string surfaceSource = File.ReadAllText(SurfacePath);
            string stylizedDiffuseSource = File.ReadAllText(StylizedDiffusePath);
            string debugSource = File.ReadAllText(DebugPath);
            string forwardLitSource = File.ReadAllText(ForwardLitPath);

            StringAssert.Contains("ESL_GetNoiseSpace", noiseSource);
            StringAssert.Contains("ESL_EvaluateWorldNoise", noiseSource);
            StringAssert.Contains("ESL_EvaluateObjectSpaceNoise", noiseSource);
            StringAssert.Contains("ESL_EvaluateSurfaceNoise", noiseSource);
            StringAssert.Contains("ESL_EvaluateBandNoise", noiseSource);
            StringAssert.Contains("ESL_EvaluateNoiseDistanceFade", noiseSource);
            StringAssert.Contains("noiseData.distanceFade = ESL_EvaluateNoiseDistanceFade(positionWS)", noiseSource);
            StringAssert.Contains("noiseData.worldNoise = ESL_EvaluateScaledWorldNoise", noiseSource);
            StringAssert.Contains("noiseData.surfaceNoise = noiseData.worldNoise", noiseSource);
            StringAssert.Contains("noiseData.surfaceNoise = noiseData.objectSpaceNoise", noiseSource);
            StringAssert.Contains("_LightBandNoiseStrength > 1e-4", noiseSource);
            StringAssert.Contains("noiseData.bandNoise = ESL_EvaluateNoiseBySpace", noiseSource);

            StringAssert.Contains("ESL_EvaluateNoiseData", surfaceSource);
            StringAssert.Contains("ESL_ApplyAlbedoNoise", surfaceSource);
            StringAssert.Contains("_AlbedoNoiseStrength <= 1e-4", surfaceSource);
            StringAssert.Contains("surfaceData.surfaceNoise", surfaceSource);
            StringAssert.Contains("surfaceData.worldNoise", surfaceSource);

            StringAssert.Contains("diffuseData.worldNoise = surfaceData.worldNoise", stylizedDiffuseSource);
            StringAssert.Contains("diffuseData.surfaceNoise = surfaceData.surfaceNoise", stylizedDiffuseSource);
            StringAssert.Contains("diffuseData.bandNoise = surfaceData.bandNoise", stylizedDiffuseSource);
            StringAssert.Contains("diffuseData.wrappedLight + diffuseData.bandNoise", stylizedDiffuseSource);

            StringAssert.Contains("ESL_DEBUG_WORLD_NOISE", debugSource);
            StringAssert.Contains("ESL_DEBUG_BAND_NOISE", debugSource);
            StringAssert.Contains("ESL_EncodeDebugNoise", debugSource);
            StringAssert.Contains("diffuseData.worldNoise", debugSource);

            StringAssert.DoesNotContain("_NoiseSpace", forwardLitSource);
            StringAssert.DoesNotContain("_WorldNoiseScale", forwardLitSource);
            StringAssert.DoesNotContain("_LightBandNoiseStrength", forwardLitSource);
            StringAssert.DoesNotContain("ESL_EvaluateBandNoise", forwardLitSource);
        }

        [Test]
        public void M7ValidationAssetsContainNoiseAnchors()
        {
            Material worldNoiseMaterial = AssetDatabase.LoadAssetAtPath<Material>(WorldNoiseMaterialPath);
            Material objectSpaceNoiseMaterial = AssetDatabase.LoadAssetAtPath<Material>(ObjectSpaceNoiseMaterialPath);
            Material bandNoiseMaterial = AssetDatabase.LoadAssetAtPath<Material>(BandNoiseMaterialPath);
            Material noiseOffMaterial = AssetDatabase.LoadAssetAtPath<Material>(NoiseOffMaterialPath);

            Assert.IsNotNull(worldNoiseMaterial, "Expected M7 world-noise material asset.");
            Assert.IsNotNull(objectSpaceNoiseMaterial, "Expected M7 object-space noise material asset.");
            Assert.IsNotNull(bandNoiseMaterial, "Expected M7 band-noise material asset.");
            Assert.IsNotNull(noiseOffMaterial, "Expected M7 disabled-noise material asset.");

            Scene testRoomScene = EditorSceneManager.OpenScene(TestRoomScenePath, OpenSceneMode.Single);

            Assert.IsTrue(testRoomScene.IsValid(), "ESL_TestRoom must open successfully.");
            AssertRendererUsesMaterial("M7WorldNoiseWall", worldNoiseMaterial);
            AssertRendererUsesMaterial("M7WorldNoiseFloor", worldNoiseMaterial);
            AssertRendererUsesMaterial("M7NoiseOffWall", noiseOffMaterial);
            AssertRendererUsesMaterial("M7ObjectSpaceNoiseCube", objectSpaceNoiseMaterial);
            AssertQuadNormalY("M7WorldNoiseFloor", 1f);
            AssertMinimumScale("M7WorldNoiseWall", 6f, 4f);
            AssertMinimumScale("M7NoiseOffWall", 6f, 4f);
            AssertViewpointDistanceDelta("M7NoiseNearViewpoint", "M7NoiseFarViewpoint", "M7WorldNoiseWall", 8f);
            Assert.AreEqual(0f, worldNoiseMaterial.GetFloat("_NoiseSpace"));
            Assert.AreEqual(1f, objectSpaceNoiseMaterial.GetFloat("_NoiseSpace"));
            Assert.Greater(worldNoiseMaterial.GetFloat("_AlbedoNoiseStrength"), 0f);
            Assert.Greater(worldNoiseMaterial.GetFloat("_WorldNoiseStrength"), 0f);
            Assert.Greater(worldNoiseMaterial.GetFloat("_NoiseDistanceFadeEnd"), worldNoiseMaterial.GetFloat("_NoiseDistanceFadeStart"));
            AssertCubeRotationY("M7ObjectSpaceNoiseCube", 35f);
            Assert.AreEqual(0f, noiseOffMaterial.GetFloat("_AlbedoNoiseStrength"));
            Assert.AreEqual(0f, noiseOffMaterial.GetFloat("_WorldNoiseStrength"));

            Scene lightingLabScene = EditorSceneManager.OpenScene(LightingLabScenePath, OpenSceneMode.Single);

            Assert.IsTrue(lightingLabScene.IsValid(), "ESL_LightingLab must open successfully.");
            AssertRendererUsesMaterial("M7BandNoiseSphere", bandNoiseMaterial);
            AssertRendererUsesMaterial("M7BandNoiseOffSphere", noiseOffMaterial);
            AssertSpherePosition("M7BandNoiseSphere", new Vector3(-1.6f, 0.95f, 12f));
            AssertSpherePosition("M7BandNoiseOffSphere", new Vector3(1.6f, 0.95f, 12f));
            AssertHorizontalSpacing("M7BandNoiseSphere", "M7BandNoiseOffSphere", 3.2f);
            Assert.Greater(bandNoiseMaterial.GetFloat("_LightBandNoiseStrength"), 0f);
            Assert.AreEqual(0f, noiseOffMaterial.GetFloat("_LightBandNoiseStrength"));
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

            string expectedMaterialPath = AssetDatabase.GetAssetPath(expectedMaterial);
            string actualMaterialPath = AssetDatabase.GetAssetPath(renderer.sharedMaterial);
            Assert.AreEqual(expectedMaterialPath, actualMaterialPath, $"Unexpected material on validation marker object: {objectName}");
        }

        private static void AssertQuadNormalY(string objectName, float expectedY)
        {
            GameObject targetObject = GameObject.Find(objectName);
            Assert.IsNotNull(targetObject, $"Expected validation marker object: {objectName}");
            Assert.That(targetObject.transform.forward.y, Is.EqualTo(expectedY).Within(1e-4f));
        }

        private static void AssertMinimumScale(string objectName, float minimumX, float minimumY)
        {
            GameObject targetObject = GameObject.Find(objectName);
            Assert.IsNotNull(targetObject, $"Expected validation marker object: {objectName}");
            Assert.GreaterOrEqual(targetObject.transform.localScale.x, minimumX);
            Assert.GreaterOrEqual(targetObject.transform.localScale.y, minimumY);
        }

        private static void AssertViewpointDistanceDelta(string nearViewpointName, string farViewpointName, string targetObjectName, float minimumDelta)
        {
            GameObject nearViewpoint = GameObject.Find(nearViewpointName);
            GameObject farViewpoint = GameObject.Find(farViewpointName);
            GameObject targetObject = GameObject.Find(targetObjectName);

            Assert.IsNotNull(nearViewpoint, $"Expected viewpoint marker object: {nearViewpointName}");
            Assert.IsNotNull(farViewpoint, $"Expected viewpoint marker object: {farViewpointName}");
            Assert.IsNotNull(targetObject, $"Expected validation marker object: {targetObjectName}");

            float nearDistance = Vector3.Distance(nearViewpoint.transform.position, targetObject.transform.position);
            float farDistance = Vector3.Distance(farViewpoint.transform.position, targetObject.transform.position);
            Assert.GreaterOrEqual(farDistance - nearDistance, minimumDelta, $"Expected far viewpoint to be farther from {targetObjectName}.");
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

        private static void AssertCubeRotationY(string objectName, float expectedYDegrees)
        {
            GameObject targetObject = GameObject.Find(objectName);
            Assert.IsNotNull(targetObject, $"Expected validation marker object: {objectName}");
            Assert.That(targetObject.transform.eulerAngles.y, Is.EqualTo(expectedYDegrees).Within(1e-3f));
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