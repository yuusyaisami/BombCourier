using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BC.Rendering.Tests
{
    public sealed class TrailUnlitValidationTests
    {
        private const string ShaderPath = "Assets/Art/Shader/Particles/TrailUnlit/BC_Particles_TrailUnlit.shader";
        private const string SpecPath = "Assets/Docs/ParticleTrailMaterialSpec.md";
        private const string ShaderGuiPath = "Assets/Art/Shader/Particles/TrailUnlit/Editor/TrailUnlitShaderGUI.cs";
        private const string ValidatorPath = "Assets/Art/Shader/Particles/TrailUnlit/Editor/TrailUnlitMaterialValidator.cs";
        private const string PresetUtilityPath = "Assets/Art/Shader/Particles/TrailUnlit/Editor/TrailUnlitPresetUtility.cs";
        private const string BootstrapperPath = "Assets/Art/Shader/Particles/TrailUnlit/Editor/TrailUnlitValidationBootstrapper.cs";

        private const string InputHlslPath = "Assets/Art/Shader/Particles/TrailUnlit/HLSL/TrailUnlit_Input.hlsl";
        private const string CommonHlslPath = "Assets/Art/Shader/Particles/TrailUnlit/HLSL/TrailUnlit_Common.hlsl";
        private const string SamplingHlslPath = "Assets/Art/Shader/Particles/TrailUnlit/HLSL/TrailUnlit_Sampling.hlsl";
        private const string SurfaceHlslPath = "Assets/Art/Shader/Particles/TrailUnlit/HLSL/TrailUnlit_Surface.hlsl";
        private const string ForwardPassHlslPath = "Assets/Art/Shader/Particles/TrailUnlit/HLSL/Passes/TrailUnlit_ForwardPass.hlsl";

        private const string SoftLineTexturePath = "Assets/Art/Textures/Particles/Trails/T_Trail_SoftLine.png";
        private const string DustLineTexturePath = "Assets/Art/Textures/Particles/Trails/T_Trail_DustNoiseLine.png";
        private const string WindStreakTexturePath = "Assets/Art/Textures/Particles/Trails/T_Trail_WindStreak.png";
        private const string LightBeamTexturePath = "Assets/Art/Textures/Particles/Trails/T_Trail_LightBeam.png";
        private const string SoftCloudNoisePath = "Assets/Art/Textures/Particles/Trails/T_Noise_SoftCloud.png";
        private const string StreakNoisePath = "Assets/Art/Textures/Particles/Trails/T_Noise_Streak.png";
        private const string DissolveNoisePath = "Assets/Art/Textures/Particles/Trails/T_Noise_Dissolve.png";

        private const string DustMaterialPath = "Assets/Art/Materials/Particles/Trails/M_Trail_Dust_Alpha.mat";
        private const string WindMaterialPath = "Assets/Art/Materials/Particles/Trails/M_Trail_Wind_AlphaScroll.mat";
        private const string LightBeamMaterialPath = "Assets/Art/Materials/Particles/Trails/M_Trail_Light_Additive.mat";
        private const string MagicMaterialPath = "Assets/Art/Materials/Particles/Trails/M_Trail_Magic_Additive.mat";
        private const string SmokeMaterialPath = "Assets/Art/Materials/Particles/Trails/M_Trail_Smoke_Alpha.mat";
        private const string SpeedLineMaterialPath = "Assets/Art/Materials/Particles/Trails/M_Trail_SpeedLine_Alpha.mat";

        private const string DustPrefabPath = "Assets/Art/Prefab/Particles/Trails/FX_Dust_Trail.prefab";
        private const string WindPrefabPath = "Assets/Art/Prefab/Particles/Trails/FX_Wind_Trail.prefab";
        private const string LightBeamPrefabPath = "Assets/Art/Prefab/Particles/Trails/FX_LightBeam_Trail.prefab";
        private const string WindLinePrefabPath = "Assets/Art/Prefab/Particles/Trails/FX_Wind_LineRenderer.prefab";
        private const string LightBeamTrailRendererPrefabPath = "Assets/Art/Prefab/Particles/Trails/FX_LightBeam_TrailRenderer.prefab";

        [Test]
        public void SourceFilesAndSpecExist()
        {
            Assert.IsTrue(File.Exists(ShaderPath), $"Expected TrailUnlit shader: {ShaderPath}");
            Assert.IsTrue(File.Exists(SpecPath), $"Expected Particle Trail spec: {SpecPath}");
            Assert.IsTrue(File.Exists(ShaderGuiPath), $"Expected TrailUnlit ShaderGUI: {ShaderGuiPath}");
            Assert.IsTrue(File.Exists(ValidatorPath), $"Expected TrailUnlit validator: {ValidatorPath}");
            Assert.IsTrue(File.Exists(PresetUtilityPath), $"Expected TrailUnlit preset utility: {PresetUtilityPath}");
            Assert.IsTrue(File.Exists(BootstrapperPath), $"Expected TrailUnlit bootstrapper: {BootstrapperPath}");
            Assert.IsTrue(File.Exists(InputHlslPath), $"Expected input HLSL: {InputHlslPath}");
            Assert.IsTrue(File.Exists(CommonHlslPath), $"Expected common HLSL: {CommonHlslPath}");
            Assert.IsTrue(File.Exists(SamplingHlslPath), $"Expected sampling HLSL: {SamplingHlslPath}");
            Assert.IsTrue(File.Exists(SurfaceHlslPath), $"Expected surface HLSL: {SurfaceHlslPath}");
            Assert.IsTrue(File.Exists(ForwardPassHlslPath), $"Expected forward pass HLSL: {ForwardPassHlslPath}");
        }

        [Test]
        public void ShaderSourceKeepsWebGLLeanContract()
        {
            string shaderSource = File.ReadAllText(ShaderPath);
            string passSource = File.ReadAllText(ForwardPassHlslPath);
            string surfaceSource = File.ReadAllText(SurfaceHlslPath);
            // WebGL向けの軽量契約はinclude側で破られることもあるため、主要ソースをまとめて監査する。
            string allShaderSource = shaderSource + passSource + surfaceSource;

            StringAssert.Contains("Shader \"BC/Particles/TrailUnlit\"", shaderSource);
            StringAssert.Contains("#pragma target 2.0", shaderSource);
            StringAssert.Contains("#pragma multi_compile_instancing", shaderSource);
            StringAssert.Contains("TrailUnlit_Input.hlsl", passSource);
            StringAssert.Contains("TrailUnlit_Common.hlsl", passSource);
            StringAssert.Contains("TrailUnlit_Sampling.hlsl", passSource);
            StringAssert.Contains("TrailUnlit_Surface.hlsl", passSource);
            StringAssert.Contains("BC_TrailUsesPremultiply", surfaceSource);
            StringAssert.DoesNotContain("shader_feature", allShaderSource);
            StringAssert.DoesNotContain("_CameraDepthTexture", allShaderSource);
            StringAssert.DoesNotContain("_CameraOpaqueTexture", allShaderSource);
            StringAssert.DoesNotContain("GrabPass", allShaderSource);
            StringAssert.DoesNotContain("ComputeShader", allShaderSource);
        }

        [Test]
        public void GeneratedMaterialsUseTrailShaderAndExpectedBlendStates()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Assert.IsNotNull(shader, "TrailUnlit shader must load.");

            AssertMaterialBlendState(DustMaterialPath, shader, 0f, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha);
            AssertMaterialBlendState(WindMaterialPath, shader, 0f, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha);
            AssertMaterialBlendState(LightBeamMaterialPath, shader, 1f, BlendMode.SrcAlpha, BlendMode.One);
            AssertMaterialBlendState(MagicMaterialPath, shader, 1f, BlendMode.SrcAlpha, BlendMode.One);
            AssertMaterialBlendState(SmokeMaterialPath, shader, 0f, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha);
            AssertMaterialBlendState(SpeedLineMaterialPath, shader, 0f, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha);
        }

        [Test]
        public void PresetUtilityNormalizesAllBlendModes()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Assert.IsNotNull(shader, "TrailUnlit shader must load.");

            Material temporaryMaterial = new Material(shader);
            try
            {
                Type presetUtilityType = GetEditorAssemblyType("BC.Rendering.TrailUnlitPresetUtility");
                MethodInfo applyPresetMethod = GetStaticMethod(presetUtilityType, "ApplyPreset", typeof(Material), typeof(string));

                applyPresetMethod.Invoke(null, new object[] { temporaryMaterial, "Dust" });
                Assert.AreEqual((float)BlendMode.SrcAlpha, temporaryMaterial.GetFloat("_SrcBlend"));
                Assert.AreEqual((float)BlendMode.OneMinusSrcAlpha, temporaryMaterial.GetFloat("_DstBlend"));

                applyPresetMethod.Invoke(null, new object[] { temporaryMaterial, "LightBeam" });
                Assert.AreEqual((float)BlendMode.SrcAlpha, temporaryMaterial.GetFloat("_SrcBlend"));
                Assert.AreEqual((float)BlendMode.One, temporaryMaterial.GetFloat("_DstBlend"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporaryMaterial);
            }
        }

        [Test]
        public void ValidatorSupportsPremultiplyAndMultiplyWithoutShaderKeywords()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Assert.IsNotNull(shader, "TrailUnlit shader must load.");

            Material temporaryMaterial = new Material(shader);
            try
            {
                Type validatorType = GetEditorAssemblyType("BC.Rendering.TrailUnlitMaterialValidator");
                MethodInfo normalizeMethod = GetStaticMethod(validatorType, "Normalize", typeof(Material));

                temporaryMaterial.SetFloat("_BlendMode", 2f);
                normalizeMethod.Invoke(null, new object[] { temporaryMaterial });
                Assert.AreEqual((float)BlendMode.One, temporaryMaterial.GetFloat("_SrcBlend"));
                Assert.AreEqual((float)BlendMode.OneMinusSrcAlpha, temporaryMaterial.GetFloat("_DstBlend"));

                temporaryMaterial.SetFloat("_BlendMode", 3f);
                normalizeMethod.Invoke(null, new object[] { temporaryMaterial });
                Assert.AreEqual((float)BlendMode.DstColor, temporaryMaterial.GetFloat("_SrcBlend"));
                Assert.AreEqual((float)BlendMode.OneMinusSrcAlpha, temporaryMaterial.GetFloat("_DstBlend"));
                Assert.IsFalse(temporaryMaterial.shaderKeywords.Length > 0, "TrailUnlit blend normalization should not add shader keywords.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporaryMaterial);
            }
        }

        [Test]
        public void ValidatorIgnoresNonTrailMaterials()
        {
            // ValidatorはEditorから直接呼ばれる可能性があるので、対象Shader以外を変更しないことを固定する。
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            if (shader == null)
            {
                Assert.Inconclusive("No built-in non-Trail shader was available for validator isolation testing.");
            }

            Material temporaryMaterial = new Material(shader);
            try
            {
                Type validatorType = GetEditorAssemblyType("BC.Rendering.TrailUnlitMaterialValidator");
                MethodInfo normalizeMethod = GetStaticMethod(validatorType, "Normalize", typeof(Material));

                temporaryMaterial.renderQueue = 2450;
                object changed = normalizeMethod.Invoke(null, new object[] { temporaryMaterial });

                Assert.AreEqual(false, changed, "TrailUnlit validator should ignore materials that use other shaders.");
                Assert.AreEqual(2450, temporaryMaterial.renderQueue, "TrailUnlit validator must not rewrite non-Trail render queues.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporaryMaterial);
            }
        }

        [Test]
        public void GeneratedPrefabsUseTrailMaterials()
        {
            AssertPrefabUsesTrailMaterial(DustPrefabPath, DustMaterialPath);
            AssertPrefabUsesTrailMaterial(WindPrefabPath, WindMaterialPath);
            AssertPrefabUsesTrailMaterial(LightBeamPrefabPath, LightBeamMaterialPath);
            AssertLineRendererUsesMaterial(WindLinePrefabPath, WindMaterialPath);
            AssertTrailRendererUsesMaterial(LightBeamTrailRendererPrefabPath, LightBeamMaterialPath);
        }

        [Test]
        public void GeneratedTexturesUseExpectedImportSettings()
        {
            // Base/line textureは色、noise textureはマスク値として読むため、sRGB設定を分けて検証する。
            AssertTextureImportSettings(SoftLineTexturePath, true);
            AssertTextureImportSettings(DustLineTexturePath, true);
            AssertTextureImportSettings(WindStreakTexturePath, true);
            AssertTextureImportSettings(LightBeamTexturePath, true);
            AssertTextureImportSettings(SoftCloudNoisePath, false);
            AssertTextureImportSettings(StreakNoisePath, false);
            AssertTextureImportSettings(DissolveNoisePath, false);
        }

        private static void AssertMaterialBlendState(string materialPath, Shader expectedShader, float expectedBlendMode, BlendMode expectedSourceBlend, BlendMode expectedDestinationBlend)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Assert.IsNotNull(material, $"Expected generated trail material: {materialPath}");
            Assert.AreSame(expectedShader, material.shader, $"Unexpected shader on material: {materialPath}");
            Assert.AreEqual(expectedBlendMode, material.GetFloat("_BlendMode"), $"Unexpected blend mode on material: {materialPath}");
            Assert.AreEqual((float)expectedSourceBlend, material.GetFloat("_SrcBlend"), $"Unexpected source blend on material: {materialPath}");
            Assert.AreEqual((float)expectedDestinationBlend, material.GetFloat("_DstBlend"), $"Unexpected destination blend on material: {materialPath}");
            Assert.AreEqual((int)RenderQueue.Transparent, material.renderQueue, $"Trail material should stay in Transparent queue: {materialPath}");
            Assert.IsNotNull(material.GetTexture("_BaseMap"), $"Expected base texture on material: {materialPath}");
            Assert.IsNotNull(material.GetTexture("_NoiseMap"), $"Expected noise texture on material: {materialPath}");
        }

        private static void AssertPrefabUsesTrailMaterial(string prefabPath, string materialPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Material expectedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Assert.IsNotNull(prefab, $"Expected generated trail prefab: {prefabPath}");
            Assert.IsNotNull(expectedMaterial, $"Expected generated trail material: {materialPath}");

            ParticleSystem particleSystem = prefab.GetComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = prefab.GetComponent<ParticleSystemRenderer>();
            Assert.IsNotNull(particleSystem, $"Expected ParticleSystem on prefab: {prefabPath}");
            Assert.IsNotNull(renderer, $"Expected ParticleSystemRenderer on prefab: {prefabPath}");

            ParticleSystem.TrailModule trails = particleSystem.trails;
            Assert.IsTrue(trails.enabled, $"Expected Trails module enabled on prefab: {prefabPath}");
            Assert.AreSame(expectedMaterial, renderer.trailMaterial, $"Unexpected trail material on prefab: {prefabPath}");
        }

        private static void AssertLineRendererUsesMaterial(string prefabPath, string materialPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Material expectedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Assert.IsNotNull(prefab, $"Expected generated LineRenderer prefab: {prefabPath}");
            Assert.IsNotNull(expectedMaterial, $"Expected generated trail material: {materialPath}");

            LineRenderer lineRenderer = prefab.GetComponent<LineRenderer>();
            Assert.IsNotNull(lineRenderer, $"Expected LineRenderer on prefab: {prefabPath}");
            Assert.AreSame(expectedMaterial, lineRenderer.sharedMaterial, $"Unexpected LineRenderer material on prefab: {prefabPath}");
            Assert.GreaterOrEqual(lineRenderer.positionCount, 2, $"LineRenderer prefab must contain authoring preview points: {prefabPath}");
        }

        private static void AssertTrailRendererUsesMaterial(string prefabPath, string materialPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Material expectedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Assert.IsNotNull(prefab, $"Expected generated TrailRenderer prefab: {prefabPath}");
            Assert.IsNotNull(expectedMaterial, $"Expected generated trail material: {materialPath}");

            TrailRenderer trailRenderer = prefab.GetComponent<TrailRenderer>();
            Assert.IsNotNull(trailRenderer, $"Expected TrailRenderer on prefab: {prefabPath}");
            Assert.AreSame(expectedMaterial, trailRenderer.sharedMaterial, $"Unexpected TrailRenderer material on prefab: {prefabPath}");
            Assert.Greater(trailRenderer.time, 0f, $"TrailRenderer prefab must keep a positive lifetime: {prefabPath}");
        }

        private static void AssertTextureImportSettings(string texturePath, bool expectedSrgb)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            Assert.IsNotNull(texture, $"Expected generated texture: {texturePath}");
            Assert.IsNotNull(importer, $"Expected TextureImporter: {texturePath}");
            Assert.AreEqual(expectedSrgb, importer.sRGBTexture, $"Unexpected sRGB setting: {texturePath}");
            Assert.AreEqual(TextureWrapMode.Repeat, importer.wrapMode, $"Unexpected wrap mode: {texturePath}");
            Assert.AreEqual(FilterMode.Bilinear, importer.filterMode, $"Unexpected filter mode: {texturePath}");
            Assert.IsFalse(importer.mipmapEnabled, $"Trail validation textures should not generate mipmaps: {texturePath}");
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

            Assert.Fail($"Expected editor type: {fullTypeName}");
            return null;
        }

        private static MethodInfo GetStaticMethod(Type declaringType, string methodName, params Type[] parameterTypes)
        {
            MethodInfo method = declaringType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, parameterTypes, null);
            Assert.IsNotNull(method, $"Expected static method: {declaringType.FullName}.{methodName}");
            return method;
        }
    }
}
