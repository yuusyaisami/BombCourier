using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BC.Rendering.Tests
{
    public sealed class EnvironmentStylizedLitM11ValidationTests
    {
        private const string ShaderPath = "Assets/Art/Shader/EnvironmentStylizedLit/EnvironmentStylizedLit.shader";
        private const string TriplanarPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_Triplanar.hlsl";
        private const string SamplingPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_Sampling.hlsl";
        private const string NoisePath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_Noise.hlsl";
        private const string SurfacePath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_Surface.hlsl";
        private const string LightingPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_Lighting.hlsl";
        private const string StylizedDiffusePath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/EnvironmentStylizedLit_StylizedDiffuse.hlsl";
        private const string ForwardLitPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/Passes/EnvironmentStylizedLit_ForwardLitPass.hlsl";
        private const string ShadowCasterPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/Passes/EnvironmentStylizedLit_ShadowCasterPass.hlsl";
        private const string DepthOnlyPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/Passes/EnvironmentStylizedLit_DepthOnlyPass.hlsl";
        private const string DepthNormalsPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/Passes/EnvironmentStylizedLit_DepthNormalsPass.hlsl";
        private const string MetaPath = "Assets/Art/Shader/EnvironmentStylizedLit/HLSL/Passes/EnvironmentStylizedLit_MetaPass.hlsl";
        private const string TestRoomScenePath = "Assets/Scenes/EnvironmentStylizedLit/ESL_TestRoom.unity";
        private const string LightingLabScenePath = "Assets/Scenes/EnvironmentStylizedLit/ESL_LightingLab.unity";
        private const string TriplanarOffMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_TriplanarOff.mat";
        private const string TriplanarFullMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_TriplanarFull.mat";
        private const string VertexColorMaskMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_VertexColorMask.mat";
        private const string WorldYGradientMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_WorldYGradient.mat";

        [Test]
        public void M11ValidationAssetsExist()
        {
            Assert.IsTrue(File.Exists(ShaderPath), $"Expected EnvironmentStylizedLit shader asset: {ShaderPath}");
            Assert.IsTrue(File.Exists(TriplanarPath), $"Expected M11 triplanar source: {TriplanarPath}");
            Assert.IsTrue(File.Exists(SamplingPath), $"Expected M11 sampling source: {SamplingPath}");
            Assert.IsTrue(File.Exists(NoisePath), $"Expected M11 noise source: {NoisePath}");
            Assert.IsTrue(File.Exists(SurfacePath), $"Expected M11 surface source: {SurfacePath}");
            Assert.IsTrue(File.Exists(ForwardLitPath), $"Expected M11 forward source: {ForwardLitPath}");
            Assert.IsTrue(File.Exists(ShadowCasterPath), $"Expected M11 shadow source: {ShadowCasterPath}");
            Assert.IsTrue(File.Exists(DepthOnlyPath), $"Expected M11 depth-only source: {DepthOnlyPath}");
            Assert.IsTrue(File.Exists(DepthNormalsPath), $"Expected M11 depth normals source: {DepthNormalsPath}");
            Assert.IsTrue(File.Exists(MetaPath), $"Expected M11 meta source: {MetaPath}");
            Assert.IsTrue(File.Exists(TestRoomScenePath), $"Expected M11 validation scene: {TestRoomScenePath}");
            Assert.IsTrue(File.Exists(LightingLabScenePath), $"Expected M11 validation scene: {LightingLabScenePath}");
            Assert.IsTrue(File.Exists(TriplanarOffMaterialPath), $"Expected M11 validation material: {TriplanarOffMaterialPath}");
            Assert.IsTrue(File.Exists(TriplanarFullMaterialPath), $"Expected M11 validation material: {TriplanarFullMaterialPath}");
            Assert.IsTrue(File.Exists(VertexColorMaskMaterialPath), $"Expected M11 validation material: {VertexColorMaskMaterialPath}");
            Assert.IsTrue(File.Exists(WorldYGradientMaterialPath), $"Expected M11 validation material: {WorldYGradientMaterialPath}");
        }

        [Test]
        public void M11ShaderDeclaresTriplanarVertexColorAndGradientProperties()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Material material = new Material(shader);

            try
            {
                Assert.IsNotNull(shader, "EnvironmentStylizedLit shader asset must load.");
                AssertShaderHasProperty(shader, "_TriplanarBaseMapEnabled");
                AssertShaderHasProperty(shader, "_TriplanarNormalMapEnabled");
                AssertShaderHasProperty(shader, "_TriplanarNoiseEnabled");
                AssertShaderHasProperty(shader, "_TriplanarScale");
                AssertShaderHasProperty(shader, "_TriplanarBlendSharpness");
                AssertShaderHasProperty(shader, "_VertexColorEnabled");
                AssertShaderHasProperty(shader, "_VertexColorCavityStrength");
                AssertShaderHasProperty(shader, "_VertexColorBandOffsetStrength");
                AssertShaderHasProperty(shader, "_VertexColorColorVariationStrength");
                AssertShaderHasProperty(shader, "_WorldYGradientEnabled");
                AssertShaderHasProperty(shader, "_WorldYGradientTopColor");
                AssertShaderHasProperty(shader, "_WorldYGradientBottomColor");
                AssertShaderHasProperty(shader, "_WorldYGradientMin");
                AssertShaderHasProperty(shader, "_WorldYGradientMax");
                AssertShaderHasProperty(shader, "_WorldYGradientStrength");

                material.SetFloat("_TriplanarBaseMapEnabled", 1f);
                material.EnableKeyword("_ESL_TRIPLANAR_BASEMAP");
                material.SetFloat("_TriplanarNormalMapEnabled", 1f);
                material.EnableKeyword("_ESL_TRIPLANAR_NORMALMAP");
                material.SetFloat("_TriplanarNoiseEnabled", 1f);
                material.EnableKeyword("_ESL_TRIPLANAR_NOISE");
                material.SetFloat("_VertexColorEnabled", 1f);
                material.SetFloat("_WorldYGradientEnabled", 1f);
                material.SetFloat("_WorldYGradientStrength", 0.6f);
                material.SetFloat("_WorldYGradientMin", 0f);
                material.SetFloat("_WorldYGradientMax", 4f);

                Assert.AreEqual(1f, material.GetFloat("_TriplanarBaseMapEnabled"));
                Assert.AreEqual(1f, material.GetFloat("_TriplanarNormalMapEnabled"));
                Assert.AreEqual(1f, material.GetFloat("_TriplanarNoiseEnabled"));
                Assert.AreEqual(1f, material.GetFloat("_VertexColorEnabled"));
                Assert.AreEqual(1f, material.GetFloat("_WorldYGradientEnabled"));
                Assert.IsTrue(material.IsKeywordEnabled("_ESL_TRIPLANAR_BASEMAP"));
                Assert.IsTrue(material.IsKeywordEnabled("_ESL_TRIPLANAR_NORMALMAP"));
                Assert.IsTrue(material.IsKeywordEnabled("_ESL_TRIPLANAR_NOISE"));
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void M11SourceFilesKeepResponsibilitiesSeparated()
        {
            string shaderSource = File.ReadAllText(ShaderPath);
            string triplanarSource = File.ReadAllText(TriplanarPath);
            string samplingSource = File.ReadAllText(SamplingPath);
            string noiseSource = File.ReadAllText(NoisePath);
            string surfaceSource = File.ReadAllText(SurfacePath);
            string lightingSource = File.ReadAllText(LightingPath);
            string stylizedDiffuseSource = File.ReadAllText(StylizedDiffusePath);
            string forwardLitSource = File.ReadAllText(ForwardLitPath);
            string shadowCasterSource = File.ReadAllText(ShadowCasterPath);
            string depthOnlySource = File.ReadAllText(DepthOnlyPath);
            string depthNormalsSource = File.ReadAllText(DepthNormalsPath);
            string metaSource = File.ReadAllText(MetaPath);

            StringAssert.Contains("[Toggle(_ESL_TRIPLANAR_BASEMAP)]", shaderSource);
            StringAssert.Contains("[Toggle(_ESL_TRIPLANAR_NORMALMAP)]", shaderSource);
            StringAssert.Contains("[Toggle(_ESL_TRIPLANAR_NOISE)]", shaderSource);
            Assert.GreaterOrEqual(CountOccurrences(shaderSource, "#pragma shader_feature_local_fragment _ _ESL_TRIPLANAR_BASEMAP"), 4);
            StringAssert.Contains("#pragma shader_feature_local_fragment _ _ESL_TRIPLANAR_NORMALMAP", shaderSource);
            StringAssert.Contains("#pragma shader_feature_local_fragment _ _ESL_TRIPLANAR_NOISE", shaderSource);

            StringAssert.Contains("ESL_BuildTriplanarData", triplanarSource);
            StringAssert.Contains("ESL_BuildTriplanarData(float3 positionWS, float3 normalWS, float2 uvScale, float2 uvOffset)", triplanarSource);
            StringAssert.Contains("ESL_SampleTriplanarBaseColor", triplanarSource);
            StringAssert.Contains("ESL_SampleTriplanarNormalWS", triplanarSource);
            StringAssert.Contains("ESL_IsTriplanarBaseMapEnabled", triplanarSource);
            StringAssert.Contains("ESL_IsTriplanarNoiseEnabled", triplanarSource);

            StringAssert.Contains("ESL_SampleBaseColor(float2 uv, float3 positionWS, float3 normalWS)", samplingSource);
            StringAssert.Contains("ESL_SampleSurfaceNormalWS", samplingSource);

            StringAssert.Contains("ESL_EvaluateSignedTriplanarNoise", noiseSource);
            StringAssert.Contains("ESL_IsTriplanarNoiseEnabled()", noiseSource);
            StringAssert.Contains("ESL_EvaluateNoiseData(float3 positionWS, float3 normalWS)", noiseSource);
            StringAssert.Contains("ESL_BuildTriplanarData(positionWS * noiseScale, normalWS, 1.0.xx, 0.0.xx)", noiseSource);
            StringAssert.DoesNotContain("ESL_BuildTriplanarData(positionWS * noiseScale, normalWS);", noiseSource);

            StringAssert.Contains("ESL_ApplyAlphaClipFromSurface", samplingSource);
            StringAssert.Contains("ESL_ApplyVertexColorMasks", surfaceSource);
            StringAssert.Contains("ESL_ApplyWorldYGradient", surfaceSource);
            StringAssert.Contains("surfaceData.cavity = surfaceContext.vertexColor.r", surfaceSource);
            StringAssert.Contains("surfaceData.bandOffsetMask = surfaceContext.vertexColor.g", surfaceSource);
            StringAssert.Contains("surfaceData.colorVariationMask = surfaceContext.vertexColor.b", surfaceSource);
            StringAssert.Contains("surfaceData.specialMask = surfaceContext.vertexColor.a", surfaceSource);
            StringAssert.Contains("surfaceData.localBandOffset", surfaceSource);
            StringAssert.Contains("ESL_EvaluateSurfaceBandOffset", surfaceSource);

            StringAssert.Contains("ESL_EvaluateSurfaceBandOffset(surfaceData)", stylizedDiffuseSource);
            StringAssert.DoesNotContain("_WorldYGradientEnabled", lightingSource);
            StringAssert.DoesNotContain("ESL_BuildTriplanarData", lightingSource);
            StringAssert.DoesNotContain("ESL_EvaluateSignedTriplanarNoise", forwardLitSource);
            StringAssert.DoesNotContain("_WorldYGradientEnabled", forwardLitSource);
            StringAssert.Contains("surfaceContext.vertexColor = input.vertexColor", forwardLitSource);

            StringAssert.Contains("ESL_ApplyAlphaClipFromSurface", shadowCasterSource);
            StringAssert.Contains("input.positionWS", shadowCasterSource);
            StringAssert.Contains("input.normalWS", shadowCasterSource);
            StringAssert.Contains("ESL_ApplyAlphaClipFromSurface", depthOnlySource);
            StringAssert.Contains("input.positionWS", depthOnlySource);
            StringAssert.Contains("input.normalWS", depthOnlySource);
            StringAssert.Contains("ESL_SampleSurfaceNormalWS", depthNormalsSource);
            StringAssert.Contains("#pragma vertex ESL_MetaPassVertex", shaderSource);
            StringAssert.Contains("ESL_SampleBaseColor(input.uv, input.positionWS, input.normalWS)", metaSource);
            StringAssert.Contains("UnityMetaVertexPosition", metaSource);
            StringAssert.DoesNotContain("ESL_SampleBaseColor(input.uv);", metaSource);
        }

        [Test]
        public void M11ValidationAssetsContainTriplanarVertexColorAndGradientAnchors()
        {
            Material triplanarOffMaterial = AssetDatabase.LoadAssetAtPath<Material>(TriplanarOffMaterialPath);
            Material triplanarFullMaterial = AssetDatabase.LoadAssetAtPath<Material>(TriplanarFullMaterialPath);
            Material vertexColorMaskMaterial = AssetDatabase.LoadAssetAtPath<Material>(VertexColorMaskMaterialPath);
            Material worldYGradientMaterial = AssetDatabase.LoadAssetAtPath<Material>(WorldYGradientMaterialPath);

            Assert.IsNotNull(triplanarOffMaterial, "Expected M11 triplanar-off material asset.");
            Assert.IsNotNull(triplanarFullMaterial, "Expected M11 triplanar material asset.");
            Assert.IsNotNull(vertexColorMaskMaterial, "Expected M11 vertex-color material asset.");
            Assert.IsNotNull(worldYGradientMaterial, "Expected M11 gradient material asset.");
            Assert.IsFalse(triplanarOffMaterial.IsKeywordEnabled("_ESL_TRIPLANAR_BASEMAP"));
            Assert.IsTrue(triplanarFullMaterial.IsKeywordEnabled("_ESL_TRIPLANAR_BASEMAP"));
            Assert.IsTrue(triplanarFullMaterial.IsKeywordEnabled("_ESL_TRIPLANAR_NORMALMAP"));
            Assert.IsTrue(triplanarFullMaterial.IsKeywordEnabled("_ESL_TRIPLANAR_NOISE"));
            Assert.AreEqual(1f, vertexColorMaskMaterial.GetFloat("_VertexColorEnabled"));
            Assert.AreEqual(1f, worldYGradientMaterial.GetFloat("_WorldYGradientEnabled"));

            Scene testRoomScene = EditorSceneManager.OpenScene(TestRoomScenePath, OpenSceneMode.Single);
            Assert.IsTrue(testRoomScene.IsValid(), "ESL_TestRoom must open successfully.");
            Assert.IsNotNull(GameObject.Find("M11TriplanarViewpoint"), "Expected M11 viewpoint marker.");
            AssertRendererUsesMaterial("M11TriplanarOffWall", triplanarOffMaterial);
            AssertRendererUsesMaterial("M11TriplanarWall", triplanarFullMaterial);
            AssertRendererUsesMaterial("M11WorldGradientWall", worldYGradientMaterial);
            AssertRendererUsesMaterial("M11VertexColorPanel_Room", vertexColorMaskMaterial);
            AssertRoughUvMesh("M11TriplanarOffWall");
            AssertRoughUvMesh("M11TriplanarWall");
            AssertVertexColorPanel("M11VertexColorPanel_Room");

            Scene lightingLabScene = EditorSceneManager.OpenScene(LightingLabScenePath, OpenSceneMode.Single);
            Assert.IsTrue(lightingLabScene.IsValid(), "ESL_LightingLab must open successfully.");
            Assert.IsNotNull(GameObject.Find("M11LightingLabViewpoint"), "Expected M11 lighting-lab viewpoint marker.");
            AssertRendererUsesMaterial("M11TriplanarOffSphere_Lab", triplanarOffMaterial);
            AssertRendererUsesMaterial("M11TriplanarSphere_Lab", triplanarFullMaterial);
            AssertRendererUsesMaterial("M11VertexColorPanel_Lab", vertexColorMaskMaterial);
            AssertRendererUsesMaterial("M11WorldGradientQuad_Lab", worldYGradientMaterial);
            AssertVertexColorPanel("M11VertexColorPanel_Lab");
            AssertHorizontalSpacing("M11TriplanarOffSphere_Lab", "M11TriplanarSphere_Lab", 3.2f);
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

        private static void AssertVertexColorPanel(string objectName)
        {
            GameObject targetObject = GameObject.Find(objectName);
            Assert.IsNotNull(targetObject, $"Expected validation marker object: {objectName}");

            MeshFilter meshFilter = targetObject.GetComponent<MeshFilter>();
            Assert.IsNotNull(meshFilter, $"Expected MeshFilter on validation marker object: {objectName}");
            Assert.IsNotNull(meshFilter.sharedMesh, $"Expected mesh on validation marker object: {objectName}");
            Assert.AreEqual(4, meshFilter.sharedMesh.colors.Length, $"Expected vertex colors on validation marker object: {objectName}");
            Assert.Greater(meshFilter.sharedMesh.colors[0].r, 0.9f);
            Assert.Greater(meshFilter.sharedMesh.colors[1].g, 0.9f);
            Assert.Greater(meshFilter.sharedMesh.colors[2].b, 0.9f);
        }

        private static void AssertRoughUvMesh(string objectName)
        {
            GameObject targetObject = GameObject.Find(objectName);
            Assert.IsNotNull(targetObject, $"Expected validation marker object: {objectName}");

            MeshFilter meshFilter = targetObject.GetComponent<MeshFilter>();
            Assert.IsNotNull(meshFilter, $"Expected MeshFilter on validation marker object: {objectName}");
            Assert.IsNotNull(meshFilter.sharedMesh, $"Expected mesh on validation marker object: {objectName}");
            Vector2[] uv = meshFilter.sharedMesh.uv;
            Assert.AreEqual(4, uv.Length, $"Expected rough UV quad mesh on validation marker object: {objectName}");
            Assert.Greater(uv[1].x, 5f, $"Expected stretched UVs on validation marker object: {objectName}");
            Assert.Less(uv[1].y, 0.1f, $"Expected compressed V range on validation marker object: {objectName}");
            Assert.Greater(uv[3].x, uv[1].x, $"Expected non-uniform UV scaling on validation marker object: {objectName}");
        }

        private static void AssertHorizontalSpacing(string leftObjectName, string rightObjectName, float expectedSpacing)
        {
            GameObject leftObject = GameObject.Find(leftObjectName);
            GameObject rightObject = GameObject.Find(rightObjectName);

            Assert.IsNotNull(leftObject, $"Expected validation marker object: {leftObjectName}");
            Assert.IsNotNull(rightObject, $"Expected validation marker object: {rightObjectName}");
            Assert.That(rightObject.transform.position.x - leftObject.transform.position.x, Is.EqualTo(expectedSpacing).Within(1e-4f));
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