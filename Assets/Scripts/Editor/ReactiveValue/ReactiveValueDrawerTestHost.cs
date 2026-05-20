using BC.ActionSystem;
using BC.Base;
using UnityEngine;

namespace BC.Editor
{
    /// <summary>
    /// Editor-only host used by ReactiveValue EditMode tests to exercise drawer contracts.
    /// </summary>
    internal sealed class ReactiveValueDrawerTestHost : ScriptableObject
    {
        [SerializeField] internal ReactiveFloat reactiveFloat;
        [SerializeField] internal ReactiveInt reactiveInt;
        [SerializeField] internal ReactiveBool reactiveBool;
        [SerializeField] internal ReactiveVector3 reactiveVector3;
        [SerializeField] internal ReactiveEntityRef reactiveEntity;
        [SerializeField] internal ReactiveString reactiveString;
        [SerializeField] internal ReactiveFaceExpressionId reactiveFaceExpression;
        [SerializeField] internal ReactiveEntityMoveState reactiveEntityMoveState;
        [SerializeField] internal ValueStoreWriteAuthoring valueStoreWrite = new();
    }
}