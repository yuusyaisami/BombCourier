using System;
using UnityEngine;
using BC.Base;
using Sirenix.OdinInspector;

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

#pragma warning disable CS0649
        [Serializable]
        private struct BlinkSettings
        {
            public bool enabled;
            [ShowIf(nameof(enabled))]
            public FaceExpressionId blinkExpression;
        }

        [Serializable]
        private struct Entry
        {
            public FaceExpressionId expression;
            public Rect pixelRect;
            public BlinkSettings blink;
        }
#pragma warning restore CS0649

        public bool TryGetExpressionUvRect(FaceExpressionId expression, out Rect uvRect)
        {
            if (TryGetEntry(expression, out Entry entry))
                return TryConvertPixelRect(entry.pixelRect, out uvRect);

            uvRect = default;
            return false;
        }

        public bool TryGetBlinkExpression(FaceExpressionId expression, out FaceExpressionId blinkExpression)
        {
            if (TryGetEntry(expression, out Entry entry) && entry.blink.enabled)
            {
                blinkExpression = entry.blink.blinkExpression;
                return true;
            }

            blinkExpression = default;
            return false;
        }

        private bool TryGetEntry(FaceExpressionId expression, out Entry entry)
        {
            if (entries != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    if (entries[i].expression == expression)
                    {
                        entry = entries[i];
                        return true;
                    }
                }
            }

            entry = default;
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