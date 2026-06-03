using System;
using System.Collections.Generic;
using BC.Base;
using BC.Manager;
using BC.Rendering;
using UnityEngine;

namespace BC.Managers
{
    [DisallowMultipleComponent]
    public sealed class EntityMaterialDatasetServiceMB : MonoBehaviour
    {
        public static EntityMaterialDatasetServiceMB Instance { get; private set; }

        [Header("Initial State")]
        [SerializeField] private string initialDatasetKind = string.Empty;

        private readonly HashSet<EntityMaterialControllerMB> registeredControllers = new();
        private GameLogicManagerMB subscribedGameLogicManager;
        private string activeDatasetKind = EntityMaterialSetSO.DefaultDatasetKind;
        private bool runtimeInitialized;

        public string ActiveDatasetKind => activeDatasetKind;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                EnsureRuntimeInitialized();
                EnsureGameLogicSubscription();
                return;
            }

            if (Instance != this)
                Destroy(gameObject);
        }

        private void OnEnable()
        {
            EnsureRuntimeInitialized();
            EnsureGameLogicSubscription();
        }

        private void Start()
        {
            EnsureRuntimeInitialized();
            EnsureGameLogicSubscription();
        }

        private void LateUpdate()
        {
            EnsureRuntimeInitialized();
            EnsureGameLogicSubscription();
        }

        private void OnDisable()
        {
            UnsubscribeFromGameLogicManager();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            UnsubscribeFromGameLogicManager();
            registeredControllers.Clear();
        }

        public void RegisterController(EntityMaterialControllerMB controller)
        {
            EnsureRuntimeInitialized();
            EnsureGameLogicSubscription();

            if (controller == null)
                return;

            registeredControllers.Add(controller);

            if (string.IsNullOrWhiteSpace(activeDatasetKind))
                return;

            if (!ReapplyCurrentTo(controller, out string failureReason))
            {
                if (controller.DefaultMaterialSet != null &&
                    !controller.DefaultMaterialSet.HasDatasetKind(activeDatasetKind))
                {
                    return;
                }

                Debug.LogError(
                    $"{nameof(EntityMaterialDatasetServiceMB)}: failed to apply dataset '{activeDatasetKind}' to controller '{controller.name}'. {failureReason}",
                    controller);
            }
        }

        public void UnregisterController(EntityMaterialControllerMB controller)
        {
            if (controller == null)
                return;

            registeredControllers.Remove(controller);
        }

        public bool TrySetActiveDatasetKind(string datasetKind, out string failureReason)
        {
            EnsureRuntimeInitialized();

            string normalizedKind = EntityMaterialSetSO.NormalizeDatasetKind(datasetKind);
            CleanupDestroyedControllers();

            EntityMaterialApplyRequest request = new EntityMaterialApplyRequest
            {
                datasetKind = normalizedKind,
            };

            foreach (EntityMaterialControllerMB controller in registeredControllers)
            {
                if (controller == null)
                    continue;

                if (!controller.CanApply(request, out failureReason))
                {
                    failureReason = $"Controller '{controller.name}' rejected dataset '{normalizedKind}'. {failureReason}";
                    return false;
                }
            }

            foreach (EntityMaterialControllerMB controller in registeredControllers)
            {
                if (controller == null)
                    continue;

                if (!controller.TryApply(request, out failureReason))
                {
                    failureReason = $"Controller '{controller.name}' failed to apply dataset '{normalizedKind}'. {failureReason}";
                    return false;
                }
            }

            activeDatasetKind = normalizedKind;
            failureReason = null;
            return true;
        }

        public bool ReapplyCurrentTo(EntityMaterialControllerMB controller, out string failureReason)
        {
            EnsureRuntimeInitialized();

            if (controller == null)
            {
                failureReason = "Target controller is null.";
                return false;
            }

            return controller.TryApply(new EntityMaterialApplyRequest
            {
                datasetKind = activeDatasetKind,
            }, out failureReason);
        }

        private void EnsureRuntimeInitialized()
        {
            if (runtimeInitialized)
                return;

            string resolvedDatasetKind = string.Empty;
            GameLogicManagerMB gameLogicManager = GameLogicManagerMB.Instance;
            if (gameLogicManager != null && !string.IsNullOrWhiteSpace(gameLogicManager.CurrentEntityMaterialDatasetKind))
            {
                resolvedDatasetKind = gameLogicManager.CurrentEntityMaterialDatasetKind;
            }
            else if (!string.IsNullOrWhiteSpace(initialDatasetKind) &&
                     !string.Equals(initialDatasetKind.Trim(), EntityMaterialSetSO.DefaultDatasetKind, StringComparison.Ordinal))
            {
                resolvedDatasetKind = initialDatasetKind;
            }

            activeDatasetKind = string.IsNullOrWhiteSpace(resolvedDatasetKind)
                ? string.Empty
                : EntityMaterialSetSO.NormalizeDatasetKind(resolvedDatasetKind);
            runtimeInitialized = true;
        }

        private void EnsureGameLogicSubscription()
        {
            GameLogicManagerMB currentGameLogicManager = GameLogicManagerMB.Instance;
            if (ReferenceEquals(currentGameLogicManager, subscribedGameLogicManager))
                return;

            UnsubscribeFromGameLogicManager();

            if (currentGameLogicManager == null)
                return;

            currentGameLogicManager.OnPlayerSpawned += HandlePlayerSpawned;
            subscribedGameLogicManager = currentGameLogicManager;
        }

        private void UnsubscribeFromGameLogicManager()
        {
            if (subscribedGameLogicManager == null)
                return;

            subscribedGameLogicManager.OnPlayerSpawned -= HandlePlayerSpawned;
            subscribedGameLogicManager = null;
        }

        private void HandlePlayerSpawned(PlayerMB player)
        {
            if (player == null)
                return;

            EntityMaterialControllerMB[] controllers = player.GetComponentsInChildren<EntityMaterialControllerMB>(true);
            for (int i = 0; i < controllers.Length; i++)
            {
                EntityMaterialControllerMB controller = controllers[i];
                if (controller == null)
                    continue;

                if (!ReapplyCurrentTo(controller, out string failureReason))
                {
                    Debug.LogError(
                        $"{nameof(EntityMaterialDatasetServiceMB)}: failed to reapply dataset '{activeDatasetKind}' to spawned player controller '{controller.name}'. {failureReason}",
                        controller);
                }
            }
        }

        private void CleanupDestroyedControllers()
        {
            if (registeredControllers.Count == 0)
                return;

            registeredControllers.RemoveWhere(controller => controller == null);
        }
    }
}
