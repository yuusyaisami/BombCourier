using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace BC.Base
{
    public enum ReactiveFloatSourceKind
    {
        Literal = 0,
        EntityValueStore = 1,
        KernelValueStore = 2,
        Distance = 3,
    }

    public enum ReactiveIntSourceKind
    {
        Literal = 0,
        EntityValueStore = 1,
        KernelValueStore = 2,
    }

    public enum ReactiveBoolSourceKind
    {
        Literal = 0,
        EntityValueStore = 1,
        KernelValueStore = 2,
        EntityAlive = 3,
        CompareNumber = 4,
    }

    public enum ReactiveVector3SourceKind
    {
        Literal = 0,
        EntityTransformPosition = 1,
        EntityTransformForward = 2,
        AddPosition = 3,
        AddForward = 4,
        Direction = 5,
    }

    public enum ReactiveEntitySourceKind
    {
        Self = 0,
        TriggerEntity = 1,
        EntityValueStore = 2,
        KernelValueStore = 3,
        TargetReference = 4,
    }

    public enum ReactiveStringSourceKind
    {
        Literal = 0,
        EntityValueStore = 1,
        KernelValueStore = 2,
    }

    public enum ReactiveFaceExpressionIdSourceKind
    {
        Literal = 0,
        EntityValueStore = 1,
        KernelValueStore = 2,
    }

    public enum ReactiveShapeExpressionIdSourceKind
    {
        Literal = 0,
        EntityValueStore = 1,
        KernelValueStore = 2,
    }

    public enum ReactiveEntityMoveStateSourceKind
    {
        Literal = 0,
        EntityValueStore = 1,
        KernelValueStore = 2,
    }

    [Serializable]
    public enum ReactiveKernelValueStoreScope
    {
        SceneKernel = 0,
        ApplicationKernel = 1,
    }

    [Serializable]
    public struct ReactiveKernelValueSource
    {
        [SerializeField] private ReactiveKernelValueStoreScope storeScope;
        [SerializeField] private ValueKeyReference key;

        public ReactiveKernelValueStoreScope StoreScope => storeScope;
        public ValueKeyReference Key => key;

        public static ReactiveKernelValueSource Create(ValueKeyReference key)
        {
            return Create(ReactiveKernelValueStoreScope.SceneKernel, key);
        }

        public static ReactiveKernelValueSource Create(ReactiveKernelValueStoreScope storeScope, ValueKeyReference key)
        {
            return new ReactiveKernelValueSource
            {
                storeScope = storeScope,
                key = key,
            };
        }
    }

    public enum ReactiveScopedEntitySourceKind
    {
        Self = 0,
        TriggerEntity = 1,
    }

    public enum ReactiveEntityFallbackKind
    {
        None = 0,
        Self = 1,
        TriggerEntity = 2,
    }

    public enum ReactiveNumberValueKind
    {
        Float = 0,
        Int = 1,
    }

    public enum ReactiveNumberComparisonKind
    {
        Equal = 0,
        NotEqual = 1,
        Greater = 2,
        GreaterOrEqual = 3,
        Less = 4,
        LessOrEqual = 5,
    }

    public enum ReactiveTransformSourceKind
    {
        Position = 0,
        Forward = 1,
    }

    [Serializable]
    public struct ReactiveEntityValueSource
    {
        [SerializeField] private ReactiveScopedEntitySourceKind entitySourceKind;
        [SerializeField] private ValueKeyReference key;

        public ReactiveScopedEntitySourceKind EntitySourceKind => entitySourceKind;
        public ValueKeyReference Key => key;

        public static ReactiveEntityValueSource Create(ReactiveEntityRef entity, ValueKeyReference key)
        {
            return new ReactiveEntityValueSource
            {
                entitySourceKind = ToScopedEntitySourceKind(entity.SourceKind),
                key = key,
            };
        }

        private static ReactiveScopedEntitySourceKind ToScopedEntitySourceKind(ReactiveEntitySourceKind sourceKind)
        {
            return sourceKind switch
            {
                ReactiveEntitySourceKind.Self => ReactiveScopedEntitySourceKind.Self,
                ReactiveEntitySourceKind.TriggerEntity => ReactiveScopedEntitySourceKind.TriggerEntity,
                _ => throw new ArgumentOutOfRangeException(nameof(sourceKind), sourceKind, "EntityValueStore targets must resolve from Self or TriggerEntity."),
            };
        }
    }

    [Serializable]
    public struct ReactiveFloatDistanceSource
    {
        [SerializeField] private ReactiveEntityRef fromEntity;
        [SerializeField] private ReactiveEntityRef toEntity;

        public ReactiveEntityRef FromEntity => fromEntity;
        public ReactiveEntityRef ToEntity => toEntity;

        public static ReactiveFloatDistanceSource Create(ReactiveEntityRef fromEntity, ReactiveEntityRef toEntity)
        {
            return new ReactiveFloatDistanceSource
            {
                fromEntity = fromEntity,
                toEntity = toEntity,
            };
        }
    }

    [Serializable]
    public struct ReactiveBoolEntityAliveSource
    {
        [SerializeField] private ReactiveEntityRef entity;

        public ReactiveEntityRef Entity => entity;

        public static ReactiveBoolEntityAliveSource Create(ReactiveEntityRef entity)
        {
            return new ReactiveBoolEntityAliveSource
            {
                entity = entity,
            };
        }
    }

    [Serializable]
    public struct ReactiveNumberCompareSource
    {
        [SerializeField] private ReactiveNumberValueKind leftValueKind;
        [FormerlySerializedAs("left")]
        [SerializeField] private ReactiveFloat leftFloat;
        [SerializeField] private ReactiveInt leftInt;
        [SerializeField] private ReactiveNumberValueKind rightValueKind;
        [FormerlySerializedAs("right")]
        [SerializeField] private ReactiveFloat rightFloat;
        [SerializeField] private ReactiveInt rightInt;
        [SerializeField] private ReactiveNumberComparisonKind comparison;
        [SerializeField] private float epsilon;

        public ReactiveNumberValueKind LeftValueKind => leftValueKind;
        public ReactiveFloat LeftFloat => leftFloat;
        public ReactiveInt LeftInt => leftInt;
        public ReactiveNumberValueKind RightValueKind => rightValueKind;
        public ReactiveFloat RightFloat => rightFloat;
        public ReactiveInt RightInt => rightInt;
        public ReactiveNumberComparisonKind Comparison => comparison;
        public float Epsilon => epsilon;

        public static ReactiveNumberCompareSource Create(
            ReactiveFloat left,
            ReactiveFloat right,
            ReactiveNumberComparisonKind comparison,
            float epsilon)
        {
            return new ReactiveNumberCompareSource
            {
                leftValueKind = ReactiveNumberValueKind.Float,
                leftFloat = left,
                rightValueKind = ReactiveNumberValueKind.Float,
                rightFloat = right,
                comparison = comparison,
                epsilon = Mathf.Max(0f, epsilon),
            };
        }

        public static ReactiveNumberCompareSource Create(
            ReactiveInt left,
            ReactiveInt right,
            ReactiveNumberComparisonKind comparison,
            float epsilon)
        {
            return new ReactiveNumberCompareSource
            {
                leftValueKind = ReactiveNumberValueKind.Int,
                leftInt = left,
                rightValueKind = ReactiveNumberValueKind.Int,
                rightInt = right,
                comparison = comparison,
                epsilon = Mathf.Max(0f, epsilon),
            };
        }

        public static ReactiveNumberCompareSource Create(
            ReactiveFloat left,
            ReactiveInt right,
            ReactiveNumberComparisonKind comparison,
            float epsilon)
        {
            return new ReactiveNumberCompareSource
            {
                leftValueKind = ReactiveNumberValueKind.Float,
                leftFloat = left,
                rightValueKind = ReactiveNumberValueKind.Int,
                rightInt = right,
                comparison = comparison,
                epsilon = Mathf.Max(0f, epsilon),
            };
        }

        public static ReactiveNumberCompareSource Create(
            ReactiveInt left,
            ReactiveFloat right,
            ReactiveNumberComparisonKind comparison,
            float epsilon)
        {
            return new ReactiveNumberCompareSource
            {
                leftValueKind = ReactiveNumberValueKind.Int,
                leftInt = left,
                rightValueKind = ReactiveNumberValueKind.Float,
                rightFloat = right,
                comparison = comparison,
                epsilon = Mathf.Max(0f, epsilon),
            };
        }
    }

    [Serializable]
    public struct ReactiveTransformVectorSource
    {
        [SerializeField] private ReactiveTransformSourceKind sourceKind;
        [SerializeField] private ReactiveEntityRef entity;

        public ReactiveTransformSourceKind SourceKind => sourceKind;
        public ReactiveEntityRef Entity => entity;

        public static ReactiveTransformVectorSource Position(ReactiveEntityRef entity)
        {
            return new ReactiveTransformVectorSource
            {
                sourceKind = ReactiveTransformSourceKind.Position,
                entity = entity,
            };
        }

        public static ReactiveTransformVectorSource Forward(ReactiveEntityRef entity)
        {
            return new ReactiveTransformVectorSource
            {
                sourceKind = ReactiveTransformSourceKind.Forward,
                entity = entity,
            };
        }
    }

    [Serializable]
    public struct ReactiveVector3AddSource
    {
        [SerializeField] private ReactiveTransformVectorSource baseValue;
        [SerializeField] private Vector3 addend;

        public ReactiveTransformVectorSource BaseValue => baseValue;
        public Vector3 Addend => addend;

        public static ReactiveVector3AddSource Create(ReactiveTransformVectorSource baseValue, Vector3 addend)
        {
            return new ReactiveVector3AddSource
            {
                baseValue = baseValue,
                addend = addend,
            };
        }
    }

    [Serializable]
    public struct ReactiveVector3DirectionSource
    {
        [SerializeField] private ReactiveEntityRef fromEntity;
        [SerializeField] private ReactiveEntityRef toEntity;

        public ReactiveEntityRef FromEntity => fromEntity;
        public ReactiveEntityRef ToEntity => toEntity;

        public static ReactiveVector3DirectionSource Create(ReactiveEntityRef fromEntity, ReactiveEntityRef toEntity)
        {
            return new ReactiveVector3DirectionSource
            {
                fromEntity = fromEntity,
                toEntity = toEntity,
            };
        }
    }

    [Serializable]
    public struct ReactiveFloat
    {
        [SerializeField] private ReactiveFloatSourceKind sourceKind;
        [SerializeField] private ReactiveEvaluationMode evaluationMode;
        [SerializeField] private ReactiveFailurePolicy failurePolicy;
        [SerializeField] private float literal;
        [SerializeField] private ReactiveEntityValueSource entityValue;
        [SerializeField] private ReactiveKernelValueSource localValue;
        [SerializeField] private ReactiveFloatDistanceSource distance;
        [SerializeField] private float fallbackValue;

        public ReactiveFloatSourceKind SourceKind => sourceKind;
        public ReactiveEvaluationMode EvaluationMode => evaluationMode;
        public ReactiveFailurePolicy FailurePolicy => failurePolicy;
        public float Literal => literal;
        public ReactiveEntityValueSource EntityValue => entityValue;
        public ReactiveKernelValueSource LocalValue => localValue;
        public ReactiveFloatDistanceSource DistanceSource => distance;
        public float FallbackValue => fallbackValue;

        public static ReactiveFloat LiteralValue(float value, ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Snapshot)
        {
            return new ReactiveFloat
            {
                sourceKind = ReactiveFloatSourceKind.Literal,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                literal = value,
                fallbackValue = value,
            };
        }

        public static ReactiveFloat EntityValueStore(
            ReactiveEntityRef entity,
            ValueKeyReference key,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Watched)
        {
            return new ReactiveFloat
            {
                sourceKind = ReactiveFloatSourceKind.EntityValueStore,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                entityValue = ReactiveEntityValueSource.Create(entity, key),
            };
        }

        public static ReactiveFloat KernelValueStore(
            ValueKeyReference key,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Watched)
        {
            return new ReactiveFloat
            {
                sourceKind = ReactiveFloatSourceKind.KernelValueStore,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                localValue = ReactiveKernelValueSource.Create(key),
            };
        }

        public static ReactiveFloat Distance(
            ReactiveEntityRef fromEntity,
            ReactiveEntityRef toEntity,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Snapshot)
        {
            return new ReactiveFloat
            {
                sourceKind = ReactiveFloatSourceKind.Distance,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                distance = ReactiveFloatDistanceSource.Create(fromEntity, toEntity),
            };
        }
    }

    [Serializable]
    public struct ReactiveInt
    {
        [SerializeField] private ReactiveIntSourceKind sourceKind;
        [SerializeField] private ReactiveEvaluationMode evaluationMode;
        [SerializeField] private ReactiveFailurePolicy failurePolicy;
        [SerializeField] private int literal;
        [SerializeField] private ReactiveEntityValueSource entityValue;
        [SerializeField] private ReactiveKernelValueSource localValue;
        [SerializeField] private int fallbackValue;

        public ReactiveIntSourceKind SourceKind => sourceKind;
        public ReactiveEvaluationMode EvaluationMode => evaluationMode;
        public ReactiveFailurePolicy FailurePolicy => failurePolicy;
        public int Literal => literal;
        public ReactiveEntityValueSource EntityValue => entityValue;
        public ReactiveKernelValueSource LocalValue => localValue;
        public int FallbackValue => fallbackValue;

        public static ReactiveInt LiteralValue(int value, ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Snapshot)
        {
            return new ReactiveInt
            {
                sourceKind = ReactiveIntSourceKind.Literal,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                literal = value,
                fallbackValue = value,
            };
        }

        public static ReactiveInt EntityValueStore(
            ReactiveEntityRef entity,
            ValueKeyReference key,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Watched)
        {
            return new ReactiveInt
            {
                sourceKind = ReactiveIntSourceKind.EntityValueStore,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                entityValue = ReactiveEntityValueSource.Create(entity, key),
            };
        }

        public static ReactiveInt KernelValueStore(
            ValueKeyReference key,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Watched)
        {
            return new ReactiveInt
            {
                sourceKind = ReactiveIntSourceKind.KernelValueStore,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                localValue = ReactiveKernelValueSource.Create(key),
            };
        }

    }

    [Serializable]
    public struct ReactiveBool
    {
        [SerializeField] private ReactiveBoolSourceKind sourceKind;
        [SerializeField] private ReactiveEvaluationMode evaluationMode;
        [SerializeField] private ReactiveFailurePolicy failurePolicy;
        [SerializeField] private bool literal;
        [SerializeField] private ReactiveEntityValueSource entityValue;
        [SerializeField] private ReactiveKernelValueSource localValue;
        [SerializeField] private ReactiveBoolEntityAliveSource entityAlive;
        [FormerlySerializedAs("compareFloat")]
        [SerializeField] private ReactiveNumberCompareSource compareNumber;
        [SerializeField] private bool fallbackValue;

        public ReactiveBoolSourceKind SourceKind => sourceKind;
        public ReactiveEvaluationMode EvaluationMode => evaluationMode;
        public ReactiveFailurePolicy FailurePolicy => failurePolicy;
        public bool Literal => literal;
        public ReactiveEntityValueSource EntityValue => entityValue;
        public ReactiveKernelValueSource LocalValue => localValue;
        public ReactiveBoolEntityAliveSource EntityAliveSource => entityAlive;
        public ReactiveNumberCompareSource CompareNumberSource => compareNumber;
        public bool FallbackValue => fallbackValue;

        public static ReactiveBool LiteralValue(bool value, ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Snapshot)
        {
            return new ReactiveBool
            {
                sourceKind = ReactiveBoolSourceKind.Literal,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                literal = value,
                fallbackValue = value,
            };
        }

        public static ReactiveBool EntityValueStore(
            ReactiveEntityRef entity,
            ValueKeyReference key,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Watched)
        {
            return new ReactiveBool
            {
                sourceKind = ReactiveBoolSourceKind.EntityValueStore,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                entityValue = ReactiveEntityValueSource.Create(entity, key),
            };
        }

        public static ReactiveBool KernelValueStore(
            ValueKeyReference key,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Watched)
        {
            return new ReactiveBool
            {
                sourceKind = ReactiveBoolSourceKind.KernelValueStore,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                localValue = ReactiveKernelValueSource.Create(key),
            };
        }

        public static ReactiveBool EntityAlive(
            ReactiveEntityRef entity,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Snapshot)
        {
            return new ReactiveBool
            {
                sourceKind = ReactiveBoolSourceKind.EntityAlive,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                entityAlive = ReactiveBoolEntityAliveSource.Create(entity),
            };
        }

        public static ReactiveBool CompareNumber(
            ReactiveFloat left,
            ReactiveFloat right,
            ReactiveNumberComparisonKind comparison,
            float epsilon = 0.0001f,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Snapshot)
        {
            return new ReactiveBool
            {
                sourceKind = ReactiveBoolSourceKind.CompareNumber,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                compareNumber = ReactiveNumberCompareSource.Create(left, right, comparison, epsilon),
            };
        }

        public static ReactiveBool CompareNumber(
            ReactiveInt left,
            ReactiveInt right,
            ReactiveNumberComparisonKind comparison,
            float epsilon = 0.0001f,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Snapshot)
        {
            return new ReactiveBool
            {
                sourceKind = ReactiveBoolSourceKind.CompareNumber,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                compareNumber = ReactiveNumberCompareSource.Create(left, right, comparison, epsilon),
            };
        }

        public static ReactiveBool CompareNumber(
            ReactiveFloat left,
            ReactiveInt right,
            ReactiveNumberComparisonKind comparison,
            float epsilon = 0.0001f,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Snapshot)
        {
            return new ReactiveBool
            {
                sourceKind = ReactiveBoolSourceKind.CompareNumber,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                compareNumber = ReactiveNumberCompareSource.Create(left, right, comparison, epsilon),
            };
        }

        public static ReactiveBool CompareNumber(
            ReactiveInt left,
            ReactiveFloat right,
            ReactiveNumberComparisonKind comparison,
            float epsilon = 0.0001f,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Snapshot)
        {
            return new ReactiveBool
            {
                sourceKind = ReactiveBoolSourceKind.CompareNumber,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                compareNumber = ReactiveNumberCompareSource.Create(left, right, comparison, epsilon),
            };
        }
    }

    [Serializable]
    public struct ReactiveVector3
    {
        [SerializeField] private ReactiveVector3SourceKind sourceKind;
        [SerializeField] private ReactiveEvaluationMode evaluationMode;
        [SerializeField] private ReactiveFailurePolicy failurePolicy;
        [SerializeField] private Vector3 literal;
        [SerializeField] private ReactiveTransformVectorSource transformValue;
        [SerializeField] private ReactiveVector3AddSource addSource;
        [SerializeField] private ReactiveVector3DirectionSource directionSource;
        [SerializeField] private Vector3 fallbackValue;

        public ReactiveVector3SourceKind SourceKind => sourceKind;
        public ReactiveEvaluationMode EvaluationMode => evaluationMode;
        public ReactiveFailurePolicy FailurePolicy => failurePolicy;
        public Vector3 Literal => literal;
        public ReactiveTransformVectorSource TransformValue => transformValue;
        public ReactiveVector3AddSource AddSource => addSource;
        public ReactiveVector3DirectionSource DirectionSource => directionSource;
        public Vector3 FallbackValue => fallbackValue;

        public static ReactiveVector3 LiteralValue(Vector3 value, ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Snapshot)
        {
            return new ReactiveVector3
            {
                sourceKind = ReactiveVector3SourceKind.Literal,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                literal = value,
                fallbackValue = value,
            };
        }

        public static ReactiveVector3 EntityTransformPosition(
            ReactiveEntityRef entity,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Snapshot)
        {
            return new ReactiveVector3
            {
                sourceKind = ReactiveVector3SourceKind.EntityTransformPosition,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                transformValue = ReactiveTransformVectorSource.Position(entity),
            };
        }

        public static ReactiveVector3 EntityTransformForward(
            ReactiveEntityRef entity,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Snapshot)
        {
            return new ReactiveVector3
            {
                sourceKind = ReactiveVector3SourceKind.EntityTransformForward,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                transformValue = ReactiveTransformVectorSource.Forward(entity),
            };
        }

        public static ReactiveVector3 AddPosition(
            ReactiveEntityRef entity,
            Vector3 addend,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Snapshot)
        {
            return new ReactiveVector3
            {
                sourceKind = ReactiveVector3SourceKind.AddPosition,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                addSource = ReactiveVector3AddSource.Create(ReactiveTransformVectorSource.Position(entity), addend),
            };
        }

        public static ReactiveVector3 AddForward(
            ReactiveEntityRef entity,
            Vector3 addend,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Snapshot)
        {
            return new ReactiveVector3
            {
                sourceKind = ReactiveVector3SourceKind.AddForward,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                addSource = ReactiveVector3AddSource.Create(ReactiveTransformVectorSource.Forward(entity), addend),
            };
        }

        public static ReactiveVector3 Direction(
            ReactiveEntityRef fromEntity,
            ReactiveEntityRef toEntity,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Snapshot)
        {
            return new ReactiveVector3
            {
                sourceKind = ReactiveVector3SourceKind.Direction,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                directionSource = ReactiveVector3DirectionSource.Create(fromEntity, toEntity),
            };
        }
    }

    [Serializable]
    public struct ReactiveEntityRef
    {
        [SerializeField] private ReactiveEntitySourceKind sourceKind;
        [SerializeField] private ReactiveEvaluationMode evaluationMode;
        [SerializeField] private ReactiveFailurePolicy failurePolicy;
        [SerializeField] private ReactiveEntityValueSource entityValue;
        [SerializeField] private ReactiveKernelValueSource localValue;
        [SerializeField] private EntityTargetReference targetReference;
        [SerializeField] private ReactiveEntityFallbackKind fallbackKind;

        public ReactiveEntitySourceKind SourceKind => sourceKind;
        public ReactiveEvaluationMode EvaluationMode => evaluationMode;
        public ReactiveFailurePolicy FailurePolicy => failurePolicy;
        public ReactiveEntityValueSource EntityValue => entityValue;
        public ReactiveKernelValueSource LocalValue => localValue;
        public EntityTargetReference TargetReferenceValue => targetReference;
        public ReactiveEntityFallbackKind FallbackKind => fallbackKind;

        public static ReactiveEntityRef Self()
        {
            return new ReactiveEntityRef
            {
                sourceKind = ReactiveEntitySourceKind.Self,
                evaluationMode = ReactiveEvaluationMode.Snapshot,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                fallbackKind = ReactiveEntityFallbackKind.Self,
            };
        }

        public static ReactiveEntityRef TriggerEntity()
        {
            return new ReactiveEntityRef
            {
                sourceKind = ReactiveEntitySourceKind.TriggerEntity,
                evaluationMode = ReactiveEvaluationMode.Snapshot,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                fallbackKind = ReactiveEntityFallbackKind.TriggerEntity,
            };
        }

        public static ReactiveEntityRef EntityValueStore(
            ReactiveEntityRef entity,
            ValueKeyReference key,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Watched)
        {
            return new ReactiveEntityRef
            {
                sourceKind = ReactiveEntitySourceKind.EntityValueStore,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                entityValue = ReactiveEntityValueSource.Create(entity, key),
                fallbackKind = ReactiveEntityFallbackKind.None,
            };
        }

        public static ReactiveEntityRef KernelValueStore(
            ValueKeyReference key,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Watched)
        {
            return new ReactiveEntityRef
            {
                sourceKind = ReactiveEntitySourceKind.KernelValueStore,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                localValue = ReactiveKernelValueSource.Create(key),
                fallbackKind = ReactiveEntityFallbackKind.None,
            };
        }

        public static ReactiveEntityRef TargetReference(
            EntityTargetReference targetReference,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Snapshot)
        {
            return new ReactiveEntityRef
            {
                sourceKind = ReactiveEntitySourceKind.TargetReference,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                targetReference = targetReference,
                fallbackKind = ReactiveEntityFallbackKind.None,
            };
        }
    }

    [Serializable]
    public struct ReactiveString
    {
        [SerializeField] private ReactiveStringSourceKind sourceKind;
        [SerializeField] private ReactiveEvaluationMode evaluationMode;
        [SerializeField] private ReactiveFailurePolicy failurePolicy;
        [SerializeField] private string literal;
        [SerializeField] private ReactiveEntityValueSource entityValue;
        [SerializeField] private ReactiveKernelValueSource localValue;
        [SerializeField] private string fallbackValue;

        public ReactiveStringSourceKind SourceKind => sourceKind;
        public ReactiveEvaluationMode EvaluationMode => evaluationMode;
        public ReactiveFailurePolicy FailurePolicy => failurePolicy;
        public string Literal => literal;
        public ReactiveEntityValueSource EntityValue => entityValue;
        public ReactiveKernelValueSource LocalValue => localValue;
        public string FallbackValue => fallbackValue;

        public static ReactiveString LiteralValue(string value, ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Snapshot)
        {
            return new ReactiveString
            {
                sourceKind = ReactiveStringSourceKind.Literal,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                literal = value,
                fallbackValue = value,
            };
        }

        public static ReactiveString EntityValueStore(
            ReactiveEntityRef entity,
            ValueKeyReference key,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Watched)
        {
            return new ReactiveString
            {
                sourceKind = ReactiveStringSourceKind.EntityValueStore,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                entityValue = ReactiveEntityValueSource.Create(entity, key),
            };
        }

        public static ReactiveString KernelValueStore(
            ValueKeyReference key,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Watched)
        {
            return new ReactiveString
            {
                sourceKind = ReactiveStringSourceKind.KernelValueStore,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                localValue = ReactiveKernelValueSource.Create(key),
            };
        }
    }

    [Serializable]
    public struct ReactiveFaceExpressionId
    {
        [SerializeField] private ReactiveFaceExpressionIdSourceKind sourceKind;
        [SerializeField] private ReactiveEvaluationMode evaluationMode;
        [SerializeField] private ReactiveFailurePolicy failurePolicy;
        [SerializeField] private FaceExpressionId literal;
        [SerializeField] private ReactiveEntityValueSource entityValue;
        [SerializeField] private ReactiveKernelValueSource localValue;
        [SerializeField] private FaceExpressionId fallbackValue;

        public ReactiveFaceExpressionIdSourceKind SourceKind => sourceKind;
        public ReactiveEvaluationMode EvaluationMode => evaluationMode;
        public ReactiveFailurePolicy FailurePolicy => failurePolicy;
        public FaceExpressionId Literal => literal;
        public ReactiveEntityValueSource EntityValue => entityValue;
        public ReactiveKernelValueSource LocalValue => localValue;
        public FaceExpressionId FallbackValue => fallbackValue;

        public static ReactiveFaceExpressionId LiteralValue(FaceExpressionId value, ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Snapshot)
        {
            return new ReactiveFaceExpressionId
            {
                sourceKind = ReactiveFaceExpressionIdSourceKind.Literal,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                literal = value,
                fallbackValue = value,
            };
        }

        public static ReactiveFaceExpressionId EntityValueStore(
            ReactiveEntityRef entity,
            ValueKeyReference key,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Watched)
        {
            return new ReactiveFaceExpressionId
            {
                sourceKind = ReactiveFaceExpressionIdSourceKind.EntityValueStore,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                entityValue = ReactiveEntityValueSource.Create(entity, key),
            };
        }

        public static ReactiveFaceExpressionId KernelValueStore(
            ValueKeyReference key,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Watched)
        {
            return new ReactiveFaceExpressionId
            {
                sourceKind = ReactiveFaceExpressionIdSourceKind.KernelValueStore,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                localValue = ReactiveKernelValueSource.Create(key),
            };
        }
    }

    [Serializable]
    public struct ReactiveShapeExpressionId
    {
        [SerializeField] private ReactiveShapeExpressionIdSourceKind sourceKind;
        [SerializeField] private ReactiveEvaluationMode evaluationMode;
        [SerializeField] private ReactiveFailurePolicy failurePolicy;
        [SerializeField] private ShapeExpressionId literal;
        [SerializeField] private ReactiveEntityValueSource entityValue;
        [SerializeField] private ReactiveKernelValueSource localValue;
        [SerializeField] private ShapeExpressionId fallbackValue;

        public ReactiveShapeExpressionIdSourceKind SourceKind => sourceKind;
        public ReactiveEvaluationMode EvaluationMode => evaluationMode;
        public ReactiveFailurePolicy FailurePolicy => failurePolicy;
        public ShapeExpressionId Literal => literal;
        public ReactiveEntityValueSource EntityValue => entityValue;
        public ReactiveKernelValueSource LocalValue => localValue;
        public ShapeExpressionId FallbackValue => fallbackValue;

        public static ReactiveShapeExpressionId LiteralValue(ShapeExpressionId value, ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Snapshot)
        {
            return new ReactiveShapeExpressionId
            {
                sourceKind = ReactiveShapeExpressionIdSourceKind.Literal,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                literal = value,
                fallbackValue = value,
            };
        }

        public static ReactiveShapeExpressionId EntityValueStore(
            ReactiveEntityRef entity,
            ValueKeyReference key,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Watched)
        {
            return new ReactiveShapeExpressionId
            {
                sourceKind = ReactiveShapeExpressionIdSourceKind.EntityValueStore,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                entityValue = ReactiveEntityValueSource.Create(entity, key),
            };
        }

        public static ReactiveShapeExpressionId KernelValueStore(
            ValueKeyReference key,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Watched)
        {
            return new ReactiveShapeExpressionId
            {
                sourceKind = ReactiveShapeExpressionIdSourceKind.KernelValueStore,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                localValue = ReactiveKernelValueSource.Create(key),
            };
        }
    }

    [Serializable]
    public struct ReactiveEntityMoveState
    {
        [SerializeField] private ReactiveEntityMoveStateSourceKind sourceKind;
        [SerializeField] private ReactiveEvaluationMode evaluationMode;
        [SerializeField] private ReactiveFailurePolicy failurePolicy;
        [SerializeField] private EntityMoveState literal;
        [SerializeField] private ReactiveEntityValueSource entityValue;
        [SerializeField] private ReactiveKernelValueSource localValue;
        [SerializeField] private EntityMoveState fallbackValue;

        public ReactiveEntityMoveStateSourceKind SourceKind => sourceKind;
        public ReactiveEvaluationMode EvaluationMode => evaluationMode;
        public ReactiveFailurePolicy FailurePolicy => failurePolicy;
        public EntityMoveState Literal => literal;
        public ReactiveEntityValueSource EntityValue => entityValue;
        public ReactiveKernelValueSource LocalValue => localValue;
        public EntityMoveState FallbackValue => fallbackValue;

        public static ReactiveEntityMoveState LiteralValue(EntityMoveState value, ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Snapshot)
        {
            return new ReactiveEntityMoveState
            {
                sourceKind = ReactiveEntityMoveStateSourceKind.Literal,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                literal = value,
                fallbackValue = value,
            };
        }

        public static ReactiveEntityMoveState EntityValueStore(
            ReactiveEntityRef entity,
            ValueKeyReference key,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Watched)
        {
            return new ReactiveEntityMoveState
            {
                sourceKind = ReactiveEntityMoveStateSourceKind.EntityValueStore,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                entityValue = ReactiveEntityValueSource.Create(entity, key),
            };
        }

        public static ReactiveEntityMoveState KernelValueStore(
            ValueKeyReference key,
            ReactiveEvaluationMode evaluationMode = ReactiveEvaluationMode.Watched)
        {
            return new ReactiveEntityMoveState
            {
                sourceKind = ReactiveEntityMoveStateSourceKind.KernelValueStore,
                evaluationMode = evaluationMode,
                failurePolicy = ReactiveFailurePolicy.FailAction,
                localValue = ReactiveKernelValueSource.Create(key),
            };
        }
    }
}