using System;
using BC.ActionSystem;
using BC.Base;
using BC.Player;
using BC.Rendering;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace BC.Character
{
    [DisallowMultipleComponent]
    public sealed class NPCObjectMB : MonoBehaviour, IInteractionTarget, IInteractionPromptProvider, IInteractionFacingTarget
    {
        [Header("Interaction")]
        [SerializeField] private Transform interactionTransform;
        [SerializeField] private Transform promptAnchor;
        [FormerlySerializedAs("outlineTarget")]
        [SerializeField] private InteractionVisualTargetMB visualTarget;
        [SerializeField, Min(0.05f)] private float maxInteractionDistance = 2.5f;
        [SerializeField, Range(0f, 180f)] private float maxInteractionAngle = 65f;
        [SerializeField, Min(0f)] private float requiredHoldDuration;
        [SerializeField] private Vector3 promptWorldOffset = new(0f, 1.5f, 0f);

        [Header("Facing")]
        [SerializeField] private bool requestInteractorToFaceTarget = true;
        [SerializeField] private bool faceInteractorOnInteraction = true;
        [SerializeField] private EntityFacingControllerMB facingController;

        [Header("Action")]
        [SerializeField] private InlineAction interactionAction;

        [Header("Debug")]
        [SerializeField] private bool logInteractionUntilTalkHooked = true;
        [SerializeField] private UnityEvent interactionCompleted;

        private bool activeInteractionFacingOwnsChannel;
        private bool isInteractionInProgress;

        public event Action<NPCObjectMB> Interacted;

        public Transform InteractionTransform => interactionTransform != null ? interactionTransform : transform;
        public float RequiredHoldDuration => requiredHoldDuration;
        public InteractionVisualTargetMB VisualTarget => visualTarget;
        public Transform PromptAnchor => promptAnchor != null ? promptAnchor : InteractionTransform;
        public Vector3 PromptWorldOffset => promptWorldOffset;
        public bool AllowInteractionSourceFacing => requestInteractorToFaceTarget;
        public Transform InteractionFacingTransform => InteractionTransform;
        public bool IsInteractionInProgress => isInteractionInProgress;

        private void Reset()
        {
            interactionTransform = transform;
            promptAnchor = transform;
            visualTarget = GetComponentInChildren<InteractionVisualTargetMB>(true);
            facingController = GetComponentInParent<EntityFacingControllerMB>();
        }

        private void OnValidate()
        {
            if (interactionTransform == null)
            {
                interactionTransform = transform;
            }

            if (promptAnchor == null)
            {
                promptAnchor = interactionTransform;
            }

            if (visualTarget == null)
            {
                visualTarget = GetComponentInChildren<InteractionVisualTargetMB>(true);
            }

            if (facingController == null)
            {
                facingController = GetComponentInParent<EntityFacingControllerMB>();
            }

            maxInteractionDistance = Mathf.Max(0.05f, maxInteractionDistance);
            requiredHoldDuration = Mathf.Max(0f, requiredHoldDuration);
        }

        public bool TryGetCandidateScore(InteractionQuery query, out float score)
        {
            score = float.MaxValue;

            Transform targetTransform = InteractionTransform;
            if (targetTransform == null || !isActiveAndEnabled)
            {
                return false;
            }

            return InteractionScoringUtility.TryGetPlanarFacingScore(
                query,
                targetTransform.position,
                maxInteractionDistance,
                maxInteractionAngle,
                out score);
        }

        public void OnInteractionStarted(InteractionEventData eventData)
        {
            isInteractionInProgress = true;

            if (!faceInteractorOnInteraction)
                return;

            EntityFacingControllerMB resolvedFacingController = ResolveFacingController();
            if (resolvedFacingController == null || eventData.SourceFacingTransform == null)
                return;

            resolvedFacingController.SetFacingTargetTransform(
                EntityFacingChannels.Interaction,
                eventData.SourceFacingTransform,
                EntityFacingPriorities.Interaction);
            activeInteractionFacingOwnsChannel = true;
        }

        public void OnInteractionUpdated(InteractionEventData eventData)
        {
        }

        public void OnInteractionCanceled(InteractionEventData eventData)
        {
            isInteractionInProgress = false;
            ClearInteractionFacing(force: false);
        }

        public void OnInteractionCompleted(InteractionEventData eventData)
        {
            if (logInteractionUntilTalkHooked)
            {
                Debug.Log($"{nameof(NPCObjectMB)} '{name}' was interacted with.", this);
            }

            Interacted?.Invoke(this);
            interactionCompleted?.Invoke();

            if (TryGetSelfEntity(out EntityRef selfEntity))
            {
                // Interact 完了後の action 実行中も interaction 扱いを維持して、
                // NeckLook の suspendWhileNpcInteractionActive が会話開始中にも効くようにする。
                RunInteractionActionAsync(selfEntity, eventData.SourceEntity).Forget();
                return;
            }

            if (interactionAction != null)
            {
                Debug.LogWarning($"{nameof(NPCObjectMB)}: interaction action was skipped because EntityMB is missing or not bound.", this);
            }

            ClearInteractionFacing(force: true);
            isInteractionInProgress = false;
        }

        private async UniTaskVoid RunInteractionActionAsync(EntityRef selfEntity, EntityRef sourceEntity)
        {
            try
            {
                if (interactionAction != null)
                {
                    ActionExecutionResult result = await InlineActionExecutionUtility.ExecuteAsync(
                        this,
                        selfEntity,
                        interactionAction,
                        sourceEntity);

                    if (result.IsFailed)
                    {
                        Debug.LogWarning($"{nameof(NPCObjectMB)}: interaction action failed. {result.Message}", this);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                // await 中に NPC が破棄された場合 (stage reload / despawn)、破棄済みオブジェクト上で
                // ClearInteractionFacing()(内部で GetComponentInParent を呼ぶ)を実行すると
                // MissingReferenceException になる。Unity の fake-null を見て、生存時のみ後始末する。
                if (this != null)
                {
                    ClearInteractionFacing(force: true);
                    // 念のため ownership flag も落として、後続 interaction へ状態を持ち越さない。
                    activeInteractionFacingOwnsChannel = false;
                    isInteractionInProgress = false;
                }
            }
        }

        private void OnDisable()
        {
            isInteractionInProgress = false;
            ClearInteractionFacing(force: true);
        }

        private EntityFacingControllerMB ResolveFacingController()
        {
            if (facingController == null)
                facingController = GetComponentInParent<EntityFacingControllerMB>();

            return facingController;
        }

        private void ClearInteractionFacing(bool force)
        {
            if (!force && !activeInteractionFacingOwnsChannel)
                return;

            activeInteractionFacingOwnsChannel = false;
            ResolveFacingController()?.ClearFacing(EntityFacingChannels.Interaction);
        }

        private bool TryGetSelfEntity(out EntityRef selfEntity)
        {
            EntityMB entityMB = GetComponentInParent<EntityMB>();
            if (entityMB != null && entityMB.HasEntity)
            {
                selfEntity = entityMB.Entity;
                return true;
            }

            selfEntity = default;
            return false;
        }
    }
}