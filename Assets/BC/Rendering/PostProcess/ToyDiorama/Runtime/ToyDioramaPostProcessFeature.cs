using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BC.Rendering
{
    public sealed class ToyDioramaPostProcessFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader compositeShader;
        [SerializeField] private RenderPassEvent passEvent = RenderPassEvent.AfterRenderingPostProcessing;
        [SerializeField] private ToyDioramaPostProcessSettings settings = new ToyDioramaPostProcessSettings();

        private Material compositeMaterial;
        private ToyDioramaCompositePass compositePass;

        public ToyDioramaPostProcessSettings Settings => settings;

        public override void Create()
        {
            compositeShader ??= Shader.Find("BC/PostProcess/ToyDioramaComposite");

            if (compositeShader != null && (compositeMaterial == null || compositeMaterial.shader != compositeShader))
            {
                CoreUtils.Destroy(compositeMaterial);
                compositeMaterial = CoreUtils.CreateEngineMaterial(compositeShader);
            }

            compositePass = new ToyDioramaCompositePass
            {
                renderPassEvent = passEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings == null || !settings.Enabled || compositeMaterial == null || compositePass == null)
            {
                return;
            }

            CameraType cameraType = renderingData.cameraData.cameraType;

            if (cameraType != CameraType.Game && cameraType != CameraType.SceneView)
            {
                return;
            }

            compositePass.renderPassEvent = passEvent;
            compositePass.Setup(compositeMaterial, settings);
            renderer.EnqueuePass(compositePass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(compositeMaterial);
            compositeMaterial = null;
        }
    }
}