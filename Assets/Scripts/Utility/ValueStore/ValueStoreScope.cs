using System;
using System.Collections.Generic;

namespace BC.Base
{
    internal sealed class ValueStoreScope
    {
        private readonly Dictionary<ValueKeyId, ValueSlot> slotsByKey = new();
        private readonly Dictionary<ValueKeyId, IValueWatchNode> watchNodesByKey = new();

        public void Clear()
        {
            foreach (IValueWatchNode watchNode in watchNodesByKey.Values)
            {
                watchNode.ClearListeners();
            }

            watchNodesByKey.Clear();
            slotsByKey.Clear();
        }

        public T Get<T>(ValueKeyReference key)
        {
            return Get(key.Resolve<T>());
        }

        public T Get<T>(ValueKey<T> key)
        {
            if (typeof(T) == typeof(float))
            {
                ValueKey<float> floatKey = UnsafeCastKey<float, T>(key);

                if (floatKey.CompositionMode == ValueCompositionMode.Raw)
                {
                    var rawFloatSlot = GetOrCreateRawSlot(floatKey);
                    object rawResult = rawFloatSlot.Get();
                    return (T)rawResult;
                }

                if (floatKey.CompositionMode == ValueCompositionMode.NumericAddMul)
                {
                    var slot = GetOrCreateFloatSlot(floatKey);
                    object result = slot.Get();
                    return (T)result;
                }

                throw new InvalidOperationException(
                    $"Value key composition mode mismatch. Path={key.Path}, Key={key.Id}, Expected={ValueCompositionMode.Raw} or {ValueCompositionMode.NumericAddMul}, Actual={key.CompositionMode}");
            }

            if (typeof(T) == typeof(int))
            {
                ValueKey<int> intKey = UnsafeCastKey<int, T>(key);

                if (intKey.CompositionMode == ValueCompositionMode.Raw)
                {
                    var rawIntSlot = GetOrCreateRawSlot(intKey);
                    object result = rawIntSlot.Get();
                    return (T)result;
                }

                if (intKey.CompositionMode == ValueCompositionMode.NumericAddMul)
                {
                    var slot = GetOrCreateIntSlot(intKey);
                    object result = slot.Get();
                    return (T)result;
                }

                throw new InvalidOperationException(
                    $"Value key composition mode mismatch. Path={key.Path}, Key={key.Id}, Expected={ValueCompositionMode.Raw} or {ValueCompositionMode.NumericAddMul}, Actual={key.CompositionMode}");
            }

            if (typeof(T) == typeof(bool))
            {
                ValueKey<bool> boolKey = UnsafeCastKey<bool, T>(key);

                if (boolKey.CompositionMode == ValueCompositionMode.Raw)
                {
                    var rawBoolSlot = GetOrCreateRawSlot(boolKey);
                    object rawResult = rawBoolSlot.Get();
                    return (T)rawResult;
                }

                if (boolKey.CompositionMode == ValueCompositionMode.BoolAnd ||
                    boolKey.CompositionMode == ValueCompositionMode.BoolOr)
                {
                    var slot = GetOrCreateBoolSlot(boolKey);
                    object result = slot.Get();
                    return (T)result;
                }

                throw new InvalidOperationException(
                    $"Value key composition mode mismatch. Path={key.Path}, Key={key.Id}, Expected={ValueCompositionMode.Raw}, {ValueCompositionMode.BoolAnd} or {ValueCompositionMode.BoolOr}, Actual={key.CompositionMode}");
            }

            var raw = GetOrCreateRawSlot(key);
            return raw.Get();
        }

        public ValueWatchHandle<T> GetHandle<T>(ValueKeyReference key)
        {
            return GetHandle(key.Resolve<T>());
        }

