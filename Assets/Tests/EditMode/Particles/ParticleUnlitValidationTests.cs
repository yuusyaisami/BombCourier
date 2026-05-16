using System;
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
        private const string InputHlslPath = "Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Input.hlsl";
        private const string CommonHlslPath = "Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Common.hlsl";
        private const string SamplingHlslPath = "Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Sampling.hlsl";
        private const string SurfaceHlslPath = "Assets/Art/Shader/Particles/ParticleUnlit/HLSL/ParticleUnlit_Surface.hlsl";
        private const string ForwardPassHlslPath = "Assets/Art/Shader/Particles/ParticleUnlit/HLSL/Passes/ParticleUnlit_ForwardPass.hlsl";
        private const string TexturePath = "Assets/Art/Textures/Particles/Unlit/T_Particle_TestSoftSprite.png";
        private const string MaterialPath = "Assets/Art/Materials/Particles/Unlit/M_Particle_Test_Alpha.mat";
        private const string ScenePath = "Assets/Scenes/Particles/ParticleMaterialTestScene.unity";

        [OneTimeSetUp]
        public void EnsureValidationAssets()
        {
            Type bootstrapperType = GetEditorAssemblyType("BC.Rendering.ParticleUnlitValidationBootstrapper");
            MethodInfo bootstrapMethod = GetStaticMethod(bootstrapperType, "BootstrapM2ValidationAssets");
            bootstrapMethod.Invoke(null, null);
            AssetDatabase.Refresh();
        }

        [Test]
        public void SourceFilesAndSpecExist()
        {
            Assert.IsTrue(File.Exists(ShaderPath), $"Expected ParticleUnlit shader: {ShaderPath}");
            Assert.IsTrue(File.Exists(SpecPath), $"Expected Particle Material System spec: {SpecPath}");
            Assert.IsTrue(File.Exists(BootstrapperPath), $"Expected ParticleUnlit bootstrapper: {BootstrapperPath}");
            Assert.IsTrue(File.Exists(InputHlslPath), $"Expected input HLSL: {InputHlslPath}");
            Assert.IsTrue(File.Exists(CommonHlslPath), $"Expected common HLSL: {CommonHlslPath}");
            Assert.IsTrue(File.Exists(SamplingHlslPath), $"Expected sampling HLSL: {SamplingHlslPath}");
            Assert.IsTrue(File.Exists(SurfaceHlslPath), $"Expected surface HLSL: {SurfaceHlslPath}");
            Assert.IsTrue(File.Exists(ForwardPassHlslPath), $"Expected forward pass HLSL: {ForwardPassHlslPath}");
        }

        [Test]
        public void ShaderSourceKeepsMinimalWebGLContract()
        {
            string shaderSource = File.ReadAllText(ShaderPath);
            string passSource = File.ReadAllText(ForwardPassHlslPath);
            string surfaceSource = File.ReadAllText(SurfaceHlslPath);
            string allShaderSource = shaderSource + passSource + surfaceSource;

            StringAssert.Contains("Shader \"BC/Particles/ParticleUnlit\"", shaderSource);
            StringAssert.Contains("#pragma target 2.0", shaderSource);
            StringAssert.Contains("#pragma multi_compile_instancing", shaderSource);
            StringAssert.Contains("ParticleUnlit_Input.hlsl", passSource);
            StringAssert.Contains("ParticleUnlit_Common.hlsl", passSource);
            StringAssert.Contains("ParticleUnlit_Sampling.hlsl", passSource);
            StringAssert.Contains("ParticleUnlit_Surface.hlsl", passSource);
            StringAssert.Contains("BC_ParticleCalculateSoftCircle", surfaceSource + File.ReadAllText(CommonHlslPath));
            StringAssert.DoesNotContain("shader_feature", allShaderSource);
            StringAssert.DoesNotContain("_CameraDepthTexture", allShaderSource);
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
            Assert.AreEqual(1f, material.GetFloat("_UseVertexColor"));
            Assert.AreEqual(1f, material.GetFloat("_SoftCircleStrength"));
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

            var colorOverLifetime = particleSystem.colorOverLifetime;
            Assert.AreEqual(expectLifetimeColor, colorOverLifetime.enabled, $"Unexpected Color over Lifetime state on validation object: {objectName}");
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

        private static MethodInfo GetStaticMethod(Type ownerType, string methodName)
        {
            MethodInfo method = ownerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, $"Expected static method on {ownerType.FullName}: {methodName}");
            return method;
        }
    }
}