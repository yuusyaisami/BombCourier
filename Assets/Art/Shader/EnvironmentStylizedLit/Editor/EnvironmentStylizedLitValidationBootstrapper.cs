using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BC.Rendering
{
    public static class EnvironmentStylizedLitValidationBootstrapper
    {
        private const string DefaultMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_Default.mat";
        private const string InteriorMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_Interior.mat";
        private const string AmbientRoomMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_AmbientRoom.mat";
        private const string AmbientBounceMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_AmbientBounce.mat";
        private const string SoftSpecularMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_SoftSpecular.mat";
        private const string QuantizedMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_Quantized.mat";
        private const string CeramicMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_Ceramic.mat";
        private const string PlasticMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_Plastic.mat";
        private const string EdgeSheenMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_EdgeSheen.mat";
        private const string WorldNoiseMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_WorldNoise.mat";
        private const string ObjectSpaceNoiseMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_ObjectSpaceNoise.mat";
        private const string BandNoiseMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_BandNoise.mat";
        private const string NoiseOffMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_NoiseOff.mat";
        private const string IndirectBaselineMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_IndirectBaseline.mat";
        private const string ProbeIndirectMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_ProbeIndirect.mat";
        private const string CavityTintMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_CavityTint.mat";
        private const string AdditionalOffMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_AdditionalOff.mat";
        private const string AdditionalFillOnlyMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_AdditionalFillOnly.mat";
        private const string AdditionalQuantizedMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_AdditionalQuantized.mat";
        private const string AdditionalContinuousMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_AdditionalContinuous.mat";
        private const string TriplanarOffMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_TriplanarOff.mat";
        private const string TriplanarFullMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_TriplanarFull.mat";
        private const string VertexColorMaskMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_VertexColorMask.mat";
        private const string WorldYGradientMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_WorldYGradient.mat";
        private const string TierLowMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_TierLow.mat";
        private const string TierMediumMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_TierMedium.mat";
        private const string TierHighMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_TierHigh.mat";
        private const string M14ClayMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_M14_ClayDiorama.mat";
        private const string M14PaintedPlasterMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_M14_PaintedPlaster.mat";
        private const string M14MatteToyPlasticMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_M14_MatteToyPlastic.mat";
        private const string M14CeramicToyMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_M14_CeramicToy.mat";
        private const string M14ChalkPastelMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_M14_ChalkPastel.mat";
        private const string ShaderAssetPath = "Assets/Art/Shader/EnvironmentStylizedLit/EnvironmentStylizedLit.shader";
        private const string TestRoomScenePath = "Assets/Scenes/EnvironmentStylizedLit/ESL_TestRoom.unity";
        private const string LightingLabScenePath = "Assets/Scenes/EnvironmentStylizedLit/ESL_LightingLab.unity";

        public static void BootstrapM5ValidationAssets()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                Material defaultMaterial = LoadRequiredMaterial(DefaultMaterialPath);
                Material interiorMaterial = LoadRequiredMaterial(InteriorMaterialPath);
                Material ambientRoomMaterial = EnsureCopiedMaterialAsset(AmbientRoomMaterialPath, interiorMaterial, ConfigureAmbientRoomMaterial);
                Material ambientBounceMaterial = EnsureCopiedMaterialAsset(AmbientBounceMaterialPath, defaultMaterial, ConfigureAmbientBounceMaterial);

                EnsureTestRoomM5Anchors(ambientRoomMaterial, ambientBounceMaterial);
                EnsureLightingLabM5Anchors(ambientBounceMaterial);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }
        }

        public static void BootstrapM6ValidationAssets()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                BootstrapM5ValidationAssets();

                Material ambientBounceMaterial = LoadRequiredMaterial(AmbientBounceMaterialPath);
                Material softSpecularMaterial = EnsureCopiedMaterialAsset(SoftSpecularMaterialPath, ambientBounceMaterial, ConfigureSoftSpecularMaterial);
                Material quantizedMaterial = EnsureCopiedMaterialAsset(QuantizedMaterialPath, ambientBounceMaterial, ConfigureQuantizedMaterial);
                Material ceramicMaterial = EnsureCopiedMaterialAsset(CeramicMaterialPath, ambientBounceMaterial, ConfigureCeramicMaterial);
                Material plasticMaterial = EnsureCopiedMaterialAsset(PlasticMaterialPath, ambientBounceMaterial, ConfigurePlasticMaterial);
                Material edgeSheenMaterial = EnsureCopiedMaterialAsset(EdgeSheenMaterialPath, ambientBounceMaterial, ConfigureEdgeSheenMaterial);

                EnsureLightingLabM6Anchors(softSpecularMaterial, quantizedMaterial, ceramicMaterial, plasticMaterial);
                EnsureTestRoomM6Anchors(edgeSheenMaterial);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }
        }

        public static void BootstrapM7ValidationAssets()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                BootstrapM6ValidationAssets();

                Material ambientBounceMaterial = LoadRequiredMaterial(AmbientBounceMaterialPath);
                Material worldNoiseMaterial = EnsureCopiedMaterialAsset(WorldNoiseMaterialPath, ambientBounceMaterial, ConfigureWorldNoiseMaterial);
                Material objectSpaceNoiseMaterial = EnsureCopiedMaterialAsset(ObjectSpaceNoiseMaterialPath, ambientBounceMaterial, ConfigureObjectSpaceNoiseMaterial);
                Material bandNoiseMaterial = EnsureCopiedMaterialAsset(BandNoiseMaterialPath, ambientBounceMaterial, ConfigureBandNoiseMaterial);
                Material noiseOffMaterial = EnsureCopiedMaterialAsset(NoiseOffMaterialPath, ambientBounceMaterial, ConfigureNoiseOffMaterial);

                EnsureTestRoomM7Anchors(worldNoiseMaterial, objectSpaceNoiseMaterial, noiseOffMaterial);
                EnsureLightingLabM7Anchors(bandNoiseMaterial, noiseOffMaterial);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }
        }

        public static void BootstrapM8ValidationAssets()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                BootstrapM7ValidationAssets();

                Material defaultMaterial = LoadRequiredMaterial(DefaultMaterialPath);
                Material interiorMaterial = LoadRequiredMaterial(InteriorMaterialPath);

                EnsureTestRoomM8Anchors(defaultMaterial, interiorMaterial);
                EnsureLightingLabM8Anchors(defaultMaterial);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }
        }

        public static void BootstrapM9ValidationAssets()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                BootstrapM8ValidationAssets();

                Material ambientBounceMaterial = LoadRequiredMaterial(AmbientBounceMaterialPath);
                Material indirectBaselineMaterial = EnsureCopiedMaterialAsset(IndirectBaselineMaterialPath, ambientBounceMaterial, ConfigureIndirectBaselineMaterial);
                Material probeIndirectMaterial = EnsureCopiedMaterialAsset(ProbeIndirectMaterialPath, ambientBounceMaterial, ConfigureProbeIndirectMaterial);
                Material cavityTintMaterial = EnsureCopiedMaterialAsset(CavityTintMaterialPath, ambientBounceMaterial, ConfigureCavityTintMaterial);

                EnsureTestRoomM9Anchors(indirectBaselineMaterial, probeIndirectMaterial, cavityTintMaterial);
                EnsureLightingLabM9Anchors(indirectBaselineMaterial, probeIndirectMaterial, cavityTintMaterial);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }
        }

        public static void BootstrapM10ValidationAssets()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                BootstrapM9ValidationAssets();

                Material ambientBounceMaterial = LoadRequiredMaterial(AmbientBounceMaterialPath);
                Material additionalOffMaterial = EnsureCopiedMaterialAsset(AdditionalOffMaterialPath, ambientBounceMaterial, ConfigureAdditionalOffMaterial);
                Material additionalFillOnlyMaterial = EnsureCopiedMaterialAsset(AdditionalFillOnlyMaterialPath, ambientBounceMaterial, ConfigureAdditionalFillOnlyMaterial);
                Material additionalQuantizedMaterial = EnsureCopiedMaterialAsset(AdditionalQuantizedMaterialPath, ambientBounceMaterial, ConfigureAdditionalQuantizedMaterial);
                Material additionalContinuousMaterial = EnsureCopiedMaterialAsset(AdditionalContinuousMaterialPath, ambientBounceMaterial, ConfigureAdditionalContinuousMaterial);

                EnsureTestRoomM10Anchors(additionalOffMaterial, additionalFillOnlyMaterial, additionalQuantizedMaterial, additionalContinuousMaterial);
                EnsureLightingLabM10Anchors(additionalOffMaterial, additionalFillOnlyMaterial, additionalQuantizedMaterial, additionalContinuousMaterial);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }
        }

        public static void BootstrapM11ValidationAssets()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                BootstrapM10ValidationAssets();

                Material ambientBounceMaterial = LoadRequiredMaterial(AmbientBounceMaterialPath);
                Material triplanarOffMaterial = EnsureCopiedMaterialAsset(TriplanarOffMaterialPath, ambientBounceMaterial, ConfigureTriplanarOffMaterial);
                Material triplanarFullMaterial = EnsureCopiedMaterialAsset(TriplanarFullMaterialPath, ambientBounceMaterial, ConfigureTriplanarFullMaterial);
                Material vertexColorMaskMaterial = EnsureCopiedMaterialAsset(VertexColorMaskMaterialPath, ambientBounceMaterial, ConfigureVertexColorMaskMaterial);
                Material worldYGradientMaterial = EnsureCopiedMaterialAsset(WorldYGradientMaterialPath, ambientBounceMaterial, ConfigureWorldYGradientMaterial);

                EnsureTestRoomM11Anchors(triplanarOffMaterial, triplanarFullMaterial, vertexColorMaskMaterial, worldYGradientMaterial);
                EnsureLightingLabM11Anchors(triplanarOffMaterial, triplanarFullMaterial, vertexColorMaskMaterial, worldYGradientMaterial);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }
        }

        public static void BootstrapM13ValidationAssets()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                BootstrapM11ValidationAssets();

                Material ambientBounceMaterial = LoadRequiredMaterial(AmbientBounceMaterialPath);
                Material tierLowMaterial = EnsureCopiedMaterialAsset(TierLowMaterialPath, ambientBounceMaterial, ConfigureTierLowMaterial);
                Material tierMediumMaterial = EnsureCopiedMaterialAsset(TierMediumMaterialPath, ambientBounceMaterial, ConfigureTierMediumMaterial);
                Material tierHighMaterial = EnsureCopiedMaterialAsset(TierHighMaterialPath, ambientBounceMaterial, ConfigureTierHighMaterial);

                EnsureTestRoomM13Anchors(tierLowMaterial, tierMediumMaterial, tierHighMaterial);
                EnsureLightingLabM13Anchors(tierLowMaterial, tierMediumMaterial, tierHighMaterial);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }
        }

        public static void BootstrapM14ValidationAssets()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                BootstrapM13ValidationAssets();

                Shader shader = LoadRequiredShader(ShaderAssetPath);
                Material clayMaterial = EnsureMaterialAsset(M14ClayMaterialPath, shader, ConfigureClayDioramaMaterial);
                Material paintedPlasterMaterial = EnsureMaterialAsset(M14PaintedPlasterMaterialPath, shader, ConfigurePaintedPlasterMaterial);
                Material matteToyPlasticMaterial = EnsureMaterialAsset(M14MatteToyPlasticMaterialPath, shader, ConfigureMatteToyPlasticMaterial);
                Material ceramicToyMaterial = EnsureMaterialAsset(M14CeramicToyMaterialPath, shader, ConfigureCeramicToyMaterial);
                Material chalkPastelMaterial = EnsureMaterialAsset(M14ChalkPastelMaterialPath, shader, ConfigureChalkPastelMaterial);

                EnsureTestRoomM14Anchors(clayMaterial, paintedPlasterMaterial, matteToyPlasticMaterial, ceramicToyMaterial, chalkPastelMaterial);
                EnsureLightingLabM14Anchors(clayMaterial, paintedPlasterMaterial, matteToyPlasticMaterial, ceramicToyMaterial, chalkPastelMaterial);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }
        }

        public static void NormalizeValidationScenes()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                NormalizeValidationScene(TestRoomScenePath);
                NormalizeValidationScene(LightingLabScenePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }
        }

        private static Material LoadRequiredMaterial(string materialPath)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                throw new FileNotFoundException($"Required validation material is missing: {materialPath}");
            }

            return material;
        }

        private static void NormalizeValidationScene(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static Shader LoadRequiredShader(string shaderPath)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            if (shader == null)
            {
                throw new FileNotFoundException($"Required shader asset is missing: {shaderPath}");
            }

            return shader;
        }

        private static Material EnsureMaterialAsset(string materialPath, Shader shader, System.Action<Material> configureMaterial)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Material configuredMaterial = new Material(shader)
            {
                name = Path.GetFileNameWithoutExtension(materialPath)
            };

            configureMaterial(configuredMaterial);

            if (material == null)
            {
                AssetDatabase.CreateAsset(configuredMaterial, materialPath);
                material = configuredMaterial;
            }
            else
            {
                EditorUtility.CopySerialized(configuredMaterial, material);
                material.name = configuredMaterial.name;
                Object.DestroyImmediate(configuredMaterial);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureCopiedMaterialAsset(string materialPath, Material sourceMaterial, System.Action<Material> configureMaterial)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(sourceMaterial), materialPath))
                {
                    throw new IOException($"Failed to create validation material: {materialPath}");
                }

                material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    throw new IOException($"Created validation material could not be loaded: {materialPath}");
                }

                material.name = Path.GetFileNameWithoutExtension(materialPath);
            }

            configureMaterial(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureAmbientRoomMaterial(Material material)
        {
            material.SetFloat("_Cull", 1f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetFloat("_ShadowInfluence", 1f);
            material.SetFloat("_ShadowSoftFill", 0.3f);
            material.SetFloat("_ShadowColorBlend", 0.75f);
            ApplyAmbientBounceProfile(
                material,
                new Color(0.58f, 0.64f, 0.76f, 1f),
                new Color(0.38f, 0.42f, 0.5f, 1f),
                new Color(0.24f, 0.21f, 0.18f, 1f),
                0.55f,
                new Color(0.92f, 0.78f, 0.62f, 1f),
                0.32f,
                new Vector4(0f, 1f, 0f, 0f),
                0.65f,
                new Color(0.7f, 0.77f, 0.88f, 1f));
        }

        private static void ConfigureAmbientBounceMaterial(Material material)
        {
            material.SetFloat("_Cull", 2f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetFloat("_ShadowInfluence", 1f);
            material.SetFloat("_ShadowSoftFill", 0.2f);
            material.SetFloat("_ShadowColorBlend", 0.6f);
            ApplyAmbientBounceProfile(
                material,
                new Color(0.54f, 0.61f, 0.74f, 1f),
                new Color(0.36f, 0.4f, 0.48f, 1f),
                new Color(0.23f, 0.2f, 0.18f, 1f),
                0.5f,
                new Color(0.94f, 0.8f, 0.64f, 1f),
                0.38f,
                new Vector4(0f, 1f, 0f, 0f),
                0.55f,
                new Color(0.72f, 0.79f, 0.9f, 1f));
        }

        private static void ConfigureSoftSpecularMaterial(Material material)
        {
            ApplySharedSpecularValidationSettings(material);
            ApplySpecularProfile(
                material,
                1f,
                0.18f,
                0.2f,
                3f,
                0.1f,
                new Color(0.72f, 0.68f, 0.62f, 1f),
                0.04f,
                2.2f,
                new Color(0.92f, 0.9f, 0.84f, 1f));
        }

        private static void ConfigureQuantizedMaterial(Material material)
        {
            ApplySharedSpecularValidationSettings(material);
            ApplySpecularProfile(
            material,
            2f,
            0.26f,
            0.52f,
            4f,
            0.06f,
            new Color(0.9f, 0.86f, 0.8f, 1f),
            0.05f,
            2.35f,
            new Color(0.96f, 0.92f, 0.86f, 1f));
        }

        private static void ConfigureCeramicMaterial(Material material)
        {
            ApplySharedSpecularValidationSettings(material);
            ApplySpecularProfile(
                material,
                3f,
                0.36f,
                0.74f,
                3f,
                0.08f,
                new Color(0.96f, 0.94f, 0.9f, 1f),
                0.08f,
                2.8f,
                new Color(0.98f, 0.96f, 0.92f, 1f));
        }

        private static void ConfigurePlasticMaterial(Material material)
        {
            ApplySharedSpecularValidationSettings(material);
            ApplySpecularProfile(
                material,
                4f,
                0.28f,
                0.48f,
                3f,
                0.08f,
                new Color(0.88f, 0.82f, 0.76f, 1f),
                0.12f,
                2.4f,
                new Color(1f, 0.94f, 0.86f, 1f));
        }

        private static void ConfigureEdgeSheenMaterial(Material material)
        {
            ApplySharedSpecularValidationSettings(material);
            ApplySpecularProfile(
                material,
                0f,
                0f,
                0.35f,
                3f,
                0.1f,
                new Color(0.2f, 0.2f, 0.2f, 1f),
                0.3f,
                2.8f,
                new Color(1f, 0.94f, 0.88f, 1f));
        }

        private static void ConfigureWorldNoiseMaterial(Material material)
        {
            ConfigureNoiseDisabledMaterial(material);
            material.SetColor("_BaseColor", new Color(0.83f, 0.81f, 0.76f, 1f));
            ApplyNoiseProfile(material, 0f, 0.28f, 0.38f, 0.42f, 1.35f, 0f, 0.75f, 8f, 24f);
        }

        private static void ConfigureObjectSpaceNoiseMaterial(Material material)
        {
            ConfigureNoiseDisabledMaterial(material);
            material.SetColor("_BaseColor", new Color(0.81f, 0.79f, 0.75f, 1f));
            ApplyNoiseProfile(material, 1f, 0.28f, 0.42f, 0.46f, 1.35f, 0f, 0.75f, 8f, 24f);
        }

        private static void ConfigureBandNoiseMaterial(Material material)
        {
            ConfigureNoiseDisabledMaterial(material);
            material.SetColor("_BaseColor", new Color(0.86f, 0.83f, 0.78f, 1f));
            material.SetFloat("_LightStepCount", 3f);
            material.SetFloat("_LightStepSmoothness", 0.05f);
            material.SetFloat("_WrapLighting", 0.14f);
            material.SetFloat("_BandContrast", 1.1f);
            ApplyNoiseProfile(material, 0f, 0f, 0.38f, 0.28f, 1.2f, 0.2f, 0.9f, 6f, 22f);
        }

        private static void ConfigureNoiseOffMaterial(Material material)
        {
            ConfigureNoiseDisabledMaterial(material);
            material.SetColor("_BaseColor", new Color(0.83f, 0.81f, 0.76f, 1f));
            ApplyNoiseProfile(material, 0f, 0f, 0.38f, 0f, 1.25f, 0f, 0.9f, 6f, 22f);
        }

        private static void ConfigureNoiseDisabledMaterial(Material material)
        {
            material.SetFloat("_Cull", 2f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_EmissionStrength", 0f);
            material.SetFloat("_SpecularMode", 0f);
            material.SetFloat("_SpecularStrength", 0f);
            material.SetFloat("_EdgeSheenStrength", 0f);
            material.SetFloat("_NoiseSpace", 0f);
        }

        private static void ConfigureM9ValidationMaterialBase(Material material)
        {
            ConfigureNoiseDisabledMaterial(material);
            material.SetFloat("_EmissionStrength", 0f);
            material.SetFloat("_NormalScale", 0.55f);
            material.SetFloat("_OcclusionStrength", 0.55f);
            material.SetFloat("_IndirectStrength", 1f);
            material.SetFloat("_IndirectStylizeStrength", 0.35f);
            material.SetFloat("_CavityStrength", 0.35f);
            material.SetColor("_CavityColor", new Color(0.68f, 0.73f, 0.82f, 1f));
        }

        private static void ConfigureIndirectBaselineMaterial(Material material)
        {
            ConfigureM9ValidationMaterialBase(material);
            material.SetColor("_BaseColor", new Color(0.84f, 0.82f, 0.78f, 1f));
            material.SetFloat("_IndirectStrength", 0.55f);
            material.SetFloat("_IndirectStylizeStrength", 0f);
            material.SetFloat("_CavityStrength", 0f);
            material.SetFloat("_OcclusionStrength", 0.25f);
        }

        private static void ConfigureProbeIndirectMaterial(Material material)
        {
            ConfigureM9ValidationMaterialBase(material);
            material.SetColor("_BaseColor", new Color(0.82f, 0.8f, 0.76f, 1f));
            material.SetFloat("_IndirectStrength", 1f);
            material.SetFloat("_IndirectStylizeStrength", 0.65f);
            material.SetFloat("_CavityStrength", 0.25f);
            material.SetFloat("_OcclusionStrength", 0.45f);
        }

        private static void ConfigureCavityTintMaterial(Material material)
        {
            ConfigureM9ValidationMaterialBase(material);
            material.SetColor("_BaseColor", new Color(0.8f, 0.8f, 0.82f, 1f));
            material.SetFloat("_IndirectStrength", 1f);
            material.SetFloat("_IndirectStylizeStrength", 0.5f);
            material.SetFloat("_CavityStrength", 0.8f);
            material.SetFloat("_OcclusionStrength", 1f);
            material.SetColor("_CavityColor", new Color(0.42f, 0.5f, 0.62f, 1f));
        }

        private static void ConfigureM10ValidationMaterialBase(Material material)
        {
            ConfigureNoiseDisabledMaterial(material);
            material.SetColor("_BaseColor", new Color(0.84f, 0.82f, 0.78f, 1f));
            material.SetFloat("_NormalScale", 0.55f);
            material.SetFloat("_OcclusionStrength", 0.5f);
            material.SetFloat("_IndirectStrength", 0.7f);
            material.SetFloat("_IndirectStylizeStrength", 0.25f);
            material.SetFloat("_CavityStrength", 0.2f);
            material.SetFloat("_AdditionalLightIntensity", 0.5f);
            material.SetFloat("_AdditionalLightShadowInfluence", 0.65f);
            material.SetFloat("_AdditionalLightColorInfluence", 0.75f);
        }

        private static void ConfigureAdditionalOffMaterial(Material material)
        {
            ConfigureM10ValidationMaterialBase(material);
            material.SetFloat("_AdditionalLightMode", 0f);
            material.SetFloat("_AdditionalLightIntensity", 0.5f);
        }

        private static void ConfigureAdditionalFillOnlyMaterial(Material material)
        {
            ConfigureM10ValidationMaterialBase(material);
            material.SetFloat("_AdditionalLightMode", 1f);
            material.SetFloat("_AdditionalLightIntensity", 0.5f);
            material.SetFloat("_AdditionalLightShadowInfluence", 0.65f);
            material.SetFloat("_AdditionalLightColorInfluence", 0.6f);
        }

        private static void ConfigureAdditionalQuantizedMaterial(Material material)
        {
            ConfigureM10ValidationMaterialBase(material);
            material.SetFloat("_AdditionalLightMode", 2f);
            material.SetFloat("_AdditionalLightIntensity", 0.38f);
            material.SetFloat("_AdditionalLightShadowInfluence", 0.75f);
            material.SetFloat("_AdditionalLightColorInfluence", 0.8f);
            material.SetFloat("_LightStepCount", 3f);
            material.SetFloat("_LightStepSmoothness", 0.06f);
        }

        private static void ConfigureAdditionalContinuousMaterial(Material material)
        {
            ConfigureM10ValidationMaterialBase(material);
            material.SetFloat("_AdditionalLightMode", 3f);
            material.SetFloat("_AdditionalLightIntensity", 0.45f);
            material.SetFloat("_AdditionalLightShadowInfluence", 0.5f);
            material.SetFloat("_AdditionalLightColorInfluence", 0.85f);
        }

        private static void ConfigureM11ValidationMaterialBase(Material material)
        {
            ConfigureNoiseDisabledMaterial(material);
            material.SetColor("_BaseColor", new Color(0.84f, 0.82f, 0.78f, 1f));
            material.SetFloat("_NormalScale", 0.8f);
            material.SetFloat("_OcclusionStrength", 0.45f);
            material.SetFloat("_IndirectStrength", 0.7f);
            material.SetFloat("_LightStepCount", 3f);
            material.SetFloat("_LightStepSmoothness", 0.08f);
            material.SetFloat("_WrapLighting", 0.14f);
            material.SetFloat("_BandContrast", 1.08f);
            material.SetFloat("_BandOffset", 0f);
            SetTriplanarKeywords(material, false, false, false);
            material.SetFloat("_TriplanarScale", 1.2f);
            material.SetFloat("_TriplanarBlendSharpness", 4f);
            material.SetFloat("_VertexColorEnabled", 0f);
            material.SetFloat("_VertexColorCavityStrength", 0.8f);
            material.SetFloat("_VertexColorBandOffsetStrength", 0.2f);
            material.SetFloat("_VertexColorColorVariationStrength", 0.75f);
            material.SetFloat("_WorldYGradientEnabled", 0f);
            material.SetColor("_WorldYGradientTopColor", Color.white);
            material.SetColor("_WorldYGradientBottomColor", Color.white);
            material.SetFloat("_WorldYGradientMin", 0f);
            material.SetFloat("_WorldYGradientMax", 3.5f);
            material.SetFloat("_WorldYGradientStrength", 0f);
        }

        private static void ConfigureTriplanarOffMaterial(Material material)
        {
            ConfigureM11ValidationMaterialBase(material);
            material.SetColor("_BaseColor", new Color(0.82f, 0.8f, 0.76f, 1f));
        }

        private static void ConfigureTriplanarFullMaterial(Material material)
        {
            ConfigureM11ValidationMaterialBase(material);
            SetTriplanarKeywords(material, true, true, true);
            material.SetFloat("_TriplanarScale", 1.35f);
            material.SetFloat("_TriplanarBlendSharpness", 5f);
            ApplyNoiseProfile(material, 0f, 0.18f, 0.42f, 0.26f, 1.2f, 0.08f, 0.9f, 8f, 26f);
        }

        private static void ConfigureVertexColorMaskMaterial(Material material)
        {
            ConfigureM11ValidationMaterialBase(material);
            material.SetFloat("_VertexColorEnabled", 1f);
            material.SetFloat("_VertexColorCavityStrength", 0.85f);
            material.SetFloat("_VertexColorBandOffsetStrength", 0.18f);
            material.SetFloat("_VertexColorColorVariationStrength", 0.8f);
            ApplyNoiseProfile(material, 0f, 0.16f, 0.38f, 0.24f, 1.15f, 0f, 0.8f, 8f, 24f);
        }

        private static void ConfigureWorldYGradientMaterial(Material material)
        {
            ConfigureM11ValidationMaterialBase(material);
            material.SetFloat("_WorldYGradientEnabled", 1f);
            material.SetColor("_WorldYGradientTopColor", new Color(1.08f, 1.0f, 0.92f, 1f));
            material.SetColor("_WorldYGradientBottomColor", new Color(0.74f, 0.8f, 0.9f, 1f));
            material.SetFloat("_WorldYGradientMin", 0f);
            material.SetFloat("_WorldYGradientMax", 4.2f);
            material.SetFloat("_WorldYGradientStrength", 0.65f);
        }

        private static void ConfigureM13ValidationMaterialBase(Material material)
        {
            ConfigureM11ValidationMaterialBase(material);
            material.SetColor("_BaseColor", new Color(0.84f, 0.82f, 0.78f, 1f));
            material.SetFloat("_AdditionalLightMode", 0f);
            material.SetFloat("_AdditionalLightIntensity", 0f);
            material.SetFloat("_AdditionalLightShadowInfluence", 0.65f);
            material.SetFloat("_AdditionalLightColorInfluence", 0.75f);
            material.SetFloat("_SpecularMode", 1f);
            material.SetFloat("_SpecularStrength", 0.18f);
            material.SetFloat("_SpecularStepCount", 3f);
            material.SetFloat("_SpecularStepSmoothness", 0.1f);
            material.SetFloat("_EdgeSheenStrength", 0f);
            material.SetFloat("_OcclusionStrength", 0f);
            material.SetFloat("_CavityStrength", 0f);
            material.SetFloat("_VertexColorEnabled", 0f);
            material.SetFloat("_DebugView", 0f);
            ApplyNoiseProfile(material, 0f, 0f, 0.42f, 0f, 1.2f, 0f, 0.9f, 8f, 26f);
            SetTriplanarKeywords(material, false, false, false);
        }

        private static void ConfigureTierLowMaterial(Material material)
        {
            ConfigureM13ValidationMaterialBase(material);
            material.SetColor("_BaseColor", new Color(0.8f, 0.79f, 0.75f, 1f));
            EnvironmentStylizedLitPerformanceTierUtility.ApplyTier(material, (int)EnvironmentStylizedLitPerformanceTier.Low);
            EnvironmentStylizedLitMaterialValidator.Normalize(material);
        }

        private static void ConfigureTierMediumMaterial(Material material)
        {
            ConfigureM13ValidationMaterialBase(material);
            material.SetColor("_BaseColor", new Color(0.82f, 0.8f, 0.76f, 1f));
            EnvironmentStylizedLitPerformanceTierUtility.ApplyTier(material, (int)EnvironmentStylizedLitPerformanceTier.Medium);
            EnvironmentStylizedLitMaterialValidator.Normalize(material);
        }

        private static void ConfigureTierHighMaterial(Material material)
        {
            ConfigureM13ValidationMaterialBase(material);
            material.SetColor("_BaseColor", new Color(0.86f, 0.84f, 0.8f, 1f));
            EnvironmentStylizedLitPerformanceTierUtility.ApplyTier(material, (int)EnvironmentStylizedLitPerformanceTier.High);
            EnvironmentStylizedLitMaterialValidator.Normalize(material);
        }

        private static void ConfigureClayDioramaMaterial(Material material)
        {
            ApplyPresetReviewMaterial(material, "ClayDiorama");
        }

        private static void ConfigurePaintedPlasterMaterial(Material material)
        {
            ApplyPresetReviewMaterial(material, "PaintedPlaster");
        }

        private static void ConfigureMatteToyPlasticMaterial(Material material)
        {
            ApplyPresetReviewMaterial(material, "MatteToyPlastic");
        }

        private static void ConfigureCeramicToyMaterial(Material material)
        {
            ApplyPresetReviewMaterial(material, "CeramicToy");
        }

        private static void ConfigureChalkPastelMaterial(Material material)
        {
            ApplyPresetReviewMaterial(material, "ChalkPastel");
        }

        private static void ApplyPresetReviewMaterial(Material material, string presetName)
        {
            ResetPresetReviewMaterialState(material);
            EnvironmentStylizedLitPresetUtility.ApplyPreset(material, presetName);
            material.SetFloat("_DebugView", 0f);
            EnvironmentStylizedLitMaterialValidator.Normalize(material);
        }

        private static void ResetPresetReviewMaterialState(Material material)
        {
            ResetTextureSlot(material, "_BaseMap");
            ResetTextureSlot(material, "_EmissionMap");
            ResetTextureSlot(material, "_NormalMap");
            ResetTextureSlot(material, "_OcclusionMap");
        }

        private static void ResetTextureSlot(Material material, string propertyName)
        {
            if (!material.HasProperty(propertyName))
            {
                return;
            }

            material.SetTexture(propertyName, null);
            material.SetTextureScale(propertyName, Vector2.one);
            material.SetTextureOffset(propertyName, Vector2.zero);
        }

        private static void SetTriplanarKeywords(Material material, bool baseMapEnabled, bool normalMapEnabled, bool noiseEnabled)
        {
            SetKeywordToggle(material, "_TriplanarBaseMapEnabled", "_ESL_TRIPLANAR_BASEMAP", baseMapEnabled);
            SetKeywordToggle(material, "_TriplanarNormalMapEnabled", "_ESL_TRIPLANAR_NORMALMAP", normalMapEnabled);
            SetKeywordToggle(material, "_TriplanarNoiseEnabled", "_ESL_TRIPLANAR_NOISE", noiseEnabled);
        }

        private static void SetKeywordToggle(Material material, string propertyName, string keywordName, bool enabled)
        {
            material.SetFloat(propertyName, enabled ? 1f : 0f);

            if (enabled)
            {
                material.EnableKeyword(keywordName);
            }
            else
            {
                material.DisableKeyword(keywordName);
            }
        }

        private static void ApplyNoiseProfile(
            Material material,
            float noiseSpace,
            float albedoNoiseStrength,
            float worldNoiseScale,
            float worldNoiseStrength,
            float worldNoiseContrast,
            float lightBandNoiseStrength,
            float lightBandNoiseScale,
            float noiseDistanceFadeStart,
            float noiseDistanceFadeEnd)
        {
            material.SetFloat("_NoiseSpace", noiseSpace);
            material.SetFloat("_AlbedoNoiseStrength", albedoNoiseStrength);
            material.SetFloat("_WorldNoiseScale", worldNoiseScale);
            material.SetFloat("_WorldNoiseStrength", worldNoiseStrength);
            material.SetFloat("_WorldNoiseContrast", worldNoiseContrast);
            material.SetFloat("_LightBandNoiseStrength", lightBandNoiseStrength);
            material.SetFloat("_LightBandNoiseScale", lightBandNoiseScale);
            material.SetFloat("_NoiseDistanceFadeStart", noiseDistanceFadeStart);
            material.SetFloat("_NoiseDistanceFadeEnd", noiseDistanceFadeEnd);
        }

        private static void ApplySharedSpecularValidationSettings(Material material)
        {
            material.SetFloat("_Cull", 2f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_EmissionStrength", 0f);
            material.SetColor("_BaseColor", new Color(0.84f, 0.82f, 0.78f, 1f));
        }

        private static void ApplySpecularProfile(
            Material material,
            float specularMode,
            float specularStrength,
            float smoothness,
            float specularStepCount,
            float specularStepSmoothness,
            Color specularColor,
            float edgeSheenStrength,
            float edgeSheenPower,
            Color edgeSheenColor)
        {
            material.SetFloat("_SpecularMode", specularMode);
            material.SetFloat("_SpecularStrength", specularStrength);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_SpecularStepCount", specularStepCount);
            material.SetFloat("_SpecularStepSmoothness", specularStepSmoothness);
            material.SetColor("_SpecularColor", specularColor);
            material.SetFloat("_EdgeSheenStrength", edgeSheenStrength);
            material.SetFloat("_EdgeSheenPower", edgeSheenPower);
            material.SetColor("_EdgeSheenColor", edgeSheenColor);
        }

        private static void ApplyAmbientBounceProfile(
            Material material,
            Color ambientTopColor,
            Color ambientSideColor,
            Color ambientBottomColor,
            float ambientStrength,
            Color bounceColor,
            float bounceStrength,
            Vector4 bounceDirection,
            float bounceWrap,
            Color indirectShadowColor)
        {
            material.SetColor("_AmbientTopColor", ambientTopColor);
            material.SetColor("_AmbientSideColor", ambientSideColor);
            material.SetColor("_AmbientBottomColor", ambientBottomColor);
            material.SetFloat("_AmbientStrength", ambientStrength);
            material.SetColor("_BounceColor", bounceColor);
            material.SetFloat("_BounceStrength", bounceStrength);
            material.SetVector("_BounceDirection", bounceDirection);
            material.SetFloat("_BounceWrap", bounceWrap);
            material.SetColor("_IndirectShadowColor", indirectShadowColor);
        }

        private static void EnsureTestRoomM5Anchors(Material ambientRoomMaterial, Material ambientBounceMaterial)
        {
            Scene scene = EditorSceneManager.OpenScene(TestRoomScenePath, OpenSceneMode.Single);

            EnsureViewpointMarker("M5AmbientRoomViewpoint", new Vector3(18f, 1.4f, -0.25f));
            EnsureCube("M5AmbientRoom", new Vector3(18f, 1.5f, 0f), new Vector3(6f, 3.2f, 6f), ambientRoomMaterial, ShadowCastingMode.Off);
            EnsureSphere("M5BounceProbeSphere", new Vector3(18f, 0.95f, 0.2f), Vector3.one * 1.2f, ambientBounceMaterial, ShadowCastingMode.On);
            EnsureCube("M5AmbientReferenceCube", new Vector3(15f, 0.85f, 6.6f), Vector3.one * 1.2f, ambientBounceMaterial, ShadowCastingMode.On);

            EditorSceneManager.SaveScene(scene, TestRoomScenePath);
        }

        private static void EnsureLightingLabM5Anchors(Material ambientBounceMaterial)
        {
            Scene scene = EditorSceneManager.OpenScene(LightingLabScenePath, OpenSceneMode.Single);

            EnsureQuad("M5AmbientTopQuad", new Vector3(-4.8f, 1.5f, 4.8f), new Vector3(-90f, 0f, 0f), new Vector3(1.8f, 1.8f, 1f), ambientBounceMaterial, ShadowCastingMode.On);
            EnsureQuad("M5AmbientSideQuad", new Vector3(-1.8f, 1.5f, 4.8f), Vector3.zero, new Vector3(1.8f, 1.8f, 1f), ambientBounceMaterial, ShadowCastingMode.On);
            EnsureQuad("M5AmbientBottomQuad", new Vector3(1.2f, 1.5f, 4.8f), new Vector3(90f, 0f, 0f), new Vector3(1.8f, 1.8f, 1f), ambientBounceMaterial, ShadowCastingMode.On);
            EnsureQuad("M5BounceFacingQuad", new Vector3(4.8f, 1.5f, 4.8f), new Vector3(-90f, 0f, 0f), new Vector3(1.8f, 1.8f, 1f), ambientBounceMaterial, ShadowCastingMode.On);
            EnsureQuad("M5BounceOpposingQuad", new Vector3(7.8f, 1.5f, 4.8f), new Vector3(90f, 0f, 0f), new Vector3(1.8f, 1.8f, 1f), ambientBounceMaterial, ShadowCastingMode.On);

            EditorSceneManager.SaveScene(scene, LightingLabScenePath);
        }

        private static void EnsureLightingLabM6Anchors(Material softSpecularMaterial, Material quantizedMaterial, Material ceramicMaterial, Material plasticMaterial)
        {
            Scene scene = EditorSceneManager.OpenScene(LightingLabScenePath, OpenSceneMode.Single);

            EnsureSphere("M6SoftSpecularSphere", new Vector3(-4.8f, 0.95f, 8.2f), Vector3.one * 1.35f, softSpecularMaterial, ShadowCastingMode.On);
            EnsureSphere("M6QuantizedSpecularSphere", new Vector3(-1.6f, 0.95f, 8.2f), Vector3.one * 1.35f, quantizedMaterial, ShadowCastingMode.On);
            EnsureSphere("M6CeramicSpecularSphere", new Vector3(1.6f, 0.95f, 8.2f), Vector3.one * 1.35f, ceramicMaterial, ShadowCastingMode.On);
            EnsureSphere("M6PlasticSpecularSphere", new Vector3(4.8f, 0.95f, 8.2f), Vector3.one * 1.35f, plasticMaterial, ShadowCastingMode.On);

            EditorSceneManager.SaveScene(scene, LightingLabScenePath);
        }

        private static void EnsureTestRoomM6Anchors(Material edgeSheenMaterial)
        {
            Scene scene = EditorSceneManager.OpenScene(TestRoomScenePath, OpenSceneMode.Single);

            EnsureViewpointMarker("M6EdgeSheenViewpoint", new Vector3(22f, 1.4f, -3.8f));
            EnsureQuad("M6EdgeSheenQuad", new Vector3(22f, 1.4f, 0f), new Vector3(0f, 75f, 0f), new Vector3(2.6f, 2.6f, 1f), edgeSheenMaterial, ShadowCastingMode.On);

            EditorSceneManager.SaveScene(scene, TestRoomScenePath);
        }

        private static void EnsureTestRoomM7Anchors(Material worldNoiseMaterial, Material objectSpaceNoiseMaterial, Material noiseOffMaterial)
        {
            Scene scene = EditorSceneManager.OpenScene(TestRoomScenePath, OpenSceneMode.Single);

            EnsureViewpointMarker("M7NoiseNearViewpoint", new Vector3(26f, 1.8f, -4.5f));
            EnsureViewpointMarker("M7NoiseFarViewpoint", new Vector3(26f, 1.8f, -18f));
            EnsureQuad("M7WorldNoiseWall", new Vector3(26f, 2f, 0f), Vector3.zero, new Vector3(6.5f, 4.5f, 1f), worldNoiseMaterial, ShadowCastingMode.On);
            EnsureQuad("M7WorldNoiseFloor", new Vector3(26f, 0.05f, 2.2f), new Vector3(-90f, 0f, 0f), new Vector3(6.5f, 6.5f, 1f), worldNoiseMaterial, ShadowCastingMode.On);
            EnsureQuad("M7NoiseOffWall", new Vector3(33f, 2f, 0f), Vector3.zero, new Vector3(6.5f, 4.5f, 1f), noiseOffMaterial, ShadowCastingMode.On);
            EnsureRotatedCube("M7ObjectSpaceNoiseCube", new Vector3(33f, 1f, 3.2f), new Vector3(0f, 35f, 0f), Vector3.one * 1.8f, objectSpaceNoiseMaterial, ShadowCastingMode.On);

            EditorSceneManager.SaveScene(scene, TestRoomScenePath);
        }

        private static void EnsureLightingLabM7Anchors(Material bandNoiseMaterial, Material noiseOffMaterial)
        {
            Scene scene = EditorSceneManager.OpenScene(LightingLabScenePath, OpenSceneMode.Single);

            EnsureSphere("M7BandNoiseSphere", new Vector3(-1.6f, 0.95f, 12f), Vector3.one * 1.35f, bandNoiseMaterial, ShadowCastingMode.On);
            EnsureSphere("M7BandNoiseOffSphere", new Vector3(1.6f, 0.95f, 12f), Vector3.one * 1.35f, noiseOffMaterial, ShadowCastingMode.On);

            EditorSceneManager.SaveScene(scene, LightingLabScenePath);
        }

        private static void EnsureTestRoomM8Anchors(Material defaultMaterial, Material interiorMaterial)
        {
            Scene scene = EditorSceneManager.OpenScene(TestRoomScenePath, OpenSceneMode.Single);

            EnsureViewpointMarker("M8DepthViewpoint", new Vector3(39.5f, 1.6f, -4.4f));
            EnsureQuad("M8DepthAlphaClipQuad", new Vector3(39.5f, 1.6f, 0f), Vector3.zero, new Vector3(2.8f, 2.8f, 1f), interiorMaterial, ShadowCastingMode.On);
            EnsureRotatedCube("M8DepthNormalsCube", new Vector3(43f, 1f, 0.35f), new Vector3(0f, 27f, 0f), Vector3.one * 1.8f, defaultMaterial, ShadowCastingMode.On);

            EditorSceneManager.SaveScene(scene, TestRoomScenePath);
        }

        private static void EnsureLightingLabM8Anchors(Material defaultMaterial)
        {
            Scene scene = EditorSceneManager.OpenScene(LightingLabScenePath, OpenSceneMode.Single);

            EnsureViewpointMarker("M8MetaViewpoint", new Vector3(0f, 1.65f, 11.4f));
            EnsureCube("M8MetaEmissionCube", new Vector3(0f, 1f, 15.6f), Vector3.one * 1.6f, defaultMaterial, ShadowCastingMode.On);

            EditorSceneManager.SaveScene(scene, LightingLabScenePath);
        }

        private static void EnsureTestRoomM9Anchors(Material indirectBaselineMaterial, Material probeIndirectMaterial, Material cavityTintMaterial)
        {
            Scene scene = EditorSceneManager.OpenScene(TestRoomScenePath, OpenSceneMode.Single);

            EnsureViewpointMarker("M9IndirectViewpoint", new Vector3(47f, 1.7f, -4.4f));
            EnsureCube("M9IndirectBaselineCube_Room", new Vector3(47f, 1f, 0f), Vector3.one * 1.8f, indirectBaselineMaterial, ShadowCastingMode.On);
            EnsureSphere("M9ProbeIndirectSphere_Room", new Vector3(50.2f, 1f, 0.1f), Vector3.one * 1.55f, probeIndirectMaterial, ShadowCastingMode.On);
            EnsureRotatedCube("M9CavityTintCube_Room", new Vector3(53.4f, 1f, 0.25f), new Vector3(0f, 24f, 0f), Vector3.one * 1.7f, cavityTintMaterial, ShadowCastingMode.On);

            EditorSceneManager.SaveScene(scene, TestRoomScenePath);
        }

        private static void EnsureLightingLabM9Anchors(Material indirectBaselineMaterial, Material probeIndirectMaterial, Material cavityTintMaterial)
        {
            Scene scene = EditorSceneManager.OpenScene(LightingLabScenePath, OpenSceneMode.Single);

            EnsureViewpointMarker("M9LightingLabViewpoint", new Vector3(0f, 1.7f, 12.8f));
            EnsureSphere("M9IndirectBaselineSphere_Lab", new Vector3(-3.2f, 0.95f, 15.8f), Vector3.one * 1.35f, indirectBaselineMaterial, ShadowCastingMode.On);
            EnsureSphere("M9ProbeIndirectSphere_Lab", new Vector3(0f, 0.95f, 15.8f), Vector3.one * 1.35f, probeIndirectMaterial, ShadowCastingMode.On);
            EnsureQuad("M9CavityTintQuad_Lab", new Vector3(3.2f, 1.45f, 15.8f), Vector3.zero, new Vector3(2.1f, 2.1f, 1f), cavityTintMaterial, ShadowCastingMode.On);

            EditorSceneManager.SaveScene(scene, LightingLabScenePath);
        }

        private static void EnsureTestRoomM10Anchors(Material additionalOffMaterial, Material additionalFillOnlyMaterial, Material additionalQuantizedMaterial, Material additionalContinuousMaterial)
        {
            Scene scene = EditorSceneManager.OpenScene(TestRoomScenePath, OpenSceneMode.Single);

            EnsureViewpointMarker("M10AdditionalLightViewpoint", new Vector3(60f, 1.7f, -5.2f));
            EnsurePointLight("M10PointLight", new Vector3(60f, 3.2f, -2.4f), Color.white * 0.98f, 4.5f, 10f, LightShadows.Soft);
            EnsureSpotLight("M10SpotLight", new Vector3(66.4f, 4.8f, -2.8f), new Vector3(38f, 180f, 0f), new Color(0.78f, 0.84f, 1f, 1f), 5f, 12f, 42f, LightShadows.Soft);
            EnsureSphere("M10AdditionalOffSphere_Room", new Vector3(56.8f, 0.95f, 0.2f), Vector3.one * 1.35f, additionalOffMaterial, ShadowCastingMode.On);
            EnsureSphere("M10FillOnlySphere_Room", new Vector3(60f, 0.95f, 0.2f), Vector3.one * 1.35f, additionalFillOnlyMaterial, ShadowCastingMode.On);
            EnsureSphere("M10QuantizedSphere_Room", new Vector3(63.2f, 0.95f, 0.2f), Vector3.one * 1.35f, additionalQuantizedMaterial, ShadowCastingMode.On);
            EnsureSphere("M10ContinuousSphere_Room", new Vector3(66.4f, 0.95f, 0.2f), Vector3.one * 1.35f, additionalContinuousMaterial, ShadowCastingMode.On);

            EditorSceneManager.SaveScene(scene, TestRoomScenePath);
        }

        private static void EnsureLightingLabM10Anchors(Material additionalOffMaterial, Material additionalFillOnlyMaterial, Material additionalQuantizedMaterial, Material additionalContinuousMaterial)
        {
            Scene scene = EditorSceneManager.OpenScene(LightingLabScenePath, OpenSceneMode.Single);

            EnsureViewpointMarker("M10LightingLabViewpoint", new Vector3(0f, 1.75f, 16.2f));
            EnsurePointLight("M10PointLight_Lab", new Vector3(-3.2f, 3.8f, 19.4f), new Color(1f, 0.9f, 0.78f, 1f), 4f, 9f, LightShadows.Soft);
            EnsureSpotLight("M10SpotLight_Lab", new Vector3(4.8f, 4.5f, 18.2f), new Vector3(36f, 205f, 0f), new Color(0.76f, 0.84f, 1f, 1f), 4.5f, 12f, 38f, LightShadows.Soft);
            EnsureSphere("M10AdditionalOffSphere_Lab", new Vector3(-4.8f, 0.95f, 19.2f), Vector3.one * 1.35f, additionalOffMaterial, ShadowCastingMode.On);
            EnsureSphere("M10FillOnlySphere_Lab", new Vector3(-1.6f, 0.95f, 19.2f), Vector3.one * 1.35f, additionalFillOnlyMaterial, ShadowCastingMode.On);
            EnsureSphere("M10QuantizedSphere_Lab", new Vector3(1.6f, 0.95f, 19.2f), Vector3.one * 1.35f, additionalQuantizedMaterial, ShadowCastingMode.On);
            EnsureSphere("M10ContinuousSphere_Lab", new Vector3(4.8f, 0.95f, 19.2f), Vector3.one * 1.35f, additionalContinuousMaterial, ShadowCastingMode.On);

            EditorSceneManager.SaveScene(scene, LightingLabScenePath);
        }

        private static void EnsureTestRoomM11Anchors(Material triplanarOffMaterial, Material triplanarFullMaterial, Material vertexColorMaskMaterial, Material worldYGradientMaterial)
        {
            Scene scene = EditorSceneManager.OpenScene(TestRoomScenePath, OpenSceneMode.Single);

            EnsureViewpointMarker("M11TriplanarViewpoint", new Vector3(74f, 1.7f, -5.4f));
            EnsureRoughUvWall("M11TriplanarOffWall", new Vector3(70.6f, 2f, 0f), Vector3.zero, new Vector3(6.5f, 4.5f, 1f), triplanarOffMaterial, ShadowCastingMode.On);
            EnsureRoughUvWall("M11TriplanarWall", new Vector3(77.4f, 2f, 0f), Vector3.zero, new Vector3(6.5f, 4.5f, 1f), triplanarFullMaterial, ShadowCastingMode.On);
            EnsureVertexColorPanel("M11VertexColorPanel_Room", new Vector3(73.8f, 1.65f, 3.3f), Vector3.zero, new Vector3(2.6f, 2.6f, 1f), vertexColorMaskMaterial, ShadowCastingMode.On);
            EnsureQuad("M11WorldGradientWall", new Vector3(80.8f, 2f, 3.2f), Vector3.zero, new Vector3(4.5f, 4.5f, 1f), worldYGradientMaterial, ShadowCastingMode.On);

            EditorSceneManager.SaveScene(scene, TestRoomScenePath);
        }

        private static void EnsureLightingLabM11Anchors(Material triplanarOffMaterial, Material triplanarFullMaterial, Material vertexColorMaskMaterial, Material worldYGradientMaterial)
        {
            Scene scene = EditorSceneManager.OpenScene(LightingLabScenePath, OpenSceneMode.Single);

            EnsureViewpointMarker("M11LightingLabViewpoint", new Vector3(0f, 1.75f, 22.8f));
            EnsureSphere("M11TriplanarOffSphere_Lab", new Vector3(-4.8f, 0.95f, 23.2f), Vector3.one * 1.35f, triplanarOffMaterial, ShadowCastingMode.On);
            EnsureSphere("M11TriplanarSphere_Lab", new Vector3(-1.6f, 0.95f, 23.2f), Vector3.one * 1.35f, triplanarFullMaterial, ShadowCastingMode.On);
            EnsureVertexColorPanel("M11VertexColorPanel_Lab", new Vector3(1.6f, 1.45f, 23.2f), Vector3.zero, new Vector3(2.2f, 2.2f, 1f), vertexColorMaskMaterial, ShadowCastingMode.On);
            EnsureQuad("M11WorldGradientQuad_Lab", new Vector3(4.8f, 1.55f, 23.2f), Vector3.zero, new Vector3(2.4f, 2.8f, 1f), worldYGradientMaterial, ShadowCastingMode.On);

            EditorSceneManager.SaveScene(scene, LightingLabScenePath);
        }

        private static void EnsureTestRoomM13Anchors(Material tierLowMaterial, Material tierMediumMaterial, Material tierHighMaterial)
        {
            Scene scene = EditorSceneManager.OpenScene(TestRoomScenePath, OpenSceneMode.Single);

            EnsureViewpointMarker("M13TierViewpoint", new Vector3(92f, 1.7f, -5.4f));
            EnsureViewpointMarker("M13NoiseNearViewpoint", new Vector3(92f, 1.8f, -4.6f));
            EnsureViewpointMarker("M13NoiseFarViewpoint", new Vector3(92f, 1.8f, -18f));
            EnsureQuad("M13TierLowWall", new Vector3(85.2f, 2f, 0f), Vector3.zero, new Vector3(6.5f, 4.5f, 1f), tierLowMaterial, ShadowCastingMode.On);
            EnsureQuad("M13TierMediumWall", new Vector3(92f, 2f, 0f), Vector3.zero, new Vector3(6.5f, 4.5f, 1f), tierMediumMaterial, ShadowCastingMode.On);
            EnsureRoughUvWall("M13TierHighWall", new Vector3(98.8f, 2f, 0f), Vector3.zero, new Vector3(6.5f, 4.5f, 1f), tierHighMaterial, ShadowCastingMode.On);
            EnsureQuad("M13TierMediumFloor", new Vector3(92f, 0.05f, 3.4f), new Vector3(-90f, 0f, 0f), new Vector3(8f, 8f, 1f), tierMediumMaterial, ShadowCastingMode.On);
            EnsureRoughUvWall("M13TriplanarStressWall", new Vector3(98.8f, 2f, 3.6f), Vector3.zero, new Vector3(8f, 4.5f, 1f), tierHighMaterial, ShadowCastingMode.On);

            EditorSceneManager.SaveScene(scene, TestRoomScenePath);
        }

        private static void EnsureLightingLabM13Anchors(Material tierLowMaterial, Material tierMediumMaterial, Material tierHighMaterial)
        {
            Scene scene = EditorSceneManager.OpenScene(LightingLabScenePath, OpenSceneMode.Single);

            EnsureViewpointMarker("M13LightingLabViewpoint", new Vector3(0f, 1.75f, 28.6f));
            EnsurePointLight("M13PointLight_Lab", new Vector3(-1.6f, 3.8f, 29.8f), new Color(1f, 0.92f, 0.8f, 1f), 4f, 9f, LightShadows.Soft);
            EnsureSpotLight("M13SpotLight_Lab", new Vector3(4.8f, 4.4f, 29.2f), new Vector3(34f, 210f, 0f), new Color(0.76f, 0.84f, 1f, 1f), 4.5f, 12f, 40f, LightShadows.Soft);
            EnsureSphere("M13TierLowSphere_Lab", new Vector3(-4.8f, 0.95f, 29.2f), Vector3.one * 1.35f, tierLowMaterial, ShadowCastingMode.On);
            EnsureSphere("M13TierMediumSphere_Lab", new Vector3(-1.6f, 0.95f, 29.2f), Vector3.one * 1.35f, tierMediumMaterial, ShadowCastingMode.On);
            EnsureSphere("M13TierHighSphere_Lab", new Vector3(1.6f, 0.95f, 29.2f), Vector3.one * 1.35f, tierHighMaterial, ShadowCastingMode.On);
            EnsureQuad("M13TierHighPanel_Lab", new Vector3(4.8f, 1.45f, 29.2f), Vector3.zero, new Vector3(2.4f, 2.4f, 1f), tierHighMaterial, ShadowCastingMode.On);
            EnsureQuad("M13AdditionalLightWall_Lab", new Vector3(-4.2f, 2.4f, 33.8f), Vector3.zero, new Vector3(7.5f, 4.8f, 1f), tierMediumMaterial, ShadowCastingMode.On);
            EnsureQuad("M13AdditionalLightFloor_Lab", new Vector3(3.4f, 0.05f, 33.8f), new Vector3(-90f, 0f, 0f), new Vector3(8f, 8f, 1f), tierMediumMaterial, ShadowCastingMode.On);

            EditorSceneManager.SaveScene(scene, LightingLabScenePath);
        }

        private static void EnsureTestRoomM14Anchors(
            Material clayMaterial,
            Material paintedPlasterMaterial,
            Material matteToyPlasticMaterial,
            Material ceramicToyMaterial,
            Material chalkPastelMaterial)
        {
            Scene scene = EditorSceneManager.OpenScene(TestRoomScenePath, OpenSceneMode.Single);

            EnsureViewpointMarker("M14IndoorViewpoint", new Vector3(108f, 1.75f, -5.6f));
            EnsureViewpointMarker("M14LightmapViewpoint", new Vector3(113.8f, 1.75f, -5.6f));
            EnsureViewpointMarker("M14SSAOOnViewpoint", new Vector3(120.5f, 1.8f, -5.6f));
            EnsureViewpointMarker("M14SSAOOffViewpoint", new Vector3(120.5f, 1.8f, -11.6f));

            EnsureProBuilderPlane(
                "M14ProBuilderFloor_Room",
                new Vector3(104.2f, 0.05f, 0f),
                Quaternion.identity,
                8f,
                8f,
                3,
                3,
                Axis.Up,
                clayMaterial,
                ShadowCastingMode.On);
            EnsureProBuilderPlane(
                "M14ProBuilderWall_Room",
                new Vector3(104.2f, 2.4f, 4f),
                Quaternion.identity,
                8f,
                4.8f,
                2,
                2,
                Axis.Forward,
                paintedPlasterMaterial,
                ShadowCastingMode.On);
            EnsureProBuilderStair(
                "M14Stair_Room",
                new Vector3(110f, 0f, 0.35f),
                Quaternion.identity,
                new Vector3(3.8f, 2.7f, 5.4f),
                5,
                matteToyPlasticMaterial,
                ShadowCastingMode.On);
            EnsureCylinder("M14Column_Room", new Vector3(115f, 1.6f, 0.2f), new Vector3(1.15f, 1.6f, 1.15f), ceramicToyMaterial, ShadowCastingMode.On);
            EnsureBeveledWall(
                "M14BeveledWall_Room",
                new Vector3(119.6f, 2.1f, 0.2f),
                Vector3.zero,
                new Vector3(5.6f, 4.6f, 1f),
                paintedPlasterMaterial,
                ShadowCastingMode.On);
            EnsureCylinder("M14LightmappedColumn_Room", new Vector3(111.8f, 1.6f, 4f), new Vector3(1.1f, 1.6f, 1.1f), ceramicToyMaterial, ShadowCastingMode.On);
            EnsureCylinder("M14DynamicColumn_Room", new Vector3(115.8f, 1.6f, 4f), new Vector3(1.1f, 1.6f, 1.1f), ceramicToyMaterial, ShadowCastingMode.On);
            ApplyLightmapState(GameObject.Find("M14LightmappedColumn_Room"), true);
            ApplyLightmapState(GameObject.Find("M14DynamicColumn_Room"), false);
            EnsureQuad("M14SSAOReferenceWall_Room", new Vector3(120.5f, 2f, 0.2f), Vector3.zero, new Vector3(6.2f, 4.6f, 1f), chalkPastelMaterial, ShadowCastingMode.On);

            EditorSceneManager.SaveScene(scene, TestRoomScenePath);
        }

        private static void EnsureLightingLabM14Anchors(
            Material clayMaterial,
            Material paintedPlasterMaterial,
            Material matteToyPlasticMaterial,
            Material ceramicToyMaterial,
            Material chalkPastelMaterial)
        {
            Scene scene = EditorSceneManager.OpenScene(LightingLabScenePath, OpenSceneMode.Single);

            EnsureViewpointMarker("M14DirectionalOnlyViewpoint", new Vector3(0f, 1.75f, 38.8f));
            EnsureViewpointMarker("M14PointLightViewpoint", new Vector3(-4.8f, 1.75f, 55.6f));
            EnsureViewpointMarker("M14SpotLightViewpoint", new Vector3(4.8f, 1.75f, 67.8f));

            EnsureSphere("M14ClayDioramaSphere_Lab", new Vector3(-6.4f, 0.95f, 42.6f), Vector3.one * 1.35f, clayMaterial, ShadowCastingMode.On);
            EnsureSphere("M14PaintedPlasterSphere_Lab", new Vector3(-3.2f, 0.95f, 42.6f), Vector3.one * 1.35f, paintedPlasterMaterial, ShadowCastingMode.On);
            EnsureSphere("M14MatteToyPlasticSphere_Lab", new Vector3(0f, 0.95f, 42.6f), Vector3.one * 1.35f, matteToyPlasticMaterial, ShadowCastingMode.On);
            EnsureSphere("M14CeramicToySphere_Lab", new Vector3(3.2f, 0.95f, 42.6f), Vector3.one * 1.35f, ceramicToyMaterial, ShadowCastingMode.On);
            EnsureSphere("M14ChalkPastelSphere_Lab", new Vector3(6.4f, 0.95f, 42.6f), Vector3.one * 1.35f, chalkPastelMaterial, ShadowCastingMode.On);

            EnsurePointLight("M14PointLight_Lab", new Vector3(-4.8f, 4f, 58f), new Color(1f, 0.92f, 0.84f, 1f), 4.2f, 8f, LightShadows.Soft);
            EnsureCylinder("M14PointLightColumn_Lab", new Vector3(-4.8f, 1.6f, 60f), new Vector3(1.1f, 1.6f, 1.1f), ceramicToyMaterial, ShadowCastingMode.On);

            EnsureSpotLight("M14SpotLight_Lab", new Vector3(4.8f, 4.4f, 70.6f), new Vector3(34f, 180f, 0f), new Color(0.76f, 0.84f, 1f, 1f), 4.4f, 10f, 38f, LightShadows.Soft);
            EnsureBeveledWall("M14SpotLightWall_Lab", new Vector3(4.8f, 2f, 73.4f), Vector3.zero, new Vector3(4.8f, 4.2f, 1f), chalkPastelMaterial, ShadowCastingMode.On);

            EditorSceneManager.SaveScene(scene, LightingLabScenePath);
        }

        private static void EnsureViewpointMarker(string name, Vector3 position)
        {
            GameObject markerObject = GameObject.Find(name);
            if (markerObject == null)
            {
                markerObject = new GameObject(name);
            }

            markerObject.transform.position = position;
        }

        private static void EnsureCube(string name, Vector3 position, Vector3 scale, Material material, ShadowCastingMode shadowCastingMode)
        {
            GameObject targetObject = EnsurePrimitive(PrimitiveType.Cube, name, position, Quaternion.identity, scale);
            ApplyRendererSettings(targetObject, material, shadowCastingMode);
        }

        private static void EnsureRotatedCube(string name, Vector3 position, Vector3 eulerAngles, Vector3 scale, Material material, ShadowCastingMode shadowCastingMode)
        {
            GameObject targetObject = EnsurePrimitive(PrimitiveType.Cube, name, position, Quaternion.Euler(eulerAngles), scale);
            ApplyRendererSettings(targetObject, material, shadowCastingMode);
        }

        private static void EnsureSphere(string name, Vector3 position, Vector3 scale, Material material, ShadowCastingMode shadowCastingMode)
        {
            GameObject targetObject = EnsurePrimitive(PrimitiveType.Sphere, name, position, Quaternion.identity, scale);
            ApplyRendererSettings(targetObject, material, shadowCastingMode);
        }

        private static void EnsureCylinder(string name, Vector3 position, Vector3 scale, Material material, ShadowCastingMode shadowCastingMode)
        {
            GameObject targetObject = EnsurePrimitive(PrimitiveType.Cylinder, name, position, Quaternion.identity, scale);
            ApplyRendererSettings(targetObject, material, shadowCastingMode);
        }

        private static void EnsureQuad(string name, Vector3 position, Vector3 eulerAngles, Vector3 scale, Material material, ShadowCastingMode shadowCastingMode)
        {
            GameObject targetObject = EnsurePrimitive(PrimitiveType.Quad, name, position, Quaternion.Euler(eulerAngles), scale);
            ApplyRendererSettings(targetObject, material, shadowCastingMode);
        }

        private static void EnsureVertexColorPanel(string name, Vector3 position, Vector3 eulerAngles, Vector3 scale, Material material, ShadowCastingMode shadowCastingMode)
        {
            GameObject targetObject = GameObject.Find(name);
            if (targetObject == null)
            {
                targetObject = new GameObject(name);
            }

            targetObject.transform.position = position;
            targetObject.transform.rotation = Quaternion.Euler(eulerAngles);
            targetObject.transform.localScale = scale;

            MeshFilter meshFilter = targetObject.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = targetObject.AddComponent<MeshFilter>();
            }

            MeshRenderer meshRenderer = targetObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = targetObject.AddComponent<MeshRenderer>();
            }

            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null || mesh.name != name + "_Mesh")
            {
                mesh = new Mesh { name = name + "_Mesh" };
                meshFilter.sharedMesh = mesh;
            }

            mesh.Clear();
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
            };
            mesh.normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            mesh.colors = new[]
            {
                new Color(1f, 0f, 0.15f, 0f),
                new Color(0f, 1f, 0.35f, 0f),
                new Color(0.15f, 0.4f, 1f, 0f),
                new Color(0.25f, 0.55f, 0.85f, 1f),
            };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateBounds();

            ApplyRendererSettings(targetObject, material, shadowCastingMode);
            meshRenderer.receiveShadows = true;
        }

        private static void EnsureRoughUvWall(string name, Vector3 position, Vector3 eulerAngles, Vector3 scale, Material material, ShadowCastingMode shadowCastingMode)
        {
            GameObject targetObject = GameObject.Find(name);
            if (targetObject == null)
            {
                targetObject = new GameObject(name);
            }

            targetObject.transform.position = position;
            targetObject.transform.rotation = Quaternion.Euler(eulerAngles);
            targetObject.transform.localScale = scale;

            MeshFilter meshFilter = targetObject.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = targetObject.AddComponent<MeshFilter>();
            }

            MeshRenderer meshRenderer = targetObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = targetObject.AddComponent<MeshRenderer>();
            }

            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null || mesh.name != name + "_Mesh")
            {
                mesh = new Mesh { name = name + "_Mesh" };
                meshFilter.sharedMesh = mesh;
            }

            mesh.Clear();
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
            };
            mesh.normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(6.5f, 0.05f),
                new Vector2(0.2f, 0.32f),
                new Vector2(9.25f, 0.41f),
            };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateBounds();

            ApplyRendererSettings(targetObject, material, shadowCastingMode);
            meshRenderer.receiveShadows = true;
        }

        private static void EnsureBeveledWall(string name, Vector3 position, Vector3 eulerAngles, Vector3 scale, Material material, ShadowCastingMode shadowCastingMode)
        {
            GameObject targetObject = GameObject.Find(name);
            if (targetObject == null)
            {
                targetObject = new GameObject(name);
            }

            targetObject.transform.position = position;
            targetObject.transform.rotation = Quaternion.Euler(eulerAngles);
            targetObject.transform.localScale = scale;

            MeshFilter meshFilter = targetObject.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = targetObject.AddComponent<MeshFilter>();
            }

            MeshRenderer meshRenderer = targetObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = targetObject.AddComponent<MeshRenderer>();
            }

            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null || mesh.name != name + "_Mesh")
            {
                mesh = new Mesh { name = name + "_Mesh" };
                meshFilter.sharedMesh = mesh;
            }

            float bevel = 0.12f;
            float halfDepth = 0.12f;
            Vector3[] ring =
            {
                new Vector3(-0.5f + bevel, -0.5f, halfDepth),
                new Vector3(0.5f - bevel, -0.5f, halfDepth),
                new Vector3(0.5f, -0.5f + bevel, halfDepth),
                new Vector3(0.5f, 0.5f - bevel, halfDepth),
                new Vector3(0.5f - bevel, 0.5f, halfDepth),
                new Vector3(-0.5f + bevel, 0.5f, halfDepth),
                new Vector3(-0.5f, 0.5f - bevel, halfDepth),
                new Vector3(-0.5f, -0.5f + bevel, halfDepth),
            };

            mesh.Clear();
            Vector3[] vertices = new Vector3[52];
            Vector2[] uv = new Vector2[52];
            int[] triangles = new int[96];

            vertices[0] = new Vector3(0f, 0f, halfDepth);
            uv[0] = new Vector2(0.5f, 0.5f);
            vertices[9] = new Vector3(0f, 0f, -halfDepth);
            uv[9] = new Vector2(0.5f, 0.5f);

            for (int index = 0; index < ring.Length; index++)
            {
                Vector3 front = ring[index];
                Vector3 back = new Vector3(front.x, front.y, -halfDepth);
                int frontIndex = 1 + index;
                int backIndex = 10 + index;
                vertices[frontIndex] = front;
                vertices[backIndex] = back;
                uv[frontIndex] = new Vector2(front.x + 0.5f, front.y + 0.5f);
                uv[backIndex] = new Vector2(back.x + 0.5f, back.y + 0.5f);
            }

            int triangleIndex = 0;
            for (int index = 0; index < ring.Length; index++)
            {
                int currentFront = 1 + index;
                int nextFront = 1 + ((index + 1) % ring.Length);
                triangles[triangleIndex++] = 0;
                triangles[triangleIndex++] = nextFront;
                triangles[triangleIndex++] = currentFront;

                int currentBack = 10 + index;
                int nextBack = 10 + ((index + 1) % ring.Length);
                triangles[triangleIndex++] = 9;
                triangles[triangleIndex++] = currentBack;
                triangles[triangleIndex++] = nextBack;
            }

            int sideVertexStart = 18;
            for (int index = 0; index < ring.Length; index++)
            {
                Vector3 frontCurrent = ring[index];
                Vector3 frontNext = ring[(index + 1) % ring.Length];
                Vector3 backCurrent = new Vector3(frontCurrent.x, frontCurrent.y, -halfDepth);
                Vector3 backNext = new Vector3(frontNext.x, frontNext.y, -halfDepth);
                int baseIndex = sideVertexStart + (index * 4);
                vertices[baseIndex] = frontCurrent;
                vertices[baseIndex + 1] = frontNext;
                vertices[baseIndex + 2] = backNext;
                vertices[baseIndex + 3] = backCurrent;
                uv[baseIndex] = new Vector2(0f, 1f);
                uv[baseIndex + 1] = new Vector2(1f, 1f);
                uv[baseIndex + 2] = new Vector2(1f, 0f);
                uv[baseIndex + 3] = new Vector2(0f, 0f);

                triangles[triangleIndex++] = baseIndex;
                triangles[triangleIndex++] = baseIndex + 1;
                triangles[triangleIndex++] = baseIndex + 2;
                triangles[triangleIndex++] = baseIndex;
                triangles[triangleIndex++] = baseIndex + 2;
                triangles[triangleIndex++] = baseIndex + 3;
            }

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            EditorSceneManager.MarkSceneDirty(targetObject.scene);

            ApplyRendererSettings(targetObject, material, shadowCastingMode);
            meshRenderer.receiveShadows = true;
        }

        private static void EnsureProBuilderPlane(
            string name,
            Vector3 position,
            Quaternion rotation,
            float width,
            float height,
            int widthCuts,
            int heightCuts,
            Axis axis,
            Material material,
            ShadowCastingMode shadowCastingMode)
        {
            GameObject existingObject = GameObject.Find(name);
            if (existingObject != null)
            {
                Object.DestroyImmediate(existingObject);
            }

            ProBuilderMesh proBuilderMesh = ShapeGenerator.GeneratePlane(PivotLocation.Center, width, height, widthCuts, heightCuts, axis);
            proBuilderMesh.gameObject.name = name;

            proBuilderMesh.transform.position = position;
            proBuilderMesh.transform.rotation = rotation;
            proBuilderMesh.transform.localScale = Vector3.one;
            proBuilderMesh.ToMesh();
            proBuilderMesh.Refresh();

            ApplyRendererSettings(proBuilderMesh.gameObject, material, shadowCastingMode);
        }

        private static void EnsureProBuilderStair(
            string name,
            Vector3 position,
            Quaternion rotation,
            Vector3 size,
            int steps,
            Material material,
            ShadowCastingMode shadowCastingMode)
        {
            GameObject existingObject = GameObject.Find(name);
            if (existingObject != null)
            {
                Object.DestroyImmediate(existingObject);
            }

            ProBuilderMesh proBuilderMesh = ShapeGenerator.GenerateStair(PivotLocation.Center, size, steps, true);
            proBuilderMesh.gameObject.name = name;

            proBuilderMesh.transform.position = position;
            proBuilderMesh.transform.rotation = rotation;
            proBuilderMesh.transform.localScale = Vector3.one;
            proBuilderMesh.ToMesh();
            proBuilderMesh.Refresh();

            ApplyRendererSettings(proBuilderMesh.gameObject, material, shadowCastingMode);
        }

        private static GameObject EnsurePrimitive(PrimitiveType primitiveType, string name, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            GameObject targetObject = GameObject.Find(name);
            if (targetObject == null)
            {
                targetObject = GameObject.CreatePrimitive(primitiveType);
                targetObject.name = name;
            }

            targetObject.transform.position = position;
            targetObject.transform.rotation = rotation;
            targetObject.transform.localScale = scale;
            return targetObject;
        }

        private static void ApplyRendererSettings(GameObject targetObject, Material material, ShadowCastingMode shadowCastingMode)
        {
            MeshRenderer renderer = targetObject.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                return;
            }

            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadowCastingMode;
            renderer.receiveShadows = true;
            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(targetObject);
            EditorSceneManager.MarkSceneDirty(targetObject.scene);
        }

        private static void ApplyLightmapState(GameObject targetObject, bool contributesGI)
        {
            if (targetObject == null)
            {
                return;
            }

            GameObjectUtility.SetStaticEditorFlags(targetObject, contributesGI ? StaticEditorFlags.ContributeGI : 0);

            MeshRenderer renderer = targetObject.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                return;
            }

            renderer.receiveGI = contributesGI ? ReceiveGI.Lightmaps : ReceiveGI.LightProbes;
            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(targetObject);
        }

        private static void EnsurePointLight(string name, Vector3 position, Color color, float intensity, float range, LightShadows shadows)
        {
            EnsureLight(name, LightType.Point, position, Quaternion.identity, color, intensity, range, 30f, shadows);
        }

        private static void EnsureSpotLight(string name, Vector3 position, Vector3 eulerAngles, Color color, float intensity, float range, float spotAngle, LightShadows shadows)
        {
            EnsureLight(name, LightType.Spot, position, Quaternion.Euler(eulerAngles), color, intensity, range, spotAngle, shadows);
        }

        private static void EnsureLight(string name, LightType lightType, Vector3 position, Quaternion rotation, Color color, float intensity, float range, float spotAngle, LightShadows shadows)
        {
            GameObject lightObject = GameObject.Find(name);
            if (lightObject == null)
            {
                lightObject = new GameObject(name);
            }

            lightObject.transform.position = position;
            lightObject.transform.rotation = rotation;

            Light light = lightObject.GetComponent<Light>();
            if (light == null)
            {
                light = lightObject.AddComponent<Light>();
            }

            light.type = lightType;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.spotAngle = spotAngle;
            light.shadows = shadows;
            light.renderMode = LightRenderMode.Auto;
        }
    }
}