        public ValueWatchHandle<T> GetHandle<T>(ValueKey<T> key)
        {
            if (typeof(T) == typeof(float))
            {
                ValueKey<float> floatKey = UnsafeCastKey<float, T>(key);

                if (floatKey.CompositionMode == ValueCompositionMode.Raw)
                {
                    var rawFloatSlot = GetOrCreateRawSlot(floatKey);
                    return (ValueWatchHandle<T>)(object)new ValueWatchHandle<float>(GetOrCreateWatchNode(floatKey, rawFloatSlot.Get));
                }

                if (floatKey.CompositionMode == ValueCompositionMode.NumericAddMul)
                {
                    return CreateFloatHandle<T>(floatKey);
                }

                throw new InvalidOperationException(
                    $"Value key composition mode mismatch. Path={key.Path}, Key={key.Id}, Expected={ValueCompositionMode.Raw} or {ValueCompositionMode.NumericAddMul}, Actual={key.CompositionMode}");
            }

            if (typeof(T) == typeof(int))
            {
                ValueKey<int> intKey = UnsafeCastKey<int, T>(key);

                if (intKey.CompositionMode == ValueCompositionMode.Raw)
                {
                    var rawIntSlot = GetOrCreateRawSlot(intKey);
                    return (ValueWatchHandle<T>)(object)new ValueWatchHandle<int>(GetOrCreateWatchNode(intKey, rawIntSlot.Get));
                }

                if (intKey.CompositionMode == ValueCompositionMode.NumericAddMul)
                {
                    return CreateIntHandle<T>(intKey);
                }

                throw new InvalidOperationException(
                    $"Value key composition mode mismatch. Path={key.Path}, Key={key.Id}, Expected={ValueCompositionMode.Raw} or {ValueCompositionMode.NumericAddMul}, Actual={key.CompositionMode}");
            }

            if (typeof(T) == typeof(bool))
            {
                ValueKey<bool> boolKey = UnsafeCastKey<bool, T>(key);

                if (boolKey.CompositionMode == ValueCompositionMode.Raw)
                {
                    var rawBoolSlot = GetOrCreateRawSlot(boolKey);
                    return (ValueWatchHandle<T>)(object)new ValueWatchHandle<bool>(GetOrCreateWatchNode(boolKey, rawBoolSlot.Get));
                }

                if (boolKey.CompositionMode == ValueCompositionMode.BoolAnd ||
                    boolKey.CompositionMode == ValueCompositionMode.BoolOr)
                {
                    return CreateBoolHandle<T>(boolKey);
                }

                throw new InvalidOperationException(
                    $"Value key composition mode mismatch. Path={key.Path}, Key={key.Id}, Expected={ValueCompositionMode.Raw}, {ValueCompositionMode.BoolAnd} or {ValueCompositionMode.BoolOr}, Actual={key.CompositionMode}");
            }

            var rawSlot = GetOrCreateRawSlot(key);
            return new ValueWatchHandle<T>(GetOrCreateWatchNode(key, rawSlot.Get));
        }

        public bool Set<T>(ValueKeyReference key, T value)
        {
            return Set(key.Resolve<T>(), value);
        }

        public bool Set<T>(ValueKey<T> key, T value)
        {
            if (typeof(T) == typeof(float))
            {
                ValueKey<float> floatKey = UnsafeCastKey<float, T>(key);
                float floatValue = (float)(object)value;

                if (floatKey.CompositionMode == ValueCompositionMode.Raw)
                {
                    var rawFloatSlot = GetOrCreateRawSlot(floatKey);
                    bool rawChanged = rawFloatSlot.Set(floatValue);
                    NotifyWatchNodeChanged(key.Id, rawChanged);
                    return rawChanged;
                }

                if (floatKey.CompositionMode == ValueCompositionMode.NumericAddMul)
                {
                    var slot = GetOrCreateFloatSlot(floatKey);
                    bool changed = slot.SetBase(floatValue);
                    NotifyWatchNodeChanged(key.Id, changed);
                    return changed;
                }

                throw new InvalidOperationException(
                    $"Value key composition mode mismatch. Path={key.Path}, Key={key.Id}, Expected={ValueCompositionMode.Raw} or {ValueCompositionMode.NumericAddMul}, Actual={key.CompositionMode}");
            }

            if (typeof(T) == typeof(int))
            {
                ValueKey<int> intKey = UnsafeCastKey<int, T>(key);
                int intValue = (int)(object)value;

                if (intKey.CompositionMode == ValueCompositionMode.Raw)
                {
                    var rawValueSlot = GetOrCreateRawSlot(intKey);
                    bool changed = rawValueSlot.Set(intValue);
                    NotifyWatchNodeChanged(key.Id, changed);
                    return changed;
                }

                if (intKey.CompositionMode == ValueCompositionMode.NumericAddMul)
                {
                    var slot = GetOrCreateIntSlot(intKey);
                    bool changed = slot.SetBase(intValue);
                    NotifyWatchNodeChanged(key.Id, changed);
                    return changed;
                }

                throw new InvalidOperationException(
                    $"Value key composition mode mismatch. Path={key.Path}, Key={key.Id}, Expected={ValueCompositionMode.Raw} or {ValueCompositionMode.NumericAddMul}, Actual={key.CompositionMode}");
            }

            if (typeof(T) == typeof(bool))
            {
                ValueKey<bool> boolKey = UnsafeCastKey<bool, T>(key);
                bool boolValue = (bool)(object)value;

                if (boolKey.CompositionMode == ValueCompositionMode.Raw)
                {
                    var rawBoolSlot = GetOrCreateRawSlot(boolKey);
                    bool rawChanged = rawBoolSlot.Set(boolValue);
                    NotifyWatchNodeChanged(key.Id, rawChanged);
                    return rawChanged;
                }

                if (boolKey.CompositionMode == ValueCompositionMode.BoolAnd ||
                    boolKey.CompositionMode == ValueCompositionMode.BoolOr)
                {
                    var slot = GetOrCreateBoolSlot(boolKey);
                    bool changed = slot.SetBase(boolValue);
                    NotifyWatchNodeChanged(key.Id, changed);
                    return changed;
                }

                throw new InvalidOperationException(
                    $"Value key composition mode mismatch. Path={key.Path}, Key={key.Id}, Expected={ValueCompositionMode.Raw}, {ValueCompositionMode.BoolAnd} or {ValueCompositionMode.BoolOr}, Actual={key.CompositionMode}");
            }

            var fallbackRawSlot = GetOrCreateRawSlot(key);
            bool fallbackRawChanged = fallbackRawSlot.Set(value);
            NotifyWatchNodeChanged(key.Id, fallbackRawChanged);
            return fallbackRawChanged;
        }

