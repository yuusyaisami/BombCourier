using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BC.Rendering
{
    // 各 milestone の validation asset を手作業なしで再生成し、ParticleUnlit の契約を固定する。
    public static class ParticleUnlitValidationBootstrapper
    {
        private const string ParticleShaderRootDirectory = "Assets/Art/Shader/Particles";
        private const string ParticleUnlitDirectory = ParticleShaderRootDirectory + "/ParticleUnlit";
        private const string ParticleUnlitHlslDirectory = ParticleUnlitDirectory + "/HLSL";
        private const string ParticleUnlitPassDirectory = ParticleUnlitHlslDirectory + "/Passes";
        private const string ParticleUnlitEditorDirectory = ParticleUnlitDirectory + "/Editor";
        private const string ParticleLitDirectory = ParticleShaderRootDirectory + "/ParticleLit";
        private const string ParticleLitHlslDirectory = ParticleLitDirectory + "/HLSL";
        private const string ParticleLitPassDirectory = ParticleLitHlslDirectory + "/Passes";
        private const string ParticleLitEditorDirectory = ParticleLitDirectory + "/Editor";
        private const string ParticleDistortionDirectory = ParticleShaderRootDirectory + "/ParticleDistortion";
        private const string ParticleDistortionHlslDirectory = ParticleDistortionDirectory + "/HLSL";
        private const string ParticleDistortionPassDirectory = ParticleDistortionHlslDirectory + "/Passes";
        private const string ParticleDistortionEditorDirectory = ParticleDistortionDirectory + "/Editor";
        private const string ParticleRingDirectory = ParticleShaderRootDirectory + "/ParticleRingUnlit";
        private const string ParticleRingHlslDirectory = ParticleRingDirectory + "/HLSL";
        private const string ParticleRingPassDirectory = ParticleRingHlslDirectory + "/Passes";
        private const string ParticleRingEditorDirectory = ParticleRingDirectory + "/Editor";
        private const string ParticleGroundDirectory = ParticleShaderRootDirectory + "/ParticleGroundUnlit";
        private const string ParticleGroundHlslDirectory = ParticleGroundDirectory + "/HLSL";
        private const string ParticleGroundPassDirectory = ParticleGroundHlslDirectory + "/Passes";
        private const string ParticleGroundEditorDirectory = ParticleGroundDirectory + "/Editor";

        private const string MaterialUnlitDirectory = "Assets/Art/Materials/Particles/Unlit";
        private const string MaterialLitDirectory = "Assets/Art/Materials/Particles/Lit";
        private const string MaterialDistortionDirectory = "Assets/Art/Materials/Particles/Distortion";
        private const string TextureUnlitDirectory = "Assets/Art/Textures/Particles/Unlit";
        private const string TextureLitDirectory = "Assets/Art/Textures/Particles/Lit";
        private const string TextureDistortionDirectory = "Assets/Art/Textures/Particles/Distortion";
        private const string TextureSharedDirectory = "Assets/Art/Textures/Particles/Shared";
        private const string PrefabUnlitDirectory = "Assets/Art/Prefab/Particles/Unlit";
        private const string PrefabLitDirectory = "Assets/Art/Prefab/Particles/Lit";
        private const string PrefabDistortionDirectory = "Assets/Art/Prefab/Particles/Distortion";
        private const string SceneDirectory = "Assets/Scenes/Particles";
        private const string ScenePath = SceneDirectory + "/ParticleMaterialTestScene.unity";
        private const string ShaderPath = ParticleUnlitDirectory + "/BC_Particles_ParticleUnlit.shader";
        private const string ParticleLitShaderPath = ParticleLitDirectory + "/BC_Particles_ParticleLit.shader";
        private const string ParticleDistortionShaderPath = ParticleDistortionDirectory + "/BC_Particles_ParticleDistortion.shader";
        private const string TexturePath = TextureUnlitDirectory + "/T_Particle_TestSoftSprite.png";
        private const string ParticleLitTexturePath = TextureLitDirectory + "/T_Particle_LitSurface.png";
        private const string ParticleLitNormalTexturePath = TextureLitDirectory + "/T_Particle_LitNormal.png";
        private const string ParticleDistortionVectorTexturePath = TextureDistortionDirectory + "/T_Particle_DistortionVector.png";
        private const string ParticleDistortionNoiseTexturePath = TextureDistortionDirectory + "/T_Particle_DistortionNoise.png";
        private const string SmokeFlipbookTexturePath = TextureUnlitDirectory + "/T_Particle_SmokeFlipbook_4x4.png";
        private const string MagicBurstFlipbookTexturePath = TextureUnlitDirectory + "/T_Particle_MagicBurstFlipbook_4x4.png";
        private const string ExplosionPlaceholderFlipbookTexturePath = TextureUnlitDirectory + "/T_Particle_ExplosionPlaceholder_4x4.png";
        private const string SoftCloudNoisePath = TextureSharedDirectory + "/T_Noise_SoftCloud.png";
        private const string DissolveNoisePath = TextureSharedDirectory + "/T_Noise_Dissolve.png";
        private const string ParticleMaskPath = TextureSharedDirectory + "/T_Mask_Particle_Test_RGBA.png";
        private const string MaterialPath = MaterialUnlitDirectory + "/M_Particle_Test_Alpha.mat";
        private const string DustMaterialPath = MaterialUnlitDirectory + "/M_Particle_Dust_Alpha.mat";
        private const string GlowMaterialPath = MaterialUnlitDirectory + "/M_Particle_Glow_Premultiply.mat";
        private const string SparkMaterialPath = MaterialUnlitDirectory + "/M_Particle_Spark_Additive.mat";
        private const string SmokeMaterialPath = MaterialUnlitDirectory + "/M_Particle_Smoke_Alpha.mat";
        private const string MagicMaterialPath = MaterialUnlitDirectory + "/M_Particle_Magic_Additive.mat";
        private const string SmokeFlipbookMaterialPath = MaterialUnlitDirectory + "/M_Particle_SmokeFlipbook_Alpha.mat";
        private const string MagicBurstMaterialPath = MaterialUnlitDirectory + "/M_Particle_MagicBurst_Additive.mat";
        private const string DustDepthValidationMaterialPath = MaterialUnlitDirectory + "/M_Particle_Test_DustSoftParticles_Alpha.mat";
        private const string SmokeDepthValidationMaterialPath = MaterialUnlitDirectory + "/M_Particle_Test_SmokeSoftParticles_Alpha.mat";
        private const string GlowCameraFadeValidationMaterialPath = MaterialUnlitDirectory + "/M_Particle_Test_GlowCameraFade_Premultiply.mat";
        private const string TierLowValidationMaterialPath = MaterialUnlitDirectory + "/M_Particle_Test_TierLow_Alpha.mat";
        private const string TierMediumValidationMaterialPath = MaterialUnlitDirectory + "/M_Particle_Test_TierMedium_Alpha.mat";
        private const string TierHighValidationMaterialPath = MaterialUnlitDirectory + "/M_Particle_Test_TierHigh_Alpha.mat";
        private const string RaindropLitMaterialPath = MaterialLitDirectory + "/M_Particle_Raindrop_Lit.mat";
        private const string BubbleLitMaterialPath = MaterialLitDirectory + "/M_Particle_Bubble_Lit.mat";
        private const string DebrisLitMaterialPath = MaterialLitDirectory + "/M_Particle_Debris_Lit.mat";
        private const string HeatHazeDistortionMaterialPath = MaterialDistortionDirectory + "/M_Particle_HeatHaze_Distortion.mat";
        private const string AirWarpDistortionMaterialPath = MaterialDistortionDirectory + "/M_Particle_AirWarp_Distortion.mat";
        private const string MagicWarpDistortionMaterialPath = MaterialDistortionDirectory + "/M_Particle_MagicWarp_Distortion.mat";
        private const string DustPrefabPath = PrefabUnlitDirectory + "/FX_Particle_Dust.prefab";
        private const string SmokePrefabPath = PrefabUnlitDirectory + "/FX_Particle_Smoke.prefab";
        private const string GlowPrefabPath = PrefabUnlitDirectory + "/FX_Particle_Glow.prefab";
        private const string SparkPrefabPath = PrefabUnlitDirectory + "/FX_Particle_Spark.prefab";
        private const string MagicPrefabPath = PrefabUnlitDirectory + "/FX_Particle_Magic.prefab";
        private const string SmokeFlipbookPrefabPath = PrefabUnlitDirectory + "/FX_Particle_SmokeFlipbook.prefab";
        private const string MagicBurstPrefabPath = PrefabUnlitDirectory + "/FX_Particle_MagicBurst.prefab";
        private const string SparkCustomDataPrefabPath = PrefabUnlitDirectory + "/FX_Particle_SparkCustomData.prefab";
        private const string MagicCustomDataPrefabPath = PrefabUnlitDirectory + "/FX_Particle_MagicCustomData.prefab";
        private const string HeatHazeDistortionPrefabPath = PrefabDistortionDirectory + "/FX_Particle_HeatHaze.prefab";
        private const string AirWarpDistortionPrefabPath = PrefabDistortionDirectory + "/FX_Particle_AirWarp.prefab";
        private const string MagicWarpDistortionPrefabPath = PrefabDistortionDirectory + "/FX_Particle_MagicWarp.prefab";

        private const string ValidationRootName = "ParticleMaterialValidationRoot";
        private const string CameraName = "Main Camera";
        private const string UniversalAdditionalCameraDataTypeName = "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime";
        private const string LightName = "Directional Light";
        private const string FloorName = "ValidationFloor";
        private const string BaseValidationObjectName = "ParticleUnlit_BaseValidation";
        private const string LifetimeValidationObjectName = "ParticleUnlit_LifetimeValidation";
        private const string DustBlendValidationObjectName = "ParticleUnlit_DustAlphaValidation";
        private const string GlowBlendValidationObjectName = "ParticleUnlit_GlowPremultiplyValidation";
        private const string SparkBlendValidationObjectName = "ParticleUnlit_SparkAdditiveValidation";
        private const string SmokeValidationObjectName = "ParticleUnlit_SmokeNoiseValidation";
        private const string MagicValidationObjectName = "ParticleUnlit_MagicDissolveValidation";
        private const string DustDepthValidationObjectName = "ParticleUnlit_DustSoftParticlesValidation";
        private const string SmokeDepthValidationObjectName = "ParticleUnlit_SmokeSoftParticlesValidation";
        private const string GlowCameraFadeValidationObjectName = "ParticleUnlit_GlowCameraFadeValidation";
        private const string DepthInteractionWallObjectName = "ParticleUnlit_DepthInteractionWall";
        private const string RaindropLitValidationObjectName = "ParticleLit_RaindropValidation";
        private const string BubbleLitValidationObjectName = "ParticleLit_BubbleValidation";
        private const string DebrisLitValidationObjectName = "ParticleLit_DebrisValidation";
        private const string ParticleLitLightReferenceObjectName = "ParticleLit_LightReference";
        private const string HeatHazeDistortionValidationObjectName = "ParticleDistortion_HeatHazeValidation";
        private const string AirWarpDistortionValidationObjectName = "ParticleDistortion_AirWarpValidation";
        private const string MagicWarpDistortionValidationObjectName = "ParticleDistortion_MagicWarpValidation";
        private const string ParticleDistortionReferenceWallObjectName = "ParticleDistortion_ReferenceWall";
        private const string QualityTierAreaName = "Quality Tier Test Area";
        private const string M16ReviewHarnessRootName = "ParticleMaterialReviewHarness";
        private const string M16BrightBackdropObjectName = "ParticleM16_BrightBackdrop";
        private const string M16DarkBackdropObjectName = "ParticleM16_DarkBackdrop";
        private const string M16RingPlaceholderObjectName = "ParticleRingUnlit_ShockwavePlaceholder";
        private const string M16GroundPlaceholderObjectName = "ParticleGroundUnlit_GroundSmokePlaceholder";
        private const string TierLowValidationObjectName = "ParticleUnlit_TierLowValidation";
        private const string TierMediumValidationObjectName = "ParticleUnlit_TierMediumValidation";
        private const string TierHighValidationObjectName = "ParticleUnlit_TierHighValidation";
        private const string DustPreviewObjectName = "FX_Particle_Dust_Preview";
        private const string SmokePreviewObjectName = "FX_Particle_Smoke_Preview";
        private const string GlowPreviewObjectName = "FX_Particle_Glow_Preview";
        private const string SparkPreviewObjectName = "FX_Particle_Spark_Preview";
        private const string MagicPreviewObjectName = "FX_Particle_Magic_Preview";
        private const string SmokeFlipbookPreviewObjectName = "FX_Particle_SmokeFlipbook_Preview";
        private const string MagicBurstPreviewObjectName = "FX_Particle_MagicBurst_Preview";
        private const string SparkCustomDataPreviewObjectName = "FX_Particle_SparkCustomData_Preview";
        private const string MagicCustomDataPreviewObjectName = "FX_Particle_MagicCustomData_Preview";
        private const string HeatHazeDistortionPreviewObjectName = "FX_Particle_HeatHaze_Preview";
        private const string AirWarpDistortionPreviewObjectName = "FX_Particle_AirWarp_Preview";
        private const string MagicWarpDistortionPreviewObjectName = "FX_Particle_MagicWarp_Preview";
        private const string WebGlDust100CaseName = "ParticleUnlit_WebGLDust100";
        private const string WebGlDust300CaseName = "ParticleUnlit_WebGLDust300";
        private const string WebGlSmoke50CaseName = "ParticleUnlit_WebGLSmoke50";
        private const string WebGlSpark200CaseName = "ParticleUnlit_WebGLSpark200";
        private const string WebGlGlow100CaseName = "ParticleUnlit_WebGLGlow100";
        private const string WebGlMagic100CaseName = "ParticleUnlit_WebGLMagic100";
        private const string WebGlMixedCaseName = "ParticleUnlit_WebGLMixed";
        private const int FlipbookTilesX = 4;
        private const int FlipbookTilesY = 4;
        private const float FlipbookFramePadding = 0.06f;

        private static readonly string[] SmokeMarkerObjectNames =
        {
            SmokeValidationObjectName,
            SmokeDepthValidationObjectName,
            DepthInteractionWallObjectName,
            SmokePreviewObjectName,
            SmokeFlipbookPreviewObjectName
        };

        private static readonly string[] DustMarkerObjectNames =
        {
            BaseValidationObjectName,
            DustBlendValidationObjectName,
            DustDepthValidationObjectName,
            DustPreviewObjectName
        };

        private static readonly string[] GlowMarkerObjectNames =
        {
            LifetimeValidationObjectName,
            GlowBlendValidationObjectName,
            GlowCameraFadeValidationObjectName,
            GlowPreviewObjectName
        };

        private static readonly string[] MagicMarkerObjectNames =
        {
            MagicValidationObjectName,
            MagicPreviewObjectName,
            MagicBurstPreviewObjectName
        };

        private static readonly string[] SparkCustomDataMarkerObjectNames =
        {
            SparkBlendValidationObjectName,
            SparkPreviewObjectName,
            SparkCustomDataPreviewObjectName
        };

        private static readonly string[] MagicCustomDataMarkerObjectNames =
        {
            MagicValidationObjectName,
            MagicPreviewObjectName,
            MagicBurstPreviewObjectName,
            MagicCustomDataPreviewObjectName
        };

        private static readonly string[] FutureLitMarkerObjectNames =
        {
            RaindropLitValidationObjectName,
            BubbleLitValidationObjectName,
            DebrisLitValidationObjectName,
            ParticleLitLightReferenceObjectName
        };

        private static readonly string[] FutureDistortionMarkerObjectNames =
        {
            HeatHazeDistortionValidationObjectName,
            AirWarpDistortionValidationObjectName,
            MagicWarpDistortionValidationObjectName,
            ParticleDistortionReferenceWallObjectName,
            HeatHazeDistortionPreviewObjectName,
            AirWarpDistortionPreviewObjectName,
            MagicWarpDistortionPreviewObjectName
        };

        // M14 は設計 + scaffold/contract の milestone とし、future family 用の命名と folder depth だけ先に固定する。
        private static readonly string[] FutureRingMarkerObjectNames =
        {
            "Future Ring Test Area",
            "ParticleRingUnlit_ShockwaveValidation",
            "ParticleRingUnlit_MagicCircleValidation",
            "ParticleRingUnlit_WaterRippleValidation"
        };

        private static readonly string[] FutureGroundMarkerObjectNames =
        {
            "Future Ground Test Area",
            "ParticleGroundUnlit_GroundSmokeValidation",
            "ParticleGroundUnlit_FloorMistValidation",
            "ParticleGroundUnlit_GroundAuraValidation"
        };

        private static readonly string[] M14RingPrefabCandidateNames =
        {
            "FX_Particle_ShockwaveRing",
            "FX_Particle_MagicCircle",
            "FX_Particle_WaterRipple",
            "FX_Particle_LandingRing",
            "FX_Particle_ExplosionRing"
        };

        private static readonly string[] M14GroundPrefabCandidateNames =
        {
            "FX_Particle_GroundSmoke",
            "FX_Particle_FloorMist",
            "FX_Particle_DustCloud",
            "FX_Particle_MagicGroundAura",
            "FX_Particle_CreepingFog"
        };

        private static readonly ParticleSystemVertexStream[] DefaultVertexStreams =
        {
            ParticleSystemVertexStream.Position,
            ParticleSystemVertexStream.Color,
            ParticleSystemVertexStream.UV
        };

        private static readonly ParticleSystemVertexStream[] Custom1VertexStreams =
        {
            ParticleSystemVertexStream.Position,
            ParticleSystemVertexStream.Color,
            ParticleSystemVertexStream.UV,
            ParticleSystemVertexStream.Custom1XYZW
        };

        private static readonly string[] MarkerNames =
        {
            "Dust Test Area",
            "Smoke Test Area",
            "Glow Test Area",
            "Spark Test Area",
            "Magic Test Area",
            "WebGL Load Test Area",
            "Future Lit Test Area",
            "Future Distortion Test Area"
        };

        private static readonly string[] WebGlLoadTestObjectNames =
        {
            WebGlDust100CaseName,
            WebGlDust300CaseName,
            WebGlSmoke50CaseName,
            WebGlSpark200CaseName,
            WebGlGlow100CaseName,
            WebGlMagic100CaseName,
            WebGlMixedCaseName
        };

        private static readonly string[] GeneratedMaterialPaths =
        {
            MaterialPath,
            DustMaterialPath,
            SmokeMaterialPath,
            GlowMaterialPath,
            SparkMaterialPath,
            MagicMaterialPath,
            SmokeFlipbookMaterialPath,
            MagicBurstMaterialPath,
            DustDepthValidationMaterialPath,
            SmokeDepthValidationMaterialPath,
            GlowCameraFadeValidationMaterialPath,
            TierLowValidationMaterialPath,
            TierMediumValidationMaterialPath,
            TierHighValidationMaterialPath,
            RaindropLitMaterialPath,
            BubbleLitMaterialPath,
            DebrisLitMaterialPath,
            HeatHazeDistortionMaterialPath,
            AirWarpDistortionMaterialPath,
            MagicWarpDistortionMaterialPath
        };

        private static readonly string[] QualityTierMarkerObjectNames =
        {
            TierLowValidationObjectName,
            TierMediumValidationObjectName,
            TierHighValidationObjectName
        };

        private static readonly string[] M16ReviewHarnessObjectNames =
        {
            M16BrightBackdropObjectName,
            M16DarkBackdropObjectName,
            M16RingPlaceholderObjectName,
            M16GroundPlaceholderObjectName
        };

        [MenuItem("Tools/BC/Particles/ParticleUnlit/Bootstrap M1 Scaffold")]
        public static void BootstrapM1Scaffold()
        {
            EnsureDirectories();
            AssetDatabase.Refresh();

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                EnsureValidationScene();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                RestoreOriginalSceneSetup(originalSetup);
            }

            Debug.Log("ParticleUnlit M1 scaffold bootstrapped successfully.");
        }

        [MenuItem("Tools/BC/Particles/ParticleUnlit/Bootstrap M2 Validation Assets")]
        public static void BootstrapM2ValidationAssets()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                BootstrapM1Scaffold();

                Shader shader = LoadRequiredShader();
                Texture2D baseTexture = EnsureValidationTexture();
                Material material = EnsureValidationMaterial(shader, baseTexture);

                EnsureM2ValidationScene(material);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("ParticleUnlit M2 validation assets bootstrapped successfully.");
            }
            finally
            {
                RestoreOriginalSceneSetup(originalSetup);
            }
        }

        [MenuItem("Tools/BC/Particles/ParticleUnlit/Bootstrap M3 Blend Preset Assets")]
        public static void BootstrapM3BlendPresetAssets()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                BootstrapM2ValidationAssets();

                Shader shader = LoadRequiredShader();
                Texture2D baseTexture = EnsureValidationTexture();
                Material dustMaterial = EnsurePresetValidationMaterial(DustMaterialPath, shader, baseTexture, "Dust");
                Material glowMaterial = EnsurePresetValidationMaterial(GlowMaterialPath, shader, baseTexture, "Glow");
                Material sparkMaterial = EnsurePresetValidationMaterial(SparkMaterialPath, shader, baseTexture, "Spark");

                EnsureM3ValidationScene(dustMaterial, glowMaterial, sparkMaterial);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("ParticleUnlit M3 blend preset assets bootstrapped successfully.");
            }
            finally
            {
                RestoreOriginalSceneSetup(originalSetup);
            }
        }

        [MenuItem("Tools/BC/Particles/ParticleUnlit/Bootstrap M4 Noise Dissolve Assets")]
        public static void BootstrapM4NoiseDissolveAssets()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                BootstrapM3BlendPresetAssets();

                Shader shader = LoadRequiredShader();
                Texture2D baseTexture = EnsureValidationTexture();
                Texture2D softCloudNoise = EnsureSoftCloudNoiseTexture();
                Texture2D dissolveNoise = EnsureDissolveNoiseTexture();
                Texture2D particleMask = EnsureParticleMaskTexture();

                Material smokeMaterial = EnsureM4ValidationMaterial(
                    SmokeMaterialPath,
                    shader,
                    baseTexture,
                    particleMask,
                    softCloudNoise,
                    "Smoke");

                Material magicMaterial = EnsureM4ValidationMaterial(
                    MagicMaterialPath,
                    shader,
                    baseTexture,
                    particleMask,
                    dissolveNoise,
                    "Magic");

                EnsureM4ValidationScene(smokeMaterial, magicMaterial);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("ParticleUnlit M4 noise dissolve assets bootstrapped successfully.");
            }
            finally
            {
                RestoreOriginalSceneSetup(originalSetup);
            }
        }

        [MenuItem("Tools/BC/Particles/ParticleUnlit/Bootstrap M5 Emission Assets")]
        public static void BootstrapM5EmissionAssets()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                BootstrapM4NoiseDissolveAssets();

                Shader shader = LoadRequiredShader();
                Texture2D baseTexture = EnsureValidationTexture();
                Texture2D softCloudNoise = EnsureSoftCloudNoiseTexture();
                Texture2D dissolveNoise = EnsureDissolveNoiseTexture();
                Texture2D particleMask = EnsureParticleMaskTexture();

                Material dustMaterial = EnsurePresetValidationMaterial(DustMaterialPath, shader, baseTexture, "Dust");
                Material smokeMaterial = EnsureM4ValidationMaterial(SmokeMaterialPath, shader, baseTexture, particleMask, softCloudNoise, "Smoke");
                Material glowMaterial = EnsureM5ValidationMaterial(GlowMaterialPath, shader, baseTexture, particleMask, null, "Glow");
                Material sparkMaterial = EnsureM5ValidationMaterial(SparkMaterialPath, shader, baseTexture, particleMask, null, "Spark");
                Material magicMaterial = EnsureM5ValidationMaterial(MagicMaterialPath, shader, baseTexture, particleMask, dissolveNoise, "Magic");

                EnsureM3ValidationScene(dustMaterial, glowMaterial, sparkMaterial);
                EnsureM4ValidationScene(smokeMaterial, magicMaterial);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("ParticleUnlit M5 emission assets bootstrapped successfully.");
            }
            finally
            {
                RestoreOriginalSceneSetup(originalSetup);
            }
        }

        [MenuItem("Tools/BC/Particles/ParticleUnlit/Bootstrap M6 Prefab Assets")]
        public static void BootstrapM6PrefabAssets()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                BootstrapM5EmissionAssets();

                Material dustMaterial = LoadRequiredMaterial(DustMaterialPath);
                Material smokeMaterial = LoadRequiredMaterial(SmokeMaterialPath);
                Material glowMaterial = LoadRequiredMaterial(GlowMaterialPath);
                Material sparkMaterial = LoadRequiredMaterial(SparkMaterialPath);
                Material magicMaterial = LoadRequiredMaterial(MagicMaterialPath);

                EnsureParticlePrefab(DustPrefabPath, dustMaterial, ConfigureDustPrefabParticle);
                EnsureParticlePrefab(SmokePrefabPath, smokeMaterial, ConfigureSmokePrefabParticle);
                EnsureParticlePrefab(GlowPrefabPath, glowMaterial, ConfigureGlowPrefabParticle);
                EnsureParticlePrefab(SparkPrefabPath, sparkMaterial, ConfigureSparkPrefabParticle);
                EnsureParticlePrefab(MagicPrefabPath, magicMaterial, ConfigureMagicPrefabParticle);

                EnsureM6PrefabPreviewScene(
                    LoadRequiredPrefab(DustPrefabPath),
                    LoadRequiredPrefab(SmokePrefabPath),
                    LoadRequiredPrefab(GlowPrefabPath),
                    LoadRequiredPrefab(SparkPrefabPath),
                    LoadRequiredPrefab(MagicPrefabPath));

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("ParticleUnlit M6 prefab assets bootstrapped successfully.");
            }
            finally
            {
                RestoreOriginalSceneSetup(originalSetup);
            }
        }

        [MenuItem("Tools/BC/Particles/ParticleUnlit/Bootstrap M7 WebGL Validation Assets")]
        public static void BootstrapM7WebGlValidationAssets()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                BootstrapM6PrefabAssets();

                EnsureM7WebGlLoadTestScene(
                    LoadRequiredPrefab(DustPrefabPath),
                    LoadRequiredPrefab(SmokePrefabPath),
                    LoadRequiredPrefab(GlowPrefabPath),
                    LoadRequiredPrefab(SparkPrefabPath),
                    LoadRequiredPrefab(MagicPrefabPath));

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("ParticleUnlit M7 WebGL validation assets bootstrapped successfully.");
            }
            finally
            {
                RestoreOriginalSceneSetup(originalSetup);
            }
        }

        [MenuItem("Tools/BC/Particles/ParticleUnlit/Bootstrap M15 Quality Tier Validation Assets")]
        public static void BootstrapM15QualityTierValidationAssets()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                BootstrapM12ParticleLitValidationAssets();

                Shader shader = LoadRequiredShader();
                Texture2D baseTexture = EnsureValidationTexture();
                Texture2D maskTexture = EnsureParticleMaskTexture();
                Texture2D noiseTexture = EnsureSoftCloudNoiseTexture();

                Material tierLowMaterial = EnsureM15QualityTierValidationMaterial(TierLowValidationMaterialPath, shader, baseTexture, maskTexture, noiseTexture, ParticleUnlitQualityTier.Low);
                Material tierMediumMaterial = EnsureM15QualityTierValidationMaterial(TierMediumValidationMaterialPath, shader, baseTexture, maskTexture, noiseTexture, ParticleUnlitQualityTier.Medium);
                Material tierHighMaterial = EnsureM15QualityTierValidationMaterial(TierHighValidationMaterialPath, shader, baseTexture, maskTexture, noiseTexture, ParticleUnlitQualityTier.High);

                EnsureM15QualityTierValidationScene(tierLowMaterial, tierMediumMaterial, tierHighMaterial);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("ParticleUnlit M15 quality tier validation assets bootstrapped successfully.");
            }
            finally
            {
                RestoreOriginalSceneSetup(originalSetup);
            }
        }

        [MenuItem("Tools/BC/Particles/ParticleUnlit/Bootstrap M16 Review Harness")]
        public static void BootstrapM16ReviewHarness()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                BootstrapM13ParticleDistortionValidationAssets();
                BootstrapM15QualityTierValidationAssets();

                EnsureM16ReviewHarnessScene();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("Particle Material System M16 review harness bootstrapped successfully.");
            }
            finally
            {
                RestoreOriginalSceneSetup(originalSetup);
            }
        }

        [MenuItem("Tools/BC/Particles/ParticleUnlit/Bootstrap M8 Debug Validation Assets")]
        public static void BootstrapM8DebugValidationAssets()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                BootstrapM7WebGlValidationAssets();
                ResetGeneratedMaterialDebugModes();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("ParticleUnlit M8 debug validation assets bootstrapped successfully.");
            }
            finally
            {
                RestoreOriginalSceneSetup(originalSetup);
            }
        }

        [MenuItem("Tools/BC/Particles/ParticleUnlit/Bootstrap M9 Flipbook Validation Assets")]
        public static void BootstrapM9FlipbookValidationAssets()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                BootstrapM8DebugValidationAssets();

                Shader shader = LoadRequiredShader();
                Texture2D smokeFlipbookTexture = EnsureSmokeFlipbookTexture();
                Texture2D magicBurstFlipbookTexture = EnsureMagicBurstFlipbookTexture();
                EnsureExplosionPlaceholderFlipbookTexture();
                Texture2D softCloudNoise = EnsureSoftCloudNoiseTexture();
                Texture2D dissolveNoise = EnsureDissolveNoiseTexture();
                Texture2D particleMask = EnsureParticleMaskTexture();

                Material smokeFlipbookMaterial = EnsureM5ValidationMaterial(
                    SmokeFlipbookMaterialPath,
                    shader,
                    smokeFlipbookTexture,
                    particleMask,
                    softCloudNoise,
                    "Smoke");

                Material magicBurstMaterial = EnsureM5ValidationMaterial(
                    MagicBurstMaterialPath,
                    shader,
                    magicBurstFlipbookTexture,
                    particleMask,
                    dissolveNoise,
                    "Magic");

                EnsureParticlePrefab(SmokeFlipbookPrefabPath, smokeFlipbookMaterial, ConfigureSmokeFlipbookPrefabParticle);
                EnsureParticlePrefab(MagicBurstPrefabPath, magicBurstMaterial, ConfigureMagicBurstFlipbookPrefabParticle);
                EnsureM9FlipbookPreviewScene(
                    LoadRequiredPrefab(SmokeFlipbookPrefabPath),
                    LoadRequiredPrefab(MagicBurstPrefabPath));
                ResetGeneratedMaterialDebugModes();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("ParticleUnlit M9 flipbook validation assets bootstrapped successfully.");
            }
            finally
            {
                RestoreOriginalSceneSetup(originalSetup);
            }
        }

        [MenuItem("Tools/BC/Particles/ParticleUnlit/Bootstrap M10 Custom Data Validation Assets")]
        public static void BootstrapM10CustomDataValidationAssets()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                BootstrapM9FlipbookValidationAssets();

                EnsureParticlePrefab(SparkCustomDataPrefabPath, LoadRequiredMaterial(SparkMaterialPath), ConfigureSparkCustomDataPrefabParticle);
                EnsureParticlePrefab(MagicCustomDataPrefabPath, LoadRequiredMaterial(MagicMaterialPath), ConfigureMagicCustomDataPrefabParticle);
                EnsureM10CustomDataPreviewScene(
                    LoadRequiredPrefab(SparkCustomDataPrefabPath),
                    LoadRequiredPrefab(MagicCustomDataPrefabPath));
                ResetGeneratedMaterialDebugModes();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("ParticleUnlit M10 custom data validation assets bootstrapped successfully.");
            }
            finally
            {
                RestoreOriginalSceneSetup(originalSetup);
            }
        }

        [MenuItem("Tools/BC/Particles/ParticleUnlit/Bootstrap M11 Depth Interaction Validation Assets")]
        public static void BootstrapM11DepthInteractionValidationAssets()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                BootstrapM10CustomDataValidationAssets();

                Shader shader = LoadRequiredShader();
                Texture2D baseTexture = EnsureValidationTexture();
                Texture2D softCloudNoise = EnsureSoftCloudNoiseTexture();
                Texture2D particleMask = EnsureParticleMaskTexture();

                Material dustDepthMaterial = EnsureM11DepthValidationMaterial(
                    DustDepthValidationMaterialPath,
                    shader,
                    baseTexture,
                    null,
                    null,
                    "Dust",
                    material =>
                    {
                        material.SetFloat("_UseSoftParticles", 1f);
                        material.SetFloat("_SoftParticleDistance", 0.55f);
                    });

                Material smokeDepthMaterial = EnsureM11DepthValidationMaterial(
                    SmokeDepthValidationMaterialPath,
                    shader,
                    baseTexture,
                    particleMask,
                    softCloudNoise,
                    "Smoke",
                    material =>
                    {
                        material.SetFloat("_UseSoftParticles", 1f);
                        material.SetFloat("_SoftParticleDistance", 1.1f);
                    });

                Material glowCameraFadeMaterial = EnsureM11DepthValidationMaterial(
                    GlowCameraFadeValidationMaterialPath,
                    shader,
                    baseTexture,
                    particleMask,
                    null,
                    "Glow",
                    material =>
                    {
                        material.SetFloat("_UseCameraFade", 1f);
                        material.SetFloat("_CameraFadeNear", 1.5f);
                        material.SetFloat("_CameraFadeFar", 4.5f);
                    });

                EnsureM11DepthInteractionScene(dustDepthMaterial, smokeDepthMaterial, glowCameraFadeMaterial);
                ResetGeneratedMaterialDebugModes();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("ParticleUnlit M11 depth interaction validation assets bootstrapped successfully.");
            }
            finally
            {
                RestoreOriginalSceneSetup(originalSetup);
            }
        }

        [MenuItem("Tools/BC/Particles/ParticleUnlit/Bootstrap M12 ParticleLit Validation Assets")]
        public static void BootstrapM12ParticleLitValidationAssets()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                BootstrapM11DepthInteractionValidationAssets();

                Shader particleLitShader = LoadRequiredParticleLitShader();
                Texture2D baseTexture = EnsureParticleLitBaseTexture();
                Texture2D normalTexture = EnsureParticleLitNormalTexture();

                Material raindropMaterial = EnsureParticleLitValidationMaterial(RaindropLitMaterialPath, particleLitShader, baseTexture, normalTexture, "Raindrop");
                Material bubbleMaterial = EnsureParticleLitValidationMaterial(BubbleLitMaterialPath, particleLitShader, baseTexture, normalTexture, "Bubble");
                Material debrisMaterial = EnsureParticleLitValidationMaterial(DebrisLitMaterialPath, particleLitShader, baseTexture, normalTexture, "Debris");

                EnsureM12ParticleLitValidationScene(raindropMaterial, bubbleMaterial, debrisMaterial);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("ParticleLit M12 validation assets bootstrapped successfully.");
            }
            finally
            {
                RestoreOriginalSceneSetup(originalSetup);
            }
        }

        [MenuItem("Tools/BC/Particles/ParticleUnlit/Bootstrap M13 ParticleDistortion Validation Assets")]
        public static void BootstrapM13ParticleDistortionValidationAssets()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                BootstrapM12ParticleLitValidationAssets();

                Shader particleDistortionShader = LoadRequiredParticleDistortionShader();
                Texture2D distortionVectorTexture = EnsureParticleDistortionVectorTexture();
                Texture2D distortionNoiseTexture = EnsureParticleDistortionNoiseTexture();

                Material heatHazeMaterial = EnsureParticleDistortionValidationMaterial(HeatHazeDistortionMaterialPath, particleDistortionShader, distortionVectorTexture, distortionNoiseTexture, "HeatHaze");
                Material airWarpMaterial = EnsureParticleDistortionValidationMaterial(AirWarpDistortionMaterialPath, particleDistortionShader, distortionVectorTexture, distortionNoiseTexture, "AirWarp");
                Material magicWarpMaterial = EnsureParticleDistortionValidationMaterial(MagicWarpDistortionMaterialPath, particleDistortionShader, distortionVectorTexture, distortionNoiseTexture, "MagicWarp");

                EnsureParticlePrefab(HeatHazeDistortionPrefabPath, heatHazeMaterial, ConfigureHeatHazeDistortionPrefabParticle);
                EnsureParticlePrefab(AirWarpDistortionPrefabPath, airWarpMaterial, ConfigureAirWarpDistortionPrefabParticle);
                EnsureParticlePrefab(MagicWarpDistortionPrefabPath, magicWarpMaterial, ConfigureMagicWarpDistortionPrefabParticle);

                EnsureM13ParticleDistortionValidationScene(
                    heatHazeMaterial,
                    airWarpMaterial,
                    magicWarpMaterial,
                    LoadRequiredPrefab(HeatHazeDistortionPrefabPath),
                    LoadRequiredPrefab(AirWarpDistortionPrefabPath),
                    LoadRequiredPrefab(MagicWarpDistortionPrefabPath));

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("ParticleDistortion M13 validation assets bootstrapped successfully.");
            }
            finally
            {
                RestoreOriginalSceneSetup(originalSetup);
            }
        }

        private static void RestoreOriginalSceneSetup(SceneSetup[] originalSetup)
        {
            if (originalSetup != null && originalSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            }
        }

        private static void EnsureDirectories()
        {
            Directory.CreateDirectory(ParticleUnlitDirectory);
            Directory.CreateDirectory(ParticleUnlitHlslDirectory);
            Directory.CreateDirectory(ParticleUnlitPassDirectory);
            Directory.CreateDirectory(ParticleUnlitEditorDirectory);

            Directory.CreateDirectory(ParticleLitDirectory);
            Directory.CreateDirectory(ParticleLitHlslDirectory);
            Directory.CreateDirectory(ParticleLitPassDirectory);
            Directory.CreateDirectory(ParticleLitEditorDirectory);

            Directory.CreateDirectory(ParticleDistortionDirectory);
            Directory.CreateDirectory(ParticleDistortionHlslDirectory);
            Directory.CreateDirectory(ParticleDistortionPassDirectory);
            Directory.CreateDirectory(ParticleDistortionEditorDirectory);

            Directory.CreateDirectory(ParticleRingDirectory);
            Directory.CreateDirectory(ParticleRingHlslDirectory);
            Directory.CreateDirectory(ParticleRingPassDirectory);
            Directory.CreateDirectory(ParticleRingEditorDirectory);

            Directory.CreateDirectory(ParticleGroundDirectory);
            Directory.CreateDirectory(ParticleGroundHlslDirectory);
            Directory.CreateDirectory(ParticleGroundPassDirectory);
            Directory.CreateDirectory(ParticleGroundEditorDirectory);

            Directory.CreateDirectory(MaterialUnlitDirectory);
            Directory.CreateDirectory(MaterialLitDirectory);
            Directory.CreateDirectory(MaterialDistortionDirectory);

            Directory.CreateDirectory(TextureUnlitDirectory);
            Directory.CreateDirectory(TextureLitDirectory);
            Directory.CreateDirectory(TextureDistortionDirectory);
            Directory.CreateDirectory(TextureSharedDirectory);

            Directory.CreateDirectory(PrefabUnlitDirectory);
            Directory.CreateDirectory(PrefabLitDirectory);
            Directory.CreateDirectory(PrefabDistortionDirectory);

            Directory.CreateDirectory(SceneDirectory);
        }

        private static void EnsureValidationScene()
        {
            Scene scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            scene.name = "ParticleMaterialTestScene";

            EnsureCamera();
            EnsureDirectionalLight();
            EnsureFloor();
            EnsureMarkerHierarchy();

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void EnsureM2ValidationScene(Material validationMaterial)
        {
            Scene scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            scene.name = "ParticleMaterialTestScene";

            EnsureCamera();
            EnsureDirectionalLight();
            EnsureFloor();
            EnsureMarkerHierarchy();

            Transform dustMarker = GetRequiredMarkerTransform("Dust Test Area");
            Transform glowMarker = GetRequiredMarkerTransform("Glow Test Area");

            EnsureParticleValidationAnchor(
                dustMarker,
                BaseValidationObjectName,
                validationMaterial,
                ConfigureBaseValidationParticle);

            EnsureParticleValidationAnchor(
                glowMarker,
                LifetimeValidationObjectName,
                validationMaterial,
                ConfigureLifetimeValidationParticle);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void EnsureM3ValidationScene(Material dustMaterial, Material glowMaterial, Material sparkMaterial)
        {
            Scene scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            scene.name = "ParticleMaterialTestScene";

            EnsureCamera();
            EnsureDirectionalLight();
            EnsureFloor();
            EnsureMarkerHierarchy();

            Transform dustMarker = GetRequiredMarkerTransform("Dust Test Area");
            Transform glowMarker = GetRequiredMarkerTransform("Glow Test Area");
            Transform sparkMarker = GetRequiredMarkerTransform("Spark Test Area");

            EnsureParticleValidationAnchor(
                dustMarker,
                DustBlendValidationObjectName,
                dustMaterial,
                ConfigureDustBlendValidationParticle);

            EnsureParticleValidationAnchor(
                glowMarker,
                GlowBlendValidationObjectName,
                glowMaterial,
                ConfigureGlowBlendValidationParticle);

            EnsureParticleValidationAnchor(
                sparkMarker,
                SparkBlendValidationObjectName,
                sparkMaterial,
                ConfigureSparkBlendValidationParticle);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void EnsureM4ValidationScene(Material smokeMaterial, Material magicMaterial)
        {
            Scene scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            scene.name = "ParticleMaterialTestScene";

            EnsureCamera();
            EnsureDirectionalLight();
            EnsureFloor();
            EnsureMarkerHierarchy();

            Transform smokeMarker = GetRequiredMarkerTransform("Smoke Test Area");
            Transform magicMarker = GetRequiredMarkerTransform("Magic Test Area");

            EnsureParticleValidationAnchor(
                smokeMarker,
                SmokeValidationObjectName,
                smokeMaterial,
                ConfigureSmokeValidationParticle);

            EnsureParticleValidationAnchor(
                magicMarker,
                MagicValidationObjectName,
                magicMaterial,
                ConfigureMagicValidationParticle);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void EnsureM6PrefabPreviewScene(GameObject dustPrefab, GameObject smokePrefab, GameObject glowPrefab, GameObject sparkPrefab, GameObject magicPrefab)
        {
            Scene scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            scene.name = "ParticleMaterialTestScene";

            EnsureCamera();
            EnsureDirectionalLight();
            EnsureFloor();
            EnsureMarkerHierarchy();

            EnsurePrefabPreviewInstance(GetRequiredMarkerTransform("Dust Test Area"), DustPreviewObjectName, dustPrefab);
            EnsurePrefabPreviewInstance(GetRequiredMarkerTransform("Smoke Test Area"), SmokePreviewObjectName, smokePrefab);
            EnsurePrefabPreviewInstance(GetRequiredMarkerTransform("Glow Test Area"), GlowPreviewObjectName, glowPrefab);
            EnsurePrefabPreviewInstance(GetRequiredMarkerTransform("Spark Test Area"), SparkPreviewObjectName, sparkPrefab);
            EnsurePrefabPreviewInstance(GetRequiredMarkerTransform("Magic Test Area"), MagicPreviewObjectName, magicPrefab);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void EnsureM7WebGlLoadTestScene(GameObject dustPrefab, GameObject smokePrefab, GameObject glowPrefab, GameObject sparkPrefab, GameObject magicPrefab)
        {
            Scene scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            scene.name = "ParticleMaterialTestScene";

            EnsureCamera();
            EnsureDirectionalLight();
            EnsureFloor();
            EnsureMarkerHierarchy();

            // M7 は既存 scene を増やさず、専用 marker 配下だけを idempotent に再構成する。
            Transform webGlMarker = GetRequiredMarkerTransform("WebGL Load Test Area");
            RemoveUnexpectedChildren(webGlMarker, WebGlLoadTestObjectNames);
            EnsureWebGlLoadTestCase(webGlMarker, WebGlDust100CaseName, dustPrefab, new Vector3(-9f, 1.2f, 0f), particleCount: 100);
            EnsureWebGlLoadTestCase(webGlMarker, WebGlDust300CaseName, dustPrefab, new Vector3(-3f, 1.2f, 0f), particleCount: 300);
            EnsureWebGlLoadTestCase(webGlMarker, WebGlSmoke50CaseName, smokePrefab, new Vector3(3f, 1.2f, 0f), particleCount: 50);
            EnsureWebGlLoadTestCase(webGlMarker, WebGlSpark200CaseName, sparkPrefab, new Vector3(9f, 1.2f, 0f), particleCount: 200);
            EnsureWebGlLoadTestCase(webGlMarker, WebGlGlow100CaseName, glowPrefab, new Vector3(-6f, 1.2f, 5.5f), particleCount: 100);
            EnsureWebGlLoadTestCase(webGlMarker, WebGlMagic100CaseName, magicPrefab, new Vector3(0f, 1.2f, 5.5f), particleCount: 100);
            EnsureWebGlMixedLoadTestCase(webGlMarker, WebGlMixedCaseName, dustPrefab, smokePrefab, glowPrefab, sparkPrefab, magicPrefab, new Vector3(6f, 1.2f, 5.5f));

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static Shader LoadRequiredShader()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
            {
                throw new FileNotFoundException("ParticleUnlit shader asset was not found.", ShaderPath);
            }

            return shader;
        }

        private static Shader LoadRequiredParticleLitShader()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ParticleLitShaderPath);
            if (shader == null)
            {
                throw new FileNotFoundException("ParticleLit shader asset was not found.", ParticleLitShaderPath);
            }

            return shader;
        }

        private static Shader LoadRequiredParticleDistortionShader()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ParticleDistortionShaderPath);
            if (shader == null)
            {
                throw new FileNotFoundException("ParticleDistortion shader asset was not found.", ParticleDistortionShaderPath);
            }

            return shader;
        }

        private static Material LoadRequiredMaterial(string materialPath)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                throw new FileNotFoundException("ParticleUnlit material asset was not found.", materialPath);
            }

            return material;
        }

        private static GameObject LoadRequiredPrefab(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new FileNotFoundException("ParticleUnlit prefab asset was not found.", prefabPath);
            }

            return prefab;
        }

        private static Texture2D EnsureValidationTexture()
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (texture == null)
            {
                Texture2D generatedTexture = BuildValidationTexture();
                File.WriteAllBytes(TexturePath, generatedTexture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(generatedTexture);
                AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceUpdate);
            }

            ConfigureValidationTextureImporter(TexturePath);
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);

            if (texture == null)
            {
                throw new IOException("ParticleUnlit validation texture could not be loaded after import: " + TexturePath);
            }

            return texture;
        }

        private static Texture2D EnsureParticleLitBaseTexture()
        {
            return EnsureGeneratedTextureAsset(ParticleLitTexturePath, 128, 128, true, TextureWrapMode.Clamp, (u, v) =>
            {
                float stripe = Mathf.Lerp(0.82f, 1.0f, Mathf.Abs(Mathf.Sin((u + v) * Mathf.PI * 3f)));
                float fresnelHint = Mathf.Lerp(0.7f, 1.0f, Mathf.Pow(1f - Mathf.Abs(v * 2f - 1f), 1.5f));
                Color baseColor = Color.Lerp(new Color(0.52f, 0.62f, 0.70f, 1f), new Color(0.94f, 0.97f, 1f, 1f), fresnelHint);
                baseColor *= stripe;
                baseColor.a = Mathf.Lerp(0.35f, 1f, fresnelHint);
                return baseColor;
            });
        }

        private static Texture2D EnsureParticleLitNormalTexture()
        {
            return EnsureGeneratedNormalTextureAsset(ParticleLitNormalTexturePath, 128, 128, (u, v) =>
            {
                float waveX = Mathf.Sin(u * Mathf.PI * 4f) * 0.18f;
                float waveY = Mathf.Cos(v * Mathf.PI * 5f) * 0.18f;
                Vector3 normal = new Vector3(waveX, waveY, 1f).normalized;
                return new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1f);
            });
        }

        private static Texture2D EnsureParticleDistortionVectorTexture()
        {
            return EnsureGeneratedTextureAsset(ParticleDistortionVectorTexturePath, 128, 128, false, TextureWrapMode.Repeat, (u, v) =>
            {
                float waveX = Mathf.Sin(u * Mathf.PI * 2f) * 0.34f + Mathf.Sin(v * Mathf.PI * 6f) * 0.12f;
                float waveY = Mathf.Cos(v * Mathf.PI * 2.5f) * 0.34f + Mathf.Cos(u * Mathf.PI * 5f) * 0.12f;
                Vector2 encodedOffset = Vector2.ClampMagnitude(new Vector2(waveX, waveY), 1f);
                Vector2 centeredUV = new Vector2(u * 2f - 1f, v * 2f - 1f);
                float alpha = Mathf.Pow(1f - Mathf.Clamp01(centeredUV.magnitude), 1.35f);
                return new Color(encodedOffset.x * 0.5f + 0.5f, encodedOffset.y * 0.5f + 0.5f, 0.5f, alpha);
            });
        }

        private static Texture2D EnsureParticleDistortionNoiseTexture()
        {
            return EnsureGeneratedTextureAsset(ParticleDistortionNoiseTexturePath, 128, 128, false, TextureWrapMode.Repeat, (u, v) =>
            {
                float lowFrequency = Mathf.PerlinNoise(u * 4f + 9f, v * 4f + 13f);
                float highFrequency = Mathf.PerlinNoise(u * 15f + 3f, v * 15f + 7f);
                float value = Mathf.Lerp(lowFrequency, highFrequency, 0.4f);
                return new Color(value, value, value, 1f);
            });
        }

        private static Texture2D EnsureSoftCloudNoiseTexture()
        {
            return EnsureGeneratedTextureAsset(SoftCloudNoisePath, 128, 128, false, TextureWrapMode.Repeat, (u, v) =>
            {
                float lowFrequency = Mathf.PerlinNoise(u * 4f, v * 4f);
                float highFrequency = Mathf.PerlinNoise(u * 13f + 4f, v * 13f + 9f);
                float value = Mathf.Lerp(lowFrequency, highFrequency, 0.35f);
                return new Color(value, value, value, 1f);
            });
        }

        private static Texture2D EnsureDissolveNoiseTexture()
        {
            return EnsureGeneratedTextureAsset(DissolveNoisePath, 128, 128, false, TextureWrapMode.Repeat, (u, v) =>
            {
                float cloud = Mathf.PerlinNoise(u * 7f + 11f, v * 7f + 17f);
                float cell = Mathf.PerlinNoise(u * 24f, v * 24f);
                float value = Mathf.Clamp01(Mathf.Lerp(cloud, cell, 0.45f));
                return new Color(value, value, value, 1f);
            });
        }

        private static Texture2D EnsureParticleMaskTexture()
        {
            return EnsureGeneratedTextureAsset(ParticleMaskPath, 128, 128, false, TextureWrapMode.Clamp, (u, v) =>
            {
                Vector2 centeredUV = new Vector2(u * 2f - 1f, v * 2f - 1f);
                float radial = 1f - Mathf.Clamp01(centeredUV.magnitude);
                float dissolve = Mathf.Clamp01(Mathf.PerlinNoise(u * 8f + 2f, v * 8f + 3f));
                float emission = Mathf.Lerp(0.2f, 1f, Mathf.Clamp01(v));
                float variation = Mathf.Clamp01(Mathf.PerlinNoise(u * 14f + 5f, v * 4f + 1f));
                float shape = Mathf.Pow(radial, 1.4f);
                return new Color(dissolve, emission, variation, shape);
            });
        }

        private static Texture2D EnsureSmokeFlipbookTexture()
        {
            return EnsureGeneratedFlipbookAtlas(SmokeFlipbookTexturePath, FlipbookTilesX, FlipbookTilesY, true, BuildSmokeFlipbookFrameColor);
        }

        private static Texture2D EnsureMagicBurstFlipbookTexture()
        {
            return EnsureGeneratedFlipbookAtlas(MagicBurstFlipbookTexturePath, FlipbookTilesX, FlipbookTilesY, true, BuildMagicBurstFlipbookFrameColor);
        }

        private static Texture2D EnsureExplosionPlaceholderFlipbookTexture()
        {
            return EnsureGeneratedFlipbookAtlas(ExplosionPlaceholderFlipbookTexturePath, FlipbookTilesX, FlipbookTilesY, true, BuildExplosionPlaceholderFrameColor);
        }

        private static Material EnsureValidationMaterial(Shader shader, Texture2D baseTexture)
        {
            return EnsureMaterialAsset(MaterialPath, shader, material =>
            {
                material.SetFloat("_BlendMode", (float)ParticleUnlitBlendMode.Alpha);
                material.SetFloat("_Cull", 0f);
                material.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
                material.SetTexture("_BaseMap", baseTexture);
                material.SetColor("_BaseColor", new Color(1f, 0.92f, 0.84f, 1f));
                material.SetFloat("_Alpha", 1f);
                material.SetFloat("_Brightness", 1f);
                material.SetFloat("_UseVertexColor", 1f);
                material.SetFloat("_DebugMode", 0f);
                material.SetFloat("_SoftCircleStrength", 1f);
                material.SetFloat("_EdgeFadePower", 1.6f);
                material.SetFloat("_EdgeFadeStrength", 1f);
                material.SetFloat("_QueueOffset", 0f);
            });
        }

        private static Material EnsurePresetValidationMaterial(string materialPath, Shader shader, Texture2D baseTexture, string presetName)
        {
            return EnsureMaterialAsset(materialPath, shader, material =>
            {
                material.SetTexture("_BaseMap", baseTexture);
                ParticleUnlitPresetUtility.ApplyPreset(material, presetName);
            });
        }

        private static Material EnsureM4ValidationMaterial(string materialPath, Shader shader, Texture2D baseTexture, Texture2D maskTexture, Texture2D noiseTexture, string presetName)
        {
            return EnsureMaterialAsset(materialPath, shader, material =>
            {
                material.SetTexture("_BaseMap", baseTexture);
                material.SetTexture("_MaskMap", maskTexture);
                material.SetTexture("_NoiseMap", noiseTexture);
                ParticleUnlitPresetUtility.ApplyPreset(material, presetName);
            });
        }

        private static Material EnsureM5ValidationMaterial(string materialPath, Shader shader, Texture2D baseTexture, Texture2D maskTexture, Texture2D noiseTexture, string presetName)
        {
            return EnsureMaterialAsset(materialPath, shader, material =>
            {
                material.SetTexture("_BaseMap", baseTexture);
                material.SetTexture("_MaskMap", maskTexture);
                if (noiseTexture != null)
                {
                    material.SetTexture("_NoiseMap", noiseTexture);
                }
                else if (material.HasProperty("_NoiseMap"))
                {
                    material.SetTexture("_NoiseMap", null);
                }

                ParticleUnlitPresetUtility.ApplyPreset(material, presetName);
            });
        }

        private static Material EnsureM11DepthValidationMaterial(string materialPath, Shader shader, Texture2D baseTexture, Texture2D maskTexture, Texture2D noiseTexture, string presetName, Action<Material> configureDepth)
        {
            return EnsureMaterialAsset(materialPath, shader, material =>
            {
                material.SetTexture("_BaseMap", baseTexture);

                if (maskTexture != null && material.HasProperty("_MaskMap"))
                {
                    material.SetTexture("_MaskMap", maskTexture);
                }

                if (noiseTexture != null && material.HasProperty("_NoiseMap"))
                {
                    material.SetTexture("_NoiseMap", noiseTexture);
                }

                ParticleUnlitPresetUtility.ApplyPreset(material, presetName);
                configureDepth?.Invoke(material);
            });
        }

        private static Material EnsureM15QualityTierValidationMaterial(string materialPath, Shader shader, Texture2D baseTexture, Texture2D maskTexture, Texture2D noiseTexture, ParticleUnlitQualityTier tier)
        {
            return EnsureMaterialAsset(materialPath, shader, material =>
            {
                material.SetTexture("_BaseMap", baseTexture);
                material.SetTexture("_MaskMap", maskTexture);
                material.SetTexture("_NoiseMap", noiseTexture);
                material.SetColor("_BaseColor", new Color(0.92f, 0.88f, 0.82f, 0.82f));
                material.SetFloat("_Alpha", 0.82f);
                material.SetFloat("_Brightness", 1f);
                material.SetFloat("_UseVertexColor", 1f);
                material.SetFloat("_BlendMode", (float)ParticleUnlitBlendMode.Alpha);
                material.SetFloat("_QueueOffset", 0f);
                material.SetFloat("_SoftCircleStrength", 1f);
                material.SetFloat("_EdgeFadePower", 1.6f);
                material.SetFloat("_EdgeFadeStrength", 1f);
                ParticleUnlitQualityTierUtility.ApplyTier(material, (int)tier);
            });
        }

        private static Material EnsureParticleLitValidationMaterial(string materialPath, Shader shader, Texture2D baseTexture, Texture2D normalTexture, string presetName)
        {
            return EnsureParticleLitMaterialAsset(materialPath, shader, material =>
            {
                material.SetTexture("_BaseMap", baseTexture);
                material.SetTexture("_NormalMap", normalTexture);
                ParticleLitPresetUtility.ApplyPreset(material, GetParticleLitPresetIndex(presetName));
            });
        }

        private static Material EnsureParticleDistortionValidationMaterial(string materialPath, Shader shader, Texture2D distortionTexture, Texture2D noiseTexture, string presetName)
        {
            return EnsureParticleDistortionMaterialAsset(materialPath, shader, material =>
            {
                material.SetTexture("_DistortionMap", distortionTexture);
                material.SetTexture("_NoiseMap", noiseTexture);
                ParticleDistortionPresetUtility.ApplyPreset(material, GetParticleDistortionPresetIndex(presetName));
            });
        }

        private static void ConfigureValidationTextureImporter(string texturePath)
        {
            ConfigureTextureImporter(texturePath, true, TextureWrapMode.Clamp);
        }

        private static Material EnsureMaterialAsset(string materialPath, Shader shader, System.Action<Material> configure)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = Path.GetFileNameWithoutExtension(materialPath)
                };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            configure(material);
            ParticleUnlitMaterialValidator.Normalize(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureParticleLitMaterialAsset(string materialPath, Shader shader, Action<Material> configure)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = Path.GetFileNameWithoutExtension(materialPath)
                };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            configure(material);
            ParticleLitMaterialValidator.Normalize(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureParticleDistortionMaterialAsset(string materialPath, Shader shader, Action<Material> configure)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = Path.GetFileNameWithoutExtension(materialPath)
                };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            configure(material);
            ParticleDistortionMaterialValidator.Normalize(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D BuildValidationTexture()
        {
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = Path.GetFileNameWithoutExtension(TexturePath)
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1);
                    float v = y / (float)(size - 1);
                    Vector2 centeredUV = new Vector2(u * 2f - 1f, v * 2f - 1f);
                    float radial = 1f - Mathf.Clamp01(centeredUV.magnitude);
                    float alpha = Mathf.Pow(radial, 1.35f);
                    Color color = Color.Lerp(new Color(1f, 0.58f, 0.22f, 1f), Color.white, Mathf.Clamp01(v));
                    color.a = alpha;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return texture;
        }

        private static Texture2D EnsureGeneratedTextureAsset(string texturePath, int width, int height, bool useSrgb, TextureWrapMode wrapMode, Func<float, float, Color> colorProvider)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false, !useSrgb)
            {
                name = Path.GetFileNameWithoutExtension(texturePath)
            };

            for (int pixelY = 0; pixelY < height; pixelY++)
            {
                float normalizedY = height <= 1 ? 0f : pixelY / (float)(height - 1);
                for (int pixelX = 0; pixelX < width; pixelX++)
                {
                    float normalizedX = width <= 1 ? 0f : pixelX / (float)(width - 1);
                    texture.SetPixel(pixelX, pixelY, colorProvider(normalizedX, normalizedY));
                }
            }

            texture.Apply(false, false);
            File.WriteAllBytes(texturePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
            ConfigureTextureImporter(texturePath, useSrgb, wrapMode);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        }

        private static Texture2D EnsureGeneratedNormalTextureAsset(string texturePath, int width, int height, Func<float, float, Color> colorProvider)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            {
                name = Path.GetFileNameWithoutExtension(texturePath)
            };

            for (int pixelY = 0; pixelY < height; pixelY++)
            {
                float normalizedY = height <= 1 ? 0f : pixelY / (float)(height - 1);
                for (int pixelX = 0; pixelX < width; pixelX++)
                {
                    float normalizedX = width <= 1 ? 0f : pixelX / (float)(width - 1);
                    texture.SetPixel(pixelX, pixelY, colorProvider(normalizedX, normalizedY));
                }
            }

            texture.Apply(false, false);
            File.WriteAllBytes(texturePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
            ConfigureNormalTextureImporter(texturePath);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        }

        private static Texture2D EnsureGeneratedFlipbookAtlas(string texturePath, int tilesX, int tilesY, bool useSrgb, Func<int, Vector2, Color> colorProvider)
        {
            const int tileResolution = 64;

            // M9 は shader 側で frame を切り替えず、Unity の Texture Sheet Animation module に渡す atlas を固定する。
            return EnsureGeneratedTextureAsset(texturePath, tilesX * tileResolution, tilesY * tileResolution, useSrgb, TextureWrapMode.Clamp, (u, v) =>
            {
                int tileX = Mathf.Min(Mathf.FloorToInt(u * tilesX), tilesX - 1);
                int tileY = Mathf.Min(Mathf.FloorToInt(v * tilesY), tilesY - 1);
                float localU = Mathf.Clamp01(u * tilesX - tileX);
                float localV = Mathf.Clamp01(v * tilesY - tileY);
                float paddedU = Mathf.Lerp(FlipbookFramePadding, 1f - FlipbookFramePadding, localU);
                float paddedV = Mathf.Lerp(FlipbookFramePadding, 1f - FlipbookFramePadding, localV);
                int frameIndex = tileY * tilesX + tileX;
                return colorProvider(frameIndex, new Vector2(paddedU, paddedV));
            });
        }

        private static Color BuildSmokeFlipbookFrameColor(int frameIndex, Vector2 localUv)
        {
            float phase = frameIndex / (float)(FlipbookTilesX * FlipbookTilesY - 1);
            Vector2 centeredUv = localUv * 2f - Vector2.one;
            float distance = centeredUv.magnitude;
            float plumeRadius = Mathf.Lerp(0.34f, 0.98f, phase);
            float plume = Mathf.Pow(Mathf.Clamp01(1f - distance / plumeRadius), Mathf.Lerp(2.8f, 1.15f, phase));
            float lowFrequency = Mathf.PerlinNoise(localUv.x * 3.5f + phase * 1.9f, localUv.y * 3.5f + phase * 1.3f);
            float wisps = Mathf.PerlinNoise(localUv.x * 10f + frameIndex * 0.31f, localUv.y * 8f + frameIndex * 0.19f);
            float alpha = plume * Mathf.Lerp(0.72f, 1.0f, lowFrequency) * Mathf.Lerp(0.8f, 1.0f, wisps);

            Color color = Color.Lerp(
                new Color(0.68f, 0.70f, 0.69f, 0f),
                new Color(0.40f, 0.42f, 0.44f, 0f),
                phase);
            color.a = Mathf.Clamp01(alpha * 0.92f);
            return color;
        }

        private static Color BuildMagicBurstFlipbookFrameColor(int frameIndex, Vector2 localUv)
        {
            float phase = frameIndex / (float)(FlipbookTilesX * FlipbookTilesY - 1);
            Vector2 centeredUv = localUv * 2f - Vector2.one;
            float distance = centeredUv.magnitude;
            float angle = Mathf.Atan2(centeredUv.y, centeredUv.x);
            float coreRadius = Mathf.Lerp(0.16f, 0.92f, phase);
            float core = Mathf.Pow(Mathf.Clamp01(1f - distance / coreRadius), Mathf.Lerp(7.5f, 1.3f, phase));
            float ringPosition = Mathf.Lerp(0.14f, 0.82f, phase);
            float ring = Mathf.Clamp01(1f - Mathf.Abs(distance - ringPosition) / 0.14f);
            float streaks = Mathf.Pow(Mathf.Clamp01(Mathf.Cos(angle * 6f + phase * 8f) * 0.5f + 0.5f), 2.5f);
            float sparkNoise = Mathf.PerlinNoise(localUv.x * 12f + frameIndex * 0.41f, localUv.y * 12f + frameIndex * 0.23f);
            float alpha = Mathf.Max(core, ring * streaks) * Mathf.Lerp(0.75f, 1.0f, sparkNoise);

            Color color = Color.Lerp(
                new Color(1.0f, 0.56f, 0.96f, 0f),
                new Color(0.24f, 0.72f, 1.0f, 0f),
                Mathf.Clamp01(phase * 0.85f));
            color.a = Mathf.Clamp01(alpha * 0.95f);
            return color;
        }

        private static Color BuildExplosionPlaceholderFrameColor(int frameIndex, Vector2 localUv)
        {
            float phase = frameIndex / (float)(FlipbookTilesX * FlipbookTilesY - 1);
            Vector2 centeredUv = localUv * 2f - Vector2.one;
            float distance = centeredUv.magnitude;
            float shockwave = Mathf.Clamp01(1f - Mathf.Abs(distance - Mathf.Lerp(0.05f, 0.85f, phase)) / 0.18f);
            float core = Mathf.Pow(Mathf.Clamp01(1f - distance / Mathf.Lerp(0.28f, 0.88f, phase)), Mathf.Lerp(4.5f, 1.2f, phase));
            float noise = Mathf.PerlinNoise(localUv.x * 9f + phase * 3f, localUv.y * 9f + phase * 2f);
            float alpha = Mathf.Max(core, shockwave) * Mathf.Lerp(0.7f, 1.0f, noise);

            Color color = Color.Lerp(
                new Color(1.0f, 0.88f, 0.52f, 0f),
                new Color(0.95f, 0.34f, 0.08f, 0f),
                Mathf.Clamp01(phase * 0.9f));
            color.a = Mathf.Clamp01(alpha * 0.98f);
            return color;
        }

        private static void ConfigureTextureImporter(string texturePath, bool useSrgb, TextureWrapMode wrapMode)
        {
            TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (textureImporter == null)
            {
                return;
            }

            textureImporter.textureType = TextureImporterType.Default;
            textureImporter.sRGBTexture = useSrgb;
            textureImporter.alphaSource = TextureImporterAlphaSource.FromInput;
            textureImporter.wrapMode = wrapMode;
            textureImporter.filterMode = FilterMode.Bilinear;
            textureImporter.mipmapEnabled = false;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            textureImporter.SaveAndReimport();
        }

        private static void ConfigureNormalTextureImporter(string texturePath)
        {
            TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (textureImporter == null)
            {
                return;
            }

            textureImporter.textureType = TextureImporterType.NormalMap;
            textureImporter.convertToNormalmap = false;
            textureImporter.alphaSource = TextureImporterAlphaSource.FromInput;
            textureImporter.wrapMode = TextureWrapMode.Repeat;
            textureImporter.filterMode = FilterMode.Bilinear;
            textureImporter.mipmapEnabled = false;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            textureImporter.SaveAndReimport();
        }

        private static void EnsureCamera()
        {
            GameObject cameraObject = GameObject.Find(CameraName);
            UnityEngine.Camera cameraComponent;

            if (cameraObject == null)
            {
                cameraObject = new GameObject(CameraName);
                cameraComponent = cameraObject.AddComponent<UnityEngine.Camera>();
            }
            else
            {
                cameraComponent = cameraObject.GetComponent<UnityEngine.Camera>() ?? cameraObject.AddComponent<UnityEngine.Camera>();
            }

            cameraObject.tag = "MainCamera";
            cameraComponent.clearFlags = CameraClearFlags.Skybox;
            cameraComponent.transform.position = new Vector3(0f, 4.5f, -12f);
            cameraComponent.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
        }

        private static void EnsureDirectionalLight()
        {
            GameObject lightObject = GameObject.Find(LightName);
            Light lightComponent;

            if (lightObject == null)
            {
                lightObject = new GameObject(LightName);
                lightComponent = lightObject.AddComponent<Light>();
            }
            else
            {
                lightComponent = lightObject.GetComponent<Light>() ?? lightObject.AddComponent<Light>();
            }

            lightComponent.type = LightType.Directional;
            lightComponent.intensity = 1.0f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void EnsureFloor()
        {
            GameObject floorObject = GameObject.Find(FloorName);
            if (floorObject == null)
            {
                floorObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floorObject.name = FloorName;
            }

            floorObject.transform.position = Vector3.zero;
            floorObject.transform.rotation = Quaternion.identity;
            floorObject.transform.localScale = new Vector3(4f, 1f, 4f);
        }

        private static void EnsureMarkerHierarchy()
        {
            GameObject root = GameObject.Find(ValidationRootName);
            if (root == null)
            {
                root = new GameObject(ValidationRootName);
            }

            root.transform.position = Vector3.zero;

            for (int markerIndex = 0; markerIndex < MarkerNames.Length; markerIndex++)
            {
                string markerName = MarkerNames[markerIndex];
                Transform markerTransform = root.transform.Find(markerName);
                if (markerTransform == null)
                {
                    GameObject markerObject = new GameObject(markerName);
                    markerTransform = markerObject.transform;
                    markerTransform.SetParent(root.transform, false);
                }

                markerTransform.localPosition = new Vector3((markerIndex % 4) * 6f - 9f, 0f, (markerIndex / 4) * 8f + 2f);
                markerTransform.localRotation = Quaternion.identity;
                markerTransform.localScale = Vector3.one;
            }

            // M15 quality-tier validation area は M1 root marker contract を壊さないよう、scene root の standalone object として管理する。
            Transform legacyQualityTierMarker = root.transform.Find(QualityTierAreaName);
            if (legacyQualityTierMarker != null)
            {
                UnityEngine.Object.DestroyImmediate(legacyQualityTierMarker.gameObject);
            }
        }

        private static Transform GetRequiredMarkerTransform(string markerName)
        {
            GameObject root = GameObject.Find(ValidationRootName);
            if (root == null)
            {
                throw new MissingReferenceException("ParticleMaterialValidationRoot was not found in the validation scene.");
            }

            Transform markerTransform = root.transform.Find(markerName);
            if (markerTransform == null)
            {
                throw new MissingReferenceException("Required validation marker was not found: " + markerName);
            }

            return markerTransform;
        }

        private static Transform EnsureQualityTierValidationArea()
        {
            GameObject qualityTierArea = GameObject.Find(QualityTierAreaName);
            if (qualityTierArea == null)
            {
                qualityTierArea = new GameObject(QualityTierAreaName);
            }

            Transform areaTransform = qualityTierArea.transform;
            areaTransform.SetParent(null, true);
            areaTransform.position = new Vector3(9f, 0f, 18f);
            areaTransform.rotation = Quaternion.identity;
            areaTransform.localScale = Vector3.one;
            return areaTransform;
        }

        private static Transform EnsureM16ReviewHarnessRoot()
        {
            GameObject reviewHarnessRoot = GameObject.Find(M16ReviewHarnessRootName);
            if (reviewHarnessRoot == null)
            {
                reviewHarnessRoot = new GameObject(M16ReviewHarnessRootName);
            }

            Transform rootTransform = reviewHarnessRoot.transform;
            rootTransform.SetParent(null, true);
            rootTransform.position = new Vector3(0f, 0f, 28f);
            rootTransform.rotation = Quaternion.identity;
            rootTransform.localScale = Vector3.one;
            return rootTransform;
        }

        private static void RemoveUnexpectedChildren(Transform parent, string[] expectedChildNames)
        {
            for (int childIndex = parent.childCount - 1; childIndex >= 0; childIndex--)
            {
                Transform child = parent.GetChild(childIndex);
                if (Array.IndexOf(expectedChildNames, child.name) >= 0)
                {
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void ResetGeneratedMaterialDebugModes()
        {
            foreach (string materialPath in GeneratedMaterialPaths)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    continue;
                }

                if (!material.HasProperty("_DebugMode"))
                {
                    continue;
                }

                // M8 の generated assets は debug default-off を authoring baseline として固定する。
                material.SetFloat("_DebugMode", 0f);
                ParticleUnlitMaterialValidator.Normalize(material);
                EditorUtility.SetDirty(material);
            }
        }

        private static void EnsureParticleValidationAnchor(Transform parent, string objectName, Material validationMaterial, System.Action<ParticleSystem> configure)
        {
            EnsureParticleValidationAnchor(parent, objectName, validationMaterial, configure, new Vector3(0f, 1.2f, 0f));
        }

        private static void EnsureParticleValidationAnchor(Transform parent, string objectName, Material validationMaterial, System.Action<ParticleSystem> configure, Vector3 localPosition)
        {
            EnsureParticleValidationAnchor(parent, objectName, validationMaterial, (particleSystem, renderer) => configure(particleSystem), localPosition);
        }

        private static void EnsureParticleValidationAnchor(Transform parent, string objectName, Material validationMaterial, Action<ParticleSystem, ParticleSystemRenderer> configure, Vector3 localPosition)
        {
            Transform anchorTransform = parent.Find(objectName);
            if (anchorTransform == null)
            {
                GameObject anchorObject = new GameObject(objectName);
                anchorTransform = anchorObject.transform;
                anchorTransform.SetParent(parent, false);
            }

            anchorTransform.localPosition = localPosition;
            anchorTransform.localRotation = Quaternion.identity;
            anchorTransform.localScale = Vector3.one;

            ParticleSystem particleSystem = anchorTransform.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                particleSystem = anchorTransform.gameObject.AddComponent<ParticleSystem>();
            }

            ParticleSystemRenderer renderer = anchorTransform.GetComponent<ParticleSystemRenderer>();
            if (renderer == null)
            {
                renderer = anchorTransform.gameObject.AddComponent<ParticleSystemRenderer>();
            }

            ResetValidationParticle(particleSystem);
            ResetValidationRenderer(renderer, validationMaterial);
            configure(particleSystem, renderer);
            renderer.sharedMaterial = validationMaterial;
            EditorUtility.SetDirty(anchorTransform.gameObject);
        }

        private static void EnsureDepthInteractionWall(Transform parent)
        {
            Transform wallTransform = parent.Find(DepthInteractionWallObjectName);
            GameObject wallObject;
            if (wallTransform == null)
            {
                wallObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wallObject.name = DepthInteractionWallObjectName;
                wallTransform = wallObject.transform;
                wallTransform.SetParent(parent, false);
            }
            else
            {
                wallObject = wallTransform.gameObject;
            }

            wallTransform.localPosition = new Vector3(-2.4f, 1.4f, 0.65f);
            wallTransform.localRotation = Quaternion.identity;
            wallTransform.localScale = new Vector3(0.25f, 2.4f, 2.2f);
        }

        private static void EnsureReviewPlaceholder(Transform parent, string objectName, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, Vector3 localEulerAngles)
        {
            Transform placeholderTransform = parent.Find(objectName);
            GameObject placeholderObject;
            if (placeholderTransform == null)
            {
                placeholderObject = GameObject.CreatePrimitive(primitiveType);
                placeholderObject.name = objectName;
                placeholderTransform = placeholderObject.transform;
                placeholderTransform.SetParent(parent, false);
            }
            else
            {
                placeholderObject = placeholderTransform.gameObject;
            }

            placeholderTransform.localPosition = localPosition;
            placeholderTransform.localRotation = Quaternion.Euler(localEulerAngles);
            placeholderTransform.localScale = localScale;
            EditorUtility.SetDirty(placeholderObject);
        }

        private static void EnsurePrefabPreviewInstance(Transform parent, string objectName, GameObject prefabAsset)
        {
            EnsurePrefabPreviewInstance(parent, objectName, prefabAsset, new Vector3(0f, 3.1f, 0f));
        }

        private static void EnsurePrefabPreviewInstance(Transform parent, string objectName, GameObject prefabAsset, Vector3 localPosition)
        {
            Transform anchorTransform = parent.Find(objectName);
            if (anchorTransform != null)
            {
                UnityEngine.Object.DestroyImmediate(anchorTransform.gameObject);
            }

            // M6 の preview は prefab 正本から再生成し、scene 側の drift を asset link ごと防ぐ。
            GameObject previewObject = PrefabUtility.InstantiatePrefab(prefabAsset, parent.gameObject.scene) as GameObject;
            if (previewObject == null)
            {
                throw new IOException("Failed to instantiate ParticleUnlit prefab preview: " + prefabAsset.name);
            }

            Transform previewTransform = previewObject.transform;
            previewObject.name = objectName;
            previewTransform.SetParent(parent, false);
            previewTransform.localPosition = localPosition;
            previewTransform.localRotation = Quaternion.identity;
            previewTransform.localScale = Vector3.one;
            EditorUtility.SetDirty(previewObject);
        }

        private static void EnsureM9FlipbookPreviewScene(GameObject smokeFlipbookPrefab, GameObject magicBurstPrefab)
        {
            Scene scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            scene.name = "ParticleMaterialTestScene";

            EnsureCamera();
            EnsureDirectionalLight();
            EnsureFloor();
            EnsureMarkerHierarchy();

            // M9 は既存 marker を使い続けるので、Smoke/Magic 配下の preview drift を名前集合で掃除しておく。
            RemoveUnexpectedChildren(GetRequiredMarkerTransform("Smoke Test Area"), SmokeMarkerObjectNames);
            RemoveUnexpectedChildren(GetRequiredMarkerTransform("Magic Test Area"), MagicMarkerObjectNames);

            EnsurePrefabPreviewInstance(GetRequiredMarkerTransform("Smoke Test Area"), SmokeFlipbookPreviewObjectName, smokeFlipbookPrefab, new Vector3(2.4f, 3.1f, 0f));
            EnsurePrefabPreviewInstance(GetRequiredMarkerTransform("Magic Test Area"), MagicBurstPreviewObjectName, magicBurstPrefab, new Vector3(2.4f, 3.1f, 0f));

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void EnsureM10CustomDataPreviewScene(GameObject sparkCustomDataPrefab, GameObject magicCustomDataPrefab)
        {
            Scene scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            scene.name = "ParticleMaterialTestScene";

            EnsureCamera();
            EnsureDirectionalLight();
            EnsureFloor();
            EnsureMarkerHierarchy();

            // M10 は Spark/Magic marker に custom-data prefab preview を追加し、許可 child 集合で drift を止める。
            RemoveUnexpectedChildren(GetRequiredMarkerTransform("Spark Test Area"), SparkCustomDataMarkerObjectNames);
            RemoveUnexpectedChildren(GetRequiredMarkerTransform("Magic Test Area"), MagicCustomDataMarkerObjectNames);

            EnsurePrefabPreviewInstance(GetRequiredMarkerTransform("Spark Test Area"), SparkCustomDataPreviewObjectName, sparkCustomDataPrefab, new Vector3(2.4f, 3.1f, 0f));
            EnsurePrefabPreviewInstance(GetRequiredMarkerTransform("Magic Test Area"), MagicCustomDataPreviewObjectName, magicCustomDataPrefab, new Vector3(-2.4f, 3.1f, 0f));

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void EnsureM11DepthInteractionScene(Material dustDepthMaterial, Material smokeDepthMaterial, Material glowCameraFadeMaterial)
        {
            Scene scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            scene.name = "ParticleMaterialTestScene";

            EnsureCamera();
            EnsureDirectionalLight();
            EnsureFloor();
            EnsureMarkerHierarchy();

            // M11 は既存 marker を流用し、depth validation 用 anchor と wall を許可 child 集合で固定する。
            RemoveUnexpectedChildren(GetRequiredMarkerTransform("Dust Test Area"), DustMarkerObjectNames);
            RemoveUnexpectedChildren(GetRequiredMarkerTransform("Smoke Test Area"), SmokeMarkerObjectNames);
            RemoveUnexpectedChildren(GetRequiredMarkerTransform("Glow Test Area"), GlowMarkerObjectNames);

            Transform dustMarker = GetRequiredMarkerTransform("Dust Test Area");
            Transform smokeMarker = GetRequiredMarkerTransform("Smoke Test Area");
            Transform glowMarker = GetRequiredMarkerTransform("Glow Test Area");

            EnsureDepthInteractionWall(smokeMarker);
            EnsureParticleValidationAnchor(dustMarker, DustDepthValidationObjectName, dustDepthMaterial, ConfigureDustBlendValidationParticle, new Vector3(2.4f, 1.2f, 0f));
            EnsureParticleValidationAnchor(smokeMarker, SmokeDepthValidationObjectName, smokeDepthMaterial, ConfigureSmokeValidationParticle, new Vector3(-2.4f, 1.2f, 0f));
            EnsureParticleValidationAnchor(glowMarker, GlowCameraFadeValidationObjectName, glowCameraFadeMaterial, ConfigureGlowBlendValidationParticle, new Vector3(-2.4f, 3.0f, -10.5f));

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void EnsureM12ParticleLitValidationScene(Material raindropMaterial, Material bubbleMaterial, Material debrisMaterial)
        {
            Scene scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            scene.name = "ParticleMaterialTestScene";

            EnsureCamera();
            EnsureDirectionalLight();
            EnsureFloor();
            EnsureMarkerHierarchy();

            Transform futureLitMarker = GetRequiredMarkerTransform("Future Lit Test Area");
            RemoveUnexpectedChildren(futureLitMarker, FutureLitMarkerObjectNames);

            EnsureParticleLitLightReference(futureLitMarker);
            EnsureParticleValidationAnchor(futureLitMarker, RaindropLitValidationObjectName, raindropMaterial, ConfigureRaindropLitValidationParticle, new Vector3(-2.6f, 1.4f, 0f));
            EnsureParticleValidationAnchor(futureLitMarker, BubbleLitValidationObjectName, bubbleMaterial, ConfigureBubbleLitValidationParticle, new Vector3(0f, 1.8f, 0f));
            EnsureParticleValidationAnchor(futureLitMarker, DebrisLitValidationObjectName, debrisMaterial, ConfigureDebrisLitValidationParticle, new Vector3(2.6f, 1.2f, 0f));

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void EnsureM15QualityTierValidationScene(Material tierLowMaterial, Material tierMediumMaterial, Material tierHighMaterial)
        {
            Scene scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            scene.name = "ParticleMaterialTestScene";

            EnsureCamera();
            EnsureDirectionalLight();
            EnsureFloor();
            EnsureMarkerHierarchy();

            Transform qualityTierMarker = EnsureQualityTierValidationArea();
            RemoveUnexpectedChildren(qualityTierMarker, QualityTierMarkerObjectNames);

            EnsureParticleValidationAnchor(qualityTierMarker, TierLowValidationObjectName, tierLowMaterial, ConfigureDustBlendValidationParticle, new Vector3(-2.6f, 1.15f, 0f));
            EnsureParticleValidationAnchor(qualityTierMarker, TierMediumValidationObjectName, tierMediumMaterial, ConfigureSmokeValidationParticle, new Vector3(0f, 1.35f, 0f));
            EnsureParticleValidationAnchor(qualityTierMarker, TierHighValidationObjectName, tierHighMaterial, ConfigureGlowBlendValidationParticle, new Vector3(2.6f, 1.35f, 0f));

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void EnsureM16ReviewHarnessScene()
        {
            Scene scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            scene.name = "ParticleMaterialTestScene";

            EnsureCamera();
            EnsureDirectionalLight();
            EnsureFloor();
            EnsureMarkerHierarchy();

            Transform reviewHarnessRoot = EnsureM16ReviewHarnessRoot();
            RemoveUnexpectedChildren(reviewHarnessRoot, M16ReviewHarnessObjectNames);

            // M16 は既存 marker contract を崩さず、manual review 用の placeholder/backdrop だけを standalone harness に集約する。
            EnsureReviewPlaceholder(reviewHarnessRoot, M16BrightBackdropObjectName, PrimitiveType.Cube, new Vector3(-3.5f, 1.5f, 3.2f), new Vector3(4.5f, 3f, 0.2f), Vector3.zero);
            EnsureReviewPlaceholder(reviewHarnessRoot, M16DarkBackdropObjectName, PrimitiveType.Cube, new Vector3(3.5f, 1.5f, 3.2f), new Vector3(4.5f, 3f, 0.2f), Vector3.zero);
            EnsureReviewPlaceholder(reviewHarnessRoot, M16RingPlaceholderObjectName, PrimitiveType.Cylinder, new Vector3(-2.4f, 0.2f, 0f), new Vector3(2.2f, 0.02f, 2.2f), Vector3.zero);
            EnsureReviewPlaceholder(reviewHarnessRoot, M16GroundPlaceholderObjectName, PrimitiveType.Cube, new Vector3(2.4f, 0.3f, 0f), new Vector3(3f, 0.12f, 2f), Vector3.zero);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void EnsureM13ParticleDistortionValidationScene(
            Material heatHazeMaterial,
            Material airWarpMaterial,
            Material magicWarpMaterial,
            GameObject heatHazePrefab,
            GameObject airWarpPrefab,
            GameObject magicWarpPrefab)
        {
            Scene scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            scene.name = "ParticleMaterialTestScene";

            EnsureCamera();
            EnsureParticleDistortionCameraRequirements();
            EnsureDirectionalLight();
            EnsureFloor();
            EnsureMarkerHierarchy();

            Transform futureDistortionMarker = GetRequiredMarkerTransform("Future Distortion Test Area");
            RemoveUnexpectedChildren(futureDistortionMarker, FutureDistortionMarkerObjectNames);

            // Distortion は背面の opaque object がないと見えないため、scene 側に基準 backdrop を固定する。
            EnsureParticleDistortionReferenceWall(futureDistortionMarker);
            EnsureParticleValidationAnchor(futureDistortionMarker, HeatHazeDistortionValidationObjectName, heatHazeMaterial, ConfigureHeatHazeDistortionPrefabParticle, new Vector3(-2.8f, 1.35f, 0f));
            EnsureParticleValidationAnchor(futureDistortionMarker, AirWarpDistortionValidationObjectName, airWarpMaterial, ConfigureAirWarpDistortionPrefabParticle, new Vector3(0f, 1.55f, 0f));
            EnsureParticleValidationAnchor(futureDistortionMarker, MagicWarpDistortionValidationObjectName, magicWarpMaterial, ConfigureMagicWarpDistortionPrefabParticle, new Vector3(2.8f, 1.4f, 0f));

            EnsurePrefabPreviewInstance(futureDistortionMarker, HeatHazeDistortionPreviewObjectName, heatHazePrefab, new Vector3(-2.8f, 3.15f, -0.35f));
            EnsurePrefabPreviewInstance(futureDistortionMarker, AirWarpDistortionPreviewObjectName, airWarpPrefab, new Vector3(0f, 3.15f, -0.35f));
            EnsurePrefabPreviewInstance(futureDistortionMarker, MagicWarpDistortionPreviewObjectName, magicWarpPrefab, new Vector3(2.8f, 3.15f, -0.35f));

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void EnsureParticleLitLightReference(Transform parent)
        {
            Transform referenceTransform = parent.Find(ParticleLitLightReferenceObjectName);
            GameObject referenceObject;
            if (referenceTransform == null)
            {
                referenceObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                referenceObject.name = ParticleLitLightReferenceObjectName;
                referenceTransform = referenceObject.transform;
                referenceTransform.SetParent(parent, false);
            }
            else
            {
                referenceObject = referenceTransform.gameObject;
            }

            referenceTransform.localPosition = new Vector3(0f, 0.75f, 2.2f);
            referenceTransform.localRotation = Quaternion.Euler(15f, -20f, 0f);
            referenceTransform.localScale = new Vector3(1.4f, 1.4f, 1.4f);
            EditorUtility.SetDirty(referenceObject);
        }

        private static void EnsureParticleDistortionReferenceWall(Transform parent)
        {
            Transform referenceTransform = parent.Find(ParticleDistortionReferenceWallObjectName);
            GameObject referenceObject;
            if (referenceTransform == null)
            {
                referenceObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                referenceObject.name = ParticleDistortionReferenceWallObjectName;
                referenceTransform = referenceObject.transform;
                referenceTransform.SetParent(parent, false);
            }
            else
            {
                referenceObject = referenceTransform.gameObject;
            }

            referenceTransform.localPosition = new Vector3(0f, 1.45f, 2.45f);
            referenceTransform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            referenceTransform.localScale = new Vector3(7.5f, 3.0f, 0.25f);
            EditorUtility.SetDirty(referenceObject);
        }

        private static void EnsureParticleDistortionCameraRequirements()
        {
            GameObject cameraObject = GameObject.Find(CameraName);
            if (cameraObject == null)
            {
                throw new MissingReferenceException("Main Camera must exist before configuring ParticleDistortion validation camera requirements.");
            }

            Type cameraDataType = Type.GetType(UniversalAdditionalCameraDataTypeName);
            if (cameraDataType == null)
            {
                throw new InvalidOperationException("UniversalAdditionalCameraData could not be resolved. ParticleDistortion validation requires URP camera data.");
            }

            Component cameraData = cameraObject.GetComponent(cameraDataType) ?? cameraObject.AddComponent(cameraDataType);
            if (cameraData == null)
            {
                throw new MissingComponentException("Failed to add UniversalAdditionalCameraData to the ParticleDistortion validation camera.");
            }

            // Distortion validation must force Opaque Texture ON so the scene remains deterministic even if the pipeline default is off.
            SetEnumPropertyIfPresent(cameraData, "renderType", "Base");
            SetEnumPropertyIfPresent(cameraData, "requiresColorOption", "On");
            SetBoolPropertyIfPresent(cameraData, "requiresColorTexture", true);

            MethodInfo setRendererMethod = cameraDataType.GetMethod("SetRenderer", new[] { typeof(int) });
            setRendererMethod?.Invoke(cameraData, new object[] { 0 });

            EditorUtility.SetDirty(cameraData);
        }

        private static void SetEnumPropertyIfPresent(object target, string propertyName, string enumName)
        {
            if (target == null)
            {
                return;
            }

            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite || !property.PropertyType.IsEnum)
            {
                return;
            }

            object enumValue = Enum.Parse(property.PropertyType, enumName);
            property.SetValue(target, enumValue);
        }

        private static void SetBoolPropertyIfPresent(object target, string propertyName, bool value)
        {
            if (target == null)
            {
                return;
            }

            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite || property.PropertyType != typeof(bool))
            {
                return;
            }

            property.SetValue(target, value);
        }

        private static void EnsureWebGlLoadTestCase(Transform parent, string objectName, GameObject prefabAsset, Vector3 localPosition, int particleCount)
        {
            GameObject loadTestObject = InstantiatePrefabChild(parent, objectName, prefabAsset, localPosition);
            ParticleSystem particleSystem = loadTestObject.GetComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = loadTestObject.GetComponent<ParticleSystemRenderer>();
            if (particleSystem == null || renderer == null)
            {
                throw new MissingComponentException("ParticleUnlit WebGL load test case must keep ParticleSystem and ParticleSystemRenderer: " + objectName);
            }

            ApplyWebGlLoadProfile(particleSystem, renderer, particleCount);
            EditorUtility.SetDirty(loadTestObject);
        }

        private static void EnsureWebGlMixedLoadTestCase(Transform parent, string objectName, GameObject dustPrefab, GameObject smokePrefab, GameObject glowPrefab, GameObject sparkPrefab, GameObject magicPrefab, Vector3 localPosition)
        {
            Transform existingRoot = parent.Find(objectName);
            if (existingRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(existingRoot.gameObject);
            }

            GameObject rootObject = new GameObject(objectName);
            Transform rootTransform = rootObject.transform;
            rootTransform.SetParent(parent, false);
            rootTransform.localPosition = localPosition;
            rootTransform.localRotation = Quaternion.identity;
            rootTransform.localScale = Vector3.one;

            // Mixed case は prefab source link を保ったまま複数 family の同時表示を固定する。
            GameObject dustInstance = InstantiatePrefabChild(rootTransform, "Dust", dustPrefab, new Vector3(-2.8f, 0f, 0f));
            GameObject smokeInstance = InstantiatePrefabChild(rootTransform, "Smoke", smokePrefab, new Vector3(-1.4f, 0f, 0f));
            GameObject glowInstance = InstantiatePrefabChild(rootTransform, "Glow", glowPrefab, new Vector3(0f, 0f, 0f));
            GameObject sparkInstance = InstantiatePrefabChild(rootTransform, "Spark", sparkPrefab, new Vector3(1.4f, 0f, 0f));
            GameObject magicInstance = InstantiatePrefabChild(rootTransform, "Magic", magicPrefab, new Vector3(2.8f, 0f, 0f));

            ApplyWebGlLoadProfile(dustInstance.GetComponent<ParticleSystem>(), dustInstance.GetComponent<ParticleSystemRenderer>(), 90);
            ApplyWebGlLoadProfile(smokeInstance.GetComponent<ParticleSystem>(), smokeInstance.GetComponent<ParticleSystemRenderer>(), 40);
            ApplyWebGlLoadProfile(glowInstance.GetComponent<ParticleSystem>(), glowInstance.GetComponent<ParticleSystemRenderer>(), 70);
            ApplyWebGlLoadProfile(sparkInstance.GetComponent<ParticleSystem>(), sparkInstance.GetComponent<ParticleSystemRenderer>(), 120);
            ApplyWebGlLoadProfile(magicInstance.GetComponent<ParticleSystem>(), magicInstance.GetComponent<ParticleSystemRenderer>(), 70);

            EditorUtility.SetDirty(rootObject);
        }

        private static GameObject InstantiatePrefabChild(Transform parent, string objectName, GameObject prefabAsset, Vector3 localPosition)
        {
            Transform existingChild = parent.Find(objectName);
            if (existingChild != null)
            {
                UnityEngine.Object.DestroyImmediate(existingChild.gameObject);
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefabAsset, parent.gameObject.scene) as GameObject;
            if (instance == null)
            {
                throw new IOException("Failed to instantiate ParticleUnlit prefab child: " + prefabAsset.name);
            }

            Transform instanceTransform = instance.transform;
            instance.name = objectName;
            instanceTransform.SetParent(parent, false);
            instanceTransform.localPosition = localPosition;
            instanceTransform.localRotation = Quaternion.identity;
            instanceTransform.localScale = Vector3.one;
            return instance;
        }

        private static void ApplyWebGlLoadProfile(ParticleSystem particleSystem, ParticleSystemRenderer renderer, int particleCount)
        {
            if (particleSystem == null || renderer == null)
            {
                throw new MissingComponentException("ParticleUnlit WebGL load profile requires ParticleSystem and ParticleSystemRenderer.");
            }

            // WebGL 向けの first-pass 検証では live particle 上限を maxParticles で固定する。
            var main = particleSystem.main;
            main.maxParticles = particleCount;

            float lifetime = Mathf.Max(main.startLifetime.constant, 0.01f);
            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = particleCount / lifetime;

            renderer.enableGPUInstancing = false;
        }

        private static void EnsureParticlePrefab(string prefabPath, Material material, Action<ParticleSystem, ParticleSystemRenderer> configure)
        {
            GameObject root = new GameObject(Path.GetFileNameWithoutExtension(prefabPath));

            try
            {
                ParticleSystem particleSystem = root.AddComponent<ParticleSystem>();
                ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();

                // Prefab は validation anchor ではなく作者用正本なので、毎回同じ baseline へ正規化する。
                ResetValidationParticle(particleSystem);
                ResetValidationRenderer(renderer, material);
                configure(particleSystem, renderer);
                renderer.sharedMaterial = material;

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                if (savedPrefab == null)
                {
                    throw new IOException("Failed to save ParticleUnlit prefab: " + prefabPath);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ResetValidationParticle(ParticleSystem particleSystem)
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Clear(true);

            var main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = 1f;
            main.startSpeed = 0f;
            main.startSize = 1f;
            main.startColor = new ParticleSystem.MinMaxGradient(Color.white);
            main.maxParticles = 32;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = particleSystem.emission;
            emission.enabled = false;
            emission.rateOverTime = 0f;

            var shape = particleSystem.shape;
            shape.enabled = false;

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = false;

            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = false;

            var rotationOverLifetime = particleSystem.rotationOverLifetime;
            rotationOverLifetime.enabled = false;

            var velocityOverLifetime = particleSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = false;

            var limitVelocityOverLifetime = particleSystem.limitVelocityOverLifetime;
            limitVelocityOverLifetime.enabled = false;

            var forceOverLifetime = particleSystem.forceOverLifetime;
            forceOverLifetime.enabled = false;

            var noise = particleSystem.noise;
            noise.enabled = false;

            var collision = particleSystem.collision;
            collision.enabled = false;

            var trigger = particleSystem.trigger;
            trigger.enabled = false;

            var textureSheetAnimation = particleSystem.textureSheetAnimation;
            textureSheetAnimation.enabled = false;

            var lights = particleSystem.lights;
            lights.enabled = false;

            var trails = particleSystem.trails;
            trails.enabled = false;

            var externalForces = particleSystem.externalForces;
            externalForces.enabled = false;

            var colorBySpeed = particleSystem.colorBySpeed;
            colorBySpeed.enabled = false;

            var sizeBySpeed = particleSystem.sizeBySpeed;
            sizeBySpeed.enabled = false;

            var rotationBySpeed = particleSystem.rotationBySpeed;
            rotationBySpeed.enabled = false;

            var customData = particleSystem.customData;
            customData.enabled = false;
            customData.SetMode(ParticleSystemCustomData.Custom1, ParticleSystemCustomDataMode.Disabled);
            customData.SetMode(ParticleSystemCustomData.Custom2, ParticleSystemCustomDataMode.Disabled);
        }

        private static void ResetValidationRenderer(ParticleSystemRenderer renderer, Material validationMaterial)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortingFudge = 0f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.sharedMaterial = validationMaterial;
            renderer.enableGPUInstancing = false;
            SetActiveVertexStreams(renderer, DefaultVertexStreams);
        }

        private static int GetParticleLitPresetIndex(string presetName)
        {
            string[] presetNames = ParticleLitPresetUtility.GetPresetNames();
            for (int presetIndex = 0; presetIndex < presetNames.Length; presetIndex++)
            {
                if (string.Equals(presetNames[presetIndex], presetName, StringComparison.Ordinal))
                {
                    return presetIndex;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(presetName), presetName, "Unknown ParticleLit preset name.");
        }

        private static int GetParticleDistortionPresetIndex(string presetName)
        {
            string[] presetNames = ParticleDistortionPresetUtility.GetPresetNames();
            for (int presetIndex = 0; presetIndex < presetNames.Length; presetIndex++)
            {
                if (string.Equals(presetNames[presetIndex], presetName, StringComparison.Ordinal))
                {
                    return presetIndex;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(presetName), presetName, "Unknown ParticleDistortion preset name.");
        }

        private static Mesh GetCubeValidationMesh()
        {
            GameObject tempPrimitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                MeshFilter meshFilter = tempPrimitive.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    throw new MissingReferenceException("Built-in cube mesh could not be resolved for ParticleLit validation.");
                }

                return meshFilter.sharedMesh;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tempPrimitive);
            }
        }

        private static void SetActiveVertexStreams(ParticleSystemRenderer renderer, ParticleSystemVertexStream[] streams)
        {
            // M10 でも default は Position/Color/UV の lean contract を維持し、Custom1 は opt-in prefab に限定する。
            renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>(streams));
        }

        private static void ConfigureCustom1Data(
            ParticleSystem particleSystem,
            ParticleSystem.MinMaxCurve dissolveDelta,
            ParticleSystem.MinMaxCurve emissionDelta,
            ParticleSystem.MinMaxCurve noiseOffset,
            ParticleSystem.MinMaxCurve variantIndex)
        {
            var customData = particleSystem.customData;
            customData.enabled = true;
            customData.SetMode(ParticleSystemCustomData.Custom1, ParticleSystemCustomDataMode.Vector);
            customData.SetVectorComponentCount(ParticleSystemCustomData.Custom1, 4);
            customData.SetVector(ParticleSystemCustomData.Custom1, 0, dissolveDelta);
            customData.SetVector(ParticleSystemCustomData.Custom1, 1, emissionDelta);
            customData.SetVector(ParticleSystemCustomData.Custom1, 2, noiseOffset);
            customData.SetVector(ParticleSystemCustomData.Custom1, 3, variantIndex);
            customData.SetMode(ParticleSystemCustomData.Custom2, ParticleSystemCustomDataMode.Disabled);
        }

        private static void ConfigureDustPrefabParticle(ParticleSystem particleSystem, ParticleSystemRenderer renderer)
        {
            var main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 8f;
            main.startSpeed = 0.08f;
            main.startSize = 0.03f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.80f, 0.73f, 0.61f, 0.55f));
            main.maxParticles = 180;

            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 22f;

            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.15f;

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        private static void ConfigureSmokePrefabParticle(ParticleSystem particleSystem, ParticleSystemRenderer renderer)
        {
            var main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 3.4f;
            main.startSpeed = 0.42f;
            main.startSize = 0.85f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.56f, 0.58f, 0.56f, 0.56f));
            main.maxParticles = 96;

            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 10f;

            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.18f;

            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.separateAxes = false;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.35f),
                new Keyframe(1f, 1.2f)));

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.62f, 0.64f, 0.62f), 0f),
                    new GradientColorKey(new Color(0.44f, 0.45f, 0.46f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.55f, 0f),
                    new GradientAlphaKey(0.35f, 0.45f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        private static void ConfigureGlowPrefabParticle(ParticleSystem particleSystem, ParticleSystemRenderer renderer)
        {
            var main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 2.2f;
            main.startSpeed = 0.12f;
            main.startSize = 0.24f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.62f, 0.88f, 1f, 0.72f));
            main.maxParticles = 72;

            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 7f;

            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.08f;

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.62f, 0.90f, 1f), 0f),
                    new GradientColorKey(new Color(0.38f, 0.72f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.72f, 0f),
                    new GradientAlphaKey(0.48f, 0.6f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        private static void ConfigureSparkPrefabParticle(ParticleSystem particleSystem, ParticleSystemRenderer renderer)
        {
            var main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 0.35f;
            main.startSpeed = 6f;
            main.startSize = 0.04f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.84f, 0.40f, 0.85f));
            main.maxParticles = 180;

            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 36f;

            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = 0.03f;
            shape.angle = 10f;

            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.2f;
            renderer.velocityScale = 0.35f;
            renderer.cameraVelocityScale = 0f;
        }

        private static void ConfigureMagicPrefabParticle(ParticleSystem particleSystem, ParticleSystemRenderer renderer)
        {
            var main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 1.6f;
            main.startSpeed = 0.45f;
            main.startSize = 0.18f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.74f, 0.46f, 1f, 0.82f));
            main.maxParticles = 84;

            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 11f;

            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.10f;

            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.separateAxes = false;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.55f),
                new Keyframe(0.45f, 1.0f),
                new Keyframe(1f, 0.15f)));

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.86f, 0.62f, 1f), 0f),
                    new GradientColorKey(new Color(0.48f, 0.30f, 1f), 0.6f),
                    new GradientColorKey(new Color(0.14f, 0.48f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.82f, 0f),
                    new GradientAlphaKey(0.5f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        private static void ConfigureRaindropLitValidationParticle(ParticleSystem particleSystem, ParticleSystemRenderer renderer)
        {
            var main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 1.1f;
            main.startSpeed = 6.8f;
            main.startSize = 0.18f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.82f, 0.92f, 1f, 0.72f));
            main.maxParticles = 90;

            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 24f;

            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.25f, 0.1f, 0.25f);

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        private static void ConfigureBubbleLitValidationParticle(ParticleSystem particleSystem, ParticleSystemRenderer renderer)
        {
            var main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 2.8f;
            main.startSpeed = 0.35f;
            main.startSize = 0.42f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.92f, 0.98f, 1f, 0.55f));
            main.maxParticles = 56;

            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 9f;

            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.12f;

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        private static void ConfigureDebrisLitValidationParticle(ParticleSystem particleSystem, ParticleSystemRenderer renderer)
        {
            var main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 1.9f;
            main.startSpeed = 1.6f;
            main.startSize = 0.22f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.76f, 0.72f, 0.68f, 0.92f));
            main.maxParticles = 48;

            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 8f;

            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = 0.08f;
            shape.angle = 18f;

            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = GetCubeValidationMesh();
        }

        private static void ConfigureHeatHazeDistortionPrefabParticle(ParticleSystem particleSystem, ParticleSystemRenderer renderer)
        {
            var main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 3.2f;
            main.startSpeed = 0.08f;
            main.startSize = 1.65f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.58f));
            main.maxParticles = 72;

            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 14f;

            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.24f;

            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.separateAxes = false;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.42f),
                new Keyframe(0.55f, 1f),
                new Keyframe(1f, 1.18f)));

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.52f, 0f),
                    new GradientAlphaKey(0.32f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        private static void ConfigureAirWarpDistortionPrefabParticle(ParticleSystem particleSystem, ParticleSystemRenderer renderer)
        {
            var main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 1.9f;
            main.startSpeed = 0.45f;
            main.startSize = 1.05f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.7f));
            main.maxParticles = 48;

            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 8f;

            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.1f;

            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.separateAxes = false;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.25f),
                new Keyframe(0.45f, 1.1f),
                new Keyframe(1f, 0.75f)));

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.68f, 0f),
                    new GradientAlphaKey(0.42f, 0.4f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        private static void ConfigureMagicWarpDistortionPrefabParticle(ParticleSystem particleSystem, ParticleSystemRenderer renderer)
        {
            var main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 0.95f;
            main.startSpeed = 0.22f;
            main.startSize = 0.74f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.86f));
            main.maxParticles = 32;

            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 6f;

            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.05f;

            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.separateAxes = false;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.35f),
                new Keyframe(0.35f, 1.15f),
                new Keyframe(1f, 0.32f)));

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.8f, 0f),
                    new GradientAlphaKey(0.5f, 0.35f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        private static void ConfigureSmokeFlipbookPrefabParticle(ParticleSystem particleSystem, ParticleSystemRenderer renderer)
        {
            ConfigureSmokePrefabParticle(particleSystem, renderer);

            var main = particleSystem.main;
            main.startLifetime = 4.1f;
            main.startSpeed = 0.18f;
            main.startSize = 1.05f;
            main.maxParticles = 120;

            var emission = particleSystem.emission;
            emission.rateOverTime = 14f;

            var shape = particleSystem.shape;
            shape.radius = 0.24f;

            ConfigureWholeSheetFlipbookAnimation(
                particleSystem,
                FlipbookTilesX,
                FlipbookTilesY,
                new AnimationCurve(
                    new Keyframe(0f, 0f),
                    new Keyframe(0.55f, 0.62f),
                    new Keyframe(1f, 1f)));

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        private static void ConfigureMagicBurstFlipbookPrefabParticle(ParticleSystem particleSystem, ParticleSystemRenderer renderer)
        {
            ConfigureMagicPrefabParticle(particleSystem, renderer);

            var main = particleSystem.main;
            main.startLifetime = 1.1f;
            main.startSpeed = 0.55f;
            main.startSize = 0.56f;
            main.maxParticles = 72;

            var emission = particleSystem.emission;
            emission.rateOverTime = 8f;

            var shape = particleSystem.shape;
            shape.radius = 0.12f;

            ConfigureWholeSheetFlipbookAnimation(
                particleSystem,
                FlipbookTilesX,
                FlipbookTilesY,
                new AnimationCurve(
                    new Keyframe(0f, 0f),
                    new Keyframe(0.3f, 0.8f),
                    new Keyframe(1f, 1f)));

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        private static void ConfigureSparkCustomDataPrefabParticle(ParticleSystem particleSystem, ParticleSystemRenderer renderer)
        {
            ConfigureSparkPrefabParticle(particleSystem, renderer);

            var main = particleSystem.main;
            main.startLifetime = 0.48f;
            main.startSpeed = 5.2f;
            main.startSize = 0.05f;
            main.maxParticles = 144;

            var emission = particleSystem.emission;
            emission.rateOverTime = 28f;

            ConfigureCustom1Data(
                particleSystem,
                new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                    new Keyframe(0f, 0f),
                    new Keyframe(0.45f, 0.08f),
                    new Keyframe(1f, 0.16f))),
                new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                    new Keyframe(0f, 0.25f),
                    new Keyframe(0.3f, 0.9f),
                    new Keyframe(1f, 0.18f))),
                new ParticleSystem.MinMaxCurve(-0.2f, 0.2f),
                new ParticleSystem.MinMaxCurve(0f));

            SetActiveVertexStreams(renderer, Custom1VertexStreams);
        }

        private static void ConfigureMagicCustomDataPrefabParticle(ParticleSystem particleSystem, ParticleSystemRenderer renderer)
        {
            ConfigureMagicPrefabParticle(particleSystem, renderer);

            var main = particleSystem.main;
            main.startLifetime = 1.45f;
            main.startSpeed = 0.32f;
            main.startSize = 0.24f;
            main.maxParticles = 96;

            var emission = particleSystem.emission;
            emission.rateOverTime = 14f;

            ConfigureCustom1Data(
                particleSystem,
                new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                    new Keyframe(0f, -0.12f),
                    new Keyframe(0.5f, 0.16f),
                    new Keyframe(1f, 0.34f))),
                new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                    new Keyframe(0f, 0.18f),
                    new Keyframe(0.35f, 0.72f),
                    new Keyframe(1f, 0.3f))),
                new ParticleSystem.MinMaxCurve(-0.3f, 0.3f),
                new ParticleSystem.MinMaxCurve(0f));

            SetActiveVertexStreams(renderer, Custom1VertexStreams);
        }

        private static void ConfigureWholeSheetFlipbookAnimation(ParticleSystem particleSystem, int tilesX, int tilesY, AnimationCurve frameCurve)
        {
            var textureSheetAnimation = particleSystem.textureSheetAnimation;

            // M9 は shader property を増やさず、ParticleSystem module だけで atlas 再生契約を固定する。
            textureSheetAnimation.enabled = true;
            textureSheetAnimation.mode = ParticleSystemAnimationMode.Grid;
            textureSheetAnimation.animation = ParticleSystemAnimationType.WholeSheet;
            textureSheetAnimation.numTilesX = tilesX;
            textureSheetAnimation.numTilesY = tilesY;
            textureSheetAnimation.cycleCount = 1;
            textureSheetAnimation.rowMode = ParticleSystemAnimationRowMode.Custom;
            textureSheetAnimation.rowIndex = 0;
            textureSheetAnimation.startFrame = new ParticleSystem.MinMaxCurve(0f, 1f);
            textureSheetAnimation.frameOverTime = new ParticleSystem.MinMaxCurve(1f, frameCurve);
        }

        private static void ConfigureBaseValidationParticle(ParticleSystem particleSystem)
        {
            var main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = 1.6f;
            main.startSpeed = 0.05f;
            main.startSize = 1.2f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.8f, 0.55f, 1f));
            main.maxParticles = 32;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 8f;

            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.12f;

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = false;
        }

        private static void ConfigureLifetimeValidationParticle(ParticleSystem particleSystem)
        {
            ConfigureBaseValidationParticle(particleSystem);

            var main = particleSystem.main;
            main.startLifetime = 2.0f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.55f, 0.85f, 1f, 1f));

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.6f, 0.9f, 1f), 0f),
                    new GradientColorKey(new Color(1f, 0.6f, 0.35f), 0.65f),
                    new GradientColorKey(new Color(0.25f, 0.2f, 0.5f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.65f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        private static void ConfigureDustBlendValidationParticle(ParticleSystem particleSystem)
        {
            ConfigureBaseValidationParticle(particleSystem);

            var main = particleSystem.main;
            main.startLifetime = 1.9f;
            main.startSize = 1.35f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.82f, 0.72f, 0.58f, 0.72f));

            var emission = particleSystem.emission;
            emission.rateOverTime = 10f;
        }

        private static void ConfigureGlowBlendValidationParticle(ParticleSystem particleSystem)
        {
            ConfigureLifetimeValidationParticle(particleSystem);

            var main = particleSystem.main;
            main.startLifetime = 1.4f;
            main.startSize = 1.45f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.6f, 0.88f, 1f, 0.82f));

            var emission = particleSystem.emission;
            emission.rateOverTime = 7f;
        }

        private static void ConfigureSparkBlendValidationParticle(ParticleSystem particleSystem)
        {
            ConfigureBaseValidationParticle(particleSystem);

            var main = particleSystem.main;
            main.startLifetime = 0.75f;
            main.startSpeed = 0.7f;
            main.startSize = 0.32f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.84f, 0.42f, 0.78f));
            main.maxParticles = 48;

            var emission = particleSystem.emission;
            emission.rateOverTime = 18f;

            var shape = particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.04f;
        }

        private static void ConfigureSmokeValidationParticle(ParticleSystem particleSystem)
        {
            ConfigureLifetimeValidationParticle(particleSystem);

            var main = particleSystem.main;
            main.startLifetime = 2.6f;
            main.startSpeed = 0.08f;
            main.startSize = 1.7f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.62f, 0.64f, 0.62f, 0.72f));

            var emission = particleSystem.emission;
            emission.rateOverTime = 6f;
        }

        private static void ConfigureMagicValidationParticle(ParticleSystem particleSystem)
        {
            ConfigureLifetimeValidationParticle(particleSystem);

            var main = particleSystem.main;
            main.startLifetime = 1.2f;
            main.startSpeed = 0.22f;
            main.startSize = 0.95f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.72f, 0.42f, 1f, 0.84f));
            main.maxParticles = 40;

            var emission = particleSystem.emission;
            emission.rateOverTime = 12f;
        }
    }
}