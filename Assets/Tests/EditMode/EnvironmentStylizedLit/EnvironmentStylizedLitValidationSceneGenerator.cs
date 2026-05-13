using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BC.Rendering.Tests
{
    public static class EnvironmentStylizedLitValidationSceneGenerator
    {
        private const string SceneFolder = "Assets/Scenes/EnvironmentStylizedLit";
        private const string DefaultMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_Default.mat";
        private const string InteriorMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_Interior.mat";
        private const string RoomMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_Room.mat";
        private const string DoubleSidedMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_DoubleSided.mat";
        private const string AmbientRoomMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_AmbientRoom.mat";
        private const string AmbientBounceMaterialPath = "Assets/Art/Materials/EnvironmentStylizedLit/ESL_Test_AmbientBounce.mat";
        private const string TestRoomScenePath = SceneFolder + "/ESL_TestRoom.unity";
        private const string LightingLabScenePath = SceneFolder + "/ESL_LightingLab.unity";

        public static void GenerateAllBatch()
        {
            if (!AssetDatabase.IsValidFolder(SceneFolder))
            {
                Directory.CreateDirectory(SceneFolder);
                AssetDatabase.Refresh();
            }

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            bool createdAssets = false;

            try
            {
                Material defaultMaterial = AssetDatabase.LoadAssetAtPath<Material>(DefaultMaterialPath);
                Material interiorMaterial = AssetDatabase.LoadAssetAtPath<Material>(InteriorMaterialPath);
                Material roomMaterial = EnsureDerivedMaterialAsset(RoomMaterialPath, defaultMaterial, material =>
                {
                    material.SetFloat("_Cull", 1f);
                    material.SetFloat("_AlphaClip", 0f);
                    material.SetFloat("_ShadowInfluence", 1f);
                    material.SetFloat("_ShadowSoftFill", 0.3f);
                    material.SetFloat("_ShadowColorBlend", 0.75f);
                }, ref createdAssets, false);
                Material doubleSidedMaterial = EnsureDerivedMaterialAsset(DoubleSidedMaterialPath, defaultMaterial, material =>
                {
                    material.SetFloat("_Cull", 0f);
                    material.SetFloat("_AlphaClip", 0f);
                    material.SetFloat("_ShadowInfluence", 1f);
                    material.SetFloat("_ShadowSoftFill", 0.2f);
                    material.SetFloat("_ShadowColorBlend", 0.6f);
                }, ref createdAssets, false);

                bool createdTestRoom = false;
                if (!File.Exists(TestRoomScenePath))
                {
                    GenerateTestRoom(defaultMaterial, interiorMaterial);
                    createdTestRoom = true;
                    createdAssets = true;
                }

                if (!File.Exists(LightingLabScenePath))
                {
                    GenerateLightingLab(defaultMaterial, interiorMaterial);
                    createdAssets = true;
                }

                if (createdTestRoom)
                {
                    EnsureTestRoomM4Anchors(defaultMaterial, interiorMaterial, roomMaterial, doubleSidedMaterial);
                }

                if (createdAssets)
                {
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }
        }

        public static void BootstrapM5ValidationAssetsBatch()
        {
            if (!AssetDatabase.IsValidFolder(SceneFolder))
            {
                Directory.CreateDirectory(SceneFolder);
                AssetDatabase.Refresh();
            }

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            bool createdAssets = false;

            try
            {
                GenerateAllBatch();

                Material defaultMaterial = AssetDatabase.LoadAssetAtPath<Material>(DefaultMaterialPath);
                Material interiorMaterial = AssetDatabase.LoadAssetAtPath<Material>(InteriorMaterialPath);
                Material ambientRoomMaterial = EnsureDerivedMaterialAsset(AmbientRoomMaterialPath, interiorMaterial, ConfigureAmbientRoomMaterial, ref createdAssets, true);
                Material ambientBounceMaterial = EnsureDerivedMaterialAsset(AmbientBounceMaterialPath, defaultMaterial, ConfigureAmbientBounceMaterial, ref createdAssets, true);

                EnsureTestRoomM5Anchors(ambientRoomMaterial, ambientBounceMaterial);
                EnsureLightingLabM5Anchors(ambientBounceMaterial);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }
        }

        private static void GenerateTestRoom(Material defaultMaterial, Material interiorMaterial)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "ESL_TestRoom";

            CreateCamera(new Vector3(0f, 1.8f, -7f), new Vector3(10f, 0f, 0f));
            CreateDirectionalLight();
            CreateFloor();

            CreateQuad("BaseMapQuad", new Vector3(-2.6f, 1.5f, 0f), Vector3.zero, new Vector3(2f, 2f, 1f), defaultMaterial);
            CreateQuad("AlphaClipQuad", new Vector3(0f, 1.5f, 0f), Vector3.zero, new Vector3(2f, 2f, 1f), interiorMaterial);
            CreateSphere("EmissionSphere", new Vector3(2.6f, 1.2f, 0f), Vector3.one * 1.6f, defaultMaterial);
            CreateCube("NormalOnCube", new Vector3(-1.6f, 0.6f, 2.4f), Vector3.one * 1.1f, defaultMaterial);
            CreateCube("NormalOffCube", new Vector3(1.6f, 0.6f, 2.4f), Vector3.one * 1.1f, interiorMaterial);

            SaveScene(scene, "ESL_TestRoom");
        }

        private static void GenerateLightingLab(Material defaultMaterial, Material interiorMaterial)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "ESL_LightingLab";

            CreateCamera(new Vector3(0f, 1.9f, -8f), new Vector3(12f, 0f, 0f));
            CreateDirectionalLight();
            CreateFloor();

            CreateSphere("LightingDefaultSphere", new Vector3(-2.25f, 1.1f, 0.5f), Vector3.one * 1.5f, defaultMaterial);
            CreateSphere("LightingInteriorSphere", new Vector3(0f, 1.1f, 0.5f), Vector3.one * 1.5f, interiorMaterial);
            CreateCube("LightingEmissionCube", new Vector3(2.35f, 0.9f, 0.5f), Vector3.one * 1.5f, defaultMaterial);
            CreateQuad("LightingAlphaClipQuad", new Vector3(0f, 1.5f, 3f), Vector3.zero, new Vector3(2.4f, 2.4f, 1f), interiorMaterial);

            SaveScene(scene, "ESL_LightingLab");
        }

        private static Material EnsureDerivedMaterialAsset(string materialPath, Material sourceMaterial, System.Action<Material> configureMaterial, ref bool createdAssets, bool updateExisting)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material != null)
            {
                if (updateExisting)
                {
                    configureMaterial(material);
                    EditorUtility.SetDirty(material);
                }

                return material;
            }

            material = new Material(sourceMaterial)
            {
                name = Path.GetFileNameWithoutExtension(materialPath)
            };

            configureMaterial(material);
            AssetDatabase.CreateAsset(material, materialPath);
            createdAssets = true;
            return material;
        }

        private static void ConfigureAmbientRoomMaterial(Material material)
        {
            material.SetFloat("_Cull", 1f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetFloat("_ShadowInfluence", 1f);
            material.SetFloat("_ShadowSoftFill", 0.3f);
            material.SetFloat("_ShadowColorBlend", 0.75f);
            ApplyAmbientBounceProfile(
                material,
                new Color(0.58f, 0.64f, 0.76f, 1f),
                new Color(0.38f, 0.42f, 0.5f, 1f),
                new Color(0.24f, 0.21f, 0.18f, 1f),
                0.55f,
                new Color(0.92f, 0.78f, 0.62f, 1f),
                0.32f,
                new Vector4(0f, 1f, 0f, 0f),
                0.65f,
                new Color(0.7f, 0.77f, 0.88f, 1f));
        }

        private static void ConfigureAmbientBounceMaterial(Material material)
        {
            material.SetFloat("_Cull", 2f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetFloat("_ShadowInfluence", 1f);
            material.SetFloat("_ShadowSoftFill", 0.2f);
            material.SetFloat("_ShadowColorBlend", 0.6f);
            ApplyAmbientBounceProfile(
                material,
                new Color(0.54f, 0.61f, 0.74f, 1f),
                new Color(0.36f, 0.4f, 0.48f, 1f),
                new Color(0.23f, 0.2f, 0.18f, 1f),
                0.5f,
                new Color(0.94f, 0.8f, 0.64f, 1f),
                0.38f,
                new Vector4(0f, 1f, 0f, 0f),
                0.55f,
                new Color(0.72f, 0.79f, 0.9f, 1f));
        }

        private static void ApplyAmbientBounceProfile(
            Material material,
            Color ambientTopColor,
            Color ambientSideColor,
            Color ambientBottomColor,
            float ambientStrength,
            Color bounceColor,
            float bounceStrength,
            Vector4 bounceDirection,
            float bounceWrap,
            Color indirectShadowColor)
        {
            material.SetColor("_AmbientTopColor", ambientTopColor);
            material.SetColor("_AmbientSideColor", ambientSideColor);
            material.SetColor("_AmbientBottomColor", ambientBottomColor);
            material.SetFloat("_AmbientStrength", ambientStrength);
            material.SetColor("_BounceColor", bounceColor);
            material.SetFloat("_BounceStrength", bounceStrength);
            material.SetVector("_BounceDirection", bounceDirection);
            material.SetFloat("_BounceWrap", bounceWrap);
            material.SetColor("_IndirectShadowColor", indirectShadowColor);
        }

        private static void EnsureTestRoomM4Anchors(Material defaultMaterial, Material interiorMaterial, Material roomMaterial, Material doubleSidedMaterial)
        {
            Scene scene = EditorSceneManager.OpenScene(TestRoomScenePath, OpenSceneMode.Single);

            EnsurePointLight("M4PointLight", new Vector3(8f, 2.1f, 0f), new Color(1f, 0.83f, 0.72f), 8f, 18f, true);
            EnsureViewpointMarker("M4RoomViewpoint", new Vector3(8f, 1.4f, -0.25f));
            EnsureCube("M4InteriorRoom", new Vector3(8f, 1.5f, 0f), new Vector3(6f, 3.2f, 6f), roomMaterial, ShadowCastingMode.Off);
            EnsureCube("M4CullBackCube", new Vector3(3.8f, 0.8f, 7f), Vector3.one * 1.3f, defaultMaterial, ShadowCastingMode.On);
            EnsureCube("M4CullFrontCube", new Vector3(6.2f, 0.8f, 7f), Vector3.one * 1.3f, roomMaterial, ShadowCastingMode.On);
            EnsureCube("M4CullBothCube", new Vector3(8.6f, 0.8f, 7f), Vector3.one * 1.3f, doubleSidedMaterial, ShadowCastingMode.On);
            EnsureSphere("M4ShadowCasterOnSphere", new Vector3(6.8f, 1.1f, 0f), Vector3.one * 1.4f, defaultMaterial, ShadowCastingMode.On);
            EnsureSphere("M4ShadowCasterOffSphere", new Vector3(9.1f, 1.1f, 0f), Vector3.one * 1.4f, defaultMaterial, ShadowCastingMode.Off);
            EnsureQuad("M4AlphaClipCasterQuad", new Vector3(11f, 1.65f, 0f), new Vector3(0f, -90f, 0f), new Vector3(2.2f, 2.2f, 1f), interiorMaterial, ShadowCastingMode.On);

            EditorSceneManager.SaveScene(scene, TestRoomScenePath);
        }

        private static void EnsureTestRoomM5Anchors(Material ambientRoomMaterial, Material ambientBounceMaterial)
        {
            Scene scene = EditorSceneManager.OpenScene(TestRoomScenePath, OpenSceneMode.Single);

            EnsureViewpointMarker("M5AmbientRoomViewpoint", new Vector3(18f, 1.4f, -0.25f));
            EnsureCube("M5AmbientRoom", new Vector3(18f, 1.5f, 0f), new Vector3(6f, 3.2f, 6f), ambientRoomMaterial, ShadowCastingMode.Off);
            EnsureSphere("M5BounceProbeSphere", new Vector3(18f, 0.95f, 0.2f), Vector3.one * 1.2f, ambientBounceMaterial, ShadowCastingMode.On);
            EnsureCube("M5AmbientReferenceCube", new Vector3(15f, 0.85f, 6.6f), Vector3.one * 1.2f, ambientBounceMaterial, ShadowCastingMode.On);

            EditorSceneManager.SaveScene(scene, TestRoomScenePath);
        }

        private static void EnsureLightingLabM5Anchors(Material ambientBounceMaterial)
        {
            Scene scene = EditorSceneManager.OpenScene(LightingLabScenePath, OpenSceneMode.Single);

            EnsureQuad("M5AmbientTopQuad", new Vector3(-4.8f, 1.5f, 4.8f), new Vector3(90f, 0f, 0f), new Vector3(1.8f, 1.8f, 1f), ambientBounceMaterial, ShadowCastingMode.On);
            EnsureQuad("M5AmbientSideQuad", new Vector3(-1.8f, 1.5f, 4.8f), Vector3.zero, new Vector3(1.8f, 1.8f, 1f), ambientBounceMaterial, ShadowCastingMode.On);
            EnsureQuad("M5AmbientBottomQuad", new Vector3(1.2f, 1.5f, 4.8f), new Vector3(-90f, 0f, 0f), new Vector3(1.8f, 1.8f, 1f), ambientBounceMaterial, ShadowCastingMode.On);
            EnsureQuad("M5BounceFacingQuad", new Vector3(4.8f, 1.5f, 4.8f), new Vector3(90f, 0f, 0f), new Vector3(1.8f, 1.8f, 1f), ambientBounceMaterial, ShadowCastingMode.On);
            EnsureQuad("M5BounceOpposingQuad", new Vector3(7.8f, 1.5f, 4.8f), new Vector3(-90f, 0f, 0f), new Vector3(1.8f, 1.8f, 1f), ambientBounceMaterial, ShadowCastingMode.On);

            EditorSceneManager.SaveScene(scene, LightingLabScenePath);
        }

        private static void CreateCamera(Vector3 position, Vector3 eulerAngles)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = position;
            cameraObject.transform.rotation = Quaternion.Euler(eulerAngles);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.72f, 0.79f, 0.88f);
        }

        private static void CreateDirectionalLight()
        {
            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.97f, 0.92f);
            light.shadows = LightShadows.Soft;
        }

        private static void CreateFloor()
        {
            GameObject floorObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floorObject.name = "Floor";
            floorObject.transform.position = Vector3.zero;
            floorObject.transform.localScale = Vector3.one * 0.8f;
        }

        private static void EnsureViewpointMarker(string name, Vector3 position)
        {
            GameObject markerObject = GameObject.Find(name);
            if (markerObject == null)
            {
                markerObject = new GameObject(name);
            }

            markerObject.transform.position = position;
        }

        private static void EnsurePointLight(string name, Vector3 position, Color color, float intensity, float range, bool castShadows)
        {
            GameObject lightObject = GameObject.Find(name);
            if (lightObject == null)
            {
                lightObject = new GameObject(name);
            }

            lightObject.transform.position = position;

            Light light = lightObject.GetComponent<Light>();
            if (light == null)
            {
                light = lightObject.AddComponent<Light>();
            }

            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = castShadows ? LightShadows.Soft : LightShadows.None;
        }

        private static void CreateCube(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetObject.name = name;
            targetObject.transform.position = position;
            targetObject.transform.localScale = scale;
            ApplyMaterial(targetObject, material);
        }

        private static void EnsureCube(string name, Vector3 position, Vector3 scale, Material material, ShadowCastingMode shadowCastingMode)
        {
            GameObject targetObject = EnsurePrimitive(PrimitiveType.Cube, name, position, Quaternion.identity, scale);
            ApplyRendererSettings(targetObject, material, shadowCastingMode);
        }

        private static void CreateSphere(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            targetObject.name = name;
            targetObject.transform.position = position;
            targetObject.transform.localScale = scale;
            ApplyMaterial(targetObject, material);
        }

        private static void EnsureSphere(string name, Vector3 position, Vector3 scale, Material material, ShadowCastingMode shadowCastingMode)
        {
            GameObject targetObject = EnsurePrimitive(PrimitiveType.Sphere, name, position, Quaternion.identity, scale);
            ApplyRendererSettings(targetObject, material, shadowCastingMode);
        }

        private static void CreateQuad(string name, Vector3 position, Vector3 eulerAngles, Vector3 scale, Material material)
        {
            GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            targetObject.name = name;
            targetObject.transform.position = position;
            targetObject.transform.rotation = Quaternion.Euler(eulerAngles);
            targetObject.transform.localScale = scale;
            ApplyMaterial(targetObject, material);
        }

        private static void EnsureQuad(string name, Vector3 position, Vector3 eulerAngles, Vector3 scale, Material material, ShadowCastingMode shadowCastingMode)
        {
            GameObject targetObject = EnsurePrimitive(PrimitiveType.Quad, name, position, Quaternion.Euler(eulerAngles), scale);
            ApplyRendererSettings(targetObject, material, shadowCastingMode);
        }

        private static GameObject EnsurePrimitive(PrimitiveType primitiveType, string name, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            GameObject targetObject = GameObject.Find(name);
            if (targetObject == null)
            {
                targetObject = GameObject.CreatePrimitive(primitiveType);
                targetObject.name = name;
            }

            targetObject.transform.position = position;
            targetObject.transform.rotation = rotation;
            targetObject.transform.localScale = scale;
            return targetObject;
        }

        private static void ApplyMaterial(GameObject targetObject, Material material)
        {
            if (material == null)
            {
                return;
            }

            Renderer renderer = targetObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void ApplyRendererSettings(GameObject targetObject, Material material, ShadowCastingMode shadowCastingMode)
        {
            Renderer renderer = targetObject.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadowCastingMode;
            renderer.receiveShadows = true;
        }

        private static void SaveScene(Scene scene, string sceneName)
        {
            string scenePath = Path.Combine(SceneFolder, sceneName + ".unity").Replace('\\', '/');
            EditorSceneManager.SaveScene(scene, scenePath);
        }
    }
}