        public bool SetBoolModifier(ValueKeyReference key, ValueModifierTagId tag, bool value)
        {
            return SetBoolModifier(key.Resolve<bool>(), tag, value);
        }

        public bool SetBoolModifier(ValueKey<bool> key, ValueModifierTagId tag, bool value)
        {
            bool changed = GetOrCreateBoolSlot(key).SetModifier(tag, value);
            NotifyWatchNodeChanged(key.Id, changed);
            return changed;
        }

        public bool RemoveBoolModifier(ValueKeyReference key, ValueModifierTagId tag)
        {
            return RemoveBoolModifier(key.Resolve<bool>(), tag);
        }

        public bool RemoveBoolModifier(ValueKey<bool> key, ValueModifierTagId tag)
        {
            bool changed = GetOrCreateBoolSlot(key).RemoveModifier(tag);
            NotifyWatchNodeChanged(key.Id, changed);
            return changed;
        }

        public bool SetAdd(ValueKeyReference key, ValueModifierTagId tag, float value)
        {
            return ExecuteNumericModifier(key, nameof(SetAdd),
                floatKey => SetAdd(floatKey, tag, value),
                intKey => SetAdd(intKey, tag, value));
        }

        public bool SetAdd(ValueKey<float> key, ValueModifierTagId tag, float value)
        {
            bool changed = GetOrCreateFloatSlot(key).SetAdd(tag, value);
            NotifyWatchNodeChanged(key.Id, changed);
            return changed;
        }

        public bool SetAdd(ValueKey<int> key, ValueModifierTagId tag, float value)
        {
            bool changed = GetOrCreateIntSlot(key).SetAdd(tag, value);
            NotifyWatchNodeChanged(key.Id, changed);
            return changed;
        }

        public bool SetMul(ValueKeyReference key, ValueModifierTagId tag, float value)
        {
            return ExecuteNumericModifier(key, nameof(SetMul),
                floatKey => SetMul(floatKey, tag, value),
                intKey => SetMul(intKey, tag, value));
        }

        public bool SetMul(ValueKey<float> key, ValueModifierTagId tag, float value)
        {
            bool changed = GetOrCreateFloatSlot(key).SetMul(tag, value);
            NotifyWatchNodeChanged(key.Id, changed);
            return changed;
        }

        public bool SetMul(ValueKey<int> key, ValueModifierTagId tag, float value)
        {
            bool changed = GetOrCreateIntSlot(key).SetMul(tag, value);
            NotifyWatchNodeChanged(key.Id, changed);
            return changed;
        }

        public bool RemoveAdd(ValueKeyReference key, ValueModifierTagId tag)
        {
            return ExecuteNumericModifier(key, nameof(RemoveAdd),
                floatKey => RemoveAdd(floatKey, tag),
                intKey => RemoveAdd(intKey, tag));
        }

        public bool RemoveAdd(ValueKey<float> key, ValueModifierTagId tag)
        {
            bool changed = GetOrCreateFloatSlot(key).RemoveAdd(tag);
            NotifyWatchNodeChanged(key.Id, changed);
            return changed;
        }

