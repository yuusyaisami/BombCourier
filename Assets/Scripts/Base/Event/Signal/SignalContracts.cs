using System;
using UnityEngine;

namespace BC.Base
{
    public readonly struct SignalId : IEquatable<SignalId>
    {
        public readonly int Value;

        public bool IsValid => Value != 0;

        public SignalId(int value)
        {
            Value = value;
        }

        public bool Equals(SignalId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is SignalId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => Value.ToString();
    }

    public readonly struct Signal : IEquatable<Signal>
    {
        public readonly SignalId Id;
        public readonly string Path;

        public Signal(SignalId id, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Signal path is null or empty.", nameof(path));

            Id = id;
            Path = path;
        }

        public bool Equals(Signal other) => Id.Equals(other.Id);
        public override bool Equals(object obj) => obj is Signal other && Equals(other);
        public override int GetHashCode() => Id.GetHashCode();
        public override string ToString() => $"{Path} ({Id})";
    }

    public static class Signals
    {
        public static class Gimmick
        {
            public static class PressurePlate
            {
                public static readonly Signal Pressed =
                    new Signal(new SignalId(10001), "Gimmick.PressurePlate.Pressed");

                public static readonly Signal Released =
                    new Signal(new SignalId(10002), "Gimmick.PressurePlate.Released");
            }

            public static class MovingPlatform
            {
                public static readonly Signal LayerEnabled =
                    new Signal(new SignalId(10101), "Gimmick.MovingPlatform.LayerEnabled");

                public static readonly Signal LayerDisabled =
                    new Signal(new SignalId(10102), "Gimmick.MovingPlatform.LayerDisabled");

                public static readonly Signal SequenceCompleted =
                    new Signal(new SignalId(10103), "Gimmick.MovingPlatform.SequenceCompleted");
            }
        }
    }

    [Serializable]
    public struct KernelSignalReference : IEquatable<KernelSignalReference>
    {
        [SerializeField] private int id;
        [SerializeField] private string path;

        public SignalId Id => new SignalId(id);
        public int RawId => id;
        public string Path => path;
        public bool IsAssigned => id != 0 || !string.IsNullOrEmpty(path);

        public static KernelSignalReference From(Signal signal)
        {
            return new KernelSignalReference
            {
                id = signal.Id.Value,
                path = signal.Path
            };
        }

        public bool TryResolve(out Signal signal)
        {
            if (SignalRegistry.TryGetDescriptor(this, out SignalDescriptor descriptor))
            {
                signal = descriptor.Signal;
                return true;
            }

            signal = default;
            return false;
        }

        public Signal Resolve()
        {
            if (TryResolve(out Signal signal))
                return signal;

            if (IsAssigned)
                throw new InvalidOperationException($"Signal could not be resolved. Id={id}, Path={path}");

            throw new InvalidOperationException("Signal is not assigned.");
        }

        public bool Equals(KernelSignalReference other)
        {
            return id == other.id && string.Equals(path, other.path, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is KernelSignalReference other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (id * 397) ^ (path != null ? StringComparer.Ordinal.GetHashCode(path) : 0);
            }
        }

        public override string ToString()
        {
            if (SignalRegistry.TryGetDescriptor(this, out SignalDescriptor descriptor))
                return descriptor.DisplayName;

            if (!string.IsNullOrEmpty(path))
                return path;

            return id != 0 ? id.ToString() : "(None)";
        }
    }

    [Serializable]
    public struct EntitySignalReference : IEquatable<EntitySignalReference>
    {
        [SerializeField] private int id;
        [SerializeField] private string path;

        public SignalId Id => new SignalId(id);
        public int RawId => id;
        public string Path => path;
        public bool IsAssigned => id != 0 || !string.IsNullOrEmpty(path);

        public static EntitySignalReference From(Signal signal)
        {
            return new EntitySignalReference
            {
                id = signal.Id.Value,
                path = signal.Path
            };
        }

        public bool TryResolve(out Signal signal)
        {
            if (SignalRegistry.TryGetDescriptor(this, out SignalDescriptor descriptor))
            {
                signal = descriptor.Signal;
                return true;
            }

            signal = default;
            return false;
        }

        public Signal Resolve()
        {
            if (TryResolve(out Signal signal))
                return signal;

            if (IsAssigned)
                throw new InvalidOperationException($"Signal could not be resolved. Id={id}, Path={path}");

            throw new InvalidOperationException("Signal is not assigned.");
        }

        public bool Equals(EntitySignalReference other)
        {
            return id == other.id && string.Equals(path, other.path, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is EntitySignalReference other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (id * 397) ^ (path != null ? StringComparer.Ordinal.GetHashCode(path) : 0);
            }
        }

        public override string ToString()
        {
            if (SignalRegistry.TryGetDescriptor(this, out SignalDescriptor descriptor))
                return descriptor.DisplayName;

            if (!string.IsNullOrEmpty(path))
                return path;

            return id != 0 ? id.ToString() : "(None)";
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class SignalDropdownAttribute : PropertyAttribute
    {
        public SignalDropdownAttribute()
            : this(null, true)
        {
        }

        public SignalDropdownAttribute(string pathPrefix, bool allowNone = true)
        {
            PathPrefix = pathPrefix;
            AllowNone = allowNone;
        }

        public string PathPrefix { get; }
        public bool AllowNone { get; }
    }

    public readonly struct KernelSignalRaisedEvent : IKernelEvent
    {
        public readonly SignalId Signal;
        public readonly string Path;

        public KernelSignalRaisedEvent(SignalId signal, string path)
        {
            Signal = signal;
            Path = path;
        }
    }

    public readonly struct EntitySignalRaisedEvent : IEntityEvent
    {
        public readonly SignalId Signal;
        public readonly string Path;

        public EntitySignalRaisedEvent(SignalId signal, string path)
        {
            Signal = signal;
            Path = path;
        }
    }

    public static class SignalEventBusExtensions
    {
        public static bool RaiseSignal(this IKernelEventBus bus, KernelSignalReference signalReference)
        {
            if (bus == null || !signalReference.TryResolve(out Signal signal))
                return false;

            bus.Publish(new KernelSignalRaisedEvent(signal.Id, signal.Path));
            return true;
        }

        public static bool RaiseSignal(this IEntityEventService service, EntityRef entity, EntitySignalReference signalReference)
        {
            if (service == null || !entity.IsValid || !signalReference.TryResolve(out Signal signal))
                return false;

            service.Publish(entity, new EntitySignalRaisedEvent(signal.Id, signal.Path));
            return true;
        }
    }
}