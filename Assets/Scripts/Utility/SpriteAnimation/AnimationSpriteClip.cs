using System;
using UnityEngine;

namespace BC.Base
{
    public enum SpriteAnimationPlayMode
    {
        Loop,
        Once,
        OnceToLoop,
    }

    [Serializable]
    public struct AnimationSpriteFrame
    {
        [Tooltip("このフレームで表示するSprite。")]
        public Sprite Sprite;

        [Min(0.0001f)]
        [Tooltip("このフレームを表示する基本秒数。例: 0.1")]
        public float Duration;

        public AnimationSpriteFrame(Sprite sprite, float duration)
        {
            Sprite = sprite;
            Duration = duration <= 0f ? 0.1f : duration;
        }
    }

    [Serializable]
    public struct AnimationSpriteClip
    {
        [Tooltip("デバッグ・Editor表示用の名前。")]
        public string Name;

        [Min(0.0001f)]
        [Tooltip("全体再生速度。1=通常、2=2倍速、0.5=半速。")]
        public float Speed;

        [Tooltip("再生するフレーム一覧。")]
        public AnimationSpriteFrame[] Frames;

        public bool IsValid => Frames != null && Frames.Length > 0;

        public int FrameCount => Frames == null ? 0 : Frames.Length;

        public float GetFrameDuration(int index)
        {
            if (Frames == null)
                throw new InvalidOperationException("AnimationSpriteClip.Frames is null.");

            if ((uint)index >= (uint)Frames.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            float frameDuration = Frames[index].Duration;
            if (frameDuration <= 0f)
                frameDuration = 0.1f;

            float speed = Speed <= 0f ? 1f : Speed;

            return frameDuration / speed;
        }

        public Sprite GetSprite(int index)
        {
            if (Frames == null)
                throw new InvalidOperationException("AnimationSpriteClip.Frames is null.");

            if ((uint)index >= (uint)Frames.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            return Frames[index].Sprite;
        }

        public static AnimationSpriteClip Empty(string name = "Empty")
        {
            return new AnimationSpriteClip
            {
                Name = name,
                Speed = 1f,
                Frames = Array.Empty<AnimationSpriteFrame>(),
            };
        }
    }
}