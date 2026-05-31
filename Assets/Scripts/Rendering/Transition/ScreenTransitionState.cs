namespace BC.Rendering.Transition
{
    public enum ScreenTransitionState
    {
        Idle = 0,
        CaptureFrom = 1,
        HoldFrom = 2,
        LoadOrPrepareTo = 3,
        Transitioning = 4,
        Complete = 5,
    }
}
