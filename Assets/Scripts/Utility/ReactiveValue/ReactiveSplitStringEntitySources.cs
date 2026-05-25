using System;
using UnityEngine;

namespace BC.Base
{
    public enum ReactiveWatchedStringSourceKind
    {
        EntityValueStore = 0,
        LocalValueStore = 1,
    }

    public enum ReactiveSnapshotStringSourceKind
    {
        Literal = 0,
        EntityValueStore = 1,
        LocalValueStore = 2,
    }

    [Serializable]
    public struct ReactiveWatchedString
    {
        [SerializeField] private ReactiveWatchedStringSourceKind sourceKind;
        [SerializeField] private ReactiveFailurePolicy failurePolicy;
        [SerializeField] private ReactiveEntityValueSource entityValue;
        [SerializeField] private ReactiveLocalValueSource localValue;
        [SerializeField] private string fallbackValue;

        public ReactiveWatchedStringSourceKind SourceKind => sourceKind;
        public ReactiveFailurePolicy FailurePolicy => failurePolicy;
        public ReactiveEntityValueSource EntityValue => entityValue;
        public ReactiveLocalValueSource LocalValue => localValue;
        public string FallbackValue => fallbackValue;

        public static ReactiveWatchedString EntityValueStore(
            ReactiveEntityRef entity,
            ValueKeyReference key,
            string fallbackValue = "")
        {
            return new ReactiveWatchedString
            {
                sourceKind = ReactiveWatchedStringSourceKind.EntityValueStore,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                entityValue = ReactiveEntityValueSource.Create(entity, key),
                fallbackValue = fallbackValue ?? string.Empty,
            };
        }

        public static ReactiveWatchedString LocalValueStore(
            ValueKeyReference key,
            string fallbackValue = "")
        {
            return new ReactiveWatchedString
            {
                sourceKind = ReactiveWatchedStringSourceKind.LocalValueStore,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                localValue = ReactiveLocalValueSource.Create(key),
                fallbackValue = fallbackValue ?? string.Empty,
            };
        }
    }

    [Serializable]
    public struct ReactiveSnapshotString
    {
        [SerializeField] private ReactiveSnapshotStringSourceKind sourceKind;
        [SerializeField] private ReactiveFailurePolicy failurePolicy;
        [SerializeField] private string literal;
        [SerializeField] private ReactiveEntityValueSource entityValue;
        [SerializeField] private ReactiveLocalValueSource localValue;
        [SerializeField] private string fallbackValue;

        public ReactiveSnapshotStringSourceKind SourceKind => sourceKind;
        public ReactiveFailurePolicy FailurePolicy => failurePolicy;
        public string Literal => literal;
        public ReactiveEntityValueSource EntityValue => entityValue;
        public ReactiveLocalValueSource LocalValue => localValue;
        public string FallbackValue => fallbackValue;

        public static ReactiveSnapshotString LiteralValue(string value)
        {
            string resolved = value ?? string.Empty;
            return new ReactiveSnapshotString
            {
                sourceKind = ReactiveSnapshotStringSourceKind.Literal,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                literal = resolved,
                fallbackValue = resolved,
            };
        }

        public static ReactiveSnapshotString EntityValueStore(
            ReactiveEntityRef entity,
            ValueKeyReference key,
            string fallbackValue = "")
        {
            return new ReactiveSnapshotString
            {
                sourceKind = ReactiveSnapshotStringSourceKind.EntityValueStore,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                entityValue = ReactiveEntityValueSource.Create(entity, key),
                fallbackValue = fallbackValue ?? string.Empty,
            };
        }

        public static ReactiveSnapshotString LocalValueStore(
            ValueKeyReference key,
            string fallbackValue = "")
        {
            return new ReactiveSnapshotString
            {
                sourceKind = ReactiveSnapshotStringSourceKind.LocalValueStore,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                localValue = ReactiveLocalValueSource.Create(key),
                fallbackValue = fallbackValue ?? string.Empty,
            };
        }
    }

    public enum ReactiveWatchedEntityRefSourceKind
    {
        EntityValueStore = 0,
        LocalValueStore = 1,
    }

    public enum ReactiveSnapshotEntityRefSourceKind
    {
        Self = 0,
        TriggerEntity = 1,
        EntityValueStore = 2,
        LocalValueStore = 3,
        TargetReference = 4,
    }

