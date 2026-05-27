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
    public sealed class EnvironmentStylizedLitM14ValidationTests
    {
        private const string BootstrapperPath = "Assets/Art/Shader/EnvironmentStylizedLit/Editor/EnvironmentStylizedLitValidationBootstrapper.cs";
        private const string ReadmePath = "Assets/Art/Shader/EnvironmentStylizedLit/Documentation/README.md";
        private const string PropertyReferencePath = "Assets/Art/Shader/EnvironmentStylizedLit/Documentation/EnvironmentStylizedLitPropertyReference.md";
        private const string AuthoringGuidePath = "Assets/Art/Shader/EnvironmentStylizedLit/Documentation/EnvironmentStylizedLitAuthoringGuide.md";
        private const string TroubleshootingPath = "Assets/Art/Shader/EnvironmentStylizedLit/Documentation/EnvironmentStylizedLitTroubleshooting.md";
        private const string MilestoneSpecPath = "Assets/Docs/ShaderMilestoneSpec.md";
        private const string ShaderPath = "Assets/Art/Shader/EnvironmentStylizedLit/EnvironmentStylizedLit.shader";
        private const string TestRoomScenePath = "Assets/Scenes/EnvironmentStylizedLit/ESL_TestRoom.unity";
        private const string LightingLabScenePath = "Assets/Scenes/EnvironmentStylizedLit/ESL_LightingLab.unity";
        private const string ClayMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_M14_ClayDiorama.mat";
        private const string PaintedPlasterMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_M14_PaintedPlaster.mat";
        private const string MatteToyPlasticMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_M14_MatteToyPlastic.mat";
        private const string CeramicToyMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_M14_CeramicToy.mat";
        private const string ChalkPastelMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_M14_ChalkPastel.mat";

        [Test]
        public void M14FilesDocumentationAndValidationAssetsExist()
        {
            Assert.IsTrue(File.Exists(BootstrapperPath), $"Expected M14 bootstrapper source: {BootstrapperPath}");
            Assert.IsTrue(File.Exists(ReadmePath), $"Expected module readme: {ReadmePath}");
            Assert.IsTrue(File.Exists(PropertyReferencePath), $"Expected M14 property reference: {PropertyReferencePath}");
            Assert.IsTrue(File.Exists(AuthoringGuidePath), $"Expected M14 authoring guide: {AuthoringGuidePath}");
            Assert.IsTrue(File.Exists(TroubleshootingPath), $"Expected M14 troubleshooting guide: {TroubleshootingPath}");
            Assert.IsTrue(File.Exists(TestRoomScenePath), $"Expected validation scene: {TestRoomScenePath}");
            Assert.IsTrue(File.Exists(LightingLabScenePath), $"Expected validation scene: {LightingLabScenePath}");
            Assert.IsTrue(File.Exists(ClayMaterialPath), $"Expected M14 preset review material: {ClayMaterialPath}");
            Assert.IsTrue(File.Exists(PaintedPlasterMaterialPath), $"Expected M14 preset review material: {PaintedPlasterMaterialPath}");
            Assert.IsTrue(File.Exists(MatteToyPlasticMaterialPath), $"Expected M14 preset review material: {MatteToyPlasticMaterialPath}");
            Assert.IsTrue(File.Exists(CeramicToyMaterialPath), $"Expected M14 preset review material: {CeramicToyMaterialPath}");
            Assert.IsTrue(File.Exists(ChalkPastelMaterialPath), $"Expected M14 preset review material: {ChalkPastelMaterialPath}");
        }

        [Test]
        public void M14BootstrapperDeclaresProductionValidationSurface()
        {
            string bootstrapperSource = File.ReadAllText(BootstrapperPath);

            StringAssert.Contains("BootstrapM14ValidationAssets", bootstrapperSource);
            StringAssert.Contains("ShapeGenerator.GeneratePlane", bootstrapperSource);
            StringAssert.Contains("ShapeGenerator.GenerateStair", bootstrapperSource);
            StringAssert.Contains("M14ProBuilderFloor_Room", bootstrapperSource);
            StringAssert.Contains("M14ProBuilderWall_Room", bootstrapperSource);
            StringAssert.Contains("M14Stair_Room", bootstrapperSource);
            StringAssert.Contains("M14BeveledWall_Room", bootstrapperSource);
            StringAssert.Contains("M14DirectionalOnlyViewpoint", bootstrapperSource);
            StringAssert.Contains("M14PointLight_Lab", bootstrapperSource);
            StringAssert.Contains("M14SpotLight_Lab", bootstrapperSource);
        }

        [Test]
        public void M14PresetReviewMaterialsMatchCanonicalPresetDefinitions()
        {
            AssertPresetMaterialMatchesDefinition(ClayMaterialPath, "ClayDiorama");
            AssertPresetMaterialMatchesDefinition(PaintedPlasterMaterialPath, "PaintedPlaster");
            AssertPresetMaterialMatchesDefinition(MatteToyPlasticMaterialPath, "MatteToyPlastic");
            AssertPresetMaterialMatchesDefinition(CeramicToyMaterialPath, "CeramicToy");
            AssertPresetMaterialMatchesDefinition(ChalkPastelMaterialPath, "ChalkPastel");
        }

        [Test]
        public void M14ValidationScenesContainProductionAnchors()
        {
            Material clayMaterial = AssetDatabase.LoadAssetAtPath<Material>(ClayMaterialPath);
            Material paintedPlasterMaterial = AssetDatabase.LoadAssetAtPath<Material>(PaintedPlasterMaterialPath);
            Material matteToyPlasticMaterial = AssetDatabase.LoadAssetAtPath<Material>(MatteToyPlasticMaterialPath);
            Material ceramicToyMaterial = AssetDatabase.LoadAssetAtPath<Material>(CeramicToyMaterialPath);
            Material chalkPastelMaterial = AssetDatabase.LoadAssetAtPath<Material>(ChalkPastelMaterialPath);

            Assert.IsNotNull(clayMaterial);
            Assert.IsNotNull(paintedPlasterMaterial);
            Assert.IsNotNull(matteToyPlasticMaterial);
            Assert.IsNotNull(ceramicToyMaterial);
            Assert.IsNotNull(chalkPastelMaterial);

            Scene testRoomScene = EditorSceneManager.OpenScene(TestRoomScenePath, OpenSceneMode.Single);
            Assert.IsTrue(testRoomScene.IsValid(), "ESL_TestRoom must open successfully.");
            Assert.IsNotNull(GameObject.Find("M14IndoorViewpoint"));
            Assert.IsNotNull(GameObject.Find("M14LightmapViewpoint"));
            Assert.IsNotNull(GameObject.Find("M14SSAOOnViewpoint"));
            Assert.IsNotNull(GameObject.Find("M14SSAOOffViewpoint"));
            AssertRendererUsesMaterial("M14ProBuilderFloor_Room", clayMaterial);
            AssertRendererUsesMaterial("M14ProBuilderWall_Room", paintedPlasterMaterial);
            AssertRendererUsesMaterial("M14Stair_Room", matteToyPlasticMaterial);
            AssertRendererUsesMaterial("M14Column_Room", ceramicToyMaterial);
            AssertRendererUsesMaterial("M14BeveledWall_Room", paintedPlasterMaterial);
            AssertRendererUsesMaterial("M14LightmappedColumn_Room", ceramicToyMaterial);
            AssertRendererUsesMaterial("M14DynamicColumn_Room", ceramicToyMaterial);
            AssertRendererUsesMaterial("M14SSAOReferenceWall_Room", chalkPastelMaterial);
            AssertHasComponentNamed("M14ProBuilderFloor_Room", "ProBuilderMesh");
            AssertHasComponentNamed("M14ProBuilderWall_Room", "ProBuilderMesh");
            AssertHasComponentNamed("M14Stair_Room", "ProBuilderMesh");
            AssertMeshVertexCountAtLeast("M14ProBuilderFloor_Room", 16);
            AssertMeshVertexCountAtLeast("M14ProBuilderWall_Room", 9);
            AssertMeshVertexCountAtLeast("M14Stair_Room", 24);
            AssertBeveledMesh("M14BeveledWall_Room");
            AssertLightmapState("M14LightmappedColumn_Room", true);
            AssertLightmapState("M14DynamicColumn_Room", false);
            AssertMinimumScale("M14ProBuilderFloor_Room", 1f, 1f);

            Scene lightingLabScene = EditorSceneManager.OpenScene(LightingLabScenePath, OpenSceneMode.Single);
            Assert.IsTrue(lightingLabScene.IsValid(), "ESL_LightingLab must open successfully.");
            AssertRendererUsesMaterial("M14ClayDioramaSphere_Lab", clayMaterial);
            AssertRendererUsesMaterial("M14PaintedPlasterSphere_Lab", paintedPlasterMaterial);
            AssertRendererUsesMaterial("M14MatteToyPlasticSphere_Lab", matteToyPlasticMaterial);
            AssertRendererUsesMaterial("M14CeramicToySphere_Lab", ceramicToyMaterial);
            AssertRendererUsesMaterial("M14ChalkPastelSphere_Lab", chalkPastelMaterial);
            AssertRendererUsesMaterial("M14PointLightColumn_Lab", ceramicToyMaterial);
            AssertRendererUsesMaterial("M14SpotLightWall_Lab", chalkPastelMaterial);
            AssertLight("M14PointLight_Lab", LightType.Point);
            AssertLight("M14SpotLight_Lab", LightType.Spot);
            AssertOutsideLightRange("M14ClayDioramaSphere_Lab", "M14PointLight_Lab", 0.5f);
            AssertOutsideLightRange("M14PaintedPlasterSphere_Lab", "M14PointLight_Lab", 0.5f);
            AssertOutsideLightRange("M14MatteToyPlasticSphere_Lab", "M14PointLight_Lab", 0.5f);
            AssertOutsideLightRange("M14CeramicToySphere_Lab", "M14PointLight_Lab", 0.5f);
            AssertOutsideLightRange("M14ChalkPastelSphere_Lab", "M14PointLight_Lab", 0.5f);
            AssertOutsideLightRange("M14ClayDioramaSphere_Lab", "M14SpotLight_Lab", 0.5f);
            AssertOutsideLightRange("M14PaintedPlasterSphere_Lab", "M14SpotLight_Lab", 0.5f);
            AssertOutsideLightRange("M14MatteToyPlasticSphere_Lab", "M14SpotLight_Lab", 0.5f);
            AssertOutsideLightRange("M14CeramicToySphere_Lab", "M14SpotLight_Lab", 0.5f);
            AssertOutsideLightRange("M14ChalkPastelSphere_Lab", "M14SpotLight_Lab", 0.5f);
            AssertHorizontalSpacing("M14ClayDioramaSphere_Lab", "M14PaintedPlasterSphere_Lab", 3.2f);
            AssertHorizontalSpacing("M14PaintedPlasterSphere_Lab", "M14MatteToyPlasticSphere_Lab", 3.2f);
            AssertHorizontalSpacing("M14MatteToyPlasticSphere_Lab", "M14CeramicToySphere_Lab", 3.2f);
            AssertHorizontalSpacing("M14CeramicToySphere_Lab", "M14ChalkPastelSphere_Lab", 3.2f);
        }

        [Test]
        public void M14DocumentationAndStatusDescribeProductionUsage()
        {
            string readme = File.ReadAllText(ReadmePath);
            string propertyReference = File.ReadAllText(PropertyReferencePath);
            string authoringGuide = File.ReadAllText(AuthoringGuidePath);
            string troubleshooting = File.ReadAllText(TroubleshootingPath);
            string milestoneSpec = File.ReadAllText(MilestoneSpecPath);

            StringAssert.Contains("EnvironmentStylizedLitPropertyReference.md", readme);
            StringAssert.Contains("EnvironmentStylizedLitAuthoringGuide.md", readme);
            StringAssert.Contains("EnvironmentStylizedLitTroubleshooting.md", readme);
            StringAssert.Contains("M14 Production Validation / Authoring Guide is implemented", readme);

            StringAssert.Contains("_LightStepCount", propertyReference);
            StringAssert.Contains("_MainLightColorInfluence", propertyReference);
            StringAssert.Contains("_MainLightIntensityResponse", propertyReference);
            StringAssert.Contains("_AdditionalLightAttenuationPower", propertyReference);
            StringAssert.Contains("_AdditionalLightPaletteBlend", propertyReference);
            StringAssert.Contains("_AdditionalFillMaxMask", propertyReference);
            StringAssert.Contains("_TriplanarBaseMapEnabled", propertyReference);
            StringAssert.Contains("_DebugView", propertyReference);
            StringAssert.Contains("Recommended", propertyReference);

            StringAssert.Contains("ProBuilder", authoringGuide);
            StringAssert.Contains("Validation Scenes", authoringGuide);
            StringAssert.Contains("Triplanar", authoringGuide);
            StringAssert.Contains("ClayDiorama", authoringGuide);
            StringAssert.Contains("Not Recommended", authoringGuide);

            StringAssert.Contains("Debug View", troubleshooting);
            StringAssert.Contains("Shadow", troubleshooting);
            StringAssert.Contains("Noise", troubleshooting);
            StringAssert.Contains("Triplanar", troubleshooting);
            StringAssert.Contains("Use Cases", troubleshooting);

            StringAssert.Contains("現在の実装到達点: M14 Production Validation / Authoring Guide", milestoneSpec);
            StringAssert.Contains("| M14 Production Validation / Authoring Guide | Complete | 100% |", milestoneSpec);
        }

        private static void AssertPresetMaterialMatchesDefinition(string materialPath, string presetName)
        {
            Material actualMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Assert.IsNotNull(actualMaterial, $"Expected preset review material asset: {materialPath}");

            Material expectedMaterial = CreateTemporaryMaterial();

            try
            {
                Type presetUtilityType = GetEditorAssemblyType("BC.Rendering.EnvironmentStylizedLitPresetUtility");
                MethodInfo applyPresetMethod = GetStaticMethod(presetUtilityType, "ApplyPreset", typeof(Material), typeof(string));
                applyPresetMethod.Invoke(null, new object[] { expectedMaterial, presetName });

                AssertPresetStateMatches(expectedMaterial, actualMaterial);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(expectedMaterial);
            }
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

        private static void AssertPresetStateMatches(Material expected, Material actual)
        {
            Assert.AreEqual(expected.GetFloat("_Cull"), actual.GetFloat("_Cull"));
            Assert.AreEqual(expected.GetFloat("_AlphaClip"), actual.GetFloat("_AlphaClip"));
            Assert.AreEqual(expected.GetFloat("_Cutoff"), actual.GetFloat("_Cutoff"));
            Assert.AreEqual(expected.GetFloat("_SpecularMode"), actual.GetFloat("_SpecularMode"));
            Assert.AreEqual(expected.GetFloat("_SpecularStrength"), actual.GetFloat("_SpecularStrength"));
            Assert.AreEqual(expected.GetFloat("_Smoothness"), actual.GetFloat("_Smoothness"));
            Assert.AreEqual(expected.GetFloat("_AmbientStrength"), actual.GetFloat("_AmbientStrength"));
            Assert.AreEqual(expected.GetFloat("_BandContrast"), actual.GetFloat("_BandContrast"));
            Assert.AreEqual(expected.GetFloat("_WrapLighting"), actual.GetFloat("_WrapLighting"));
            Assert.AreEqual(expected.GetFloat("_BandOffset"), actual.GetFloat("_BandOffset"));
            Assert.AreEqual(expected.GetFloat("_BounceStrength"), actual.GetFloat("_BounceStrength"));
            Assert.AreEqual(expected.GetFloat("_IndirectStrength"), actual.GetFloat("_IndirectStrength"));
            Assert.AreEqual(expected.GetFloat("_IndirectStylizeStrength"), actual.GetFloat("_IndirectStylizeStrength"));
            Assert.AreEqual(expected.GetFloat("_CavityStrength"), actual.GetFloat("_CavityStrength"));
            Assert.AreEqual(expected.GetFloat("_AdditionalLightMode"), actual.GetFloat("_AdditionalLightMode"));
            Assert.AreEqual(expected.GetFloat("_AdditionalLightIntensity"), actual.GetFloat("_AdditionalLightIntensity"));
            Assert.AreEqual(expected.GetFloat("_TriplanarBaseMapEnabled"), actual.GetFloat("_TriplanarBaseMapEnabled"));
            Assert.AreEqual(expected.GetFloat("_TriplanarNormalMapEnabled"), actual.GetFloat("_TriplanarNormalMapEnabled"));
            Assert.AreEqual(expected.GetFloat("_TriplanarNoiseEnabled"), actual.GetFloat("_TriplanarNoiseEnabled"));
            Assert.AreEqual(expected.GetFloat("_VertexColorEnabled"), actual.GetFloat("_VertexColorEnabled"));
            Assert.AreEqual(expected.GetFloat("_WorldYGradientEnabled"), actual.GetFloat("_WorldYGradientEnabled"));
            Assert.AreEqual(expected.GetFloat("_WorldYGradientStrength"), actual.GetFloat("_WorldYGradientStrength"));
            Assert.AreEqual(expected.GetFloat("_DebugView"), actual.GetFloat("_DebugView"));
            Assert.AreEqual(expected.GetColor("_BaseColor"), actual.GetColor("_BaseColor"));
            Assert.AreEqual(expected.GetColor("_DeepShadowColor"), actual.GetColor("_DeepShadowColor"));
            Assert.AreEqual(expected.GetColor("_ShadowColor"), actual.GetColor("_ShadowColor"));
            Assert.AreEqual(expected.GetColor("_MidColor"), actual.GetColor("_MidColor"));
            Assert.AreEqual(expected.GetColor("_LightColor"), actual.GetColor("_LightColor"));
            Assert.AreEqual(expected.GetColor("_HighlightColor"), actual.GetColor("_HighlightColor"));
            Assert.AreEqual(expected.GetColor("_AmbientTopColor"), actual.GetColor("_AmbientTopColor"));
            Assert.AreEqual(expected.GetColor("_AmbientSideColor"), actual.GetColor("_AmbientSideColor"));
            Assert.AreEqual(expected.GetColor("_AmbientBottomColor"), actual.GetColor("_AmbientBottomColor"));
            Assert.AreEqual(expected.GetColor("_BounceColor"), actual.GetColor("_BounceColor"));
            Assert.AreEqual(expected.GetColor("_IndirectShadowColor"), actual.GetColor("_IndirectShadowColor"));
            Assert.AreEqual(expected.GetColor("_SpecularColor"), actual.GetColor("_SpecularColor"));
            AssertTextureMatches(expected, actual, "_BaseMap");
            AssertTextureMatches(expected, actual, "_EmissionMap");
            AssertTextureMatches(expected, actual, "_NormalMap");
            AssertTextureMatches(expected, actual, "_OcclusionMap");
            Assert.AreEqual(expected.GetColor("_WorldYGradientTopColor"), actual.GetColor("_WorldYGradientTopColor"));
            Assert.AreEqual(expected.GetColor("_WorldYGradientBottomColor"), actual.GetColor("_WorldYGradientBottomColor"));
        }

        private static void AssertTextureMatches(Material expected, Material actual, string propertyName)
        {
            Texture expectedTexture = expected.GetTexture(propertyName);
            Texture actualTexture = actual.GetTexture(propertyName);

            Assert.AreEqual(AssetDatabase.GetAssetPath(expectedTexture), AssetDatabase.GetAssetPath(actualTexture), $"Unexpected texture state for {propertyName}.");
            Assert.AreEqual(expected.GetTextureScale(propertyName), actual.GetTextureScale(propertyName), $"Unexpected texture scale for {propertyName}.");
            Assert.AreEqual(expected.GetTextureOffset(propertyName), actual.GetTextureOffset(propertyName), $"Unexpected texture offset for {propertyName}.");
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

        private static void AssertHasComponentNamed(string objectName, string componentTypeName)
        {
            GameObject targetObject = GameObject.Find(objectName);
            Assert.IsNotNull(targetObject, $"Expected validation marker object: {objectName}");

            foreach (Component component in targetObject.GetComponents<Component>())
            {
                if (component != null && component.GetType().Name == componentTypeName)
                {
                    return;
                }
            }

            Assert.Fail($"Expected component {componentTypeName} on {objectName}.");
        }

        private static void AssertBeveledMesh(string objectName)
        {
            GameObject targetObject = GameObject.Find(objectName);
            Assert.IsNotNull(targetObject, $"Expected validation marker object: {objectName}");

            MeshFilter meshFilter = targetObject.GetComponent<MeshFilter>();
            Assert.IsNotNull(meshFilter, $"Expected MeshFilter on {objectName}");
            Assert.IsNotNull(meshFilter.sharedMesh, $"Expected mesh on {objectName}");
            Assert.GreaterOrEqual(meshFilter.sharedMesh.vertexCount, 24);
            Assert.Greater(meshFilter.sharedMesh.bounds.size.z, 0.05f);

            Vector3 firstNormal = meshFilter.sharedMesh.normals[0];
            bool foundDistinctNormal = false;
            foreach (Vector3 normal in meshFilter.sharedMesh.normals)
            {
                if (Vector3.Angle(firstNormal, normal) > 1f)
                {
                    foundDistinctNormal = true;
                    break;
                }
            }

            Assert.IsTrue(foundDistinctNormal, $"Expected varied normals on beveled validation mesh: {objectName}");
        }

        private static void AssertLightmapState(string objectName, bool expectedContributeGi)
        {
            GameObject targetObject = GameObject.Find(objectName);
            Assert.IsNotNull(targetObject, $"Expected validation marker object: {objectName}");

            MeshRenderer renderer = targetObject.GetComponent<MeshRenderer>();
            Assert.IsNotNull(renderer, $"Expected renderer on validation marker object: {objectName}");
            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(targetObject);

            if (expectedContributeGi)
            {
                Assert.IsTrue((flags & StaticEditorFlags.ContributeGI) != 0, $"Expected ContributeGI on {objectName}.");
                Assert.AreEqual(ReceiveGI.Lightmaps, renderer.receiveGI);
            }
            else
            {
                Assert.IsFalse((flags & StaticEditorFlags.ContributeGI) != 0, $"Did not expect ContributeGI on {objectName}.");
                Assert.AreEqual(-1, renderer.lightmapIndex, $"Did not expect baked lightmap payload on {objectName}.");
            }
        }

        private static void AssertMeshVertexCountAtLeast(string objectName, int minimumVertexCount)
        {
            GameObject targetObject = GameObject.Find(objectName);
            Assert.IsNotNull(targetObject, $"Expected validation marker object: {objectName}");

            MeshFilter meshFilter = targetObject.GetComponent<MeshFilter>();
            Assert.IsNotNull(meshFilter, $"Expected MeshFilter on {objectName}");
            Assert.IsNotNull(meshFilter.sharedMesh, $"Expected mesh on {objectName}");
            Assert.GreaterOrEqual(meshFilter.sharedMesh.vertexCount, minimumVertexCount, $"Expected stable mesh density on {objectName}.");
        }

        private static void AssertMinimumScale(string objectName, float minimumX, float minimumY)
        {
            GameObject targetObject = GameObject.Find(objectName);
            Assert.IsNotNull(targetObject, $"Expected validation marker object: {objectName}");
            Assert.GreaterOrEqual(targetObject.transform.localScale.x, minimumX);
            Assert.GreaterOrEqual(targetObject.transform.localScale.y, minimumY);
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

        private static void AssertOutsideLightRange(string objectName, string lightName, float margin)
        {
            GameObject targetObject = GameObject.Find(objectName);
            Assert.IsNotNull(targetObject, $"Expected validation marker object: {objectName}");

            GameObject lightObject = GameObject.Find(lightName);
            Assert.IsNotNull(lightObject, $"Expected validation light object: {lightName}");

            Light light = lightObject.GetComponent<Light>();
            Assert.IsNotNull(light, $"Expected Light component on validation light object: {lightName}");
            float distance = Vector3.Distance(targetObject.transform.position, lightObject.transform.position);
            Assert.Greater(distance, light.range + margin, $"Expected {objectName} to remain outside {lightName} range for directional-only review.");
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