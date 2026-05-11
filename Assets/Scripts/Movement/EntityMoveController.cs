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
        Dead,
        Disabled
    }

    public abstract class EntityMoveController : MonoBehaviour
    {
        protected StateMachine<EntityMoveState> StateMachine { get; } = new StateMachine<EntityMoveState>();

        protected SceneKernel SceneKernel { get; private set; }
        protected EntityRef Entity { get; private set; }

        private ValueWatchHandle<bool> canMoveHandle;
        private ValueWatchHandle<float> moveBaseSpeedHandle;
        private ValueWatchHandle<float> sprintMultiplierHandle;
        private ValueWatchHandle<float> jumpHeightMultiplierHandle;

        public EntityMoveState MoveState => StateMachine.CurrentState;
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
                Debug.LogError($"{nameof(EntityMoveController)}: SceneKernelMB was not found.", this);
                enabled = false;
                return;
            }

            EntityMB entityMB = GetComponentInParent<EntityMB>();

            if (entityMB == null)
            {
                Debug.LogError($"{nameof(EntityMoveController)}: EntityMB was not found.", this);
                enabled = false;
                return;
            }

            if (!entityMB.HasEntity)
            {
                Debug.LogError($"{nameof(EntityMoveController)}: EntityMB is not bound yet.", this);
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

            if (!TryEnsureMoveValueHandles())
            {
                return false;
            }

            return canMoveHandle.CurrentValue;
        }

        protected float GetMoveBaseSpeed(float fallback)
        {
            if (!IsRuntimeReady || !TryEnsureMoveValueHandles())
                return fallback;

            return moveBaseSpeedHandle.CurrentValue;
        }

        protected float GetSprintMultiplier(float fallback)
        {
            if (!IsRuntimeReady || !TryEnsureMoveValueHandles())
                return fallback;

            return sprintMultiplierHandle.CurrentValue;
        }

        protected float GetJumpHeightMultiplier(float fallback)
        {
            if (!IsRuntimeReady || !TryEnsureMoveValueHandles())
                return fallback;

            return jumpHeightMultiplierHandle.CurrentValue;
        }

        private bool TryEnsureMoveValueHandles()
        {
            if (canMoveHandle != null &&
                moveBaseSpeedHandle != null &&
                sprintMultiplierHandle != null &&
                jumpHeightMultiplierHandle != null)
            {
                return true;
            }

            if (SceneKernel == null || SceneKernel.EntityValueStore == null)
            {
                Debug.LogError($"{nameof(EntityMoveController)}: SceneKernel.EntityValueStore is null.", this);
                return false;
            }

            canMoveHandle = SceneKernel.EntityValueStore.GetHandle(Entity, ValueKeys.Move.CanMove);
            moveBaseSpeedHandle = SceneKernel.EntityValueStore.GetHandle(Entity, ValueKeys.Move.BaseSpeed);
            sprintMultiplierHandle = SceneKernel.EntityValueStore.GetHandle(Entity, ValueKeys.Move.SprintMultiplier);
            jumpHeightMultiplierHandle = SceneKernel.EntityValueStore.GetHandle(Entity, ValueKeys.Move.JumpHeightMultiplier);
            return true;
        }
    }
}
