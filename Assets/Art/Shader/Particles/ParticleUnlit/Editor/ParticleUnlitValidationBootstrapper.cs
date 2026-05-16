using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BC.Rendering
{
    // M1 では shader logic を作らず、後続 milestone が迷わない受け皿だけを固定する。
    public static class ParticleUnlitValidationBootstrapper
    {
        private const string ParticleShaderRootDirectory = "Assets/Art/Shader/Particles";
        private const string ParticleUnlitDirectory = ParticleShaderRootDirectory + "/ParticleUnlit";
        private const string ParticleUnlitHlslDirectory = ParticleUnlitDirectory + "/HLSL";
        private const string ParticleUnlitPassDirectory = ParticleUnlitHlslDirectory + "/Passes";
        private const string ParticleUnlitEditorDirectory = ParticleUnlitDirectory + "/Editor";
        private const string ParticleLitDirectory = ParticleShaderRootDirectory + "/ParticleLit";
        private const string ParticleLitHlslDirectory = ParticleLitDirectory + "/HLSL";
        private const string ParticleLitPassDirectory = ParticleLitHlslDirectory + "/Passes";
        private const string ParticleLitEditorDirectory = ParticleLitDirectory + "/Editor";
        private const string ParticleDistortionDirectory = ParticleShaderRootDirectory + "/ParticleDistortion";
        private const string ParticleDistortionHlslDirectory = ParticleDistortionDirectory + "/HLSL";
        private const string ParticleDistortionPassDirectory = ParticleDistortionHlslDirectory + "/Passes";
        private const string ParticleDistortionEditorDirectory = ParticleDistortionDirectory + "/Editor";
        private const string ParticleRingDirectory = ParticleShaderRootDirectory + "/ParticleRingUnlit";
        private const string ParticleGroundDirectory = ParticleShaderRootDirectory + "/ParticleGroundUnlit";

        private const string MaterialUnlitDirectory = "Assets/Art/Materials/Particles/Unlit";
        private const string MaterialLitDirectory = "Assets/Art/Materials/Particles/Lit";
        private const string MaterialDistortionDirectory = "Assets/Art/Materials/Particles/Distortion";
        private const string TextureUnlitDirectory = "Assets/Art/Textures/Particles/Unlit";
        private const string TextureLitDirectory = "Assets/Art/Textures/Particles/Lit";
        private const string TextureDistortionDirectory = "Assets/Art/Textures/Particles/Distortion";
        private const string TextureSharedDirectory = "Assets/Art/Textures/Particles/Shared";
        private const string PrefabUnlitDirectory = "Assets/Art/Prefab/Particles/Unlit";
        private const string PrefabLitDirectory = "Assets/Art/Prefab/Particles/Lit";
        private const string PrefabDistortionDirectory = "Assets/Art/Prefab/Particles/Distortion";
        private const string SceneDirectory = "Assets/Scenes/Particles";
        private const string ScenePath = SceneDirectory + "/ParticleMaterialTestScene.unity";
        private const string ShaderPath = ParticleUnlitDirectory + "/BC_Particles_ParticleUnlit.shader";
        private const string TexturePath = TextureUnlitDirectory + "/T_Particle_TestSoftSprite.png";
        private const string MaterialPath = MaterialUnlitDirectory + "/M_Particle_Test_Alpha.mat";

        private const string ValidationRootName = "ParticleMaterialValidationRoot";
        private const string CameraName = "Main Camera";
        private const string LightName = "Directional Light";
        private const string FloorName = "ValidationFloor";
        private const string BaseValidationObjectName = "ParticleUnlit_BaseValidation";
        private const string LifetimeValidationObjectName = "ParticleUnlit_LifetimeValidation";

        private static readonly string[] MarkerNames =
        {
            "Dust Test Area",
            "Smoke Test Area",
            "Glow Test Area",
            "Spark Test Area",
            "Magic Test Area",
            "WebGL Load Test Area",
            "Future Lit Test Area",
            "Future Distortion Test Area"
        };

        [MenuItem("Tools/BC/Particles/ParticleUnlit/Bootstrap M1 Scaffold")]
        public static void BootstrapM1Scaffold()
        {
            EnsureDirectories();
            AssetDatabase.Refresh();

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                EnsureValidationScene();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                RestoreOriginalSceneSetup(originalSetup);
            }

            Debug.Log("ParticleUnlit M1 scaffold bootstrapped successfully.");
        }

        [MenuItem("Tools/BC/Particles/ParticleUnlit/Bootstrap M2 Validation Assets")]
        public static void BootstrapM2ValidationAssets()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                BootstrapM1Scaffold();

                Shader shader = LoadRequiredShader();
                Texture2D baseTexture = EnsureValidationTexture();
                Material material = EnsureValidationMaterial(shader, baseTexture);

                EnsureM2ValidationScene(material);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("ParticleUnlit M2 validation assets bootstrapped successfully.");
            }
            finally
            {
                RestoreOriginalSceneSetup(originalSetup);
            }
        }

        private static void RestoreOriginalSceneSetup(SceneSetup[] originalSetup)
        {
            if (originalSetup != null && originalSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            }
        }

        private static void EnsureDirectories()
        {
            Directory.CreateDirectory(ParticleUnlitDirectory);
            Directory.CreateDirectory(ParticleUnlitHlslDirectory);
            Directory.CreateDirectory(ParticleUnlitPassDirectory);
            Directory.CreateDirectory(ParticleUnlitEditorDirectory);

            Directory.CreateDirectory(ParticleLitDirectory);
            Directory.CreateDirectory(ParticleLitHlslDirectory);
            Directory.CreateDirectory(ParticleLitPassDirectory);
            Directory.CreateDirectory(ParticleLitEditorDirectory);

            Directory.CreateDirectory(ParticleDistortionDirectory);
            Directory.CreateDirectory(ParticleDistortionHlslDirectory);
            Directory.CreateDirectory(ParticleDistortionPassDirectory);
            Directory.CreateDirectory(ParticleDistortionEditorDirectory);

            Directory.CreateDirectory(ParticleRingDirectory);
            Directory.CreateDirectory(ParticleGroundDirectory);

            Directory.CreateDirectory(MaterialUnlitDirectory);
            Directory.CreateDirectory(MaterialLitDirectory);
            Directory.CreateDirectory(MaterialDistortionDirectory);

            Directory.CreateDirectory(TextureUnlitDirectory);
            Directory.CreateDirectory(TextureLitDirectory);
            Directory.CreateDirectory(TextureDistortionDirectory);
            Directory.CreateDirectory(TextureSharedDirectory);

            Directory.CreateDirectory(PrefabUnlitDirectory);
            Directory.CreateDirectory(PrefabLitDirectory);
            Directory.CreateDirectory(PrefabDistortionDirectory);

            Directory.CreateDirectory(SceneDirectory);
        }

        private static void EnsureValidationScene()
        {
            Scene scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            scene.name = "ParticleMaterialTestScene";

            EnsureCamera();
            EnsureDirectionalLight();
            EnsureFloor();
            EnsureMarkerHierarchy();

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void EnsureM2ValidationScene(Material validationMaterial)
        {
            Scene scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            scene.name = "ParticleMaterialTestScene";

            EnsureCamera();
            EnsureDirectionalLight();
            EnsureFloor();
            EnsureMarkerHierarchy();

            Transform dustMarker = GetRequiredMarkerTransform("Dust Test Area");
            Transform glowMarker = GetRequiredMarkerTransform("Glow Test Area");

            EnsureParticleValidationAnchor(
                dustMarker,
                BaseValidationObjectName,
                validationMaterial,
                ConfigureBaseValidationParticle);

            EnsureParticleValidationAnchor(
                glowMarker,
                LifetimeValidationObjectName,
                validationMaterial,
                ConfigureLifetimeValidationParticle);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static Shader LoadRequiredShader()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
            {
                throw new FileNotFoundException("ParticleUnlit shader asset was not found.", ShaderPath);
            }

            return shader;
        }

        private static Texture2D EnsureValidationTexture()
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (texture == null)
            {
                Texture2D generatedTexture = BuildValidationTexture();
                File.WriteAllBytes(TexturePath, generatedTexture.EncodeToPNG());
                Object.DestroyImmediate(generatedTexture);
                AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceUpdate);

                TextureImporter textureImporter = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
                if (textureImporter != null)
                {
                    textureImporter.textureType = TextureImporterType.Default;
                    textureImporter.sRGBTexture = true;
                    textureImporter.alphaSource = TextureImporterAlphaSource.FromInput;
                    textureImporter.wrapMode = TextureWrapMode.Clamp;
                    textureImporter.filterMode = FilterMode.Bilinear;
                    textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
                    textureImporter.SaveAndReimport();
                }

                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            }

            if (texture == null)
            {
                throw new IOException("ParticleUnlit validation texture could not be loaded after import: " + TexturePath);
            }

            return texture;
        }

        private static Material EnsureValidationMaterial(Shader shader, Texture2D baseTexture)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = Path.GetFileNameWithoutExtension(MaterialPath)
                };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            material.shader = shader;
            material.SetFloat("_Cull", 0f);
            material.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            material.SetTexture("_BaseMap", baseTexture);
            material.SetColor("_BaseColor", new Color(1f, 0.92f, 0.84f, 1f));
            material.SetFloat("_Alpha", 1f);
            material.SetFloat("_Brightness", 1f);
            material.SetFloat("_UseVertexColor", 1f);
            material.SetFloat("_SoftCircleStrength", 1f);
            material.SetFloat("_EdgeFadePower", 1.6f);
            material.SetFloat("_EdgeFadeStrength", 1f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_ColorMask", 15f);
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetOverrideTag("RenderType", "Transparent");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D BuildValidationTexture()
        {
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = Path.GetFileNameWithoutExtension(TexturePath)
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1);
                    float v = y / (float)(size - 1);
                    Vector2 centeredUV = new Vector2(u * 2f - 1f, v * 2f - 1f);
                    float radial = 1f - Mathf.Clamp01(centeredUV.magnitude);
                    float alpha = Mathf.Pow(radial, 1.35f);
                    Color color = Color.Lerp(new Color(1f, 0.58f, 0.22f, 1f), Color.white, Mathf.Clamp01(v));
                    color.a = alpha;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return texture;
        }

        private static void EnsureCamera()
        {
            GameObject cameraObject = GameObject.Find(CameraName);
            Camera cameraComponent;

            if (cameraObject == null)
            {
                cameraObject = new GameObject(CameraName);
                cameraComponent = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }
            else
            {
                cameraComponent = cameraObject.GetComponent<Camera>() ?? cameraObject.AddComponent<Camera>();
            }

            cameraComponent.clearFlags = CameraClearFlags.Skybox;
            cameraComponent.transform.position = new Vector3(0f, 4.5f, -12f);
            cameraComponent.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
        }

        private static void EnsureDirectionalLight()
        {
            GameObject lightObject = GameObject.Find(LightName);
            Light lightComponent;

            if (lightObject == null)
            {
                lightObject = new GameObject(LightName);
                lightComponent = lightObject.AddComponent<Light>();
            }
            else
            {
                lightComponent = lightObject.GetComponent<Light>() ?? lightObject.AddComponent<Light>();
            }

            lightComponent.type = LightType.Directional;
            lightComponent.intensity = 1.0f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void EnsureFloor()
        {
            GameObject floorObject = GameObject.Find(FloorName);
            if (floorObject == null)
            {
                floorObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floorObject.name = FloorName;
            }

            floorObject.transform.position = Vector3.zero;
            floorObject.transform.rotation = Quaternion.identity;
            floorObject.transform.localScale = new Vector3(4f, 1f, 4f);
        }

        private static void EnsureMarkerHierarchy()
        {
            GameObject root = GameObject.Find(ValidationRootName);
            if (root == null)
            {
                root = new GameObject(ValidationRootName);
            }

            root.transform.position = Vector3.zero;

            for (int markerIndex = 0; markerIndex < MarkerNames.Length; markerIndex++)
            {
                string markerName = MarkerNames[markerIndex];
                Transform markerTransform = root.transform.Find(markerName);
                if (markerTransform == null)
                {
                    GameObject markerObject = new GameObject(markerName);
                    markerTransform = markerObject.transform;
                    markerTransform.SetParent(root.transform, false);
                }

                markerTransform.localPosition = new Vector3((markerIndex % 4) * 6f - 9f, 0f, (markerIndex / 4) * 8f + 2f);
                markerTransform.localRotation = Quaternion.identity;
                markerTransform.localScale = Vector3.one;
            }
        }

        private static Transform GetRequiredMarkerTransform(string markerName)
        {
            GameObject root = GameObject.Find(ValidationRootName);
            if (root == null)
            {
                throw new MissingReferenceException("ParticleMaterialValidationRoot was not found in the validation scene.");
            }

            Transform markerTransform = root.transform.Find(markerName);
            if (markerTransform == null)
            {
                throw new MissingReferenceException("Required validation marker was not found: " + markerName);
            }

            return markerTransform;
        }

        private static void EnsureParticleValidationAnchor(Transform parent, string objectName, Material validationMaterial, System.Action<ParticleSystem> configure)
        {
            Transform anchorTransform = parent.Find(objectName);
            if (anchorTransform == null)
            {
                GameObject anchorObject = new GameObject(objectName);
                anchorTransform = anchorObject.transform;
                anchorTransform.SetParent(parent, false);
            }

            anchorTransform.localPosition = new Vector3(0f, 1.2f, 0f);
            anchorTransform.localRotation = Quaternion.identity;
            anchorTransform.localScale = Vector3.one;

            ParticleSystem particleSystem = anchorTransform.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                particleSystem = anchorTransform.gameObject.AddComponent<ParticleSystem>();
            }

            ParticleSystemRenderer renderer = anchorTransform.GetComponent<ParticleSystemRenderer>();
            if (renderer == null)
            {
                renderer = anchorTransform.gameObject.AddComponent<ParticleSystemRenderer>();
            }

            configure(particleSystem);
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sharedMaterial = validationMaterial;
            EditorUtility.SetDirty(anchorTransform.gameObject);
        }

        private static void ConfigureBaseValidationParticle(ParticleSystem particleSystem)
        {
            var main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = 1.6f;
            main.startSpeed = 0.05f;
            main.startSize = 1.2f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.8f, 0.55f, 1f));
            main.maxParticles = 32;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 8f;

            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.12f;

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = false;
        }

        private static void ConfigureLifetimeValidationParticle(ParticleSystem particleSystem)
        {
            ConfigureBaseValidationParticle(particleSystem);

            var main = particleSystem.main;
            main.startLifetime = 2.0f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.55f, 0.85f, 1f, 1f));

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.6f, 0.9f, 1f), 0f),
                    new GradientColorKey(new Color(1f, 0.6f, 0.35f), 0.65f),
                    new GradientColorKey(new Color(0.25f, 0.2f, 0.5f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.65f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        }
    }
}