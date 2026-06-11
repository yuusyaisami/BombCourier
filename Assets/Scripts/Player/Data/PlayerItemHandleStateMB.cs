using System;
using System.Collections.Generic;
using BC.Audio;
using BC.Base;
using BC.Bomb;
using BC.Item;
using BC.Manager;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BC.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerItemHandleStateMB : MonoBehaviour, IEntityHandleItemAnimationSource, IInteractionSource
    {
        [Header("Detection")]
        [Tooltip("アイテムを拾える最大距離です。")]
        [SerializeField] private float handleItemDistance = 1.5f;
        [Tooltip("アイテムを拾うときに許容する正面角度です。")]
        [SerializeField] private float handleItemAngleThreshold = 45f;
        [Tooltip("拾える対象アイテムのレイヤーマスクです。")]
        [SerializeField] private LayerMask itemLayerMask = ~0;
        [Tooltip("所持アイテムを持つ手元の基準 Transform です。")]
        [SerializeField] private Transform handleItemPoint;
        [Tooltip("プレイヤーモデルの Root です。投擲方向や表示制御に使います。")]
        [SerializeField] private GameObject playerModel;

        [Header("Input")]
        [Tooltip("アイテム取得・投擲に使う入力アクションです。")]
        [SerializeField] private InputActionReference handleItemAction;

        [Header("Interaction Facing")]
        [Tooltip("交互作用開始時に対象へ向きを合わせるかを指定します。")]
        [SerializeField] private bool faceInteractionTargetOnStart = true;
        [Tooltip("交互作用時の向き制御を行う Facing Controller です。")]
        [SerializeField] private EntityFacingControllerMB facingController;

        [Header("Throw")]
        [Tooltip("投擲時の最大速度です。")]
        [SerializeField] private float maxThrowForce = 12f;
        [Tooltip("最低投擲速度です。")]
        [SerializeField] private float minThrowForce = 4f;
        [Tooltip("短押しをドロップ扱いにするための保持猶予です。")]
        [SerializeField, Min(0.01f)] private float throwChargeActivationHoldTime = 0.1f;
        [Tooltip("チャージに応じて投擲速度が伸びる時間です。")]
        [SerializeField] private float throwForceChargeTime = 1.5f;
        [Tooltip("ドロップ時に前方へ出す速度です。")]
        [SerializeField, Min(0f)] private float dropForwardVelocity = 0.75f;
        [Tooltip("ドロップ時にキャリア速度へ掛ける平面成分の割合です。")]
        [SerializeField, Range(0f, 1f)] private float dropCarrierPlanarVelocityFactor = 0.35f;
        [Tooltip("投擲直後に、投げた本人との衝突を無視する時間です。")]
        [SerializeField, Min(0f)] private float throwOwnerCollisionIgnoreDuration = 0.2f;
        [Tooltip("手ぶら状態で投擲予測を出し始めるまでの保持時間です。")]
        [SerializeField, Min(0f)] private float emptyHandThrowPreviewHoldTime = 0.2f;
        [Tooltip("カメラ forward に加える上向き補正角度です。")]
        [SerializeField, Range(0f, 30f)] private float throwUpwardCompensationAngle = 8f;

        [Header("Release Safety")]
        [Tooltip("リリース時にプレイヤーと重ならないよう、前方へ追加する最小オフセットです。")]
        [SerializeField, Min(0.0f)] private float releasePositionSafetyMargin = 0.06f;
        [Tooltip("安全位置探索で前方へ試行する最大距離です。")]
        [SerializeField, Min(0.0f)] private float releasePositionSearchDistance = 0.8f;
        [Tooltip("安全位置探索の分割数です。大きいほど高精度ですが負荷が増えます。")]
        [SerializeField, Min(1)] private int releasePositionSearchSteps = 12;
        [Tooltip("地面めり込み回避のため、リリース位置に加える上方向オフセットです。")]
        [SerializeField] private float releasePositionVerticalBias = 0.02f;
        [Tooltip("リリース位置補正の詳細ログを出力します。")]
        [SerializeField] private bool enableReleaseSafetyDebugLog;

        [Header("Sound")]
        [Tooltip("アイテムを拾ったときに再生するサウンドです。")]
        [SerializeField] private AudioDataSO pickUpSound;
        [Tooltip("投げたときに再生するサウンドです。")]
        [SerializeField] private AudioDataSO throwSound;
        [Tooltip("落としたときに再生するサウンドです。")]
        [SerializeField] private AudioDataSO dropSound;

        [Header("Trajectory")]
        [Tooltip("投擲予測線を描画する LineRenderer です。")]
        [SerializeField] private LineRenderer trajectoryLineRenderer;
        [Tooltip("投擲予測の当たり判定に使うレイヤーマスクです。")]
        [SerializeField] private LayerMask trajectoryCollisionMask = ~0;
        [Tooltip("投擲軌跡のサンプル点数です。")]
        [SerializeField, Min(2)] private int trajectoryPointCount = 32;
        [Tooltip("投擲軌跡のサンプリング時間間隔です。")]
        [SerializeField, Min(0.01f)] private float trajectoryTimeStep = 0.08f;
        [Tooltip("投擲予測の判定半径です。")]
        [SerializeField, Min(0.01f)] private float trajectoryProbeRadius = 0.45f;
        [Tooltip("投擲予測ラインの太さです。")]
        [SerializeField, Min(0.001f)] private float trajectoryLineWidth = 0.04f;
        [Tooltip("投擲予測ラインの色です。")]
        [SerializeField] private Color trajectoryColor = new Color(1.0f, 0.62f, 0.08f, 0.9f);
        [Tooltip("投擲予測ライン用の fallback マテリアルです。Build で Shader が欠ける場合に指定してください。")]
        [SerializeField] private Material trajectoryLineFallbackMaterial;
        [Tooltip("トリガーコライダーを予測判定に含めるかを指定します。")]
        [SerializeField] private bool includeTriggerCollidersInTrajectory = true;
        [Tooltip("着地点マーカーを表示し始める最小距離です。")]
        [SerializeField, Min(0f)] private float trajectoryHitMarkerMinDistance = 1.0f;
        [Tooltip("着地点マーカーの直径です。")]
        [SerializeField, Min(0.01f)] private float trajectoryHitMarkerDiameter = 0.24f;
        [Tooltip("着地点マーカーの色です。")]
        [SerializeField] private Color trajectoryHitMarkerColor = new Color(1.0f, 0.82f, 0.24f, 0.95f);
        [Tooltip("着地点マーカー用の fallback マテリアルです。Build で Shader が欠ける場合に指定してください。")]
        [SerializeField] private Material trajectoryHitMarkerFallbackMaterial;

        [Header("Runtime Debug")]
        [Tooltip("現在インタラクト可能かをデバッグ表示するための runtime 値です。")]
        [SerializeField] private bool currentCanInteract = true;
        [Tooltip("現在アイテム所持中かをデバッグ表示するための runtime 値です。")]
        [SerializeField] private bool isHandlingItem;


        private static readonly ValueModifierTagId CarryItemJumpPenaltyTag = new ValueModifierTagId(10003);
    private static readonly int BaseColorShaderPropertyId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorShaderPropertyId = Shader.PropertyToID("_Color");
        private const int MaxTrajectoryHits = 16;
        // アイテムを拾った後、プレイヤーが入力を離すまで次のアイテムを拾えないようにするためのフラグ。これがないと、例えば爆弾を投げた瞬間にもう一度掴んでしまう。
        private bool waitForPickupInputRelease; // 拾い直後の入力を投擲へ流さないための抑止フラグ。
        private bool isThrowChargePending; // 短押し / 長押しの判定待ち状態。
        private bool isThrowCharging; // 投擲チャージ中かどうか。
        private bool carryJumpPenaltyApplied; // 所持アイテム由来のジャンプ補正を適用済みかどうか。
        private bool activeInteractionFacingOwnsChannel; // interaction facing の制御権をこのコンポーネントが持っているか。

        private readonly RaycastHit[] trajectoryHits = new RaycastHit[MaxTrajectoryHits];
        private readonly Collider[] trajectoryOverlapHits = new Collider[MaxTrajectoryHits];

        private ValueStoreService valueStore; // Runtime の値ストア参照。
        private EntityRef entityRef; // このプレイヤーの EntityRef。
        private ValueWatchHandle<bool> canInteractHandle; // Interaction.CanInteract の監視ハンドル。
        private ValueWatchHandle<bool> fatigueInteractHandle; // 疲労インタラクト状態の監視ハンドル。
        private ICarryableItem currentlyHandledItem; // 現在持っているアイテム。
        private BombMB subscribedHandledBomb; // 現在所持中として Exploded を購読している爆弾。
        private IEntityVelocitySource velocitySource; // 投擲速度の参照元。
        private PlayerInteractionController interactionController; // アイテム操作の入力・候補管理。
        private float emptyHandThrowPreviewTimer; // 手ぶら投擲予測の経過時間。
        private float throwChargePendingTimer; // 投擲 pending の保持時間。
        private float throwForceChargeTimer; // 投擲チャージ時間。
        private Vector3[] trajectoryPoints = new Vector3[32]; // 軌跡描画用の点列。
        private Material ownedTrajectoryMaterial; // 軌跡用に内部生成した材質。
        private MaterialPropertyBlock trajectoryHitMarkerPropertyBlock; // 着地点マーカー色反映用。
        private Transform trajectoryHitMarkerTransform; // 着地点マーカーの Transform。
        private Renderer trajectoryHitMarkerRenderer; // 着地点マーカーの Renderer。
        private bool isEmptyHandThrowPreviewActive; // 手ぶら投擲予測が有効かどうか。
        private int throwSequence; // 投擲回数のシーケンス値。
        private int lastConsumedInputPressSequence; // 最後に投擲判定へ消費した入力シーケンス。

        public ICarryableItem CurrentHandledItem => currentlyHandledItem;
        public bool IsHandlingItem => isHandlingItem;
        public bool CanInteract => currentCanInteract;
        public ICarryableItem CurrentlyHandledItem => currentlyHandledItem;
        public bool IsInputPressed => interactionController != null && interactionController.IsInputPressed;
        public float InputHoldDuration => interactionController != null ? interactionController.InputHoldDuration : 0f;
        public int InputPressSequence => interactionController != null ? interactionController.InputPressSequence : 0;
        public int InputReleaseSequence => interactionController != null ? interactionController.InputReleaseSequence : 0;
        public bool HasCandidate => interactionController != null && interactionController.HasCandidate;
        public IInteractionTarget CurrentBestInteractable => interactionController != null ? interactionController.CurrentBestInteractable : null;
        public IInteractionTarget ActiveInteractable => interactionController != null ? interactionController.ActiveInteractable : null;
        public float ActiveHoldProgress => interactionController != null ? interactionController.ActiveHoldProgress : 0f;
        public Transform HandleItemPoint => handleItemPoint;
        public float HandleItemDistance => handleItemDistance;
        public IReadOnlyList<InteractionCandidate> Candidates =>
            interactionController != null ? interactionController.Candidates : Array.Empty<InteractionCandidate>();

        // throw系の状態
        public bool IsThrowCharging => isThrowCharging || isEmptyHandThrowPreviewActive;
        public float CurrentThrowForce => CalculateThrowForce();
        public float CurrentThrowChargeRatio => Mathf.Clamp01(throwForceChargeTimer / Mathf.Max(0.01f, throwForceChargeTime));
        public Action OnThrowChargeStart { get; set; }
        public Action OnThrowChargeEnd { get; set; }
        public event Action<ICarryableItem> CurrentHandledItemChanged;
        public event Action<InteractionEventData> InteractionEvent
        {
            add
            {
                if (interactionController != null)
                    interactionController.InteractionEvent += value;
            }
            remove
            {
                if (interactionController != null)
                    interactionController.InteractionEvent -= value;
            }
        }

        // 参照の初期解決と入力コントローラ生成を行う。
        private void Awake()
        {
            ResolveFacingController();

            if (handleItemPoint == null)
                Debug.LogError($"{nameof(PlayerItemHandleStateMB)}: handleItemPoint is not assigned.", this);

            if (playerModel == null)
                Debug.LogError($"{nameof(PlayerItemHandleStateMB)}: playerModel is not assigned.", this);

            if (handleItemAction == null || handleItemAction.action == null)
                Debug.LogError($"{nameof(PlayerItemHandleStateMB)}: handleItemAction is not assigned.", this);

            interactionController = new PlayerInteractionController(
                handleItemAction != null ? handleItemAction.action : null,
                handleItemPoint,
                playerModel != null ? playerModel.transform : transform,
                () => entityRef,
                handleItemDistance,
                handleItemAngleThreshold,
                itemLayerMask);
            interactionController.InteractionEvent += HandleInteractionEvent;

            ResolveRuntimeReferences(logMissingEntity: true);
        }

        // 有効化時に入力系を復帰し、操作状態を再接続する。
        private void OnEnable()
        {
            handleItemAction?.action?.Enable();
            interactionController?.Bind();
        }

        // 無効化時は入力と一時状態を必ず落とす。
        private void OnDisable()
        {
            ClearInteractionFacing(force: true);
            interactionController?.ResetRuntimeState();
            interactionController?.Unbind();
            handleItemAction?.action?.Disable();
            HideTrajectory();
            RemoveCarryJumpPenalty();
            ResetEmptyHandThrowPreview();
        }

        private void Reset()
        {
            ResolveFacingController();
        }

        private void OnValidate()
        {
            ResolveFacingController();
        }

        private void OnDestroy()
        {
            UnsubscribeHeldBombExplosion();

            if (interactionController != null)
            {
                interactionController.InteractionEvent -= HandleInteractionEvent;
                interactionController.Dispose();
                interactionController = null;
            }

            if (ownedTrajectoryMaterial != null)
            {
                Destroy(ownedTrajectoryMaterial);
                ownedTrajectoryMaterial = null;
            }
        }

        // 毎フレーム、所持状態・投擲予測・入力ゲートを更新する。
        private void Update()
        {
            if (handleItemAction == null || handleItemAction.action == null)
                return;

            if (handleItemPoint == null || playerModel == null)
                return;

            bool canInteract = RefreshInteractionGateDebugValue();
            interactionController?.Tick(Time.deltaTime, canInteract && !isHandlingItem);

            if (!canInteract)
            {
                CancelInputDrivenItemAction();
                PublishRuntimeValues();
                return;
            }

            if (!isHandlingItem)
            {
                HideTrajectory();

                if (!isHandlingItem)
                    TickEmptyHandThrowPreview(Time.deltaTime);
                else
                    ResetEmptyHandThrowPreview();
            }
            else
            {
                ResetEmptyHandThrowPreview();
                TickThrow();
            }

            PublishRuntimeValues();
        }

        // 手ぶらで押し続けたときだけ投擲予測を出す。
        private void TickEmptyHandThrowPreview(float dt)
        {
            if (GetCurrentBestCarryableItem() != null || IsFatigueInteracting())
            {
                ResetEmptyHandThrowPreview();
                return;
            }

            if (!IsInputPressed)
            {
                ResetEmptyHandThrowPreview();
                return;
            }

            emptyHandThrowPreviewTimer += dt;

            if (emptyHandThrowPreviewTimer >= emptyHandThrowPreviewHoldTime)
                isEmptyHandThrowPreviewActive = true;
        }

        // 所持中のアイテムに対して、ドロップ / チャージ / 投擲を切り替える。
        private void TickThrow()
        {
            if (!IsCarryableItemAlive(currentlyHandledItem))
            {
                ClearHeldState();
                return;
            }

            // 拾った時の入力がまだ押されている間は、投げ処理を一切しない。
            if (waitForPickupInputRelease)
            {
                HideTrajectory();

                if (IsInputPressed)
                {
                    return;
                }

                waitForPickupInputRelease = false;
                return;
            }

            // 2回目の押下直後はまだ「置く」か「投げる」か確定していないので、
            // 短い保持猶予を経てから初めて投げチャージへ遷移する。
            if (isThrowChargePending)
            {
                HideTrajectory();

                if (!IsInputPressed)
                {
                    ReleaseCurrentItem(HeldItemReleaseKind.Drop);
                    return;
                }

                throwChargePendingTimer += Time.deltaTime;
                float activationHoldTime = Mathf.Max(0.01f, throwChargeActivationHoldTime);
                if (throwChargePendingTimer < activationHoldTime)
                    return;

                StartThrowCharge(throwChargePendingTimer - activationHoldTime);
                return;
            }

            // まだ投げチャージに入っていない。
            // 2回目の押下でまず pending に入り、短押しならドロップ、長押しで投げに切り替わる。
            if (!isThrowCharging)
            {
                HideTrajectory();

                if (ConsumeInputPress())
                {
                    BeginThrowChargePending();
                }

                return;
            }

            // チャージ中。
            if (IsInputPressed)
            {
                throwForceChargeTimer += Time.deltaTime;
                UpdateThrowTrajectory();
                return;
            }

            // チャージ後に離したら投げる。
            EndThrowCharge();
            ReleaseCurrentItem(HeldItemReleaseKind.Throw);
        }

        // 2回目の押下直後に、短押し判定待ちへ入る。
        private void BeginThrowChargePending()
        {
            isThrowChargePending = true;
            throwChargePendingTimer = 0f;
            throwForceChargeTimer = 0f;
        }

        // 投擲チャージに入る。短押し分の時間を引き継ぐ。
        private void StartThrowCharge(float initialChargeTime)
        {
            isThrowChargePending = false;
            throwChargePendingTimer = 0f;

            if (isThrowCharging)
                return;

            isThrowCharging = true;
            throwForceChargeTimer = Mathf.Max(0f, initialChargeTime);
            OnThrowChargeStart?.Invoke();
            UpdateThrowTrajectory();
        }

        // 投擲チャージを終了する。
        private void EndThrowCharge()
        {
            if (!isThrowCharging)
                return;

            isThrowCharging = false;
            OnThrowChargeEnd?.Invoke();
        }

        // アイテム取得の確定処理を行う。爆弾の場合は retry checkpoint を先に積む。
        private void HandleItem(ICarryableItem item)
        {
            if (item == null || item.ItemTransform == null)
                return;

            if (item is BombMB bomb && GameLogicManagerMB.Instance != null)
            {
                EntityMB ownerEntity = handleItemPoint != null ? handleItemPoint.GetComponentInParent<EntityMB>() : null;
                LogThrowReleaseDebug(
                    $"BombPickup item={bomb.name} scene={gameObject.scene.name} handlePoint={handleItemPoint.name} handleRoot={(handleItemPoint.root != null ? handleItemPoint.root.name : "(null)")} ownerEntity={(ownerEntity != null ? ownerEntity.name : "(null)")}",
                    this);

                // Reload は「掴む直前」へ戻す。ここで先に checkpoint を積むことで、
                // 起爆開始後に手持ち状態を復元してしまう不整合を防ぐ。
                GameLogicManagerMB.Instance.CaptureRetryCheckpointBeforeBombPickup(bomb);

                // current bomb の追従を更新する。
                GameLogicManagerMB.Instance.SetCurrentBomb(bomb);
            }

            interactionController?.ClearCandidateState();
            ResetEmptyHandThrowPreview();

            SetCurrentHandledItem(item);

            isHandlingItem = true;
            throwForceChargeTimer = 0f;

            // 拾った時の押下シーケンスをここで消費済みにしないと、
            // ボタンを離した直後に「2回目の押下」と誤判定して即投げへ進んでしまう。
            lastConsumedInputPressSequence = InputPressSequence;

            // 拾った時の押下を、投げチャージに流用させない。
            waitForPickupInputRelease = true;
            isThrowChargePending = false;
            isThrowCharging = false;
            throwChargePendingTimer = 0f;

            item.OnHandle(handleItemPoint);

            if (pickUpSound != null && pickUpSound.Clip != null)
            {
                Vector3 pickUpPos = handleItemPoint != null ? handleItemPoint.position : transform.position;
                AudioSource.PlayClipAtPoint(pickUpSound.Clip, pickUpPos, pickUpSound.BaseVolume);
            }

            ApplyCarryJumpPenaltyIfNeeded(item);

            PublishRuntimeValues();
        }

        // 現在の所持アイテムを指定種別で手放す。
        private void ReleaseCurrentItem(HeldItemReleaseKind releaseKind)
        {
            if (currentlyHandledItem == null)
            {
                ClearHeldState();
                return;
            }

            if (currentlyHandledItem is BombMB bomb)
            {
                LogThrowReleaseDebug(
                    $"BombRelease item={bomb.name} scene={gameObject.scene.name} kind={releaseKind} handlePoint={(handleItemPoint != null ? handleItemPoint.name : "(null)")} handleRoot={(handleItemPoint != null && handleItemPoint.root != null ? handleItemPoint.root.name : "(null)")}",
                    this);
            }

            Vector3 releaseVelocity = releaseKind == HeldItemReleaseKind.Throw
                ? BuildThrowVelocity()
                : BuildDropVelocity();

            ICarryableItem releasingItem = currentlyHandledItem;

            bool hasSafePose = TryResolveSafeReleasePose(
                releasingItem,
                releaseKind,
                releaseVelocity,
                out Vector3 resolvedReleasePosition,
                out int resolvedStep,
                out int overlapCount,
                out bool fallbackUsed);

            if (hasSafePose || fallbackUsed)
                ApplyReleasePose(releasingItem, resolvedReleasePosition);

            LogReleaseSafetyDebug(
                $"Release kind={releaseKind} hasSafePose={hasSafePose} fallbackUsed={fallbackUsed} step={resolvedStep} overlapCount={overlapCount} resolvedPosition={resolvedReleasePosition} releaseVelocity={releaseVelocity}",
                this);

            releasingItem.OnRelease(releaseVelocity);

            {
                AudioDataSO releaseSound = releaseKind == HeldItemReleaseKind.Throw ? throwSound : dropSound;
                if (releaseSound != null && releaseSound.Clip != null)
                {
                    Vector3 releasePos = handleItemPoint != null ? handleItemPoint.position : transform.position;
                    AudioSource.PlayClipAtPoint(releaseSound.Clip, releasePos, releaseSound.BaseVolume);
                }
            }

            ApplyThrowOwnerCollisionIgnore(releasingItem, releaseKind, releaseVelocity);

            if (releaseKind == HeldItemReleaseKind.Throw)
                throwSequence++;

            ClearHeldState();
        }

        // 外部要因で現在の所持アイテムを強制的に放す。
        public bool ForceReleaseCurrentItem(Vector3 releaseVelocity)
        {
            if (currentlyHandledItem == null)
                return false;

            if (isThrowCharging)
            {
                isThrowCharging = false;
                OnThrowChargeEnd?.Invoke();
            }

            // 着地ショックでの手放しは「投げた」扱いにしない。
            // 投擲トリガーを増やすと着地アニメーションと競合するため、throwSequence は進めない。
            ICarryableItem releasingItem = currentlyHandledItem;

            bool hasSafePose = TryResolveSafeReleasePose(
                releasingItem,
                HeldItemReleaseKind.Throw,
                releaseVelocity,
                out Vector3 resolvedReleasePosition,
                out int resolvedStep,
                out int overlapCount,
                out bool fallbackUsed);

            if (hasSafePose || fallbackUsed)
                ApplyReleasePose(releasingItem, resolvedReleasePosition);

            LogReleaseSafetyDebug(
                $"ForceRelease hasSafePose={hasSafePose} fallbackUsed={fallbackUsed} step={resolvedStep} overlapCount={overlapCount} resolvedPosition={resolvedReleasePosition} releaseVelocity={releaseVelocity}",
                this);

            releasingItem.OnRelease(releaseVelocity);
            ApplyThrowOwnerCollisionIgnore(releasingItem, HeldItemReleaseKind.Throw, releaseVelocity);
            ClearHeldState();
            return true;
        }

        private bool TryResolveSafeReleasePose(
            ICarryableItem releasingItem,
            HeldItemReleaseKind releaseKind,
            Vector3 releaseVelocity,
            out Vector3 resolvedPosition,
            out int resolvedStep,
            out int overlapCount,
            out bool fallbackUsed)
        {
            resolvedPosition = handleItemPoint != null
                ? handleItemPoint.position
                : transform.position;
            resolvedStep = 0;
            overlapCount = 0;
            fallbackUsed = false;

            if (releasingItem == null || releasingItem.ItemTransform == null)
                return false;

            Transform ownerRoot = ResolveOwnerCollisionRoot();
            Transform itemRoot = releasingItem.ItemTransform;

            if (ownerRoot == null || itemRoot == null)
                return false;

            Collider[] itemColliders = itemRoot.GetComponentsInChildren<Collider>(true);
            Collider[] ownerColliders = ownerRoot.GetComponentsInChildren<Collider>(true);

            if (itemColliders == null || itemColliders.Length == 0 || ownerColliders == null || ownerColliders.Length == 0)
                return false;

            Vector3 searchDirection = ResolveReleaseSearchDirection(releaseKind, releaseVelocity);
            float startOffset = Mathf.Max(0.0f, releasePositionSafetyMargin);
            float maxDistance = Mathf.Max(startOffset, releasePositionSearchDistance);
            int steps = Mathf.Max(1, releasePositionSearchSteps);

            Vector3 startPosition = (handleItemPoint != null ? handleItemPoint.position : itemRoot.position) +
                                    (Vector3.up * releasePositionVerticalBias);

            int bestOverlapCount = int.MaxValue;
            Vector3 bestPosition = startPosition;
            int bestStep = 0;

            for (int i = 0; i <= steps; i++)
            {
                float t = steps > 0 ? (i / (float)steps) : 1.0f;
                float distance = Mathf.Lerp(startOffset, maxDistance, t);
                Vector3 candidate = startPosition + (searchDirection * distance);

                int candidateOverlapCount = CountOwnerOverlapsAtPose(itemRoot, itemColliders, ownerColliders, candidate, itemRoot.rotation);

                if (candidateOverlapCount < bestOverlapCount)
                {
                    bestOverlapCount = candidateOverlapCount;
                    bestPosition = candidate;
                    bestStep = i;
                }

                if (candidateOverlapCount == 0)
                {
                    resolvedPosition = candidate;
                    resolvedStep = i;
                    overlapCount = 0;
                    return true;
                }
            }

            resolvedPosition = bestPosition;
            resolvedStep = bestStep;
            overlapCount = bestOverlapCount;
            fallbackUsed = bestOverlapCount < int.MaxValue;
            return false;
        }

        private static void ApplyReleasePose(ICarryableItem releasingItem, Vector3 resolvedPosition)
        {
            if (releasingItem?.ItemTransform == null)
                return;

            releasingItem.ItemTransform.position = resolvedPosition;
        }

        private static int CountOwnerOverlapsAtPose(
            Transform itemRoot,
            Collider[] itemColliders,
            Collider[] ownerColliders,
            Vector3 rootPosition,
            Quaternion rootRotation)
        {
            int overlapCount = 0;

            for (int i = 0; i < itemColliders.Length; i++)
            {
                Collider itemCollider = itemColliders[i];

                if (itemCollider == null || !itemCollider.enabled || !itemCollider.gameObject.activeInHierarchy)
                    continue;

                Vector3 itemLocalPosition = itemRoot.InverseTransformPoint(itemCollider.transform.position);
                Quaternion itemLocalRotation = Quaternion.Inverse(itemRoot.rotation) * itemCollider.transform.rotation;

                Vector3 itemWorldPosition = rootPosition + (rootRotation * itemLocalPosition);
                Quaternion itemWorldRotation = rootRotation * itemLocalRotation;

                for (int j = 0; j < ownerColliders.Length; j++)
                {
                    Collider ownerCollider = ownerColliders[j];

                    if (ownerCollider == null ||
                        !ownerCollider.enabled ||
                        !ownerCollider.gameObject.activeInHierarchy ||
                        ownerCollider.transform.IsChildOf(itemRoot))
                    {
                        continue;
                    }

                    if (Physics.ComputePenetration(
                            itemCollider,
                            itemWorldPosition,
                            itemWorldRotation,
                            ownerCollider,
                            ownerCollider.transform.position,
                            ownerCollider.transform.rotation,
                            out _,
                            out _))
                    {
                        overlapCount++;
                    }
                }
            }

            return overlapCount;
        }

        private Vector3 ResolveReleaseSearchDirection(HeldItemReleaseKind releaseKind, Vector3 releaseVelocity)
        {
            Vector3 direction;

            if (releaseKind == HeldItemReleaseKind.Throw)
            {
                direction = BuildThrowDirection();
            }
            else
            {
                direction = BuildDropDirection();
            }

            if (direction.sqrMagnitude <= 0.0001f)
                direction = new Vector3(releaseVelocity.x, 0.0f, releaseVelocity.z);

            direction.y = 0.0f;

            if (direction.sqrMagnitude <= 0.0001f)
                direction = transform.forward;
            direction.y = 0.0f;

            // Player から外側（手の方向）へ逃がす水平成分を加味する。下を向いて投げたときでも探索方向が
            // Player 内へ向かわないようにし、アイテムが Player と重なったままリリースされて押し合うのを防ぐ。
            Transform ownerRoot = ResolveOwnerCollisionRoot();
            Vector3 handPosition = handleItemPoint != null ? handleItemPoint.position : transform.position;
            if (ownerRoot != null)
            {
                Vector3 outward = handPosition - ownerRoot.position;
                outward.y = 0.0f;
                if (outward.sqrMagnitude > 0.0001f)
                {
                    Vector3 horizontalThrow = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
                    direction = outward.normalized + horizontalThrow;
                    direction.y = 0.0f;
                }
            }

            return direction.sqrMagnitude <= 0.0001f ? Vector3.forward : direction.normalized;
        }

        private void ApplyThrowOwnerCollisionIgnore(ICarryableItem releasedItem, HeldItemReleaseKind releaseKind, Vector3 releaseVelocity)
        {
            if (releasedItem is not ICarryReleaseOwnerCollisionGuard collisionGuard)
            {
                LogThrowReleaseDebug($"OwnerCollisionIgnore skipped item={releasedItem?.GetType().Name ?? "(null)"} reason=no-guard kind={releaseKind} releaseVelocity={releaseVelocity}", this);
                return;
            }

            if (throwOwnerCollisionIgnoreDuration <= 0f)
            {
                LogThrowReleaseDebug($"OwnerCollisionIgnore skipped item={releasedItem.GetType().Name} reason=duration<=0 kind={releaseKind} releaseVelocity={releaseVelocity}", this);
                return;
            }

            Transform ownerRoot = ResolveOwnerCollisionRoot();

            if (ownerRoot == null)
            {
                LogThrowReleaseDebug($"OwnerCollisionIgnore skipped item={releasedItem.GetType().Name} reason=owner-root-null kind={releaseKind} releaseVelocity={releaseVelocity}", this);
                return;
            }

            collisionGuard.IgnoreOwnerCollisionAfterRelease(ownerRoot, throwOwnerCollisionIgnoreDuration);
            LogThrowReleaseDebug($"OwnerCollisionIgnore applied item={releasedItem.GetType().Name} owner={ownerRoot.name} duration={throwOwnerCollisionIgnoreDuration:F3} kind={releaseKind} releaseVelocity={releaseVelocity}", this);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private static void LogThrowReleaseDebug(string message, UnityEngine.Object context)
        {
            UnityEngine.Debug.Log($"[PlayerItemHandle] {message}", context);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogReleaseSafetyDebug(string message, UnityEngine.Object context)
        {
            if (!enableReleaseSafetyDebugLog)
                return;

            UnityEngine.Debug.Log($"[PlayerItemReleaseSafety] {message}", context);
        }

        // 現在持っているアイテムの tag を取得する。
        public bool TryGetHeldItemTag(out EntityTagId heldItemTag)
        {
            heldItemTag = default;

            if (currentlyHandledItem == null || currentlyHandledItem.ItemTransform == null)
                return false;

            EntityMB heldItemEntity = currentlyHandledItem.ItemTransform.GetComponentInParent<EntityMB>();

            if (heldItemEntity == null || !heldItemEntity.Tag.IsValid)
                return false;

            heldItemTag = heldItemEntity.Tag;
            return true;
        }

        // 指定 tag のアイテムを持っているか判定する。
        public bool IsHoldingItemWithTag(EntityTagId tagId)
        {
            return tagId.IsValid &&
                   TryGetHeldItemTag(out EntityTagId heldItemTag) &&
                   heldItemTag.Equals(tagId);
        }

        // 所持・チャージ・予測線など、アイテム操作の runtime 状態をまとめて初期化する。
        private void ClearHeldState()
        {
            SetCurrentHandledItem(null);

            isHandlingItem = false;
            isThrowChargePending = false;
            throwForceChargeTimer = 0f;
            throwChargePendingTimer = 0f;
            waitForPickupInputRelease = false;
            isThrowCharging = false;

            ResetEmptyHandThrowPreview();
            HideTrajectory();
            RemoveCarryJumpPenalty();
            PublishRuntimeValues();
        }

        // Retry 復帰時は所持状態だけを安全に初期化する。
        public void RestoreRetryCheckpointState()
        {
            // Retry 復帰では item 側の transform / rigidbody を checkpoint restore に任せる。
            // Player 側は所持状態と throw 入力状態だけを安全に初期化する。
            ClearHeldState();
        }

        // 現在所持アイテムを更新し、変更通知を出す。
        private void SetCurrentHandledItem(ICarryableItem item)
        {
            if (EqualityComparer<ICarryableItem>.Default.Equals(currentlyHandledItem, item))
                return;

            UnsubscribeHeldBombExplosion();
            currentlyHandledItem = item;
            SubscribeHeldBombExplosion(item);
            CurrentHandledItemChanged?.Invoke(currentlyHandledItem);
        }

        private void SubscribeHeldBombExplosion(ICarryableItem item)
        {
            if (item is not BombMB bomb)
                return;

            subscribedHandledBomb = bomb;
            subscribedHandledBomb.Exploded += HandleHeldBombExploded;
        }

        private void UnsubscribeHeldBombExplosion()
        {
            if (ReferenceEquals(subscribedHandledBomb, null))
                return;

            subscribedHandledBomb.Exploded -= HandleHeldBombExploded;
            subscribedHandledBomb = null;
        }

        private void HandleHeldBombExploded(BombMB bomb)
        {
            if (!ReferenceEquals(subscribedHandledBomb, bomb))
                return;

            ClearHeldState();
        }

        private static bool IsCarryableItemAlive(ICarryableItem item)
        {
            if (item == null)
                return false;

            if (item is UnityEngine.Object unityObject && unityObject == null)
                return false;

            Transform itemTransform;
            try
            {
                itemTransform = item.ItemTransform;
            }
            catch (MissingReferenceException)
            {
                return false;
            }

            return itemTransform != null && itemTransform.gameObject.activeInHierarchy;
        }

        // 現在のチャージ量から投擲速度を計算する。
        private float CalculateThrowForce()
        {
            float chargeRatio = Mathf.Clamp01(throwForceChargeTimer / Mathf.Max(0.01f, throwForceChargeTime));
            return Mathf.Lerp(minThrowForce, maxThrowForce, chargeRatio);
        }

        // 投擲用の最終速度を構築する。
        private Vector3 BuildThrowVelocity()
        {
            return BuildThrowDirection() * CalculateThrowForce() + GetCarrierVelocity();
        }

        // 落とすときの最終速度を構築する。
        private Vector3 BuildDropVelocity()
        {
            Vector3 carrierVelocity = GetCarrierVelocity();
            carrierVelocity.y = 0f;
            carrierVelocity *= Mathf.Clamp01(dropCarrierPlanarVelocityFactor);

            return BuildDropDirection() * Mathf.Max(0f, dropForwardVelocity) + carrierVelocity;
        }

        // 所持中のキャリア速度を取得する。
        private Vector3 GetCarrierVelocity()
        {
            if (velocitySource == null)
                velocitySource = ResolveVelocitySource();

            return velocitySource != null ? velocitySource.CurrentVelocity : Vector3.zero;
        }

        // 速度の参照元となるコンポーネントを親階層から探す。
        private IEntityVelocitySource ResolveVelocitySource()
        {
            MonoBehaviour[] behaviours = GetComponentsInParent<MonoBehaviour>();

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IEntityVelocitySource source)
                    return source;
            }

            return null;
        }

        // カメラ向き基準の投擲方向を作る。
        private Vector3 BuildThrowDirection()
        {
            UnityEngine.Camera mainCamera = UnityEngine.Camera.main;

            if (mainCamera != null && mainCamera.transform.forward.sqrMagnitude > 0.0001f)
            {
                // カメラ forward をそのまま使うと低めに落ちやすいので、少しだけ上方向へ補正する。
                return ApplyThrowUpwardCompensation(
                    mainCamera.transform.forward,
                    mainCamera.transform.right,
                    throwUpwardCompensationAngle);
            }

            Vector3 fallbackDirection = playerModel != null
                ? playerModel.transform.forward
                : transform.forward;

            fallbackDirection.y = 0.0f;

            if (fallbackDirection.sqrMagnitude <= 0.0001f)
                fallbackDirection = transform.forward;

            return ApplyThrowUpwardCompensation(
                fallbackDirection,
                transform.right,
                throwUpwardCompensationAngle);
        }

        // ドロップ時の前方方向を作る。
        private Vector3 BuildDropDirection()
        {
            Vector3 forward = playerModel != null
                ? playerModel.transform.forward
                : transform.forward;

            forward.y = 0f;

            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = transform.forward;
                forward.y = 0f;
            }

            if (forward.sqrMagnitude <= 0.0001f)
                return Vector3.forward;

            return forward.normalized;
        }

        private static Vector3 ApplyThrowUpwardCompensation(Vector3 forward, Vector3 rightAxis, float upwardCompensationAngle)
        {
            Vector3 normalizedForward = forward.normalized;
            if (normalizedForward.sqrMagnitude <= 0.0001f)
                return Vector3.forward;

            Vector3 normalizedRight = rightAxis.normalized;
            if (normalizedRight.sqrMagnitude <= 0.0001f)
                normalizedRight = Vector3.right;

            // Unityの右手軸回転では負方向がカメラforwardを上へ起こす向きになる。
            Quaternion compensationRotation = Quaternion.AngleAxis(-upwardCompensationAngle, normalizedRight);
            return (compensationRotation * normalizedForward).normalized;
        }

        // 一部アイテム所持時のジャンプ補正を適用する。
        private void ApplyCarryJumpPenaltyIfNeeded(ICarryableItem item)
        {
            if (item is not ICarryMoveModifier moveModifier)
                return;

            if (!moveModifier.TryGetJumpHeightMultiplier(out float jumpHeightMultiplier))
                return;

            if (Mathf.Approximately(jumpHeightMultiplier, 1.0f))
                return;

            ResolveRuntimeReferences(logMissingEntity: false);

            if (valueStore == null || !entityRef.IsValid)
                return;

            // 持ち物ごとの移動補正は、拾う側ではなくアイテム側の任意インターフェースから受け取る。
            valueStore.SetMul(
                entityRef,
                ValueKeys.Move.JumpHeightMultiplier,
                CarryItemJumpPenaltyTag,
                Mathf.Max(0.0f, jumpHeightMultiplier));

            carryJumpPenaltyApplied = true;
        }

        // 所持補正を解除する。
        private void RemoveCarryJumpPenalty()
        {
            if (!carryJumpPenaltyApplied)
                return;

            ResolveRuntimeReferences(logMissingEntity: false);

            if (valueStore != null && entityRef.IsValid)
            {
                valueStore.RemoveMul(
                    entityRef,
                    ValueKeys.Move.JumpHeightMultiplier,
                    CarryItemJumpPenaltyTag);
            }

            carryJumpPenaltyApplied = false;
        }

        // 投擲予測線を更新する。
        private void UpdateThrowTrajectory()
        {
            if (currentlyHandledItem == null || currentlyHandledItem.ItemTransform == null)
            {
                HideTrajectory();
                return;
            }

            EnsureTrajectoryRenderer();

            if (trajectoryLineRenderer == null)
                return;

            int pointCount = Mathf.Clamp(trajectoryPointCount, 2, 64);

            if (trajectoryPoints == null || trajectoryPoints.Length < pointCount)
                trajectoryPoints = new Vector3[pointCount];

            Vector3 origin = handleItemPoint != null
                ? handleItemPoint.position
                : currentlyHandledItem.ItemTransform.position;
            Vector3 velocity = BuildThrowVelocity();
            Vector3 previous = origin;
            bool hasTrajectoryHit = false;
            Vector3 trajectoryHitPoint = Vector3.zero;

            // 物理位置を点列に落とし込み、途中でColliderに当たったらそこでLineを止める。
            trajectoryPoints[0] = origin;
            int usedPointCount = 1;

            for (int i = 1; i < pointCount; i++)
            {
                float time = trajectoryTimeStep * i;
                Vector3 next = origin + velocity * time + 0.5f * Physics.gravity * time * time;

                if (TryFindTrajectoryHit(previous, next, out Vector3 hitPoint))
                {
                    hasTrajectoryHit = true;
                    trajectoryHitPoint = hitPoint;
                    trajectoryPoints[usedPointCount++] = hitPoint;
                    break;
                }

                trajectoryPoints[usedPointCount++] = next;
                previous = next;
            }

            trajectoryLineRenderer.positionCount = usedPointCount;

            for (int i = 0; i < usedPointCount; i++)
            {
                trajectoryLineRenderer.SetPosition(i, trajectoryPoints[i]);
            }

            trajectoryLineRenderer.enabled = true;

            if (hasTrajectoryHit && Vector3.Distance(origin, trajectoryHitPoint) >= trajectoryHitMarkerMinDistance)
            {
                ShowTrajectoryHitMarker(trajectoryHitPoint);
            }
            else
            {
                HideTrajectoryHitMarker();
            }
        }

        // 投擲予測の区間内ヒットを検出する。
        private bool TryFindTrajectoryHit(Vector3 from, Vector3 to, out Vector3 hitPoint)
        {
            hitPoint = to;

            Vector3 segment = to - from;
            float distance = segment.magnitude;

            if (distance <= 0.0001f)
                return false;

            Vector3 direction = segment / distance;
            float radius = Mathf.Max(0.01f, trajectoryProbeRadius);
            QueryTriggerInteraction triggerInteraction = includeTriggerCollidersInTrajectory
                ? QueryTriggerInteraction.Collide
                : QueryTriggerInteraction.Ignore;

            // 始点がすでに collider に重なっている場合、SphereCast だけでは取りこぼすことがある。
            int overlapCount = Physics.OverlapSphereNonAlloc(
                from,
                radius,
                trajectoryOverlapHits,
                trajectoryCollisionMask,
                triggerInteraction);

            for (int i = 0; i < overlapCount; i++)
            {
                Collider overlapCollider = trajectoryOverlapHits[i];

                if (overlapCollider == null || IsIgnoredTrajectoryCollider(overlapCollider))
                    continue;

                // MarkerはLineの中心ではなく、Collider表面に近い位置へ置く。
                hitPoint = overlapCollider.ClosestPoint(from);
                return true;
            }

            int hitCount = Physics.SphereCastNonAlloc(
                from,
                radius,
                direction,
                trajectoryHits,
                distance,
                trajectoryCollisionMask,
                triggerInteraction);

            bool found = false;
            float bestDistance = float.MaxValue;
            Vector3 bestPoint = to;

            // NonAllocの結果は距離順とは限らないため、明示的に一番手前のHitを選ぶ。
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = trajectoryHits[i];

                if (hit.collider == null || IsIgnoredTrajectoryCollider(hit.collider))
                    continue;

                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    bestPoint = hit.point;
                    found = true;
                }
            }

            if (!found)
                return false;

            hitPoint = bestPoint;
            return true;
        }

        // 所持者や所持アイテム自身への当たりを予測判定から除外する。
        private bool IsIgnoredTrajectoryCollider(Collider hit)
        {
            if (hit == null)
                return true;

            Transform holderRoot = ResolveTrajectoryHolderRoot();

            if (holderRoot != null && hit.transform.IsChildOf(holderRoot))
                return true;

            Transform itemTransform = currentlyHandledItem?.ItemTransform;
            return itemTransform != null && hit.transform.IsChildOf(itemTransform);
        }

        // 所持者ルートを解決する。
        private Transform ResolveTrajectoryHolderRoot()
        {
            if (handleItemPoint == null)
                return transform;

            EntityMB ownerEntity = handleItemPoint.GetComponentInParent<EntityMB>();

            if (ownerEntity != null)
                return ownerEntity.transform;

            return handleItemPoint.root != null ? handleItemPoint.root : transform;
        }

        private Transform ResolveOwnerCollisionRoot()
        {
            if (handleItemPoint != null)
            {
                EntityMB ownerEntity = handleItemPoint.GetComponentInParent<EntityMB>();
                if (ownerEntity != null)
                    return ownerEntity.transform;

                if (handleItemPoint.root != null)
                    return handleItemPoint.root;
            }

            EntityMB selfEntity = GetComponentInParent<EntityMB>();
            if (selfEntity != null)
                return selfEntity.transform;

            return transform.root != null ? transform.root : transform;
        }

        // 軌跡描画用 LineRenderer を遅延生成・初期化する。
        private void EnsureTrajectoryRenderer()
        {
            if (trajectoryLineRenderer == null)
            {
                GameObject lineObject = new GameObject("Bomb Throw Trajectory");
                lineObject.transform.SetParent(transform, false);
                trajectoryLineRenderer = lineObject.AddComponent<LineRenderer>();
            }

            trajectoryLineRenderer.useWorldSpace = true;
            trajectoryLineRenderer.startWidth = trajectoryLineWidth;
            trajectoryLineRenderer.endWidth = trajectoryLineWidth;
            trajectoryLineRenderer.startColor = trajectoryColor;
            trajectoryLineRenderer.endColor = trajectoryColor;
            trajectoryLineRenderer.numCapVertices = 4;
            trajectoryLineRenderer.numCornerVertices = 4;

            if (trajectoryLineRenderer.sharedMaterial == null)
            {
                if (trajectoryLineFallbackMaterial != null)
                {
                    trajectoryLineRenderer.sharedMaterial = trajectoryLineFallbackMaterial;
                    return;
                }

                Shader shader = Shader.Find("Sprites/Default");

                if (shader == null)
                    shader = Shader.Find("Universal Render Pipeline/Unlit");

                if (shader != null)
                {
                    ownedTrajectoryMaterial = new Material(shader);
                    trajectoryLineRenderer.sharedMaterial = ownedTrajectoryMaterial;
                }
            }
        }

        // 着地点マーカーを遅延生成・初期化する。
        private void EnsureTrajectoryHitMarker()
        {
            if (trajectoryHitMarkerTransform == null)
            {
                GameObject markerObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                markerObject.name = "Bomb Throw Hit Marker";
                markerObject.transform.SetParent(transform, false);

                Collider markerCollider = markerObject.GetComponent<Collider>();
                if (markerCollider != null)
                    Destroy(markerCollider);

                trajectoryHitMarkerTransform = markerObject.transform;
                trajectoryHitMarkerRenderer = markerObject.GetComponent<Renderer>();
            }

            trajectoryHitMarkerTransform.localScale = Vector3.one * trajectoryHitMarkerDiameter;

            if (trajectoryHitMarkerRenderer == null)
                return;

            if (trajectoryHitMarkerRenderer.sharedMaterial == null && trajectoryHitMarkerFallbackMaterial != null)
            {
                trajectoryHitMarkerRenderer.sharedMaterial = trajectoryHitMarkerFallbackMaterial;
            }

            trajectoryHitMarkerPropertyBlock ??= new MaterialPropertyBlock();
            trajectoryHitMarkerRenderer.GetPropertyBlock(trajectoryHitMarkerPropertyBlock);
            trajectoryHitMarkerPropertyBlock.SetColor(BaseColorShaderPropertyId, trajectoryHitMarkerColor);
            trajectoryHitMarkerPropertyBlock.SetColor(ColorShaderPropertyId, trajectoryHitMarkerColor);
            trajectoryHitMarkerRenderer.SetPropertyBlock(trajectoryHitMarkerPropertyBlock);
        }

        // 着地点マーカーを表示する。
        private void ShowTrajectoryHitMarker(Vector3 hitPoint)
        {
            EnsureTrajectoryHitMarker();

            if (trajectoryHitMarkerTransform == null)
                return;

            // 予想着地点を見失わないよう、ヒット地点にだけ小さな球を表示する。
            trajectoryHitMarkerTransform.position = hitPoint;
            trajectoryHitMarkerTransform.gameObject.SetActive(true);
        }

        // 着地点マーカーを非表示にする。
        private void HideTrajectoryHitMarker()
        {
            if (trajectoryHitMarkerTransform == null)
                return;

            trajectoryHitMarkerTransform.gameObject.SetActive(false);
        }

        // 軌跡描画をまとめて隠す。
        private void HideTrajectory()
        {
            if (trajectoryLineRenderer == null)
            {
                HideTrajectoryHitMarker();
                return;
            }

            trajectoryLineRenderer.enabled = false;
            trajectoryLineRenderer.positionCount = 0;
            HideTrajectoryHitMarker();
        }

        // Interaction イベントを受けて、向き制御と所持開始を振り分ける。
        private void HandleInteractionEvent(InteractionEventData eventData)
        {
            switch (eventData.EventType)
            {
                case InteractionEventType.Started:
                    TryApplyInteractionFacing(eventData);
                    break;

                case InteractionEventType.Canceled:
                    ClearInteractionFacing(force: false);
                    break;

                case InteractionEventType.Completed:
                    ClearInteractionFacing(force: true);

                    if (eventData.Interactable is CarryableItemInteractableAdapter adapter)
                        HandleItem(adapter.CarryableItem);
                    break;
            }
        }

        // 交互作用開始時に対象へ向きを合わせる。
        private void TryApplyInteractionFacing(InteractionEventData eventData)
        {
            if (!faceInteractionTargetOnStart)
                return;

            if (eventData.Interactable is not IInteractionFacingTarget facingTarget || !facingTarget.AllowInteractionSourceFacing)
                return;

            Transform targetTransform = facingTarget.InteractionFacingTransform;
            EntityFacingControllerMB resolvedFacingController = ResolveFacingController();

            if (resolvedFacingController == null || targetTransform == null)
                return;

            resolvedFacingController.SetFacingTargetTransform(
                EntityFacingChannels.Interaction,
                targetTransform,
                EntityFacingPriorities.Interaction);
            activeInteractionFacingOwnsChannel = true;
        }

        // interaction facing の ownership を見て向き制御を解除する。
        private void ClearInteractionFacing(bool force)
        {
            if (!force && !activeInteractionFacingOwnsChannel)
                return;

            activeInteractionFacingOwnsChannel = false;
            ResolveFacingController()?.ClearFacing(EntityFacingChannels.Interaction);
        }

        // Facing Controller を解決する。
        private EntityFacingControllerMB ResolveFacingController()
        {
            if (facingController == null)
                facingController = GetComponentInParent<EntityFacingControllerMB>();

            return facingController;
        }

        // 現在の最良候補が所持可能アイテムかを取り出す。
        private ICarryableItem GetCurrentBestCarryableItem()
        {
            return CurrentBestInteractable is CarryableItemInteractableAdapter adapter
                ? adapter.CarryableItem
                : null;
        }

        // runtime 用のデバッグ値を ValueStore に反映する。
        private void PublishRuntimeValues()
        {
            ResolveRuntimeReferences(logMissingEntity: false);

            if (valueStore == null || !entityRef.IsValid)
                return;

            valueStore.Set(entityRef, ValueKeys.Runtime.IsHandlingItem, isHandlingItem);
            valueStore.Set(entityRef, ValueKeys.Runtime.IsThrowPoseActive, IsThrowPoseActive());
            valueStore.Set(entityRef, ValueKeys.Runtime.IsItemThrowAiming, IsItemThrowAiming());
            valueStore.Set(entityRef, ValueKeys.Runtime.ThrowSequence, throwSequence);
            valueStore.Set(entityRef, ValueKeys.Runtime.CanInteract, currentCanInteract);
        }

        // ValueStore / EntityRef / watch handle を安全に解決する。
        private void ResolveRuntimeReferences(bool logMissingEntity)
        {
            if (valueStore == null)
            {
                SceneKernelMB kernelMB = GetComponentInParent<SceneKernelMB>();

                if (kernelMB != null && kernelMB.Kernel != null)
                    valueStore = kernelMB.Kernel.ValueStore;
            }

            if (!entityRef.IsValid)
            {
                EntityMB entityMB = GetComponentInParent<EntityMB>();

                if (entityMB != null && entityMB.HasEntity)
                {
                    entityRef = entityMB.Entity;
                }
                else if (logMissingEntity)
                {
                    Debug.LogWarning($"{nameof(PlayerItemHandleStateMB)}: EntityMB is not found or not bound.", this);
                }
            }

            if (canInteractHandle == null && valueStore != null && entityRef.IsValid)
                canInteractHandle = valueStore.GetHandle(entityRef, ValueKeys.Interaction.CanInteract);

            if (fatigueInteractHandle == null && valueStore != null && entityRef.IsValid)
                fatigueInteractHandle = valueStore.GetHandle(entityRef, ValueKeys.Runtime.IsFatigueInteracting);
        }

        // 疲労インタラクト中かどうかを判定する。
        private bool IsFatigueInteracting()
        {
            ResolveRuntimeReferences(logMissingEntity: false);
            return fatigueInteractHandle != null && fatigueInteractHandle.CurrentValue;
        }

        // Interaction 可否の runtime 値を更新する。
        private bool RefreshInteractionGateDebugValue()
        {
            ResolveRuntimeReferences(logMissingEntity: false);
            currentCanInteract = canInteractHandle == null || canInteractHandle.CurrentValue;
            return currentCanInteract;
        }

        // 入力由来の投擲 / チャージを中断する。
        private void CancelInputDrivenItemAction()
        {
            isThrowChargePending = false;
            throwChargePendingTimer = 0f;
            EndThrowCharge();

            throwForceChargeTimer = 0f;
            ResetEmptyHandThrowPreview();
            HideTrajectory();
        }

        // 投擲予測やチャージ pose が有効かを判定する。
        private bool IsThrowPoseActive()
        {
            return isThrowCharging || isEmptyHandThrowPreviewActive;
        }

        // 実際に投擲チャージ中かを判定する。
        private bool IsItemThrowAiming()
        {
            return isHandlingItem && currentlyHandledItem != null && isThrowCharging;
        }

        // 手ぶら投擲予測の runtime 状態を初期化する。
        private void ResetEmptyHandThrowPreview()
        {
            emptyHandThrowPreviewTimer = 0f;
            isEmptyHandThrowPreviewActive = false;
        }

        // まだ未消費の押下入力だけを1回分消費する。
        private bool ConsumeInputPress()
        {
            int currentSequence = InputPressSequence;

            if (currentSequence == 0 || currentSequence == lastConsumedInputPressSequence)
                return false;

            lastConsumedInputPressSequence = currentSequence;
            return true;
        }

        private enum HeldItemReleaseKind
        {
            Drop,
            Throw
        }
    }
}
