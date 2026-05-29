using System;
using UnityEngine;
using UnityEngine.Serialization;

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
        CompareNumber = 4,
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
        [FormerlySerializedAs("compareFloat")]
        [SerializeField] private ReactiveNumberCompareSource compareNumber;
        [SerializeField] private bool fallbackValue;

        public ReactiveSnapshotBoolSourceKind SourceKind => sourceKind;
        public ReactiveFailurePolicy FailurePolicy => failurePolicy;
        public bool Literal => literal;
        public ReactiveEntityValueSource EntityValue => entityValue;
        public ReactiveKernelValueSource LocalValue => localValue;
        public ReactiveBoolEntityAliveSource EntityAliveSource => entityAlive;
        public ReactiveNumberCompareSource CompareNumberSource => compareNumber;
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

        public static ReactiveSnapshotBool CompareNumber(
            ReactiveFloat left,
            ReactiveFloat right,
            ReactiveNumberComparisonKind comparison,
            float epsilon = 0.0001f)
        {
            return new ReactiveSnapshotBool
            {
                sourceKind = ReactiveSnapshotBoolSourceKind.CompareNumber,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                compareNumber = ReactiveNumberCompareSource.Create(left, right, comparison, epsilon),
            };
        }

        public static ReactiveSnapshotBool CompareNumber(
            ReactiveInt left,
            ReactiveInt right,
            ReactiveNumberComparisonKind comparison,
            float epsilon = 0.0001f)
        {
            return new ReactiveSnapshotBool
            {
                sourceKind = ReactiveSnapshotBoolSourceKind.CompareNumber,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                compareNumber = ReactiveNumberCompareSource.Create(left, right, comparison, epsilon),
            };
        }

        public static ReactiveSnapshotBool CompareNumber(
            ReactiveFloat left,
            ReactiveInt right,
            ReactiveNumberComparisonKind comparison,
            float epsilon = 0.0001f)
        {
            return new ReactiveSnapshotBool
            {
                sourceKind = ReactiveSnapshotBoolSourceKind.CompareNumber,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                compareNumber = ReactiveNumberCompareSource.Create(left, right, comparison, epsilon),
            };
        }

        public static ReactiveSnapshotBool CompareNumber(
            ReactiveInt left,
            ReactiveFloat right,
            ReactiveNumberComparisonKind comparison,
            float epsilon = 0.0001f)
        {
            return new ReactiveSnapshotBool
            {
                sourceKind = ReactiveSnapshotBoolSourceKind.CompareNumber,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                compareNumber = ReactiveNumberCompareSource.Create(left, right, comparison, epsilon),
            };
        }
    }
}
