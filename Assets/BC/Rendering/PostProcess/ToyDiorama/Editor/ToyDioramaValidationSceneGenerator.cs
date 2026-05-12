using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BC.Rendering.Editor
{
    public static class ToyDioramaValidationSceneGenerator
    {
        private const string SceneFolder = "Assets/Scenes/ToyDiorama";
        private const string ValidationRootFolder = "Assets/BC/Rendering/PostProcess/ToyDiorama/Validation";
        private const string MaterialFolder = ValidationRootFolder + "/Materials";

        [MenuItem("Tools/BC/Rendering/Generate ToyDiorama Validation Scenes")]
        public static void GenerateAll()
        {
            GenerateAllInternal();
        }

        public static void GenerateAllBatch()
        {
            GenerateAllInternal();
        }

        private static void GenerateAllInternal()
        {
            EnsureFolder(SceneFolder);
            EnsureFolder(ValidationRootFolder);
            EnsureFolder(MaterialFolder);

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                ValidationMaterials materials = EnsureValidationMaterials();

                GenerateColorLab(materials);
                GenerateDepthLab(materials);
                GenerateBloomLab(materials);
                GenerateGameplayLab(materials);

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

        private static void GenerateColorLab(ValidationMaterials materials)
        {
            Scene scene = CreateBaseScene(
                "ToyDiorama_ColorLab",
                "ColorLab",
                "Preset / tint / pastel balance validation.",
                new Vector3(0f, 1.8f, -8f),
                new Vector3(10f, 0f, 0f),
                new Color(0.76f, 0.82f, 0.90f),
                1.1f);

            CreatePedestal(new Vector3(-2.5f, 0.5f, 1.5f), materials.MatteBlue);
            CreatePedestal(new Vector3(0f, 0.5f, 0f), materials.MatteCream);
            CreatePedestal(new Vector3(2.5f, 0.5f, 1.5f), materials.MatteRed);
            CreateWall(new Vector3(0f, 2f, 5f), new Vector3(10f, 4f, 0.5f), materials.MatteCream, "BackWall");
            CreateWall(new Vector3(-5f, 2f, 0f), new Vector3(0.5f, 4f, 10f), materials.MatteBlue, "LeftWall");
            CreateWorldSpaceCanvas(new Vector3(0f, 2.2f, 2.2f), "World Space UI", new Vector2(1.8f, 0.6f));

            SaveScene(scene, "ToyDiorama_ColorLab");
        }

        private static void GenerateDepthLab(ValidationMaterials materials)
        {
            Scene scene = CreateBaseScene(
                "ToyDiorama_DepthLab",
                "DepthLab",
                "Depth haze near / mid / far separation validation.",
                new Vector3(0f, 2.5f, -14f),
                new Vector3(8f, 0f, 0f),
                new Color(0.82f, 0.88f, 0.94f),
                1.0f);

            CreateDepthMarker(new Vector3(0f, 1.2f, -1f), new Vector3(2f, 2f, 2f), materials.MatteCream, "NearMarker");
            CreateDepthMarker(new Vector3(-2.5f, 1.4f, 8f), new Vector3(2.6f, 2.6f, 2.6f), materials.MatteBlue, "MidMarker");
            CreateDepthMarker(new Vector3(3f, 1.8f, 18f), new Vector3(3.4f, 3.4f, 3.4f), materials.MatteRed, "FarMarker");
            CreateDepthMarker(new Vector3(0f, 2.2f, 30f), new Vector3(5f, 5f, 5f), materials.MatteCream, "HorizonMarker");
            CreateWorldSpaceCanvas(new Vector3(0f, 2.8f, 8f), "Depth Haze Target", new Vector2(2f, 0.6f));

            SaveScene(scene, "ToyDiorama_DepthLab");
        }

        private static void GenerateBloomLab(ValidationMaterials materials)
        {
            Scene scene = CreateBaseScene(
                "ToyDiorama_BloomLab",
                "BloomLab",
                "Bloom / halation threshold and radius validation.",
                new Vector3(0f, 2.1f, -9f),
                new Vector3(12f, 0f, 0f),
                new Color(0.10f, 0.10f, 0.12f),
                0.15f);

            CreateWall(new Vector3(0f, 2f, 6f), new Vector3(12f, 4f, 0.5f), materials.MatteBlue, "BloomBackWall");
            CreateLightTarget(new Vector3(-2.25f, 1.2f, 0f), materials.EmissiveWarm, "WarmBloomTarget");
            CreateLightTarget(new Vector3(2.25f, 1.2f, 0f), materials.EmissiveCool, "CoolBloomTarget");
            CreatePointLight(new Vector3(-2.25f, 2.8f, -0.5f), new Color(1f, 0.78f, 0.56f), 14f, 8f, "WarmPointLight");
            CreatePointLight(new Vector3(2.25f, 2.8f, -0.5f), new Color(0.64f, 0.86f, 1f), 12f, 8f, "CoolPointLight");
            CreateWorldSpaceCanvas(new Vector3(0f, 2.6f, 1.5f), "Bloom Target", new Vector2(1.8f, 0.6f));

            SaveScene(scene, "ToyDiorama_BloomLab");
        }

        private static void GenerateGameplayLab(ValidationMaterials materials)
        {
            Scene scene = CreateBaseScene(
                "ToyDiorama_GameplayLab",
                "GameplayLab",
                "Canonical gameplay camera, UI, and visibility validation.",
                new Vector3(0f, 2f, -10f),
                new Vector3(10f, 0f, 0f),
                new Color(0.73f, 0.80f, 0.88f),
                1.05f);

            CreateGround(new Vector3(0f, 0f, 8f), new Vector3(2.2f, 1f, 2.4f), materials.MatteCream, "GameplayGround");
            CreateRamp(new Vector3(-2.5f, 0.5f, 2.5f), new Vector3(20f, 0f, 0f), materials.MatteBlue, "GameplayRamp");
            CreateCharacterProxy(new Vector3(0f, 1f, 1.5f), materials.MatteRed, "CharacterProxy");
            CreateCoverBlock(new Vector3(3f, 1f, 4f), new Vector3(1.5f, 2f, 1.5f), materials.MatteBlue, "CoverBlockA");
            CreateCoverBlock(new Vector3(-3f, 0.75f, 6f), new Vector3(2.5f, 1.5f, 1.5f), materials.MatteCream, "CoverBlockB");
            CreateWorldSpaceCanvas(new Vector3(0f, 2.3f, 3.5f), "Objective Marker", new Vector2(1.8f, 0.6f));

            SaveScene(scene, "ToyDiorama_GameplayLab");
        }

        private static Scene CreateBaseScene(
            string sceneName,
            string overlayTitle,
            string overlaySubtitle,
            Vector3 cameraPosition,
            Vector3 cameraEuler,
            Color ambientColor,
            float directionalIntensity)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = sceneName;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor * 0.35f;

            CreateCamera(cameraPosition, cameraEuler);
            CreateDirectionalLight(new Color(1f, 0.96f, 0.92f), directionalIntensity);
            CreateGround(Vector3.zero, new Vector3(2f, 1f, 2f), null, "Ground");
            CreateOverlayCanvas(overlayTitle, overlaySubtitle);

            return scene;
        }

        private static void CreateCamera(Vector3 position, Vector3 eulerAngles)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = position;
            cameraObject.transform.rotation = Quaternion.Euler(eulerAngles);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.83f, 0.87f, 0.93f);
            camera.allowHDR = true;

            UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.renderType = CameraRenderType.Base;
            cameraData.SetRenderer(0);
        }

        private static void CreateDirectionalLight(Color color, float intensity)
        {
            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
        }

        private static void CreateOverlayCanvas(string title, string subtitle)
        {
            GameObject canvasObject = new GameObject("UIScreenCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            CreateLabel("Header", canvasObject.transform, title, new Vector2(0f, -40f), 28, FontStyle.Bold);
            CreateLabel("SubHeader", canvasObject.transform, subtitle, new Vector2(0f, -80f), 18, FontStyle.Normal);
        }

        private static void CreateWorldSpaceCanvas(Vector3 position, string text, Vector2 size)
        {
            GameObject canvasObject = new GameObject("WorldSpaceCanvas");
            canvasObject.transform.position = position;
            canvasObject.transform.rotation = Quaternion.identity;
            canvasObject.transform.localScale = Vector3.one * 0.01f;

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            RectTransform rectTransform = canvas.GetComponent<RectTransform>();
            rectTransform.sizeDelta = size * 100f;

            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            CreateLabel("WorldLabel", canvasObject.transform, text, Vector2.zero, 28, FontStyle.Bold);
        }

        private static void CreateLabel(string name, Transform parent, string text, Vector2 anchoredPosition, int fontSize, FontStyle fontStyle)
        {
            GameObject labelObject = new GameObject(name);
            labelObject.transform.SetParent(parent, false);

            RectTransform rectTransform = labelObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.sizeDelta = new Vector2(720f, 56f);
            rectTransform.anchoredPosition = anchoredPosition;

            Text label = labelObject.AddComponent<Text>();
            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.color = Color.white;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static void CreatePedestal(Vector3 position, Material material)
        {
            GameObject baseObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseObject.name = "PedestalBase";
            baseObject.transform.position = position + new Vector3(0f, -0.35f, 0f);
            baseObject.transform.localScale = new Vector3(1.4f, 0.7f, 1.4f);
            ApplyMaterial(baseObject, material);

            GameObject topObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            topObject.name = "PedestalTop";
            topObject.transform.position = position + new Vector3(0f, 0.65f, 0f);
            topObject.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
            ApplyMaterial(topObject, material);
        }

        private static void CreateDepthMarker(Vector3 position, Vector3 scale, Material material, string name)
        {
            GameObject markerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            markerObject.name = name;
            markerObject.transform.position = position;
            markerObject.transform.localScale = scale;
            ApplyMaterial(markerObject, material);
        }

        private static void CreateLightTarget(Vector3 position, Material material, string name)
        {
            GameObject lightTargetObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lightTargetObject.name = name;
            lightTargetObject.transform.position = position;
            lightTargetObject.transform.localScale = Vector3.one * 1.4f;
            ApplyMaterial(lightTargetObject, material);
        }

        private static void CreatePointLight(Vector3 position, Color color, float intensity, float range, string name)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.position = position;

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        private static void CreateCharacterProxy(Vector3 position, Material material, string name)
        {
            GameObject proxyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            proxyObject.name = name;
            proxyObject.transform.position = position;
            proxyObject.transform.localScale = new Vector3(1f, 1.9f, 1f);
            ApplyMaterial(proxyObject, material);
        }

        private static void CreateCoverBlock(Vector3 position, Vector3 scale, Material material, string name)
        {
            GameObject blockObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blockObject.name = name;
            blockObject.transform.position = position;
            blockObject.transform.localScale = scale;
            ApplyMaterial(blockObject, material);
        }

        private static void CreateRamp(Vector3 position, Vector3 eulerAngles, Material material, string name)
        {
            GameObject rampObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rampObject.name = name;
            rampObject.transform.position = position;
            rampObject.transform.rotation = Quaternion.Euler(eulerAngles);
            rampObject.transform.localScale = new Vector3(3.5f, 0.8f, 2.5f);
            ApplyMaterial(rampObject, material);
        }

        private static void CreateWall(Vector3 position, Vector3 scale, Material material, string name)
        {
            GameObject wallObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallObject.name = name;
            wallObject.transform.position = position;
            wallObject.transform.localScale = scale;
            ApplyMaterial(wallObject, material);
        }

        private static void CreateGround(Vector3 position, Vector3 scale, Material material, string name)
        {
            GameObject groundObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundObject.name = name;
            groundObject.transform.position = position;
            groundObject.transform.localScale = scale;

            if (material != null)
            {
                ApplyMaterial(groundObject, material);
            }
        }

        private static void ApplyMaterial(GameObject target, Material material)
        {
            if (material == null)
            {
                return;
            }

            Renderer renderer = target.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void SaveScene(Scene scene, string sceneName)
        {
            string scenePath = Path.Combine(SceneFolder, sceneName + ".unity").Replace('\\', '/');
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"Generated ToyDiorama validation scene: {scenePath}");
        }

        private static ValidationMaterials EnsureValidationMaterials()
        {
            return new ValidationMaterials(
                GetOrCreateLitMaterial("TD_Validation_MatteCream", new Color(0.94f, 0.92f, 0.86f), Color.black),
                GetOrCreateLitMaterial("TD_Validation_MatteBlue", new Color(0.42f, 0.56f, 0.74f), Color.black),
                GetOrCreateLitMaterial("TD_Validation_MatteRed", new Color(0.78f, 0.38f, 0.36f), Color.black),
                GetOrCreateLitMaterial("TD_Validation_EmissiveWarm", new Color(0.92f, 0.72f, 0.52f), new Color(1.4f, 0.75f, 0.42f)),
                GetOrCreateLitMaterial("TD_Validation_EmissiveCool", new Color(0.60f, 0.80f, 0.98f), new Color(0.45f, 0.82f, 1.6f)));
        }

        private static Material GetOrCreateLitMaterial(string assetName, Color baseColor, Color emissionColor)
        {
            string assetPath = $"{MaterialFolder}/{assetName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, assetPath);
            }

            material.name = assetName;
            material.SetColor("_BaseColor", baseColor);
            material.SetColor("_EmissionColor", emissionColor);

            if (emissionColor.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parentFolder = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string folderName = Path.GetFileName(folderPath);

            if (!string.IsNullOrEmpty(parentFolder) && !AssetDatabase.IsValidFolder(parentFolder))
            {
                EnsureFolder(parentFolder);
            }

            AssetDatabase.CreateFolder(parentFolder, folderName);
        }

        private readonly struct ValidationMaterials
        {
            public ValidationMaterials(
                Material matteCream,
                Material matteBlue,
                Material matteRed,
                Material emissiveWarm,
                Material emissiveCool)
            {
                MatteCream = matteCream;
                MatteBlue = matteBlue;
                MatteRed = matteRed;
                EmissiveWarm = emissiveWarm;
                EmissiveCool = emissiveCool;
            }

            public Material MatteCream { get; }

            public Material MatteBlue { get; }

            public Material MatteRed { get; }

            public Material EmissiveWarm { get; }

            public Material EmissiveCool { get; }
        }
    }
}