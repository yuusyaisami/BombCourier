using System;
using System.Collections.Generic;

namespace BC.Base
{
    public sealed class ValueWatchHandle<T>
    {
        private readonly IValueWatchSource<T> source;

        internal ValueWatchHandle(IValueWatchSource<T> source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public T CurrentValue => source.CurrentValue;
        public int Version => source.Version;

        public EventSubscription Subscribe(Action<T> handler)
        {
            return source.Subscribe(handler);
        }

        public bool TryGetChanged(ref int lastSeenVersion, out T value)
        {
            value = source.CurrentValue;

            if (lastSeenVersion == source.Version)
                return false;

            lastSeenVersion = source.Version;
            return true;
        }
    }

    internal interface IValueWatchSource<T>
    {
        T CurrentValue { get; }
        int Version { get; }
        EventSubscription Subscribe(Action<T> handler);
    }

    internal interface IValueWatchNode
    {
        Type ValueType { get; }
        void Refresh();
        void ClearListeners();
    }

    internal sealed class ValueWatchNode<T> : IValueWatchSource<T>, IValueWatchNode
    {
        private readonly Func<T> readCurrentValue;
        private readonly List<Action<T>> listeners = new();
        private T currentValue;

        public Type ValueType => typeof(T);
        public T CurrentValue => currentValue;
        public int Version { get; private set; } = 1;

        public ValueWatchNode(Func<T> readCurrentValue)
        {
            this.readCurrentValue = readCurrentValue ?? throw new ArgumentNullException(nameof(readCurrentValue));
            currentValue = readCurrentValue();
        }

        public EventSubscription Subscribe(Action<T> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            listeners.Add(handler);

            return new EventSubscription(() =>
            {
                listeners.Remove(handler);
            });
        }

        public void Refresh()
        {
            T nextValue = readCurrentValue();

            if (EqualityComparer<T>.Default.Equals(currentValue, nextValue))
                return;

            currentValue = nextValue;
            Version++;

            for (int i = 0; i < listeners.Count; i++)
            {
                listeners[i].Invoke(currentValue);
            }
        }

        public void ClearListeners()
        {
            listeners.Clear();
        }
    }
}