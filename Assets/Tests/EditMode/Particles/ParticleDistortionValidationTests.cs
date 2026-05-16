using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BC.Rendering.Tests
{
    public sealed class ParticleDistortionValidationTests
    {
        private const string ShaderPath = "Assets/Art/Shader/Particles/ParticleDistortion/BC_Particles_ParticleDistortion.shader";
        private const string UniversalAdditionalCameraDataTypeName = "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime";
        private const string BootstrapperPath = "Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs";
        private const string BuildValidatorPath = "Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitBuildValidator.cs";
        private const string WebGlBuildUtilityPath = "Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitWebGlBuildUtility.cs";
        private const string ShaderGuiPath = "Assets/Art/Shader/Particles/ParticleDistortion/Editor/ParticleDistortionShaderGUI.cs";
        private const string ValidatorPath = "Assets/Art/Shader/Particles/ParticleDistortion/Editor/ParticleDistortionMaterialValidator.cs";
        private const string PresetUtilityPath = "Assets/Art/Shader/Particles/ParticleDistortion/Editor/ParticleDistortionPresetUtility.cs";
        private const string InputHlslPath = "Assets/Art/Shader/Particles/ParticleDistortion/HLSL/ParticleDistortion_Input.hlsl";
        private const string CommonHlslPath = "Assets/Art/Shader/Particles/ParticleDistortion/HLSL/ParticleDistortion_Common.hlsl";
        private const string SamplingHlslPath = "Assets/Art/Shader/Particles/ParticleDistortion/HLSL/ParticleDistortion_Sampling.hlsl";
        private const string SurfaceHlslPath = "Assets/Art/Shader/Particles/ParticleDistortion/HLSL/ParticleDistortion_Surface.hlsl";
        private const string ForwardPassHlslPath = "Assets/Art/Shader/Particles/ParticleDistortion/HLSL/Passes/ParticleDistortion_ForwardPass.hlsl";
        private const string ScenePath = "Assets/Scenes/Particles/ParticleMaterialTestScene.unity";
        private const string DistortionTexturePath = "Assets/Art/Textures/Particles/Distortion/T_Particle_DistortionVector.png";
        private const string NoiseTexturePath = "Assets/Art/Textures/Particles/Distortion/T_Particle_DistortionNoise.png";
        private const string HeatHazeMaterialPath = "Assets/Art/Materials/Particles/Distortion/M_Particle_HeatHaze_Distortion.mat";
        private const string AirWarpMaterialPath = "Assets/Art/Materials/Particles/Distortion/M_Particle_AirWarp_Distortion.mat";
        private const string MagicWarpMaterialPath = "Assets/Art/Materials/Particles/Distortion/M_Particle_MagicWarp_Distortion.mat";
        private const string HeatHazePrefabPath = "Assets/Art/Prefab/Particles/Distortion/FX_Particle_HeatHaze.prefab";
        private const string AirWarpPrefabPath = "Assets/Art/Prefab/Particles/Distortion/FX_Particle_AirWarp.prefab";
        private const string MagicWarpPrefabPath = "Assets/Art/Prefab/Particles/Distortion/FX_Particle_MagicWarp.prefab";
        private const string HeatHazeObjectName = "ParticleDistortion_HeatHazeValidation";
        private const string AirWarpObjectName = "ParticleDistortion_AirWarpValidation";
        private const string MagicWarpObjectName = "ParticleDistortion_MagicWarpValidation";
        private const string ReferenceWallObjectName = "ParticleDistortion_ReferenceWall";
        private const string HeatHazePreviewObjectName = "FX_Particle_HeatHaze_Preview";
        private const string AirWarpPreviewObjectName = "FX_Particle_AirWarp_Preview";
        private const string MagicWarpPreviewObjectName = "FX_Particle_MagicWarp_Preview";

        [OneTimeSetUp]
        public void EnsureValidationAssets()
        {
            Type bootstrapperType = GetEditorAssemblyType("BC.Rendering.ParticleUnlitValidationBootstrapper");
            MethodInfo bootstrapMethod = GetStaticMethod(bootstrapperType, "BootstrapM13ParticleDistortionValidationAssets");
            bootstrapMethod.Invoke(null, null);
            AssetDatabase.Refresh();
        }

        [Test]
        public void SourceFilesExistForM13ParticleDistortion()
        {
            Assert.IsTrue(File.Exists(ShaderPath), $"Expected ParticleDistortion shader: {ShaderPath}");
            Assert.IsTrue(File.Exists(BootstrapperPath), $"Expected validation bootstrapper: {BootstrapperPath}");
            Assert.IsTrue(File.Exists(BuildValidatorPath), $"Expected build validator: {BuildValidatorPath}");
            Assert.IsTrue(File.Exists(WebGlBuildUtilityPath), $"Expected WebGL build utility: {WebGlBuildUtilityPath}");
            Assert.IsTrue(File.Exists(ShaderGuiPath), $"Expected ParticleDistortion ShaderGUI: {ShaderGuiPath}");
            Assert.IsTrue(File.Exists(ValidatorPath), $"Expected ParticleDistortion validator: {ValidatorPath}");
            Assert.IsTrue(File.Exists(PresetUtilityPath), $"Expected ParticleDistortion preset utility: {PresetUtilityPath}");
            Assert.IsTrue(File.Exists(InputHlslPath), $"Expected ParticleDistortion input HLSL: {InputHlslPath}");
            Assert.IsTrue(File.Exists(CommonHlslPath), $"Expected ParticleDistortion common HLSL: {CommonHlslPath}");
            Assert.IsTrue(File.Exists(SamplingHlslPath), $"Expected ParticleDistortion sampling HLSL: {SamplingHlslPath}");
            Assert.IsTrue(File.Exists(SurfaceHlslPath), $"Expected ParticleDistortion surface HLSL: {SurfaceHlslPath}");
            Assert.IsTrue(File.Exists(ForwardPassHlslPath), $"Expected ParticleDistortion forward pass HLSL: {ForwardPassHlslPath}");
        }

        [Test]
        public void ShaderSourceKeepsMinimalDistortionContract()
        {
            string shaderSource = File.ReadAllText(ShaderPath);
            string commonSource = File.ReadAllText(CommonHlslPath);
            string samplingSource = File.ReadAllText(SamplingHlslPath);
            string surfaceSource = File.ReadAllText(SurfaceHlslPath);
            string passSource = File.ReadAllText(ForwardPassHlslPath);
            string allSource = shaderSource + commonSource + samplingSource + surfaceSource + passSource;

            StringAssert.Contains("Shader \"BC/Particles/ParticleDistortion\"", shaderSource);
            StringAssert.Contains("_DistortionMap", shaderSource);
            StringAssert.Contains("_DistortionStrength", shaderSource);
            StringAssert.Contains("_DistortionScale", shaderSource);
            StringAssert.Contains("_DistortionScrollSpeed", shaderSource);
            StringAssert.Contains("_NoiseMap", shaderSource);
            StringAssert.Contains("CustomEditor \"BC.Rendering.ParticleDistortionShaderGUI\"", shaderSource);
            StringAssert.Contains("BC_ParticleDistortionBuildEdgeFade", commonSource);
            StringAssert.Contains("BC_ParticleDistortionSampleDistortion", samplingSource);
            StringAssert.Contains("BC_ParticleDistortionBuildSurfaceColor", surfaceSource);
            StringAssert.Contains("DeclareOpaqueTexture.hlsl", passSource);
            StringAssert.Contains("SampleSceneColor", surfaceSource);
            StringAssert.Contains("GetNormalizedScreenSpaceUV", surfaceSource);
            StringAssert.Contains("ParticleDistortionVertex", passSource);
            StringAssert.Contains("ParticleDistortionFragment", passSource);
            StringAssert.DoesNotContain("GrabPass", allSource);
            StringAssert.DoesNotContain("_CameraDepthTexture", allSource);
            StringAssert.DoesNotContain("FlowMap", allSource);
        }

        [Test]
        public void ShaderGuiTypeExistsForCustomEditorBinding()
        {
            Type shaderGuiType = GetEditorAssemblyType("BC.Rendering.ParticleDistortionShaderGUI");
            Assert.IsTrue(typeof(ShaderGUI).IsAssignableFrom(shaderGuiType), "ParticleDistortion ShaderGUI must derive from ShaderGUI.");
        }

        [Test]
        public void BootstrapperSourceDefinesM13ParticleDistortionContract()
        {
            string bootstrapperSource = File.ReadAllText(BootstrapperPath);

            StringAssert.Contains("BootstrapM13ParticleDistortionValidationAssets", bootstrapperSource);
            StringAssert.Contains("ParticleDistortionShaderPath", bootstrapperSource);
            StringAssert.Contains("EnsureParticleDistortionVectorTexture", bootstrapperSource);
            StringAssert.Contains("EnsureParticleDistortionNoiseTexture", bootstrapperSource);
            StringAssert.Contains("EnsureParticleDistortionValidationMaterial", bootstrapperSource);
            StringAssert.Contains("EnsureM13ParticleDistortionValidationScene", bootstrapperSource);
            StringAssert.Contains("Future Distortion Test Area", bootstrapperSource);
            StringAssert.Contains("EnsureParticleDistortionReferenceWall", bootstrapperSource);
            StringAssert.Contains("EnsureParticleDistortionCameraRequirements", bootstrapperSource);
            StringAssert.Contains("ConfigureHeatHazeDistortionPrefabParticle", bootstrapperSource);
            StringAssert.Contains("ConfigureAirWarpDistortionPrefabParticle", bootstrapperSource);
            StringAssert.Contains("ConfigureMagicWarpDistortionPrefabParticle", bootstrapperSource);
        }

        [Test]
        public void BuildValidationSourceKeepsM13OutOfRequiredWebGlContract()
        {
            string buildValidatorSource = File.ReadAllText(BuildValidatorPath);
            string buildUtilitySource = File.ReadAllText(WebGlBuildUtilityPath);

            StringAssert.DoesNotContain("BootstrapM13ParticleDistortionValidationAssets", buildValidatorSource);
            StringAssert.DoesNotContain("M_Particle_HeatHaze_Distortion.mat", buildValidatorSource);
            StringAssert.DoesNotContain("M_Particle_AirWarp_Distortion.mat", buildValidatorSource);
            StringAssert.DoesNotContain("M_Particle_MagicWarp_Distortion.mat", buildValidatorSource);
            StringAssert.DoesNotContain("BootstrapM13ParticleDistortionValidationAssets", buildUtilitySource);
            StringAssert.DoesNotContain("RunM13WebGlValidationBuild", buildUtilitySource);
        }

        [Test]
        public void ValidatorNormalizesDistortionValuesAndReportsWarnings()
        {
            Material material = CreateTemporaryParticleDistortionMaterial();

            try
            {
                Type validatorType = GetEditorAssemblyType("BC.Rendering.ParticleDistortionMaterialValidator");
                MethodInfo normalizeMethod = GetStaticMethod(validatorType, "Normalize", typeof(Material));
                MethodInfo opaqueWarningMethod = GetStaticMethod(validatorType, "TryGetOpaqueTextureDependencyWarning", typeof(Material), typeof(string).MakeByRefType());
                MethodInfo performanceWarningMethod = GetStaticMethod(validatorType, "TryGetPerformanceWarning", typeof(Material), typeof(string).MakeByRefType());

                material.SetFloat("_BlendMode", 8f);
                material.SetFloat("_Cull", -3f);
                material.SetFloat("_ZTest", 99f);
                material.SetFloat("_DistortionStrength", 4f);
                material.SetFloat("_DistortionScale", -1f);
                material.SetFloat("_Alpha", 4f);
                material.SetFloat("_UseVertexColor", 3f);
                material.SetFloat("_EdgeFadePower", 0f);
                material.SetFloat("_EdgeFadeStrength", 3f);
                material.SetFloat("_NoiseStrength", 2f);
                material.SetFloat("_NoiseScale", -4f);
                material.SetFloat("_QueueOffset", 80f);

                Assert.IsTrue((bool)normalizeMethod.Invoke(null, new object[] { material }));
                Assert.AreEqual(2f, material.GetFloat("_BlendMode"));
                Assert.AreEqual(0f, material.GetFloat("_Cull"));
                Assert.AreEqual(8f, material.GetFloat("_ZTest"));
                Assert.AreEqual(2f, material.GetFloat("_DistortionStrength"));
                Assert.AreEqual(0.01f, material.GetFloat("_DistortionScale"));
                Assert.AreEqual(1f, material.GetFloat("_Alpha"));
                Assert.AreEqual(1f, material.GetFloat("_UseVertexColor"));
                Assert.AreEqual(0.1f, material.GetFloat("_EdgeFadePower"));
                Assert.AreEqual(1f, material.GetFloat("_EdgeFadeStrength"));
                Assert.AreEqual(1f, material.GetFloat("_NoiseStrength"));
                Assert.AreEqual(0.01f, material.GetFloat("_NoiseScale"));
                Assert.AreEqual(50f, material.GetFloat("_QueueOffset"));
                Assert.AreEqual((float)BlendMode.One, material.GetFloat("_SrcBlend"));
                Assert.AreEqual((float)BlendMode.OneMinusSrcAlpha, material.GetFloat("_DstBlend"));
                Assert.AreEqual((int)RenderQueue.Transparent + 50, material.renderQueue);
                Assert.IsEmpty(material.shaderKeywords, "ParticleDistortion validation should not leave shader keywords behind.");

                object[] opaqueWarningArguments = { material, null };
                Assert.IsTrue((bool)opaqueWarningMethod.Invoke(null, opaqueWarningArguments));
                StringAssert.Contains("Opaque Texture", (string)opaqueWarningArguments[1]);

                object[] performanceWarningArguments = { material, null };
                Assert.IsTrue((bool)performanceWarningMethod.Invoke(null, performanceWarningArguments));
                StringAssert.Contains("full-screen", (string)performanceWarningArguments[1]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }

            Shader foreignShader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            if (foreignShader == null)
            {
                Assert.Inconclusive("No non-ParticleDistortion shader was available for validator isolation testing.");
            }

            Material foreignMaterial = new Material(foreignShader);
            try
            {
                foreignMaterial.renderQueue = 2460;
                Type validatorType = GetEditorAssemblyType("BC.Rendering.ParticleDistortionMaterialValidator");
                MethodInfo normalizeMethod = GetStaticMethod(validatorType, "Normalize", typeof(Material));
                Assert.AreEqual(false, normalizeMethod.Invoke(null, new object[] { foreignMaterial }));
                Assert.AreEqual(2460, foreignMaterial.renderQueue);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(foreignMaterial);
            }
        }

        [Test]
        public void PresetUtilityDeclaresThreePresetsAndAppliesDeterministically()
        {
            Material carryOverMaterial = CreateTemporaryParticleDistortionMaterial();
            Material freshMaterial = CreateTemporaryParticleDistortionMaterial();

            try
            {
                Type presetUtilityType = GetEditorAssemblyType("BC.Rendering.ParticleDistortionPresetUtility");
                MethodInfo getPresetNamesMethod = GetStaticMethod(presetUtilityType, "GetPresetNames");
                MethodInfo applyPresetMethod = GetStaticMethod(presetUtilityType, "ApplyPreset", typeof(Material), typeof(int));

                string[] presetNames = (string[])getPresetNamesMethod.Invoke(null, null);
                CollectionAssert.AreEqual(new[] { "HeatHaze", "AirWarp", "MagicWarp" }, presetNames);

                applyPresetMethod.Invoke(null, new object[] { carryOverMaterial, 0 });
                float heatHazeStrength = carryOverMaterial.GetFloat("_DistortionStrength");
                applyPresetMethod.Invoke(null, new object[] { carryOverMaterial, 2 });
                applyPresetMethod.Invoke(null, new object[] { freshMaterial, 2 });

                Assert.AreEqual(0.08f, heatHazeStrength, 0.0001f);
                Assert.AreEqual(freshMaterial.GetFloat("_BlendMode"), carryOverMaterial.GetFloat("_BlendMode"));
                Assert.AreEqual(freshMaterial.GetFloat("_DistortionStrength"), carryOverMaterial.GetFloat("_DistortionStrength"));
                Assert.AreEqual(freshMaterial.GetFloat("_DistortionScale"), carryOverMaterial.GetFloat("_DistortionScale"));
                Assert.AreEqual(freshMaterial.GetFloat("_Alpha"), carryOverMaterial.GetFloat("_Alpha"));
                Assert.AreEqual(freshMaterial.GetVector("_DistortionScrollSpeed"), carryOverMaterial.GetVector("_DistortionScrollSpeed"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(carryOverMaterial);
                UnityEngine.Object.DestroyImmediate(freshMaterial);
            }
        }

        [Test]
        public void GeneratedDistortionMaterialsUseExpectedShaderTexturesAndPresetStates()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Texture2D distortionTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(DistortionTexturePath);
            Texture2D noiseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(NoiseTexturePath);

            Assert.IsNotNull(shader, "ParticleDistortion shader must load.");
            Assert.IsNotNull(distortionTexture, $"Expected generated ParticleDistortion vector texture: {DistortionTexturePath}");
            Assert.IsNotNull(noiseTexture, $"Expected generated ParticleDistortion noise texture: {NoiseTexturePath}");

            AssertDistortionMaterial(HeatHazeMaterialPath, shader, distortionTexture, noiseTexture, 0f, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha, 0.08f, 0.34f);
            AssertDistortionMaterial(AirWarpMaterialPath, shader, distortionTexture, noiseTexture, 0f, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha, 0.13f, 0.48f);
            AssertDistortionMaterial(MagicWarpMaterialPath, shader, distortionTexture, noiseTexture, 2f, BlendMode.One, BlendMode.OneMinusSrcAlpha, 0.22f, 0.58f);
        }

        [Test]
        public void GeneratedTexturesUseExpectedImporterSettings()
        {
            TextureImporter distortionImporter = AssetImporter.GetAtPath(DistortionTexturePath) as TextureImporter;
            TextureImporter noiseImporter = AssetImporter.GetAtPath(NoiseTexturePath) as TextureImporter;

            Assert.IsNotNull(distortionImporter, $"Expected importer for ParticleDistortion vector texture: {DistortionTexturePath}");
            Assert.AreEqual(TextureImporterType.Default, distortionImporter.textureType);
            Assert.IsFalse(distortionImporter.sRGBTexture);
            Assert.AreEqual(TextureWrapMode.Repeat, distortionImporter.wrapMode);
            Assert.IsFalse(distortionImporter.mipmapEnabled);

            Assert.IsNotNull(noiseImporter, $"Expected importer for ParticleDistortion noise texture: {NoiseTexturePath}");
            Assert.AreEqual(TextureImporterType.Default, noiseImporter.textureType);
            Assert.IsFalse(noiseImporter.sRGBTexture);
            Assert.AreEqual(TextureWrapMode.Repeat, noiseImporter.wrapMode);
            Assert.IsFalse(noiseImporter.mipmapEnabled);
        }

        [Test]
        public void GeneratedPrefabsKeepExpectedMaterials()
        {
            AssertPrefabMaterial(HeatHazePrefabPath, HeatHazeMaterialPath);
            AssertPrefabMaterial(AirWarpPrefabPath, AirWarpMaterialPath);
            AssertPrefabMaterial(MagicWarpPrefabPath, MagicWarpMaterialPath);
        }

        [Test]
        public void ValidationSceneContainsFutureDistortionAnchorsAndPreviewObjects()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject heatHazeObject = GameObject.Find(HeatHazeObjectName);
            GameObject airWarpObject = GameObject.Find(AirWarpObjectName);
            GameObject magicWarpObject = GameObject.Find(MagicWarpObjectName);
            GameObject referenceWallObject = GameObject.Find(ReferenceWallObjectName);
            GameObject heatHazePreviewObject = GameObject.Find(HeatHazePreviewObjectName);
            GameObject airWarpPreviewObject = GameObject.Find(AirWarpPreviewObjectName);
            GameObject magicWarpPreviewObject = GameObject.Find(MagicWarpPreviewObjectName);

            Assert.IsNotNull(heatHazeObject, $"Expected validation anchor: {HeatHazeObjectName}");
            Assert.IsNotNull(airWarpObject, $"Expected validation anchor: {AirWarpObjectName}");
            Assert.IsNotNull(magicWarpObject, $"Expected validation anchor: {MagicWarpObjectName}");
            Assert.IsNotNull(referenceWallObject, $"Expected reference object: {ReferenceWallObjectName}");
            Assert.IsNotNull(heatHazePreviewObject, $"Expected preview object: {HeatHazePreviewObjectName}");
            Assert.IsNotNull(airWarpPreviewObject, $"Expected preview object: {AirWarpPreviewObjectName}");
            Assert.IsNotNull(magicWarpPreviewObject, $"Expected preview object: {MagicWarpPreviewObjectName}");

            AssertParticleRendererMode(heatHazeObject, ParticleSystemRenderMode.Billboard);
            AssertParticleRendererMode(airWarpObject, ParticleSystemRenderMode.Billboard);
            AssertParticleRendererMode(magicWarpObject, ParticleSystemRenderMode.Billboard);

            Assert.IsNotNull(PrefabUtility.GetCorrespondingObjectFromSource(heatHazePreviewObject), "HeatHaze preview should keep prefab linkage.");
            Assert.IsNotNull(PrefabUtility.GetCorrespondingObjectFromSource(airWarpPreviewObject), "AirWarp preview should keep prefab linkage.");
            Assert.IsNotNull(PrefabUtility.GetCorrespondingObjectFromSource(magicWarpPreviewObject), "MagicWarp preview should keep prefab linkage.");

            AssertParticleDistortionCameraContract();
        }

        private static void AssertParticleDistortionCameraContract()
        {
            GameObject cameraObject = GameObject.Find("Main Camera");
            Assert.IsNotNull(cameraObject, "Expected Main Camera in ParticleDistortion validation scene.");

            Type cameraDataType = Type.GetType(UniversalAdditionalCameraDataTypeName);
            Assert.IsNotNull(cameraDataType, "UniversalAdditionalCameraData type must resolve for ParticleDistortion validation.");

            Component cameraData = cameraObject.GetComponent(cameraDataType);
            Assert.IsNotNull(cameraData, "ParticleDistortion validation camera must have UniversalAdditionalCameraData.");

            PropertyInfo requiresColorTextureProperty = cameraDataType.GetProperty("requiresColorTexture", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(requiresColorTextureProperty, "UniversalAdditionalCameraData.requiresColorTexture must exist.");
            Assert.IsTrue((bool)requiresColorTextureProperty.GetValue(cameraData), "ParticleDistortion validation camera must force Opaque Texture ON.");
        }

        private static Material CreateTemporaryParticleDistortionMaterial()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Assert.IsNotNull(shader, "ParticleDistortion shader asset must load.");
            return new Material(shader);
        }

        private static void AssertDistortionMaterial(string materialPath, Shader shader, Texture2D distortionTexture, Texture2D noiseTexture, float blendMode, BlendMode srcBlend, BlendMode dstBlend, float distortionStrength, float alpha)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Assert.IsNotNull(material, $"Expected generated ParticleDistortion material: {materialPath}");
            Assert.AreSame(shader, material.shader);
            Assert.AreSame(distortionTexture, material.GetTexture("_DistortionMap"));
            Assert.AreSame(noiseTexture, material.GetTexture("_NoiseMap"));
            Assert.AreEqual(blendMode, material.GetFloat("_BlendMode"));
            Assert.AreEqual((float)srcBlend, material.GetFloat("_SrcBlend"));
            Assert.AreEqual((float)dstBlend, material.GetFloat("_DstBlend"));
            Assert.AreEqual(distortionStrength, material.GetFloat("_DistortionStrength"), 0.0001f);
            Assert.AreEqual(alpha, material.GetFloat("_Alpha"), 0.0001f);
            Assert.AreEqual(0f, material.GetFloat("_Cull"));
            Assert.AreEqual((float)CompareFunction.LessEqual, material.GetFloat("_ZTest"));
            Assert.AreEqual(0f, material.GetFloat("_ZWrite"));
            Assert.AreEqual(15f, material.GetFloat("_ColorMask"));
        }

        private static void AssertPrefabMaterial(string prefabPath, string materialPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            Assert.IsNotNull(prefab, $"Expected generated ParticleDistortion prefab: {prefabPath}");
            Assert.IsNotNull(material, $"Expected generated ParticleDistortion material: {materialPath}");

            ParticleSystemRenderer renderer = prefab.GetComponent<ParticleSystemRenderer>();
            Assert.IsNotNull(renderer, $"Expected ParticleSystemRenderer on prefab: {prefabPath}");
            Assert.AreSame(material, renderer.sharedMaterial);
        }

        private static void AssertParticleRendererMode(GameObject particleObject, ParticleSystemRenderMode expectedMode)
        {
            ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();
            Assert.IsNotNull(renderer, $"Expected ParticleSystemRenderer on {particleObject.name}");
            Assert.AreEqual(expectedMode, renderer.renderMode);
        }

        private static Type GetEditorAssemblyType(string fullTypeName)
        {
            Type type = null;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullTypeName);
                if (type != null)
                {
                    break;
                }
            }

            Assert.IsNotNull(type, $"Expected editor type: {fullTypeName}");
            return type;
        }

        private static MethodInfo GetStaticMethod(Type declaringType, string methodName, params Type[] parameterTypes)
        {
            MethodInfo method = parameterTypes.Length == 0
                ? declaringType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                : declaringType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, parameterTypes, null);

            Assert.IsNotNull(method, $"Expected static method: {declaringType.FullName}.{methodName}");
            return method;
        }
    }
}