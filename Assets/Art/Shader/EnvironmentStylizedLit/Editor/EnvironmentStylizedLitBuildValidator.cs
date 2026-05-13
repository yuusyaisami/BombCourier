using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BC.Rendering
{
    internal static class EnvironmentStylizedLitBuildValidator
    {
        private const string ShaderPath = "Assets/Art/Shader/EnvironmentStylizedLit/EnvironmentStylizedLit.shader";
        private const string ValidationMaterialPrefix = "ESL_Test_";
        private static readonly string[] MaterialSearchFolders = { "Assets" };

        internal static bool TryGetDebugViewBuildError(Material material, out string errorMessage)
        {
            if (material == null || !material.HasProperty("_DebugView"))
            {
                errorMessage = null;
                return false;
            }

            if (Mathf.RoundToInt(material.GetFloat("_DebugView")) == 0)
            {
                errorMessage = null;
                return false;
            }

            string materialPath = AssetDatabase.GetAssetPath(material);
            errorMessage =
                $"EnvironmentStylizedLit material keeps Debug View enabled for a non-development build: {material.name}" +
                (string.IsNullOrEmpty(materialPath) ? string.Empty : $" ({materialPath})") +
                ". Reset Debug View to Off before shipping.";
            return true;
        }

        internal static bool TryGetNonDevelopmentBuildError(out string errorMessage)
        {
            List<string> invalidMaterials = new List<string>();

            foreach (Material material in FindEnvironmentStylizedLitMaterials())
            {
                if (!TryGetDebugViewBuildError(material, out string materialError))
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

        internal static bool TryGetTierDescription(Material material, out string description)
        {
            if (EnvironmentStylizedLitPerformanceTierUtility.TryInferTier(material, out EnvironmentStylizedLitPerformanceTier tier))
            {
                description = EnvironmentStylizedLitPerformanceTierUtility.GetTierDescription((int)tier);
                return true;
            }

            description = null;
            return false;
        }

        private static IEnumerable<Material> FindEnvironmentStylizedLitMaterials()
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

    internal sealed class EnvironmentStylizedLitBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if ((report.summary.options & BuildOptions.Development) != 0)
            {
                return;
            }

            if (EnvironmentStylizedLitBuildValidator.TryGetNonDevelopmentBuildError(out string errorMessage))
            {
                throw new BuildFailedException(errorMessage);
            }
        }
    }
}