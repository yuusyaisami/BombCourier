using UnityEngine;

namespace BC.Base
{
    public readonly struct PositionCorrection
    {
        public PositionCorrection(Vector3 delta)
        {
            Delta = delta;
            HasCorrection = delta.sqrMagnitude > 0.0000001f;
        }

        public Vector3 Delta { get; }
        public bool HasCorrection { get; }

        public static PositionCorrection None => default;

        public PositionCorrection Combine(in PositionCorrection other)
        {
            if (!HasCorrection)
                return other;

            if (!other.HasCorrection)
                return this;

            return new PositionCorrection(Delta + other.Delta);
        }
    }
}
