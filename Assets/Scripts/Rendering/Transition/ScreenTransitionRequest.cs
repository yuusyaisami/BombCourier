using UnityEngine;

namespace BC.Rendering.Transition
{
    public readonly struct ScreenTransitionRequest
    {
        public ScreenTransitionRequest(
            ScreenTransitionProfileSO profile,
            Texture explicitToTexture = null,
            float? overrideDuration = null,
            bool captureFromCurrentFrame = true,
            bool waitUntilToReady = false)
        {
            Profile = profile;
            ExplicitToTexture = explicitToTexture;
            OverrideDuration = overrideDuration;
            CaptureFromCurrentFrame = captureFromCurrentFrame;
            WaitUntilToReady = waitUntilToReady;
        }

        public ScreenTransitionProfileSO Profile { get; }
        public Texture ExplicitToTexture { get; }
        public float? OverrideDuration { get; }
        public bool CaptureFromCurrentFrame { get; }
        public bool WaitUntilToReady { get; }
    }
}
