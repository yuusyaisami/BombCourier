using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BC.Base
{
    public enum WiringActionKind
    {
        None = 0,
        KernelStoreSet = 10,
        KernelStoreAdd = 11,
        KernelStoreMul = 12,
        KernelStoreBoolModifier = 13,
        KernelStoreRemoveAdd = 14,
        KernelStoreRemoveMul = 15,
        KernelStoreRemoveBoolModifier = 16,
        EntityStoreSet = 30,
        EntityStoreAdd = 31,
        EntityStoreMul = 32,
        EntityStoreBoolModifier = 33,
        EntityStoreRemoveAdd = 34,
        EntityStoreRemoveMul = 35,
        EntityStoreRemoveBoolModifier = 36,
        KernelSignalPublish = 50,
        EntitySignalPublish = 51,
    }

    [Serializable]
    public sealed class WiringAction
    {
        [SerializeField] private WiringActionKind kind;
        [ShowIf(nameof(UsesKernelValueKey))]
        [SerializeField, ValueKeyDropdown("Kernel")]
        private ValueKeyReference kernelValueKey;

        [ShowIf(nameof(UsesEntityValueKey))]
        [SerializeField, ValueKeyDropdown]
        private ValueKeyReference entityValueKey;

        [ShowIf(nameof(UsesEntityTarget))]
        [SerializeField]
        private EntityTargetReference entityTarget = EntityTargetReference.Trigger();

        [ShowIf(nameof(UsesKernelSignal))]
        [SerializeField, SignalDropdown]
        private KernelSignalReference kernelSignal;

        [ShowIf(nameof(UsesEntitySignal))]
        [SerializeField, SignalDropdown]
        private EntitySignalReference entitySignal;

        [ShowIf(nameof(UsesModifierTag))]
        [SerializeField]
        private int modifierTagId = 1;

        [ShowIf(nameof(ShowsBoolValue))]
        [SerializeField]
        private bool boolValue;

        [ShowIf(nameof(ShowsIntValue))]
        [SerializeField]
        private int intValue;

        [ShowIf(nameof(ShowsFloatValue))]
        [SerializeField]
        private float floatValue;

        [ShowIf(nameof(ShowsStringValue))]
        [SerializeField]
        private string stringValue;

        [NonSerialized] private List<EntityRef> targetBuffer;

        public WiringActionKind Kind => kind;

        private bool UsesKernelValueKey => IsKernelStoreAction;
        private bool UsesEntityValueKey => IsEntityStoreAction;
        private bool UsesEntityTarget => IsEntityStoreAction || kind == WiringActionKind.EntitySignalPublish;
        private bool UsesKernelSignal => kind == WiringActionKind.KernelSignalPublish;
        private bool UsesEntitySignal => kind == WiringActionKind.EntitySignalPublish;
        private bool UsesModifierTag => IsModifierTagAction;
        private bool ShowsBoolValue => IsSetBoolAction || IsBoolModifierAction;
        private bool ShowsIntValue => IsSetIntAction;
        private bool ShowsFloatValue => IsSetFloatAction || IsNumericModifierAction;
        private bool ShowsStringValue => IsSetStringAction;

        private bool IsKernelStoreAction =>
            kind == WiringActionKind.KernelStoreSet ||
            kind == WiringActionKind.KernelStoreAdd ||
            kind == WiringActionKind.KernelStoreMul ||
            kind == WiringActionKind.KernelStoreBoolModifier ||
            kind == WiringActionKind.KernelStoreRemoveAdd ||
            kind == WiringActionKind.KernelStoreRemoveMul ||
            kind == WiringActionKind.KernelStoreRemoveBoolModifier;

        private bool IsEntityStoreAction =>
            kind == WiringActionKind.EntityStoreSet ||
            kind == WiringActionKind.EntityStoreAdd ||
            kind == WiringActionKind.EntityStoreMul ||
            kind == WiringActionKind.EntityStoreBoolModifier ||
            kind == WiringActionKind.EntityStoreRemoveAdd ||
            kind == WiringActionKind.EntityStoreRemoveMul ||
            kind == WiringActionKind.EntityStoreRemoveBoolModifier;

        private bool IsSetAction =>
            kind == WiringActionKind.KernelStoreSet ||
            kind == WiringActionKind.EntityStoreSet;

        private bool IsNumericModifierAction =>
            kind == WiringActionKind.KernelStoreAdd ||
            kind == WiringActionKind.KernelStoreMul ||
            kind == WiringActionKind.EntityStoreAdd ||
            kind == WiringActionKind.EntityStoreMul;

        private bool IsBoolModifierAction =>
            kind == WiringActionKind.KernelStoreBoolModifier ||
            kind == WiringActionKind.EntityStoreBoolModifier;

        private bool IsModifierTagAction =>
            kind == WiringActionKind.KernelStoreAdd ||
            kind == WiringActionKind.KernelStoreMul ||
            kind == WiringActionKind.KernelStoreBoolModifier ||
            kind == WiringActionKind.KernelStoreRemoveAdd ||
            kind == WiringActionKind.KernelStoreRemoveMul ||
            kind == WiringActionKind.KernelStoreRemoveBoolModifier ||
            kind == WiringActionKind.EntityStoreAdd ||
            kind == WiringActionKind.EntityStoreMul ||
            kind == WiringActionKind.EntityStoreBoolModifier ||
            kind == WiringActionKind.EntityStoreRemoveAdd ||
            kind == WiringActionKind.EntityStoreRemoveMul ||
            kind == WiringActionKind.EntityStoreRemoveBoolModifier;

        private bool IsSetBoolAction => IsSetAction && IsActiveValueType(typeof(bool));
        private bool IsSetIntAction => IsSetAction && IsActiveValueType(typeof(int));
        private bool IsSetFloatAction => IsSetAction && IsActiveValueType(typeof(float));
        private bool IsSetStringAction => IsSetAction && IsActiveValueType(typeof(string));

        private bool IsActiveValueType(Type valueType)
        {
            return TryGetActiveValueType(out Type activeValueType) && activeValueType == valueType;
        }

        private bool TryGetActiveValueType(out Type valueType)
        {
            ValueKeyReference reference = IsKernelStoreAction
                ? kernelValueKey
                : IsEntityStoreAction
                    ? entityValueKey
                    : default;

            if (reference.TryResolve(out ValueKeyDescriptor descriptor))
            {
                valueType = descriptor.ValueType;
                return true;
            }

            valueType = null;
            return false;
        }

        public bool Execute(in WiringActionContext context)
        {
            switch (kind)
            {
                case WiringActionKind.KernelStoreSet:
                    return ExecuteStoreSet(context.SceneKernel?.KernelValueStore, kernelValueKey);

                case WiringActionKind.KernelStoreAdd:
                    return ExecuteStoreAdd(context.SceneKernel?.KernelValueStore, kernelValueKey);

                case WiringActionKind.KernelStoreMul:
                    return ExecuteStoreMul(context.SceneKernel?.KernelValueStore, kernelValueKey);

                case WiringActionKind.KernelStoreBoolModifier:
                    return ExecuteStoreBoolModifier(context.SceneKernel?.KernelValueStore, kernelValueKey);

                case WiringActionKind.KernelStoreRemoveAdd:
                    return ExecuteStoreRemoveAdd(context.SceneKernel?.KernelValueStore, kernelValueKey);

                case WiringActionKind.KernelStoreRemoveMul:
                    return ExecuteStoreRemoveMul(context.SceneKernel?.KernelValueStore, kernelValueKey);

                case WiringActionKind.KernelStoreRemoveBoolModifier:
                    return ExecuteStoreRemoveBoolModifier(context.SceneKernel?.KernelValueStore, kernelValueKey);

                case WiringActionKind.EntityStoreSet:
                    return ExecuteForTargets(context, entity => ExecuteEntityStoreSet(context, entity));

                case WiringActionKind.EntityStoreAdd:
                    return ExecuteForTargets(context, entity => ExecuteEntityStoreAdd(context, entity));

                case WiringActionKind.EntityStoreMul:
                    return ExecuteForTargets(context, entity => ExecuteEntityStoreMul(context, entity));

                case WiringActionKind.EntityStoreBoolModifier:
                    return ExecuteForTargets(context, entity => ExecuteEntityStoreBoolModifier(context, entity));

                case WiringActionKind.EntityStoreRemoveAdd:
                    return ExecuteForTargets(context, entity => ExecuteEntityStoreRemoveAdd(context, entity));

                case WiringActionKind.EntityStoreRemoveMul:
                    return ExecuteForTargets(context, entity => ExecuteEntityStoreRemoveMul(context, entity));

                case WiringActionKind.EntityStoreRemoveBoolModifier:
                    return ExecuteForTargets(context, entity => ExecuteEntityStoreRemoveBoolModifier(context, entity));

                case WiringActionKind.KernelSignalPublish:
                    return context.SceneKernel != null && context.SceneKernel.KernelEvents.RaiseSignal(kernelSignal);

                case WiringActionKind.EntitySignalPublish:
                    return ExecuteForTargets(context, entity => context.SceneKernel.EntityEvents.RaiseSignal(entity, entitySignal));

                default:
                    return false;
            }
        }

        private bool ExecuteForTargets(in WiringActionContext context, Func<EntityRef, bool> action)
        {
            if (context.SceneKernel == null || action == null)
                return false;

            targetBuffer ??= new List<EntityRef>(4);
            List<EntityRef> targets = targetBuffer;
            EntityTargetResolver.Resolve(context, entityTarget, targets);

            bool handled = false;

            for (int i = 0; i < targets.Count; i++)
            {
                handled |= action(targets[i]);
            }

            return handled;
        }

        private bool ExecuteEntityStoreSet(in WiringActionContext context, EntityRef entity)
        {
            return ExecuteStoreSet(new EntityStoreAdapter(context.SceneKernel?.EntityValueStore, entity), entityValueKey);
        }

        private bool ExecuteEntityStoreAdd(in WiringActionContext context, EntityRef entity)
        {
            return ExecuteStoreAdd(new EntityStoreAdapter(context.SceneKernel?.EntityValueStore, entity), entityValueKey);
        }

        private bool ExecuteEntityStoreMul(in WiringActionContext context, EntityRef entity)
        {
            return ExecuteStoreMul(new EntityStoreAdapter(context.SceneKernel?.EntityValueStore, entity), entityValueKey);
        }

        private bool ExecuteEntityStoreBoolModifier(in WiringActionContext context, EntityRef entity)
        {
            return ExecuteStoreBoolModifier(new EntityStoreAdapter(context.SceneKernel?.EntityValueStore, entity), entityValueKey);
        }

        private bool ExecuteEntityStoreRemoveAdd(in WiringActionContext context, EntityRef entity)
        {
            return ExecuteStoreRemoveAdd(new EntityStoreAdapter(context.SceneKernel?.EntityValueStore, entity), entityValueKey);
        }

        private bool ExecuteEntityStoreRemoveMul(in WiringActionContext context, EntityRef entity)
        {
            return ExecuteStoreRemoveMul(new EntityStoreAdapter(context.SceneKernel?.EntityValueStore, entity), entityValueKey);
        }

        private bool ExecuteEntityStoreRemoveBoolModifier(in WiringActionContext context, EntityRef entity)
        {
            return ExecuteStoreRemoveBoolModifier(new EntityStoreAdapter(context.SceneKernel?.EntityValueStore, entity), entityValueKey);
        }

        private bool ExecuteStoreSet(IWiringStoreAdapter store, ValueKeyReference key)
        {
            if (store == null || !key.TryResolve(out ValueKeyDescriptor descriptor))
                return false;

            if (descriptor.ValueType == typeof(bool))
                return store.Set(descriptor.GetKey<bool>(), boolValue);

            if (descriptor.ValueType == typeof(int))
                return store.Set(descriptor.GetKey<int>(), intValue);

            if (descriptor.ValueType == typeof(float))
                return store.Set(descriptor.GetKey<float>(), floatValue);

            if (descriptor.ValueType == typeof(string))
                return store.Set(descriptor.GetKey<string>(), stringValue);

            return false;
        }

        private bool ExecuteStoreAdd(IWiringStoreAdapter store, ValueKeyReference key)
        {
            if (store == null || !key.TryResolve(out ValueKeyDescriptor descriptor))
                return false;

            ValueModifierTagId tag = new ValueModifierTagId(modifierTagId);

            if (descriptor.ValueType == typeof(float))
                return store.SetAdd(descriptor.GetKey<float>(), tag, floatValue);

            if (descriptor.ValueType == typeof(int))
                return store.SetAdd(descriptor.GetKey<int>(), tag, floatValue);

            return false;
        }

        private bool ExecuteStoreMul(IWiringStoreAdapter store, ValueKeyReference key)
        {
            if (store == null || !key.TryResolve(out ValueKeyDescriptor descriptor))
                return false;

            ValueModifierTagId tag = new ValueModifierTagId(modifierTagId);

            if (descriptor.ValueType == typeof(float))
                return store.SetMul(descriptor.GetKey<float>(), tag, floatValue);

            if (descriptor.ValueType == typeof(int))
                return store.SetMul(descriptor.GetKey<int>(), tag, floatValue);

            return false;
        }

        private bool ExecuteStoreBoolModifier(IWiringStoreAdapter store, ValueKeyReference key)
        {
            if (store == null || !key.TryResolve(out ValueKeyDescriptor descriptor) || descriptor.ValueType != typeof(bool))
                return false;

            return store.SetBoolModifier(descriptor.GetKey<bool>(), new ValueModifierTagId(modifierTagId), boolValue);
        }

        private bool ExecuteStoreRemoveAdd(IWiringStoreAdapter store, ValueKeyReference key)
        {
            if (store == null || !key.TryResolve(out ValueKeyDescriptor descriptor))
                return false;

            ValueModifierTagId tag = new ValueModifierTagId(modifierTagId);

            if (descriptor.ValueType == typeof(float))
                return store.RemoveAdd(descriptor.GetKey<float>(), tag);

            if (descriptor.ValueType == typeof(int))
                return store.RemoveAdd(descriptor.GetKey<int>(), tag);

            return false;
        }

        private bool ExecuteStoreRemoveMul(IWiringStoreAdapter store, ValueKeyReference key)
        {
            if (store == null || !key.TryResolve(out ValueKeyDescriptor descriptor))
                return false;

            ValueModifierTagId tag = new ValueModifierTagId(modifierTagId);

            if (descriptor.ValueType == typeof(float))
                return store.RemoveMul(descriptor.GetKey<float>(), tag);

            if (descriptor.ValueType == typeof(int))
                return store.RemoveMul(descriptor.GetKey<int>(), tag);

            return false;
        }

        private bool ExecuteStoreRemoveBoolModifier(IWiringStoreAdapter store, ValueKeyReference key)
        {
            if (store == null || !key.TryResolve(out ValueKeyDescriptor descriptor) || descriptor.ValueType != typeof(bool))
                return false;

            return store.RemoveBoolModifier(descriptor.GetKey<bool>(), new ValueModifierTagId(modifierTagId));
        }

        private interface IWiringStoreAdapter
        {
            bool Set<T>(ValueKey<T> key, T value);
            bool SetAdd(ValueKey<float> key, ValueModifierTagId tag, float value);
            bool SetAdd(ValueKey<int> key, ValueModifierTagId tag, float value);
            bool SetMul(ValueKey<float> key, ValueModifierTagId tag, float value);
            bool SetMul(ValueKey<int> key, ValueModifierTagId tag, float value);
            bool RemoveAdd(ValueKey<float> key, ValueModifierTagId tag);
            bool RemoveAdd(ValueKey<int> key, ValueModifierTagId tag);
            bool RemoveMul(ValueKey<float> key, ValueModifierTagId tag);
            bool RemoveMul(ValueKey<int> key, ValueModifierTagId tag);
            bool SetBoolModifier(ValueKey<bool> key, ValueModifierTagId tag, bool value);
            bool RemoveBoolModifier(ValueKey<bool> key, ValueModifierTagId tag);
        }

        private sealed class KernelStoreAdapter : IWiringStoreAdapter
        {
            private readonly KernelValueStoreService store;

            public KernelStoreAdapter(KernelValueStoreService store)
            {
                this.store = store;
            }

            public bool Set<T>(ValueKey<T> key, T value) => store != null && store.Set(key, value);
            public bool SetAdd(ValueKey<float> key, ValueModifierTagId tag, float value) => store != null && store.SetAdd(key, tag, value);
            public bool SetAdd(ValueKey<int> key, ValueModifierTagId tag, float value) => store != null && store.SetAdd(key, tag, value);
            public bool SetMul(ValueKey<float> key, ValueModifierTagId tag, float value) => store != null && store.SetMul(key, tag, value);
            public bool SetMul(ValueKey<int> key, ValueModifierTagId tag, float value) => store != null && store.SetMul(key, tag, value);
            public bool RemoveAdd(ValueKey<float> key, ValueModifierTagId tag) => store != null && store.RemoveAdd(key, tag);
            public bool RemoveAdd(ValueKey<int> key, ValueModifierTagId tag) => store != null && store.RemoveAdd(key, tag);
            public bool RemoveMul(ValueKey<float> key, ValueModifierTagId tag) => store != null && store.RemoveMul(key, tag);
            public bool RemoveMul(ValueKey<int> key, ValueModifierTagId tag) => store != null && store.RemoveMul(key, tag);
            public bool SetBoolModifier(ValueKey<bool> key, ValueModifierTagId tag, bool value) => store != null && store.SetBoolModifier(key, tag, value);
            public bool RemoveBoolModifier(ValueKey<bool> key, ValueModifierTagId tag) => store != null && store.RemoveBoolModifier(key, tag);
        }

        private sealed class EntityStoreAdapter : IWiringStoreAdapter
        {
            private readonly ValueStoreService store;
            private readonly EntityRef entity;

            public EntityStoreAdapter(ValueStoreService store, EntityRef entity)
            {
                this.store = store;
                this.entity = entity;
            }

            public bool Set<T>(ValueKey<T> key, T value) => store != null && entity.IsValid && store.Set(entity, key, value);
            public bool SetAdd(ValueKey<float> key, ValueModifierTagId tag, float value) => store != null && entity.IsValid && store.SetAdd(entity, key, tag, value);
            public bool SetAdd(ValueKey<int> key, ValueModifierTagId tag, float value) => store != null && entity.IsValid && store.SetAdd(entity, key, tag, value);
            public bool SetMul(ValueKey<float> key, ValueModifierTagId tag, float value) => store != null && entity.IsValid && store.SetMul(entity, key, tag, value);
            public bool SetMul(ValueKey<int> key, ValueModifierTagId tag, float value) => store != null && entity.IsValid && store.SetMul(entity, key, tag, value);
            public bool RemoveAdd(ValueKey<float> key, ValueModifierTagId tag) => store != null && entity.IsValid && store.RemoveAdd(entity, key, tag);
            public bool RemoveAdd(ValueKey<int> key, ValueModifierTagId tag) => store != null && entity.IsValid && store.RemoveAdd(entity, key, tag);
            public bool RemoveMul(ValueKey<float> key, ValueModifierTagId tag) => store != null && entity.IsValid && store.RemoveMul(entity, key, tag);
            public bool RemoveMul(ValueKey<int> key, ValueModifierTagId tag) => store != null && entity.IsValid && store.RemoveMul(entity, key, tag);
            public bool SetBoolModifier(ValueKey<bool> key, ValueModifierTagId tag, bool value) => store != null && entity.IsValid && store.SetBoolModifier(entity, key, tag, value);
            public bool RemoveBoolModifier(ValueKey<bool> key, ValueModifierTagId tag) => store != null && entity.IsValid && store.RemoveBoolModifier(entity, key, tag);
        }

        private bool ExecuteStoreSet(KernelValueStoreService store, ValueKeyReference key)
        {
            return ExecuteStoreSet(new KernelStoreAdapter(store), key);
        }

        private bool ExecuteStoreAdd(KernelValueStoreService store, ValueKeyReference key)
        {
            return ExecuteStoreAdd(new KernelStoreAdapter(store), key);
        }

        private bool ExecuteStoreMul(KernelValueStoreService store, ValueKeyReference key)
        {
            return ExecuteStoreMul(new KernelStoreAdapter(store), key);
        }

        private bool ExecuteStoreBoolModifier(KernelValueStoreService store, ValueKeyReference key)
        {
            return ExecuteStoreBoolModifier(new KernelStoreAdapter(store), key);
        }

        private bool ExecuteStoreRemoveAdd(KernelValueStoreService store, ValueKeyReference key)
        {
            return ExecuteStoreRemoveAdd(new KernelStoreAdapter(store), key);
        }

        private bool ExecuteStoreRemoveMul(KernelValueStoreService store, ValueKeyReference key)
        {
            return ExecuteStoreRemoveMul(new KernelStoreAdapter(store), key);
        }

        private bool ExecuteStoreRemoveBoolModifier(KernelValueStoreService store, ValueKeyReference key)
        {
            return ExecuteStoreRemoveBoolModifier(new KernelStoreAdapter(store), key);
        }
    }

    public static class WiringActionRunner
    {
        public static int ExecuteAll(WiringAction[] actions, in WiringActionContext context)
        {
            if (actions == null)
                return 0;

            int handledCount = 0;

            for (int i = 0; i < actions.Length; i++)
            {
                WiringAction action = actions[i];

                if (action != null && action.Execute(context))
                    handledCount++;
            }

            return handledCount;
        }
    }
}