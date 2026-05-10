using System;
using UnityEngine;

namespace BC.Base
{
    public interface IAnimationSpriteClipSource
    {
        string DisplayName { get; }
        bool TryGetClip(out AnimationSpriteClip clip);
    }

    [Serializable]
    public sealed class InlineAnimationSpriteClipSource : IAnimationSpriteClipSource
    {
        [SerializeField]
        private AnimationSpriteClip clip = new AnimationSpriteClip
        {
            Name = "Inline Clip",
            Speed = 1f,
            Frames = Array.Empty<AnimationSpriteFrame>(),
        };

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(clip.Name))
                    return clip.Name;

                return "Inline Clip";
            }
        }

        public bool TryGetClip(out AnimationSpriteClip result)
        {
            result = clip;
            return result.IsValid;
        }
    }

    [Serializable]
    public sealed class ScriptableAnimationSpriteClipSource : IAnimationSpriteClipSource
    {
        [SerializeField]
        private AnimationSpriteClipAsset asset;

        public string DisplayName
        {
            get
            {
                if (asset == null)
                    return "Missing Clip Asset";

                return asset.name;
            }
        }

        public bool TryGetClip(out AnimationSpriteClip clip)
        {
            if (asset == null)
            {
                clip = default;
                return false;
            }

            clip = asset.Clip;
            return clip.IsValid;
        }
    }
}