using BC.Utility;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;

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

        private ValueWatchHandle<bool> canMoveByInputHandle;
        private ValueWatchHandle<bool> canMoveBySystemHandle;
        private ValueWatchHandle<float> moveBaseSpeedHandle;
        private ValueWatchHandle<float> sprintMultiplierHandle;
        private ValueWatchHandle<float> jumpHeightMultiplierHandle;
        private bool hasLoggedMissingKernel;
        private bool hasLoggedMissingEntity;
        private bool hasLoggedUnboundEntity;
        private bool hasLoggedMissingEntityValueStore;

        public EntityMoveState MoveState => StateMachine.CurrentState;
        public bool IsRuntimeReady { get; private set; }

        protected virtual void Start()
        {
            InitializeRuntimeReferences();
        }

        public abstract UniTask<bool> MoveToAsync(Vector3 targetPosition, float arriveDistance = 0.1f, CancellationToken cancellationToken = default);

        protected void InitializeRuntimeReferences()
        {
            SceneKernelMB kernelMB = GetComponentInParent<SceneKernelMB>();

            if (kernelMB == null)
            {
                if (!hasLoggedMissingKernel)
                {
                    Debug.LogWarning($"{nameof(EntityMoveController)}: SceneKernelMB was not found yet.", this);
                    hasLoggedMissingKernel = true;
                }
                return;
            }

            hasLoggedMissingKernel = false;

            EntityMB entityMB = GetComponentInParent<EntityMB>();

            if (entityMB == null)
            {
                if (!hasLoggedMissingEntity)
                {
                    Debug.LogWarning($"{nameof(EntityMoveController)}: EntityMB was not found yet.", this);
                    hasLoggedMissingEntity = true;
                }
                return;
            }

            hasLoggedMissingEntity = false;

            if (!entityMB.HasEntity)
            {
                if (!hasLoggedUnboundEntity)
                {
                    Debug.LogWarning($"{nameof(EntityMoveController)}: EntityMB is not bound yet.", this);
                    hasLoggedUnboundEntity = true;
                }
                return;
            }

            hasLoggedUnboundEntity = false;

            SceneKernel = kernelMB.Kernel;
            Entity = entityMB.Entity;

            StateMachine.ChangeState(EntityMoveState.Idle);
            IsRuntimeReady = true;
        }

        protected bool CanReceiveMoveInput()
        {
            if (!IsRuntimeReady)
                InitializeRuntimeReferences();

            if (!IsRuntimeReady)
                return false;

            if (!TryEnsureMoveValueHandles())
            {
                return false;
            }

            return canMoveByInputHandle.CurrentValue;
        }

        protected bool CanApplySystemMovement()
        {
            if (!IsRuntimeReady)
                InitializeRuntimeReferences();

            if (!IsRuntimeReady)
                return false;

            if (!TryEnsureMoveValueHandles())
            {
                return false;
            }

            return canMoveBySystemHandle.CurrentValue;
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
            if (canMoveByInputHandle != null &&
                canMoveBySystemHandle != null &&
                moveBaseSpeedHandle != null &&
                sprintMultiplierHandle != null &&
                jumpHeightMultiplierHandle != null)
            {
                return true;
            }

            if (SceneKernel == null || SceneKernel.EntityValueStore == null)
            {
                if (!hasLoggedMissingEntityValueStore)
                {
                    Debug.LogWarning($"{nameof(EntityMoveController)}: SceneKernel.EntityValueStore is null.", this);
                    hasLoggedMissingEntityValueStore = true;
                }
                return false;
            }

            hasLoggedMissingEntityValueStore = false;

            canMoveByInputHandle = SceneKernel.EntityValueStore.GetHandle(Entity, ValueKeys.Move.CanMoveByInput);
            canMoveBySystemHandle = SceneKernel.EntityValueStore.GetHandle(Entity, ValueKeys.Move.CanMoveBySystem);
            moveBaseSpeedHandle = SceneKernel.EntityValueStore.GetHandle(Entity, ValueKeys.Move.BaseSpeed);
            sprintMultiplierHandle = SceneKernel.EntityValueStore.GetHandle(Entity, ValueKeys.Move.SprintMultiplier);
            jumpHeightMultiplierHandle = SceneKernel.EntityValueStore.GetHandle(Entity, ValueKeys.Move.JumpHeightMultiplier);
            return true;
        }
    }
}
