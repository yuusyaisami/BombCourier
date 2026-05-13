using System.Collections.Generic;
using System.Collections;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace BC.Rendering.PlayModeTests
{
    public sealed class ToyDioramaPostProcessPlayModeSmokeTests : IPrebuildSetup, IPostBuildCleanup
    {
        private const string GameplayLabScenePath = "Assets/Scenes/ToyDiorama/ToyDiorama_GameplayLab.unity";
        private const string MobileRenderPipelineAssetPath = "Assets/Settings/Mobile_RPAsset.asset";

#if UNITY_EDITOR
        private const string AddedGameplayLabSceneSessionKey = "BC.Rendering.PlayModeTests.AddedGameplayLabScene";
#endif

        private Scene loadedValidationScene;

        public void Setup()
        {
#if UNITY_EDITOR
            if (HasBuildSettingsScene(GameplayLabScenePath))
            {
                SessionState.SetBool(AddedGameplayLabSceneSessionKey, false);
                return;
            }

            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes)
            {
                new EditorBuildSettingsScene(GameplayLabScenePath, true)
            };

            EditorBuildSettings.scenes = scenes.ToArray();
            SessionState.SetBool(AddedGameplayLabSceneSessionKey, true);
#endif
        }

        public void Cleanup()
        {
#if UNITY_EDITOR
            if (!SessionState.GetBool(AddedGameplayLabSceneSessionKey, false))
            {
                return;
            }

            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.path == GameplayLabScenePath)
                {
                    continue;
                }

                scenes.Add(scene);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            SessionState.SetBool(AddedGameplayLabSceneSessionKey, false);
#endif
        }

        [UnityTearDown]
        public IEnumerator TearDownLoadedValidationScene()
        {
            if (loadedValidationScene.IsValid() && loadedValidationScene.isLoaded)
            {
                AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(loadedValidationScene);
                if (unloadOperation != null)
                {
                    yield return unloadOperation;
                }
            }

            loadedValidationScene = default;
        }

        [UnityTest]
        public IEnumerator RuntimeCamera_UsesCanonicalToyDioramaGameCameraPath()
        {
            GameObject runtimeCameraObject = new GameObject("ToyDioramaPlayModeCamera");
            GameObject uiScreenCanvasObject = new GameObject("UIScreenCanvas");
            Camera runtimeCamera = runtimeCameraObject.AddComponent<Camera>();
            UniversalAdditionalCameraData cameraData = runtimeCamera.GetUniversalAdditionalCameraData();
            Canvas uiScreenCanvas = uiScreenCanvasObject.AddComponent<Canvas>();

            try
            {
                uiScreenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

                yield return null;

                Assert.AreEqual(CameraType.Game, runtimeCamera.cameraType);
                Assert.AreEqual(CameraRenderType.Base, cameraData.renderType);
                Assert.Zero(cameraData.cameraStack.Count);
                Assert.IsNotNull(cameraData.scriptableRenderer, "Runtime camera must resolve a URP ScriptableRenderer.");

                Assert.AreEqual(RenderMode.ScreenSpaceOverlay, uiScreenCanvas.renderMode);
                Assert.IsNull(uiScreenCanvas.worldCamera);

                ToyDioramaPostProcessFeature feature = FindActiveToyDioramaFeature(cameraData.scriptableRenderer, out int activeLegacyFullScreenPassCount);
                Assert.IsNotNull(feature, "The runtime ScriptableRenderer must expose an active ToyDioramaPostProcessFeature.");
                Assert.IsTrue(feature.isActive, "ToyDiorama feature must remain active on the canonical renderer.");
                Assert.IsTrue(feature.Settings.Enabled, "ToyDiorama settings.Enabled must remain on for the canonical PC renderer.");
                Assert.Zero(activeLegacyFullScreenPassCount, "The runtime ScriptableRenderer must not keep an active FullScreenPassRendererFeature alongside ToyDiorama.");
                Assert.IsTrue(feature.ShouldApplyToCameraType(runtimeCamera.cameraType));
                Assert.IsFalse(feature.ShouldApplyToCameraType(CameraType.Preview));
            }
            finally
            {
                Object.DestroyImmediate(runtimeCameraObject);
                Object.DestroyImmediate(uiScreenCanvasObject);
            }
        }

        [UnityTest]
        public IEnumerator GameplayLabScene_UsesCanonicalToyDioramaGameCameraPath()
        {
            Assert.IsTrue(
                Application.CanStreamedLevelBeLoaded(GameplayLabScenePath),
                $"GameplayLab scene must be included in build settings for PlayMode smoke: {GameplayLabScenePath}");

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(GameplayLabScenePath, LoadSceneMode.Additive);
            Assert.IsNotNull(loadOperation, $"Failed to start additive load for {GameplayLabScenePath}.");

            yield return loadOperation;

            loadedValidationScene = SceneManager.GetSceneByPath(GameplayLabScenePath);
            Assert.IsTrue(loadedValidationScene.IsValid(), $"Loaded scene must be valid: {GameplayLabScenePath}");
            Assert.IsTrue(loadedValidationScene.isLoaded, $"Loaded scene must remain loaded: {GameplayLabScenePath}");

            GameObject mainCameraObject = FindGameObjectInScene(loadedValidationScene, "Main Camera");
            GameObject overlayCanvasObject = FindGameObjectInScene(loadedValidationScene, "UIScreenCanvas");
            GameObject worldCanvasObject = FindGameObjectInScene(loadedValidationScene, "WorldSpaceCanvas");
            GameObject characterProxyObject = FindGameObjectInScene(loadedValidationScene, "CharacterProxy");

            Assert.IsNotNull(mainCameraObject, "GameplayLab must contain Main Camera.");
            Assert.IsNotNull(overlayCanvasObject, "GameplayLab must contain UIScreenCanvas.");
            Assert.IsNotNull(worldCanvasObject, "GameplayLab must contain WorldSpaceCanvas.");
            Assert.IsNotNull(characterProxyObject, "GameplayLab must contain CharacterProxy.");

            Camera runtimeCamera = mainCameraObject.GetComponent<Camera>();
            Canvas overlayCanvas = overlayCanvasObject.GetComponent<Canvas>();
            Canvas worldCanvas = worldCanvasObject.GetComponent<Canvas>();
            UniversalAdditionalCameraData cameraData = runtimeCamera.GetUniversalAdditionalCameraData();

            yield return null;

            Assert.AreEqual(CameraType.Game, runtimeCamera.cameraType);
            Assert.AreEqual(CameraRenderType.Base, cameraData.renderType);
            Assert.Zero(cameraData.cameraStack.Count);
            Assert.IsNotNull(cameraData.scriptableRenderer, "GameplayLab camera must resolve a URP ScriptableRenderer.");

            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, overlayCanvas.renderMode);
            Assert.IsNull(overlayCanvas.worldCamera);
            Assert.AreEqual(RenderMode.WorldSpace, worldCanvas.renderMode);
            Assert.AreSame(runtimeCamera, worldCanvas.worldCamera);

            ToyDioramaPostProcessFeature feature = FindActiveToyDioramaFeature(cameraData.scriptableRenderer, out int activeLegacyFullScreenPassCount);
            Assert.IsNotNull(feature, "GameplayLab camera must expose an active ToyDioramaPostProcessFeature.");
            Assert.IsTrue(feature.isActive, "ToyDiorama feature must remain active on the canonical renderer.");
            Assert.IsTrue(feature.Settings.Enabled, "ToyDiorama settings.Enabled must remain on for the canonical PC renderer.");
            Assert.Zero(activeLegacyFullScreenPassCount, "GameplayLab renderer must not keep an active FullScreenPassRendererFeature alongside ToyDiorama.");
            Assert.IsTrue(feature.ShouldApplyToCameraType(runtimeCamera.cameraType));
        }

        [UnityTest]
        public IEnumerator GameplayLabScene_QualityTiersExposeExpectedRuntimePassTopology()
        {
            Assert.IsTrue(
                Application.CanStreamedLevelBeLoaded(GameplayLabScenePath),
                $"GameplayLab scene must be included in build settings for PlayMode topology validation: {GameplayLabScenePath}");

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(GameplayLabScenePath, LoadSceneMode.Additive);
            Assert.IsNotNull(loadOperation, $"Failed to start additive load for {GameplayLabScenePath}.");

            yield return loadOperation;

            loadedValidationScene = SceneManager.GetSceneByPath(GameplayLabScenePath);
            Assert.IsTrue(loadedValidationScene.IsValid(), $"Loaded scene must be valid: {GameplayLabScenePath}");

            GameObject mainCameraObject = FindGameObjectInScene(loadedValidationScene, "Main Camera");
            Assert.IsNotNull(mainCameraObject, "GameplayLab must contain Main Camera.");

            Camera runtimeCamera = mainCameraObject.GetComponent<Camera>();
            UniversalAdditionalCameraData cameraData = runtimeCamera.GetUniversalAdditionalCameraData();
            ToyDioramaPostProcessFeature feature = FindActiveToyDioramaFeature(cameraData.scriptableRenderer, out _);
            Assert.IsNotNull(feature, "GameplayLab camera must expose an active ToyDioramaPostProcessFeature.");

            ToyDioramaPostProcessSettings settings = feature.Settings;
            bool originalEnabled = settings.Enabled;
            ToyDioramaQualityTier originalQualityTier = settings.QualityTier;
            ToyDioramaDebugView originalDebugView = settings.DebugView;
            bool originalSoftBloomEnabled = settings.SoftBloomEnabled;
            float originalSoftBloomIntensity = settings.SoftBloomIntensity;
            bool originalHalationEnabled = settings.HalationEnabled;
            float originalHalationStrength = settings.HalationStrength;

            try
            {
                settings.Enabled = true;
                settings.DebugView = ToyDioramaDebugView.Off;
                settings.SoftBloomEnabled = true;
                settings.SoftBloomIntensity = 0.14f;
                settings.HalationEnabled = true;
                settings.HalationStrength = 0.04f;

                settings.QualityTier = ToyDioramaQualityTier.Low;
                AssertRuntimeQueueTopology(feature, ToyDioramaQualityTier.Low, 0, 2, 1);

                settings.QualityTier = ToyDioramaQualityTier.Medium;
                AssertRuntimeQueueTopology(feature, ToyDioramaQualityTier.Medium, 4, 6, 1);

                settings.QualityTier = ToyDioramaQualityTier.High;
                AssertRuntimeQueueTopology(feature, ToyDioramaQualityTier.High, 6, 8, 1);

                settings.QualityTier = ToyDioramaQualityTier.Cinematic;
                AssertRuntimeQueueTopology(feature, ToyDioramaQualityTier.Cinematic, 8, 10, 1);
            }
            finally
            {
                settings.Enabled = originalEnabled;
                settings.QualityTier = originalQualityTier;
                settings.DebugView = originalDebugView;
                settings.SoftBloomEnabled = originalSoftBloomEnabled;
                settings.SoftBloomIntensity = originalSoftBloomIntensity;
                settings.HalationEnabled = originalHalationEnabled;
                settings.HalationStrength = originalHalationStrength;
            }
        }

        [UnityTest]
        public IEnumerator GameplayLabScene_MobileRendererForcesLowRuntimeTopology()
        {
            RenderPipelineAsset originalDefaultRenderPipeline = GraphicsSettings.defaultRenderPipeline;
            RenderPipelineAsset originalQualityRenderPipeline = QualitySettings.renderPipeline;
            RenderPipelineAsset mobileRenderPipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(MobileRenderPipelineAssetPath);

            Assert.IsNotNull(mobileRenderPipeline, $"Mobile render pipeline asset must exist: {MobileRenderPipelineAssetPath}");

            GraphicsSettings.defaultRenderPipeline = mobileRenderPipeline;
            QualitySettings.renderPipeline = mobileRenderPipeline;
            yield return null;

            Assert.IsTrue(
                Application.CanStreamedLevelBeLoaded(GameplayLabScenePath),
                $"GameplayLab scene must be included in build settings for mobile renderer validation: {GameplayLabScenePath}");

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(GameplayLabScenePath, LoadSceneMode.Additive);
            Assert.IsNotNull(loadOperation, $"Failed to start additive load for {GameplayLabScenePath}.");

            yield return loadOperation;

            loadedValidationScene = SceneManager.GetSceneByPath(GameplayLabScenePath);
            Assert.IsTrue(loadedValidationScene.IsValid(), $"Loaded scene must be valid: {GameplayLabScenePath}");

            GameObject mainCameraObject = FindGameObjectInScene(loadedValidationScene, "Main Camera");
            Assert.IsNotNull(mainCameraObject, "GameplayLab must contain Main Camera.");

            Camera runtimeCamera = mainCameraObject.GetComponent<Camera>();
            UniversalAdditionalCameraData cameraData = runtimeCamera.GetUniversalAdditionalCameraData();
            ToyDioramaPostProcessFeature feature = FindActiveToyDioramaFeature(cameraData.scriptableRenderer, out _);
            Assert.IsNotNull(feature, "Mobile renderer path must expose an active ToyDioramaPostProcessFeature.");

            try
            {
                Assert.IsTrue(feature.ForceLowQualityTier, "Mobile renderer path must force Low quality tier.");
                Assert.AreEqual(ToyDioramaQualityTier.Low, feature.GetResolvedQualityTier());
                AssertRuntimeQueueTopology(feature, ToyDioramaQualityTier.Low, 0, 2, 1);
            }
            finally
            {
                QualitySettings.renderPipeline = originalQualityRenderPipeline;
                GraphicsSettings.defaultRenderPipeline = originalDefaultRenderPipeline;
            }
        }

        private static ToyDioramaPostProcessFeature FindActiveToyDioramaFeature(
            ScriptableRenderer renderer,
            out int activeLegacyFullScreenPassCount)
        {
            Assert.IsNotNull(renderer, "ScriptableRenderer must not be null.");

            FieldInfo rendererFeaturesField = typeof(ScriptableRenderer).GetField(
                "m_RendererFeatures",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(rendererFeaturesField, "ScriptableRenderer.m_RendererFeatures field was not found.");

            List<ScriptableRendererFeature> rendererFeatures = rendererFeaturesField.GetValue(renderer) as List<ScriptableRendererFeature>;
            Assert.IsNotNull(rendererFeatures, "Runtime renderer feature list must not be null.");

            activeLegacyFullScreenPassCount = 0;

            foreach (ScriptableRendererFeature rendererFeature in rendererFeatures)
            {
                if (rendererFeature is FullScreenPassRendererFeature fullScreenPassFeature && fullScreenPassFeature.isActive)
                {
                    activeLegacyFullScreenPassCount++;
                    continue;
                }

                if (rendererFeature is ToyDioramaPostProcessFeature feature && feature.isActive)
                {
                    return feature;
                }
            }

            return null;
        }

        private static GameObject FindGameObjectInScene(Scene scene, string objectName)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                GameObject match = FindGameObjectInHierarchy(rootObject.transform, objectName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static GameObject FindGameObjectInHierarchy(Transform root, string objectName)
        {
            if (root.name == objectName)
            {
                return root.gameObject;
            }

            for (int childIndex = 0; childIndex < root.childCount; childIndex++)
            {
                GameObject match = FindGameObjectInHierarchy(root.GetChild(childIndex), objectName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static void AssertRuntimeQueueTopology(
            ToyDioramaPostProcessFeature feature,
            ToyDioramaQualityTier expectedResolvedQualityTier,
            int expectedBloomRasterPassCount,
            int expectedTotalRasterPassCount,
            int expectedFinalCompositeRasterPassCount)
        {
            ToyDioramaPostProcessFeature.RuntimeQueuePlan queuePlan = feature.EvaluateRuntimeQueuePlan(CameraType.Game);

            Assert.IsTrue(queuePlan.RuntimeResourcesReady, "ToyDiorama runtime queue planning requires valid runtime resources.");
            Assert.IsTrue(queuePlan.AppliesToCameraType, "ToyDiorama should apply to the Game Camera on the canonical runtime path.");
            Assert.AreEqual(expectedResolvedQualityTier, queuePlan.ResolvedQualityTier);
            Assert.AreEqual(1, queuePlan.PreBloomPassCount, "ToyDiorama should always queue the pre-bloom pass when the effect is active on the Game Camera.");
            Assert.AreEqual(expectedBloomRasterPassCount, queuePlan.BloomRasterPassCount, "Bloom raster pass count drifted from the runtime feature queue plan.");
            Assert.AreEqual(expectedFinalCompositeRasterPassCount, queuePlan.FinalCompositePassCount, "Final composite queue plan drifted from the runtime feature branch.");
            Assert.AreEqual(expectedTotalRasterPassCount, queuePlan.TotalRasterPassCount, "Total raster pass count must be validated from the runtime feature queue plan, not only from helper methods.");
        }

#if UNITY_EDITOR
        private static bool HasBuildSettingsScene(string scenePath)
        {
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.path == scenePath)
                {
                    return true;
                }
            }

            return false;
        }
#endif
    }
}