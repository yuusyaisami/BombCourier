using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BC.Rendering.PlayModeTests
{
    public sealed class ParticleMaterialSystemPlayModeSmokeTests : IPrebuildSetup, IPostBuildCleanup
    {
        private const string ValidationScenePath = "Assets/Scenes/Particles/ParticleMaterialTestScene.unity";

#if UNITY_EDITOR
        private const string AddedValidationSceneSessionKey = "BC.Rendering.PlayModeTests.AddedParticleValidationScene";
#endif

        private Scene loadedValidationScene;

        public void Setup()
        {
#if UNITY_EDITOR
            Type bootstrapperType = GetEditorAssemblyType("BC.Rendering.ParticleUnlitValidationBootstrapper");
            MethodInfo bootstrapMethod = GetStaticMethod(bootstrapperType, "BootstrapM16ReviewHarness");
            bootstrapMethod.Invoke(null, null);
            AssetDatabase.Refresh();

            if (HasBuildSettingsScene(ValidationScenePath))
            {
                SessionState.SetBool(AddedValidationSceneSessionKey, false);
                return;
            }

            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes)
            {
                new EditorBuildSettingsScene(ValidationScenePath, true)
            };

            EditorBuildSettings.scenes = scenes.ToArray();
            SessionState.SetBool(AddedValidationSceneSessionKey, true);
#endif
        }

        public void Cleanup()
        {
#if UNITY_EDITOR
            if (!SessionState.GetBool(AddedValidationSceneSessionKey, false))
            {
                return;
            }

            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.path == ValidationScenePath)
                {
                    continue;
                }

                scenes.Add(scene);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            SessionState.SetBool(AddedValidationSceneSessionKey, false);
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
        public IEnumerator ParticleMaterialTestScene_LoadsRepresentativeReviewAreas()
        {
            Assert.IsTrue(
                Application.CanStreamedLevelBeLoaded(ValidationScenePath),
                $"Validation scene must be included in build settings for PlayMode smoke: {ValidationScenePath}");

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(ValidationScenePath, LoadSceneMode.Additive);
            Assert.IsNotNull(loadOperation, $"Failed to start additive load for {ValidationScenePath}.");

            yield return loadOperation;

            loadedValidationScene = SceneManager.GetSceneByPath(ValidationScenePath);
            Assert.IsTrue(loadedValidationScene.IsValid(), $"Loaded scene must be valid: {ValidationScenePath}");
            Assert.IsTrue(loadedValidationScene.isLoaded, $"Loaded scene must remain loaded: {ValidationScenePath}");

            yield return null;

            Assert.IsNotNull(FindGameObjectInLoadedScene("Main Camera"), "Validation scene must contain Main Camera.");
            Assert.IsNotNull(FindGameObjectInLoadedScene("Directional Light"), "Validation scene must contain Directional Light.");
            Assert.IsNotNull(FindGameObjectInLoadedScene("ValidationFloor"), "Validation scene must contain ValidationFloor.");
            Assert.IsNotNull(FindGameObjectInLoadedScene("ParticleMaterialValidationRoot"), "Validation scene must contain ParticleMaterialValidationRoot.");
            Assert.IsNotNull(FindGameObjectInLoadedScene("Quality Tier Test Area"), "Validation scene must contain Quality Tier Test Area.");
            Assert.IsNotNull(FindGameObjectInLoadedScene("Future Lit Test Area"), "Validation scene must contain Future Lit Test Area.");
            Assert.IsNotNull(FindGameObjectInLoadedScene("Future Distortion Test Area"), "Validation scene must contain Future Distortion Test Area.");
            Assert.IsNotNull(FindGameObjectInLoadedScene("ParticleMaterialReviewHarness"), "Validation scene must contain the M16 review harness.");
            Assert.IsNotNull(FindGameObjectInLoadedScene("ParticleM16_BrightBackdrop"), "Validation scene must contain the M16 bright backdrop.");
            Assert.IsNotNull(FindGameObjectInLoadedScene("ParticleM16_DarkBackdrop"), "Validation scene must contain the M16 dark backdrop.");
            Assert.IsNotNull(FindGameObjectInLoadedScene("ParticleRingUnlit_ShockwavePlaceholder"), "Validation scene must contain the Ring placeholder.");
            Assert.IsNotNull(FindGameObjectInLoadedScene("ParticleGroundUnlit_GroundSmokePlaceholder"), "Validation scene must contain the Ground placeholder.");
        }

        [UnityTest]
        public IEnumerator ParticleMaterialTestScene_LoadsRepresentativeParticleObjects()
        {
            Assert.IsTrue(
                Application.CanStreamedLevelBeLoaded(ValidationScenePath),
                $"Validation scene must be included in build settings for PlayMode smoke: {ValidationScenePath}");

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(ValidationScenePath, LoadSceneMode.Additive);
            Assert.IsNotNull(loadOperation, $"Failed to start additive load for {ValidationScenePath}.");

            yield return loadOperation;

            loadedValidationScene = SceneManager.GetSceneByPath(ValidationScenePath);
            Assert.IsTrue(loadedValidationScene.IsValid(), $"Loaded scene must be valid: {ValidationScenePath}");

            yield return null;

            AssertParticleObjectHasMaterial("FX_Particle_Dust_Preview");
            AssertParticleObjectHasMaterial("FX_Particle_MagicCustomData_Preview");
            AssertParticleObjectHasMaterial("ParticleUnlit_TierHighValidation");
            AssertParticleObjectHasMaterial("ParticleLit_BubbleValidation");
            AssertParticleObjectHasMaterial("ParticleDistortion_MagicWarpValidation");
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

            Assert.Fail($"Expected editor type to exist: {fullTypeName}");
            return null;
        }

        private static MethodInfo GetStaticMethod(Type ownerType, string methodName)
        {
            MethodInfo method = ownerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, $"Expected static method on {ownerType.FullName}: {methodName}");
            return method;
        }
#endif

        private GameObject FindGameObjectInLoadedScene(string objectName)
        {
            if (!loadedValidationScene.IsValid() || !loadedValidationScene.isLoaded)
            {
                return null;
            }

            foreach (GameObject rootObject in loadedValidationScene.GetRootGameObjects())
            {
                Transform match = rootObject.transform.Find(objectName);
                if (match != null)
                {
                    return match.gameObject;
                }

                if (rootObject.name == objectName)
                {
                    return rootObject;
                }

                Transform nestedMatch = rootObject.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(transform => transform.name == objectName);
                if (nestedMatch != null)
                {
                    return nestedMatch.gameObject;
                }
            }

            return null;
        }

        private void AssertParticleObjectHasMaterial(string objectName)
        {
            GameObject particleObject = FindGameObjectInLoadedScene(objectName);
            Assert.IsNotNull(particleObject, $"Expected particle object in PlayMode smoke: {objectName}");

            ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();
            Assert.IsNotNull(renderer, $"Particle object must have ParticleSystemRenderer: {objectName}");
            Assert.IsNotNull(renderer.sharedMaterial, $"Particle object must keep a shared material assignment: {objectName}");
        }
    }
}