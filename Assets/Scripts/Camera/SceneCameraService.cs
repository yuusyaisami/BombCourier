using System;
using System.Collections.Generic;
using BC.Base;
using Unity.Cinemachine;
using UnityEngine;

namespace BC.Camera
{
    public readonly struct TalkCameraContext
    {
        public readonly EntityRef SubjectEntity;
        public readonly EntityRef ViewerEntity;

        public TalkCameraContext(EntityRef subjectEntity, EntityRef viewerEntity)
        {
            SubjectEntity = subjectEntity;
            ViewerEntity = viewerEntity;
        }
    }

    public sealed class SceneCameraService : ITickable
    {
        private const string GoalRequestChannel = "__Goal";
        private const int ActionRequestPriority = 100;
        private const int GoalRequestPriority = 200;

        private static readonly ValueModifierTagId TalkMoveInputTag = new ValueModifierTagId(11001);
        private static readonly ValueModifierTagId TalkInteractTag = new ValueModifierTagId(11002);
        private static readonly ValueModifierTagId TalkLookInputTag = new ValueModifierTagId(11003);
        private static readonly ValueModifierTagId GoalMoveInputTag = new ValueModifierTagId(11004);
        private static readonly ValueModifierTagId GoalInteractTag = new ValueModifierTagId(11005);
        private static readonly ValueModifierTagId GoalLookInputTag = new ValueModifierTagId(11006);

        private readonly SceneKernel sceneKernel;
        private readonly Dictionary<string, DirectCameraRequest> requestsByChannel = new(StringComparer.Ordinal);
        private readonly RaycastHit[] talkOcclusionHits = new RaycastHit[8];

        private CameraManager cameraManager;
        private EntityRef trackedPlayerEntity;
        private EntityRef lastModifierEntity;
        private EntityRef activeTalkFacingEntity;
        private EntityRef throwPoseEntity;
        private ValueWatchHandle<bool> throwPoseHandle;

        private TalkCameraContext talkContext;
        private bool talkActive;
        private int nextRevision = 1;

        private int activePathVersion;
        private CameraPathPlayRequest activePathRequest;

        private CinemachineThirdPersonFollow thirdPersonFollow;
        private CinemachineRotateWithFollowTarget rotateWithFollowTarget;
        private bool hasRigDefaults;
        private Vector3 defaultShoulderOffset;
        private float defaultCameraDistance;
        private bool defaultRotateWithFollowTargetEnabled;
        private bool hasDefaultRotateWithFollowTargetEnabled;

        public SceneCameraService(SceneKernel sceneKernel)
        {
            this.sceneKernel = sceneKernel ?? throw new ArgumentNullException(nameof(sceneKernel));
        }

        public void Tick(float deltaTime)
        {
            ApplyPresentationInputModifiers();
            ApplyTalkFacing();
            ApplyThirdPersonRig(deltaTime);
            ApplyCameraPriorities();
        }

        public void SetPlayerEntity(EntityRef playerEntity)
        {
            if (trackedPlayerEntity.Equals(playerEntity))
                return;

            if (lastModifierEntity.IsValid && !lastModifierEntity.Equals(playerEntity))
                ClearPresentationInputModifiers(lastModifierEntity);

            if (activeTalkFacingEntity.IsValid && !activeTalkFacingEntity.Equals(playerEntity))
                ClearTalkFacing(activeTalkFacingEntity);

            trackedPlayerEntity = playerEntity;
            throwPoseEntity = default;
            throwPoseHandle = null;

            RefreshImmediateState();
        }

        public void ResetPresentationState()
        {
            talkActive = false;
            talkContext = default;
            requestsByChannel.Clear();
            activePathVersion = 0;
            activePathRequest = null;
            throwPoseEntity = default;
            throwPoseHandle = null;

            ClearTalkFacing(activeTalkFacingEntity);

            if (lastModifierEntity.IsValid)
                ClearPresentationInputModifiers(lastModifierEntity);

            lastModifierEntity = default;
            ResetThirdPersonRigImmediate();
            ApplyCameraPriorities();
        }

        public void Dispose()
        {
            ResetPresentationState();
        }

        public void BeginTalk(TalkCameraContext context)
        {
            talkActive = true;
            talkContext = context;

            if (context.ViewerEntity.IsValid)
                SetPlayerEntity(context.ViewerEntity);
            else
                RefreshImmediateState();
        }

