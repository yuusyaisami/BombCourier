using System;
using BC.ActionSystem;
using BC.Base;
using BC.Player;
using BC.Rendering;
using UnityEngine;
using UnityEngine.Events;

namespace BC.Character
{
    [DisallowMultipleComponent]
    public sealed class NPCObjectMB : MonoBehaviour, IPlayerInteractable, IPlayerInteractionPromptProvider
    {
        [Header("Interaction")]
        [SerializeField] private Transform interactionTransform;
        [SerializeField] private Transform promptAnchor;
        [SerializeField] private PickupOutlineTargetMB outlineTarget;
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
        public PickupOutlineTargetMB OutlineTarget => outlineTarget;
        public Transform PromptAnchor => promptAnchor != null ? promptAnchor : InteractionTransform;
        public Vector3 PromptWorldOffset => promptWorldOffset;

        private void Reset()
        {
            interactionTransform = transform;
            promptAnchor = transform;
            outlineTarget = GetComponentInChildren<PickupOutlineTargetMB>(true);
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

            maxInteractionDistance = Mathf.Max(0.05f, maxInteractionDistance);
            requiredHoldDuration = Mathf.Max(0f, requiredHoldDuration);
        }

        public bool TryGetCandidateScore(PlayerInteractionQuery query, out float score)
        {
            score = float.MaxValue;

            Transform targetTransform = InteractionTransform;
            if (targetTransform == null || !isActiveAndEnabled)
            {
                return false;
            }

            float allowedDistance = Mathf.Min(Mathf.Max(0.05f, maxInteractionDistance), Mathf.Max(0.05f, query.MaxDistance));
            float allowedAngle = Mathf.Min(maxInteractionAngle, query.MaxAngle);

            Vector3 toNpc = targetTransform.position - query.FacingPosition;
            toNpc.y = 0f;

            float sqrDistance = toNpc.sqrMagnitude;
            if (sqrDistance <= 0.0001f || sqrDistance > allowedDistance * allowedDistance)
            {
                return false;
            }

            Vector3 planarFacingForward = query.PlanarFacingForward;
            if (planarFacingForward.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            Vector3 directionToNpc = toNpc.normalized;
            float angle = Vector3.Angle(planarFacingForward, directionToNpc);
            if (angle > allowedAngle)
            {
                return false;
            }

            score = sqrDistance + angle * 0.05f;
            return true;
        }

        public void OnInteractionStarted(PlayerInteractionEventData eventData)
        {
        }

        public void OnInteractionUpdated(PlayerInteractionEventData eventData)
        {
        }

        public void OnInteractionCanceled(PlayerInteractionEventData eventData)
        {
        }

        public void OnInteractionCompleted(PlayerInteractionEventData eventData)
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