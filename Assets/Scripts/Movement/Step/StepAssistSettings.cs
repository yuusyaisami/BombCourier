using System;

namespace BC.Base
{
    [Serializable]
    public sealed class StepAssistSettings
    {
        public bool Enabled = true;
        public float MaxStepHeight = 0.32f;
        public float ForwardProbeDistance = 0.28f;
        public float LowerProbeHeight = 0.08f;
        public float UpperClearanceSkin = 0.03f;
        public float StepDownProbeDistance = 0.36f;
        public float MinIntentMagnitude = 0.05f;
        public float SnapSpeed = 12.0f;
    }
}
