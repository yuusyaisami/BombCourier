using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BC.Rendering.Tests
{
    public sealed class ParticleUnlitValidationTests
    {
        private const string ShaderPath = "Assets/Art/Shader/Particles/ParticleUnlit/BC_Particles_ParticleUnlit.shader";
        private const string SpecPath = "Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemSpec.md";
        private const string BootstrapperPath = "Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs";
        private const string BuildValidatorPath = "Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitBuildValidator.cs";
        private const string WebGlBuildUtilityPath = "Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitWebGlBuildUtility.cs";
        private const string ShaderGuiPath = "Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitShaderGUI.cs";
        private const string ValidatorPath = "Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitMaterialValidator.cs";
        private const string PresetUtilityPath = "Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitPresetUtility.cs";
        private const string InputHlslPath = "Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Input.hlsl";
        private const string CommonHlslPath = "Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Common.hlsl";
        private const string SamplingHlslPath = "Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Sampling.hlsl";
        private const string DebugHlslPath = "Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Debug.hlsl";
        private const string SurfaceHlslPath = "Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Surface.hlsl";
        private const string ForwardPassHlslPath = "Assets/Art/Shader/Particles/ParticleUnlit/HLSL/Passes/ParticleUnlit_ForwardPass.hlsl";
        private const string TexturePath = "Assets/Art/Textures/Particles/Unlit/T_Particle_TestSoftSprite.png";
        private const string SmokeFlipbookTexturePath = "Assets/Art/Textures/Particles/Unlit/T_Particle_SmokeFlipbook_4x4.png";
        private const string MagicBurstFlipbookTexturePath = "Assets/Art/Textures/Particles/Unlit/T_Particle_MagicBurstFlipbook_4x4.png";
        private const string ExplosionPlaceholderFlipbookTexturePath = "Assets/Art/Textures/Particles/Unlit/T_Particle_ExplosionPlaceholder_4x4.png";
        private const string SoftCloudNoisePath = "Assets/Art/Textures/Particles/Shared/T_Noise_SoftCloud.png";
        private const string DissolveNoisePath = "Assets/Art/Textures/Particles/Shared/T_Noise_Dissolve.png";
        private const string ParticleMaskPath = "Assets/Art/Textures/Particles/Shared/T_Mask_Particle_Test_RGBA.png";
        private const string MaterialPath = "Assets/Art/Materials/Particles/Unlit/M_Particle_Test_Alpha.mat";
        private const string DustMaterialPath = "Assets/Art/Materials/Particles/Unlit/M_Particle_Dust_Alpha.mat";
        private const string GlowMaterialPath = "Assets/Art/Materials/Particles/Unlit/M_Particle_Glow_Premultiply.mat";
        private const string SparkMaterialPath = "Assets/Art/Materials/Particles/Unlit/M_Particle_Spark_Additive.mat";
        private const string SmokeMaterialPath = "Assets/Art/Materials/Particles/Unlit/M_Particle_Smoke_Alpha.mat";
        private const string MagicMaterialPath = "Assets/Art/Materials/Particles/Unlit/M_Particle_Magic_Additive.mat";
        private const string SmokeFlipbookMaterialPath = "Assets/Art/Materials/Particles/Unlit/M_Particle_SmokeFlipbook_Alpha.mat";
        private const string MagicBurstMaterialPath = "Assets/Art/Materials/Particles/Unlit/M_Particle_MagicBurst_Additive.mat";
        private const string DustDepthValidationMaterialPath = "Assets/Art/Materials/Particles/Unlit/M_Particle_Test_DustSoftParticles_Alpha.mat";
        private const string SmokeDepthValidationMaterialPath = "Assets/Art/Materials/Particles/Unlit/M_Particle_Test_SmokeSoftParticles_Alpha.mat";
        private const string GlowCameraFadeValidationMaterialPath = "Assets/Art/Materials/Particles/Unlit/M_Particle_Test_GlowCameraFade_Premultiply.mat";
        private const string DustPrefabPath = "Assets/Art/Prefab/Particles/Unlit/FX_Particle_Dust.prefab";
        private const string SmokePrefabPath = "Assets/Art/Prefab/Particles/Unlit/FX_Particle_Smoke.prefab";
        private const string GlowPrefabPath = "Assets/Art/Prefab/Particles/Unlit/FX_Particle_Glow.prefab";
        private const string SparkPrefabPath = "Assets/Art/Prefab/Particles/Unlit/FX_Particle_Spark.prefab";
        private const string MagicPrefabPath = "Assets/Art/Prefab/Particles/Unlit/FX_Particle_Magic.prefab";
        private const string SmokeFlipbookPrefabPath = "Assets/Art/Prefab/Particles/Unlit/FX_Particle_SmokeFlipbook.prefab";
        private const string MagicBurstPrefabPath = "Assets/Art/Prefab/Particles/Unlit/FX_Particle_MagicBurst.prefab";
        private const string SparkCustomDataPrefabPath = "Assets/Art/Prefab/Particles/Unlit/FX_Particle_SparkCustomData.prefab";
        private const string MagicCustomDataPrefabPath = "Assets/Art/Prefab/Particles/Unlit/FX_Particle_MagicCustomData.prefab";
        private const string ScenePath = "Assets/Scenes/Particles/ParticleMaterialTestScene.unity";
        private const string DustPreviewObjectName = "FX_Particle_Dust_Preview";
        private const string SmokePreviewObjectName = "FX_Particle_Smoke_Preview";
        private const string GlowPreviewObjectName = "FX_Particle_Glow_Preview";
        private const string SparkPreviewObjectName = "FX_Particle_Spark_Preview";
        private const string MagicPreviewObjectName = "FX_Particle_Magic_Preview";
        private const string SmokeFlipbookPreviewObjectName = "FX_Particle_SmokeFlipbook_Preview";
        private const string MagicBurstPreviewObjectName = "FX_Particle_MagicBurst_Preview";
        private const string SparkCustomDataPreviewObjectName = "FX_Particle_SparkCustomData_Preview";
        private const string MagicCustomDataPreviewObjectName = "FX_Particle_MagicCustomData_Preview";
        private const string DustDepthValidationObjectName = "ParticleUnlit_DustSoftParticlesValidation";
        private const string SmokeDepthValidationObjectName = "ParticleUnlit_SmokeSoftParticlesValidation";
        private const string GlowCameraFadeValidationObjectName = "ParticleUnlit_GlowCameraFadeValidation";
        private const string DepthInteractionWallObjectName = "ParticleUnlit_DepthInteractionWall";
        private const string WebGlDust100CaseName = "ParticleUnlit_WebGLDust100";
        private const string WebGlDust300CaseName = "ParticleUnlit_WebGLDust300";
        private const string WebGlSmoke50CaseName = "ParticleUnlit_WebGLSmoke50";
        private const string WebGlSpark200CaseName = "ParticleUnlit_WebGLSpark200";
        private const string WebGlGlow100CaseName = "ParticleUnlit_WebGLGlow100";
        private const string WebGlMagic100CaseName = "ParticleUnlit_WebGLMagic100";
        private const string WebGlMixedCaseName = "ParticleUnlit_WebGLMixed";

        [OneTimeSetUp]
        public void EnsureValidationAssets()
        {
            Type bootstrapperType = GetEditorAssemblyType("BC.Rendering.ParticleUnlitValidationBootstrapper");
            MethodInfo bootstrapMethod = GetStaticMethod(bootstrapperType, "BootstrapM11DepthInteractionValidationAssets");
            bootstrapMethod.Invoke(null, null);
            AssetDatabase.Refresh();
        }

        [Test]
        public void SourceFilesAndSpecExist()
        {
            Assert.IsTrue(File.Exists(ShaderPath), $"Expected ParticleUnlit shader: {ShaderPath}");
            Assert.IsTrue(File.Exists(SpecPath), $"Expected Particle Material System spec: {SpecPath}");
            Assert.IsTrue(File.Exists(BootstrapperPath), $"Expected ParticleUnlit bootstrapper: {BootstrapperPath}");
            Assert.IsTrue(File.Exists(BuildValidatorPath), $"Expected ParticleUnlit build validator: {BuildValidatorPath}");
            Assert.IsTrue(File.Exists(WebGlBuildUtilityPath), $"Expected ParticleUnlit WebGL build utility: {WebGlBuildUtilityPath}");
            Assert.IsTrue(File.Exists(ShaderGuiPath), $"Expected ParticleUnlit ShaderGUI: {ShaderGuiPath}");
            Assert.IsTrue(File.Exists(ValidatorPath), $"Expected ParticleUnlit validator: {ValidatorPath}");
            Assert.IsTrue(File.Exists(PresetUtilityPath), $"Expected ParticleUnlit preset utility: {PresetUtilityPath}");
            Assert.IsTrue(File.Exists(InputHlslPath), $"Expected input HLSL: {InputHlslPath}");
            Assert.IsTrue(File.Exists(CommonHlslPath), $"Expected common HLSL: {CommonHlslPath}");
            Assert.IsTrue(File.Exists(SamplingHlslPath), $"Expected sampling HLSL: {SamplingHlslPath}");
            Assert.IsTrue(File.Exists(DebugHlslPath), $"Expected debug HLSL: {DebugHlslPath}");
            Assert.IsTrue(File.Exists(SurfaceHlslPath), $"Expected surface HLSL: {SurfaceHlslPath}");
            Assert.IsTrue(File.Exists(ForwardPassHlslPath), $"Expected forward pass HLSL: {ForwardPassHlslPath}");
        }

        [Test]
        public void ShaderGuiTypeExistsForCustomEditorBinding()
        {
            Type shaderGuiType = GetEditorAssemblyType("BC.Rendering.ParticleUnlitShaderGUI");
            Assert.IsTrue(typeof(ShaderGUI).IsAssignableFrom(shaderGuiType), "ParticleUnlit ShaderGUI must derive from ShaderGUI.");
        }

        [Test]
        public void WebGlBuildUtilityTypeExistsForValidationBuildEntry()
        {
            Type buildUtilityType = GetEditorAssemblyType("BC.Rendering.ParticleUnlitWebGlBuildUtility");
            MethodInfo buildMethod = GetStaticMethod(buildUtilityType, "RunM11WebGlValidationBuild");
            Type buildValidatorType = GetEditorAssemblyType("BC.Rendering.ParticleUnlitBuildValidator");
            MethodInfo validatorMethod = GetStaticMethod(buildValidatorType, "EnsureM11ValidationAssetsReady");
            MethodInfo shippingGuardMethod = GetStaticMethod(buildValidatorType, "TryGetDebugModeBuildError", typeof(Material), typeof(string).MakeByRefType());
            Type buildPreprocessorType = GetEditorAssemblyType("BC.Rendering.ParticleUnlitBuildPreprocessor");

            Assert.IsNotNull(buildUtilityType, "ParticleUnlit WebGL build utility type must exist.");
            Assert.IsNotNull(buildMethod, "ParticleUnlit WebGL build utility must expose RunM11WebGlValidationBuild.");
            Assert.IsNotNull(buildValidatorType, "ParticleUnlit build validator type must exist.");
            Assert.IsNotNull(validatorMethod, "ParticleUnlit build validator must expose EnsureM11ValidationAssetsReady.");
            Assert.IsNotNull(shippingGuardMethod, "ParticleUnlit build validator must expose TryGetDebugModeBuildError.");
            Assert.IsTrue(typeof(UnityEditor.Build.IPreprocessBuildWithReport).IsAssignableFrom(buildPreprocessorType), "ParticleUnlit build preprocessor must implement IPreprocessBuildWithReport.");
        }

        [Test]
        public void WebGlBuildSourceDefinesValidationContract()
        {
            string buildValidatorSource = File.ReadAllText(BuildValidatorPath);
            string buildUtilitySource = File.ReadAllText(WebGlBuildUtilityPath);

            StringAssert.Contains("EnsureM11ValidationAssetsReady", buildValidatorSource);
            StringAssert.Contains("EnsureM16ValidationAssetsReady", buildValidatorSource);
            StringAssert.Contains("TryGetDebugModeBuildError", buildValidatorSource);
            StringAssert.Contains("TryGetNonDevelopmentBuildError", buildValidatorSource);
            StringAssert.Contains("RequiredM11ValidationMaterialPaths", buildValidatorSource);
            StringAssert.Contains("Missing M11 validation material:", buildValidatorSource);
            StringAssert.Contains("IPreprocessBuildWithReport", buildValidatorSource);
            StringAssert.Contains("BuildTarget.WebGL", buildValidatorSource);
            StringAssert.Contains("BuildOptions.Development", buildValidatorSource);
            StringAssert.Contains("BuildTarget.WebGL", buildUtilitySource);
            StringAssert.Contains("RunM11WebGlValidationBuild", buildUtilitySource);
            StringAssert.Contains("RunM16WebGlValidationBuild", buildUtilitySource);
            StringAssert.Contains("EnsureM16ValidationAssetsReady", buildUtilitySource);
        }

        [Test]
        public void BootstrapperSourceDefinesM9FlipbookContract()
        {
            string bootstrapperSource = File.ReadAllText(BootstrapperPath);

            StringAssert.Contains("BootstrapM9FlipbookValidationAssets", bootstrapperSource);
            StringAssert.Contains("EnsureSmokeFlipbookTexture", bootstrapperSource);
            StringAssert.Contains("EnsureMagicBurstFlipbookTexture", bootstrapperSource);
            StringAssert.Contains("EnsureExplosionPlaceholderFlipbookTexture", bootstrapperSource);
            StringAssert.Contains("ConfigureSmokeFlipbookPrefabParticle", bootstrapperSource);
            StringAssert.Contains("ConfigureMagicBurstFlipbookPrefabParticle", bootstrapperSource);
            StringAssert.Contains("ConfigureWholeSheetFlipbookAnimation", bootstrapperSource);
            StringAssert.Contains("FlipbookFramePadding", bootstrapperSource);
            StringAssert.Contains("RemoveUnexpectedChildren(GetRequiredMarkerTransform(\"Smoke Test Area\")", bootstrapperSource);
            StringAssert.Contains("RemoveUnexpectedChildren(GetRequiredMarkerTransform(\"Magic Test Area\")", bootstrapperSource);
            StringAssert.Contains("textureSheetAnimation.enabled = true", bootstrapperSource);
        }

        [Test]
        public void BootstrapperSourceDefinesM10CustomDataContract()
        {
            string bootstrapperSource = File.ReadAllText(BootstrapperPath);

            StringAssert.Contains("BootstrapM10CustomDataValidationAssets", bootstrapperSource);
            StringAssert.Contains("SparkCustomDataPrefabPath", bootstrapperSource);
            StringAssert.Contains("MagicCustomDataPrefabPath", bootstrapperSource);
            StringAssert.Contains("ConfigureSparkCustomDataPrefabParticle", bootstrapperSource);
            StringAssert.Contains("ConfigureMagicCustomDataPrefabParticle", bootstrapperSource);
            StringAssert.Contains("ConfigureCustom1Data", bootstrapperSource);
            StringAssert.Contains("ParticleSystemCustomDataMode.Vector", bootstrapperSource);
            StringAssert.Contains("SetVectorComponentCount(ParticleSystemCustomData.Custom1, 4)", bootstrapperSource);
            StringAssert.Contains("ParticleSystemVertexStream.Custom1XYZW", bootstrapperSource);
            StringAssert.Contains("EnsureM10CustomDataPreviewScene", bootstrapperSource);
        }

        [Test]
        public void BootstrapperSourceDefinesM11DepthInteractionContract()
        {
            string bootstrapperSource = File.ReadAllText(BootstrapperPath);

            StringAssert.Contains("BootstrapM11DepthInteractionValidationAssets", bootstrapperSource);
            StringAssert.Contains("EnsureM11DepthValidationMaterial", bootstrapperSource);
            StringAssert.Contains("EnsureM11DepthInteractionScene", bootstrapperSource);
            StringAssert.Contains("DustDepthValidationMaterialPath", bootstrapperSource);
            StringAssert.Contains("SmokeDepthValidationMaterialPath", bootstrapperSource);
            StringAssert.Contains("GlowCameraFadeValidationMaterialPath", bootstrapperSource);
            StringAssert.Contains("EnsureDepthInteractionWall", bootstrapperSource);
            StringAssert.Contains("_UseSoftParticles", bootstrapperSource);
            StringAssert.Contains("_UseCameraFade", bootstrapperSource);
        }

        [Test]
        public void BuildValidatorRejectsDebugModeForShippingMaterials()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Assert.IsNotNull(shader, "ParticleUnlit shader must load.");

            Type buildValidatorType = GetEditorAssemblyType("BC.Rendering.ParticleUnlitBuildValidator");
            MethodInfo buildErrorMethod = GetStaticMethod(buildValidatorType, "TryGetDebugModeBuildError", typeof(Material), typeof(string).MakeByRefType());

            Material material = new Material(shader);
            try
            {
                object[] disabledArguments = { material, null };
                bool disabledError = (bool)buildErrorMethod.Invoke(null, disabledArguments);
                Assert.IsFalse(disabledError, "Debug Mode off should not fail the build validator.");

                material.SetFloat("_DebugMode", 9f);
                object[] enabledArguments = { material, null };
                bool enabledError = (bool)buildErrorMethod.Invoke(null, enabledArguments);
                Assert.IsTrue(enabledError, "Active Debug Mode should fail the shipping build validator.");
                StringAssert.Contains("Debug Mode", enabledArguments[1] as string);
                StringAssert.Contains("before shipping", enabledArguments[1] as string);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void ShaderSourceKeepsM11DepthAwareContract()
        {
            string shaderSource = File.ReadAllText(ShaderPath);
            string inputSource = File.ReadAllText(InputHlslPath);
            string commonSource = File.ReadAllText(CommonHlslPath);
            string samplingSource = File.ReadAllText(SamplingHlslPath);
            string debugSource = File.ReadAllText(DebugHlslPath);
            string passSource = File.ReadAllText(ForwardPassHlslPath);
            string surfaceSource = File.ReadAllText(SurfaceHlslPath);
            string allShaderSource = shaderSource + inputSource + commonSource + samplingSource + debugSource + passSource + surfaceSource;

            StringAssert.Contains("Shader \"BC/Particles/ParticleUnlit\"", shaderSource);
            StringAssert.Contains("#pragma target 2.0", shaderSource);
            StringAssert.Contains("#pragma multi_compile_instancing", shaderSource);
            StringAssert.Contains("_BlendMode", shaderSource);
            StringAssert.Contains("_QueueOffset", shaderSource);
            StringAssert.Contains("_MaskMap", shaderSource);
            StringAssert.Contains("_NoiseMap", shaderSource);
            StringAssert.Contains("_DissolveAmount", shaderSource);
            StringAssert.Contains("_EmissionColor", shaderSource);
            StringAssert.Contains("_EmissionStrength", shaderSource);
            StringAssert.Contains("_EmissionAlphaInfluence", shaderSource);
            StringAssert.Contains("_DebugMode", shaderSource);
            StringAssert.Contains("_UseSoftParticles", shaderSource);
            StringAssert.Contains("_SoftParticleDistance", shaderSource);
            StringAssert.Contains("_UseCameraFade", shaderSource);
            StringAssert.Contains("_CameraFadeNear", shaderSource);
            StringAssert.Contains("_CameraFadeFar", shaderSource);
            StringAssert.Contains("CustomEditor \"BC.Rendering.ParticleUnlitShaderGUI\"", shaderSource);
            StringAssert.Contains("_BlendMode", inputSource);
            StringAssert.Contains("_MaskMap", inputSource);
            StringAssert.Contains("_NoiseMap", inputSource);
            StringAssert.Contains("_NoiseScrollSpeed", inputSource);
            StringAssert.Contains("_EmissionColor", inputSource);
            StringAssert.Contains("_DebugMode", inputSource);
            StringAssert.Contains("_UseSoftParticles", inputSource);
            StringAssert.Contains("_UseCameraFade", inputSource);
            StringAssert.Contains("ParticleUnlit_Input.hlsl", passSource);
            StringAssert.Contains("ParticleUnlit_Common.hlsl", passSource);
            StringAssert.Contains("ParticleUnlit_Sampling.hlsl", passSource);
            StringAssert.Contains("ParticleUnlit_Debug.hlsl", passSource);
            StringAssert.Contains("ParticleUnlit_Surface.hlsl", passSource);
            StringAssert.Contains("float4 custom1 : TEXCOORD1;", passSource);
            StringAssert.Contains("float3 positionWS : TEXCOORD2;", passSource);
            StringAssert.Contains("output.positionWS = TransformObjectToWorld(input.positionOS.xyz);", passSource);
            StringAssert.Contains("BC_ParticleBuildSurfaceColor(baseUV, input.uv, input.color, input.custom1, input.positionWS, input.positionCS, _Time.y)", passSource);
            StringAssert.Contains("BC_ParticleBuildCustomNoiseOffset", commonSource);
            StringAssert.Contains("DeclareDepthTexture.hlsl", commonSource);
            StringAssert.Contains("BC_ParticleBuildSceneDepthFadeFactor", commonSource);
            StringAssert.Contains("BC_ParticleBuildCameraFadeFactor", commonSource);
            StringAssert.Contains("SampleSceneDepth", commonSource);
            StringAssert.Contains("GetNormalizedScreenSpaceUV", commonSource);
            StringAssert.Contains("if (rawSceneDepth <= 0.0)", commonSource);
            StringAssert.Contains("BC_ParticleBuildNoiseUV(rawUV, timeSeconds, custom1.z)", surfaceSource);
            StringAssert.Contains("dissolveAmount = saturate(_DissolveAmount + custom1.x)", surfaceSource);
            StringAssert.Contains("emissionStrength = max(_EmissionStrength + custom1.y, 0.0)", surfaceSource);
            StringAssert.Contains("softParticleFade", surfaceSource);
            StringAssert.Contains("cameraFade", surfaceSource);
            StringAssert.Contains("BC_ParticleBuildDebugColor", debugSource);
            StringAssert.Contains("resolvedMode == 13", debugSource);
            StringAssert.Contains("resolvedMode == 15", debugSource);
            StringAssert.DoesNotContain("SAMPLE_TEXTURE2D", debugSource);
            StringAssert.Contains("BC_ParticleCalculateSoftCircle", surfaceSource + commonSource);
            StringAssert.Contains("BC_ParticleUsesPremultiply", surfaceSource + commonSource);
            StringAssert.Contains("BC_ParticleCalculateDissolveMask", surfaceSource + commonSource);
            StringAssert.Contains("BC_ParticleSampleMask", passSource + surfaceSource);
            StringAssert.Contains("BC_ParticleSampleNoise", samplingSource + surfaceSource);
            StringAssert.Contains("BC_ParticleBuildDebugColor(_DebugMode", surfaceSource);
            StringAssert.Contains("maskValue.r", surfaceSource);
            StringAssert.Contains("maskValue.g", surfaceSource);
            StringAssert.Contains("maskValue.b", surfaceSource);
            StringAssert.Contains("maskValue.a", surfaceSource);
            StringAssert.Contains("emissionMask = saturate(maskValue.g)", surfaceSource);
            StringAssert.Contains("emissionAlphaFactor", surfaceSource);
            StringAssert.DoesNotContain("_FlipbookRows", allShaderSource);
            StringAssert.DoesNotContain("_FlipbookColumns", allShaderSource);
            StringAssert.DoesNotContain("_FlipbookMode", allShaderSource);
            StringAssert.DoesNotContain("_FlipbookBlend", allShaderSource);
            StringAssert.DoesNotContain("shader_feature", allShaderSource);
            StringAssert.DoesNotContain("_CameraOpaqueTexture", allShaderSource);
            StringAssert.DoesNotContain("GrabPass", allShaderSource);
            StringAssert.DoesNotContain("ComputeShader", allShaderSource);
        }

        [Test]
        public void GeneratedMaterialUsesParticleUnlitShaderAndTexture()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);

            Assert.IsNotNull(shader, "ParticleUnlit shader must load.");
            Assert.IsNotNull(material, $"Expected generated ParticleUnlit material: {MaterialPath}");
            Assert.IsNotNull(texture, $"Expected generated ParticleUnlit texture: {TexturePath}");
            Assert.AreSame(shader, material.shader, "Generated ParticleUnlit material should use the ParticleUnlit shader.");
            Assert.AreSame(texture, material.GetTexture("_BaseMap"), "Generated ParticleUnlit material should reference the generated base texture.");
            Assert.AreEqual(0f, material.GetFloat("_Cull"));
            Assert.AreEqual((float)CompareFunction.LessEqual, material.GetFloat("_ZTest"));
            Assert.AreEqual((float)BlendMode.SrcAlpha, material.GetFloat("_SrcBlend"));
            Assert.AreEqual((float)BlendMode.OneMinusSrcAlpha, material.GetFloat("_DstBlend"));
            Assert.AreEqual(0f, material.GetFloat("_ZWrite"));
            Assert.AreEqual(15f, material.GetFloat("_ColorMask"));
            Assert.AreEqual((int)RenderQueue.Transparent, material.renderQueue);
            Assert.AreEqual(0f, material.GetFloat("_BlendMode"));
            Assert.AreEqual(0f, material.GetFloat("_QueueOffset"));
            Assert.AreEqual(1f, material.GetFloat("_UseVertexColor"));
            Assert.AreEqual(1f, material.GetFloat("_SoftCircleStrength"));
            Assert.AreEqual(0f, material.GetFloat("_UseSoftParticles"));
            Assert.AreEqual(0f, material.GetFloat("_UseCameraFade"));
        }

        [Test]
        public void PresetUtilityNormalizesAllBlendModes()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Assert.IsNotNull(shader, "ParticleUnlit shader must load.");

            Material temporaryMaterial = new Material(shader);
            try
            {
                Type presetUtilityType = GetEditorAssemblyType("BC.Rendering.ParticleUnlitPresetUtility");
                MethodInfo applyPresetMethod = GetStaticMethod(presetUtilityType, "ApplyPreset", typeof(Material), typeof(string));

                applyPresetMethod.Invoke(null, new object[] { temporaryMaterial, "Dust" });
                Assert.AreEqual(0f, temporaryMaterial.GetFloat("_BlendMode"));
                Assert.AreEqual((float)BlendMode.SrcAlpha, temporaryMaterial.GetFloat("_SrcBlend"));
                Assert.AreEqual((float)BlendMode.OneMinusSrcAlpha, temporaryMaterial.GetFloat("_DstBlend"));
                Assert.AreEqual(0f, temporaryMaterial.GetFloat("_UseSoftParticles"));
                Assert.AreEqual(0f, temporaryMaterial.GetFloat("_UseCameraFade"));

                applyPresetMethod.Invoke(null, new object[] { temporaryMaterial, "Glow" });
                Assert.AreEqual(2f, temporaryMaterial.GetFloat("_BlendMode"));
                Assert.AreEqual((float)BlendMode.One, temporaryMaterial.GetFloat("_SrcBlend"));
                Assert.AreEqual((float)BlendMode.OneMinusSrcAlpha, temporaryMaterial.GetFloat("_DstBlend"));
                Assert.Greater(temporaryMaterial.GetFloat("_EmissionStrength"), 0f);

                applyPresetMethod.Invoke(null, new object[] { temporaryMaterial, "Spark" });
                Assert.AreEqual(1f, temporaryMaterial.GetFloat("_BlendMode"));
                Assert.AreEqual((float)BlendMode.SrcAlpha, temporaryMaterial.GetFloat("_SrcBlend"));
                Assert.AreEqual((float)BlendMode.One, temporaryMaterial.GetFloat("_DstBlend"));
                Assert.Greater(temporaryMaterial.GetFloat("_EmissionStrength"), 0f);

                applyPresetMethod.Invoke(null, new object[] { temporaryMaterial, "Smoke" });
                Assert.Greater(temporaryMaterial.GetFloat("_NoiseStrength"), 0f);
                Assert.Greater(temporaryMaterial.GetFloat("_DissolveAmount"), 0f);
                Assert.AreEqual(0f, temporaryMaterial.GetFloat("_EmissionStrength"));

                applyPresetMethod.Invoke(null, new object[] { temporaryMaterial, "Magic" });
                Assert.AreEqual(1f, temporaryMaterial.GetFloat("_BlendMode"));
                Assert.Greater(temporaryMaterial.GetFloat("_NoiseScale"), 1f);
                Assert.Greater(temporaryMaterial.GetFloat("_DissolveAmount"), 0f);
                Assert.Greater(temporaryMaterial.GetFloat("_EmissionStrength"), 0f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporaryMaterial);
            }
        }

        [Test]
        public void ShaderGuiFollowsM11InspectorSectionOrder()
        {
            string shaderGuiSource = File.ReadAllText(ShaderGuiPath);

            AssertContainsInOrder(
                shaderGuiSource,
                "DrawRenderingSection();",
                "DrawBaseSection();",
                "DrawShapeSection();",
                "DrawMaskSection();",
                "DrawNoiseSection();",
                "DrawDissolveSection();",
                "DrawEmissionSection();",
                "DrawDepthSection();",
                "DrawDebugSection();",
                "DrawOptionalSection();");
            StringAssert.Contains("TryGetDebugViewAuthoringWarning", shaderGuiSource);
            StringAssert.Contains("TryGetDepthInteractionWarning", shaderGuiSource);
        }

        [Test]
        public void ValidatorSupportsQueueOffsetAndIgnoresNonParticleMaterials()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Assert.IsNotNull(shader, "ParticleUnlit shader must load.");

            Type validatorType = GetEditorAssemblyType("BC.Rendering.ParticleUnlitMaterialValidator");
            MethodInfo normalizeMethod = GetStaticMethod(validatorType, "Normalize", typeof(Material));

            Material particleMaterial = new Material(shader);
            try
            {
                particleMaterial.SetFloat("_BlendMode", 2f);
                particleMaterial.SetFloat("_QueueOffset", 12f);
                particleMaterial.SetFloat("_MaskStrength", -2f);
                particleMaterial.SetFloat("_NoiseStrength", 5f);
                particleMaterial.SetFloat("_NoiseScale", 0f);
                particleMaterial.SetFloat("_DissolveAmount", -1f);
                particleMaterial.SetFloat("_DissolveSoftness", 4f);
                particleMaterial.SetFloat("_EmissionStrength", 40f);
                particleMaterial.SetFloat("_EmissionAlphaInfluence", -3f);
                particleMaterial.SetFloat("_UseSoftParticles", 3f);
                particleMaterial.SetFloat("_SoftParticleDistance", -0.5f);
                particleMaterial.SetFloat("_UseCameraFade", -2f);
                particleMaterial.SetFloat("_CameraFadeNear", 6f);
                particleMaterial.SetFloat("_CameraFadeFar", 0f);
                normalizeMethod.Invoke(null, new object[] { particleMaterial });

                Assert.AreEqual((float)BlendMode.One, particleMaterial.GetFloat("_SrcBlend"));
                Assert.AreEqual((float)BlendMode.OneMinusSrcAlpha, particleMaterial.GetFloat("_DstBlend"));
                Assert.AreEqual((int)RenderQueue.Transparent + 12, particleMaterial.renderQueue);
                Assert.AreEqual(0f, particleMaterial.GetFloat("_MaskStrength"));
                Assert.AreEqual(1f, particleMaterial.GetFloat("_NoiseStrength"));
                Assert.AreEqual(0.01f, particleMaterial.GetFloat("_NoiseScale"));
                Assert.AreEqual(0f, particleMaterial.GetFloat("_NoiseSpace"));
                Assert.AreEqual(0f, particleMaterial.GetFloat("_DissolveAmount"));
                Assert.AreEqual(1f, particleMaterial.GetFloat("_DissolveSoftness"));
                Assert.AreEqual(16f, particleMaterial.GetFloat("_EmissionStrength"));
                Assert.AreEqual(0f, particleMaterial.GetFloat("_EmissionAlphaInfluence"));
                Assert.AreEqual(1f, particleMaterial.GetFloat("_UseSoftParticles"));
                Assert.AreEqual(0.001f, particleMaterial.GetFloat("_SoftParticleDistance"));
                Assert.AreEqual(0f, particleMaterial.GetFloat("_UseCameraFade"));
                Assert.AreEqual(5f, particleMaterial.GetFloat("_CameraFadeNear"));
                Assert.AreEqual(5.001f, particleMaterial.GetFloat("_CameraFadeFar"), 0.0001f);
                Assert.IsFalse(particleMaterial.shaderKeywords.Length > 0, "ParticleUnlit validation should not leave shader keywords behind.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(particleMaterial);
            }

            Shader foreignShader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            if (foreignShader == null)
            {
                Assert.Inconclusive("No built-in non-ParticleUnlit shader was available for validator isolation testing.");
            }

            Material foreignMaterial = new Material(foreignShader);
            try
            {
                foreignMaterial.renderQueue = 2450;
                object changed = normalizeMethod.Invoke(null, new object[] { foreignMaterial });
                Assert.AreEqual(false, changed, "ParticleUnlit validator should ignore materials that use other shaders.");
                Assert.AreEqual(2450, foreignMaterial.renderQueue, "ParticleUnlit validator must not rewrite non-ParticleUnlit render queues.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(foreignMaterial);
            }
        }

        [Test]
        public void ValidatorSupportsDebugViewWarningWithoutResettingActiveDebugMode()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Assert.IsNotNull(shader, "ParticleUnlit shader must load.");

            Type validatorType = GetEditorAssemblyType("BC.Rendering.ParticleUnlitMaterialValidator");
            MethodInfo normalizeMethod = GetStaticMethod(validatorType, "Normalize", typeof(Material));
            MethodInfo warningMethod = GetStaticMethod(validatorType, "TryGetDebugViewAuthoringWarning", typeof(Material), typeof(string).MakeByRefType());

            Material material = new Material(shader);
            try
            {
                material.SetFloat("_DebugMode", 99f);
                normalizeMethod.Invoke(null, new object[] { material });
                Assert.AreEqual(15f, material.GetFloat("_DebugMode"), "ParticleUnlit validator should clamp debug mode to the supported M10 range.");

                material.SetFloat("_DebugMode", 15f);
                normalizeMethod.Invoke(null, new object[] { material });
                Assert.AreEqual(15f, material.GetFloat("_DebugMode"), "ParticleUnlit validator should preserve an active supported debug mode.");

                object[] warningArguments = { material, null };
                bool hasWarning = (bool)warningMethod.Invoke(null, warningArguments);
                Assert.IsTrue(hasWarning, "Active debug mode should surface an authoring warning.");
                StringAssert.Contains("UV", warningArguments[1] as string);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void ValidatorSurfacesDepthInteractionWarningWhenEnabled()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Assert.IsNotNull(shader, "ParticleUnlit shader must load.");

            Type validatorType = GetEditorAssemblyType("BC.Rendering.ParticleUnlitMaterialValidator");
            MethodInfo warningMethod = GetStaticMethod(validatorType, "TryGetDepthInteractionWarning", typeof(Material), typeof(string).MakeByRefType());

            Material material = new Material(shader);
            try
            {
                object[] disabledArguments = { material, null };
                bool disabledWarning = (bool)warningMethod.Invoke(null, disabledArguments);
                Assert.IsFalse(disabledWarning, "Depth interaction warning should stay silent while all M11 toggles are disabled.");

                material.SetFloat("_UseSoftParticles", 1f);
                object[] enabledArguments = { material, null };
                bool enabledWarning = (bool)warningMethod.Invoke(null, enabledArguments);
                Assert.IsTrue(enabledWarning, "Depth interaction warning should surface when M11 depth toggles are enabled.");
                StringAssert.Contains("WebGL", enabledArguments[1] as string);
                StringAssert.Contains("ParticleMaterialTestScene", enabledArguments[1] as string);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void GeneratedTextureUsesExpectedImporterSettings()
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;

            Assert.IsNotNull(texture, $"Expected generated ParticleUnlit texture: {TexturePath}");
            Assert.IsNotNull(importer, $"Expected texture importer for generated ParticleUnlit texture: {TexturePath}");
            Assert.AreEqual(TextureImporterType.Default, importer.textureType);
            Assert.IsTrue(importer.sRGBTexture, "Generated ParticleUnlit texture should stay in sRGB space.");
            Assert.AreEqual(TextureImporterAlphaSource.FromInput, importer.alphaSource);
            Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapMode);
            Assert.AreEqual(FilterMode.Bilinear, importer.filterMode);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
            Assert.IsFalse(importer.mipmapEnabled, "Generated ParticleUnlit texture should not generate mipmaps.");

            AssertTextureImportSettings(SoftCloudNoisePath, false, TextureWrapMode.Repeat);
            AssertTextureImportSettings(DissolveNoisePath, false, TextureWrapMode.Repeat);
            AssertTextureImportSettings(ParticleMaskPath, false, TextureWrapMode.Clamp);
            AssertTextureImportSettings(SmokeFlipbookTexturePath, true, TextureWrapMode.Clamp);
            AssertTextureImportSettings(MagicBurstFlipbookTexturePath, true, TextureWrapMode.Clamp);
            AssertTextureImportSettings(ExplosionPlaceholderFlipbookTexturePath, true, TextureWrapMode.Clamp);

            AssertTextureShowsVariation(SmokeFlipbookTexturePath, "Smoke flipbook atlas should not be uniform.");
            AssertTextureShowsVariation(MagicBurstFlipbookTexturePath, "Magic burst flipbook atlas should not be uniform.");
            AssertTextureShowsVariation(ExplosionPlaceholderFlipbookTexturePath, "Explosion placeholder flipbook atlas should not be uniform.");
            AssertFlipbookTextureUsesSafeTilePadding(SmokeFlipbookTexturePath, "Smoke flipbook atlas should pad tile borders to reduce frame bleed.");
            AssertFlipbookTextureUsesSafeTilePadding(MagicBurstFlipbookTexturePath, "Magic burst flipbook atlas should pad tile borders to reduce frame bleed.");
        }

        [Test]
        public void GeneratedBlendPresetMaterialsUseExpectedBlendStates()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);

            Assert.IsNotNull(shader, "ParticleUnlit shader must load.");
            Assert.IsNotNull(texture, $"Expected generated ParticleUnlit texture: {TexturePath}");

            AssertMaterialBlendState(DustMaterialPath, shader, texture, 0f, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha, new Color(0.78f, 0.70f, 0.58f, 0.58f), 0.62f, 0.95f);
            AssertMaterialBlendState(GlowMaterialPath, shader, texture, 2f, BlendMode.One, BlendMode.OneMinusSrcAlpha, new Color(0.72f, 0.90f, 1.0f, 0.72f), 0.72f, 1.3f);
            AssertMaterialBlendState(SparkMaterialPath, shader, texture, 1f, BlendMode.SrcAlpha, BlendMode.One, new Color(1.0f, 0.86f, 0.54f, 0.66f), 0.68f, 1.55f);
        }

        [Test]
        public void GeneratedEmissionMaterialsUseExpectedValues()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Texture2D baseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            Texture2D particleMask = AssetDatabase.LoadAssetAtPath<Texture2D>(ParticleMaskPath);
            Texture2D dissolveNoise = AssetDatabase.LoadAssetAtPath<Texture2D>(DissolveNoisePath);

            Assert.IsNotNull(shader, "ParticleUnlit shader must load.");
            Assert.IsNotNull(baseTexture, $"Expected generated ParticleUnlit texture: {TexturePath}");
            Assert.IsNotNull(particleMask, $"Expected generated mask texture: {ParticleMaskPath}");

            AssertEmissionMaterial(GlowMaterialPath, shader, baseTexture, particleMask, null, new Color(0.42f, 0.76f, 1.0f, 1f), 1.8f, 0.35f);
            AssertEmissionMaterial(SparkMaterialPath, shader, baseTexture, particleMask, null, new Color(1.0f, 0.62f, 0.18f, 1f), 2.6f, 0.1f);
            AssertEmissionMaterial(MagicMaterialPath, shader, baseTexture, particleMask, dissolveNoise, new Color(0.55f, 0.30f, 1.0f, 1f), 2.2f, 0.25f);
        }

        [Test]
        public void GeneratedFlipbookMaterialsUseExpectedTexturesAndBlendStates()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Texture2D smokeFlipbookTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(SmokeFlipbookTexturePath);
            Texture2D magicBurstFlipbookTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(MagicBurstFlipbookTexturePath);
            Texture2D particleMask = AssetDatabase.LoadAssetAtPath<Texture2D>(ParticleMaskPath);
            Texture2D softCloudNoise = AssetDatabase.LoadAssetAtPath<Texture2D>(SoftCloudNoisePath);
            Texture2D dissolveNoise = AssetDatabase.LoadAssetAtPath<Texture2D>(DissolveNoisePath);

            Assert.IsNotNull(shader, "ParticleUnlit shader must load.");
            Assert.IsNotNull(smokeFlipbookTexture, $"Expected generated smoke flipbook atlas: {SmokeFlipbookTexturePath}");
            Assert.IsNotNull(magicBurstFlipbookTexture, $"Expected generated magic burst flipbook atlas: {MagicBurstFlipbookTexturePath}");
            Assert.IsNotNull(particleMask, $"Expected generated mask texture: {ParticleMaskPath}");

            AssertFlipbookMaterial(SmokeFlipbookMaterialPath, shader, smokeFlipbookTexture, particleMask, softCloudNoise, 0f, new Color(0.50f, 0.52f, 0.50f, 0.48f), 0.5f, 0.78f);
            AssertFlipbookMaterial(MagicBurstMaterialPath, shader, magicBurstFlipbookTexture, particleMask, dissolveNoise, 1f, new Color(0.72f, 0.45f, 1.0f, 0.68f), 0.7f, 1.35f);
        }

        [Test]
        public void GeneratedMaterialsKeepDebugModeDisabledByDefault()
        {
            AssertDebugModeDisabled(MaterialPath);
            AssertDebugModeDisabled(DustMaterialPath);
            AssertDebugModeDisabled(SmokeMaterialPath);
            AssertDebugModeDisabled(GlowMaterialPath);
            AssertDebugModeDisabled(SparkMaterialPath);
            AssertDebugModeDisabled(MagicMaterialPath);
            AssertDebugModeDisabled(SmokeFlipbookMaterialPath);
            AssertDebugModeDisabled(MagicBurstMaterialPath);
            AssertDebugModeDisabled(DustDepthValidationMaterialPath);
            AssertDebugModeDisabled(SmokeDepthValidationMaterialPath);
            AssertDebugModeDisabled(GlowCameraFadeValidationMaterialPath);
        }

        [Test]
        public void ShippingMaterialsKeepDepthInteractionDisabledAndValidationMaterialsEnableTargetedM11Modes()
        {
            Assert.AreEqual(0f, AssetDatabase.LoadAssetAtPath<Material>(DustMaterialPath).GetFloat("_UseSoftParticles"));
            Assert.AreEqual(0f, AssetDatabase.LoadAssetAtPath<Material>(SmokeMaterialPath).GetFloat("_UseSoftParticles"));
            Assert.AreEqual(0f, AssetDatabase.LoadAssetAtPath<Material>(GlowMaterialPath).GetFloat("_UseCameraFade"));

            Material dustDepthMaterial = AssetDatabase.LoadAssetAtPath<Material>(DustDepthValidationMaterialPath);
            Material smokeDepthMaterial = AssetDatabase.LoadAssetAtPath<Material>(SmokeDepthValidationMaterialPath);
            Material glowCameraFadeMaterial = AssetDatabase.LoadAssetAtPath<Material>(GlowCameraFadeValidationMaterialPath);

            Assert.IsNotNull(dustDepthMaterial, $"Expected generated M11 validation material: {DustDepthValidationMaterialPath}");
            Assert.IsNotNull(smokeDepthMaterial, $"Expected generated M11 validation material: {SmokeDepthValidationMaterialPath}");
            Assert.IsNotNull(glowCameraFadeMaterial, $"Expected generated M11 validation material: {GlowCameraFadeValidationMaterialPath}");

            Assert.AreEqual(1f, dustDepthMaterial.GetFloat("_UseSoftParticles"));
            Assert.AreEqual(0.55f, dustDepthMaterial.GetFloat("_SoftParticleDistance"), 0.0001f);
            Assert.AreEqual(1f, smokeDepthMaterial.GetFloat("_UseSoftParticles"));
            Assert.AreEqual(1.1f, smokeDepthMaterial.GetFloat("_SoftParticleDistance"), 0.0001f);
            Assert.AreEqual(1f, glowCameraFadeMaterial.GetFloat("_UseCameraFade"));
            Assert.AreEqual(1.5f, glowCameraFadeMaterial.GetFloat("_CameraFadeNear"), 0.0001f);
            Assert.AreEqual(4.5f, glowCameraFadeMaterial.GetFloat("_CameraFadeFar"), 0.0001f);
        }

        [Test]
        public void GeneratedPrefabsUseExpectedProfiles()
        {
            AssertParticlePrefabUsesProfile(DustPrefabPath, "FX_Particle_Dust", AssetDatabase.LoadAssetAtPath<Material>(DustMaterialPath), ParticleSystemSimulationSpace.World, ParticleSystemRenderMode.Billboard, ParticleSystemShapeType.Circle, 8f, 0.08f, 0.03f, 180, 22f, expectColorOverLifetime: false, expectSizeOverLifetime: false);
            AssertParticlePrefabUsesProfile(SmokePrefabPath, "FX_Particle_Smoke", AssetDatabase.LoadAssetAtPath<Material>(SmokeMaterialPath), ParticleSystemSimulationSpace.World, ParticleSystemRenderMode.Billboard, ParticleSystemShapeType.Circle, 3.4f, 0.42f, 0.85f, 96, 10f, expectColorOverLifetime: true, expectSizeOverLifetime: true);
            AssertParticlePrefabUsesProfile(GlowPrefabPath, "FX_Particle_Glow", AssetDatabase.LoadAssetAtPath<Material>(GlowMaterialPath), ParticleSystemSimulationSpace.Local, ParticleSystemRenderMode.Billboard, ParticleSystemShapeType.Circle, 2.2f, 0.12f, 0.24f, 72, 7f, expectColorOverLifetime: true, expectSizeOverLifetime: false);
            AssertParticlePrefabUsesProfile(SparkPrefabPath, "FX_Particle_Spark", AssetDatabase.LoadAssetAtPath<Material>(SparkMaterialPath), ParticleSystemSimulationSpace.Local, ParticleSystemRenderMode.Stretch, ParticleSystemShapeType.Cone, 0.35f, 6f, 0.04f, 180, 36f, expectColorOverLifetime: false, expectSizeOverLifetime: false);
            AssertParticlePrefabUsesProfile(MagicPrefabPath, "FX_Particle_Magic", AssetDatabase.LoadAssetAtPath<Material>(MagicMaterialPath), ParticleSystemSimulationSpace.Local, ParticleSystemRenderMode.Billboard, ParticleSystemShapeType.Circle, 1.6f, 0.45f, 0.18f, 84, 11f, expectColorOverLifetime: true, expectSizeOverLifetime: true);
        }

        [Test]
        public void GeneratedFlipbookPrefabsUseTextureSheetAnimationProfiles()
        {
            AssertFlipbookPrefabUsesProfile(SmokeFlipbookPrefabPath, "FX_Particle_SmokeFlipbook", AssetDatabase.LoadAssetAtPath<Material>(SmokeFlipbookMaterialPath), ParticleSystemSimulationSpace.World, 4.1f, 0.18f, 1.05f, 120, 14f, 0.24f, 0.34f);
            AssertFlipbookPrefabUsesProfile(MagicBurstPrefabPath, "FX_Particle_MagicBurst", AssetDatabase.LoadAssetAtPath<Material>(MagicBurstMaterialPath), ParticleSystemSimulationSpace.Local, 1.1f, 0.55f, 0.56f, 72, 8f, 0.12f, 0.8f);
        }

        [Test]
        public void GeneratedCustomDataPrefabsUseExpectedProfiles()
        {
            AssertCustomDataPrefabUsesProfile(
                SparkCustomDataPrefabPath,
                "FX_Particle_SparkCustomData",
                AssetDatabase.LoadAssetAtPath<Material>(SparkMaterialPath),
                ParticleSystemSimulationSpace.Local,
                ParticleSystemRenderMode.Stretch,
                ParticleSystemShapeType.Cone,
                0.48f,
                5.2f,
                0.05f,
                144,
                28f,
                0.03f,
                0f,
                0.08f,
                0.16f,
                0.25f,
                0.9f,
                0.18f,
                -0.2f,
                0.2f);

            AssertCustomDataPrefabUsesProfile(
                MagicCustomDataPrefabPath,
                "FX_Particle_MagicCustomData",
                AssetDatabase.LoadAssetAtPath<Material>(MagicMaterialPath),
                ParticleSystemSimulationSpace.Local,
                ParticleSystemRenderMode.Billboard,
                ParticleSystemShapeType.Circle,
                1.45f,
                0.32f,
                0.24f,
                96,
                14f,
                0.10f,
                -0.12f,
                0.16f,
                0.34f,
                0.18f,
                0.72f,
                0.3f,
                -0.3f,
                0.3f);
        }

        [Test]
        public void GeneratedNoiseAndDissolveMaterialsUseExpectedTexturesAndValues()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Texture2D baseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            Texture2D softCloudNoise = AssetDatabase.LoadAssetAtPath<Texture2D>(SoftCloudNoisePath);
            Texture2D dissolveNoise = AssetDatabase.LoadAssetAtPath<Texture2D>(DissolveNoisePath);
            Texture2D particleMask = AssetDatabase.LoadAssetAtPath<Texture2D>(ParticleMaskPath);

            Assert.IsNotNull(shader, "ParticleUnlit shader must load.");
            Assert.IsNotNull(baseTexture, $"Expected generated ParticleUnlit base texture: {TexturePath}");
            Assert.IsNotNull(softCloudNoise, $"Expected generated noise texture: {SoftCloudNoisePath}");
            Assert.IsNotNull(dissolveNoise, $"Expected generated noise texture: {DissolveNoisePath}");
            Assert.IsNotNull(particleMask, $"Expected generated mask texture: {ParticleMaskPath}");

            AssertNoiseDissolveMaterial(SmokeMaterialPath, shader, baseTexture, particleMask, softCloudNoise, 0f, 0.7f, 0.58f, 2.0f, new Vector4(0.02f, 0.05f, 0f, 0f), 0.12f, 0.5f);
            AssertNoiseDissolveMaterial(MagicMaterialPath, shader, baseTexture, particleMask, dissolveNoise, 1f, 0.8f, 0.65f, 3.2f, new Vector4(0.12f, 0.18f, 0f, 0f), 0.24f, 0.2f);

            AssertTextureShowsVariation(SoftCloudNoisePath, "Soft cloud noise texture should not be uniform.");
            AssertTextureShowsVariation(DissolveNoisePath, "Dissolve noise texture should not be uniform.");
            AssertMaskTextureUsesPackedChannels(ParticleMaskPath);
        }

        [Test]
        public void ValidationSceneContainsParticleUnlitAnchors()
        {
            Assert.IsTrue(File.Exists(ScenePath), $"Expected ParticleUnlit validation scene: {ScenePath}");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Assert.IsTrue(scene.IsValid(), "ParticleMaterialTestScene must open successfully.");
            Assert.IsNotNull(GameObject.Find("Main Camera"), "ParticleMaterialTestScene must contain a Main Camera.");
            Assert.IsNotNull(GameObject.Find("Directional Light"), "ParticleMaterialTestScene must contain a Directional Light.");
            Assert.IsNotNull(GameObject.Find("ValidationFloor"), "ParticleMaterialTestScene must contain a ValidationFloor.");
            AssertParticleAnchorUsesMaterial("Dust Test Area", "ParticleUnlit_BaseValidation", material, expectLifetimeColor: false);
            AssertParticleAnchorUsesMaterial("Glow Test Area", "ParticleUnlit_LifetimeValidation", material, expectLifetimeColor: true);
            AssertParticleAnchorUsesMaterial("Dust Test Area", "ParticleUnlit_DustAlphaValidation", AssetDatabase.LoadAssetAtPath<Material>(DustMaterialPath), expectLifetimeColor: false);
            AssertParticleAnchorUsesMaterial("Glow Test Area", "ParticleUnlit_GlowPremultiplyValidation", AssetDatabase.LoadAssetAtPath<Material>(GlowMaterialPath), expectLifetimeColor: true);
            AssertParticleAnchorUsesMaterial("Spark Test Area", "ParticleUnlit_SparkAdditiveValidation", AssetDatabase.LoadAssetAtPath<Material>(SparkMaterialPath), expectLifetimeColor: false);
            AssertParticleAnchorUsesMaterial("Smoke Test Area", "ParticleUnlit_SmokeNoiseValidation", AssetDatabase.LoadAssetAtPath<Material>(SmokeMaterialPath), expectLifetimeColor: true);
            AssertParticleAnchorUsesMaterial("Magic Test Area", "ParticleUnlit_MagicDissolveValidation", AssetDatabase.LoadAssetAtPath<Material>(MagicMaterialPath), expectLifetimeColor: true);
            AssertParticleAnchorUsesMaterial("Dust Test Area", DustDepthValidationObjectName, AssetDatabase.LoadAssetAtPath<Material>(DustDepthValidationMaterialPath), expectLifetimeColor: false);
            AssertParticleAnchorUsesMaterial("Smoke Test Area", SmokeDepthValidationObjectName, AssetDatabase.LoadAssetAtPath<Material>(SmokeDepthValidationMaterialPath), expectLifetimeColor: true);
            AssertParticleAnchorUsesMaterial("Glow Test Area", GlowCameraFadeValidationObjectName, AssetDatabase.LoadAssetAtPath<Material>(GlowCameraFadeValidationMaterialPath), expectLifetimeColor: true);

            GameObject depthWall = GameObject.Find(DepthInteractionWallObjectName);
            Assert.IsNotNull(depthWall, $"Expected M11 depth interaction occluder: {DepthInteractionWallObjectName}");
            Assert.IsNotNull(depthWall.transform.parent, "Depth interaction wall should stay under a validation marker.");
            Assert.AreEqual("Smoke Test Area", depthWall.transform.parent.name, "Depth interaction wall should live under the Smoke marker.");
        }

        [Test]
        public void ValidationSceneContainsParticlePrefabPreviews()
        {
            Assert.IsTrue(File.Exists(ScenePath), $"Expected ParticleUnlit validation scene: {ScenePath}");

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid(), "ParticleMaterialTestScene must open successfully.");

            AssertParticlePreviewUsesPrefab("Dust Test Area", DustPreviewObjectName, DustPrefabPath, AssetDatabase.LoadAssetAtPath<Material>(DustMaterialPath), ParticleSystemSimulationSpace.World, ParticleSystemRenderMode.Billboard, expectColorOverLifetime: false, expectSizeOverLifetime: false);
            AssertParticlePreviewUsesPrefab("Smoke Test Area", SmokePreviewObjectName, SmokePrefabPath, AssetDatabase.LoadAssetAtPath<Material>(SmokeMaterialPath), ParticleSystemSimulationSpace.World, ParticleSystemRenderMode.Billboard, expectColorOverLifetime: true, expectSizeOverLifetime: true);
            AssertParticlePreviewUsesPrefab("Glow Test Area", GlowPreviewObjectName, GlowPrefabPath, AssetDatabase.LoadAssetAtPath<Material>(GlowMaterialPath), ParticleSystemSimulationSpace.Local, ParticleSystemRenderMode.Billboard, expectColorOverLifetime: true, expectSizeOverLifetime: false);
            AssertParticlePreviewUsesPrefab("Spark Test Area", SparkPreviewObjectName, SparkPrefabPath, AssetDatabase.LoadAssetAtPath<Material>(SparkMaterialPath), ParticleSystemSimulationSpace.Local, ParticleSystemRenderMode.Stretch, expectColorOverLifetime: false, expectSizeOverLifetime: false);
            AssertParticlePreviewUsesPrefab("Magic Test Area", MagicPreviewObjectName, MagicPrefabPath, AssetDatabase.LoadAssetAtPath<Material>(MagicMaterialPath), ParticleSystemSimulationSpace.Local, ParticleSystemRenderMode.Billboard, expectColorOverLifetime: true, expectSizeOverLifetime: true);
            AssertParticlePreviewUsesPrefab("Smoke Test Area", SmokeFlipbookPreviewObjectName, SmokeFlipbookPrefabPath, AssetDatabase.LoadAssetAtPath<Material>(SmokeFlipbookMaterialPath), ParticleSystemSimulationSpace.World, ParticleSystemRenderMode.Billboard, expectColorOverLifetime: true, expectSizeOverLifetime: true, new Vector3(2.4f, 3.1f, 0f));
            AssertParticlePreviewUsesPrefab("Magic Test Area", MagicBurstPreviewObjectName, MagicBurstPrefabPath, AssetDatabase.LoadAssetAtPath<Material>(MagicBurstMaterialPath), ParticleSystemSimulationSpace.Local, ParticleSystemRenderMode.Billboard, expectColorOverLifetime: true, expectSizeOverLifetime: true, new Vector3(2.4f, 3.1f, 0f));
            AssertParticlePreviewUsesPrefab("Spark Test Area", SparkCustomDataPreviewObjectName, SparkCustomDataPrefabPath, AssetDatabase.LoadAssetAtPath<Material>(SparkMaterialPath), ParticleSystemSimulationSpace.Local, ParticleSystemRenderMode.Stretch, expectColorOverLifetime: false, expectSizeOverLifetime: false, new Vector3(2.4f, 3.1f, 0f));
            AssertParticlePreviewUsesPrefab("Magic Test Area", MagicCustomDataPreviewObjectName, MagicCustomDataPrefabPath, AssetDatabase.LoadAssetAtPath<Material>(MagicMaterialPath), ParticleSystemSimulationSpace.Local, ParticleSystemRenderMode.Billboard, expectColorOverLifetime: true, expectSizeOverLifetime: true, new Vector3(-2.4f, 3.1f, 0f));
            AssertMarkerContainsOnlyExpectedChildren("Dust Test Area", "ParticleUnlit_BaseValidation", "ParticleUnlit_DustAlphaValidation", DustDepthValidationObjectName, DustPreviewObjectName);
            AssertMarkerContainsOnlyExpectedChildren("Smoke Test Area", "ParticleUnlit_SmokeNoiseValidation", SmokeDepthValidationObjectName, DepthInteractionWallObjectName, SmokePreviewObjectName, SmokeFlipbookPreviewObjectName);
            AssertMarkerContainsOnlyExpectedChildren("Glow Test Area", "ParticleUnlit_LifetimeValidation", "ParticleUnlit_GlowPremultiplyValidation", GlowCameraFadeValidationObjectName, GlowPreviewObjectName);
            AssertMarkerContainsOnlyExpectedChildren("Spark Test Area", "ParticleUnlit_SparkAdditiveValidation", SparkPreviewObjectName, SparkCustomDataPreviewObjectName);
            AssertMarkerContainsOnlyExpectedChildren("Magic Test Area", "ParticleUnlit_MagicDissolveValidation", MagicPreviewObjectName, MagicBurstPreviewObjectName, MagicCustomDataPreviewObjectName);
        }

        [Test]
        public void ValidationSceneContainsWebGlLoadTestCases()
        {
            Assert.IsTrue(File.Exists(ScenePath), $"Expected ParticleUnlit validation scene: {ScenePath}");

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid(), "ParticleMaterialTestScene must open successfully.");
            AssertWebGlMarkerContainsOnlyExpectedCases();

            AssertParticleLoadTestCaseUsesPrefab("WebGL Load Test Area", WebGlDust100CaseName, DustPrefabPath, AssetDatabase.LoadAssetAtPath<Material>(DustMaterialPath), 100, ParticleSystemRenderMode.Billboard);
            AssertParticleLoadTestCaseUsesPrefab("WebGL Load Test Area", WebGlDust300CaseName, DustPrefabPath, AssetDatabase.LoadAssetAtPath<Material>(DustMaterialPath), 300, ParticleSystemRenderMode.Billboard);
            AssertParticleLoadTestCaseUsesPrefab("WebGL Load Test Area", WebGlSmoke50CaseName, SmokePrefabPath, AssetDatabase.LoadAssetAtPath<Material>(SmokeMaterialPath), 50, ParticleSystemRenderMode.Billboard);
            AssertParticleLoadTestCaseUsesPrefab("WebGL Load Test Area", WebGlSpark200CaseName, SparkPrefabPath, AssetDatabase.LoadAssetAtPath<Material>(SparkMaterialPath), 200, ParticleSystemRenderMode.Stretch);
            AssertParticleLoadTestCaseUsesPrefab("WebGL Load Test Area", WebGlGlow100CaseName, GlowPrefabPath, AssetDatabase.LoadAssetAtPath<Material>(GlowMaterialPath), 100, ParticleSystemRenderMode.Billboard);
            AssertParticleLoadTestCaseUsesPrefab("WebGL Load Test Area", WebGlMagic100CaseName, MagicPrefabPath, AssetDatabase.LoadAssetAtPath<Material>(MagicMaterialPath), 100, ParticleSystemRenderMode.Billboard);
            AssertParticleMixedLoadTestCase("WebGL Load Test Area", WebGlMixedCaseName);
        }

        private static void AssertMaterialBlendState(string materialPath, Shader expectedShader, Texture expectedTexture, float expectedBlendMode, BlendMode expectedSourceBlend, BlendMode expectedDestinationBlend, Color expectedBaseColor, float expectedAlpha, float expectedBrightness)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Assert.IsNotNull(material, $"Expected generated ParticleUnlit material: {materialPath}");
            Assert.AreSame(expectedShader, material.shader, $"Unexpected shader on material: {materialPath}");
            Assert.AreSame(expectedTexture, material.GetTexture("_BaseMap"), $"Unexpected base texture on material: {materialPath}");
            Assert.AreEqual(expectedBlendMode, material.GetFloat("_BlendMode"), $"Unexpected blend mode on material: {materialPath}");
            Assert.AreEqual((float)expectedSourceBlend, material.GetFloat("_SrcBlend"), $"Unexpected source blend on material: {materialPath}");
            Assert.AreEqual((float)expectedDestinationBlend, material.GetFloat("_DstBlend"), $"Unexpected destination blend on material: {materialPath}");
            Assert.AreEqual((int)RenderQueue.Transparent, material.renderQueue, $"ParticleUnlit material should stay in Transparent queue: {materialPath}");
            Assert.That(material.GetColor("_BaseColor"), Is.EqualTo(expectedBaseColor).Using(ColorEqualityComparer.Instance), $"Unexpected base color on material: {materialPath}");
            Assert.AreEqual(expectedAlpha, material.GetFloat("_Alpha"), $"Unexpected alpha on material: {materialPath}");
            Assert.AreEqual(expectedBrightness, material.GetFloat("_Brightness"), $"Unexpected brightness on material: {materialPath}");
        }

        private static void AssertFlipbookMaterial(string materialPath, Shader expectedShader, Texture expectedBaseTexture, Texture expectedMaskTexture, Texture expectedNoiseTexture, float expectedBlendMode, Color expectedBaseColor, float expectedAlpha, float expectedBrightness)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Assert.IsNotNull(material, $"Expected generated ParticleUnlit material: {materialPath}");
            Assert.AreSame(expectedShader, material.shader, $"Unexpected shader on material: {materialPath}");
            Assert.AreSame(expectedBaseTexture, material.GetTexture("_BaseMap"), $"Unexpected flipbook atlas on material: {materialPath}");
            Assert.AreSame(expectedMaskTexture, material.GetTexture("_MaskMap"), $"Unexpected mask texture on material: {materialPath}");
            Assert.AreSame(expectedNoiseTexture, material.GetTexture("_NoiseMap"), $"Unexpected noise texture on material: {materialPath}");
            Assert.AreEqual(expectedBlendMode, material.GetFloat("_BlendMode"), $"Unexpected blend mode on material: {materialPath}");
            Assert.That(material.GetColor("_BaseColor"), Is.EqualTo(expectedBaseColor).Using(ColorEqualityComparer.Instance), $"Unexpected base color on material: {materialPath}");
            Assert.AreEqual(expectedAlpha, material.GetFloat("_Alpha"), $"Unexpected alpha on material: {materialPath}");
            Assert.AreEqual(expectedBrightness, material.GetFloat("_Brightness"), $"Unexpected brightness on material: {materialPath}");
        }

        private static void AssertParticleAnchorUsesMaterial(string expectedParentName, string objectName, Material expectedMaterial, bool expectLifetimeColor)
        {
            GameObject anchorObject = GameObject.Find(objectName);
            Assert.IsNotNull(anchorObject, $"Expected validation particle object: {objectName}");
            Assert.IsNotNull(anchorObject.transform.parent, $"Expected validation particle parent for: {objectName}");
            Assert.AreEqual(expectedParentName, anchorObject.transform.parent.name, $"Unexpected validation marker for: {objectName}");

            ParticleSystem particleSystem = anchorObject.GetComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = anchorObject.GetComponent<ParticleSystemRenderer>();
            Assert.IsNotNull(particleSystem, $"Expected ParticleSystem on validation object: {objectName}");
            Assert.IsNotNull(renderer, $"Expected ParticleSystemRenderer on validation object: {objectName}");
            Assert.AreSame(expectedMaterial, renderer.sharedMaterial, $"Unexpected material on validation object: {objectName}");
            Assert.AreEqual(ParticleSystemRenderMode.Billboard, renderer.renderMode, $"Unexpected render mode on validation object: {objectName}");
            Assert.AreEqual(ParticleSystemRenderSpace.View, renderer.alignment, $"Unexpected renderer alignment on validation object: {objectName}");
            Assert.AreEqual(0f, renderer.sortingFudge, $"Unexpected sorting fudge on validation object: {objectName}");
            Assert.AreEqual(ShadowCastingMode.Off, renderer.shadowCastingMode, $"Unexpected shadow casting mode on validation object: {objectName}");
            Assert.IsFalse(renderer.receiveShadows, $"Unexpected shadow receiving state on validation object: {objectName}");
            Assert.AreEqual(LightProbeUsage.Off, renderer.lightProbeUsage, $"Unexpected light probe usage on validation object: {objectName}");
            Assert.AreEqual(ReflectionProbeUsage.Off, renderer.reflectionProbeUsage, $"Unexpected reflection probe usage on validation object: {objectName}");

            var colorOverLifetime = particleSystem.colorOverLifetime;
            Assert.AreEqual(expectLifetimeColor, colorOverLifetime.enabled, $"Unexpected Color over Lifetime state on validation object: {objectName}");
        }

        private static void AssertParticlePrefabUsesProfile(string prefabPath, string expectedRootName, Material expectedMaterial, ParticleSystemSimulationSpace expectedSimulationSpace, ParticleSystemRenderMode expectedRenderMode, ParticleSystemShapeType expectedShapeType, float expectedLifetime, float expectedSpeed, float expectedSize, int expectedMaxParticles, float expectedEmissionRate, bool expectColorOverLifetime, bool expectSizeOverLifetime)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, $"Expected generated ParticleUnlit prefab: {prefabPath}");
            Assert.IsNotNull(expectedMaterial, $"Expected generated ParticleUnlit material: {prefabPath}");
            Assert.AreEqual(expectedRootName, prefab.name, $"Unexpected prefab root name: {prefabPath}");

            ParticleSystem particleSystem = prefab.GetComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = prefab.GetComponent<ParticleSystemRenderer>();
            Assert.IsNotNull(particleSystem, $"Expected ParticleSystem on prefab: {prefabPath}");
            Assert.IsNotNull(renderer, $"Expected ParticleSystemRenderer on prefab: {prefabPath}");

            var main = particleSystem.main;
            Assert.IsTrue(main.loop, $"Prefab should loop by default: {prefabPath}");
            Assert.IsTrue(main.playOnAwake, $"Prefab should autoplay by default: {prefabPath}");
            Assert.AreEqual(expectedSimulationSpace, main.simulationSpace, $"Unexpected simulation space on prefab: {prefabPath}");
            Assert.AreEqual(expectedLifetime, main.startLifetime.constant, 0.0001f, $"Unexpected lifetime on prefab: {prefabPath}");
            Assert.AreEqual(expectedSpeed, main.startSpeed.constant, 0.0001f, $"Unexpected speed on prefab: {prefabPath}");
            Assert.AreEqual(expectedSize, main.startSize.constant, 0.0001f, $"Unexpected size on prefab: {prefabPath}");
            Assert.AreEqual(expectedMaxParticles, main.maxParticles, $"Unexpected max particles on prefab: {prefabPath}");

            var emission = particleSystem.emission;
            Assert.IsTrue(emission.enabled, $"Expected emission enabled on prefab: {prefabPath}");
            Assert.AreEqual(expectedEmissionRate, emission.rateOverTime.constant, 0.0001f, $"Unexpected emission rate on prefab: {prefabPath}");

            var shape = particleSystem.shape;
            Assert.IsTrue(shape.enabled, $"Expected shape enabled on prefab: {prefabPath}");
            Assert.AreEqual(expectedShapeType, shape.shapeType, $"Unexpected shape type on prefab: {prefabPath}");

            Assert.AreSame(expectedMaterial, renderer.sharedMaterial, $"Unexpected material on prefab: {prefabPath}");
            Assert.AreEqual(expectedRenderMode, renderer.renderMode, $"Unexpected render mode on prefab: {prefabPath}");
            Assert.AreEqual(ParticleSystemRenderSpace.View, renderer.alignment, $"Unexpected renderer alignment on prefab: {prefabPath}");
            Assert.AreEqual(0f, renderer.sortingFudge, $"Unexpected sorting fudge on prefab: {prefabPath}");
            AssertRendererUsesVertexStreams(renderer, ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV);

            Assert.AreEqual(expectColorOverLifetime, particleSystem.colorOverLifetime.enabled, $"Unexpected Color over Lifetime state on prefab: {prefabPath}");
            Assert.AreEqual(expectSizeOverLifetime, particleSystem.sizeOverLifetime.enabled, $"Unexpected Size over Lifetime state on prefab: {prefabPath}");

            if (expectedRenderMode == ParticleSystemRenderMode.Stretch)
            {
                Assert.Greater(renderer.lengthScale, 1f, $"Spark-style prefab should stretch along motion: {prefabPath}");
            }
        }

        private static void AssertParticlePreviewUsesPrefab(string expectedParentName, string objectName, string prefabPath, Material expectedMaterial, ParticleSystemSimulationSpace expectedSimulationSpace, ParticleSystemRenderMode expectedRenderMode, bool expectColorOverLifetime, bool expectSizeOverLifetime)
        {
            AssertParticlePreviewUsesPrefab(expectedParentName, objectName, prefabPath, expectedMaterial, expectedSimulationSpace, expectedRenderMode, expectColorOverLifetime, expectSizeOverLifetime, new Vector3(0f, 3.1f, 0f));
        }

        private static void AssertParticlePreviewUsesPrefab(string expectedParentName, string objectName, string prefabPath, Material expectedMaterial, ParticleSystemSimulationSpace expectedSimulationSpace, ParticleSystemRenderMode expectedRenderMode, bool expectColorOverLifetime, bool expectSizeOverLifetime, Vector3 expectedLocalPosition)
        {
            GameObject previewObject = GameObject.Find(objectName);
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(previewObject, $"Expected prefab preview object: {objectName}");
            Assert.IsNotNull(prefabAsset, $"Expected generated prefab asset: {prefabPath}");
            Assert.IsNotNull(previewObject.transform.parent, $"Expected preview parent for: {objectName}");
            Assert.AreEqual(expectedParentName, previewObject.transform.parent.name, $"Unexpected preview marker for: {objectName}");
            Assert.That(previewObject.transform.localPosition, Is.EqualTo(expectedLocalPosition).Using(Vector3EqualityComparer.Instance), $"Unexpected preview local position for: {objectName}");
            Assert.AreSame(prefabAsset, PrefabUtility.GetCorrespondingObjectFromSource(previewObject), $"Preview object must stay linked to its prefab asset: {objectName}");

            ParticleSystem particleSystem = previewObject.GetComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = previewObject.GetComponent<ParticleSystemRenderer>();
            Assert.IsNotNull(particleSystem, $"Expected ParticleSystem on preview object: {objectName}");
            Assert.IsNotNull(renderer, $"Expected ParticleSystemRenderer on preview object: {objectName}");
            Assert.AreSame(expectedMaterial, renderer.sharedMaterial, $"Unexpected material on preview object: {objectName}");
            Assert.AreEqual(expectedSimulationSpace, particleSystem.main.simulationSpace, $"Unexpected simulation space on preview object: {objectName}");
            Assert.AreEqual(expectedRenderMode, renderer.renderMode, $"Unexpected render mode on preview object: {objectName}");
            Assert.AreEqual(expectColorOverLifetime, particleSystem.colorOverLifetime.enabled, $"Unexpected Color over Lifetime state on preview object: {objectName}");
            Assert.AreEqual(expectSizeOverLifetime, particleSystem.sizeOverLifetime.enabled, $"Unexpected Size over Lifetime state on preview object: {objectName}");
        }

        private static void AssertFlipbookPrefabUsesProfile(string prefabPath, string expectedRootName, Material expectedMaterial, ParticleSystemSimulationSpace expectedSimulationSpace, float expectedLifetime, float expectedSpeed, float expectedSize, int expectedMaxParticles, float expectedEmissionRate, float expectedShapeRadius, float expectedMidCurveValue)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, $"Expected generated flipbook prefab: {prefabPath}");
            Assert.IsNotNull(expectedMaterial, $"Expected generated flipbook material: {prefabPath}");
            Assert.AreEqual(expectedRootName, prefab.name, $"Unexpected prefab root name: {prefabPath}");

            ParticleSystem particleSystem = prefab.GetComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = prefab.GetComponent<ParticleSystemRenderer>();
            Assert.IsNotNull(particleSystem, $"Expected ParticleSystem on flipbook prefab: {prefabPath}");
            Assert.IsNotNull(renderer, $"Expected ParticleSystemRenderer on flipbook prefab: {prefabPath}");

            var main = particleSystem.main;
            Assert.AreEqual(expectedSimulationSpace, main.simulationSpace, $"Unexpected simulation space on flipbook prefab: {prefabPath}");
            Assert.AreEqual(expectedLifetime, main.startLifetime.constant, 0.0001f, $"Unexpected lifetime on flipbook prefab: {prefabPath}");
            Assert.AreEqual(expectedSpeed, main.startSpeed.constant, 0.0001f, $"Unexpected speed on flipbook prefab: {prefabPath}");
            Assert.AreEqual(expectedSize, main.startSize.constant, 0.0001f, $"Unexpected size on flipbook prefab: {prefabPath}");
            Assert.AreEqual(expectedMaxParticles, main.maxParticles, $"Unexpected max particles on flipbook prefab: {prefabPath}");

            var emission = particleSystem.emission;
            Assert.IsTrue(emission.enabled, $"Expected emission enabled on flipbook prefab: {prefabPath}");
            Assert.AreEqual(expectedEmissionRate, emission.rateOverTime.constant, 0.0001f, $"Unexpected emission rate on flipbook prefab: {prefabPath}");

            var shape = particleSystem.shape;
            Assert.IsTrue(shape.enabled, $"Expected shape enabled on flipbook prefab: {prefabPath}");
            Assert.AreEqual(ParticleSystemShapeType.Circle, shape.shapeType, $"Unexpected shape type on flipbook prefab: {prefabPath}");
            Assert.AreEqual(expectedShapeRadius, shape.radius, 0.0001f, $"Unexpected shape radius on flipbook prefab: {prefabPath}");

            var textureSheetAnimation = particleSystem.textureSheetAnimation;
            Assert.IsTrue(textureSheetAnimation.enabled, $"Flipbook prefab must enable Texture Sheet Animation: {prefabPath}");
            Assert.AreEqual(ParticleSystemAnimationMode.Grid, textureSheetAnimation.mode, $"Unexpected flipbook mode on prefab: {prefabPath}");
            Assert.AreEqual(ParticleSystemAnimationType.WholeSheet, textureSheetAnimation.animation, $"Unexpected flipbook animation type on prefab: {prefabPath}");
            Assert.AreEqual(4, textureSheetAnimation.numTilesX, $"Unexpected flipbook tile count X on prefab: {prefabPath}");
            Assert.AreEqual(4, textureSheetAnimation.numTilesY, $"Unexpected flipbook tile count Y on prefab: {prefabPath}");
            Assert.AreEqual(ParticleSystemCurveMode.TwoConstants, textureSheetAnimation.startFrame.mode, $"Flipbook prefabs should randomize start frame: {prefabPath}");
            Assert.AreEqual(0f, textureSheetAnimation.startFrame.constantMin, 0.0001f, $"Unexpected start frame min on prefab: {prefabPath}");
            Assert.AreEqual(1f, textureSheetAnimation.startFrame.constantMax, 0.0001f, $"Unexpected start frame max on prefab: {prefabPath}");
            Assert.AreEqual(ParticleSystemCurveMode.Curve, textureSheetAnimation.frameOverTime.mode, $"Flipbook prefabs should animate frame over time with a curve: {prefabPath}");
            Assert.IsNotNull(textureSheetAnimation.frameOverTime.curve, $"Flipbook frame curve must exist: {prefabPath}");
            Assert.AreEqual(0f, textureSheetAnimation.frameOverTime.curve.Evaluate(0f), 0.0001f, $"Flipbook curve should start at frame 0: {prefabPath}");
            Assert.AreEqual(expectedMidCurveValue, textureSheetAnimation.frameOverTime.curve.Evaluate(0.3f), 0.08f, $"Unexpected flipbook curve midpoint on prefab: {prefabPath}");
            Assert.AreEqual(1f, textureSheetAnimation.frameOverTime.curve.Evaluate(1f), 0.0001f, $"Flipbook curve should reach the final frame by the end of life: {prefabPath}");

            Assert.AreSame(expectedMaterial, renderer.sharedMaterial, $"Unexpected material on flipbook prefab: {prefabPath}");
            Assert.AreEqual(ParticleSystemRenderMode.Billboard, renderer.renderMode, $"Unexpected render mode on flipbook prefab: {prefabPath}");
            AssertRendererUsesVertexStreams(renderer, ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV);
        }

        private static void AssertCustomDataPrefabUsesProfile(
            string prefabPath,
            string expectedRootName,
            Material expectedMaterial,
            ParticleSystemSimulationSpace expectedSimulationSpace,
            ParticleSystemRenderMode expectedRenderMode,
            ParticleSystemShapeType expectedShapeType,
            float expectedLifetime,
            float expectedSpeed,
            float expectedSize,
            int expectedMaxParticles,
            float expectedEmissionRate,
            float expectedShapeRadius,
            float expectedDissolveStart,
            float expectedDissolveMid,
            float expectedDissolveEnd,
            float expectedEmissionStart,
            float expectedEmissionMid,
            float expectedEmissionEnd,
            float expectedNoiseMin,
            float expectedNoiseMax)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, $"Expected generated custom-data prefab: {prefabPath}");
            Assert.IsNotNull(expectedMaterial, $"Expected generated custom-data material: {prefabPath}");
            Assert.AreEqual(expectedRootName, prefab.name, $"Unexpected prefab root name: {prefabPath}");

            ParticleSystem particleSystem = prefab.GetComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = prefab.GetComponent<ParticleSystemRenderer>();
            Assert.IsNotNull(particleSystem, $"Expected ParticleSystem on custom-data prefab: {prefabPath}");
            Assert.IsNotNull(renderer, $"Expected ParticleSystemRenderer on custom-data prefab: {prefabPath}");

            var main = particleSystem.main;
            Assert.AreEqual(expectedSimulationSpace, main.simulationSpace, $"Unexpected simulation space on custom-data prefab: {prefabPath}");
            Assert.AreEqual(expectedLifetime, main.startLifetime.constant, 0.0001f, $"Unexpected lifetime on custom-data prefab: {prefabPath}");
            Assert.AreEqual(expectedSpeed, main.startSpeed.constant, 0.0001f, $"Unexpected speed on custom-data prefab: {prefabPath}");
            Assert.AreEqual(expectedSize, main.startSize.constant, 0.0001f, $"Unexpected size on custom-data prefab: {prefabPath}");
            Assert.AreEqual(expectedMaxParticles, main.maxParticles, $"Unexpected max particles on custom-data prefab: {prefabPath}");

            var emission = particleSystem.emission;
            Assert.IsTrue(emission.enabled, $"Expected emission enabled on custom-data prefab: {prefabPath}");
            Assert.AreEqual(expectedEmissionRate, emission.rateOverTime.constant, 0.0001f, $"Unexpected emission rate on custom-data prefab: {prefabPath}");

            var shape = particleSystem.shape;
            Assert.IsTrue(shape.enabled, $"Expected shape enabled on custom-data prefab: {prefabPath}");
            Assert.AreEqual(expectedShapeType, shape.shapeType, $"Unexpected shape type on custom-data prefab: {prefabPath}");
            Assert.AreEqual(expectedShapeRadius, shape.radius, 0.0001f, $"Unexpected shape radius on custom-data prefab: {prefabPath}");

            Assert.AreSame(expectedMaterial, renderer.sharedMaterial, $"Unexpected material on custom-data prefab: {prefabPath}");
            Assert.AreEqual(expectedRenderMode, renderer.renderMode, $"Unexpected render mode on custom-data prefab: {prefabPath}");
            AssertRendererUsesVertexStreams(renderer, ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV, ParticleSystemVertexStream.Custom1XYZW);

            var customData = particleSystem.customData;
            Assert.IsTrue(customData.enabled, $"Custom data module must be enabled on custom-data prefab: {prefabPath}");
            Assert.AreEqual(ParticleSystemCustomDataMode.Vector, customData.GetMode(ParticleSystemCustomData.Custom1), $"Custom1 must use Vector mode: {prefabPath}");
            Assert.AreEqual(4, customData.GetVectorComponentCount(ParticleSystemCustomData.Custom1), $"Custom1 must expose four components: {prefabPath}");
            Assert.AreEqual(ParticleSystemCustomDataMode.Disabled, customData.GetMode(ParticleSystemCustomData.Custom2), $"Custom2 must remain reserved on custom-data prefab: {prefabPath}");

            ParticleSystem.MinMaxCurve dissolveDelta = customData.GetVector(ParticleSystemCustomData.Custom1, 0);
            ParticleSystem.MinMaxCurve emissionDelta = customData.GetVector(ParticleSystemCustomData.Custom1, 1);
            ParticleSystem.MinMaxCurve noiseOffset = customData.GetVector(ParticleSystemCustomData.Custom1, 2);
            ParticleSystem.MinMaxCurve variantIndex = customData.GetVector(ParticleSystemCustomData.Custom1, 3);

            Assert.AreEqual(ParticleSystemCurveMode.Curve, dissolveDelta.mode, $"Dissolve delta should animate over lifetime: {prefabPath}");
            Assert.AreEqual(expectedDissolveStart, dissolveDelta.curve.Evaluate(0f), 0.0001f, $"Unexpected dissolve delta at birth: {prefabPath}");
            Assert.AreEqual(expectedDissolveMid, dissolveDelta.curve.Evaluate(0.5f), 0.08f, $"Unexpected dissolve delta mid-life: {prefabPath}");
            Assert.AreEqual(expectedDissolveEnd, dissolveDelta.curve.Evaluate(1f), 0.0001f, $"Unexpected dissolve delta at end-of-life: {prefabPath}");

            Assert.AreEqual(ParticleSystemCurveMode.Curve, emissionDelta.mode, $"Emission delta should animate over lifetime: {prefabPath}");
            Assert.AreEqual(expectedEmissionStart, emissionDelta.curve.Evaluate(0f), 0.0001f, $"Unexpected emission delta at birth: {prefabPath}");
            Assert.AreEqual(expectedEmissionMid, emissionDelta.curve.Evaluate(0.35f), 0.12f, $"Unexpected emission delta mid-life: {prefabPath}");
            Assert.AreEqual(expectedEmissionEnd, emissionDelta.curve.Evaluate(1f), 0.0001f, $"Unexpected emission delta at end-of-life: {prefabPath}");

            Assert.AreEqual(ParticleSystemCurveMode.TwoConstants, noiseOffset.mode, $"Noise offset should use a random range on custom-data prefab: {prefabPath}");
            Assert.AreEqual(expectedNoiseMin, noiseOffset.constantMin, 0.0001f, $"Unexpected custom noise offset min: {prefabPath}");
            Assert.AreEqual(expectedNoiseMax, noiseOffset.constantMax, 0.0001f, $"Unexpected custom noise offset max: {prefabPath}");

            Assert.AreEqual(ParticleSystemCurveMode.Constant, variantIndex.mode, $"Variant index must stay reserved by default: {prefabPath}");
            Assert.AreEqual(0f, variantIndex.constant, 0.0001f, $"Variant index should default to zero: {prefabPath}");
        }

        private static void AssertParticleLoadTestCaseUsesPrefab(string expectedParentName, string objectName, string prefabPath, Material expectedMaterial, int expectedMaxParticles, ParticleSystemRenderMode expectedRenderMode)
        {
            GameObject loadTestObject = GameObject.Find(objectName);
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(loadTestObject, $"Expected WebGL load test object: {objectName}");
            Assert.IsNotNull(prefabAsset, $"Expected prefab asset for WebGL load test: {prefabPath}");
            Assert.IsNotNull(loadTestObject.transform.parent, $"Expected WebGL load test parent for: {objectName}");
            Assert.AreEqual(expectedParentName, loadTestObject.transform.parent.name, $"Unexpected WebGL load test marker for: {objectName}");
            Assert.AreSame(prefabAsset, PrefabUtility.GetCorrespondingObjectFromSource(loadTestObject), $"WebGL load test object must stay linked to its prefab asset: {objectName}");

            ParticleSystem particleSystem = loadTestObject.GetComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = loadTestObject.GetComponent<ParticleSystemRenderer>();
            Assert.IsNotNull(particleSystem, $"Expected ParticleSystem on WebGL load test object: {objectName}");
            Assert.IsNotNull(renderer, $"Expected ParticleSystemRenderer on WebGL load test object: {objectName}");
            Assert.AreSame(expectedMaterial, renderer.sharedMaterial, $"Unexpected material on WebGL load test object: {objectName}");
            Assert.AreEqual(expectedMaxParticles, particleSystem.main.maxParticles, $"Unexpected max particle contract on WebGL load test object: {objectName}");
            Assert.IsTrue(particleSystem.emission.enabled, $"WebGL load test case must keep emission enabled: {objectName}");
            Assert.Greater(particleSystem.emission.rateOverTime.constant, 0f, $"WebGL load test case must emit particles: {objectName}");
            Assert.AreEqual(expectedRenderMode, renderer.renderMode, $"Unexpected render mode on WebGL load test object: {objectName}");
            Assert.IsFalse(renderer.enableGPUInstancing, $"WebGL load test case must keep GPU instancing disabled: {objectName}");
        }

        private static void AssertParticleMixedLoadTestCase(string expectedParentName, string objectName)
        {
            GameObject mixedRoot = GameObject.Find(objectName);
            Assert.IsNotNull(mixedRoot, $"Expected WebGL mixed load test root: {objectName}");
            Assert.IsNotNull(mixedRoot.transform.parent, $"Expected mixed load test parent for: {objectName}");
            Assert.AreEqual(expectedParentName, mixedRoot.transform.parent.name, $"Unexpected WebGL mixed load marker for: {objectName}");
            Assert.AreEqual(5, mixedRoot.transform.childCount, $"Mixed WebGL load case must contain five prefab instances: {objectName}");

            AssertChildLoadCaseUsesPrefab(mixedRoot.transform, "Dust", DustPrefabPath, 90);
            AssertChildLoadCaseUsesPrefab(mixedRoot.transform, "Smoke", SmokePrefabPath, 40);
            AssertChildLoadCaseUsesPrefab(mixedRoot.transform, "Glow", GlowPrefabPath, 70);
            AssertChildLoadCaseUsesPrefab(mixedRoot.transform, "Spark", SparkPrefabPath, 120);
            AssertChildLoadCaseUsesPrefab(mixedRoot.transform, "Magic", MagicPrefabPath, 70);
        }

        private static void AssertChildLoadCaseUsesPrefab(Transform parent, string childName, string prefabPath, int expectedMaxParticles)
        {
            Transform childTransform = parent.Find(childName);
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(childTransform, $"Expected mixed WebGL child: {childName}");
            Assert.IsNotNull(prefabAsset, $"Expected mixed WebGL prefab asset: {prefabPath}");
            Assert.AreSame(prefabAsset, PrefabUtility.GetCorrespondingObjectFromSource(childTransform.gameObject), $"Mixed WebGL child must stay linked to its prefab asset: {childName}");

            ParticleSystem particleSystem = childTransform.GetComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = childTransform.GetComponent<ParticleSystemRenderer>();
            Assert.IsNotNull(particleSystem, $"Expected ParticleSystem on mixed WebGL child: {childName}");
            Assert.IsNotNull(renderer, $"Expected ParticleSystemRenderer on mixed WebGL child: {childName}");
            Assert.AreEqual(expectedMaxParticles, particleSystem.main.maxParticles, $"Unexpected mixed WebGL particle count on child: {childName}");
            Assert.IsFalse(renderer.enableGPUInstancing, $"Mixed WebGL child must keep GPU instancing disabled: {childName}");
        }

        private static void AssertDebugModeDisabled(string materialPath)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Assert.IsNotNull(material, $"Expected generated ParticleUnlit material: {materialPath}");
            Assert.AreEqual(0f, material.GetFloat("_DebugMode"), $"Generated ParticleUnlit materials must keep Debug Mode disabled by default: {materialPath}");
        }

        private static void AssertWebGlMarkerContainsOnlyExpectedCases()
        {
            GameObject markerObject = GameObject.Find("WebGL Load Test Area");
            Assert.IsNotNull(markerObject, "Expected WebGL Load Test Area marker.");

            string[] expectedNames =
            {
                WebGlDust100CaseName,
                WebGlDust300CaseName,
                WebGlSmoke50CaseName,
                WebGlSpark200CaseName,
                WebGlGlow100CaseName,
                WebGlMagic100CaseName,
                WebGlMixedCaseName
            };

            string[] childNames = markerObject.transform
                .Cast<Transform>()
                .Select(child => child.name)
                .OrderBy(name => name)
                .ToArray();

            CollectionAssert.AreEquivalent(expectedNames, childNames, "WebGL Load Test Area must contain only the expected M7 load-test cases.");
        }

        private static void AssertContainsInOrder(string source, params string[] values)
        {
            int currentIndex = -1;
            foreach (string value in values)
            {
                int nextIndex = source.IndexOf(value, currentIndex + 1, StringComparison.Ordinal);
                Assert.GreaterOrEqual(nextIndex, 0, $"Expected source to contain '{value}' after index {currentIndex}.");
                currentIndex = nextIndex;
            }
        }

        private static void AssertTextureImportSettings(string texturePath, bool expectedSrgb, TextureWrapMode expectedWrapMode)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            Assert.IsNotNull(texture, $"Expected generated texture: {texturePath}");
            Assert.IsNotNull(importer, $"Expected TextureImporter: {texturePath}");
            Assert.AreEqual(expectedSrgb, importer.sRGBTexture, $"Unexpected sRGB setting: {texturePath}");
            Assert.AreEqual(expectedWrapMode, importer.wrapMode, $"Unexpected wrap mode: {texturePath}");
            Assert.AreEqual(FilterMode.Bilinear, importer.filterMode, $"Unexpected filter mode: {texturePath}");
            Assert.IsFalse(importer.mipmapEnabled, $"Generated validation textures should not generate mipmaps: {texturePath}");
        }

        private static void AssertNoiseDissolveMaterial(string materialPath, Shader expectedShader, Texture expectedBaseTexture, Texture expectedMaskTexture, Texture expectedNoiseTexture, float expectedBlendMode, float expectedMaskStrength, float expectedNoiseStrength, float expectedNoiseScale, Vector4 expectedNoiseScrollSpeed, float expectedDissolveAmount, float expectedDissolveSoftness)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Assert.IsNotNull(material, $"Expected generated ParticleUnlit material: {materialPath}");
            Assert.AreSame(expectedShader, material.shader, $"Unexpected shader on material: {materialPath}");
            Assert.AreSame(expectedBaseTexture, material.GetTexture("_BaseMap"), $"Unexpected base texture on material: {materialPath}");
            Assert.AreSame(expectedMaskTexture, material.GetTexture("_MaskMap"), $"Unexpected mask texture on material: {materialPath}");
            Assert.AreSame(expectedNoiseTexture, material.GetTexture("_NoiseMap"), $"Unexpected noise texture on material: {materialPath}");
            Assert.AreEqual(expectedBlendMode, material.GetFloat("_BlendMode"), $"Unexpected blend mode on material: {materialPath}");
            Assert.AreEqual(expectedMaskStrength, material.GetFloat("_MaskStrength"), $"Unexpected mask strength on material: {materialPath}");
            Assert.AreEqual(expectedNoiseStrength, material.GetFloat("_NoiseStrength"), $"Unexpected noise strength on material: {materialPath}");
            Assert.AreEqual(expectedNoiseScale, material.GetFloat("_NoiseScale"), $"Unexpected noise scale on material: {materialPath}");
            Assert.That(material.GetVector("_NoiseScrollSpeed"), Is.EqualTo(expectedNoiseScrollSpeed), $"Unexpected noise scroll speed on material: {materialPath}");
            Assert.AreEqual(0f, material.GetFloat("_NoiseSpace"), $"Unexpected noise space on material: {materialPath}");
            Assert.AreEqual(expectedDissolveAmount, material.GetFloat("_DissolveAmount"), $"Unexpected dissolve amount on material: {materialPath}");
            Assert.AreEqual(expectedDissolveSoftness, material.GetFloat("_DissolveSoftness"), $"Unexpected dissolve softness on material: {materialPath}");
        }

        private static void AssertEmissionMaterial(string materialPath, Shader expectedShader, Texture expectedBaseTexture, Texture expectedMaskTexture, Texture expectedNoiseTexture, Color expectedEmissionColor, float expectedEmissionStrength, float expectedEmissionAlphaInfluence)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Assert.IsNotNull(material, $"Expected generated ParticleUnlit material: {materialPath}");
            Assert.AreSame(expectedShader, material.shader, $"Unexpected shader on material: {materialPath}");
            Assert.AreSame(expectedBaseTexture, material.GetTexture("_BaseMap"), $"Unexpected base texture on material: {materialPath}");
            Assert.AreSame(expectedMaskTexture, material.GetTexture("_MaskMap"), $"Unexpected mask texture on material: {materialPath}");

            if (expectedNoiseTexture == null)
            {
                Assert.IsNull(material.GetTexture("_NoiseMap"), $"Unexpected noise texture on material: {materialPath}");
            }
            else
            {
                Assert.AreSame(expectedNoiseTexture, material.GetTexture("_NoiseMap"), $"Unexpected noise texture on material: {materialPath}");
            }

            Assert.That(material.GetColor("_EmissionColor"), Is.EqualTo(expectedEmissionColor).Using(ColorEqualityComparer.Instance), $"Unexpected emission color on material: {materialPath}");
            Assert.AreEqual(expectedEmissionStrength, material.GetFloat("_EmissionStrength"), $"Unexpected emission strength on material: {materialPath}");
            Assert.AreEqual(expectedEmissionAlphaInfluence, material.GetFloat("_EmissionAlphaInfluence"), $"Unexpected emission alpha influence on material: {materialPath}");
        }

        private static void AssertTextureShowsVariation(string texturePath, string message)
        {
            Texture2D texture = LoadTexturePixels(texturePath);

            try
            {
                Color bottomLeft = texture.GetPixel(0, 0);
                Color center = texture.GetPixel(texture.width / 2, texture.height / 2);
                Color topRight = texture.GetPixel(texture.width - 1, texture.height - 1);

                bool differsFromBottomLeft = !ColorEqualityComparer.Instance.Equals(bottomLeft, center) || !ColorEqualityComparer.Instance.Equals(bottomLeft, topRight);
                Assert.IsTrue(differsFromBottomLeft, message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void AssertMaskTextureUsesPackedChannels(string texturePath)
        {
            Texture2D texture = LoadTexturePixels(texturePath);

            try
            {
                Color center = texture.GetPixel(texture.width / 2, texture.height / 2);
                Color corner = texture.GetPixel(0, 0);
                Color topCenter = texture.GetPixel(texture.width / 2, texture.height - 1);
                Color bottomCenter = texture.GetPixel(texture.width / 2, 0);

                Assert.Greater(center.a, corner.a, "MaskMap.a should provide a stronger center shape mask than the corner.");
                Assert.Greater(topCenter.g, bottomCenter.g, "MaskMap.g should encode a usable emission mask gradient.");
                Assert.IsFalse(Mathf.Approximately(center.r, center.b) && Mathf.Approximately(corner.r, corner.b), "MaskMap.r and MaskMap.b should encode different packed data roles.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void AssertFlipbookTextureUsesSafeTilePadding(string texturePath, string message)
        {
            Texture2D texture = LoadTexturePixels(texturePath);

            try
            {
                int tileWidth = texture.width / 4;
                int tileHeight = texture.height / 4;
                Color leftBorder = texture.GetPixel(0, tileHeight / 2);
                Color leftInset = texture.GetPixel(Mathf.Max(1, tileWidth / 16), tileHeight / 2);
                Color rightBorder = texture.GetPixel(tileWidth - 1, tileHeight / 2);
                Color rightInset = texture.GetPixel(tileWidth - 1 - Mathf.Max(1, tileWidth / 16), tileHeight / 2);
                Color bottomBorder = texture.GetPixel(tileWidth / 2, 0);
                Color bottomInset = texture.GetPixel(tileWidth / 2, Mathf.Max(1, tileHeight / 16));

                bool keepsPaddedBorder = ColorApproximately(leftBorder, leftInset)
                    && ColorApproximately(rightBorder, rightInset)
                    && ColorApproximately(bottomBorder, bottomInset);
                Assert.IsTrue(keepsPaddedBorder, message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void AssertMarkerContainsOnlyExpectedChildren(string markerName, params string[] expectedChildNames)
        {
            GameObject markerObject = GameObject.Find(markerName);
            Assert.IsNotNull(markerObject, $"Expected marker object: {markerName}");

            string[] childNames = markerObject.transform
                .Cast<Transform>()
                .Select(child => child.name)
                .OrderBy(name => name)
                .ToArray();

            CollectionAssert.AreEquivalent(expectedChildNames, childNames, $"Marker must contain only the expected child set: {markerName}");
        }

        private static void AssertRendererUsesVertexStreams(ParticleSystemRenderer renderer, params ParticleSystemVertexStream[] expectedStreams)
        {
            List<ParticleSystemVertexStream> actualStreams = new List<ParticleSystemVertexStream>();
            renderer.GetActiveVertexStreams(actualStreams);
            CollectionAssert.AreEqual(expectedStreams, actualStreams, "Unexpected active vertex stream contract.");
        }

        private static bool ColorApproximately(Color left, Color right)
        {
            return Mathf.Abs(left.r - right.r) <= 0.08f
                && Mathf.Abs(left.g - right.g) <= 0.08f
                && Mathf.Abs(left.b - right.b) <= 0.08f
                && Mathf.Abs(left.a - right.a) <= 0.08f;
        }

        private static Texture2D LoadTexturePixels(string texturePath)
        {
            Assert.IsTrue(File.Exists(texturePath), $"Expected texture file to exist: {texturePath}");

            byte[] pngBytes = File.ReadAllBytes(texturePath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            bool loaded = texture.LoadImage(pngBytes, false);
            Assert.IsTrue(loaded, $"Expected texture bytes to decode successfully: {texturePath}");
            return texture;
        }

        private sealed class ColorEqualityComparer : IEqualityComparer<Color>
        {
            internal static readonly ColorEqualityComparer Instance = new ColorEqualityComparer();

            public bool Equals(Color x, Color y)
            {
                return Mathf.Approximately(x.r, y.r)
                    && Mathf.Approximately(x.g, y.g)
                    && Mathf.Approximately(x.b, y.b)
                    && Mathf.Approximately(x.a, y.a);
            }

            public int GetHashCode(Color obj)
            {
                return obj.GetHashCode();
            }
        }

        private sealed class Vector3EqualityComparer : IEqualityComparer<Vector3>
        {
            internal static readonly Vector3EqualityComparer Instance = new Vector3EqualityComparer();

            public bool Equals(Vector3 x, Vector3 y)
            {
                return Mathf.Approximately(x.x, y.x)
                    && Mathf.Approximately(x.y, y.y)
                    && Mathf.Approximately(x.z, y.z);
            }

            public int GetHashCode(Vector3 obj)
            {
                return obj.GetHashCode();
            }
        }

        private static Type GetEditorAssemblyType(string fullTypeName)
        {
            Type type = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType(fullTypeName, false))
                .FirstOrDefault(candidate => candidate != null);

            Assert.IsNotNull(type, $"Expected editor type to exist: {fullTypeName}");
            return type;
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