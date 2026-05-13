using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BC.Rendering.Tests
{
    public sealed class EnvironmentStylizedLitM8ValidationTests
    {
        private const string ShaderPath = "Assets/Art/Shader/EnvironmentStylizedLit/EnvironmentStylizedLit.shader";
        private const string SamplingPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_Sampling.hlsl";
        private const string SurfacePath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_Surface.hlsl";
        private const string ShadowCasterPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/Passes/EnvironmentStylizedLit_ShadowCasterPass.hlsl";
        private const string DepthOnlyPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/Passes/EnvironmentStylizedLit_DepthOnlyPass.hlsl";
        private const string DepthNormalsPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/Passes/EnvironmentStylizedLit_DepthNormalsPass.hlsl";
        private const string MetaPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/Passes/EnvironmentStylizedLit_MetaPass.hlsl";
        private const string TestRoomScenePath = "Assets/Scenes/EnvironmentStylizedLit/ESL_TestRoom.unity";
        private const string LightingLabScenePath = "Assets/Scenes/EnvironmentStylizedLit/ESL_LightingLab.unity";
        private const string DefaultMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_Default.mat";
        private const string InteriorMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_Interior.mat";

        [Test]
        public void M8ValidationAssetsExist()
        {
            Assert.IsTrue(File.Exists(ShaderPath), $"Expected EnvironmentStylizedLit shader asset: {ShaderPath}");
            Assert.IsTrue(File.Exists(SamplingPath), $"Expected shared sampling source: {SamplingPath}");
            Assert.IsTrue(File.Exists(SurfacePath), $"Expected shared surface source: {SurfacePath}");
            Assert.IsTrue(File.Exists(ShadowCasterPath), $"Expected ShadowCaster source: {ShadowCasterPath}");
            Assert.IsTrue(File.Exists(DepthOnlyPath), $"Expected DepthOnly source: {DepthOnlyPath}");
            Assert.IsTrue(File.Exists(DepthNormalsPath), $"Expected DepthNormals source: {DepthNormalsPath}");
            Assert.IsTrue(File.Exists(MetaPath), $"Expected Meta source: {MetaPath}");
            Assert.IsTrue(File.Exists(TestRoomScenePath), $"Expected M8 validation scene: {TestRoomScenePath}");
            Assert.IsTrue(File.Exists(LightingLabScenePath), $"Expected M8 validation scene: {LightingLabScenePath}");
            Assert.IsTrue(File.Exists(DefaultMaterialPath), $"Expected default validation material: {DefaultMaterialPath}");
            Assert.IsTrue(File.Exists(InteriorMaterialPath), $"Expected interior validation material: {InteriorMaterialPath}");
        }

        [Test]
        public void M8ShaderDeclaresRequiredRuntimePasses()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            string shaderSource = File.ReadAllText(ShaderPath);
            Material material = new Material(shader);

            try
            {
                Assert.IsNotNull(shader, "EnvironmentStylizedLit shader asset must load.");
                StringAssert.Contains("Name \"ShadowCaster\"", shaderSource);
                StringAssert.Contains("\"LightMode\" = \"ShadowCaster\"", shaderSource);
                StringAssert.Contains("Name \"DepthOnly\"", shaderSource);
                StringAssert.Contains("\"LightMode\" = \"DepthOnly\"", shaderSource);
                StringAssert.Contains("Name \"DepthNormalsOnly\"", shaderSource);
                StringAssert.Contains("\"LightMode\" = \"DepthNormalsOnly\"", shaderSource);
                StringAssert.Contains("Name \"Meta\"", shaderSource);
                StringAssert.Contains("\"LightMode\" = \"Meta\"", shaderSource);
                Assert.GreaterOrEqual(CountOccurrences(shaderSource, "#pragma multi_compile_instancing"), 5, "Forward, ShadowCaster, DepthOnly, DepthNormalsOnly, and Meta must all declare instancing support.");
                Assert.GreaterOrEqual(material.FindPass("ShadowCaster"), 0);
                Assert.GreaterOrEqual(material.FindPass("DepthOnly"), 0);
                Assert.GreaterOrEqual(material.FindPass("DepthNormalsOnly"), 0);
                Assert.GreaterOrEqual(material.FindPass("Meta"), 0);
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void M8SourceFilesKeepPassResponsibilitiesSeparated()
        {
            string samplingSource = File.ReadAllText(SamplingPath);
            string surfaceSource = File.ReadAllText(SurfacePath);
            string shadowCasterSource = File.ReadAllText(ShadowCasterPath);
            string depthOnlySource = File.ReadAllText(DepthOnlyPath);
            string depthNormalsSource = File.ReadAllText(DepthNormalsPath);
            string metaSource = File.ReadAllText(MetaPath);

            StringAssert.Contains("float3 ESL_SampleNormalTS", samplingSource);
            StringAssert.Contains("float3 ESL_SampleEmission", samplingSource);
            StringAssert.Contains("void ESL_ApplyAlphaClip", samplingSource);
            StringAssert.Contains("#include \"EnvironmentStylizedLit_Sampling.hlsl\"", surfaceSource);
            StringAssert.Contains("#include \"EnvironmentStylizedLit_Noise.hlsl\"", surfaceSource);

            StringAssert.Contains("#include \"../EnvironmentStylizedLit_Sampling.hlsl\"", shadowCasterSource);
            StringAssert.Contains("ApplyShadowBias", shadowCasterSource);
            Assert.IsTrue(
                shadowCasterSource.Contains("ESL_ApplyAlphaClipFromUV") || shadowCasterSource.Contains("ESL_ApplyAlphaClipFromSurface"),
                "ShadowCaster pass must alpha-clip through the shared sampling layer.");
            StringAssert.Contains("UNITY_VERTEX_INPUT_INSTANCE_ID", shadowCasterSource);
            StringAssert.Contains("UNITY_SETUP_INSTANCE_ID", shadowCasterSource);
            StringAssert.DoesNotContain("EnvironmentStylizedLit_Surface.hlsl", shadowCasterSource);
            StringAssert.DoesNotContain("ESL_EvaluateStylizedDiffuse", shadowCasterSource);
            StringAssert.DoesNotContain("_WorldNoiseScale", shadowCasterSource);

            StringAssert.Contains("#include \"../EnvironmentStylizedLit_Sampling.hlsl\"", depthOnlySource);
            StringAssert.Contains("TransformObjectToHClip", depthOnlySource);
            Assert.IsTrue(
                depthOnlySource.Contains("ESL_ApplyAlphaClipFromUV") || depthOnlySource.Contains("ESL_ApplyAlphaClipFromSurface"),
                "DepthOnly pass must alpha-clip through the shared sampling layer.");
            StringAssert.DoesNotContain("GetMainLight(", depthOnlySource);
            StringAssert.DoesNotContain("EnvironmentStylizedLit_Surface.hlsl", depthOnlySource);
            StringAssert.DoesNotContain("ESL_EvaluateNoiseData", depthOnlySource);
            StringAssert.DoesNotContain("metaInput", depthOnlySource);

            StringAssert.Contains("#include \"../EnvironmentStylizedLit_Sampling.hlsl\"", depthNormalsSource);
            Assert.IsTrue(
                depthNormalsSource.Contains("ESL_SampleNormalTS") || depthNormalsSource.Contains("ESL_SampleSurfaceNormalWS"),
                "DepthNormals pass must sample surface normals through the shared sampling layer.");
            StringAssert.Contains("ESL_ApplyAlphaClipFromUV", depthNormalsSource);
            StringAssert.Contains("_GBUFFER_NORMALS_OCT", depthNormalsSource);
            StringAssert.DoesNotContain("GetMainLight(", depthNormalsSource);
            StringAssert.DoesNotContain("EnvironmentStylizedLit_Surface.hlsl", depthNormalsSource);
            StringAssert.DoesNotContain("ESL_EvaluateNoiseData", depthNormalsSource);
            StringAssert.DoesNotContain("ESL_EvaluateStylizedDiffuse", depthNormalsSource);

            StringAssert.Contains("#include \"../EnvironmentStylizedLit_Sampling.hlsl\"", metaSource);
            StringAssert.Contains("MetaInput metaInput", metaSource);
            StringAssert.Contains("metaInput.Albedo", metaSource);
            StringAssert.Contains("metaInput.Emission", metaSource);
            Assert.IsTrue(
                metaSource.Contains("UniversalFragmentMeta") || metaSource.Contains("UnityMetaFragment"),
                "Meta pass must end in the URP meta fragment path.");
            StringAssert.Contains("ESL_SampleBaseColor", metaSource);
            StringAssert.Contains("ESL_SampleEmission", metaSource);
            StringAssert.Contains("ESL_ApplyAlphaClip", metaSource);
            StringAssert.DoesNotContain("EnvironmentStylizedLit_Surface.hlsl", metaSource);
            StringAssert.DoesNotContain("SAMPLE_TEXTURE2D(_BaseMap", metaSource);
            StringAssert.DoesNotContain("SAMPLE_TEXTURE2D(_EmissionMap", metaSource);
            StringAssert.DoesNotContain("ESL_EvaluateStylizedDiffuse", metaSource);
            StringAssert.DoesNotContain("ESL_EvaluateNoiseData", metaSource);
            StringAssert.DoesNotContain("_SpecularMode", metaSource);
            StringAssert.DoesNotContain("_WorldNoiseScale", metaSource);
        }

        [Test]
        public void M8ValidationScenesContainPassCoverageAnchors()
        {
            Material defaultMaterial = AssetDatabase.LoadAssetAtPath<Material>(DefaultMaterialPath);
            Material interiorMaterial = AssetDatabase.LoadAssetAtPath<Material>(InteriorMaterialPath);

            Assert.IsNotNull(defaultMaterial, "Expected default validation material asset.");
            Assert.IsNotNull(interiorMaterial, "Expected interior validation material asset.");

            Scene testRoomScene = EditorSceneManager.OpenScene(TestRoomScenePath, OpenSceneMode.Single);

            Assert.IsTrue(testRoomScene.IsValid(), "ESL_TestRoom must open successfully.");
            Assert.IsNotNull(GameObject.Find("M8DepthViewpoint"), "Expected M8 depth viewpoint marker.");
            AssertRendererUsesMaterial("M8DepthAlphaClipQuad", interiorMaterial);
            AssertRendererUsesMaterial("M8DepthNormalsCube", defaultMaterial);
            AssertRendererShadowCastingMode("M8DepthAlphaClipQuad", ShadowCastingMode.On);
            AssertRendererShadowCastingMode("M8DepthNormalsCube", ShadowCastingMode.On);
            Assert.That(GameObject.Find("M8DepthNormalsCube").transform.eulerAngles.y, Is.EqualTo(27f).Within(1e-3f));
            Assert.AreEqual(1f, interiorMaterial.GetFloat("_AlphaClip"));
            Assert.Greater(defaultMaterial.GetFloat("_NormalScale"), 0f);

            Scene lightingLabScene = EditorSceneManager.OpenScene(LightingLabScenePath, OpenSceneMode.Single);

            Assert.IsTrue(lightingLabScene.IsValid(), "ESL_LightingLab must open successfully.");
            Assert.IsNotNull(GameObject.Find("M8MetaViewpoint"), "Expected M8 meta viewpoint marker.");
            AssertRendererUsesMaterial("M8MetaEmissionCube", defaultMaterial);
            AssertRendererShadowCastingMode("M8MetaEmissionCube", ShadowCastingMode.On);
            Assert.Greater(defaultMaterial.GetFloat("_EmissionStrength"), 0f);
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

        private static int CountOccurrences(string source, string value)
        {
            int count = 0;
            int startIndex = 0;

            while ((startIndex = source.IndexOf(value, startIndex, System.StringComparison.Ordinal)) >= 0)
            {
                count++;
                startIndex += value.Length;
            }

            return count;
        }
    }
}