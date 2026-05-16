using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BC.Rendering
{
    internal static class ParticleUnlitWebGlBuildUtility
    {
        private const string ValidationScenePath = "Assets/Scenes/Particles/ParticleMaterialTestScene.unity";
        private const string ValidationBuildOutputPath = "Temp/ParticleUnlitWebGLValidation";

        [MenuItem("Tools/BC/Particles/ParticleUnlit/Run M11 WebGL Validation Build")]
        public static void RunM11WebGlValidationBuild()
        {
            RunM15WebGlValidationBuild();
        }

        [MenuItem("Tools/BC/Particles/ParticleUnlit/Run M15 WebGL Validation Build")]
        public static void RunM15WebGlValidationBuild()
        {
            RunM16WebGlValidationBuild();
        }

        [MenuItem("Tools/BC/Particles/ParticleUnlit/Run M16 WebGL Validation Build")]
        public static void RunM16WebGlValidationBuild()
        {
            BuildReport report = BuildValidationPlayer();
            Debug.Log($"ParticleUnlit M16 WebGL validation build completed: {report.summary.outputPath}");
        }

        public static void RunM12WebGlValidationBuild()
        {
            RunM15WebGlValidationBuild();
        }

        public static void RunM10WebGlValidationBuild()
        {
            RunM15WebGlValidationBuild();
        }

        public static void RunM9WebGlValidationBuild()
        {
            RunM15WebGlValidationBuild();
        }

        public static void RunM7WebGlValidationBuild()
        {
            RunM15WebGlValidationBuild();
        }

        internal static BuildReport BuildValidationPlayer()
        {
            ParticleUnlitBuildValidator.EnsureM16ValidationAssetsReady();

            if (Directory.Exists(ValidationBuildOutputPath))
            {
                Directory.Delete(ValidationBuildOutputPath, true);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ValidationBuildOutputPath) ?? "Temp");

            // Build settings を汚さず validation scene 単体だけで WebGL compile を確認する。
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ValidationScenePath },
                locationPathName = ValidationBuildOutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"ParticleUnlit M16 WebGL validation build failed with result {report.summary.result}.");
            }

            return report;
        }
    }
}