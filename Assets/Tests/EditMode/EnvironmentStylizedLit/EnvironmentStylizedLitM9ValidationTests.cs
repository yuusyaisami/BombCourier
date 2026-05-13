using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BC.Rendering.Tests
{
    public sealed class EnvironmentStylizedLitM9ValidationTests
    {
        private const string ShaderPath = "Assets/Art/Shader/EnvironmentStylizedLit/EnvironmentStylizedLit.shader";
        private const string CommonPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_Common.hlsl";
        private const string AmbientPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_Ambient.hlsl";
        private const string LightingPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_Lighting.hlsl";
        private const string StylizedDiffusePath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_StylizedDiffuse.hlsl";
        private const string ForwardLitPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/Passes/EnvironmentStylizedLit_ForwardLitPass.hlsl";
        private const string DepthNormalsPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/Passes/EnvironmentStylizedLit_DepthNormalsPass.hlsl";
        private const string TestRoomScenePath = "Assets/Scenes/EnvironmentStylizedLit/ESL_TestRoom.unity";
        private const string LightingLabScenePath = "Assets/Scenes/EnvironmentStylizedLit/ESL_LightingLab.unity";
        private const string IndirectBaselineMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_IndirectBaseline.mat";
        private const string ProbeIndirectMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_ProbeIndirect.mat";
        private const string CavityTintMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_CavityTint.mat";

        [Test]
        public void M9ValidationAssetsExist()
        {
            Assert.IsTrue(File.Exists(ShaderPath), $"Expected EnvironmentStylizedLit shader asset: {ShaderPath}");
            Assert.IsTrue(File.Exists(CommonPath), $"Expected shared common source: {CommonPath}");
            Assert.IsTrue(File.Exists(AmbientPath), $"Expected ambient source: {AmbientPath}");
            Assert.IsTrue(File.Exists(LightingPath), $"Expected lighting source: {LightingPath}");
            Assert.IsTrue(File.Exists(StylizedDiffusePath), $"Expected stylized diffuse source: {StylizedDiffusePath}");
            Assert.IsTrue(File.Exists(ForwardLitPath), $"Expected forward pass source: {ForwardLitPath}");
            Assert.IsTrue(File.Exists(DepthNormalsPath), $"Expected depth normals source: {DepthNormalsPath}");
            Assert.IsTrue(File.Exists(TestRoomScenePath), $"Expected M9 validation scene: {TestRoomScenePath}");
            Assert.IsTrue(File.Exists(LightingLabScenePath), $"Expected M9 validation scene: {LightingLabScenePath}");
            Assert.IsTrue(File.Exists(IndirectBaselineMaterialPath), $"Expected M9 validation material: {IndirectBaselineMaterialPath}");
            Assert.IsTrue(File.Exists(ProbeIndirectMaterialPath), $"Expected M9 validation material: {ProbeIndirectMaterialPath}");
            Assert.IsTrue(File.Exists(CavityTintMaterialPath), $"Expected M9 validation material: {CavityTintMaterialPath}");
        }

        [Test]
        public void M9ShaderDeclaresIndirectCompatibilityProperties()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Material material = new Material(shader);

            try
            {
                Assert.IsNotNull(shader, "EnvironmentStylizedLit shader asset must load.");
                AssertShaderHasProperty(shader, "_IndirectStrength");
                AssertShaderHasProperty(shader, "_IndirectStylizeStrength");
                AssertShaderHasProperty(shader, "_CavityStrength");
                AssertShaderHasProperty(shader, "_CavityColor");

                material.SetFloat("_IndirectStrength", 0.8f);
                material.SetFloat("_IndirectStylizeStrength", 0.45f);
                material.SetFloat("_CavityStrength", 0.62f);
                material.SetColor("_CavityColor", new Color(0.43f, 0.5f, 0.61f, 1f));

                Assert.AreEqual(0.8f, material.GetFloat("_IndirectStrength"));
                Assert.AreEqual(0.45f, material.GetFloat("_IndirectStylizeStrength"));
                Assert.AreEqual(0.62f, material.GetFloat("_CavityStrength"));
                AssertColorApproximately(new Color(0.43f, 0.5f, 0.61f, 1f), material.GetColor("_CavityColor"));
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void M9SourceFilesKeepIndirectCompatibilityResponsibilitiesSeparated()
        {
            string shaderSource = File.ReadAllText(ShaderPath);
            string commonSource = File.ReadAllText(CommonPath);
            string ambientSource = File.ReadAllText(AmbientPath);
            string lightingSource = File.ReadAllText(LightingPath);
            string stylizedDiffuseSource = File.ReadAllText(StylizedDiffusePath);
            string forwardLitSource = File.ReadAllText(ForwardLitPath);
            string depthNormalsSource = File.ReadAllText(DepthNormalsPath);

            StringAssert.Contains("#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION", shaderSource);
            StringAssert.Contains("#pragma multi_compile_fragment _ _SCREEN_SPACE_IRRADIANCE", shaderSource);
            StringAssert.Contains("#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING", shaderSource);
            StringAssert.Contains("#pragma multi_compile _ SHADOWS_SHADOWMASK", shaderSource);
            StringAssert.Contains("#pragma multi_compile _ LIGHTMAP_ON", shaderSource);
            StringAssert.Contains("#pragma multi_compile _ DYNAMICLIGHTMAP_ON", shaderSource);
            StringAssert.Contains("ProbeVolumeVariants.hlsl", shaderSource);

            StringAssert.Contains("float3 bakedGI", commonSource);
            StringAssert.Contains("float4 shadowMask", commonSource);
            StringAssert.Contains("float2 normalizedScreenSpaceUV", commonSource);
            StringAssert.Contains("float indirectAmbientOcclusion", commonSource);
            StringAssert.Contains("float directAmbientOcclusion", commonSource);

            StringAssert.Contains("ESL_EvaluateAmbientData", ambientSource);
            StringAssert.Contains("ESL_EvaluateDirectionalAmbient", ambientSource);
            StringAssert.Contains("ESL_EvaluateBounceFactor", ambientSource);

            StringAssert.Contains("MixRealtimeAndBakedGI", lightingSource);
            StringAssert.Contains("ESL_EvaluateCavityTint", lightingSource);
            StringAssert.Contains("ESL_EvaluateStylizedBakedGI", lightingSource);
            StringAssert.Contains("inputData.bakedGI", lightingSource);
            StringAssert.Contains("indirectLightingData.bakedGIColor", lightingSource);
            StringAssert.Contains("_IndirectStrength", lightingSource);
            StringAssert.Contains("_IndirectStylizeStrength", lightingSource);
            StringAssert.Contains("_CavityStrength", lightingSource);
            StringAssert.Contains("_CavityColor", lightingSource);
            StringAssert.DoesNotContain("color *= ao", lightingSource);

            StringAssert.Contains("OUTPUT_LIGHTMAP_UV", forwardLitSource);
            StringAssert.Contains("OUTPUT_SH4", forwardLitSource);
            StringAssert.Contains("ESL_GetNormalizedScreenSpaceUV", forwardLitSource);
            StringAssert.Contains("UNITY_PRETRANSFORM_TO_DISPLAY_ORIENTATION", forwardLitSource);
            StringAssert.Contains("SAMPLE_GI", forwardLitSource);
            StringAssert.Contains("CreateAmbientOcclusionFactor", forwardLitSource);
            StringAssert.DoesNotContain("GetAdditionalLight(", forwardLitSource);

            StringAssert.Contains("ESL_EvaluateIndirectLighting(inputData, surfaceData", stylizedDiffuseSource);
            StringAssert.Contains("diffuseData.bakedGIColor", stylizedDiffuseSource);

            Assert.IsTrue(
                depthNormalsSource.Contains("ESL_SampleNormalTS") || depthNormalsSource.Contains("ESL_SampleSurfaceNormalWS"),
                "DepthNormals pass must continue sampling normals through the shared sampling layer.");
            StringAssert.Contains("_GBUFFER_NORMALS_OCT", depthNormalsSource);
            StringAssert.DoesNotContain("GetMainLight(", depthNormalsSource);
        }

        [Test]
        public void M9ValidationAssetsContainIndirectCompatibilityAnchors()
        {
            Material indirectBaselineMaterial = AssetDatabase.LoadAssetAtPath<Material>(IndirectBaselineMaterialPath);
            Material probeIndirectMaterial = AssetDatabase.LoadAssetAtPath<Material>(ProbeIndirectMaterialPath);
            Material cavityTintMaterial = AssetDatabase.LoadAssetAtPath<Material>(CavityTintMaterialPath);

            Assert.IsNotNull(indirectBaselineMaterial, "Expected M9 indirect baseline material asset.");
            Assert.IsNotNull(probeIndirectMaterial, "Expected M9 probe indirect material asset.");
            Assert.IsNotNull(cavityTintMaterial, "Expected M9 cavity tint material asset.");

            Scene testRoomScene = EditorSceneManager.OpenScene(TestRoomScenePath, OpenSceneMode.Single);

            Assert.IsTrue(testRoomScene.IsValid(), "ESL_TestRoom must open successfully.");
            Assert.IsNotNull(GameObject.Find("M9IndirectViewpoint"), "Expected M9 indirect viewpoint marker.");
            AssertRendererUsesMaterial("M9IndirectBaselineCube_Room", indirectBaselineMaterial);
            AssertRendererUsesMaterial("M9ProbeIndirectSphere_Room", probeIndirectMaterial);
            AssertRendererUsesMaterial("M9CavityTintCube_Room", cavityTintMaterial);
            AssertRendererShadowCastingMode("M9IndirectBaselineCube_Room", ShadowCastingMode.On);
            AssertRendererShadowCastingMode("M9ProbeIndirectSphere_Room", ShadowCastingMode.On);
            AssertRendererShadowCastingMode("M9CavityTintCube_Room", ShadowCastingMode.On);
            Assert.AreEqual(0f, indirectBaselineMaterial.GetFloat("_IndirectStylizeStrength"));
            Assert.Greater(probeIndirectMaterial.GetFloat("_IndirectStylizeStrength"), indirectBaselineMaterial.GetFloat("_IndirectStylizeStrength"));
            Assert.Greater(cavityTintMaterial.GetFloat("_CavityStrength"), probeIndirectMaterial.GetFloat("_CavityStrength"));

            Scene lightingLabScene = EditorSceneManager.OpenScene(LightingLabScenePath, OpenSceneMode.Single);

            Assert.IsTrue(lightingLabScene.IsValid(), "ESL_LightingLab must open successfully.");
            Assert.IsNotNull(GameObject.Find("M9LightingLabViewpoint"), "Expected M9 lighting-lab viewpoint marker.");
            AssertRendererUsesMaterial("M9IndirectBaselineSphere_Lab", indirectBaselineMaterial);
            AssertRendererUsesMaterial("M9ProbeIndirectSphere_Lab", probeIndirectMaterial);
            AssertRendererUsesMaterial("M9CavityTintQuad_Lab", cavityTintMaterial);
            AssertHorizontalSpacing("M9IndirectBaselineSphere_Lab", "M9ProbeIndirectSphere_Lab", 3.2f);
            AssertColorApproximately(new Color(0.42f, 0.5f, 0.62f, 1f), cavityTintMaterial.GetColor("_CavityColor"));
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

        private static void AssertRendererShadowCastingMode(string objectName, ShadowCastingMode expectedMode)
        {
            GameObject targetObject = GameObject.Find(objectName);
            Assert.IsNotNull(targetObject, $"Expected validation marker object: {objectName}");

            Renderer renderer = targetObject.GetComponent<Renderer>();
            Assert.IsNotNull(renderer, $"Expected renderer on validation marker object: {objectName}");
            Assert.AreEqual(expectedMode, renderer.shadowCastingMode);
        }

        private static void AssertHorizontalSpacing(string leftObjectName, string rightObjectName, float expectedSpacing)
        {
            GameObject leftObject = GameObject.Find(leftObjectName);
            GameObject rightObject = GameObject.Find(rightObjectName);

            Assert.IsNotNull(leftObject, $"Expected validation marker object: {leftObjectName}");
            Assert.IsNotNull(rightObject, $"Expected validation marker object: {rightObjectName}");
            Assert.That(rightObject.transform.position.x - leftObject.transform.position.x, Is.EqualTo(expectedSpacing).Within(1e-4f));
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