        public bool RemoveAdd(ValueKey<int> key, ValueModifierTagId tag)
        {
            bool changed = GetOrCreateIntSlot(key).RemoveAdd(tag);
            NotifyWatchNodeChanged(key.Id, changed);
            return changed;
        }

        public bool RemoveMul(ValueKeyReference key, ValueModifierTagId tag)
        {
            return ExecuteNumericModifier(key, nameof(RemoveMul),
                floatKey => RemoveMul(floatKey, tag),
                intKey => RemoveMul(intKey, tag));
        }

        public bool RemoveMul(ValueKey<float> key, ValueModifierTagId tag)
        {
            bool changed = GetOrCreateFloatSlot(key).RemoveMul(tag);
            NotifyWatchNodeChanged(key.Id, changed);
            return changed;
        }

        public bool RemoveMul(ValueKey<int> key, ValueModifierTagId tag)
        {
            bool changed = GetOrCreateIntSlot(key).RemoveMul(tag);
            NotifyWatchNodeChanged(key.Id, changed);
            return changed;
        }

        private ValueWatchHandle<T> CreateFloatHandle<T>(ValueKey<float> key)
        {
            var slot = GetOrCreateFloatSlot(key);
            return (ValueWatchHandle<T>)(object)new ValueWatchHandle<float>(GetOrCreateWatchNode(key, slot.Get));
        }

        private ValueWatchHandle<T> CreateIntHandle<T>(ValueKey<int> key)
        {
            var slot = GetOrCreateIntSlot(key);
            return (ValueWatchHandle<T>)(object)new ValueWatchHandle<int>(GetOrCreateWatchNode(key, slot.Get));
        }

        private ValueWatchHandle<T> CreateBoolHandle<T>(ValueKey<bool> key)
        {
            var slot = GetOrCreateBoolSlot(key);
            return (ValueWatchHandle<T>)(object)new ValueWatchHandle<bool>(GetOrCreateWatchNode(key, slot.Get));
        }

        private ValueWatchNode<T> GetOrCreateWatchNode<T>(ValueKey<T> key, Func<T> readCurrentValue)
        {
            if (watchNodesByKey.TryGetValue(key.Id, out IValueWatchNode existing))
            {
                if (existing is ValueWatchNode<T> typed)
                    return typed;

                throw new InvalidOperationException(
                    $"Value watch node type mismatch. Path={key.Path}, Key={key.Id}, Expected={typeof(T).Name}, Actual={existing.ValueType.Name}");
            }

            var created = new ValueWatchNode<T>(readCurrentValue);
            watchNodesByKey.Add(key.Id, created);
            return created;
        }

        private void NotifyWatchNodeChanged(ValueKeyId keyId, bool changed)
        {
            if (!changed)
                return;

            if (!watchNodesByKey.TryGetValue(keyId, out IValueWatchNode watchNode))
                return;

            watchNode.Refresh();
        }

        private RawValueSlot<T> GetOrCreateRawSlot<T>(ValueKey<T> key)
        {
            if (slotsByKey.TryGetValue(key.Id, out ValueSlot slot))
            {
                if (key.CompositionMode != ValueCompositionMode.Raw)
                    throw new InvalidOperationException($"Value key composition mode mismatch. Path={key.Path}, Key={key.Id}, Expected={ValueCompositionMode.Raw}, Actual={key.CompositionMode}");
                if (slot is RawValueSlot<T> typed)
                    return typed;

                throw CreateSlotTypeMismatch(key, slot);
            }

            if (key.CompositionMode != ValueCompositionMode.Raw)
                throw new InvalidOperationException($"Value key composition mode mismatch. Path={key.Path}, Key={key.Id}, Expected={ValueCompositionMode.Raw}, Actual={key.CompositionMode}");

            var created = new RawValueSlot<T>(key);
            slotsByKey.Add(key.Id, created);
            return created;
        }

        private FloatNumericValueSlot GetOrCreateFloatSlot(ValueKey<float> key)
        {
            if (slotsByKey.TryGetValue(key.Id, out ValueSlot slot))
            {
                if (key.CompositionMode != ValueCompositionMode.NumericAddMul)
                    throw new InvalidOperationException($"Value key composition mode mismatch. Path={key.Path}, Key={key.Id}, Expected={ValueCompositionMode.NumericAddMul}, Actual={key.CompositionMode}");
                if (slot is FloatNumericValueSlot typed)
                    return typed;
                throw CreateSlotTypeMismatch(key, slot);
            }

            if (key.CompositionMode != ValueCompositionMode.NumericAddMul)
                throw new InvalidOperationException($"Value key composition mode mismatch. Path={key.Path}, Key={key.Id}, Expected={ValueCompositionMode.NumericAddMul}, Actual={key.CompositionMode}");

            var created = new FloatNumericValueSlot(key);
            slotsByKey.Add(key.Id, created);
            return created;
        }

