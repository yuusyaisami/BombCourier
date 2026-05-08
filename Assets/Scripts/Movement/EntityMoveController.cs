using BC.Utility;
using UnityEngine;
namespace BC.Base
{
    public enum EntityMoveState
    {
        Idle,
        Moving,
        Jumping,
        Falling
    }
    public abstract class EntityMoveController : MonoBehaviour
    {
        StateMachine<EntityMoveState> stateMachine { get; }
        SceneKernel SceneKernel { get; }
        EntityRef Entity { get; } // 自分
        EntityMoveState State => stateMachine.CurrentState;
        public bool IsActive => SceneKernel.ValueStore.Get<bool>(Entity, ValueKeys.Move.CanMove);
    }
}