using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;

namespace BC.Rendering.Tests
{
    public sealed class ParticleMaterialSystemM14DesignTests
    {
        private const string SpecPath = "Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemSpec.md";
        private const string MilestonesPath = "Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemMilestones.md";
        private const string ProgressPath = "Assets/Docs/ParticleUnitSpec/ParticleMaterialSystemProgress.md";
        private const string BootstrapperPath = "Assets/Art/Shader/Particles/ParticleUnlit/Editor/ParticleUnlitValidationBootstrapper.cs";
        private const string RingReadmePath = "Assets/Art/Shader/Particles/ParticleRingUnlit/README.md";
        private const string RingHlslReadmePath = "Assets/Art/Shader/Particles/ParticleRingUnlit/HLSL/README.md";
        private const string RingPassReadmePath = "Assets/Art/Shader/Particles/ParticleRingUnlit/HLSL/Passes/README.md";
        private const string RingEditorReadmePath = "Assets/Art/Shader/Particles/ParticleRingUnlit/Editor/README.md";
        private const string GroundReadmePath = "Assets/Art/Shader/Particles/ParticleGroundUnlit/README.md";
        private const string GroundHlslReadmePath = "Assets/Art/Shader/Particles/ParticleGroundUnlit/HLSL/README.md";
        private const string GroundPassReadmePath = "Assets/Art/Shader/Particles/ParticleGroundUnlit/HLSL/Passes/README.md";
        private const string GroundEditorReadmePath = "Assets/Art/Shader/Particles/ParticleGroundUnlit/Editor/README.md";

        private static readonly string[] RequiredFolders =
        {
            "Assets/Art/Shader/Particles/ParticleRingUnlit",
            "Assets/Art/Shader/Particles/ParticleRingUnlit/HLSL",
            "Assets/Art/Shader/Particles/ParticleRingUnlit/HLSL/Passes",
            "Assets/Art/Shader/Particles/ParticleRingUnlit/Editor",
            "Assets/Art/Shader/Particles/ParticleGroundUnlit",
            "Assets/Art/Shader/Particles/ParticleGroundUnlit/HLSL",
            "Assets/Art/Shader/Particles/ParticleGroundUnlit/HLSL/Passes",
            "Assets/Art/Shader/Particles/ParticleGroundUnlit/Editor"
        };

        [OneTimeSetUp]
        public void EnsureScaffold()
        {
            Type bootstrapperType = GetEditorAssemblyType("BC.Rendering.ParticleUnlitValidationBootstrapper");
            MethodInfo bootstrapMethod = GetStaticMethod(bootstrapperType, "BootstrapM1Scaffold");
            bootstrapMethod.Invoke(null, null);
            AssetDatabase.Refresh();
        }

        [Test]
        public void SourceFilesExistForM14DesignMilestone()
        {
            Assert.IsTrue(File.Exists(SpecPath), $"Expected particle material system spec: {SpecPath}");
            Assert.IsTrue(File.Exists(MilestonesPath), $"Expected particle material system milestones: {MilestonesPath}");
            Assert.IsTrue(File.Exists(ProgressPath), $"Expected particle material system progress: {ProgressPath}");
            Assert.IsTrue(File.Exists(BootstrapperPath), $"Expected validation bootstrapper source: {BootstrapperPath}");
            Assert.IsTrue(File.Exists(RingReadmePath), $"Expected ParticleRingUnlit scaffold readme: {RingReadmePath}");
            Assert.IsTrue(File.Exists(RingHlslReadmePath), $"Expected ParticleRingUnlit HLSL scaffold readme: {RingHlslReadmePath}");
            Assert.IsTrue(File.Exists(RingPassReadmePath), $"Expected ParticleRingUnlit pass scaffold readme: {RingPassReadmePath}");
            Assert.IsTrue(File.Exists(RingEditorReadmePath), $"Expected ParticleRingUnlit editor scaffold readme: {RingEditorReadmePath}");
            Assert.IsTrue(File.Exists(GroundReadmePath), $"Expected ParticleGroundUnlit scaffold readme: {GroundReadmePath}");
            Assert.IsTrue(File.Exists(GroundHlslReadmePath), $"Expected ParticleGroundUnlit HLSL scaffold readme: {GroundHlslReadmePath}");
            Assert.IsTrue(File.Exists(GroundPassReadmePath), $"Expected ParticleGroundUnlit pass scaffold readme: {GroundPassReadmePath}");
            Assert.IsTrue(File.Exists(GroundEditorReadmePath), $"Expected ParticleGroundUnlit editor scaffold readme: {GroundEditorReadmePath}");
        }

