using System;
using UnityEngine;

namespace BC.Base
{
    public enum ReactiveWatchedIntSourceKind
    {
        EntityValueStore = 0,
        LocalValueStore = 1,
    }

    public enum ReactiveSnapshotIntSourceKind
    {
        Literal = 0,
        EntityValueStore = 1,
        LocalValueStore = 2,
    }

    [Serializable]
    public struct ReactiveWatchedInt
    {
        [SerializeField] private ReactiveWatchedIntSourceKind sourceKind;
        [SerializeField] private ReactiveFailurePolicy failurePolicy;
        [SerializeField] private ReactiveEntityValueSource entityValue;
        [SerializeField] private ReactiveLocalValueSource localValue;
        [SerializeField] private int fallbackValue;

        public ReactiveWatchedIntSourceKind SourceKind => sourceKind;
        public ReactiveFailurePolicy FailurePolicy => failurePolicy;
        public ReactiveEntityValueSource EntityValue => entityValue;
        public ReactiveLocalValueSource LocalValue => localValue;
        public int FallbackValue => fallbackValue;

        public static ReactiveWatchedInt EntityValueStore(
            ReactiveEntityRef entity,
            ValueKeyReference key,
            int fallbackValue = 0)
        {
            return new ReactiveWatchedInt
            {
                sourceKind = ReactiveWatchedIntSourceKind.EntityValueStore,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                entityValue = ReactiveEntityValueSource.Create(entity, key),
                fallbackValue = fallbackValue,
            };
        }

        public static ReactiveWatchedInt LocalValueStore(
            ValueKeyReference key,
            int fallbackValue = 0)
        {
            return new ReactiveWatchedInt
            {
                sourceKind = ReactiveWatchedIntSourceKind.LocalValueStore,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                localValue = ReactiveLocalValueSource.Create(key),
                fallbackValue = fallbackValue,
            };
        }
    }

    [Serializable]
    public struct ReactiveSnapshotInt
    {
        [SerializeField] private ReactiveSnapshotIntSourceKind sourceKind;
        [SerializeField] private ReactiveFailurePolicy failurePolicy;
        [SerializeField] private int literal;
        [SerializeField] private ReactiveEntityValueSource entityValue;
        [SerializeField] private ReactiveLocalValueSource localValue;
        [SerializeField] private int fallbackValue;

        public ReactiveSnapshotIntSourceKind SourceKind => sourceKind;
        public ReactiveFailurePolicy FailurePolicy => failurePolicy;
        public int Literal => literal;
        public ReactiveEntityValueSource EntityValue => entityValue;
        public ReactiveLocalValueSource LocalValue => localValue;
        public int FallbackValue => fallbackValue;

        public static ReactiveSnapshotInt LiteralValue(int value)
        {
            return new ReactiveSnapshotInt
            {
                sourceKind = ReactiveSnapshotIntSourceKind.Literal,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                literal = value,
                fallbackValue = value,
            };
        }

        public static ReactiveSnapshotInt EntityValueStore(
            ReactiveEntityRef entity,
            ValueKeyReference key,
            int fallbackValue = 0)
        {
            return new ReactiveSnapshotInt
            {
                sourceKind = ReactiveSnapshotIntSourceKind.EntityValueStore,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                entityValue = ReactiveEntityValueSource.Create(entity, key),
                fallbackValue = fallbackValue,
            };
        }

        public static ReactiveSnapshotInt LocalValueStore(
            ValueKeyReference key,
            int fallbackValue = 0)
        {
            return new ReactiveSnapshotInt
            {
                sourceKind = ReactiveSnapshotIntSourceKind.LocalValueStore,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                localValue = ReactiveLocalValueSource.Create(key),
                fallbackValue = fallbackValue,
            };
        }
    }

    public enum ReactiveWatchedFloatSourceKind
    {
        EntityValueStore = 0,
        LocalValueStore = 1,
    }

    public enum ReactiveSnapshotFloatSourceKind
    {
        Literal = 0,
        EntityValueStore = 1,
        LocalValueStore = 2,
        Distance = 3,
    }

