using System;
using UnityEngine;
using BC.Base;

namespace BC.Character
{
    [CreateAssetMenu(
        fileName = "FaceExpressionUvSet",
        menuName = "BC/Character/Face Expression UV Set")]
    public sealed class FaceExpressionUvSet : ScriptableObject
    {
        [Header("Atlas")]
        [SerializeField] private Texture2D atlasTexture;

        [Tooltip("画像編集ソフト基準の左上原点pixel座標で入力するなら true")]
        [SerializeField] private bool pixelRectUsesTopLeftOrigin = true;

        [Header("Expressions")]
        [SerializeField] private Entry[] entries;

        [Serializable]
        private struct Entry
        {
            public FaceExpressionId expression;
            public Rect pixelRect;
        }

        public bool TryGetExpressionUvRect(FaceExpressionId expression, out Rect uvRect)
        {
            if (entries != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    if (entries[i].expression == expression)
                    {
                        return TryConvertPixelRect(entries[i].pixelRect, out uvRect);
                    }
                }
            }

            uvRect = default;
            return false;
        }

        private bool TryConvertPixelRect(Rect pixelRect, out Rect uvRect)
        {
            if (atlasTexture == null)
            {
                uvRect = default;
                return false;
            }

            float textureWidth = atlasTexture.width;
            float textureHeight = atlasTexture.height;

            float x = pixelRect.x;
            float y = pixelRect.y;

            if (pixelRectUsesTopLeftOrigin)
            {
                y = textureHeight - pixelRect.y - pixelRect.height;
            }

            uvRect = new Rect(
                x / textureWidth,
                y / textureHeight,
                pixelRect.width / textureWidth,
                pixelRect.height / textureHeight
            );

            return uvRect.width > 0f && uvRect.height > 0f;
        }
    }
}