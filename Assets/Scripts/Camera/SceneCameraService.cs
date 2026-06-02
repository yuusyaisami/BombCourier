using System;
using System.Collections.Generic;
using BC.ActionSystem;
using BC.Base;
using Unity.Cinemachine;
using UnityEngine;

namespace BC.Camera
{
    // 会話や演出で「誰に注目するか」を表す軽量コンテキスト。
    // FocusTargetEntity は注視対象、ObserverEntity はその対象を見る側です。
    public readonly struct SceneCameraFocusContext
    {
        public readonly EntityRef FocusTargetEntity;
        public readonly EntityRef ObserverEntity;

        public SceneCameraFocusContext(EntityRef focusTargetEntity, EntityRef observerEntity)
        {
            FocusTargetEntity = focusTargetEntity;
            ObserverEntity = observerEntity;
        }
    }

    // シーン内カメラの最終状態を毎フレーム決定するサービス。
    // Action からの一時上書き、演出用カメラ、パス再生、三人称 rig 補正をここで集約します。
    public sealed class SceneCameraService : ITickable
    {
        // Action からの一時上書きより、明示的な演出カメラを少し強く扱います。
        private const int ActionRequestPriority = 100;
        private const int PresentationRequestPriority = 200;

        private readonly SceneKernel sceneKernel;
        // どのカメラ要求が現在勝っているか、という「選択」の責務を分離した内部状態です。
        private readonly CameraOverrideState overrideState = new();
        // 注視時の入力制御や向き制御、三人称カメラ補正は presentation 専用ロジックに寄せます。
        private readonly PresentationController presentationController;

        private CameraManager cameraManager;
        private int nextCameraManagerLookupFrame;
        // 現在この service が追跡しているプレイヤー。会話や演出の対象未指定時の基準になります。
        private EntityRef trackedPlayerEntity;
        // 現在の注視対象コンテキスト。会話以外の演出でも使えるよう汎用名にしています。
        private SceneCameraFocusContext focusContext;
        private bool focusActive;

        public SceneCameraService(SceneKernel sceneKernel)
        {
            this.sceneKernel = sceneKernel ?? throw new ArgumentNullException(nameof(sceneKernel));
            presentationController = new PresentationController(sceneKernel, ResolveCameraManager);
        }

        public void Tick(float deltaTime)
        {
            CameraManager manager = ResolveCameraManager();
            EntityRef presentationPlayerEntity = ResolvePresentationPlayerEntity();

            // カメラの選択結果に応じて、入力制御、注視向き、三人称 rig を更新する。
            presentationController.UpdateInputModifiers(presentationPlayerEntity, focusActive, overrideState.HasPresentationCamera);
            presentationController.UpdateFocusFacing(focusActive, focusContext, ResolveFocusObserverEntity());
            presentationController.UpdateTalkCamera(deltaTime, manager, focusActive, focusContext, presentationPlayerEntity);

            CinemachineCamera activeCamera = ResolveActiveCamera(manager);
            presentationController.UpdateThirdPersonRig(deltaTime, manager, activeCamera, presentationPlayerEntity);
            ApplyCameraPriorities(manager, activeCamera);
        }

        // プレイヤーの差し替えや再生成に追従する入口。GameLogic 側からここだけ叩けばよいようにする。
        public void SetTrackedPlayer(EntityRef playerEntity)
        {
            if (trackedPlayerEntity.Equals(playerEntity))
                return;

            presentationController.OnTrackedPlayerChanged(playerEntity);
            trackedPlayerEntity = playerEntity;
            RefreshImmediateState();
        }

        // スポーン直後にカメラの初期向きを world 方向から設定する。
        // SetTrackedPlayer の後に呼ぶことで、前回の look state を引きずらないようにする。
        public void InitializeCameraLookDirection(Vector3 worldDirection)
        {
            presentationController.SetInitialLookDirection(trackedPlayerEntity, worldDirection);
        }

        // シーン切り替えやステージ再ロード時に、service が持つ一時状態をまとめて初期化する。
        public void ResetRuntimeState()
        {
            focusActive = false;
            focusContext = default;
            overrideState.Clear();
            presentationController.ResetState();
            RefreshImmediateState();
        }

        public void Dispose()
        {
            ResetRuntimeState();
        }

        // 注視対象を開始する。会話カメラだけでなく、任意の「誰かを見る」演出に使える形にしている。
        public void BeginFocus(SceneCameraFocusContext context)
        {
            focusActive = true;
            focusContext = context;

            if (!trackedPlayerEntity.IsValid && context.ObserverEntity.IsValid)
                SetTrackedPlayer(context.ObserverEntity);
            else
                RefreshImmediateState();
        }

        // 注視演出を終了して、通常の三人称制御へ戻す。
        public void EndFocus()
        {
            if (!focusActive && !focusContext.FocusTargetEntity.IsValid)
                return;

            EntityRef alignTarget = trackedPlayerEntity.IsValid ? trackedPlayerEntity : ResolveFocusObserverEntity();
            presentationController.AlignThirdPersonYawToFocusObserver(alignTarget);
            focusActive = false;
            focusContext = default;
            presentationController.ClearFocusFacing();
            RefreshImmediateState();
        }

        // Action 実行単位で一時カメラを上書きする。
        // string channel ではなく ActionExecutionHandle をキーにすることで typo と後片付け漏れを防ぐ。
        public void SetActionCamera(ActionExecutionHandle executionHandle, CinemachineCamera camera)
        {
            if (!executionHandle.IsValid)
            {
                Debug.LogWarning($"{nameof(SceneCameraService)}: action camera request handle is not valid.");
                return;
            }

            if (camera == null)
            {
                Debug.LogWarning($"{nameof(SceneCameraService)}: action camera request was ignored because camera is null.");
                return;
            }

            overrideState.SetActionCamera(executionHandle, camera, ActionRequestPriority);
            ApplyCameraPriorities();
        }