    [Serializable]
    public struct ReactiveWatchedFloat
    {
        [SerializeField] private ReactiveWatchedFloatSourceKind sourceKind;
        [SerializeField] private ReactiveFailurePolicy failurePolicy;
        [SerializeField] private ReactiveEntityValueSource entityValue;
        [SerializeField] private ReactiveLocalValueSource localValue;
        [SerializeField] private float fallbackValue;

        public ReactiveWatchedFloatSourceKind SourceKind => sourceKind;
        public ReactiveFailurePolicy FailurePolicy => failurePolicy;
        public ReactiveEntityValueSource EntityValue => entityValue;
        public ReactiveLocalValueSource LocalValue => localValue;
        public float FallbackValue => fallbackValue;

        public static ReactiveWatchedFloat EntityValueStore(
            ReactiveEntityRef entity,
            ValueKeyReference key,
            float fallbackValue = 0f)
        {
            return new ReactiveWatchedFloat
            {
                sourceKind = ReactiveWatchedFloatSourceKind.EntityValueStore,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                entityValue = ReactiveEntityValueSource.Create(entity, key),
                fallbackValue = fallbackValue,
            };
        }

        public static ReactiveWatchedFloat LocalValueStore(
            ValueKeyReference key,
            float fallbackValue = 0f)
        {
            return new ReactiveWatchedFloat
            {
                sourceKind = ReactiveWatchedFloatSourceKind.LocalValueStore,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                localValue = ReactiveLocalValueSource.Create(key),
                fallbackValue = fallbackValue,
            };
        }
    }

    [Serializable]
    public struct ReactiveSnapshotFloat
    {
        [SerializeField] private ReactiveSnapshotFloatSourceKind sourceKind;
        [SerializeField] private ReactiveFailurePolicy failurePolicy;
        [SerializeField] private float literal;
        [SerializeField] private ReactiveEntityValueSource entityValue;
        [SerializeField] private ReactiveLocalValueSource localValue;
        [SerializeField] private ReactiveFloatDistanceSource distance;
        [SerializeField] private float fallbackValue;

        public ReactiveSnapshotFloatSourceKind SourceKind => sourceKind;
        public ReactiveFailurePolicy FailurePolicy => failurePolicy;
        public float Literal => literal;
        public ReactiveEntityValueSource EntityValue => entityValue;
        public ReactiveLocalValueSource LocalValue => localValue;
        public ReactiveFloatDistanceSource DistanceSource => distance;
        public float FallbackValue => fallbackValue;

        public static ReactiveSnapshotFloat LiteralValue(float value)
        {
            return new ReactiveSnapshotFloat
            {
                sourceKind = ReactiveSnapshotFloatSourceKind.Literal,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                literal = value,
                fallbackValue = value,
            };
        }

        public static ReactiveSnapshotFloat EntityValueStore(
            ReactiveEntityRef entity,
            ValueKeyReference key,
            float fallbackValue = 0f)
        {
            return new ReactiveSnapshotFloat
            {
                sourceKind = ReactiveSnapshotFloatSourceKind.EntityValueStore,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                entityValue = ReactiveEntityValueSource.Create(entity, key),
                fallbackValue = fallbackValue,
            };
        }

        public static ReactiveSnapshotFloat LocalValueStore(
            ValueKeyReference key,
            float fallbackValue = 0f)
        {
            return new ReactiveSnapshotFloat
            {
                sourceKind = ReactiveSnapshotFloatSourceKind.LocalValueStore,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                localValue = ReactiveLocalValueSource.Create(key),
                fallbackValue = fallbackValue,
            };
        }

        public static ReactiveSnapshotFloat Distance(
            ReactiveEntityRef fromEntity,
            ReactiveEntityRef toEntity)
        {
            return new ReactiveSnapshotFloat
            {
                sourceKind = ReactiveSnapshotFloatSourceKind.Distance,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                distance = ReactiveFloatDistanceSource.Create(fromEntity, toEntity),
            };
        }
    }
}
