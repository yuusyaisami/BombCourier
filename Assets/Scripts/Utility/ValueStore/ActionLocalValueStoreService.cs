namespace BC.Base
{
    // ActionExecution 単位で生きる一時 Store。typed ValueKey と watch handle を既存 ValueStore と揃える。
    public sealed class ActionLocalValueStoreService : ILocalValueStoreService
    {
        private readonly ValueStoreScope scope = new();

        public void Clear()
        {
            scope.Clear();
        }

        public T Get<T>(ValueKeyReference key)
        {
            return scope.Get<T>(key);
        }

        public T Get<T>(ValueKey<T> key)
        {
            return scope.Get(key);
        }

        public ValueWatchHandle<T> GetHandle<T>(ValueKeyReference key)
        {
            return scope.GetHandle<T>(key);
        }

        public ValueWatchHandle<T> GetHandle<T>(ValueKey<T> key)
        {
            return scope.GetHandle(key);
        }

        public bool Set<T>(ValueKeyReference key, T value)
        {
            return scope.Set(key, value);
        }

        public bool Set<T>(ValueKey<T> key, T value)
        {
            return scope.Set(key, value);
        }

        public bool SetAdd(ValueKeyReference key, ValueModifierTagId tag, float value)
        {
            return scope.SetAdd(key, tag, value);
        }

        public bool SetAdd(ValueKey<float> key, ValueModifierTagId tag, float value)
        {
            return scope.SetAdd(key, tag, value);
        }

        public bool SetAdd(ValueKey<int> key, ValueModifierTagId tag, float value)
        {
            return scope.SetAdd(key, tag, value);
        }

        public bool SetMul(ValueKeyReference key, ValueModifierTagId tag, float value)
        {
            return scope.SetMul(key, tag, value);
        }

        public bool SetMul(ValueKey<float> key, ValueModifierTagId tag, float value)
        {
            return scope.SetMul(key, tag, value);
        }

        public bool SetMul(ValueKey<int> key, ValueModifierTagId tag, float value)
        {
            return scope.SetMul(key, tag, value);
        }

        public bool RemoveAdd(ValueKeyReference key, ValueModifierTagId tag)
        {
            return scope.RemoveAdd(key, tag);
        }

        public bool RemoveAdd(ValueKey<float> key, ValueModifierTagId tag)
        {
            return scope.RemoveAdd(key, tag);
        }

        public bool RemoveAdd(ValueKey<int> key, ValueModifierTagId tag)
        {
            return scope.RemoveAdd(key, tag);
        }

        public bool RemoveMul(ValueKeyReference key, ValueModifierTagId tag)
        {
            return scope.RemoveMul(key, tag);
        }

        public bool RemoveMul(ValueKey<float> key, ValueModifierTagId tag)
        {
            return scope.RemoveMul(key, tag);
        }

        public bool RemoveMul(ValueKey<int> key, ValueModifierTagId tag)
        {
            return scope.RemoveMul(key, tag);
        }

        public bool SetBoolModifier(ValueKeyReference key, ValueModifierTagId tag, bool value)
        {
            return scope.SetBoolModifier(key, tag, value);
        }

        public bool SetBoolModifier(ValueKey<bool> key, ValueModifierTagId tag, bool value)
        {
            return scope.SetBoolModifier(key, tag, value);
        }

        public bool RemoveBoolModifier(ValueKeyReference key, ValueModifierTagId tag)
        {
            return scope.RemoveBoolModifier(key, tag);
        }

        public bool RemoveBoolModifier(ValueKey<bool> key, ValueModifierTagId tag)
        {
            return scope.RemoveBoolModifier(key, tag);
        }
    }
}