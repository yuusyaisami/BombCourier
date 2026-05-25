using System.Collections.Generic;
using BC.Utility;
using UnityEngine;

namespace BC.Gimmick.MovingPlatform
{
    internal sealed class MovingPlatformRuntimePathVisualizerRegistry
    {
        private readonly Transform owner;
        private readonly List<LayerRuntimePathVisualizer> visualizers = new();

        public MovingPlatformRuntimePathVisualizerRegistry(Transform owner)
        {
            this.owner = owner;
        }

        public void EnsureLayerCount(int layerCount)
        {
            int targetCount = Mathf.Max(0, layerCount);

            while (visualizers.Count < targetCount)
                visualizers.Add(CreateVisualizer(visualizers.Count));

            for (int i = visualizers.Count - 1; i >= targetCount; i--)
            {
                visualizers[i].Dispose();
                visualizers.RemoveAt(i);
            }
        }

        public void ApplyLayer(int layerIndex, Material material, in RuntimePathVisualizationData data)
        {
            if (layerIndex < 0 || layerIndex >= visualizers.Count)
                return;

            LayerRuntimePathVisualizer visualizer = visualizers[layerIndex];
            visualizer.Apply(material, data);
        }

        public void HideAll()
        {
            for (int i = 0; i < visualizers.Count; i++)
                visualizers[i].SetVisible(false);
        }

        public void DisposeAll()
        {
            for (int i = 0; i < visualizers.Count; i++)
                visualizers[i].Dispose();

            visualizers.Clear();
        }

        private LayerRuntimePathVisualizer CreateVisualizer(int layerIndex)
        {
            string objectName = $"MovingPlatform Path Layer {layerIndex}";
            GameObject container = new(objectName);
            container.transform.SetParent(owner, false);

            LineRenderer renderer = container.AddComponent<LineRenderer>();
            renderer.useWorldSpace = true;
            renderer.loop = false;
            renderer.numCapVertices = 4;
            renderer.numCornerVertices = 4;

            LineRendererVisualControllerMB controller = container.AddComponent<LineRendererVisualControllerMB>();
            controller.ConfigureRenderer(renderer);

            return new LayerRuntimePathVisualizer(container, controller);
        }

        private sealed class LayerRuntimePathVisualizer
        {
            private readonly GameObject container;
            private readonly LineRendererVisualControllerMB controller;

            private int lastPointCount = -1;
            private float lastLineWidth = -1.0f;
            private Color lastLineColor = new(-1.0f, -1.0f, -1.0f, -1.0f);
            private bool lastVisible;
            private bool lastIsActive;

            public LayerRuntimePathVisualizer(GameObject container, LineRendererVisualControllerMB controller)
            {
                this.container = container;
                this.controller = controller;
            }

            public void Apply(Material material, in RuntimePathVisualizationData data)
            {
                if (controller == null)
                    return;

                controller.SetMaterial(material);
                controller.SetEmissionSettings(
                    data.EmissionSettings.EnableEmission,
                    data.EmissionSettings.EmissionColor,
                    data.EmissionSettings.ActiveEmissionStrength,
                    data.EmissionSettings.InactiveEmissionStrength,
                    data.EmissionSettings.SyncSimpleBoost,
                    data.EmissionSettings.ActiveSimpleBoostIntensity,
                    data.EmissionSettings.InactiveSimpleBoostIntensity);
                controller.SetInactiveStyle(
                    data.EmissionSettings.DimInactive,
                    data.EmissionSettings.InactiveAlphaMultiplier);

                controller.SetVisible(data.IsVisible);
                if (!data.IsVisible)
                {
                    lastVisible = false;
                    lastPointCount = -1;
                    return;
                }

                bool widthChanged = !Mathf.Approximately(lastLineWidth, data.LineWidth);
                bool colorChanged = lastLineColor != data.LineColor;
                bool pointCountChanged = lastPointCount != (data.Points != null ? data.Points.Count : 0);
                bool activeChanged = lastIsActive != data.IsActiveLayer;

                if (widthChanged)
                {
                    controller.SetLineWidth(data.LineWidth);
                    lastLineWidth = data.LineWidth;
                }

                if (pointCountChanged || !lastVisible)
                {
                    controller.SetLinePoints(data.Points);
                    lastPointCount = data.Points != null ? data.Points.Count : 0;
                }

                if (colorChanged || activeChanged || !lastVisible)
                {
                    controller.ApplyVisual(data.LineColor, data.IsActiveLayer);
                    lastLineColor = data.LineColor;
                    lastIsActive = data.IsActiveLayer;
                }

                lastVisible = true;
            }

            public void SetVisible(bool visible)
            {
                if (controller == null)
                    return;

                controller.SetVisible(visible);
                lastVisible = visible;
                if (!visible)
                    lastPointCount = -1;
            }

            public void Dispose()
            {
                if (controller != null)
                    controller.DisposeOwnedResources();

                if (container == null)
                    return;

                if (Application.isPlaying)
                    Object.Destroy(container);
                else
                    Object.DestroyImmediate(container);
            }
        }
    }
}