    [Serializable]
    public struct ReactiveWatchedEntityRef
    {
        [SerializeField] private ReactiveWatchedEntityRefSourceKind sourceKind;
        [SerializeField] private ReactiveFailurePolicy failurePolicy;
        [SerializeField] private ReactiveEntityValueSource entityValue;
        [SerializeField] private ReactiveLocalValueSource localValue;
        [SerializeField] private ReactiveEntityFallbackKind fallbackKind;

        public ReactiveWatchedEntityRefSourceKind SourceKind => sourceKind;
        public ReactiveFailurePolicy FailurePolicy => failurePolicy;
        public ReactiveEntityValueSource EntityValue => entityValue;
        public ReactiveLocalValueSource LocalValue => localValue;
        public ReactiveEntityFallbackKind FallbackKind => fallbackKind;

        public static ReactiveWatchedEntityRef EntityValueStore(
            ReactiveEntityRef entity,
            ValueKeyReference key,
            ReactiveEntityFallbackKind fallbackKind = ReactiveEntityFallbackKind.None)
        {
            return new ReactiveWatchedEntityRef
            {
                sourceKind = ReactiveWatchedEntityRefSourceKind.EntityValueStore,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                entityValue = ReactiveEntityValueSource.Create(entity, key),
                fallbackKind = fallbackKind,
            };
        }

        public static ReactiveWatchedEntityRef LocalValueStore(
            ValueKeyReference key,
            ReactiveEntityFallbackKind fallbackKind = ReactiveEntityFallbackKind.None)
        {
            return new ReactiveWatchedEntityRef
            {
                sourceKind = ReactiveWatchedEntityRefSourceKind.LocalValueStore,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                localValue = ReactiveLocalValueSource.Create(key),
                fallbackKind = fallbackKind,
            };
        }
    }

    [Serializable]
    public struct ReactiveSnapshotEntityRef
    {
        [SerializeField] private ReactiveSnapshotEntityRefSourceKind sourceKind;
        [SerializeField] private ReactiveFailurePolicy failurePolicy;
        [SerializeField] private ReactiveEntityValueSource entityValue;
        [SerializeField] private ReactiveLocalValueSource localValue;
        [SerializeField] private EntityTargetReference targetReference;
        [SerializeField] private ReactiveEntityFallbackKind fallbackKind;

        public ReactiveSnapshotEntityRefSourceKind SourceKind => sourceKind;
        public ReactiveFailurePolicy FailurePolicy => failurePolicy;
        public ReactiveEntityValueSource EntityValue => entityValue;
        public ReactiveLocalValueSource LocalValue => localValue;
        public EntityTargetReference TargetReferenceValue => targetReference;
        public ReactiveEntityFallbackKind FallbackKind => fallbackKind;

        public static ReactiveSnapshotEntityRef Self()
        {
            return new ReactiveSnapshotEntityRef
            {
                sourceKind = ReactiveSnapshotEntityRefSourceKind.Self,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                fallbackKind = ReactiveEntityFallbackKind.Self,
            };
        }

        public static ReactiveSnapshotEntityRef TriggerEntity()
        {
            return new ReactiveSnapshotEntityRef
            {
                sourceKind = ReactiveSnapshotEntityRefSourceKind.TriggerEntity,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                fallbackKind = ReactiveEntityFallbackKind.TriggerEntity,
            };
        }

        public static ReactiveSnapshotEntityRef EntityValueStore(
            ReactiveEntityRef entity,
            ValueKeyReference key,
            ReactiveEntityFallbackKind fallbackKind = ReactiveEntityFallbackKind.None)
        {
            return new ReactiveSnapshotEntityRef
            {
                sourceKind = ReactiveSnapshotEntityRefSourceKind.EntityValueStore,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                entityValue = ReactiveEntityValueSource.Create(entity, key),
                fallbackKind = fallbackKind,
            };
        }

        public static ReactiveSnapshotEntityRef LocalValueStore(
            ValueKeyReference key,
            ReactiveEntityFallbackKind fallbackKind = ReactiveEntityFallbackKind.None)
        {
            return new ReactiveSnapshotEntityRef
            {
                sourceKind = ReactiveSnapshotEntityRefSourceKind.LocalValueStore,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                localValue = ReactiveLocalValueSource.Create(key),
                fallbackKind = fallbackKind,
            };
        }

        public static ReactiveSnapshotEntityRef TargetReference(
            EntityTargetReference targetReference,
            ReactiveEntityFallbackKind fallbackKind = ReactiveEntityFallbackKind.None)
        {
            return new ReactiveSnapshotEntityRef
            {
                sourceKind = ReactiveSnapshotEntityRefSourceKind.TargetReference,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                targetReference = targetReference,
                fallbackKind = fallbackKind,
            };
        }
    }
}
