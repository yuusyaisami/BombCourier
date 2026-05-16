using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BC.Rendering.Tests
{
    public sealed class ParticleUnlitQualityTierValidationTests
    {
        private const string ShaderPath = "Assets/Art/Shader/Particles/ParticleUnlit/BC_Particles_ParticleUnlit.shader";
        private const string SpecPath = "Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemSpec.md";
        private const string MilestonesPath = "Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemMilestones.md";
        private const string BootstrapperPath = "Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs";
        private const string BuildValidatorPath = "Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitBuildValidator.cs";
        private const string WebGlBuildUtilityPath = "Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitWebGlBuildUtility.cs";
        private const string ShaderGuiPath = "Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitShaderGUI.cs";
        private const string ValidatorPath = "Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitMaterialValidator.cs";
        private const string TierUtilityPath = "Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitQualityTierUtility.cs";
        private const string ScenePath = "Assets/Scenes/Particles/ParticleMaterialTestScene.unity";
        private const string TierLowMaterialPath = "Assets/Art/Materials/Particles/Unlit/M_Particle_Test_TierLow_Alpha.mat";
        private const string TierMediumMaterialPath = "Assets/Art/Materials/Particles/Unlit/M_Particle_Test_TierMedium_Alpha.mat";
        private const string TierHighMaterialPath = "Assets/Art/Materials/Particles/Unlit/M_Particle_Test_TierHigh_Alpha.mat";

        [OneTimeSetUp]
        public void EnsureValidationAssets()
        {
            Type bootstrapperType = GetEditorAssemblyType("BC.Rendering.ParticleUnlitValidationBootstrapper");
            MethodInfo bootstrapMethod = GetStaticMethod(bootstrapperType, "BootstrapM15QualityTierValidationAssets");
            bootstrapMethod.Invoke(null, null);
            AssetDatabase.Refresh();
        }

        [Test]
        public void SourceFilesAndValidationAssetsExistForM15()
        {
            Assert.IsTrue(File.Exists(ShaderPath), $"Expected ParticleUnlit shader: {ShaderPath}");
            Assert.IsTrue(File.Exists(SpecPath), $"Expected particle spec: {SpecPath}");
            Assert.IsTrue(File.Exists(MilestonesPath), $"Expected milestone spec: {MilestonesPath}");
            Assert.IsTrue(File.Exists(BootstrapperPath), $"Expected ParticleUnlit bootstrapper: {BootstrapperPath}");
            Assert.IsTrue(File.Exists(BuildValidatorPath), $"Expected ParticleUnlit build validator: {BuildValidatorPath}");
            Assert.IsTrue(File.Exists(WebGlBuildUtilityPath), $"Expected ParticleUnlit WebGL build utility: {WebGlBuildUtilityPath}");
            Assert.IsTrue(File.Exists(ShaderGuiPath), $"Expected ParticleUnlit ShaderGUI: {ShaderGuiPath}");
            Assert.IsTrue(File.Exists(ValidatorPath), $"Expected ParticleUnlit validator: {ValidatorPath}");
            Assert.IsTrue(File.Exists(TierUtilityPath), $"Expected ParticleUnlit quality tier utility: {TierUtilityPath}");
            Assert.IsTrue(File.Exists(TierLowMaterialPath), $"Expected M15 low-tier validation material: {TierLowMaterialPath}");
            Assert.IsTrue(File.Exists(TierMediumMaterialPath), $"Expected M15 medium-tier validation material: {TierMediumMaterialPath}");
            Assert.IsTrue(File.Exists(TierHighMaterialPath), $"Expected M15 high-tier validation material: {TierHighMaterialPath}");
        }

        [Test]
        public void M15SourceDeclaresTierAndBuildContracts()
        {
            string shaderSource = File.ReadAllText(ShaderPath);
            string shaderGuiSource = File.ReadAllText(ShaderGuiPath);
            string validatorSource = File.ReadAllText(ValidatorPath);
            string tierUtilitySource = File.ReadAllText(TierUtilityPath);
            string buildValidatorSource = File.ReadAllText(BuildValidatorPath);
            string buildUtilitySource = File.ReadAllText(WebGlBuildUtilityPath);
            string bootstrapperSource = File.ReadAllText(BootstrapperPath);

            StringAssert.Contains("_QualityTier", shaderSource);
            StringAssert.Contains("ParticleUnlitQualityTier", tierUtilitySource);
            StringAssert.Contains("GetTierNames", tierUtilitySource);
            StringAssert.Contains("ApplyTier", tierUtilitySource);
            StringAssert.Contains("TryInferTier", tierUtilitySource);
            StringAssert.Contains("MatchesTier", tierUtilitySource);
            StringAssert.Contains("DrawQualityTierSection", shaderGuiSource);
            StringAssert.Contains("Apply Selected Tier", shaderGuiSource);
            StringAssert.Contains("TryGetTierSummary", validatorSource);
            StringAssert.Contains("TryGetTierMismatchWarning", validatorSource);
            StringAssert.Contains("TryGetWebGlTierWarning", validatorSource);
            StringAssert.Contains("EnsureM15ValidationAssetsReady", buildValidatorSource);
            StringAssert.Contains("TryGetWebGlTierBuildError", buildValidatorSource);
            StringAssert.Contains("TryGetTierDescription", buildValidatorSource);
            StringAssert.Contains("RunM15WebGlValidationBuild", buildUtilitySource);
            StringAssert.Contains("BootstrapM15QualityTierValidationAssets", bootstrapperSource);
            StringAssert.Contains("Quality Tier Test Area", bootstrapperSource);
            StringAssert.Contains("M_Particle_Test_TierLow_Alpha.mat", bootstrapperSource);
            StringAssert.Contains("M_Particle_Test_TierMedium_Alpha.mat", bootstrapperSource);
            StringAssert.Contains("M_Particle_Test_TierHigh_Alpha.mat", bootstrapperSource);
        }

        [Test]
        public void TierUtilityAppliesAndInfersCanonicalTiers()
        {
            Material lowMaterial = CreateTemporaryMaterial();
            Material mediumMaterial = CreateTemporaryMaterial();
            Material highMaterial = CreateTemporaryMaterial();
            Material depthOnlyMaterial = CreateTemporaryMaterial();

            try
            {
                Type tierUtilityType = GetEditorAssemblyType("BC.Rendering.ParticleUnlitQualityTierUtility");
                MethodInfo getTierNamesMethod = GetStaticMethod(tierUtilityType, "GetTierNames");
                MethodInfo applyTierMethod = GetStaticMethod(tierUtilityType, "ApplyTier", typeof(Material), typeof(int));
                MethodInfo tryInferTierMethod = GetStaticMethod(tierUtilityType, "TryInferTier", typeof(Material), tierUtilityType.Assembly.GetType("BC.Rendering.ParticleUnlitQualityTier").MakeByRefType());

                CollectionAssert.AreEqual(new[] { "Low", "Medium", "High" }, (string[])getTierNamesMethod.Invoke(null, null));

                Assert.IsTrue((bool)applyTierMethod.Invoke(null, new object[] { lowMaterial, 0 }));
                Assert.IsTrue((bool)applyTierMethod.Invoke(null, new object[] { mediumMaterial, 1 }));
                Assert.IsTrue((bool)applyTierMethod.Invoke(null, new object[] { highMaterial, 2 }));

                Assert.AreEqual(0f, lowMaterial.GetFloat("_QualityTier"));
                Assert.AreEqual(0f, lowMaterial.GetFloat("_MaskStrength"));
                Assert.AreEqual(0f, lowMaterial.GetFloat("_UseSoftParticles"));

                Assert.AreEqual(1f, mediumMaterial.GetFloat("_QualityTier"));
                Assert.Greater(mediumMaterial.GetFloat("_MaskStrength"), 0f);
                Assert.Greater(mediumMaterial.GetFloat("_EmissionStrength"), 0f);
                Assert.AreEqual(0f, mediumMaterial.GetFloat("_UseSoftParticles"));

                Assert.AreEqual(2f, highMaterial.GetFloat("_QualityTier"));
                Assert.AreEqual(1f, highMaterial.GetFloat("_UseSoftParticles"));
                Assert.AreEqual(1f, highMaterial.GetFloat("_UseCameraFade"));

                AssertTierInference(tryInferTierMethod, lowMaterial, "Low");
                AssertTierInference(tryInferTierMethod, mediumMaterial, "Medium");
                AssertTierInference(tryInferTierMethod, highMaterial, "High");

                depthOnlyMaterial.SetFloat("_UseSoftParticles", 1f);
                AssertTierDoesNotInfer(tryInferTierMethod, depthOnlyMaterial);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(lowMaterial);
                UnityEngine.Object.DestroyImmediate(mediumMaterial);
                UnityEngine.Object.DestroyImmediate(highMaterial);
                UnityEngine.Object.DestroyImmediate(depthOnlyMaterial);
            }
        }

        [Test]
        public void ValidatorAndBuildPolicyWarnAboutTierMismatchAndWebGlHighTier()
        {
            Material material = CreateTemporaryMaterial();
            Material depthOnlyMaterial = CreateTemporaryMaterial();

            try
            {
                Type tierUtilityType = GetEditorAssemblyType("BC.Rendering.ParticleUnlitQualityTierUtility");
                MethodInfo applyTierMethod = GetStaticMethod(tierUtilityType, "ApplyTier", typeof(Material), typeof(int));

                Type validatorType = GetEditorAssemblyType("BC.Rendering.ParticleUnlitMaterialValidator");
                MethodInfo mismatchMethod = GetStaticMethod(validatorType, "TryGetTierMismatchWarning", typeof(Material), typeof(string).MakeByRefType());
                MethodInfo webGlWarningMethod = GetStaticMethod(validatorType, "TryGetWebGlTierWarning", typeof(Material), typeof(string).MakeByRefType());

                Type buildValidatorType = GetEditorAssemblyType("BC.Rendering.ParticleUnlitBuildValidator");
                MethodInfo webGlBuildErrorMethod = GetStaticMethod(buildValidatorType, "TryGetWebGlTierBuildError", typeof(Material), typeof(string).MakeByRefType());

                applyTierMethod.Invoke(null, new object[] { material, 2 });
                material.SetFloat("_QualityTier", 0f);

                object[] mismatchArguments = { material, null };
                Assert.IsTrue((bool)mismatchMethod.Invoke(null, mismatchArguments));
                StringAssert.Contains("Authored Quality Tier", mismatchArguments[1] as string);

                object[] webGlWarningArguments = { material, null };
                Assert.IsTrue((bool)webGlWarningMethod.Invoke(null, webGlWarningArguments));
                StringAssert.Contains("standard WebGL path", webGlWarningArguments[1] as string);

                object[] webGlBuildArguments = { material, null };
                Assert.IsTrue((bool)webGlBuildErrorMethod.Invoke(null, webGlBuildArguments));
                StringAssert.Contains("high-tier boundary features", webGlBuildArguments[1] as string);

                depthOnlyMaterial.SetFloat("_UseSoftParticles", 1f);

                object[] depthOnlyWebGlWarningArguments = { depthOnlyMaterial, null };
                Assert.IsTrue((bool)webGlWarningMethod.Invoke(null, depthOnlyWebGlWarningArguments));

                object[] depthOnlyWebGlBuildArguments = { depthOnlyMaterial, null };
                Assert.IsTrue((bool)webGlBuildErrorMethod.Invoke(null, depthOnlyWebGlBuildArguments));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(depthOnlyMaterial);
            }
        }

        [Test]
        public void GeneratedTierMaterialsAndSceneAnchorsExist()
        {
            Material lowMaterial = AssetDatabase.LoadAssetAtPath<Material>(TierLowMaterialPath);
            Material mediumMaterial = AssetDatabase.LoadAssetAtPath<Material>(TierMediumMaterialPath);
            Material highMaterial = AssetDatabase.LoadAssetAtPath<Material>(TierHighMaterialPath);

            Assert.IsNotNull(lowMaterial, "Expected low-tier validation material asset.");
            Assert.IsNotNull(mediumMaterial, "Expected medium-tier validation material asset.");
            Assert.IsNotNull(highMaterial, "Expected high-tier validation material asset.");
            Assert.AreEqual(0f, lowMaterial.GetFloat("_QualityTier"));
            Assert.AreEqual(1f, mediumMaterial.GetFloat("_QualityTier"));
            Assert.AreEqual(2f, highMaterial.GetFloat("_QualityTier"));

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid(), "ParticleMaterialTestScene must open successfully.");
            Assert.IsNotNull(GameObject.Find("Quality Tier Test Area"), "Expected M15 quality tier marker.");
            AssertParticleAnchorUsesMaterial("Quality Tier Test Area", "ParticleUnlit_TierLowValidation", lowMaterial);
            AssertParticleAnchorUsesMaterial("Quality Tier Test Area", "ParticleUnlit_TierMediumValidation", mediumMaterial);
            AssertParticleAnchorUsesMaterial("Quality Tier Test Area", "ParticleUnlit_TierHighValidation", highMaterial);
        }

        private static Material CreateTemporaryMaterial()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Assert.IsNotNull(shader, "ParticleUnlit shader asset must load.");
            return new Material(shader);
        }

        private static void AssertTierInference(MethodInfo tryInferTierMethod, Material material, string expectedName)
        {
            object tierEnum = Enum.ToObject(tryInferTierMethod.GetParameters()[1].ParameterType.GetElementType(), 0);
            object[] arguments = { material, tierEnum };
            Assert.IsTrue((bool)tryInferTierMethod.Invoke(null, arguments));
            Assert.AreEqual(expectedName, arguments[1].ToString());
        }

        private static void AssertTierDoesNotInfer(MethodInfo tryInferTierMethod, Material material)
        {
            object tierEnum = Enum.ToObject(tryInferTierMethod.GetParameters()[1].ParameterType.GetElementType(), 0);
            object[] arguments = { material, tierEnum };
            Assert.IsFalse((bool)tryInferTierMethod.Invoke(null, arguments));
        }

        private static void AssertParticleAnchorUsesMaterial(string markerName, string objectName, Material expectedMaterial)
        {
            GameObject marker = GameObject.Find(markerName);
            Assert.IsNotNull(marker, $"Expected marker object: {markerName}");
            Transform child = marker.transform.Find(objectName);
            Assert.IsNotNull(child, $"Expected validation anchor: {objectName}");

            ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
            Assert.IsNotNull(renderer, $"Validation anchor must have ParticleSystemRenderer: {objectName}");
            Assert.AreSame(expectedMaterial, renderer.sharedMaterial, $"Validation anchor must use the expected material: {objectName}");
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