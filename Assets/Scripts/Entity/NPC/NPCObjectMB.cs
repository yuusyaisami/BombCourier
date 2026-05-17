using System;
using BC.ActionSystem;
using BC.Base;
using BC.Player;
using BC.Rendering;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace BC.Character
{
    [DisallowMultipleComponent]
    public sealed class NPCObjectMB : MonoBehaviour, IInteractionTarget, IInteractionPromptProvider
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

        [Header("Action")]
        [SerializeField] private InlineAction interactionAction;

        [Header("Debug")]
        [SerializeField] private bool logInteractionUntilTalkHooked = true;
        [SerializeField] private UnityEvent interactionCompleted;

        public event Action<NPCObjectMB> Interacted;

        public Transform InteractionTransform => interactionTransform != null ? interactionTransform : transform;
        public float RequiredHoldDuration => requiredHoldDuration;
        public InteractionVisualTargetMB VisualTarget => visualTarget;
        public Transform PromptAnchor => promptAnchor != null ? promptAnchor : InteractionTransform;
        public Vector3 PromptWorldOffset => promptWorldOffset;

        private void Reset()
        {
            interactionTransform = transform;
            promptAnchor = transform;
            visualTarget = GetComponentInChildren<InteractionVisualTargetMB>(true);
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
        }

        public void OnInteractionUpdated(InteractionEventData eventData)
        {
        }

        public void OnInteractionCanceled(InteractionEventData eventData)
        {
        }

        public void OnInteractionCompleted(InteractionEventData eventData)
        {
            if (logInteractionUntilTalkHooked)
            {
                Debug.Log($"{nameof(NPCObjectMB)} '{name}' was interacted with.", this);
            }

            if (TryGetSelfEntity(out EntityRef selfEntity))
            {
                // Interact 完了から action を起動して、会話などの振る舞いを差し込めるようにする。
                InlineActionExecutionUtility.ExecuteAndForget(this, selfEntity, interactionAction, default, $"NPC interact '{name}'");
            }
            else if (interactionAction != null)
            {
                Debug.LogWarning($"{nameof(NPCObjectMB)}: interaction action was skipped because EntityMB is missing or not bound.", this);
            }

            Interacted?.Invoke(this);
            interactionCompleted?.Invoke();
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