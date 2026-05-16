using UnityEngine;
namespace BC.Utility
{
    // RendererのMaterialPropertyBlockを操作して、マテリアルの色を変更するためのクラス。
    // 主に、ゴールゲートやボーナスアイテムなどのオブジェクトの透明度を制御するために使用します。
    public sealed class MeshMaterialControllerMB : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Renderer targetRenderer;
        [SerializeField, Range(0f, 1f)] private float alpha = 0.5f;

        private MaterialPropertyBlock propertyBlock;
        private Color baseColor = Color.white;

        private void Awake()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<Renderer>();

            propertyBlock = new MaterialPropertyBlock();

            // Materialの元の色を取得
            var mat = targetRenderer.sharedMaterial;
            if (mat != null && mat.HasProperty(BaseColorId))
                baseColor = mat.GetColor(BaseColorId);

            SetAlpha(alpha);
        }

        public void SetAlpha(float value)
        {
            alpha = Mathf.Clamp01(value);

            targetRenderer.GetPropertyBlock(propertyBlock);

            var color = baseColor;
            color.a = alpha;

            propertyBlock.SetColor(BaseColorId, color);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}