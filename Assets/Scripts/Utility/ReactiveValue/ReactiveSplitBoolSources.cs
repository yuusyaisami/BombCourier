using System;
using UnityEngine;

namespace BC.Base
{
    public enum ReactiveWatchedBoolSourceKind
    {
        EntityValueStore = 0,
        KernelValueStore = 1,
    }

    public enum ReactiveSnapshotBoolSourceKind
    {
        Literal = 0,
        EntityValueStore = 1,
        KernelValueStore = 2,
        EntityAlive = 3,
        CompareFloat = 4,
    }

    [Serializable]
    public struct ReactiveWatchedBool
    {
        [SerializeField] private ReactiveWatchedBoolSourceKind sourceKind;
        [SerializeField] private ReactiveFailurePolicy failurePolicy;
        [SerializeField] private ReactiveEntityValueSource entityValue;
        [SerializeField] private ReactiveKernelValueSource localValue;
        [SerializeField] private bool fallbackValue;

        public ReactiveWatchedBoolSourceKind SourceKind => sourceKind;
        public ReactiveFailurePolicy FailurePolicy => failurePolicy;
        public ReactiveEntityValueSource EntityValue => entityValue;
        public ReactiveKernelValueSource LocalValue => localValue;
        public bool FallbackValue => fallbackValue;

        public static ReactiveWatchedBool EntityValueStore(
            ReactiveEntityRef entity,
            ValueKeyReference key,
            bool fallbackValue = false)
        {
            return new ReactiveWatchedBool
            {
                sourceKind = ReactiveWatchedBoolSourceKind.EntityValueStore,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                entityValue = ReactiveEntityValueSource.Create(entity, key),
                fallbackValue = fallbackValue,
            };
        }

        public static ReactiveWatchedBool KernelValueStore(
            ValueKeyReference key,
            bool fallbackValue = false)
        {
            return new ReactiveWatchedBool
            {
                sourceKind = ReactiveWatchedBoolSourceKind.KernelValueStore,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                localValue = ReactiveKernelValueSource.Create(key),
                fallbackValue = fallbackValue,
            };
        }
    }

    [Serializable]
    public struct ReactiveSnapshotBool
    {
        [SerializeField] private ReactiveSnapshotBoolSourceKind sourceKind;
        [SerializeField] private ReactiveFailurePolicy failurePolicy;
        [SerializeField] private bool literal;
        [SerializeField] private ReactiveEntityValueSource entityValue;
        [SerializeField] private ReactiveKernelValueSource localValue;
        [SerializeField] private ReactiveBoolEntityAliveSource entityAlive;
        [SerializeField] private ReactiveFloatCompareSource compareFloat;
        [SerializeField] private bool fallbackValue;

        public ReactiveSnapshotBoolSourceKind SourceKind => sourceKind;
        public ReactiveFailurePolicy FailurePolicy => failurePolicy;
        public bool Literal => literal;
        public ReactiveEntityValueSource EntityValue => entityValue;
        public ReactiveKernelValueSource LocalValue => localValue;
        public ReactiveBoolEntityAliveSource EntityAliveSource => entityAlive;
        public ReactiveFloatCompareSource CompareFloatSource => compareFloat;
        public bool FallbackValue => fallbackValue;

        public static ReactiveSnapshotBool LiteralValue(bool value)
        {
            return new ReactiveSnapshotBool
            {
                sourceKind = ReactiveSnapshotBoolSourceKind.Literal,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                literal = value,
                fallbackValue = value,
            };
        }

        public static ReactiveSnapshotBool EntityValueStore(
            ReactiveEntityRef entity,
            ValueKeyReference key,
            bool fallbackValue = false)
        {
            return new ReactiveSnapshotBool
            {
                sourceKind = ReactiveSnapshotBoolSourceKind.EntityValueStore,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                entityValue = ReactiveEntityValueSource.Create(entity, key),
                fallbackValue = fallbackValue,
            };
        }

        public static ReactiveSnapshotBool KernelValueStore(
            ValueKeyReference key,
            bool fallbackValue = false)
        {
            return new ReactiveSnapshotBool
            {
                sourceKind = ReactiveSnapshotBoolSourceKind.KernelValueStore,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                localValue = ReactiveKernelValueSource.Create(key),
                fallbackValue = fallbackValue,
            };
        }

        public static ReactiveSnapshotBool EntityAlive(ReactiveEntityRef entity)
        {
            return new ReactiveSnapshotBool
            {
                sourceKind = ReactiveSnapshotBoolSourceKind.EntityAlive,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                entityAlive = ReactiveBoolEntityAliveSource.Create(entity),
            };
        }

        public static ReactiveSnapshotBool CompareFloat(
            ReactiveFloat left,
            ReactiveFloat right,
            ReactiveFloatComparisonKind comparison,
            float epsilon = 0.0001f)
        {
            return new ReactiveSnapshotBool
            {
                sourceKind = ReactiveSnapshotBoolSourceKind.CompareFloat,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                compareFloat = ReactiveFloatCompareSource.Create(left, right, comparison, epsilon),
            };
        }
    }
}