        [Test]
        public void M14DocsDefineDesignAndScaffoldContract()
        {
            string milestonesSource = File.ReadAllText(MilestonesPath);
            string specSource = File.ReadAllText(SpecPath);
            string progressSource = File.ReadAllText(ProgressPath);

            StringAssert.Contains("design + scaffold/contract", milestonesSource);
            StringAssert.Contains("ParticleRingUnlit", milestonesSource);
            StringAssert.Contains("ParticleGroundUnlit", milestonesSource);
            StringAssert.Contains("_RingInnerRadius", milestonesSource);
            StringAssert.Contains("_WorldUvScale", milestonesSource);
            StringAssert.Contains("FX_Particle_ShockwaveRing", milestonesSource);
            StringAssert.Contains("FX_Particle_GroundSmoke", milestonesSource);
            StringAssert.Contains("Ring / Ground を ParticleUnlit 本体へ無理に詰め込まない方針", milestonesSource);

            StringAssert.Contains("BC/Particles/ParticleRingUnlit", specSource);
            StringAssert.Contains("BC/Particles/ParticleGroundUnlit", specSource);
            StringAssert.Contains("_RingThickness", specSource);
            StringAssert.Contains("_GroundContactFade", specSource);
            StringAssert.Contains("ParticleRingUnlit:", specSource);
            StringAssert.Contains("ParticleGroundUnlit:", specSource);
            StringAssert.Contains("quality tier へ未配属の reserved family", specSource);

            StringAssert.Contains("M14 Ring / Ground 系拡張設計 | In Progress", progressSource);
            StringAssert.Contains("functional shader / generated asset / scene marker は未着手", progressSource);
        }

        [Test]
        public void BootstrapperSourceDefinesM14FutureFamilyContract()
        {
            string bootstrapperSource = File.ReadAllText(BootstrapperPath);

            StringAssert.Contains("ParticleRingHlslDirectory", bootstrapperSource);
            StringAssert.Contains("ParticleRingPassDirectory", bootstrapperSource);
            StringAssert.Contains("ParticleRingEditorDirectory", bootstrapperSource);
            StringAssert.Contains("ParticleGroundHlslDirectory", bootstrapperSource);
            StringAssert.Contains("ParticleGroundPassDirectory", bootstrapperSource);
            StringAssert.Contains("ParticleGroundEditorDirectory", bootstrapperSource);
            StringAssert.Contains("FutureRingMarkerObjectNames", bootstrapperSource);
            StringAssert.Contains("FutureGroundMarkerObjectNames", bootstrapperSource);
            StringAssert.Contains("M14RingPrefabCandidateNames", bootstrapperSource);
            StringAssert.Contains("M14GroundPrefabCandidateNames", bootstrapperSource);
            StringAssert.Contains("M14 は設計 + scaffold/contract の milestone", bootstrapperSource);
        }

        [Test]
        public void RingAndGroundScaffoldFoldersExist()
        {
            foreach (string folderPath in RequiredFolders)
            {
                Assert.IsTrue(AssetDatabase.IsValidFolder(folderPath), $"Expected M14 scaffold folder: {folderPath}");
            }
        }

        [Test]
        public void ScaffoldReadmesCaptureOwnershipAndNonGoals()
        {
            string ringReadme = File.ReadAllText(RingReadmePath);
            string ringHlslReadme = File.ReadAllText(RingHlslReadmePath);
            string ringPassReadme = File.ReadAllText(RingPassReadmePath);
            string ringEditorReadme = File.ReadAllText(RingEditorReadmePath);
            string groundReadme = File.ReadAllText(GroundReadmePath);
            string groundHlslReadme = File.ReadAllText(GroundHlslReadmePath);
            string groundPassReadme = File.ReadAllText(GroundPassReadmePath);
            string groundEditorReadme = File.ReadAllText(GroundEditorReadmePath);

            StringAssert.Contains("design + scaffold/contract milestone", ringReadme);
            StringAssert.Contains("FX_Particle_ShockwaveRing", ringReadme);
            StringAssert.Contains("shader root はまだ追加しない", ringReadme);
            StringAssert.Contains("HLSL 分割の folder contract", ringHlslReadme);
            StringAssert.Contains("pass include", ringPassReadme);
            StringAssert.Contains("ShaderGUI", ringEditorReadme);

            StringAssert.Contains("design + scaffold/contract milestone", groundReadme);
            StringAssert.Contains("FX_Particle_GroundSmoke", groundReadme);
            StringAssert.Contains("generated material / prefab はまだ追加しない", groundReadme);
            StringAssert.Contains("HLSL 分割の folder contract", groundHlslReadme);
            StringAssert.Contains("pass include", groundPassReadme);
            StringAssert.Contains("MaterialValidator", groundEditorReadme);
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