using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BC.Rendering.Tests
{
    public sealed class ParticleMaterialSystemM16HardeningTests
    {
        private const string MilestonesPath = "Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemMilestones.md";
        private const string SpecPath = "Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemSpec.md";
        private const string ProgressPath = "Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemProgress.md";
        private const string UsageGuidePath = "Assets/Docs/ParticleUnitSpec/ParticleMaterialUsageGuide.md";
        private const string PresetGuidePath = "Assets/Docs/ParticleUnitSpec/ParticleMaterialPresetGuide.md";
        private const string PerformanceGuidePath = "Assets/Docs/ParticleUnitSpec/ParticleMaterialPerformanceGuide.md";
        private const string BootstrapperPath = "Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs";
        private const string BuildValidatorPath = "Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitBuildValidator.cs";
        private const string WebGlBuildUtilityPath = "Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitWebGlBuildUtility.cs";
        private const string ScenePath = "Assets/Scenes/Particles/ParticleMaterialTestScene.unity";

        [OneTimeSetUp]
        public void EnsureM16Harness()
        {
            Type bootstrapperType = GetEditorAssemblyType("BC.Rendering.ParticleUnlitValidationBootstrapper");
            MethodInfo bootstrapMethod = GetStaticMethod(bootstrapperType, "BootstrapM16ReviewHarness");
            bootstrapMethod.Invoke(null, null);
            AssetDatabase.Refresh();
        }

        [Test]
        public void M16SourceFilesAndDocsExist()
        {
            Assert.IsTrue(File.Exists(MilestonesPath), $"Expected milestones doc: {MilestonesPath}");
            Assert.IsTrue(File.Exists(SpecPath), $"Expected spec doc: {SpecPath}");
            Assert.IsTrue(File.Exists(ProgressPath), $"Expected progress doc: {ProgressPath}");
            Assert.IsTrue(File.Exists(UsageGuidePath), $"Expected usage guide: {UsageGuidePath}");
            Assert.IsTrue(File.Exists(PresetGuidePath), $"Expected preset guide: {PresetGuidePath}");
            Assert.IsTrue(File.Exists(PerformanceGuidePath), $"Expected performance guide: {PerformanceGuidePath}");
            Assert.IsTrue(File.Exists(BootstrapperPath), $"Expected ParticleUnlit bootstrapper: {BootstrapperPath}");
            Assert.IsTrue(File.Exists(BuildValidatorPath), $"Expected ParticleUnlit build validator: {BuildValidatorPath}");
            Assert.IsTrue(File.Exists(WebGlBuildUtilityPath), $"Expected ParticleUnlit WebGL build utility: {WebGlBuildUtilityPath}");
            Assert.IsTrue(File.Exists(ScenePath), $"Expected particle validation scene: {ScenePath}");
        }

        [Test]
        public void M16SourceDeclaresReviewHarnessAndValidationAliases()
        {
            string bootstrapperSource = File.ReadAllText(BootstrapperPath);
            string buildValidatorSource = File.ReadAllText(BuildValidatorPath);
            string buildUtilitySource = File.ReadAllText(WebGlBuildUtilityPath);

            StringAssert.Contains("BootstrapM16ReviewHarness", bootstrapperSource);
            StringAssert.Contains("ParticleMaterialReviewHarness", bootstrapperSource);
            StringAssert.Contains("ParticleM16_BrightBackdrop", bootstrapperSource);
            StringAssert.Contains("ParticleM16_DarkBackdrop", bootstrapperSource);
            StringAssert.Contains("ParticleRingUnlit_ShockwavePlaceholder", bootstrapperSource);
            StringAssert.Contains("ParticleGroundUnlit_GroundSmokePlaceholder", bootstrapperSource);
            StringAssert.Contains("EnsureM16ReviewHarnessScene", bootstrapperSource);
            StringAssert.Contains("EnsureM16ValidationAssetsReady", buildValidatorSource);
            StringAssert.Contains("RunM16WebGlValidationBuild", buildUtilitySource);
        }

        [Test]
        public void M16ReviewHarnessExistsInSceneWithoutBreakingLegacyMarkers()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid(), "ParticleMaterialTestScene must open successfully.");

            GameObject validationRoot = GameObject.Find("ParticleMaterialValidationRoot");
            Assert.IsNotNull(validationRoot, "Expected validation root.");
            Assert.AreEqual(8, validationRoot.transform.Cast<Transform>().Count(), "M16 must preserve the eight-marker validation root contract.");

            Assert.IsNotNull(GameObject.Find("Quality Tier Test Area"), "Expected standalone Quality Tier Test Area.");

            GameObject reviewHarnessRoot = GameObject.Find("ParticleMaterialReviewHarness");
            Assert.IsNotNull(reviewHarnessRoot, "Expected M16 review harness root.");
            Assert.IsNotNull(reviewHarnessRoot.transform.Find("ParticleM16_BrightBackdrop"), "Expected bright backdrop in M16 review harness.");
            Assert.IsNotNull(reviewHarnessRoot.transform.Find("ParticleM16_DarkBackdrop"), "Expected dark backdrop in M16 review harness.");
            Assert.IsNotNull(reviewHarnessRoot.transform.Find("ParticleRingUnlit_ShockwavePlaceholder"), "Expected Ring placeholder in M16 review harness.");
            Assert.IsNotNull(reviewHarnessRoot.transform.Find("ParticleGroundUnlit_GroundSmokePlaceholder"), "Expected Ground placeholder in M16 review harness.");
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

        private static MethodInfo GetStaticMethod(Type ownerType, string methodName, params Type[] parameterTypes)
        {
            MethodInfo method = parameterTypes == null || parameterTypes.Length == 0
                ? ownerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                : ownerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, parameterTypes, null);

            Assert.IsNotNull(method, $"Expected static method on {ownerType.FullName}: {methodName}");
            return method;
        }
    }
}