        private IntNumericValueSlot GetOrCreateIntSlot(ValueKey<int> key)
        {
            if (slotsByKey.TryGetValue(key.Id, out ValueSlot slot))
            {
                if (key.CompositionMode != ValueCompositionMode.NumericAddMul)
                    throw new InvalidOperationException($"Value key composition mode mismatch. Path={key.Path}, Key={key.Id}, Expected={ValueCompositionMode.NumericAddMul}, Actual={key.CompositionMode}");
                if (slot is IntNumericValueSlot typed)
                    return typed;

                throw CreateSlotTypeMismatch(key, slot);
            }

            if (key.CompositionMode != ValueCompositionMode.NumericAddMul)
                throw new InvalidOperationException($"Value key composition mode mismatch. Path={key.Path}, Key={key.Id}, Expected={ValueCompositionMode.NumericAddMul}, Actual={key.CompositionMode}");

            var created = new IntNumericValueSlot(key);
            slotsByKey.Add(key.Id, created);
            return created;
        }

        private BoolCompositeValueSlot GetOrCreateBoolSlot(ValueKey<bool> key)
        {
            if (slotsByKey.TryGetValue(key.Id, out ValueSlot slot))
            {
                if (key.CompositionMode != ValueCompositionMode.BoolAnd &&
                    key.CompositionMode != ValueCompositionMode.BoolOr)
                    throw new InvalidOperationException($"Value key composition mode mismatch. Path={key.Path}, Key={key.Id}, Expected={ValueCompositionMode.BoolAnd} or {ValueCompositionMode.BoolOr}, Actual={key.CompositionMode}");
                if (slot is BoolCompositeValueSlot typed)
                    return typed;

                throw CreateSlotTypeMismatch(key, slot);
            }

            if (key.CompositionMode != ValueCompositionMode.BoolAnd &&
                key.CompositionMode != ValueCompositionMode.BoolOr)
                throw new InvalidOperationException($"Value key composition mode mismatch. Path={key.Path}, Key={key.Id}, Expected={ValueCompositionMode.BoolAnd} or {ValueCompositionMode.BoolOr}, Actual={key.CompositionMode}");

            var created = new BoolCompositeValueSlot(key);
            slotsByKey.Add(key.Id, created);
            return created;
        }

        private static InvalidOperationException CreateSlotTypeMismatch<T>(ValueKey<T> key, ValueSlot slot)
        {
            return new InvalidOperationException(
                $"Value slot type mismatch. Path={key.Path}, Key={key.Id}, Expected={typeof(T).Name}, ActualSlot={slot.GetType().Name}");
        }

        private static ValueKey<TTarget> UnsafeCastKey<TTarget, TSource>(ValueKey<TSource> key)
        {
            return new ValueKey<TTarget>(
                key.Id,
                key.Path,
                (TTarget)(object)key.DefaultValue,
                key.CompositionMode
            );
        }

        private static InvalidOperationException CreateUnsupportedNumericModifierException(
            string operation,
            ValueKeyReference key)
        {
            if (key.TryResolve(out ValueKeyDescriptor descriptor))
            {
                return new InvalidOperationException(
                    $"{operation} supports only int or float ValueKeys. Path={descriptor.Path}, Actual={descriptor.TypeName}");
            }

            return new InvalidOperationException(
                $"{operation} could not resolve ValueKey. Id={key.RawId}, Path={key.Path}");
        }

        private static bool ExecuteNumericModifier(
            ValueKeyReference key,
            string operation,
            Func<ValueKey<float>, bool> floatHandler,
            Func<ValueKey<int>, bool> intHandler)
        {
            if (key.TryResolve(out ValueKeyDescriptor descriptor))
            {
                if (descriptor.ValueType == typeof(float))
                    return floatHandler(descriptor.GetKey<float>());

                if (descriptor.ValueType == typeof(int))
                    return intHandler(descriptor.GetKey<int>());
            }

            throw CreateUnsupportedNumericModifierException(operation, key);
        }
    }
}