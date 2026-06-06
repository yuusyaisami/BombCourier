using System;

namespace BC.Stage.Snapshot
{
    /// <summary>
    /// 復元対象を一意に識別するための安定ID（32桁hexのGUID文字列ラッパ）。
    /// 参照ではなくIDでスナップショットを照合するための土台。
    /// </summary>
    public readonly struct StableObjectId : IEquatable<StableObjectId>
    {
        public readonly string Value;

        public StableObjectId(string value)
        {
            Value = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public static StableObjectId New()
        {
            return new StableObjectId(Guid.NewGuid().ToString("N"));
        }

        public bool Equals(StableObjectId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is StableObjectId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value == null ? 0 : Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }
    }
}
