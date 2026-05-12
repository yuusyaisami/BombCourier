using System.IO;
using BC.Rendering.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BC.Rendering.Tests
{
    public sealed class ToyDioramaValidationSceneTests
    {
        private const string SceneFolder = "Assets/Scenes/ToyDiorama";

        [Test]
        public void GenerateAllBatch_CreatesAllValidationScenes()
        {
            ToyDioramaValidationSceneGenerator.GenerateAllBatch();

            AssertSceneExists("ToyDiorama_ColorLab");
            AssertSceneExists("ToyDiorama_DepthLab");
            AssertSceneExists("ToyDiorama_BloomLab");
            AssertSceneExists("ToyDiorama_GameplayLab");
        }

        [Test]
        public void GameplayLab_ContainsMainCameraAndUiSurfaces()
        {
            ToyDioramaValidationSceneGenerator.GenerateAllBatch();

            string gameplayLabPath = GetScenePath("ToyDiorama_GameplayLab");
            Scene gameplayLabScene = EditorSceneManager.OpenScene(gameplayLabPath, OpenSceneMode.Single);

            Assert.IsTrue(gameplayLabScene.IsValid(), "GameplayLab scene must open successfully.");
            Assert.IsNotNull(GameObject.Find("Main Camera"), "GameplayLab must contain a Main Camera.");
            Assert.IsNotNull(GameObject.Find("UIScreenCanvas"), "GameplayLab must contain UIScreenCanvas.");
            Assert.IsNotNull(GameObject.Find("WorldSpaceCanvas"), "GameplayLab must contain WorldSpaceCanvas.");

            Canvas overlayCanvas = GameObject.Find("UIScreenCanvas").GetComponent<Canvas>();
            Canvas worldCanvas = GameObject.Find("WorldSpaceCanvas").GetComponent<Canvas>();

            Assert.IsNotNull(overlayCanvas);
            Assert.IsNotNull(worldCanvas);
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, overlayCanvas.renderMode);
            Assert.AreEqual(RenderMode.WorldSpace, worldCanvas.renderMode);
        }

        [TestCase("ToyDiorama_ColorLab", "PedestalTop")]
        [TestCase("ToyDiorama_DepthLab", "HorizonMarker")]
        [TestCase("ToyDiorama_BloomLab", "WarmBloomTarget")]
        [TestCase("ToyDiorama_GameplayLab", "CharacterProxy")]
        public void ValidationScenes_ContainRoleSpecificMarkers(string sceneName, string markerName)
        {
            ToyDioramaValidationSceneGenerator.GenerateAllBatch();

            Scene scene = EditorSceneManager.OpenScene(GetScenePath(sceneName), OpenSceneMode.Single);

            Assert.IsTrue(scene.IsValid(), $"Expected scene to open: {sceneName}");
            Assert.IsNotNull(GameObject.Find("Main Camera"), $"{sceneName} must contain a Main Camera.");
            Assert.IsNotNull(GameObject.Find("UIScreenCanvas"), $"{sceneName} must contain UIScreenCanvas.");
            Assert.IsNotNull(GameObject.Find("WorldSpaceCanvas"), $"{sceneName} must contain WorldSpaceCanvas.");
            Assert.IsNotNull(GameObject.Find(markerName), $"{sceneName} must contain role marker {markerName}.");
        }

        private static void AssertSceneExists(string sceneName)
        {
            Assert.IsTrue(File.Exists(GetScenePath(sceneName)), $"Expected validation scene to exist: {sceneName}");
        }

        private static string GetScenePath(string sceneName)
        {
            return Path.Combine(SceneFolder, sceneName + ".unity").Replace('\\', '/');
        }
    }
}