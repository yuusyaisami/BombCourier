using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BC.Rendering.Tests
{
    public sealed class EnvironmentStylizedLitM13ValidationTests
    {
        private const string ShaderPath = "Assets/Art/Shader/EnvironmentStylizedLit/EnvironmentStylizedLit.shader";
        private const string BuildValidatorPath = "Assets/Art/Shader/EnvironmentStylizedLit/Editor/EnvironmentStylizedLitBuildValidator.cs";
        private const string TierUtilityPath = "Assets/Art/Shader/EnvironmentStylizedLit/Editor/EnvironmentStylizedLitPerformanceTierUtility.cs";
        private const string BootstrapperPath = "Assets/Art/Shader/EnvironmentStylizedLit/Editor/EnvironmentStylizedLitValidationBootstrapper.cs";
        private const string TestRoomScenePath = "Assets/Scenes/EnvironmentStylizedLit/ESL_TestRoom.unity";
        private const string LightingLabScenePath = "Assets/Scenes/EnvironmentStylizedLit/ESL_LightingLab.unity";
        private const string TierLowMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_TierLow.mat";
        private const string TierMediumMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_TierMedium.mat";
        private const string TierHighMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_TierHigh.mat";

        [Test]
        public void M13FilesAndValidationAssetsExist()
        {
            Assert.IsTrue(File.Exists(ShaderPath), $"Expected shader asset: {ShaderPath}");
            Assert.IsTrue(File.Exists(BuildValidatorPath), $"Expected M13 build validator source: {BuildValidatorPath}");
            Assert.IsTrue(File.Exists(TierUtilityPath), $"Expected M13 tier utility source: {TierUtilityPath}");
            Assert.IsTrue(File.Exists(BootstrapperPath), $"Expected M13 bootstrapper source: {BootstrapperPath}");
            Assert.IsTrue(File.Exists(TestRoomScenePath), $"Expected M13 validation scene: {TestRoomScenePath}");
            Assert.IsTrue(File.Exists(LightingLabScenePath), $"Expected M13 validation scene: {LightingLabScenePath}");
            Assert.IsTrue(File.Exists(TierLowMaterialPath), $"Expected M13 tier material: {TierLowMaterialPath}");
            Assert.IsTrue(File.Exists(TierMediumMaterialPath), $"Expected M13 tier material: {TierMediumMaterialPath}");
            Assert.IsTrue(File.Exists(TierHighMaterialPath), $"Expected M13 tier material: {TierHighMaterialPath}");
        }

        [Test]
        public void M13SourceDeclaresVariantAndBuildPolicies()
        {
            string shaderSource = File.ReadAllText(ShaderPath);
            string buildValidatorSource = File.ReadAllText(BuildValidatorPath);
            string tierUtilitySource = File.ReadAllText(TierUtilityPath);
            string bootstrapperSource = File.ReadAllText(BootstrapperPath);

            StringAssert.Contains("#pragma shader_feature_local_fragment _ _ESL_TRIPLANAR_BASEMAP", shaderSource);
            StringAssert.Contains("#pragma shader_feature_local_fragment _ _ESL_TRIPLANAR_NORMALMAP", shaderSource);
            StringAssert.Contains("#pragma shader_feature_local_fragment _ _ESL_TRIPLANAR_NOISE", shaderSource);
            StringAssert.DoesNotContain("_DEBUG_VIEW", shaderSource);
            StringAssert.DoesNotContain("_ESL_TIER_", shaderSource);
            StringAssert.DoesNotContain("_ESL_VERTEX_COLOR", shaderSource);
            StringAssert.DoesNotContain("_ESL_WORLD_Y_GRADIENT", shaderSource);
            StringAssert.Contains("#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3", shaderSource);

            StringAssert.Contains("TryGetDebugViewBuildError", buildValidatorSource);
            StringAssert.Contains("TryGetNonDevelopmentBuildError", buildValidatorSource);
            StringAssert.Contains("IPreprocessBuildWithReport", buildValidatorSource);

            StringAssert.Contains("\"Low\"", tierUtilitySource);
            StringAssert.Contains("\"Medium\"", tierUtilitySource);
            StringAssert.Contains("\"High\"", tierUtilitySource);
            StringAssert.Contains("ApplyTier", tierUtilitySource);
            StringAssert.Contains("TryInferTier", tierUtilitySource);

            StringAssert.Contains("BootstrapM13ValidationAssets", bootstrapperSource);
            StringAssert.Contains("ESL_Test_TierLow.mat", bootstrapperSource);
            StringAssert.Contains("ESL_Test_TierMedium.mat", bootstrapperSource);
            StringAssert.Contains("ESL_Test_TierHigh.mat", bootstrapperSource);
        }

        [Test]
        public void M13TierUtilityAppliesAndInfersCanonicalTiers()
        {
            Material lowMaterial = CreateTemporaryMaterial();
            Material mediumMaterial = CreateTemporaryMaterial();
            Material highMaterial = CreateTemporaryMaterial();
            Material nearMissMediumMaterial = CreateTemporaryMaterial();
            Material nearMissHighMaterial = CreateTemporaryMaterial();

            try
            {
                Type tierUtilityType = GetEditorAssemblyType("BC.Rendering.EnvironmentStylizedLitPerformanceTierUtility");
                MethodInfo getTierNamesMethod = GetStaticMethod(tierUtilityType, "GetTierNames");
                MethodInfo applyTierMethod = GetStaticMethod(tierUtilityType, "ApplyTier", typeof(Material), typeof(int));
                MethodInfo tryInferTierMethod = GetStaticMethod(tierUtilityType, "TryInferTier", typeof(Material), tierUtilityType.Assembly.GetType("BC.Rendering.EnvironmentStylizedLitPerformanceTier").MakeByRefType());

                CollectionAssert.AreEqual(new[] { "Low", "Medium", "High" }, (string[])getTierNamesMethod.Invoke(null, null));

                Assert.IsTrue((bool)applyTierMethod.Invoke(null, new object[] { lowMaterial, 0 }));
                Assert.IsTrue((bool)applyTierMethod.Invoke(null, new object[] { mediumMaterial, 1 }));
                Assert.IsTrue((bool)applyTierMethod.Invoke(null, new object[] { highMaterial, 2 }));

                AssertTierMaterialState(lowMaterial, 0f, false, false, false, 0f, 0f, 1f, 0f);
                AssertTierMaterialState(mediumMaterial, 1f, false, false, false, 0.26f, 0f, 1f, 0f);
                AssertTierMaterialState(highMaterial, 1f, true, true, true, 0.26f, 0.1f, 3f, 1f);

                AssertTierInference(tryInferTierMethod, lowMaterial, "Low");
                AssertTierInference(tryInferTierMethod, mediumMaterial, "Medium");
                AssertTierInference(tryInferTierMethod, highMaterial, "High");

                Assert.IsTrue((bool)applyTierMethod.Invoke(null, new object[] { nearMissMediumMaterial, 1 }));
                nearMissMediumMaterial.SetFloat("_AdditionalLightIntensity", 0f);
                AssertTierInferenceFails(tryInferTierMethod, nearMissMediumMaterial);

                Assert.IsTrue((bool)applyTierMethod.Invoke(null, new object[] { nearMissHighMaterial, 2 }));
                nearMissHighMaterial.SetFloat("_AdditionalLightMode", 0f);
                AssertTierInferenceFails(tryInferTierMethod, nearMissHighMaterial);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(lowMaterial);
                UnityEngine.Object.DestroyImmediate(mediumMaterial);
                UnityEngine.Object.DestroyImmediate(highMaterial);
                UnityEngine.Object.DestroyImmediate(nearMissMediumMaterial);
                UnityEngine.Object.DestroyImmediate(nearMissHighMaterial);
            }
        }

        [Test]
        public void M13BuildValidatorRejectsDebugViewForShipping()
        {
            Material material = CreateTemporaryMaterial();
            Material validationMaterial = AssetDatabase.LoadAssetAtPath<Material>(TierLowMaterialPath);

            try
            {
                Type validatorType = GetEditorAssemblyType("BC.Rendering.EnvironmentStylizedLitBuildValidator");
                MethodInfo perMaterialErrorMethod = GetStaticMethod(validatorType, "TryGetDebugViewBuildError", typeof(Material), typeof(string).MakeByRefType());
                MethodInfo buildWideErrorMethod = GetStaticMethod(validatorType, "TryGetNonDevelopmentBuildError", typeof(string).MakeByRefType());

                material.SetFloat("_DebugView", 5f);

                object[] arguments = { material, null };
                Assert.IsTrue((bool)perMaterialErrorMethod.Invoke(null, arguments));
                StringAssert.Contains("Debug View enabled", arguments[1] as string);
                StringAssert.Contains(material.name, arguments[1] as string);

                Assert.IsNotNull(validationMaterial, "Expected M13 validation material asset.");
                float originalDebugView = validationMaterial.GetFloat("_DebugView");
                validationMaterial.SetFloat("_DebugView", 5f);

                try
                {
                    object[] buildWideArguments = { null };
                    Assert.IsFalse((bool)buildWideErrorMethod.Invoke(null, buildWideArguments));
                }
                finally
                {
                    validationMaterial.SetFloat("_DebugView", originalDebugView);
                }

                material.SetFloat("_DebugView", 0f);
                arguments = new object[] { material, null };
                Assert.IsFalse((bool)perMaterialErrorMethod.Invoke(null, arguments));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void M13ValidationScenesContainTierAndMeasurementAnchors()
        {
            Material tierLowMaterial = AssetDatabase.LoadAssetAtPath<Material>(TierLowMaterialPath);
            Material tierMediumMaterial = AssetDatabase.LoadAssetAtPath<Material>(TierMediumMaterialPath);
            Material tierHighMaterial = AssetDatabase.LoadAssetAtPath<Material>(TierHighMaterialPath);

            Assert.IsNotNull(tierLowMaterial, "Expected M13 low-tier material asset.");
            Assert.IsNotNull(tierMediumMaterial, "Expected M13 medium-tier material asset.");
            Assert.IsNotNull(tierHighMaterial, "Expected M13 high-tier material asset.");

            Scene testRoomScene = EditorSceneManager.OpenScene(TestRoomScenePath, OpenSceneMode.Single);
            Assert.IsTrue(testRoomScene.IsValid(), "ESL_TestRoom must open successfully.");
            Assert.IsNotNull(GameObject.Find("M13TierViewpoint"), "Expected M13 tier viewpoint marker.");
            AssertRendererUsesMaterial("M13TierLowWall", tierLowMaterial);
            AssertRendererUsesMaterial("M13TierMediumWall", tierMediumMaterial);
            AssertRendererUsesMaterial("M13TierHighWall", tierHighMaterial);
            AssertRendererUsesMaterial("M13TierMediumFloor", tierMediumMaterial);
            AssertRendererUsesMaterial("M13TriplanarStressWall", tierHighMaterial);
            AssertRoughUvMesh("M13TierHighWall");
            AssertRoughUvMesh("M13TriplanarStressWall");
            AssertMinimumScale("M13TierMediumFloor", 8f, 8f);
            AssertViewpointDistanceDelta("M13NoiseNearViewpoint", "M13NoiseFarViewpoint", "M13TierMediumWall", 10f);

            Scene lightingLabScene = EditorSceneManager.OpenScene(LightingLabScenePath, OpenSceneMode.Single);
            Assert.IsTrue(lightingLabScene.IsValid(), "ESL_LightingLab must open successfully.");
            Assert.IsNotNull(GameObject.Find("M13LightingLabViewpoint"), "Expected M13 lighting-lab viewpoint marker.");
            AssertRendererUsesMaterial("M13TierLowSphere_Lab", tierLowMaterial);
            AssertRendererUsesMaterial("M13TierMediumSphere_Lab", tierMediumMaterial);
            AssertRendererUsesMaterial("M13TierHighSphere_Lab", tierHighMaterial);
            AssertRendererUsesMaterial("M13TierHighPanel_Lab", tierHighMaterial);
            AssertRendererUsesMaterial("M13AdditionalLightWall_Lab", tierMediumMaterial);
            AssertRendererUsesMaterial("M13AdditionalLightFloor_Lab", tierMediumMaterial);
            AssertLight("M13PointLight_Lab", LightType.Point);
            AssertLight("M13SpotLight_Lab", LightType.Spot);
            AssertHorizontalSpacing("M13TierLowSphere_Lab", "M13TierMediumSphere_Lab", 3.2f);
            AssertHorizontalSpacing("M13TierMediumSphere_Lab", "M13TierHighSphere_Lab", 3.2f);
            AssertMinimumScale("M13AdditionalLightWall_Lab", 7.5f, 4.8f);
            AssertMinimumScale("M13AdditionalLightFloor_Lab", 8f, 8f);
        }

        private static Material CreateTemporaryMaterial()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Assert.IsNotNull(shader, "EnvironmentStylizedLit shader asset must load.");
            return new Material(shader);
        }

        private static Type GetEditorAssemblyType(string fullTypeName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullTypeName);
                if (type != null)
                {
                    return type;
                }
            }

            Assert.Fail($"Expected editor type: {fullTypeName}");
            return null;
        }

        private static MethodInfo GetStaticMethod(Type declaringType, string methodName, params Type[] parameterTypes)
        {
            MethodInfo method = declaringType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, parameterTypes, null);
            Assert.IsNotNull(method, $"Expected static method: {declaringType.FullName}.{methodName}");
            return method;
        }

        private static void AssertTierInference(MethodInfo tryInferTierMethod, Material material, string expectedTier)
        {
            object[] arguments = { material, null };
            Assert.IsTrue((bool)tryInferTierMethod.Invoke(null, arguments), $"Expected canonical tier inference for {material.name}.");
            Assert.AreEqual(expectedTier, arguments[1].ToString());
        }

        private static void AssertTierInferenceFails(MethodInfo tryInferTierMethod, Material material)
        {
            object[] arguments = { material, null };
            Assert.IsFalse((bool)tryInferTierMethod.Invoke(null, arguments), $"Did not expect canonical tier inference for {material.name}.");
        }

        private static void AssertTierMaterialState(
            Material material,
            float additionalLightMode,
            bool triplanarBaseEnabled,
            bool triplanarNormalEnabled,
            bool triplanarNoiseEnabled,
            float worldNoiseStrength,
            float lightBandNoiseStrength,
            float specularMode,
            float vertexColorEnabled)
        {
            Assert.AreEqual(additionalLightMode, material.GetFloat("_AdditionalLightMode"));
            Assert.AreEqual(worldNoiseStrength, material.GetFloat("_WorldNoiseStrength"));
            Assert.AreEqual(lightBandNoiseStrength, material.GetFloat("_LightBandNoiseStrength"));
            Assert.AreEqual(specularMode, material.GetFloat("_SpecularMode"));
            Assert.AreEqual(vertexColorEnabled, material.GetFloat("_VertexColorEnabled"));
            Assert.AreEqual(triplanarBaseEnabled, material.IsKeywordEnabled("_ESL_TRIPLANAR_BASEMAP"));
            Assert.AreEqual(triplanarNormalEnabled, material.IsKeywordEnabled("_ESL_TRIPLANAR_NORMALMAP"));
            Assert.AreEqual(triplanarNoiseEnabled, material.IsKeywordEnabled("_ESL_TRIPLANAR_NOISE"));
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

        private static void AssertRoughUvMesh(string objectName)
        {
            GameObject targetObject = GameObject.Find(objectName);
            Assert.IsNotNull(targetObject, $"Expected validation marker object: {objectName}");

            MeshFilter meshFilter = targetObject.GetComponent<MeshFilter>();
            Assert.IsNotNull(meshFilter, $"Expected MeshFilter on validation marker object: {objectName}");
            Assert.IsNotNull(meshFilter.sharedMesh, $"Expected mesh on validation marker object: {objectName}");
            Assert.Greater(meshFilter.sharedMesh.uv[1].x, 6f);
            Assert.Less(meshFilter.sharedMesh.uv[1].y, 0.1f);
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