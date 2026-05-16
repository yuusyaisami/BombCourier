using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BC.Rendering
{
    internal static class ParticleUnlitBuildValidator
    {
        private const string ValidationScenePath = "Assets/Scenes/Particles/ParticleMaterialTestScene.unity";
        private const string ShaderPath = "Assets/Art/Shader/Particles/ParticleUnlit/BC_Particles_ParticleUnlit.shader";
        private const string ValidationMaterialPrefix = "M_Particle_Test_";
        private static readonly string[] MaterialSearchFolders = { "Assets/Art/Materials/Particles" };
        private static readonly string[] RequiredM11ValidationMaterialPaths =
        {
            "Assets/Art/Materials/Particles/Unlit/M_Particle_Test_DustSoftParticles_Alpha.mat",
            "Assets/Art/Materials/Particles/Unlit/M_Particle_Test_SmokeSoftParticles_Alpha.mat",
            "Assets/Art/Materials/Particles/Unlit/M_Particle_Test_GlowCameraFade_Premultiply.mat"
        };

        private static readonly string[] RequiredM12ValidationMaterialPaths =
        {
            "Assets/Art/Materials/Particles/Lit/M_Particle_Raindrop_Lit.mat",
            "Assets/Art/Materials/Particles/Lit/M_Particle_Bubble_Lit.mat",
            "Assets/Art/Materials/Particles/Lit/M_Particle_Debris_Lit.mat"
        };

        private static readonly string[] RequiredM15ValidationMaterialPaths =
        {
            "Assets/Art/Materials/Particles/Unlit/M_Particle_Test_TierLow_Alpha.mat",
            "Assets/Art/Materials/Particles/Unlit/M_Particle_Test_TierMedium_Alpha.mat",
            "Assets/Art/Materials/Particles/Unlit/M_Particle_Test_TierHigh_Alpha.mat"
        };

        private static readonly string[] RequiredPrefabPaths =
        {
            "Assets/Art/Prefab/Particles/Unlit/FX_Particle_Dust.prefab",
            "Assets/Art/Prefab/Particles/Unlit/FX_Particle_Smoke.prefab",
            "Assets/Art/Prefab/Particles/Unlit/FX_Particle_Glow.prefab",
            "Assets/Art/Prefab/Particles/Unlit/FX_Particle_Spark.prefab",
            "Assets/Art/Prefab/Particles/Unlit/FX_Particle_Magic.prefab",
            "Assets/Art/Prefab/Particles/Unlit/FX_Particle_SmokeFlipbook.prefab",
            "Assets/Art/Prefab/Particles/Unlit/FX_Particle_MagicBurst.prefab",
            "Assets/Art/Prefab/Particles/Unlit/FX_Particle_SparkCustomData.prefab",
            "Assets/Art/Prefab/Particles/Unlit/FX_Particle_MagicCustomData.prefab"
        };

        internal static void EnsureM8ValidationAssetsReady()
        {
            EnsureM15ValidationAssetsReady();
        }

        internal static void EnsureM9ValidationAssetsReady()
        {
            EnsureM15ValidationAssetsReady();
        }

        internal static void EnsureM10ValidationAssetsReady()
        {
            EnsureM15ValidationAssetsReady();
        }

        internal static void EnsureM11ValidationAssetsReady()
        {
            EnsureM15ValidationAssetsReady();
        }

        internal static void EnsureM15ValidationAssetsReady()
        {
            ParticleUnlitValidationBootstrapper.BootstrapM15QualityTierValidationAssets();

            if (TryGetWebGlBuildError(out string errorMessage))
            {
                throw new BuildFailedException(errorMessage);
            }
        }

        internal static void EnsureM16ValidationAssetsReady()
        {
            ParticleUnlitValidationBootstrapper.BootstrapM16ReviewHarness();

            if (TryGetWebGlBuildError(out string errorMessage))
            {
                throw new BuildFailedException(errorMessage);
            }
        }

        internal static void EnsureM12ValidationAssetsReady()
        {
            EnsureM15ValidationAssetsReady();
        }

        internal static bool TryGetTierDescription(Material material, out string description)
        {
            if (ParticleUnlitQualityTierUtility.TryInferTier(material, out ParticleUnlitQualityTier tier))
            {
                description = ParticleUnlitQualityTierUtility.GetTierDescription((int)tier);
                return true;
            }

            description = null;
            return false;
        }

        internal static bool TryGetDebugModeBuildError(Material material, out string errorMessage)
        {
            if (material == null || material.shader == null || material.shader.name != "BC/Particles/ParticleUnlit" || !material.HasProperty("_DebugMode"))
            {
                errorMessage = null;
                return false;
            }

            if (Mathf.RoundToInt(material.GetFloat("_DebugMode")) == 0)
            {
                errorMessage = null;
                return false;
            }

            string materialPath = AssetDatabase.GetAssetPath(material);
            errorMessage =
                $"ParticleUnlit material keeps Debug Mode enabled for a non-development build: {material.name}" +
                (string.IsNullOrEmpty(materialPath) ? string.Empty : $" ({materialPath})") +
                ". Reset Debug Mode to Final before shipping.";
            return true;
        }

        internal static bool TryGetNonDevelopmentBuildError(out string errorMessage)
        {
            List<string> invalidMaterials = new List<string>();

            foreach (Material material in FindParticleUnlitMaterials())
            {
                if (!TryGetDebugModeBuildError(material, out string materialError))
                {
                    continue;
                }

                invalidMaterials.Add(materialError);
            }

            if (invalidMaterials.Count == 0)
            {
                errorMessage = null;
                return false;
            }

            errorMessage = string.Join("\n", invalidMaterials);
            return true;
        }

        internal static bool TryGetWebGlBuildError(out string errorMessage)
        {
            List<string> missingAssets = new List<string>();
            List<string> tierPolicyErrors = new List<string>();

            if (!File.Exists(ValidationScenePath))
            {
                missingAssets.Add("Missing validation scene: " + ValidationScenePath);
            }

            foreach (string prefabPath in RequiredPrefabPaths)
            {
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(prefabPath) != null)
                {
                    continue;
                }

                missingAssets.Add("Missing validation prefab: " + prefabPath);
            }

            foreach (string materialPath in RequiredM11ValidationMaterialPaths)
            {
                if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) != null)
                {
                    continue;
                }

                missingAssets.Add("Missing M11 validation material: " + materialPath);
            }

            foreach (string materialPath in RequiredM12ValidationMaterialPaths)
            {
                if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) != null)
                {
                    continue;
                }

                missingAssets.Add("Missing M12 validation material: " + materialPath);
            }

            foreach (string materialPath in RequiredM15ValidationMaterialPaths)
            {
                if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) != null)
                {
                    continue;
                }

                missingAssets.Add("Missing M15 validation material: " + materialPath);
            }

            foreach (Material material in FindParticleUnlitMaterials())
            {
                if (!TryGetWebGlTierBuildError(material, out string materialError))
                {
                    continue;
                }

                tierPolicyErrors.Add(materialError);
            }

            if (missingAssets.Count == 0 && tierPolicyErrors.Count == 0)
            {
                errorMessage = null;
                return false;
            }

            List<string> allErrors = new List<string>(missingAssets.Count + tierPolicyErrors.Count);
            allErrors.AddRange(missingAssets);
            allErrors.AddRange(tierPolicyErrors);
            errorMessage = string.Join("\n", allErrors);
            return true;
        }

        internal static bool TryGetWebGlTierBuildError(Material material, out string errorMessage)
        {
            if (material == null || !ParticleUnlitQualityTierUtility.RequiresHighTierPath(material))
            {
                errorMessage = null;
                return false;
            }

            string materialPath = AssetDatabase.GetAssetPath(material);
            errorMessage =
                $"ParticleUnlit material enables high-tier boundary features for the standard WebGL path: {material.name}" +
                (string.IsNullOrEmpty(materialPath) ? string.Empty : $" ({materialPath})") +
                ". Keep standard WebGL materials at Low or Medium, or formalize a separate high-quality path before shipping.";
            return true;
        }

        private static IEnumerable<Material> FindParticleUnlitMaterials()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
            {
                yield break;
            }

            string[] materialGuids = AssetDatabase.FindAssets("t:Material", MaterialSearchFolders);
            foreach (string materialGuid in materialGuids)
            {
                string materialPath = AssetDatabase.GUIDToAssetPath(materialGuid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                if (material == null || material.shader != shader || !IsShippingCandidateMaterial(materialPath))
                {
                    continue;
                }

                yield return material;
            }
        }

        private static bool IsShippingCandidateMaterial(string materialPath)
        {
            if (string.IsNullOrEmpty(materialPath))
            {
                return false;
            }

            string fileName = Path.GetFileNameWithoutExtension(materialPath);
            return !fileName.StartsWith(ValidationMaterialPrefix, System.StringComparison.OrdinalIgnoreCase);
        }
    }

    internal sealed class ParticleUnlitBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.WebGL)
            {
                ParticleUnlitBuildValidator.EnsureM12ValidationAssetsReady();
            }

            if ((report.summary.options & BuildOptions.Development) != 0)
            {
                return;
            }

            if (ParticleUnlitBuildValidator.TryGetNonDevelopmentBuildError(out string errorMessage))
            {
                throw new BuildFailedException(errorMessage);
            }
        }
    }
}