        public void EndTalk()
        {
            if (!talkActive && !talkContext.SubjectEntity.IsValid)
                return;

            talkActive = false;
            talkContext = default;
            ClearTalkFacing(activeTalkFacingEntity);
            RefreshImmediateState();
        }

        public void SetActionCameraRequest(string channel, CinemachineCamera camera)
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                Debug.LogWarning($"{nameof(SceneCameraService)}: action camera channel is not assigned.");
                return;
            }

            if (camera == null)
            {
                Debug.LogWarning($"{nameof(SceneCameraService)}: action camera request '{channel}' was ignored because camera is null.");
                return;
            }

            SetDirectCameraRequest(channel, camera, ActionRequestPriority);
        }

        public void ClearActionCameraRequest(string channel)
        {
            if (string.IsNullOrWhiteSpace(channel))
                return;

            if (requestsByChannel.Remove(channel))
                ApplyCameraPriorities();
        }

        public void BeginGoalPresentation(CinemachineCamera goalCamera, EntityRef playerEntity)
        {
            if (goalCamera == null)
            {
                Debug.LogWarning($"{nameof(SceneCameraService)}: goal presentation camera is null.");
                return;
            }

            if (playerEntity.IsValid)
                SetPlayerEntity(playerEntity);

            SetDirectCameraRequest(GoalRequestChannel, goalCamera, GoalRequestPriority);
        }

        public void EndGoalPresentation()
        {
            if (requestsByChannel.Remove(GoalRequestChannel))
                RefreshImmediateState();
        }

        public void BeginPathPlayback(CameraPathPlayRequest request, int version)
        {
            activePathRequest = request;
            activePathVersion = version;
            ApplyCameraPriorities();
        }

        public void EndPathPlayback(int version)
        {
            if (activePathVersion != version)
                return;

            activePathVersion = 0;
            activePathRequest = null;
            ApplyCameraPriorities();
        }

        private void RefreshImmediateState()
        {
            ApplyPresentationInputModifiers();
            ApplyTalkFacing();
            ApplyCameraPriorities();
        }

        private void SetDirectCameraRequest(string channel, CinemachineCamera camera, int priority)
        {
            requestsByChannel[channel] = new DirectCameraRequest(camera, priority, nextRevision++);
            ApplyCameraPriorities();
        }

        private void ApplyPresentationInputModifiers()
        {
            if (sceneKernel.ValueStore == null)
                return;

            EntityRef playerEntity = ResolvePresentationPlayerEntity();

            if (lastModifierEntity.IsValid && !lastModifierEntity.Equals(playerEntity))
                ClearPresentationInputModifiers(lastModifierEntity);

            if (!playerEntity.IsValid)
            {
                lastModifierEntity = default;
                return;
            }

            bool goalPresentationActive = requestsByChannel.ContainsKey(GoalRequestChannel);

            SetOrRemoveBoolModifier(playerEntity, ValueKeys.Move.CanMoveByInput, TalkMoveInputTag, talkActive, false);
            SetOrRemoveBoolModifier(playerEntity, ValueKeys.Interaction.CanInteract, TalkInteractTag, talkActive, false);
            SetOrRemoveBoolModifier(playerEntity, ValueKeys.Camera.CanLookByInput, TalkLookInputTag, talkActive, true);

            SetOrRemoveBoolModifier(playerEntity, ValueKeys.Move.CanMoveByInput, GoalMoveInputTag, goalPresentationActive, false);
            SetOrRemoveBoolModifier(playerEntity, ValueKeys.Interaction.CanInteract, GoalInteractTag, goalPresentationActive, false);
            SetOrRemoveBoolModifier(playerEntity, ValueKeys.Camera.CanLookByInput, GoalLookInputTag, goalPresentationActive, false);

            lastModifierEntity = playerEntity;
        }

        private void SetOrRemoveBoolModifier(EntityRef entity, ValueKey<bool> key, ValueModifierTagId tag, bool active, bool value)
        {
            if (!entity.IsValid || sceneKernel.ValueStore == null)
                return;

            if (active)
                sceneKernel.ValueStore.SetBoolModifier(entity, key, tag, value);
            else
                sceneKernel.ValueStore.RemoveBoolModifier(entity, key, tag);
        }

        private void ClearPresentationInputModifiers(EntityRef entity)
        {
            if (!entity.IsValid || sceneKernel.ValueStore == null)
                return;

            sceneKernel.ValueStore.RemoveBoolModifier(entity, ValueKeys.Move.CanMoveByInput, TalkMoveInputTag);
            sceneKernel.ValueStore.RemoveBoolModifier(entity, ValueKeys.Interaction.CanInteract, TalkInteractTag);
            sceneKernel.ValueStore.RemoveBoolModifier(entity, ValueKeys.Camera.CanLookByInput, TalkLookInputTag);
            sceneKernel.ValueStore.RemoveBoolModifier(entity, ValueKeys.Move.CanMoveByInput, GoalMoveInputTag);
            sceneKernel.ValueStore.RemoveBoolModifier(entity, ValueKeys.Interaction.CanInteract, GoalInteractTag);
            sceneKernel.ValueStore.RemoveBoolModifier(entity, ValueKeys.Camera.CanLookByInput, GoalLookInputTag);
        }

        private void ApplyTalkFacing()
        {
            EntityRef viewerEntity = ResolveTalkViewerEntity();

            if (!talkActive || !viewerEntity.IsValid || !talkContext.SubjectEntity.IsValid)
            {
                ClearTalkFacing(activeTalkFacingEntity);
                return;
            }

            if (sceneKernel.EntityComponents == null ||
                !sceneKernel.EntityComponents.TryGetTransform(talkContext.SubjectEntity, out Transform subjectTransform) ||
                subjectTransform == null)
            {
                ClearTalkFacing(activeTalkFacingEntity);
                return;
            }

            if (activeTalkFacingEntity.IsValid && !activeTalkFacingEntity.Equals(viewerEntity))
                ClearTalkFacing(activeTalkFacingEntity);

            if (!sceneKernel.EntityComponents.TryResolve(viewerEntity, out EntityFacingControllerMB facingController))
                return;

            float turnSharpness = ResolveCameraManager() != null ? ResolveCameraManager().TalkFacingSharpness : -1.0f;
            facingController.SetFacingTargetTransform(
                EntityFacingChannels.Talk,
                subjectTransform,
                EntityFacingPriorities.Talk,
                turnSharpness);
            activeTalkFacingEntity = viewerEntity;
        }

        private void ClearTalkFacing(EntityRef entity)
        {
            if (!entity.IsValid || sceneKernel.EntityComponents == null)
            {
                activeTalkFacingEntity = default;
                return;
            }

            if (sceneKernel.EntityComponents.TryResolve(entity, out EntityFacingControllerMB facingController))
                facingController.ClearFacing(EntityFacingChannels.Talk);

            if (activeTalkFacingEntity.Equals(entity))
                activeTalkFacingEntity = default;
        }

        private void ApplyThirdPersonRig(float deltaTime)
        {
            if (!TryResolveThirdPersonRig(out CameraManager manager, out ThirdPersonCameraController controller, out CinemachineThirdPersonFollow follow, out CinemachineRotateWithFollowTarget rotate))
                return;

            CacheThirdPersonRigDefaults(follow, rotate);

            bool talkProfileActive = talkActive && ResolveEffectiveCamera(manager) == manager.ThirdPersonCamera;
            bool throwProfileActive = !talkProfileActive && IsThrowPoseActive();

            Vector3 targetShoulderOffset = defaultShoulderOffset;
            float targetCameraDistance = defaultCameraDistance;
            bool targetRotateEnabled = defaultRotateWithFollowTargetEnabled;
            Vector3 profileOffset = controller != null ? controller.ThrowShoulderOffset : new Vector3(0.85f, 0.15f, 0.0f);
            float blendSharpness = controller != null ? controller.ThrowShoulderOffsetBlendSharpness : 12.0f;

            if (talkProfileActive || throwProfileActive)
            {
                float targetX = throwProfileActive
                    ? profileOffset.x
                    : (ShouldUseLeftTalkShoulder(manager, profileOffset) ? -Mathf.Abs(profileOffset.x) : Mathf.Abs(profileOffset.x));

                targetShoulderOffset = new Vector3(targetX, profileOffset.y, defaultShoulderOffset.z);
                targetCameraDistance = Mathf.Max(0.0f, defaultCameraDistance - profileOffset.z);
                targetRotateEnabled = !talkProfileActive;
            }

            float blend = deltaTime <= 0.0f || blendSharpness <= 0.0f
                ? 1.0f
                : 1.0f - Mathf.Exp(-blendSharpness * deltaTime);

            follow.ShoulderOffset = Vector3.Lerp(follow.ShoulderOffset, targetShoulderOffset, blend);
            follow.CameraDistance = Mathf.Lerp(follow.CameraDistance, targetCameraDistance, blend);

            if (rotate != null && rotate.enabled != targetRotateEnabled)
                rotate.enabled = targetRotateEnabled;
        }

        private bool TryResolveThirdPersonRig(
            out CameraManager manager,
            out ThirdPersonCameraController controller,
            out CinemachineThirdPersonFollow follow,
            out CinemachineRotateWithFollowTarget rotate)
        {
            manager = ResolveCameraManager();
            controller = null;
            follow = null;
            rotate = null;

            if (manager == null || manager.ThirdPersonCamera == null || !manager.TryGetThirdPersonRig(out follow, out rotate) || follow == null)
                return false;

            EntityRef playerEntity = ResolvePresentationPlayerEntity();

            if (playerEntity.IsValid && sceneKernel.EntityComponents != null)
                sceneKernel.EntityComponents.TryResolve(playerEntity, out controller);

            thirdPersonFollow = follow;
            rotateWithFollowTarget = rotate;
            return true;
        }

        private void CacheThirdPersonRigDefaults(CinemachineThirdPersonFollow follow, CinemachineRotateWithFollowTarget rotate)
        {
            if (follow != null && !hasRigDefaults)
            {
                defaultShoulderOffset = follow.ShoulderOffset;
                defaultCameraDistance = follow.CameraDistance;
                hasRigDefaults = true;
            }

            if (rotate != null && !hasDefaultRotateWithFollowTargetEnabled)
            {
                defaultRotateWithFollowTargetEnabled = rotate.enabled;
                hasDefaultRotateWithFollowTargetEnabled = true;
            }
        }

        private void ResetThirdPersonRigImmediate()
        {
            if (!hasRigDefaults || thirdPersonFollow == null)
                return;

            thirdPersonFollow.ShoulderOffset = defaultShoulderOffset;
            thirdPersonFollow.CameraDistance = defaultCameraDistance;

            if (rotateWithFollowTarget != null && hasDefaultRotateWithFollowTargetEnabled)
                rotateWithFollowTarget.enabled = defaultRotateWithFollowTargetEnabled;
        }

        private bool IsThrowPoseActive()
        {
            EntityRef playerEntity = ResolvePresentationPlayerEntity();

            if (!playerEntity.IsValid || sceneKernel.EntityValueStore == null)
                return false;

            if (throwPoseHandle == null || !throwPoseEntity.Equals(playerEntity))
            {
                throwPoseHandle = sceneKernel.EntityValueStore.GetHandle(playerEntity, ValueKeys.Runtime.IsThrowPoseActive);
                throwPoseEntity = playerEntity;
            }

            return throwPoseHandle != null && throwPoseHandle.CurrentValue;
        }

        private bool ShouldUseLeftTalkShoulder(CameraManager manager, Vector3 profileOffset)
        {
            Transform followTarget = manager.CurrentThirdPersonTarget != null
                ? manager.CurrentThirdPersonTarget
                : manager.ThirdPersonCamera != null ? manager.ThirdPersonCamera.Follow : null;

            if (followTarget == null)
                return false;

            float targetDistance = Mathf.Max(0.0f, defaultCameraDistance - profileOffset.z);
            Vector3 rightShoulderOffset = new Vector3(Mathf.Abs(profileOffset.x), profileOffset.y, defaultShoulderOffset.z);
            Vector3 leftShoulderOffset = new Vector3(-Mathf.Abs(profileOffset.x), profileOffset.y, defaultShoulderOffset.z);

            bool rightBlocked = IsCameraPathBlocked(followTarget, rightShoulderOffset, targetDistance, manager.TalkOcclusionProbeRadius, manager.TalkOcclusionMask);

            if (!rightBlocked)
                return false;

            bool leftBlocked = IsCameraPathBlocked(followTarget, leftShoulderOffset, targetDistance, manager.TalkOcclusionProbeRadius, manager.TalkOcclusionMask);
            return !leftBlocked;
        }

        private bool IsCameraPathBlocked(Transform followTarget, Vector3 shoulderOffset, float cameraDistance, float probeRadius, LayerMask probeMask)
        {
            Vector3 origin = followTarget.position;
            Vector3 desiredCameraPosition = CalculateDesiredCameraPosition(followTarget, shoulderOffset, cameraDistance);
            Vector3 direction = desiredCameraPosition - origin;
            float distance = direction.magnitude;

            if (distance <= 0.001f)
                return false;

            direction /= distance;

            int hitCount = probeRadius > 0.001f
                ? Physics.SphereCastNonAlloc(origin, probeRadius, direction, talkOcclusionHits, distance, probeMask, QueryTriggerInteraction.Ignore)
                : Physics.RaycastNonAlloc(origin, direction, talkOcclusionHits, distance, probeMask, QueryTriggerInteraction.Ignore);

            Transform ignoreRoot = followTarget.root;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = talkOcclusionHits[i].collider;

                if (hitCollider == null)
                    continue;

                Transform hitTransform = hitCollider.transform;

                if (hitTransform == followTarget || hitTransform.IsChildOf(ignoreRoot))
                    continue;

                return true;
            }

            return false;
        }

        private static Vector3 CalculateDesiredCameraPosition(Transform followTarget, Vector3 shoulderOffset, float cameraDistance)
        {
            Vector3 shoulderWorldPosition = followTarget.position + followTarget.rotation * shoulderOffset;
            return shoulderWorldPosition - followTarget.forward * cameraDistance;
        }

        private void ApplyCameraPriorities()
        {
            CameraManager manager = ResolveCameraManager();

            if (manager == null)
                return;

            CinemachineCamera effectiveCamera = ResolveEffectiveCamera(manager);

            SetPriority(manager.PathCamera, manager.InactivePriority);
            SetPriority(manager.ThirdPersonCamera, effectiveCamera == manager.ThirdPersonCamera ? manager.ThirdPersonPriority : manager.InactivePriority);

            foreach (DirectCameraRequest request in requestsByChannel.Values)
            {
                if (request.Camera == null || request.Camera == effectiveCamera)
                    continue;

                SetPriority(request.Camera, manager.InactivePriority);
            }

            if (activePathRequest != null && activePathVersion != 0)
            {
                SetPriority(activePathRequest.ReturnCamera, activePathRequest.InactivePriority);
                SetPriority(activePathRequest.PathCamera, activePathRequest.PathPriority);
                activePathRequest.PathCamera?.Prioritize();
                return;
            }

            if (effectiveCamera == null)
                return;

            int targetPriority = effectiveCamera == manager.ThirdPersonCamera
                ? manager.ThirdPersonPriority
                : manager.PresentationPriority;

            SetPriority(effectiveCamera, targetPriority);
            effectiveCamera.Prioritize();
        }

        private CinemachineCamera ResolveEffectiveCamera(CameraManager manager)
        {
            if (activePathRequest != null && activePathVersion != 0)
                return activePathRequest.PathCamera;

            if (TryGetHighestPriorityRequest(out DirectCameraRequest request))
                return request.Camera;

            return manager != null ? manager.ThirdPersonCamera : null;
        }

        private bool TryGetHighestPriorityRequest(out DirectCameraRequest request)
        {
            request = default;
            bool found = false;

            foreach (DirectCameraRequest candidate in requestsByChannel.Values)
            {
                if (candidate.Camera == null)
                    continue;

                if (!found || candidate.Priority > request.Priority ||
                    (candidate.Priority == request.Priority && candidate.Revision > request.Revision))
                {
                    request = candidate;
                    found = true;
                }
            }

            return found;
        }

        private CameraManager ResolveCameraManager()
        {
            if (cameraManager != null)
                return cameraManager;

            cameraManager = CameraManager.Instance != null
                ? CameraManager.Instance
                : UnityEngine.Object.FindAnyObjectByType<CameraManager>();
            return cameraManager;
        }

        private EntityRef ResolvePresentationPlayerEntity()
        {
            EntityRef viewerEntity = ResolveTalkViewerEntity();
            return viewerEntity.IsValid ? viewerEntity : trackedPlayerEntity;
        }

        private EntityRef ResolveTalkViewerEntity()
        {
            return talkContext.ViewerEntity.IsValid ? talkContext.ViewerEntity : trackedPlayerEntity;
        }

        private static void SetPriority(CinemachineCamera camera, int priority)
        {
            if (camera == null)
                return;

            PrioritySettings settings = camera.Priority;
            settings.Enabled = true;
            settings.Value = priority;
            camera.Priority = settings;
        }

        private readonly struct DirectCameraRequest
        {
            public readonly CinemachineCamera Camera;
            public readonly int Priority;
            public readonly int Revision;

            public DirectCameraRequest(CinemachineCamera camera, int priority, int revision)
            {
                Camera = camera;
                Priority = priority;
                Revision = revision;
            }
        }
    }
}