using System;
using System.Collections.Generic;

namespace BC.Base
{
    public readonly struct GameLogicValueStoreSnapshot
    {
        private readonly GameLogicValueSnapshotEntry[] entries;

        internal GameLogicValueStoreSnapshot(GameLogicValueSnapshotEntry[] entries)
        {
            this.entries = entries ?? Array.Empty<GameLogicValueSnapshotEntry>();
        }

        public bool IsValid => entries != null;
        internal IReadOnlyList<GameLogicValueSnapshotEntry> Entries => entries ?? Array.Empty<GameLogicValueSnapshotEntry>();
    }

    internal readonly struct GameLogicValueSnapshotEntry
    {
        public GameLogicValueSnapshotEntry(ValueKeyReference key, object value)
        {
            Key = key;
            Value = value;
        }

        public ValueKeyReference Key { get; }
        public object Value { get; }
    }

    public static class GameLogicValueStoreSnapshotUtility
    {
        private static readonly IReadOnlyList<ValueKeyDescriptor> GameLogicDescriptors =
            ValueKeyRegistry.GetDescriptors(pathPrefix: "GameLogic");

        public static GameLogicValueStoreSnapshot Capture(IEntityValueStoreService store, EntityRef entity)
        {
            if (store == null || !entity.IsValid)
                return default;

            GameLogicValueSnapshotEntry[] entries = new GameLogicValueSnapshotEntry[GameLogicDescriptors.Count];

            for (int i = 0; i < GameLogicDescriptors.Count; i++)
            {
                ValueKeyDescriptor descriptor = GameLogicDescriptors[i];
                entries[i] = new GameLogicValueSnapshotEntry(
                    CreateKeyReference(descriptor),
                    ReadValue(store, entity, descriptor));
            }

            return new GameLogicValueStoreSnapshot(entries);
        }

        public static void Restore(IEntityValueStoreService store, EntityRef entity, in GameLogicValueStoreSnapshot snapshot)
        {
            if (store == null || !entity.IsValid || !snapshot.IsValid)
                return;

            IReadOnlyList<GameLogicValueSnapshotEntry> entries = snapshot.Entries;

            for (int i = 0; i < entries.Count; i++)
            {
                GameLogicValueSnapshotEntry entry = entries[i];

                if (!entry.Key.TryResolve(out ValueKeyDescriptor descriptor))
                    continue;

                WriteValue(store, entity, descriptor, entry.Value);
            }
        }

        private static object ReadValue(IEntityValueStoreService store, EntityRef entity, in ValueKeyDescriptor descriptor)
        {
            if (descriptor.ValueType == typeof(bool))
                return store.Get(entity, descriptor.GetKey<bool>());

            if (descriptor.ValueType == typeof(int))
                return store.Get(entity, descriptor.GetKey<int>());

            if (descriptor.ValueType == typeof(float))
                return store.Get(entity, descriptor.GetKey<float>());

            if (descriptor.ValueType == typeof(string))
                return store.Get(entity, descriptor.GetKey<string>());

            if (descriptor.ValueType == typeof(EntityRef))
                return store.Get(entity, descriptor.GetKey<EntityRef>());

            if (descriptor.ValueType == typeof(FaceExpressionId))
                return store.Get(entity, descriptor.GetKey<FaceExpressionId>());

            if (descriptor.ValueType == typeof(EntityMoveState))
                return store.Get(entity, descriptor.GetKey<EntityMoveState>());

            if (descriptor.ValueType == typeof(ShapeExpressionId))
                return store.Get(entity, descriptor.GetKey<ShapeExpressionId>());

            throw new InvalidOperationException(
                $"Unsupported GameLogic value type. Path={descriptor.Path}, Type={descriptor.TypeName}");
        }

        private static void WriteValue(
            IEntityValueStoreService store,
            EntityRef entity,
            in ValueKeyDescriptor descriptor,
            object value)
        {
            if (descriptor.ValueType == typeof(bool))
            {
                store.Set(entity, descriptor.GetKey<bool>(), value is bool boolValue ? boolValue : default);
                return;
            }

            if (descriptor.ValueType == typeof(int))
            {
                store.Set(entity, descriptor.GetKey<int>(), value is int intValue ? intValue : default);
                return;
            }

            if (descriptor.ValueType == typeof(float))
            {
                store.Set(entity, descriptor.GetKey<float>(), value is float floatValue ? floatValue : default);
                return;
            }

            if (descriptor.ValueType == typeof(string))
            {
                store.Set(entity, descriptor.GetKey<string>(), value as string ?? string.Empty);
                return;
            }

            if (descriptor.ValueType == typeof(EntityRef))
            {
                store.Set(entity, descriptor.GetKey<EntityRef>(), value is EntityRef entityValue ? entityValue : default);
                return;
            }

            if (descriptor.ValueType == typeof(FaceExpressionId))
            {
                store.Set(entity, descriptor.GetKey<FaceExpressionId>(), value is FaceExpressionId faceValue ? faceValue : default);
                return;
            }

            if (descriptor.ValueType == typeof(EntityMoveState))
            {
                store.Set(entity, descriptor.GetKey<EntityMoveState>(), value is EntityMoveState moveStateValue ? moveStateValue : default);
                return;
            }

            if (descriptor.ValueType == typeof(ShapeExpressionId))
            {
                store.Set(entity, descriptor.GetKey<ShapeExpressionId>(), value is ShapeExpressionId shapeValue ? shapeValue : default);
                return;
            }

            throw new InvalidOperationException(
                $"Unsupported GameLogic value type. Path={descriptor.Path}, Type={descriptor.TypeName}");
        }

        private static ValueKeyReference CreateKeyReference(in ValueKeyDescriptor descriptor)
        {
            if (descriptor.ValueType == typeof(bool))
                return ValueKeyReference.From(descriptor.GetKey<bool>());

            if (descriptor.ValueType == typeof(int))
                return ValueKeyReference.From(descriptor.GetKey<int>());

            if (descriptor.ValueType == typeof(float))
                return ValueKeyReference.From(descriptor.GetKey<float>());

            if (descriptor.ValueType == typeof(string))
                return ValueKeyReference.From(descriptor.GetKey<string>());

            if (descriptor.ValueType == typeof(EntityRef))
                return ValueKeyReference.From(descriptor.GetKey<EntityRef>());

            if (descriptor.ValueType == typeof(FaceExpressionId))
                return ValueKeyReference.From(descriptor.GetKey<FaceExpressionId>());

            if (descriptor.ValueType == typeof(EntityMoveState))
                return ValueKeyReference.From(descriptor.GetKey<EntityMoveState>());

            if (descriptor.ValueType == typeof(ShapeExpressionId))
                return ValueKeyReference.From(descriptor.GetKey<ShapeExpressionId>());

            throw new InvalidOperationException(
                $"Unsupported GameLogic value type. Path={descriptor.Path}, Type={descriptor.TypeName}");
        }
    }
}
