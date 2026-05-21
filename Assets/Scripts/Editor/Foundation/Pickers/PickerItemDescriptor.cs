using System;

namespace BC.Editor.Foundation.Pickers
{
    public readonly struct PickerItemDescriptor : IEquatable<PickerItemDescriptor>
    {
        public PickerItemDescriptor(
            string id,
            string displayName,
            string path = null,
            string tooltip = null,
            bool enabled = true)
        {
            Id = id ?? string.Empty;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? Id : displayName;
            Path = path ?? string.Empty;
            Tooltip = tooltip ?? string.Empty;
            Enabled = enabled;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Path { get; }
        public string Tooltip { get; }
        public bool Enabled { get; }

        public bool Equals(PickerItemDescriptor other)
        {
            return string.Equals(Id, other.Id, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PickerItemDescriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Id ?? string.Empty);
        }
    }
}
