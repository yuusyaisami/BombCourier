using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BC.Rendering
{
    // TrailUnlit の最低限の作者用アセットを、手作業なしで再生成・検証するための入口。
    // Shader 本体の仕様を変えたときは、この生成物も同じ契約で更新されるようにしている。
    public static class TrailUnlitValidationBootstrapper
    {
        private const string ShaderPath = "Assets/Art/Shader/Particles/TrailUnlit/BC_Particles_TrailUnlit.shader";
        private const string TextureDirectory = "Assets/Art/Textures/Particles/Trails";
        private const string MaterialDirectory = "Assets/Art/Materials/Particles/Trails";
        private const string PrefabDirectory = "Assets/Art/Prefab/Particles/Trails";

        private const string SoftLineTexturePath = TextureDirectory + "/T_Trail_SoftLine.png";
        private const string DustLineTexturePath = TextureDirectory + "/T_Trail_DustNoiseLine.png";
        private const string WindStreakTexturePath = TextureDirectory + "/T_Trail_WindStreak.png";
        private const string LightBeamTexturePath = TextureDirectory + "/T_Trail_LightBeam.png";
        private const string SoftCloudNoisePath = TextureDirectory + "/T_Noise_SoftCloud.png";
        private const string StreakNoisePath = TextureDirectory + "/T_Noise_Streak.png";
        private const string DissolveNoisePath = TextureDirectory + "/T_Noise_Dissolve.png";

        private const string DustMaterialPath = MaterialDirectory + "/M_Trail_Dust_Alpha.mat";
        private const string WindMaterialPath = MaterialDirectory + "/M_Trail_Wind_AlphaScroll.mat";
        private const string LightBeamMaterialPath = MaterialDirectory + "/M_Trail_Light_Additive.mat";
        private const string MagicMaterialPath = MaterialDirectory + "/M_Trail_Magic_Additive.mat";
        private const string SmokeMaterialPath = MaterialDirectory + "/M_Trail_Smoke_Alpha.mat";
        private const string SpeedLineMaterialPath = MaterialDirectory + "/M_Trail_SpeedLine_Alpha.mat";

        private const string DustPrefabPath = PrefabDirectory + "/FX_Dust_Trail.prefab";
        private const string WindPrefabPath = PrefabDirectory + "/FX_Wind_Trail.prefab";
        private const string LightBeamPrefabPath = PrefabDirectory + "/FX_LightBeam_Trail.prefab";
        private const string WindLinePrefabPath = PrefabDirectory + "/FX_Wind_LineRenderer.prefab";
        private const string LightBeamTrailRendererPrefabPath = PrefabDirectory + "/FX_LightBeam_TrailRenderer.prefab";

        [MenuItem("Tools/BC/Particles/TrailUnlit/Bootstrap Validation Assets")]
        public static void BootstrapValidationAssets()
        {
            EnsureDirectories();

            // Texture は外部素材に依存しない検証用。WebGL向けに小さく、圧縮なしで見た目の差を抑える。
            Shader shader = LoadRequiredShader();
            Texture2D softLineTexture = EnsureSoftLineTexture();
            Texture2D dustLineTexture = EnsureDustLineTexture();
            Texture2D windStreakTexture = EnsureWindStreakTexture();
            Texture2D lightBeamTexture = EnsureLightBeamTexture();
            Texture2D softCloudNoise = EnsureSoftCloudNoiseTexture();
            Texture2D streakNoise = EnsureStreakNoiseTexture();
            Texture2D dissolveNoise = EnsureDissolveNoiseTexture();

            // Preset は Material 値の単一の正として扱い、最後に Validator でBlend/Queueを同期する。
            Material dustMaterial = EnsureMaterialAsset(DustMaterialPath, shader, material =>
            {
                TrailUnlitPresetUtility.ApplyPreset(material, "Dust");
                AssignTexture(material, "_BaseMap", dustLineTexture);
                AssignTexture(material, "_NoiseMap", softCloudNoise);
            });

            Material windMaterial = EnsureMaterialAsset(WindMaterialPath, shader, material =>
            {
                TrailUnlitPresetUtility.ApplyPreset(material, "Wind");
                AssignTexture(material, "_BaseMap", windStreakTexture);
                AssignTexture(material, "_NoiseMap", streakNoise);
            });

            Material lightBeamMaterial = EnsureMaterialAsset(LightBeamMaterialPath, shader, material =>
            {
                TrailUnlitPresetUtility.ApplyPreset(material, "LightBeam");
                AssignTexture(material, "_BaseMap", lightBeamTexture);
                AssignTexture(material, "_NoiseMap", softCloudNoise);
            });

            EnsureMaterialAsset(MagicMaterialPath, shader, material =>
            {
                TrailUnlitPresetUtility.ApplyPreset(material, "Magic");
                AssignTexture(material, "_BaseMap", softLineTexture);
                AssignTexture(material, "_NoiseMap", dissolveNoise);
            });

            EnsureMaterialAsset(SmokeMaterialPath, shader, material =>
            {
                TrailUnlitPresetUtility.ApplyPreset(material, "Smoke");
                AssignTexture(material, "_BaseMap", dustLineTexture);
                AssignTexture(material, "_NoiseMap", softCloudNoise);
            });

            EnsureMaterialAsset(SpeedLineMaterialPath, shader, material =>
            {
                TrailUnlitPresetUtility.ApplyPreset(material, "SpeedLine");
                AssignTexture(material, "_BaseMap", windStreakTexture);
                AssignTexture(material, "_NoiseMap", streakNoise);
            });

            EnsureTrailPrefab(
                DustPrefabPath,
                dustMaterial,
                new TrailPrefabProfile(
                    "FX_Dust_Trail",
                    new Color(0.73f, 0.66f, 0.56f, 0.55f),
                    2.4f,
                    0.35f,
                    0.8f,
                    0.6f,
                    42f,
                    0.12f,
                    0.45f,
                    0.08f,
                    0.05f,
                    160));

            EnsureTrailPrefab(
                WindPrefabPath,
                windMaterial,
                new TrailPrefabProfile(
                    "FX_Wind_Trail",
                    new Color(0.72f, 0.88f, 1.0f, 0.46f),
                    1.6f,
                    0.28f,
                    0.5f,
                    1.35f,
                    28f,
                    0.04f,
                    0.32f,
                    0.035f,
                    0.03f,
                    120));

            EnsureTrailPrefab(
                LightBeamPrefabPath,
                lightBeamMaterial,
                new TrailPrefabProfile(
                    "FX_LightBeam_Trail",
                    new Color(0.86f, 0.94f, 1.0f, 0.72f),
                    1.2f,
                    0.22f,
                    0.38f,
                    0.9f,
                    18f,
                    0.02f,
                    0.24f,
                    0.025f,
                    0.02f,
                    80));

                    // ParticleSystem Trails だけでなく、LineRenderer / TrailRenderer でも同じMaterial契約を確認する。
            EnsureLineRendererPrefab(
                WindLinePrefabPath,
                windMaterial);
            EnsureTrailRendererPrefab(
                LightBeamTrailRendererPrefabPath,
                lightBeamMaterial);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TrailUnlit validation assets bootstrapped successfully.");
        }

        [MenuItem("Tools/BC/Particles/TrailUnlit/Validate Generated Assets")]
        public static void ValidateGeneratedAssets()
        {
            Shader shader = LoadRequiredShader();

            // BlendMode は keyword ではなく hidden render-state property で管理するため、ここで実値を固定する。
            AssertMaterialBlendState(DustMaterialPath, shader, TrailUnlitBlendMode.Alpha, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha);
            AssertMaterialBlendState(WindMaterialPath, shader, TrailUnlitBlendMode.Alpha, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha);
            AssertMaterialBlendState(LightBeamMaterialPath, shader, TrailUnlitBlendMode.Additive, BlendMode.SrcAlpha, BlendMode.One);
            AssertMaterialBlendState(MagicMaterialPath, shader, TrailUnlitBlendMode.Additive, BlendMode.SrcAlpha, BlendMode.One);
            AssertMaterialBlendState(SmokeMaterialPath, shader, TrailUnlitBlendMode.Alpha, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha);
            AssertMaterialBlendState(SpeedLineMaterialPath, shader, TrailUnlitBlendMode.Alpha, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha);

            AssertTrailPrefab(DustPrefabPath, DustMaterialPath);
            AssertTrailPrefab(WindPrefabPath, WindMaterialPath);
            AssertTrailPrefab(LightBeamPrefabPath, LightBeamMaterialPath);
            AssertLineRendererPrefab(WindLinePrefabPath, WindMaterialPath);
            AssertTrailRendererPrefab(LightBeamTrailRendererPrefabPath, LightBeamMaterialPath);

            // 色として読むラインテクスチャはsRGB、マスク/ノイズとして読むテクスチャはLinearに固定する。
            AssertTextureImporter(SoftLineTexturePath, true);
            AssertTextureImporter(DustLineTexturePath, true);
            AssertTextureImporter(WindStreakTexturePath, true);
            AssertTextureImporter(LightBeamTexturePath, true);
            AssertTextureImporter(SoftCloudNoisePath, false);
            AssertTextureImporter(StreakNoisePath, false);
            AssertTextureImporter(DissolveNoisePath, false);

            Debug.Log("TrailUnlit generated asset validation completed successfully.");
        }

        private static Shader LoadRequiredShader()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
            {
                throw new FileNotFoundException("TrailUnlit shader asset was not found.", ShaderPath);
            }

            return shader;
        }

        private static void EnsureDirectories()
        {
            Directory.CreateDirectory(TextureDirectory);
            Directory.CreateDirectory(MaterialDirectory);
            Directory.CreateDirectory(PrefabDirectory);
        }

        private static void AssertMaterialBlendState(string materialPath, Shader expectedShader, TrailUnlitBlendMode expectedBlendMode, BlendMode expectedSourceBlend, BlendMode expectedDestinationBlend)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                throw new FileNotFoundException("Expected generated TrailUnlit material.", materialPath);
            }

            if (material.shader != expectedShader)
            {
                throw new InvalidOperationException("TrailUnlit material uses an unexpected shader: " + materialPath);
            }

            AssertFloat(material, "_BlendMode", (float)expectedBlendMode, materialPath);
            AssertFloat(material, "_SrcBlend", (float)expectedSourceBlend, materialPath);
            AssertFloat(material, "_DstBlend", (float)expectedDestinationBlend, materialPath);

            if (material.renderQueue != (int)RenderQueue.Transparent)
            {
                throw new InvalidOperationException("TrailUnlit material should stay in the Transparent render queue: " + materialPath);
            }

            if (material.GetTexture("_BaseMap") == null || material.GetTexture("_NoiseMap") == null)
            {
                throw new InvalidOperationException("TrailUnlit material is missing a generated texture assignment: " + materialPath);
            }
        }

        private static void AssertTrailPrefab(string prefabPath, string materialPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Material expectedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (prefab == null)
            {
                throw new FileNotFoundException("Expected generated TrailUnlit prefab.", prefabPath);
            }

            if (expectedMaterial == null)
            {
                throw new FileNotFoundException("Expected generated TrailUnlit material.", materialPath);
            }

            ParticleSystem particleSystem = prefab.GetComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = prefab.GetComponent<ParticleSystemRenderer>();
            if (particleSystem == null || renderer == null)
            {
                throw new InvalidOperationException("TrailUnlit prefab must contain ParticleSystem and ParticleSystemRenderer: " + prefabPath);
            }

            ParticleSystem.TrailModule trails = particleSystem.trails;
            if (!trails.enabled)
            {
                throw new InvalidOperationException("TrailUnlit prefab must enable the Trails module: " + prefabPath);
            }

            if (renderer.trailMaterial != expectedMaterial)
            {
                throw new InvalidOperationException("TrailUnlit prefab has an unexpected trail material: " + prefabPath);
            }
        }

        private static void AssertLineRendererPrefab(string prefabPath, string materialPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Material expectedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (prefab == null)
            {
                throw new FileNotFoundException("Expected generated TrailUnlit LineRenderer prefab.", prefabPath);
            }

            LineRenderer lineRenderer = prefab.GetComponent<LineRenderer>();
            if (lineRenderer == null || expectedMaterial == null)
            {
                throw new InvalidOperationException("TrailUnlit LineRenderer prefab is missing its renderer or material: " + prefabPath);
            }

            if (lineRenderer.sharedMaterial != expectedMaterial || lineRenderer.positionCount < 2)
            {
                throw new InvalidOperationException("TrailUnlit LineRenderer prefab has an invalid material or point setup: " + prefabPath);
            }
        }

        private static void AssertTrailRendererPrefab(string prefabPath, string materialPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Material expectedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (prefab == null)
            {
                throw new FileNotFoundException("Expected generated TrailUnlit TrailRenderer prefab.", prefabPath);
            }

            TrailRenderer trailRenderer = prefab.GetComponent<TrailRenderer>();
            if (trailRenderer == null || expectedMaterial == null)
            {
                throw new InvalidOperationException("TrailUnlit TrailRenderer prefab is missing its renderer or material: " + prefabPath);
            }

            if (trailRenderer.sharedMaterial != expectedMaterial || trailRenderer.time <= 0f)
            {
                throw new InvalidOperationException("TrailUnlit TrailRenderer prefab has an invalid material or lifetime setup: " + prefabPath);
            }
        }

        private static void AssertTextureImporter(string texturePath, bool expectedSrgb)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (texture == null || textureImporter == null)
            {
                throw new FileNotFoundException("Expected generated TrailUnlit texture and importer.", texturePath);
            }

            if (textureImporter.sRGBTexture != expectedSrgb || textureImporter.wrapMode != TextureWrapMode.Repeat || textureImporter.filterMode != FilterMode.Bilinear || textureImporter.mipmapEnabled)
            {
                throw new InvalidOperationException("TrailUnlit texture importer settings are invalid: " + texturePath);
            }
        }

        private static void AssertFloat(Material material, string propertyName, float expectedValue, string materialPath)
        {
            if (!material.HasProperty(propertyName) || !Mathf.Approximately(material.GetFloat(propertyName), expectedValue))
            {
                throw new InvalidOperationException("TrailUnlit material has an invalid " + propertyName + " value: " + materialPath);
            }
        }

        private static Texture2D EnsureSoftLineTexture()
        {
            return EnsureTextureAsset(SoftLineTexturePath, 128, 32, true, (normalizedX, normalizedY) =>
            {
                float widthMask = 1f - Mathf.Abs(normalizedY * 2f - 1f);
                float lengthMask = Mathf.SmoothStep(0f, 1f, normalizedX) * Mathf.SmoothStep(0f, 1f, 1f - normalizedX);
                float alpha = Mathf.Pow(Mathf.Clamp01(widthMask), 0.65f) * Mathf.Lerp(0.55f, 1f, lengthMask);
                return new Color(1f, 1f, 1f, alpha);
            });
        }

        private static Texture2D EnsureDustLineTexture()
        {
            return EnsureTextureAsset(DustLineTexturePath, 128, 32, true, (normalizedX, normalizedY) =>
            {
                float widthMask = 1f - Mathf.Abs(normalizedY * 2f - 1f);
                float cloudNoise = Mathf.PerlinNoise(normalizedX * 9.5f, normalizedY * 5.0f);
                float brokenLength = Mathf.PerlinNoise(normalizedX * 18.0f + 12.0f, normalizedY * 2.0f);
                float alpha = Mathf.Pow(Mathf.Clamp01(widthMask), 1.15f) * Mathf.Lerp(0.35f, 1f, cloudNoise) * Mathf.Lerp(0.5f, 1f, brokenLength);
                return new Color(1f, 1f, 1f, alpha);
            });
        }

        private static Texture2D EnsureWindStreakTexture()
        {
            return EnsureTextureAsset(WindStreakTexturePath, 128, 32, true, (normalizedX, normalizedY) =>
            {
                float widthMask = 1f - Mathf.Abs(normalizedY * 2f - 1f);
                float stripe = Mathf.Pow(Mathf.Clamp01(Mathf.PerlinNoise(normalizedX * 22f, normalizedY * 4f)), 1.8f);
                float taper = Mathf.SmoothStep(0f, 0.25f, normalizedX) * Mathf.SmoothStep(0f, 0.25f, 1f - normalizedX);
                float alpha = Mathf.Pow(Mathf.Clamp01(widthMask), 2.2f) * stripe * taper;
                return new Color(1f, 1f, 1f, alpha);
            });
        }

        private static Texture2D EnsureLightBeamTexture()
        {
            return EnsureTextureAsset(LightBeamTexturePath, 128, 32, true, (normalizedX, normalizedY) =>
            {
                float widthMask = 1f - Mathf.Abs(normalizedY * 2f - 1f);
                float core = Mathf.Pow(Mathf.Clamp01(widthMask), 0.35f);
                float taper = Mathf.SmoothStep(0f, 0.2f, normalizedX) * Mathf.SmoothStep(0f, 0.2f, 1f - normalizedX);
                return new Color(1f, 1f, 1f, core * taper);
            });
        }

        private static Texture2D EnsureSoftCloudNoiseTexture()
        {
            return EnsureTextureAsset(SoftCloudNoisePath, 128, 128, false, (normalizedX, normalizedY) =>
            {
                float lowFrequency = Mathf.PerlinNoise(normalizedX * 4f, normalizedY * 4f);
                float highFrequency = Mathf.PerlinNoise(normalizedX * 13f + 4f, normalizedY * 13f + 9f);
                float value = Mathf.Lerp(lowFrequency, highFrequency, 0.35f);
                return new Color(value, value, value, 1f);
            });
        }

        private static Texture2D EnsureStreakNoiseTexture()
        {
            return EnsureTextureAsset(StreakNoisePath, 128, 128, false, (normalizedX, normalizedY) =>
            {
                float streak = Mathf.PerlinNoise(normalizedX * 20f, normalizedY * 3f);
                float detail = Mathf.PerlinNoise(normalizedX * 48f + 7f, normalizedY * 7f + 3f);
                float value = Mathf.Clamp01(streak * 0.75f + detail * 0.25f);
                return new Color(value, value, value, 1f);
            });
        }

        private static Texture2D EnsureDissolveNoiseTexture()
        {
            return EnsureTextureAsset(DissolveNoisePath, 128, 128, false, (normalizedX, normalizedY) =>
            {
                float cloud = Mathf.PerlinNoise(normalizedX * 7f + 11f, normalizedY * 7f + 17f);
                float cell = Mathf.PerlinNoise(normalizedX * 24f, normalizedY * 24f);
                float value = Mathf.Clamp01(Mathf.Lerp(cloud, cell, 0.45f));
                return new Color(value, value, value, 1f);
            });
        }

        private static Texture2D EnsureTextureAsset(string texturePath, int width, int height, bool useSrgb, Func<float, float, Color> colorProvider)
        {
            EnsureAssetDirectory(texturePath);

            // PNG書き出し後にImporterを正規化するため、一時TextureはAsset化せず破棄する。
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false, !useSrgb);
            for (int pixelY = 0; pixelY < height; pixelY++)
            {
                float normalizedY = height <= 1 ? 0f : pixelY / (float)(height - 1);
                for (int pixelX = 0; pixelX < width; pixelX++)
                {
                    float normalizedX = width <= 1 ? 0f : pixelX / (float)(width - 1);
                    texture.SetPixel(pixelX, pixelY, colorProvider(normalizedX, normalizedY));
                }
            }

            texture.Apply(false, false);
            File.WriteAllBytes(texturePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
            ConfigureTextureImporter(texturePath, useSrgb);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        }

        private static void ConfigureTextureImporter(string texturePath, bool useSrgb)
        {
            TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (textureImporter == null)
            {
                return;
            }

            textureImporter.textureType = TextureImporterType.Default;
            textureImporter.alphaSource = TextureImporterAlphaSource.FromInput;
            textureImporter.sRGBTexture = useSrgb;
            // Trail/LineRenderer のUV伸縮とスクロールを想定し、端で切れないRepeatにする。
            textureImporter.wrapMode = TextureWrapMode.Repeat;
            textureImporter.filterMode = FilterMode.Bilinear;
            textureImporter.mipmapEnabled = false;
            textureImporter.maxTextureSize = 256;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            textureImporter.SaveAndReimport();
        }

        private static Material EnsureMaterialAsset(string materialPath, Shader shader, Action<Material> configure)
        {
            EnsureAssetDirectory(materialPath);

            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            configure(material);
            // Preset変更後も renderQueue / blend state / 不要keyword を必ず同期する。
            TrailUnlitMaterialValidator.Normalize(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void AssignTexture(Material material, string propertyName, Texture texture)
        {
            if (material != null && texture != null && material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);
            }
        }

        private static void EnsureTrailPrefab(string prefabPath, Material trailMaterial, TrailPrefabProfile profile)
        {
            EnsureAssetDirectory(prefabPath);

            GameObject root = new GameObject(profile.ObjectName);
            try
            {
                ParticleSystem particleSystem = root.AddComponent<ParticleSystem>();
                ConfigureParticleSystem(particleSystem, profile);

                ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
                // Trailだけを見せる検証Prefabなので、Particle本体は描画しない。
                renderer.renderMode = ParticleSystemRenderMode.None;
                renderer.sharedMaterial = trailMaterial;
                renderer.trailMaterial = trailMaterial;
                renderer.sortingFudge = 0.1f;

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                if (savedPrefab == null)
                {
                    throw new IOException("Failed to save TrailUnlit prefab: " + prefabPath);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void EnsureLineRendererPrefab(string prefabPath, Material lineMaterial)
        {
            EnsureAssetDirectory(prefabPath);

            GameObject root = new GameObject("FX_Wind_LineRenderer");
            try
            {
                LineRenderer lineRenderer = root.AddComponent<LineRenderer>();
                lineRenderer.sharedMaterial = lineMaterial;
                lineRenderer.useWorldSpace = false;
                // 直線だけだと幅・Cap・UV stretchの破綻を見落とすため、少し曲げたプレビュー形状にする。
                lineRenderer.positionCount = 5;
                lineRenderer.SetPositions(new[]
                {
                    new Vector3(-1.4f, 0.0f, 0.0f),
                    new Vector3(-0.7f, 0.08f, 0.0f),
                    new Vector3(0.0f, -0.05f, 0.0f),
                    new Vector3(0.7f, 0.06f, 0.0f),
                    new Vector3(1.4f, 0.0f, 0.0f)
                });
                lineRenderer.widthMultiplier = 0.12f;
                lineRenderer.widthCurve = CreateWidthCurve();
                lineRenderer.colorGradient = CreateTrailGradient(new Color(0.72f, 0.88f, 1.0f, 0.55f));
                lineRenderer.textureMode = LineTextureMode.Stretch;
                lineRenderer.alignment = LineAlignment.View;
                lineRenderer.numCornerVertices = 2;
                lineRenderer.numCapVertices = 2;
                lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
                lineRenderer.receiveShadows = false;

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                if (savedPrefab == null)
                {
                    throw new IOException("Failed to save TrailUnlit LineRenderer prefab: " + prefabPath);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void EnsureTrailRendererPrefab(string prefabPath, Material trailMaterial)
        {
            EnsureAssetDirectory(prefabPath);

            GameObject root = new GameObject("FX_LightBeam_TrailRenderer");
            try
            {
                TrailRenderer trailRenderer = root.AddComponent<TrailRenderer>();
                trailRenderer.sharedMaterial = trailMaterial;
                // Scene上に置いた直後でもInspectorで用途が分かるよう、短い光跡の初期値にしておく。
                trailRenderer.time = 0.55f;
                trailRenderer.minVertexDistance = 0.025f;
                trailRenderer.widthMultiplier = 0.1f;
                trailRenderer.widthCurve = CreateWidthCurve();
                trailRenderer.colorGradient = CreateTrailGradient(new Color(0.86f, 0.94f, 1.0f, 0.72f));
                trailRenderer.textureMode = LineTextureMode.Stretch;
                trailRenderer.alignment = LineAlignment.View;
                trailRenderer.numCornerVertices = 2;
                trailRenderer.numCapVertices = 2;
                trailRenderer.emitting = true;
                trailRenderer.shadowCastingMode = ShadowCastingMode.Off;
                trailRenderer.receiveShadows = false;

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                if (savedPrefab == null)
                {
                    throw new IOException("Failed to save TrailUnlit TrailRenderer prefab: " + prefabPath);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ConfigureParticleSystem(ParticleSystem particleSystem, TrailPrefabProfile profile)
        {
            ParticleSystem.MainModule main = particleSystem.main;
            main.duration = profile.Duration;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(profile.StartLifetimeMin, profile.StartLifetimeMax);
            main.startSpeed = profile.StartSpeed;
            main.startSize = profile.StartSize;
            main.startColor = profile.StartColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = profile.MaxParticles;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.rateOverTime = profile.RateOverTime;

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = profile.ShapeAngle;
            shape.radius = profile.ShapeRadius;

            ParticleSystem.TrailModule trails = particleSystem.trails;
            trails.enabled = true;
            trails.mode = ParticleSystemTrailMode.PerParticle;
            trails.ratio = 1f;
            trails.lifetime = profile.TrailLifetime;
            trails.minVertexDistance = profile.MinVertexDistance;
            // Shader側のEdgeFadeはTrail UVを前提にしているため、検証PrefabもStretchで揃える。
            trails.textureMode = ParticleSystemTrailTextureMode.Stretch;
            trails.worldSpace = true;
            trails.dieWithParticles = true;
            trails.sizeAffectsWidth = true;
            trails.inheritParticleColor = true;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(profile.TrailWidth, CreateWidthCurve());
            trails.colorOverTrail = new ParticleSystem.MinMaxGradient(CreateTrailGradient(profile.StartColor));
        }

        private static AnimationCurve CreateWidthCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 1f, 0f, -0.5f),
                new Keyframe(0.75f, 0.7f, -0.4f, -1.2f),
                new Keyframe(1f, 0f, -1f, 0f));
        }

        private static Gradient CreateTrailGradient(Color startColor)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(startColor.r, startColor.g, startColor.b), 0f),
                    new GradientColorKey(new Color(startColor.r, startColor.g, startColor.b), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(startColor.a, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }

        private static void EnsureAssetDirectory(string assetPath)
        {
            string directoryPath = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        private readonly struct TrailPrefabProfile
        {
            internal TrailPrefabProfile(
                string objectName,
                Color startColor,
                float duration,
                float startLifetimeMin,
                float startLifetimeMax,
                float startSpeed,
                float rateOverTime,
                float shapeAngle,
                float trailLifetime,
                float trailWidth,
                float minVertexDistance,
                int maxParticles)
            {
                ObjectName = objectName;
                StartColor = startColor;
                Duration = duration;
                StartLifetimeMin = startLifetimeMin;
                StartLifetimeMax = startLifetimeMax;
                StartSpeed = startSpeed;
                RateOverTime = rateOverTime;
                ShapeAngle = shapeAngle;
                ShapeRadius = 0.08f;
                StartSize = trailWidth;
                TrailLifetime = trailLifetime;
                TrailWidth = trailWidth;
                MinVertexDistance = minVertexDistance;
                MaxParticles = maxParticles;
            }

            internal string ObjectName { get; }

            internal Color StartColor { get; }

            internal float Duration { get; }

            internal float StartLifetimeMin { get; }

            internal float StartLifetimeMax { get; }

            internal float StartSpeed { get; }

            internal float RateOverTime { get; }

            internal float ShapeAngle { get; }

            internal float ShapeRadius { get; }

            internal float StartSize { get; }

            internal float TrailLifetime { get; }

            internal float TrailWidth { get; }

            internal float MinVertexDistance { get; }

            internal int MaxParticles { get; }
        }
    }
}
