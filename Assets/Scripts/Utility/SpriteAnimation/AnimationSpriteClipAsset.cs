using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace BC.Base
{
    [CreateAssetMenu(
        fileName = "AnimationSpriteClip",
        menuName = "BC/Sprite Animation/Animation Sprite Clip")]
    public sealed class AnimationSpriteClipAsset : ScriptableObject
    {
#if ODIN_INSPECTOR
        [Title("Clip")]
        [InlineProperty]
#endif
        [SerializeField]
        private AnimationSpriteClip clip = new AnimationSpriteClip
        {
            Name = "New Sprite Clip",
            Speed = 1f,
            Frames = new AnimationSpriteFrame[0],
        };

#if ODIN_INSPECTOR
        [Title("Import Helper")]
        [SerializeField]
        private Sprite[] importSprites;

        [SerializeField]
        [MinValue(0.0001f)]
        private float importFrameDuration = 0.1f;

        [Button(ButtonSizes.Medium)]
        private void BuildFramesFromSprites()
        {
            if (importSprites == null)
            {
                clip.Frames = new AnimationSpriteFrame[0];
                return;
            }

            var frames = new AnimationSpriteFrame[importSprites.Length];

            for (int i = 0; i < importSprites.Length; i++)
            {
                frames[i] = new AnimationSpriteFrame(
                    importSprites[i],
                    importFrameDuration
                );
            }

            clip.Frames = frames;
        }

        [Button(ButtonSizes.Medium)]
        private void ClearFrames()
        {
            clip.Frames = new AnimationSpriteFrame[0];
        }
#endif

        public AnimationSpriteClip Clip => clip;
    }
}