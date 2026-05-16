using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BC.Rendering.Tests
{
    public sealed class ParticleMaterialSystemM1ScaffoldTests
    {
        private const string SpecPath = "Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemSpec.md";
        private const string MilestonesPath = "Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemMilestones.md";
        private const string ProgressPath = "Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemProgress.md";
        private const string BootstrapperPath = "Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs";
        private const string ScenePath = "Assets/Scenes/Particles/ParticleMaterialTestScene.unity";

        private static readonly string[] RequiredFolders =
        {
            "Assets/Art/Shader/Particles/ParticleUnlit",
            "Assets/Art/Shader/Particles/ParticleUnlit/HLSL",
            "Assets/Art/Shader/Particles/ParticleUnlit/HLSL/Passes",
            "Assets/Art/Shader/Particles/ParticleUnlit/Editor",
            "Assets/Art/Shader/Particles/ParticleLit",
            "Assets/Art/Shader/Particles/ParticleLit/HLSL",
            "Assets/Art/Shader/Particles/ParticleLit/HLSL/Passes",
            "Assets/Art/Shader/Particles/ParticleLit/Editor",
            "Assets/Art/Shader/Particles/ParticleDistortion",
            "Assets/Art/Shader/Particles/ParticleDistortion/HLSL",
            "Assets/Art/Shader/Particles/ParticleDistortion/HLSL/Passes",
            "Assets/Art/Shader/Particles/ParticleDistortion/Editor",
            "Assets/Art/Shader/Particles/ParticleRingUnlit",
            "Assets/Art/Shader/Particles/ParticleGroundUnlit",
            "Assets/Art/Materials/Particles/Unlit",
            "Assets/Art/Materials/Particles/Lit",
            "Assets/Art/Materials/Particles/Distortion",
            "Assets/Art/Textures/Particles/Unlit",
            "Assets/Art/Textures/Particles/Lit",
            "Assets/Art/Textures/Particles/Distortion",
            "Assets/Art/Textures/Particles/Shared",
            "Assets/Art/Prefab/Particles/Unlit",
            "Assets/Art/Prefab/Particles/Lit",
            "Assets/Art/Prefab/Particles/Distortion",
            "Assets/Scenes/Particles"
        };

        private static readonly string[] RequiredMarkerObjects =
        {
            "ParticleMaterialValidationRoot",
            "Dust Test Area",
            "Smoke Test Area",
            "Glow Test Area",
            "Spark Test Area",
            "Magic Test Area",
            "WebGL Load Test Area",
            "Future Lit Test Area",
            "Future Distortion Test Area"
        };

        [OneTimeSetUp]
        public void EnsureScaffold()
        {
            Assert.IsTrue(File.Exists(BootstrapperPath), $"Expected ParticleUnlit bootstrapper source: {BootstrapperPath}");

            Type bootstrapperType = GetEditorAssemblyType("BC.Rendering.ParticleUnlitValidationBootstrapper");
            MethodInfo bootstrapMethod = GetStaticMethod(bootstrapperType, "BootstrapM1Scaffold");
            bootstrapMethod.Invoke(null, null);
            AssetDatabase.Refresh();
        }

        [Test]
        public void M1DocsAndBootstrapperExist()
        {
            Assert.IsTrue(File.Exists(SpecPath), $"Expected particle material system spec: {SpecPath}");
            Assert.IsTrue(File.Exists(MilestonesPath), $"Expected particle material system milestones: {MilestonesPath}");
            Assert.IsTrue(File.Exists(ProgressPath), $"Expected particle material system progress: {ProgressPath}");
            Assert.IsTrue(File.Exists(BootstrapperPath), $"Expected ParticleUnlit bootstrapper source: {BootstrapperPath}");
        }

        [Test]
        public void M1RequiredFoldersExist()
        {
            foreach (string folderPath in RequiredFolders)
            {
                Assert.IsTrue(AssetDatabase.IsValidFolder(folderPath), $"Expected scaffold folder: {folderPath}");
            }
        }

        [Test]
        public void ParticleMaterialTestSceneExistsAndOpens()
        {
            Assert.IsTrue(File.Exists(ScenePath), $"Expected particle validation scene: {ScenePath}");

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid(), "ParticleMaterialTestScene must open successfully.");
            Assert.IsNotNull(GameObject.Find("Main Camera"), "ParticleMaterialTestScene must contain a Main Camera.");
            Assert.IsNotNull(GameObject.Find("Directional Light"), "ParticleMaterialTestScene must contain a Directional Light.");
            Assert.IsNotNull(GameObject.Find("ValidationFloor"), "ParticleMaterialTestScene must contain a ValidationFloor.");
        }

        [Test]
        public void ParticleMaterialTestSceneContainsM1Markers()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid(), "ParticleMaterialTestScene must open successfully.");

            foreach (string markerName in RequiredMarkerObjects)
            {
                Assert.IsNotNull(GameObject.Find(markerName), $"Expected validation marker object: {markerName}");
            }

            GameObject rootObject = GameObject.Find("ParticleMaterialValidationRoot");
            Assert.IsNotNull(rootObject, "ParticleMaterialValidationRoot must exist.");
            Assert.AreEqual(8, rootObject.transform.Cast<Transform>().Count(), "M1 validation root must keep eight marker children.");
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