        // その action 実行が確保していた一時カメラを解放する。
        public void ClearActionCamera(ActionExecutionHandle executionHandle)
        {
            if (!executionHandle.IsValid)
                return;

            if (overrideState.ClearActionCamera(executionHandle))
                ApplyCameraPriorities();
        }

        // ゴールやチュートリアルのような scene-wide 演出で、一時的に優先表示したいカメラを設定する。
        public void ShowPresentationCamera(CinemachineCamera camera, EntityRef playerEntity)
        {
            if (camera == null)
            {
                Debug.LogWarning($"{nameof(SceneCameraService)}: presentation camera is null.");
                return;
            }

            if (playerEntity.IsValid)
                SetTrackedPlayer(playerEntity);

            overrideState.SetPresentationCamera(camera, PresentationRequestPriority);
            ApplyCameraPriorities();
        }

        // 演出用カメラを解除し、通常の解決ルールへ戻す。
        public void ClearPresentationCamera()
        {
            if (overrideState.ClearPresentationCamera())
                RefreshImmediateState();
        }

        // ---- OverlayCamera ---- //
        // InlineAction から動的に登録・切り替えできる追加カメラ。string tag でキーを持ち、
        // 同時に active にできるのは 1 つだけ。path camera より低優先で動作する。

        // camera を tag 名で登録する。activateImmediately が true の場合、その場で active にする。
        // 既存 tag を上書き登録しても問題ない。
        public void RegisterOverlayCamera(string tag, CinemachineCamera camera, bool activateImmediately)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                Debug.LogWarning($"{nameof(SceneCameraService)}: overlay camera tag must not be empty.");
                return;
            }

            if (camera == null)
            {
                Debug.LogWarning($"{nameof(SceneCameraService)}: overlay camera '{tag}' is null and will not be registered.");
                return;
            }

            overrideState.RegisterOverlayCamera(tag, camera, ActionRequestPriority);

