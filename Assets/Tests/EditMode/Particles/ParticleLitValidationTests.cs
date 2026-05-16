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
    public sealed class ParticleLitValidationTests
    {
        private const string ShaderPath = "Assets/Art/Shader/Particles/ParticleLit/BC_Particles_ParticleLit.shader";
        private const string BootstrapperPath = "Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs";
        private const string BuildValidatorPath = "Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitBuildValidator.cs";
        private const string WebGlBuildUtilityPath = "Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitWebGlBuildUtility.cs";
        private const string ShaderGuiPath = "Assets/Art/Shader/Particles/ParticleLit/Editor/ParticleLitShaderGUI.cs";
        private const string ValidatorPath = "Assets/Art/Shader/Particles/ParticleLit/Editor/ParticleLitMaterialValidator.cs";
        private const string PresetUtilityPath = "Assets/Art/Shader/Particles/ParticleLit/Editor/ParticleLitPresetUtility.cs";
        private const string InputHlslPath = "Assets/Art/Shader/Particles/ParticleLit/HLSL/ParticleLit_Input.hlsl";
        private const string CommonHlslPath = "Assets/Art/Shader/Particles/ParticleLit/HLSL/ParticleLit_Common.hlsl";
        private const string SamplingHlslPath = "Assets/Art/Shader/Particles/ParticleLit/HLSL/ParticleLit_Sampling.hlsl";
        private const string LightingHlslPath = "Assets/Art/Shader/Particles/ParticleLit/HLSL/ParticleLit_Lighting.hlsl";
        private const string SurfaceHlslPath = "Assets/Art/Shader/Particles/ParticleLit/HLSL/ParticleLit_Surface.hlsl";
        private const string ForwardPassHlslPath = "Assets/Art/Shader/Particles/ParticleLit/HLSL/Passes/ParticleLit_ForwardPass.hlsl";
        private const string ScenePath = "Assets/Scenes/Particles/ParticleMaterialTestScene.unity";
        private const string BaseTexturePath = "Assets/Art/Textures/Particles/Lit/T_Particle_LitSurface.png";
        private const string NormalTexturePath = "Assets/Art/Textures/Particles/Lit/T_Particle_LitNormal.png";
        private const string RaindropMaterialPath = "Assets/Art/Materials/Particles/Lit/M_Particle_Raindrop_Lit.mat";
        private const string BubbleMaterialPath = "Assets/Art/Materials/Particles/Lit/M_Particle_Bubble_Lit.mat";
        private const string DebrisMaterialPath = "Assets/Art/Materials/Particles/Lit/M_Particle_Debris_Lit.mat";
        private const string RaindropObjectName = "ParticleLit_RaindropValidation";
        private const string BubbleObjectName = "ParticleLit_BubbleValidation";
        private const string DebrisObjectName = "ParticleLit_DebrisValidation";
        private const string LightReferenceObjectName = "ParticleLit_LightReference";

        [OneTimeSetUp]
        public void EnsureValidationAssets()
        {
            Type bootstrapperType = GetEditorAssemblyType("BC.Rendering.ParticleUnlitValidationBootstrapper");
            MethodInfo bootstrapMethod = GetStaticMethod(bootstrapperType, "BootstrapM12ParticleLitValidationAssets");
            bootstrapMethod.Invoke(null, null);
            AssetDatabase.Refresh();
        }

        [Test]
        public void SourceFilesExistForM12ParticleLit()
        {
            Assert.IsTrue(File.Exists(ShaderPath), $"Expected ParticleLit shader: {ShaderPath}");
            Assert.IsTrue(File.Exists(BootstrapperPath), $"Expected validation bootstrapper: {BootstrapperPath}");
            Assert.IsTrue(File.Exists(BuildValidatorPath), $"Expected build validator: {BuildValidatorPath}");
            Assert.IsTrue(File.Exists(WebGlBuildUtilityPath), $"Expected WebGL build utility: {WebGlBuildUtilityPath}");
            Assert.IsTrue(File.Exists(ShaderGuiPath), $"Expected ParticleLit ShaderGUI: {ShaderGuiPath}");
            Assert.IsTrue(File.Exists(ValidatorPath), $"Expected ParticleLit validator: {ValidatorPath}");
            Assert.IsTrue(File.Exists(PresetUtilityPath), $"Expected ParticleLit preset utility: {PresetUtilityPath}");
            Assert.IsTrue(File.Exists(InputHlslPath), $"Expected ParticleLit input HLSL: {InputHlslPath}");
            Assert.IsTrue(File.Exists(CommonHlslPath), $"Expected ParticleLit common HLSL: {CommonHlslPath}");
            Assert.IsTrue(File.Exists(SamplingHlslPath), $"Expected ParticleLit sampling HLSL: {SamplingHlslPath}");
            Assert.IsTrue(File.Exists(LightingHlslPath), $"Expected ParticleLit lighting HLSL: {LightingHlslPath}");
            Assert.IsTrue(File.Exists(SurfaceHlslPath), $"Expected ParticleLit surface HLSL: {SurfaceHlslPath}");
            Assert.IsTrue(File.Exists(ForwardPassHlslPath), $"Expected ParticleLit forward pass HLSL: {ForwardPassHlslPath}");
        }

        [Test]
        public void ShaderSourceKeepsMinimalLitContract()
        {
            string shaderSource = File.ReadAllText(ShaderPath);
            string inputSource = File.ReadAllText(InputHlslPath);
            string commonSource = File.ReadAllText(CommonHlslPath);
            string samplingSource = File.ReadAllText(SamplingHlslPath);
            string lightingSource = File.ReadAllText(LightingHlslPath);
            string surfaceSource = File.ReadAllText(SurfaceHlslPath);
            string passSource = File.ReadAllText(ForwardPassHlslPath);
            string allSource = shaderSource + inputSource + commonSource + samplingSource + lightingSource + surfaceSource + passSource;

            StringAssert.Contains("Shader \"BC/Particles/ParticleLit\"", shaderSource);
            StringAssert.Contains("_NormalMap", shaderSource);
            StringAssert.Contains("_NormalScale", shaderSource);
            StringAssert.Contains("_Smoothness", shaderSource);
            StringAssert.Contains("_Metallic", shaderSource);
            StringAssert.Contains("_LightInfluence", shaderSource);
            StringAssert.Contains("_EmissionColor", shaderSource);
            StringAssert.Contains("_EmissionStrength", shaderSource);
            StringAssert.Contains("#pragma multi_compile _ _MAIN_LIGHT_SHADOWS", shaderSource);
            StringAssert.Contains("CustomEditor \"BC.Rendering.ParticleLitShaderGUI\"", shaderSource);
            StringAssert.Contains("BC_ParticleLitBuildFallbackNormalWS", commonSource);
            StringAssert.Contains("BC_ParticleLitBuildNormalWS", commonSource);
            StringAssert.Contains("UnpackNormalScale", samplingSource);
            StringAssert.Contains("GetMainLight()", lightingSource);
            StringAssert.Contains("BC_ParticleLitBuildLightingData", lightingSource);
            StringAssert.Contains("BC_ParticleLitBuildSurfaceColor", surfaceSource);
            StringAssert.Contains("ParticleLitVertex", passSource);
            StringAssert.Contains("ParticleLitFragment", passSource);
            StringAssert.Contains("ParticleSystemRenderMode.Mesh", File.ReadAllText(BootstrapperPath));
            StringAssert.DoesNotContain("shader_feature", allSource);
            StringAssert.DoesNotContain("_ADDITIONAL_LIGHTS", allSource);
            StringAssert.DoesNotContain("_AlphaClip", allSource);
            StringAssert.DoesNotContain("Rim", allSource);
        }

        [Test]
        public void ShaderGuiTypeExistsForCustomEditorBinding()
        {
            Type shaderGuiType = GetEditorAssemblyType("BC.Rendering.ParticleLitShaderGUI");
            Assert.IsTrue(typeof(ShaderGUI).IsAssignableFrom(shaderGuiType), "ParticleLit ShaderGUI must derive from ShaderGUI.");
        }

        [Test]
        public void BootstrapperSourceDefinesM12ParticleLitContract()
        {
            string bootstrapperSource = File.ReadAllText(BootstrapperPath);

            StringAssert.Contains("BootstrapM12ParticleLitValidationAssets", bootstrapperSource);
            StringAssert.Contains("ParticleLitShaderPath", bootstrapperSource);
            StringAssert.Contains("ParticleLitTexturePath", bootstrapperSource);
            StringAssert.Contains("ParticleLitNormalTexturePath", bootstrapperSource);
            StringAssert.Contains("EnsureParticleLitValidationMaterial", bootstrapperSource);
            StringAssert.Contains("EnsureParticleLitBaseTexture", bootstrapperSource);
            StringAssert.Contains("EnsureParticleLitNormalTexture", bootstrapperSource);
            StringAssert.Contains("EnsureM12ParticleLitValidationScene", bootstrapperSource);
            StringAssert.Contains("Future Lit Test Area", bootstrapperSource);
            StringAssert.Contains("ConfigureRaindropLitValidationParticle", bootstrapperSource);
            StringAssert.Contains("ConfigureBubbleLitValidationParticle", bootstrapperSource);
            StringAssert.Contains("ConfigureDebrisLitValidationParticle", bootstrapperSource);
            StringAssert.Contains("renderer.renderMode = ParticleSystemRenderMode.Mesh", bootstrapperSource);
            StringAssert.Contains("renderer.mesh = GetCubeValidationMesh()", bootstrapperSource);
        }

        [Test]
        public void BuildValidationSourceDefinesM12Contract()
        {
            string buildValidatorSource = File.ReadAllText(BuildValidatorPath);
            string buildUtilitySource = File.ReadAllText(WebGlBuildUtilityPath);

            StringAssert.Contains("EnsureM12ValidationAssetsReady", buildValidatorSource);
            StringAssert.Contains("BootstrapM12ParticleLitValidationAssets", buildValidatorSource);
            StringAssert.Contains("Missing M12 validation material:", buildValidatorSource);
            StringAssert.Contains("M_Particle_Raindrop_Lit.mat", buildValidatorSource);
            StringAssert.Contains("M_Particle_Bubble_Lit.mat", buildValidatorSource);
            StringAssert.Contains("M_Particle_Debris_Lit.mat", buildValidatorSource);
            StringAssert.Contains("EnsureM12ValidationAssetsReady", buildUtilitySource);
            StringAssert.Contains("RunM12WebGlValidationBuild", buildUtilitySource);
        }

        [Test]
        public void ValidatorNormalizesLitValuesAndIgnoresForeignShaders()
        {
            Material material = CreateTemporaryParticleLitMaterial();

            try
            {
                Type validatorType = GetEditorAssemblyType("BC.Rendering.ParticleLitMaterialValidator");
                MethodInfo normalizeMethod = GetStaticMethod(validatorType, "Normalize", typeof(Material));

                material.SetFloat("_BlendMode", 8f);
                material.SetFloat("_Cull", -4f);
                material.SetFloat("_ZTest", 99f);
                material.SetFloat("_Alpha", -1f);
                material.SetFloat("_UseVertexColor", 3f);
                material.SetFloat("_NormalScale", 4f);
                material.SetFloat("_Smoothness", 3f);
                material.SetFloat("_Metallic", -2f);
                material.SetFloat("_LightInfluence", 6f);
                material.SetFloat("_EmissionStrength", 12f);
                material.SetFloat("_QueueOffset", 60f);

                Assert.IsTrue((bool)normalizeMethod.Invoke(null, new object[] { material }));
                Assert.AreEqual(2f, material.GetFloat("_BlendMode"));
                Assert.AreEqual(0f, material.GetFloat("_Cull"));
                Assert.AreEqual(8f, material.GetFloat("_ZTest"));
                Assert.AreEqual(0f, material.GetFloat("_Alpha"));
                Assert.AreEqual(1f, material.GetFloat("_UseVertexColor"));
                Assert.AreEqual(2f, material.GetFloat("_NormalScale"));
                Assert.AreEqual(1f, material.GetFloat("_Smoothness"));
                Assert.AreEqual(0f, material.GetFloat("_Metallic"));
                Assert.AreEqual(1f, material.GetFloat("_LightInfluence"));
                Assert.AreEqual(8f, material.GetFloat("_EmissionStrength"));
                Assert.AreEqual(50f, material.GetFloat("_QueueOffset"));
                Assert.AreEqual((float)BlendMode.One, material.GetFloat("_SrcBlend"));
                Assert.AreEqual((float)BlendMode.OneMinusSrcAlpha, material.GetFloat("_DstBlend"));
                Assert.AreEqual((int)RenderQueue.Transparent + 50, material.renderQueue);
                Assert.IsEmpty(material.shaderKeywords, "ParticleLit validation should not leave shader keywords behind.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }

            Shader foreignShader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            if (foreignShader == null)
            {
                Assert.Inconclusive("No non-ParticleLit shader was available for validator isolation testing.");
            }

            Material foreignMaterial = new Material(foreignShader);
            try
            {
                foreignMaterial.renderQueue = 2460;
                Type validatorType = GetEditorAssemblyType("BC.Rendering.ParticleLitMaterialValidator");
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
            Material carryOverMaterial = CreateTemporaryParticleLitMaterial();
            Material freshMaterial = CreateTemporaryParticleLitMaterial();

            try
            {
                Type presetUtilityType = GetEditorAssemblyType("BC.Rendering.ParticleLitPresetUtility");
                MethodInfo getPresetNamesMethod = GetStaticMethod(presetUtilityType, "GetPresetNames");
                MethodInfo applyPresetMethod = GetStaticMethod(presetUtilityType, "ApplyPreset", typeof(Material), typeof(int));

                string[] presetNames = (string[])getPresetNamesMethod.Invoke(null, null);
                CollectionAssert.AreEqual(new[] { "Raindrop", "Bubble", "Debris" }, presetNames);

                applyPresetMethod.Invoke(null, new object[] { carryOverMaterial, 0 });
                float raindropSmoothness = carryOverMaterial.GetFloat("_Smoothness");
                applyPresetMethod.Invoke(null, new object[] { carryOverMaterial, 2 });
                applyPresetMethod.Invoke(null, new object[] { freshMaterial, 2 });

                Assert.AreEqual(raindropSmoothness, 0.82f, 0.0001f);
                Assert.AreEqual(freshMaterial.GetFloat("_BlendMode"), carryOverMaterial.GetFloat("_BlendMode"));
                Assert.AreEqual(freshMaterial.GetFloat("_Smoothness"), carryOverMaterial.GetFloat("_Smoothness"));
                Assert.AreEqual(freshMaterial.GetFloat("_Metallic"), carryOverMaterial.GetFloat("_Metallic"));
                Assert.AreEqual(freshMaterial.GetColor("_BaseColor"), carryOverMaterial.GetColor("_BaseColor"));
                Assert.AreEqual(freshMaterial.GetColor("_EmissionColor"), carryOverMaterial.GetColor("_EmissionColor"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(carryOverMaterial);
                UnityEngine.Object.DestroyImmediate(freshMaterial);
            }
        }

        [Test]
        public void GeneratedLitMaterialsUseExpectedShaderTexturesAndPresetStates()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Texture2D baseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(BaseTexturePath);
            Texture2D normalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalTexturePath);

            Assert.IsNotNull(shader, "ParticleLit shader must load.");
            Assert.IsNotNull(baseTexture, $"Expected generated ParticleLit base texture: {BaseTexturePath}");
            Assert.IsNotNull(normalTexture, $"Expected generated ParticleLit normal texture: {NormalTexturePath}");

            AssertLitMaterial(RaindropMaterialPath, shader, baseTexture, normalTexture, 0f, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha, 0.82f, 0f);
            AssertLitMaterial(BubbleMaterialPath, shader, baseTexture, normalTexture, 2f, BlendMode.One, BlendMode.OneMinusSrcAlpha, 0.92f, 0f);
            AssertLitMaterial(DebrisMaterialPath, shader, baseTexture, normalTexture, 0f, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha, 0.36f, 0.3f);
        }

        [Test]
        public void GeneratedTexturesUseExpectedImporterSettings()
        {
            TextureImporter baseImporter = AssetImporter.GetAtPath(BaseTexturePath) as TextureImporter;
            TextureImporter normalImporter = AssetImporter.GetAtPath(NormalTexturePath) as TextureImporter;

            Assert.IsNotNull(baseImporter, $"Expected importer for ParticleLit base texture: {BaseTexturePath}");
            Assert.AreEqual(TextureImporterType.Default, baseImporter.textureType);
            Assert.IsTrue(baseImporter.sRGBTexture);
            Assert.AreEqual(TextureWrapMode.Clamp, baseImporter.wrapMode);
            Assert.IsFalse(baseImporter.mipmapEnabled);

            Assert.IsNotNull(normalImporter, $"Expected importer for ParticleLit normal texture: {NormalTexturePath}");
            Assert.AreEqual(TextureImporterType.NormalMap, normalImporter.textureType);
            Assert.AreEqual(TextureWrapMode.Repeat, normalImporter.wrapMode);
            Assert.IsFalse(normalImporter.mipmapEnabled);
        }

        [Test]
        public void ValidationSceneContainsFutureLitAnchorsAndExpectedRendererModes()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject raindropObject = GameObject.Find(RaindropObjectName);
            GameObject bubbleObject = GameObject.Find(BubbleObjectName);
            GameObject debrisObject = GameObject.Find(DebrisObjectName);
            GameObject lightReferenceObject = GameObject.Find(LightReferenceObjectName);

            Assert.IsNotNull(raindropObject, $"Expected validation anchor: {RaindropObjectName}");
            Assert.IsNotNull(bubbleObject, $"Expected validation anchor: {BubbleObjectName}");
            Assert.IsNotNull(debrisObject, $"Expected validation anchor: {DebrisObjectName}");
            Assert.IsNotNull(lightReferenceObject, $"Expected reference object: {LightReferenceObjectName}");

            AssertParticleRendererMode(raindropObject, ParticleSystemRenderMode.Billboard, false);
            AssertParticleRendererMode(bubbleObject, ParticleSystemRenderMode.Billboard, false);
            AssertParticleRendererMode(debrisObject, ParticleSystemRenderMode.Mesh, true);
        }

        private static Material CreateTemporaryParticleLitMaterial()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Assert.IsNotNull(shader, "ParticleLit shader asset must load.");
            return new Material(shader);
        }

        private static void AssertLitMaterial(string materialPath, Shader shader, Texture2D baseTexture, Texture2D normalTexture, float blendMode, BlendMode srcBlend, BlendMode dstBlend, float smoothness, float metallic)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Assert.IsNotNull(material, $"Expected generated ParticleLit material: {materialPath}");
            Assert.AreSame(shader, material.shader);
            Assert.AreSame(baseTexture, material.GetTexture("_BaseMap"));
            Assert.AreSame(normalTexture, material.GetTexture("_NormalMap"));
            Assert.AreEqual(blendMode, material.GetFloat("_BlendMode"));
            Assert.AreEqual((float)srcBlend, material.GetFloat("_SrcBlend"));
            Assert.AreEqual((float)dstBlend, material.GetFloat("_DstBlend"));
            Assert.AreEqual(smoothness, material.GetFloat("_Smoothness"), 0.0001f);
            Assert.AreEqual(metallic, material.GetFloat("_Metallic"), 0.0001f);
            Assert.AreEqual(0f, material.GetFloat("_Cull"));
            Assert.AreEqual((float)CompareFunction.LessEqual, material.GetFloat("_ZTest"));
            Assert.AreEqual(0f, material.GetFloat("_ZWrite"));
            Assert.AreEqual(15f, material.GetFloat("_ColorMask"));
        }

        private static void AssertParticleRendererMode(GameObject particleObject, ParticleSystemRenderMode expectedMode, bool requireMesh)
        {
            ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();
            Assert.IsNotNull(renderer, $"Expected ParticleSystemRenderer on {particleObject.name}");
            Assert.AreEqual(expectedMode, renderer.renderMode);
            if (requireMesh)
            {
                Assert.IsNotNull(renderer.mesh, $"Expected mesh assignment on {particleObject.name}");
            }
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