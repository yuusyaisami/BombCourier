using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BC.Rendering.Tests
{
    public sealed class EnvironmentStylizedLitM2ValidationTests
    {
        private const string ShaderPath = "Assets/Art/Shader/EnvironmentStylizedLit/EnvironmentStylizedLit.shader";
        private const string DefaultMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_Default.mat";
        private const string InteriorMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_Interior.mat";
        private const string TestRoomScenePath = "Assets/Scenes/EnvironmentStylizedLit/ESL_TestRoom.unity";
        private const string LightingLabScenePath = "Assets/Scenes/EnvironmentStylizedLit/ESL_LightingLab.unity";

        [Test]
        public void M2ValidationAssetsExist()
        {
            Assert.IsTrue(File.Exists(ShaderPath), $"Expected EnvironmentStylizedLit shader asset: {ShaderPath}");
            Assert.IsTrue(File.Exists(DefaultMaterialPath), $"Expected default validation material: {DefaultMaterialPath}");
            Assert.IsTrue(File.Exists(InteriorMaterialPath), $"Expected interior validation material: {InteriorMaterialPath}");
            Assert.IsTrue(File.Exists(TestRoomScenePath), $"Expected M2 validation scene: {TestRoomScenePath}");
            Assert.IsTrue(File.Exists(LightingLabScenePath), $"Expected M2 validation scene: {LightingLabScenePath}");
        }

        [Test]
        public void M2ValidationMaterialsExposeExpectedContracts()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Material defaultMaterial = AssetDatabase.LoadAssetAtPath<Material>(DefaultMaterialPath);
            Material interiorMaterial = AssetDatabase.LoadAssetAtPath<Material>(InteriorMaterialPath);

            Assert.IsNotNull(shader, "EnvironmentStylizedLit shader asset must load.");
            Assert.AreSame(shader, defaultMaterial.shader);
            Assert.AreSame(shader, interiorMaterial.shader);
            Assert.IsTrue(defaultMaterial.HasProperty("_BaseMap"));
            Assert.IsTrue(interiorMaterial.HasProperty("_BaseMap"));

            AssertMaterialAssetContainsPropertyReference(DefaultMaterialPath, "_BaseMap");
            AssertMaterialAssetContainsPropertyReference(DefaultMaterialPath, "_NormalMap");
            AssertMaterialAssetContainsPropertyReference(DefaultMaterialPath, "_OcclusionMap");
            AssertMaterialAssetContainsPropertyReference(DefaultMaterialPath, "_EmissionMap");
            Assert.AreEqual(0f, defaultMaterial.GetFloat("_AlphaClip"));
            Assert.Greater(defaultMaterial.GetFloat("_NormalScale"), 0f);
            Assert.Greater(defaultMaterial.GetFloat("_EmissionStrength"), 0f);

            AssertMaterialAssetContainsPropertyReference(InteriorMaterialPath, "_BaseMap");
            AssertMaterialAssetContainsPropertyReference(InteriorMaterialPath, "_NormalMap");
            AssertMaterialAssetContainsPropertyReference(InteriorMaterialPath, "_OcclusionMap");
            AssertMaterialAssetContainsPropertyReference(InteriorMaterialPath, "_EmissionMap");
            Assert.AreEqual(1f, interiorMaterial.GetFloat("_AlphaClip"));
            Assert.AreEqual(0f, interiorMaterial.GetFloat("_NormalScale"));
            Assert.Greater(interiorMaterial.GetFloat("_Cutoff"), 0f);
        }

        [Test]
        public void TestRoomContainsM2MarkersAndAssignedValidationMaterials()
        {
            Material defaultMaterial = AssetDatabase.LoadAssetAtPath<Material>(DefaultMaterialPath);
            Material interiorMaterial = AssetDatabase.LoadAssetAtPath<Material>(InteriorMaterialPath);
            Scene scene = EditorSceneManager.OpenScene(TestRoomScenePath, OpenSceneMode.Single);

            Assert.IsTrue(scene.IsValid(), "ESL_TestRoom must open successfully.");
            Assert.IsNotNull(GameObject.Find("Main Camera"), "ESL_TestRoom must contain a Main Camera.");
            AssertRendererUsesMaterial("BaseMapQuad", defaultMaterial);
            AssertRendererUsesMaterial("AlphaClipQuad", interiorMaterial);
            AssertRendererUsesMaterial("EmissionSphere", defaultMaterial);
            AssertRendererUsesMaterial("NormalOnCube", defaultMaterial);
            AssertRendererUsesMaterial("NormalOffCube", interiorMaterial);
        }

        [Test]
        public void LightingLabContainsM2ComparisonMarkersAndAssignedValidationMaterials()
        {
            Material defaultMaterial = AssetDatabase.LoadAssetAtPath<Material>(DefaultMaterialPath);
            Material interiorMaterial = AssetDatabase.LoadAssetAtPath<Material>(InteriorMaterialPath);
            Scene scene = EditorSceneManager.OpenScene(LightingLabScenePath, OpenSceneMode.Single);

            Assert.IsTrue(scene.IsValid(), "ESL_LightingLab must open successfully.");
            Assert.IsNotNull(GameObject.Find("Main Camera"), "ESL_LightingLab must contain a Main Camera.");
            AssertRendererUsesMaterial("LightingDefaultSphere", defaultMaterial);
            AssertRendererUsesMaterial("LightingInteriorSphere", interiorMaterial);
            AssertRendererUsesMaterial("LightingEmissionCube", defaultMaterial);
            AssertRendererUsesMaterial("LightingAlphaClipQuad", interiorMaterial);
        }

        private static void AssertRendererUsesMaterial(string objectName, Material expectedMaterial)
        {
            GameObject targetObject = GameObject.Find(objectName);
            Assert.IsNotNull(targetObject, $"Expected validation marker object: {objectName}");

            Renderer renderer = targetObject.GetComponent<Renderer>();
            Assert.IsNotNull(renderer, $"Expected renderer on validation marker object: {objectName}");
            Assert.AreSame(expectedMaterial, renderer.sharedMaterial, $"Unexpected material on validation marker object: {objectName}");
        }

        private static void AssertMaterialAssetContainsPropertyReference(string materialPath, string propertyName)
        {
            string materialAssetText = File.ReadAllText(materialPath);
            StringAssert.Contains(propertyName + ":", materialAssetText, $"Expected property reference in material asset: {materialPath} -> {propertyName}");
            StringAssert.DoesNotContain(propertyName + ":\n        m_Texture: {fileID: 0}", materialAssetText, $"Expected non-null texture reference in material asset: {materialPath} -> {propertyName}");
        }
    }
}