            if (activateImmediately)
                ActivateOverlayCamera(tag);
            else
                ApplyCameraPriorities();
        }

        // tag に紐付く overlay camera を active にする（以前 active だった overlay は非 active になる）。
        public void ActivateOverlayCamera(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return;

            overrideState.ActivateOverlayCamera(tag);
            ApplyCameraPriorities();
        }

        // tag に紐付く overlay camera を registry から外し、非アクティブにする。GameObject は破棄しない。
        public void DisableOverlayCamera(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return;

            if (overrideState.DisableOverlayCamera(tag, out CinemachineCamera camera))
            {
                CameraManager manager = ResolveCameraManager();
                SetPriority(camera, manager != null ? manager.InactivePriority : 0);
                ApplyCameraPriorities();
            }
        }

        // パス再生中は path camera を最優先にする。
        public void BeginPathCameraOverride(CameraPathPlayRequest request, int version)
        {
            overrideState.BeginPathCameraOverride(request, version);
            ApplyCameraPriorities();
        }

        // 直近の path playback version と一致したときだけ上書きを解除する。
        public void EndPathCameraOverride(int version)
        {
            if (overrideState.EndPathCameraOverride(version))
                ApplyCameraPriorities();
        }

        // Tick を待たずに即時反映したい場面用の共通再評価処理。
        private void RefreshImmediateState()
        {
            CameraManager manager = ResolveCameraManager();
            EntityRef presentationPlayerEntity = ResolvePresentationPlayerEntity();

            presentationController.UpdateInputModifiers(presentationPlayerEntity, focusActive, overrideState.HasPresentationCamera);
            presentationController.UpdateFocusFacing(focusActive, focusContext, ResolveFocusObserverEntity());
            presentationController.UpdateTalkCamera(0.0f, manager, focusActive, focusContext, presentationPlayerEntity);

            CinemachineCamera activeCamera = ResolveActiveCamera(manager);
            presentationController.UpdateThirdPersonRig(0.0f, manager, activeCamera, presentationPlayerEntity);
            ApplyCameraPriorities(manager, activeCamera);
        }

        // 呼び出し側が manager を気にしなくてよいよう、通常経路の priority 再計算入口を 1 つにまとめる。
        private void ApplyCameraPriorities()
        {
            CameraManager manager = ResolveCameraManager();
            ApplyCameraPriorities(manager, ResolveActiveCamera(manager));
        }

        // 実際に Cinemachine priority を書き換える場所。
        // 「どのカメラを使うか」の判定と、「priority をどう振るか」は分けておく。
        private void ApplyCameraPriorities(CameraManager manager, CinemachineCamera activeCamera)
        {
            if (manager == null)
                return;

            SetPriority(manager.PathCamera, manager.InactivePriority);
            SetPriority(manager.TalkCamera, activeCamera == manager.TalkCamera ? manager.TalkPriority : manager.InactivePriority);
            SetPriority(manager.ThirdPersonCamera, activeCamera == manager.ThirdPersonCamera ? manager.ThirdPersonPriority : manager.InactivePriority);

            overrideState.ApplyInactiveDirectRequestPriorities(activeCamera, manager.InactivePriority);

            if (overrideState.TryGetPathCameraOverride(out CameraPathPlayRequest pathRequest))
            {
                SetPriority(pathRequest.ReturnCamera, pathRequest.InactivePriority);
                SetPriority(pathRequest.PathCamera, pathRequest.PathPriority);
                pathRequest.PathCamera?.Prioritize();
                return;
            }

            if (activeCamera == null)
                return;

            int targetPriority = activeCamera == manager.ThirdPersonCamera
                ? manager.ThirdPersonPriority
                : activeCamera == manager.TalkCamera
                    ? manager.TalkPriority
                    : manager.PresentationPriority;

            SetPriority(activeCamera, targetPriority);
            activeCamera.Prioritize();
        }

        // 最終的に有効にすべきカメラを解決する。
        // 優先順位は path override > direct request > third person の順。
        private CinemachineCamera ResolveActiveCamera(CameraManager manager)
        {
            if (overrideState.TryGetPathCameraOverride(out CameraPathPlayRequest pathRequest))
                return pathRequest.PathCamera;

            if (overrideState.TryGetHighestPriorityDirectRequest(out DirectCameraRequest request))
                return request.Camera;

            if (focusActive && manager != null && manager.TalkCamera != null)
                return manager.TalkCamera;

            return manager != null ? manager.ThirdPersonCamera : null;
        }

        // SceneCameraService は scene 上の CameraManager 実体に依存するので、最初の 1 回だけ解決してキャッシュする。
        private CameraManager ResolveCameraManager()
        {
            if (cameraManager != null)
                return cameraManager;

            cameraManager = CameraManager.Instance;
            if (cameraManager != null)
                return cameraManager;

            if (Time.frameCount < nextCameraManagerLookupFrame)
                return null;

            cameraManager = UnityEngine.Object.FindAnyObjectByType<CameraManager>();
            if (cameraManager == null)
                nextCameraManagerLookupFrame = Time.frameCount + 60;

            return cameraManager;
        }

        // 注視時は observer を優先し、そうでない通常時は追跡中プレイヤーを使う。
        private EntityRef ResolvePresentationPlayerEntity()
        {
            if (trackedPlayerEntity.IsValid)
                return trackedPlayerEntity;

            return ResolveFocusObserverEntity();
        }

        // 現在の注視演出で「見る側」として扱う entity を返す。
        private EntityRef ResolveFocusObserverEntity()
        {
            return focusContext.ObserverEntity.IsValid ? focusContext.ObserverEntity : trackedPlayerEntity;
        }

        // CinemachineCamera の PrioritySettings を統一的に書き換える小さな helper。
        private static void SetPriority(CinemachineCamera camera, int priority)
        {
            if (camera == null)
                return;

            PrioritySettings settings = camera.Priority;
            settings.Enabled = true;
            settings.Value = priority;
            camera.Priority = settings;
        }

        // Override selection is isolated from presentation logic so action/presentation/path requests can evolve independently.
        private sealed class CameraOverrideState
        {
            // Action 単位の camera override。実行終了で自動 cleanup される前提です。
            private readonly Dictionary<ActionExecutionHandle, DirectCameraRequest> actionRequestsByExecution = new();
            // tag → camera の overlay registry。active な overlay は activeOverlayTag で管理する。
            private readonly Dictionary<string, DirectCameraRequest> overlayCamerasByTag = new();
            private string activeOverlayTag;

            private int nextRevision = 1;
            // ゴールや演出で使う 1 本の scene-wide presentation camera。
            private DirectCameraRequest presentationRequest;
            private bool hasPresentationRequest;
            // パス再生中だけ有効な override。version で古い再生終了通知を弾く。
            private int activePathVersion;
            private CameraPathPlayRequest activePathRequest;

            public bool HasPresentationCamera => hasPresentationRequest;

            public void Clear()
            {
                // ステージ再読み込み時は request 状態を丸ごと初期化する。
                actionRequestsByExecution.Clear();
                overlayCamerasByTag.Clear();
                activeOverlayTag = null;
                presentationRequest = default;
                hasPresentationRequest = false;
                activePathVersion = 0;
                activePathRequest = null;
                nextRevision = 1;
            }

            // ---- Overlay camera registry ---- //

            public void RegisterOverlayCamera(string tag, CinemachineCamera camera, int priority)
            {
                overlayCamerasByTag[tag] = new DirectCameraRequest(camera, priority, nextRevision++);
            }

            // 指定 tag を active にする。以前 active だった tag は単に上書きされる（priority で負ける）。
            public void ActivateOverlayCamera(string tag)
            {
                if (!overlayCamerasByTag.ContainsKey(tag))
                    return;

                activeOverlayTag = tag;
            }

            // 指定 tag を registry から除去し、camera を out に返す。active だった場合は active も解除する。
            public bool DisableOverlayCamera(string tag, out CinemachineCamera camera)
            {
                if (!overlayCamerasByTag.TryGetValue(tag, out DirectCameraRequest request))
                {
                    camera = null;
                    return false;
                }

                overlayCamerasByTag.Remove(tag);

                if (string.Equals(activeOverlayTag, tag, StringComparison.Ordinal))
                    activeOverlayTag = null;

                camera = request.Camera;
                return true;
            }

            public bool TryGetActiveOverlayRequest(out DirectCameraRequest request)
            {
                if (activeOverlayTag != null && overlayCamerasByTag.TryGetValue(activeOverlayTag, out request))
                    return request.Camera != null;

                request = default;
                return false;
            }

            public void SetActionCamera(ActionExecutionHandle executionHandle, CinemachineCamera camera, int priority)
            {
                actionRequestsByExecution[executionHandle] = new DirectCameraRequest(camera, priority, nextRevision++);
            }

            public bool ClearActionCamera(ActionExecutionHandle executionHandle)
            {
                return actionRequestsByExecution.Remove(executionHandle);
            }

            public void SetPresentationCamera(CinemachineCamera camera, int priority)
            {
                presentationRequest = new DirectCameraRequest(camera, priority, nextRevision++);
                hasPresentationRequest = true;
            }

            public bool ClearPresentationCamera()
            {
                if (!hasPresentationRequest)
                    return false;

                hasPresentationRequest = false;
                presentationRequest = default;
                return true;
            }

            public void BeginPathCameraOverride(CameraPathPlayRequest request, int version)
            {
                activePathRequest = request;
                activePathVersion = version;
            }

            public bool EndPathCameraOverride(int version)
            {
                if (activePathVersion != version)
                    return false;

                activePathVersion = 0;
                activePathRequest = null;
                return true;
            }

            public bool TryGetPathCameraOverride(out CameraPathPlayRequest request)
            {
                request = activePathRequest;
                return activePathRequest != null && activePathVersion != 0;
            }

            // 勝っていない direct request は全部 inactive priority に落としておく。
            public void ApplyInactiveDirectRequestPriorities(CinemachineCamera activeCamera, int inactivePriority)
            {
                foreach (DirectCameraRequest request in actionRequestsByExecution.Values)
                {
                    if (request.Camera == null || request.Camera == activeCamera)
                        continue;

                    SetPriority(request.Camera, inactivePriority);
                }

                if (hasPresentationRequest && presentationRequest.Camera != null && presentationRequest.Camera != activeCamera)
                    SetPriority(presentationRequest.Camera, inactivePriority);

                // 非 active overlay camera も inactive に落とす。
                foreach (KeyValuePair<string, DirectCameraRequest> kvp in overlayCamerasByTag)
                {
                    if (kvp.Value.Camera == null || kvp.Value.Camera == activeCamera)
                        continue;

                    SetPriority(kvp.Value.Camera, inactivePriority);
                }
            }

            // 直指定カメラ群の中で一番強いものを 1 つ選ぶ。
            // 同 priority なら後から入った request を優先する。
            public bool TryGetHighestPriorityDirectRequest(out DirectCameraRequest request)
            {
                request = default;
                bool found = false;

                if (hasPresentationRequest && presentationRequest.Camera != null)
                {
                    request = presentationRequest;
                    found = true;
                }

                foreach (DirectCameraRequest candidate in actionRequestsByExecution.Values)
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

                // active な overlay camera も候補に含める。
                if (TryGetActiveOverlayRequest(out DirectCameraRequest overlayRequest))
                {
                    if (!found || overlayRequest.Priority > request.Priority ||
                        (overlayRequest.Priority == request.Priority && overlayRequest.Revision > request.Revision))
                    {
                        request = overlayRequest;
                        found = true;
                    }
                }

                return found;
            }
        }

        // Focus-facing, player input gating, and third-person rig shaping are kept together because they all depend on
        // the same tracked player and currently active presentation mode.
        private sealed class PresentationController
        {
            // 注視演出中の入力制御タグ。会話専用名を避けて focus に寄せる。
            private static readonly ValueModifierTagId FocusMoveInputTag = new ValueModifierTagId(11001);
            private static readonly ValueModifierTagId FocusInteractTag = new ValueModifierTagId(11002);
            private static readonly ValueModifierTagId FocusLookInputTag = new ValueModifierTagId(11003);
            // scene-wide presentation camera 中の入力制御タグ。
            private static readonly ValueModifierTagId PresentationMoveInputTag = new ValueModifierTagId(11004);
            private static readonly ValueModifierTagId PresentationInteractTag = new ValueModifierTagId(11005);
            private static readonly ValueModifierTagId PresentationLookInputTag = new ValueModifierTagId(11006);

            private readonly SceneKernel sceneKernel;
            private readonly Func<CameraManager> resolveCameraManager;
            private readonly RaycastHit[] focusOcclusionHits = new RaycastHit[8];

            private EntityRef lastModifierEntity;
            private EntityRef activeFocusFacingEntity;
            private EntityRef throwPoseEntity;
            private ValueWatchHandle<bool> throwPoseHandle;
            private CinemachineThirdPersonFollow thirdPersonFollow;
            private CinemachineRotateWithFollowTarget rotateWithFollowTarget;
            private bool hasRigDefaults;
            private Vector3 defaultShoulderOffset;
            private float defaultCameraDistance;
            private bool defaultRotateWithFollowTargetEnabled;
            private bool hasDefaultRotateWithFollowTargetEnabled;
            private Transform talkCameraTarget;
            private readonly RaycastHit[] talkOcclusionHits = new RaycastHit[8];
            private CinemachineThirdPersonFollow talkThirdPersonFollow;
            private CinemachineRotateWithFollowTarget talkRotateWithFollowTarget;
            private bool hasTalkRigDefaults;
            private Vector3 defaultTalkShoulderOffset;
            private float defaultTalkCameraDistance;
            private bool defaultTalkRotateWithFollowTargetEnabled;
            private bool hasDefaultTalkRotateWithFollowTargetEnabled;
            private bool talkCameraInitialized;
            private float currentTalkCameraDistance;
            private float currentTalkSideSign = 1.0f;

            public PresentationController(SceneKernel sceneKernel, Func<CameraManager> resolveCameraManager)
            {
                this.sceneKernel = sceneKernel ?? throw new ArgumentNullException(nameof(sceneKernel));
                this.resolveCameraManager = resolveCameraManager ?? throw new ArgumentNullException(nameof(resolveCameraManager));
            }

            // プレイヤー差し替え時は、古い player に付いた modifier や focus facing を外しておく。
            public void OnTrackedPlayerChanged(EntityRef newTrackedPlayer)
            {
                if (lastModifierEntity.IsValid && !lastModifierEntity.Equals(newTrackedPlayer))
                    ClearInputModifiers(lastModifierEntity);

                if (activeFocusFacingEntity.IsValid && !activeFocusFacingEntity.Equals(newTrackedPlayer))
                    ClearFocusFacing(activeFocusFacingEntity);

                throwPoseEntity = default;
                throwPoseHandle = null;
            }

            // focus/presentation 関連の補助状態を初期値へ戻す。
            public void ResetState()
            {
                ClearFocusFacing(activeFocusFacingEntity);

                if (lastModifierEntity.IsValid)
                    ClearInputModifiers(lastModifierEntity);

                lastModifierEntity = default;
                throwPoseEntity = default;
                throwPoseHandle = null;
                ResetTalkCameraImmediate(resolveCameraManager());
                ResetThirdPersonRigImmediate();
            }

            // 会話終了時は TPS の yaw を現在の player 向きへ戻し、
            // talk camera 中に accumulated した look state と player facing のズレを解消します。
            public void AlignThirdPersonYawToFocusObserver(EntityRef observerEntity)
            {
                if (!observerEntity.IsValid || !TryResolveThirdPersonController(observerEntity, out ThirdPersonCameraController controller))
                    return;

                if (sceneKernel.EntityComponents != null &&
                    sceneKernel.EntityComponents.TryResolve(observerEntity, out EntityFacingControllerMB facingController) &&
                    facingController != null &&
                    facingController.TryGetWorldFrontDirection(out Vector3 facingDirection))
                {
                    controller.SyncYawToWorldForward(facingDirection);
                    return;
                }

                if (sceneKernel.EntityComponents != null &&
                    sceneKernel.EntityComponents.TryGetTransform(observerEntity, out Transform observerTransform) &&
                    observerTransform != null)
                {
                    controller.SyncYawToWorldForward(observerTransform.forward);
                }
            }

            // スポーン時など、3D world 方向から ThirdPersonCameraController の look state を即時設定する。
            public void SetInitialLookDirection(EntityRef playerEntity, Vector3 worldDirection)
            {
                if (!playerEntity.IsValid || !TryResolveThirdPersonController(playerEntity, out ThirdPersonCameraController controller))
                    return;

                controller.SetLookDirection(worldDirection);
            }

            // 注視演出や presentation camera 中に、プレイヤー入力をどこまで許可するかをここで制御する。
            public void UpdateInputModifiers(EntityRef playerEntity, bool focusActive, bool presentationCameraActive)
            {
                if (sceneKernel.ValueStore == null)
                    return;

                if (lastModifierEntity.IsValid && !lastModifierEntity.Equals(playerEntity))
                    ClearInputModifiers(lastModifierEntity);

                if (!playerEntity.IsValid)
                {
                    lastModifierEntity = default;
                    return;
                }

                SetOrRemoveBoolModifier(playerEntity, ValueKeys.Move.CanMoveByInput, FocusMoveInputTag, focusActive, false);
                SetOrRemoveBoolModifier(playerEntity, ValueKeys.Interaction.CanInteract, FocusInteractTag, focusActive, false);
                SetOrRemoveBoolModifier(playerEntity, ValueKeys.Camera.CanLookByInput, FocusLookInputTag, focusActive, true);

                SetOrRemoveBoolModifier(playerEntity, ValueKeys.Move.CanMoveByInput, PresentationMoveInputTag, presentationCameraActive, false);
                SetOrRemoveBoolModifier(playerEntity, ValueKeys.Interaction.CanInteract, PresentationInteractTag, presentationCameraActive, false);
                SetOrRemoveBoolModifier(playerEntity, ValueKeys.Camera.CanLookByInput, PresentationLookInputTag, presentationCameraActive, false);

                lastModifierEntity = playerEntity;
            }

            // observer を注視対象へ向かせる。会話だけでなく、対話演出やイベント演出でも再利用できる。
            public void UpdateFocusFacing(bool focusActive, SceneCameraFocusContext focusContext, EntityRef observerEntity)
            {
                if (!focusActive || !observerEntity.IsValid || !focusContext.FocusTargetEntity.IsValid)
                {
                    ClearFocusFacing(activeFocusFacingEntity);
                    return;
                }

                if (sceneKernel.EntityComponents == null ||
                    !sceneKernel.EntityComponents.TryGetTransform(focusContext.FocusTargetEntity, out Transform focusTargetTransform) ||
                    focusTargetTransform == null)
                {
                    ClearFocusFacing(activeFocusFacingEntity);
                    return;
                }

                if (activeFocusFacingEntity.IsValid && !activeFocusFacingEntity.Equals(observerEntity))
                    ClearFocusFacing(activeFocusFacingEntity);

                if (!sceneKernel.EntityComponents.TryResolve(observerEntity, out EntityFacingControllerMB facingController))
                    return;

                CameraManager manager = resolveCameraManager();
                float turnSharpness = manager != null ? manager.FocusFacingSharpness : -1.0f;
                facingController.SetFacingTargetTransform(
                    EntityFacingChannels.Talk,
                    focusTargetTransform,
                    EntityFacingPriorities.Talk,
                    turnSharpness);
                activeFocusFacingEntity = observerEntity;
            }

            // 外から「今の focus facing を解除したい」ときの明示入口。
            public void ClearFocusFacing()
            {
                ClearFocusFacing(activeFocusFacingEntity);
            }

            // Talk camera は TPS とは別 rig を使い、player の look state だけを共有します。
            // これにより move/interact を止めたまま、会話専用 camera で orbit できます。
            public void UpdateTalkCamera(
                float deltaTime,
                CameraManager manager,
                bool focusActive,
                SceneCameraFocusContext focusContext,
                EntityRef playerEntity)
            {
                if (!focusActive || manager == null || manager.TalkCamera == null || !focusContext.FocusTargetEntity.IsValid)
                {
                    ResetTalkCameraImmediate(manager);
                    return;
                }

                if (!TryResolveThirdPersonController(playerEntity, out ThirdPersonCameraController controller) ||
                    !TryResolveTalkRig(manager, out CinemachineThirdPersonFollow talkFollow, out CinemachineRotateWithFollowTarget talkRotate))
                {
                    ResetTalkCameraImmediate(manager);
                    return;
                }

                Transform observerTransform = sceneKernel.EntityComponents != null &&
                    sceneKernel.EntityComponents.TryGetTransform(playerEntity, out Transform resolvedObserverTransform)
                    ? resolvedObserverTransform
                    : null;

                Transform focusTargetTransform = sceneKernel.EntityComponents != null &&
                    sceneKernel.EntityComponents.TryGetTransform(focusContext.FocusTargetEntity, out Transform resolvedFocusTransform)
                    ? resolvedFocusTransform
                    : null;

                if (observerTransform == null || focusTargetTransform == null)
                {
                    ResetTalkCameraImmediate(manager);
                    return;
                }

                EnsureTalkCameraTarget();
                CacheTalkRigDefaults(talkFollow, talkRotate);

                Transform playerViewTarget = controller.CameraTarget != null ? controller.CameraTarget : observerTransform;
                Vector3 observerViewPosition = playerViewTarget.position;
                Vector3 focusViewPosition = focusTargetTransform.position;
                Vector3 talkPivotPosition = ResolveTalkPivotPosition(observerViewPosition, focusViewPosition);
                float desiredCameraDistance = ComputeDesiredTalkCameraDistance(manager, observerViewPosition, focusViewPosition);

                if (!talkCameraInitialized)
                {
                    InitializeTalkCameraOrientation(
                        manager,
                        controller,
                        observerTransform,
                        focusTargetTransform,
                        talkPivotPosition,
                        desiredCameraDistance);
                    currentTalkCameraDistance = desiredCameraDistance;
                    talkCameraInitialized = true;
                }

                float distanceBlend = deltaTime <= 0.0f || manager.TalkDistanceBlendSharpness <= 0.0f
                    ? 1.0f
                    : 1.0f - Mathf.Exp(-manager.TalkDistanceBlendSharpness * deltaTime);
                currentTalkCameraDistance = Mathf.Lerp(currentTalkCameraDistance, desiredCameraDistance, distanceBlend);

                Vector3 talkShoulderOffset = defaultTalkShoulderOffset;
                talkShoulderOffset.x = Mathf.Abs(defaultTalkShoulderOffset.x) * currentTalkSideSign;
                talkFollow.ShoulderOffset = talkShoulderOffset;
                talkFollow.CameraDistance = currentTalkCameraDistance;

                if (talkRotate != null && hasDefaultTalkRotateWithFollowTargetEnabled)
                    talkRotate.enabled = defaultTalkRotateWithFollowTargetEnabled;

                manager.TalkCamera.Follow = talkCameraTarget;
                manager.TalkCamera.LookAt = talkCameraTarget;

                talkCameraTarget.SetPositionAndRotation(
                    talkPivotPosition,
                    Quaternion.Euler(controller.GetPitchAngle(), controller.GetYawAngle(), 0.0f));
            }

            // 通常 TPS は throw pose のときだけ補正し、会話 camera は別 rig に分離します。
            public void UpdateThirdPersonRig(
                float deltaTime,
                CameraManager manager,
                CinemachineCamera activeCamera,
                EntityRef playerEntity)
            {
                if (!TryResolveThirdPersonRig(manager, playerEntity, out ThirdPersonCameraController controller, out CinemachineThirdPersonFollow follow, out CinemachineRotateWithFollowTarget rotate))
                    return;

                CacheThirdPersonRigDefaults(follow, rotate);

                bool throwProfileActive = IsThrowPoseActive(playerEntity);

                Vector3 targetShoulderOffset = defaultShoulderOffset;
                float targetCameraDistance = defaultCameraDistance;
                bool targetRotateEnabled = defaultRotateWithFollowTargetEnabled;
                Vector3 profileOffset = controller != null ? controller.ThrowShoulderOffset : new Vector3(0.85f, 0.15f, 0.0f);
                float blendSharpness = controller != null ? controller.ThrowShoulderOffsetBlendSharpness : 12.0f;

                if (throwProfileActive)
                {
                    float targetX = profileOffset.x;
                    targetShoulderOffset = new Vector3(targetX, profileOffset.y, defaultShoulderOffset.z);
                    targetCameraDistance = Mathf.Max(0.0f, defaultCameraDistance - profileOffset.z);
                    targetRotateEnabled = true;
                }

                float blend = deltaTime <= 0.0f || blendSharpness <= 0.0f
                    ? 1.0f
                    : 1.0f - Mathf.Exp(-blendSharpness * deltaTime);

                follow.ShoulderOffset = Vector3.Lerp(follow.ShoulderOffset, targetShoulderOffset, blend);
                follow.CameraDistance = Mathf.Lerp(follow.CameraDistance, targetCameraDistance, blend);

                if (rotate != null && rotate.enabled != targetRotateEnabled)
                    rotate.enabled = targetRotateEnabled;
            }

            // ValueStore への bool modifier 追加・削除を隠蔽する helper。
            private void SetOrRemoveBoolModifier(EntityRef entity, ValueKey<bool> key, ValueModifierTagId tag, bool active, bool value)
            {
                if (!entity.IsValid || sceneKernel.ValueStore == null)
                    return;

                if (active)
                    sceneKernel.ValueStore.SetBoolModifier(entity, key, tag, value);
                else
                    sceneKernel.ValueStore.RemoveBoolModifier(entity, key, tag);
            }

            // 以前の player に付いていた入力制御 modifier をまとめて解除する。
            private void ClearInputModifiers(EntityRef entity)
            {
                if (!entity.IsValid || sceneKernel.ValueStore == null)
                    return;

                sceneKernel.ValueStore.RemoveBoolModifier(entity, ValueKeys.Move.CanMoveByInput, FocusMoveInputTag);
                sceneKernel.ValueStore.RemoveBoolModifier(entity, ValueKeys.Interaction.CanInteract, FocusInteractTag);
                sceneKernel.ValueStore.RemoveBoolModifier(entity, ValueKeys.Camera.CanLookByInput, FocusLookInputTag);
                sceneKernel.ValueStore.RemoveBoolModifier(entity, ValueKeys.Move.CanMoveByInput, PresentationMoveInputTag);
                sceneKernel.ValueStore.RemoveBoolModifier(entity, ValueKeys.Interaction.CanInteract, PresentationInteractTag);
                sceneKernel.ValueStore.RemoveBoolModifier(entity, ValueKeys.Camera.CanLookByInput, PresentationLookInputTag);
            }

            // observer に設定した focus facing を解除する。
            private void ClearFocusFacing(EntityRef entity)
            {
                if (!entity.IsValid || sceneKernel.EntityComponents == null)
                {
                    activeFocusFacingEntity = default;
                    return;
                }

                if (sceneKernel.EntityComponents.TryResolve(entity, out EntityFacingControllerMB facingController))
                    facingController.ClearFacing(EntityFacingChannels.Talk);

                if (activeFocusFacingEntity.Equals(entity))
                    activeFocusFacingEntity = default;
            }

            // third person rig と、その owner に近い controller をまとめて解決する。
            private bool TryResolveThirdPersonRig(
                CameraManager manager,
                EntityRef playerEntity,
                out ThirdPersonCameraController controller,
                out CinemachineThirdPersonFollow follow,
                out CinemachineRotateWithFollowTarget rotate)
            {
                controller = null;
                follow = null;
                rotate = null;

                if (manager == null || manager.ThirdPersonCamera == null || !manager.TryGetThirdPersonRig(out follow, out rotate) || follow == null)
                    return false;

                if (playerEntity.IsValid && sceneKernel.EntityComponents != null)
                    sceneKernel.EntityComponents.TryResolve(playerEntity, out controller);

                thirdPersonFollow = follow;
                rotateWithFollowTarget = rotate;
                return true;
            }

            private bool TryResolveThirdPersonController(EntityRef playerEntity, out ThirdPersonCameraController controller)
            {
                controller = null;

                if (!playerEntity.IsValid || sceneKernel.EntityComponents == null)
                    return false;

                return sceneKernel.EntityComponents.TryResolve(playerEntity, out controller) && controller != null;
            }

            private bool TryResolveTalkRig(
                CameraManager manager,
                out CinemachineThirdPersonFollow follow,
                out CinemachineRotateWithFollowTarget rotate)
            {
                follow = null;
                rotate = null;

                if (manager == null || manager.TalkCamera == null || !manager.TryGetTalkRig(out follow, out rotate) || follow == null)
                    return false;

                talkThirdPersonFollow = follow;
                talkRotateWithFollowTarget = rotate;
                return true;
            }

            // 初回だけ rig のデフォルト値を覚えて、解除時に元へ戻せるようにする。
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

            // focus/throw 補正を外すときは cached default に戻す。
            private void ResetThirdPersonRigImmediate()
            {
                if (!hasRigDefaults || thirdPersonFollow == null)
                    return;

                thirdPersonFollow.ShoulderOffset = defaultShoulderOffset;
                thirdPersonFollow.CameraDistance = defaultCameraDistance;

                if (rotateWithFollowTarget != null && hasDefaultRotateWithFollowTargetEnabled)
                    rotateWithFollowTarget.enabled = defaultRotateWithFollowTargetEnabled;
            }

            private void EnsureTalkCameraTarget()
            {
                if (talkCameraTarget != null)
                    return;

                GameObject targetObject = new GameObject("[TalkCameraTarget]");
                targetObject.hideFlags = HideFlags.HideInHierarchy;
                talkCameraTarget = targetObject.transform;
            }

            private void CacheTalkRigDefaults(CinemachineThirdPersonFollow follow, CinemachineRotateWithFollowTarget rotate)
            {
                if (follow != null && !hasTalkRigDefaults)
                {
                    defaultTalkShoulderOffset = follow.ShoulderOffset;
                    defaultTalkCameraDistance = follow.CameraDistance;
                    currentTalkCameraDistance = defaultTalkCameraDistance;
                    hasTalkRigDefaults = true;
                }

                if (rotate != null && !hasDefaultTalkRotateWithFollowTargetEnabled)
                {
                    defaultTalkRotateWithFollowTargetEnabled = rotate.enabled;
                    hasDefaultTalkRotateWithFollowTargetEnabled = true;
                }
            }

            private void ResetTalkCameraImmediate(CameraManager manager)
            {
                talkCameraInitialized = false;
                currentTalkSideSign = 1.0f;
                currentTalkCameraDistance = defaultTalkCameraDistance;

                if (talkThirdPersonFollow != null && hasTalkRigDefaults)
                {
                    talkThirdPersonFollow.ShoulderOffset = defaultTalkShoulderOffset;
                    talkThirdPersonFollow.CameraDistance = defaultTalkCameraDistance;
                }

                if (talkRotateWithFollowTarget != null && hasDefaultTalkRotateWithFollowTargetEnabled)
                    talkRotateWithFollowTarget.enabled = defaultTalkRotateWithFollowTargetEnabled;

                if (manager != null && manager.TalkCamera != null)
                {
                    manager.TalkCamera.Follow = talkCameraTarget;
                    manager.TalkCamera.LookAt = talkCameraTarget;
                }
            }

            // throw pose は runtime value から読む。毎回 lookup せず handle をキャッシュする。
            private bool IsThrowPoseActive(EntityRef playerEntity)
            {
                if (!playerEntity.IsValid || sceneKernel.EntityValueStore == null)
                    return false;

                if (throwPoseHandle == null || !throwPoseEntity.Equals(playerEntity))
                {
                    throwPoseHandle = sceneKernel.EntityValueStore.GetHandle(playerEntity, ValueKeys.Runtime.IsThrowPoseActive);
                    throwPoseEntity = playerEntity;
                }

                return throwPoseHandle != null && throwPoseHandle.CurrentValue;
            }

            private void InitializeTalkCameraOrientation(
                CameraManager manager,
                ThirdPersonCameraController controller,
                Transform observerTransform,
                Transform focusTargetTransform,
                Vector3 talkPivotPosition,
                float cameraDistance)
            {
                Vector3 observerToTarget = focusTargetTransform.position - observerTransform.position;
                observerToTarget.y = 0.0f;

                if (observerToTarget.sqrMagnitude <= 0.0001f)
                    observerToTarget = controller.GetYawRotation() * Vector3.forward;

                observerToTarget.Normalize();

                float yawBias = Mathf.Abs(manager.TalkInitialYawBias);
                Vector3 rightForward = (Quaternion.AngleAxis(yawBias, Vector3.up) * observerToTarget).normalized;
                Vector3 leftForward = (Quaternion.AngleAxis(-yawBias, Vector3.up) * observerToTarget).normalized;

                bool rightBlocked = IsTalkCameraPathBlocked(talkPivotPosition, rightForward, 1.0f, cameraDistance, observerTransform, focusTargetTransform, manager);
                bool leftBlocked = IsTalkCameraPathBlocked(talkPivotPosition, leftForward, -1.0f, cameraDistance, observerTransform, focusTargetTransform, manager);

                Vector3 chosenForward = rightForward;
                currentTalkSideSign = 1.0f;

                if (rightBlocked && !leftBlocked)
                {
                    chosenForward = leftForward;
                    currentTalkSideSign = -1.0f;
                }

                float desiredYaw = Mathf.Atan2(chosenForward.x, chosenForward.z) * Mathf.Rad2Deg;
                float currentYaw = controller.GetYawAngle();

                // Talk 開始時は TPS の現在向きを基準に、focus 方向へ少しだけ寄せる。
                // これで会話開始直後のヨー回転ジャンプを抑える。
                float blendedYaw = Mathf.LerpAngle(currentYaw, desiredYaw, 0.35f);
                controller.SetLookAngles(blendedYaw, controller.GetPitchAngle());
            }

            private float ComputeDesiredTalkCameraDistance(CameraManager manager, Vector3 observerViewPosition, Vector3 focusViewPosition)
            {
                float separation = Vector3.Distance(observerViewPosition, focusViewPosition);
                float desiredDistance = manager.TalkDistanceOffset + (separation * manager.TalkDistanceScale);
                return Mathf.Clamp(desiredDistance, manager.TalkMinCameraDistance, manager.TalkMaxCameraDistance);
            }

            private static Vector3 ResolveTalkPivotPosition(Vector3 observerViewPosition, Vector3 focusViewPosition)
            {
                return Vector3.Lerp(observerViewPosition, focusViewPosition, 0.5f);
            }

            private bool IsTalkCameraPathBlocked(
                Vector3 pivotPosition,
                Vector3 forward,
                float sideSign,
                float cameraDistance,
                Transform observerTransform,
                Transform focusTargetTransform,
                CameraManager manager)
            {
                Vector3 shoulderOffset = defaultTalkShoulderOffset;
                shoulderOffset.x = Mathf.Abs(defaultTalkShoulderOffset.x) * sideSign;
                Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
                Vector3 desiredCameraPosition = CalculateDesiredCameraPosition(pivotPosition, rotation, shoulderOffset, cameraDistance);
                return IsCameraPathBlocked(
                    pivotPosition,
                    desiredCameraPosition,
                    observerTransform != null ? observerTransform.root : null,
                    focusTargetTransform != null ? focusTargetTransform.root : null,
                    manager.FocusOcclusionProbeRadius,
                    manager.FocusOcclusionMask,
                    talkOcclusionHits);
            }

            // 右肩が遮蔽されるときだけ左肩へ切り替える。
            private bool ShouldUseLeftFocusShoulder(CameraManager manager, Vector3 profileOffset)
            {
                Transform followTarget = manager.CurrentThirdPersonTarget != null
                    ? manager.CurrentThirdPersonTarget
                    : manager.ThirdPersonCamera != null ? manager.ThirdPersonCamera.Follow : null;

                if (followTarget == null)
                    return false;

                float targetDistance = Mathf.Max(0.0f, defaultCameraDistance - profileOffset.z);
                Vector3 rightShoulderOffset = new Vector3(Mathf.Abs(profileOffset.x), profileOffset.y, defaultShoulderOffset.z);
                Vector3 leftShoulderOffset = new Vector3(-Mathf.Abs(profileOffset.x), profileOffset.y, defaultShoulderOffset.z);

                bool rightBlocked = IsCameraPathBlocked(followTarget, rightShoulderOffset, targetDistance, manager.FocusOcclusionProbeRadius, manager.FocusOcclusionMask);

                if (!rightBlocked)
                    return false;

                bool leftBlocked = IsCameraPathBlocked(followTarget, leftShoulderOffset, targetDistance, manager.FocusOcclusionProbeRadius, manager.FocusOcclusionMask);
                return !leftBlocked;
            }

            // follow target から desired camera position へ向かう経路が遮蔽されているかを判定する。
            private bool IsCameraPathBlocked(Transform followTarget, Vector3 shoulderOffset, float cameraDistance, float probeRadius, LayerMask probeMask)
            {
                Vector3 origin = followTarget.position;
                Vector3 desiredCameraPosition = CalculateDesiredCameraPosition(followTarget, shoulderOffset, cameraDistance);
                return IsCameraPathBlocked(origin, desiredCameraPosition, followTarget.root, null, probeRadius, probeMask, focusOcclusionHits);
            }

            private bool IsCameraPathBlocked(
                Vector3 origin,
                Vector3 desiredCameraPosition,
                Transform primaryIgnoreRoot,
                Transform secondaryIgnoreRoot,
                float probeRadius,
                LayerMask probeMask,
                RaycastHit[] hitBuffer)
            {
                Vector3 direction = desiredCameraPosition - origin;
                float distance = direction.magnitude;

                if (distance <= 0.001f)
                    return false;

                direction /= distance;

                int hitCount = probeRadius > 0.001f
                    ? Physics.SphereCastNonAlloc(origin, probeRadius, direction, hitBuffer, distance, probeMask, QueryTriggerInteraction.Ignore)
                    : Physics.RaycastNonAlloc(origin, direction, hitBuffer, distance, probeMask, QueryTriggerInteraction.Ignore);

                for (int i = 0; i < hitCount; i++)
                {
                    Collider hitCollider = hitBuffer[i].collider;

                    if (hitCollider == null)
                        continue;

                    Transform hitTransform = hitCollider.transform;

                    if (hitTransform == primaryIgnoreRoot || hitTransform == secondaryIgnoreRoot)
                        continue;

                    if (primaryIgnoreRoot != null && hitTransform.IsChildOf(primaryIgnoreRoot))
                        continue;

                    if (secondaryIgnoreRoot != null && hitTransform.IsChildOf(secondaryIgnoreRoot))
                        continue;

                    return true;
                }

                return false;
            }

            // shoulder offset と camera distance から、third person camera の理想位置を算出する。
            private static Vector3 CalculateDesiredCameraPosition(Transform followTarget, Vector3 shoulderOffset, float cameraDistance)
            {
                Vector3 shoulderWorldPosition = followTarget.position + followTarget.rotation * shoulderOffset;
                return shoulderWorldPosition - followTarget.forward * cameraDistance;
            }

            private static Vector3 CalculateDesiredCameraPosition(Vector3 followPosition, Quaternion followRotation, Vector3 shoulderOffset, float cameraDistance)
            {
                Vector3 shoulderWorldPosition = followPosition + (followRotation * shoulderOffset);
                return shoulderWorldPosition - ((followRotation * Vector3.forward) * cameraDistance);
            }
        }

        // 直接指定カメラ request の共通表現。priority と revision だけ service 側で見ます。
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
