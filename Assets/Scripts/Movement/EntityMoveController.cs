using BC.Utility;
using UnityEngine;

namespace BC.Base
{
    public enum EntityMoveState
    {
        Idle,
        Moving,
        Jumping,
        Falling,
        LedgeHang,
        WallMove,
        Disabled
    }

    public abstract class EntityMoveController : MonoBehaviour
    {
        protected StateMachine<EntityMoveState> StateMachine { get; } = new StateMachine<EntityMoveState>();

        protected SceneKernel SceneKernel { get; private set; }
        protected EntityRef Entity { get; private set; }

        public EntityMoveState State => StateMachine.CurrentState;
        public bool IsRuntimeReady { get; private set; }

        protected virtual void Start()
        {
            InitializeRuntimeReferences();
        }

        protected void InitializeRuntimeReferences()
        {
            SceneKernelMB kernelMB = GetComponentInParent<SceneKernelMB>();

            if (kernelMB == null)
            {
                Debug.LogError($"{nameof(EntityMoveController)}: SceneKernelMB was not found in parent hierarchy.", this);
                enabled = false;
                return;
            }

            if (kernelMB.Kernel == null)
            {
                Debug.LogError($"{nameof(EntityMoveController)}: SceneKernelMB.Kernel is null. Check kernel build order.", this);
                enabled = false;
                return;
            }

            EntityMB entityMB = GetComponentInParent<EntityMB>();

            if (entityMB == null)
            {
                Debug.LogError($"{nameof(EntityMoveController)}: EntityMB was not found in parent hierarchy.", this);
                enabled = false;
                return;
            }

            if (!entityMB.HasEntity)
            {
                Debug.LogError($"{nameof(EntityMoveController)}: EntityMB is not bound yet. Check SceneEntityBootstrapper order.", this);
                enabled = false;
                return;
            }

            SceneKernel = kernelMB.Kernel;
            Entity = entityMB.Entity;

            StateMachine.ChangeState(EntityMoveState.Idle);
            IsRuntimeReady = true;
        }

        protected bool CanReceiveMoveInput()
        {
            if (!IsRuntimeReady)
                return false;

            if (SceneKernel.ValueStore == null)
            {
                Debug.LogError($"{nameof(EntityMoveController)}: SceneKernel.ValueStore is null.", this);
                return false;
            }

            return SceneKernel.ValueStore.Get(Entity, ValueKeys.Move.CanMove);
        }

        protected float GetMoveBaseSpeed(float fallback)
        {
            if (!IsRuntimeReady || SceneKernel.ValueStore == null)
                return fallback;

            return SceneKernel.ValueStore.Get(Entity, ValueKeys.Move.BaseSpeed);
        }

        protected float GetSprintMultiplier(float fallback)
        {
            if (!IsRuntimeReady || SceneKernel.ValueStore == null)
                return fallback;

            return SceneKernel.ValueStore.Get(Entity, ValueKeys.Move.SprintMultiplier);
        }
    }
}