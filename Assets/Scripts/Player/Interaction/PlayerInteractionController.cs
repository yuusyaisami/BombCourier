using System;
using System.Collections.Generic;
using BC.Item;
using BC.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BC.Player
{
    public sealed class PlayerInteractionController : IPlayerInteractionSource, IDisposable
    {
        private readonly struct OutlineState
        {
            public readonly int Revision;
            public readonly PickupOutlineKind Kind;

            public OutlineState(int revision, PickupOutlineKind kind)
            {
                Revision = revision;
                Kind = kind;
            }
        }

        private const int MaxItemHits = 32;

        private readonly InputAction inputAction;
        private readonly Transform interactionPoint;
        private readonly Transform facingTransform;
        private readonly float interactionDistance;
        private readonly float interactionAngleThreshold;
        private readonly LayerMask interactionLayerMask;
        private readonly Collider[] interactionHits = new Collider[MaxItemHits];
        private readonly List<PlayerInteractionCandidate> candidates = new(16);
        private readonly HashSet<UnityEngine.Object> candidateKeys = new();
        private readonly Dictionary<MonoBehaviour, CarryableItemInteractableAdapter> carryableAdapters = new();
        private readonly Dictionary<PickupOutlineTargetMB, OutlineState> outlinedTargets = new();
        private readonly List<PickupOutlineTargetMB> staleOutlinedTargets = new();

        private bool isBound;
        private bool pendingPress;
        private bool pendingRelease;
        private bool isInputPressed;
        private float inputHoldDuration;
        private int inputPressSequence;
        private int inputReleaseSequence;
        private int outlineRevision;
        private IPlayerInteractable currentBestInteractable;
        private IPlayerInteractable activeInteractable;
        private float activeHoldDuration;

        public PlayerInteractionController(
            InputAction inputAction,
            Transform interactionPoint,
            Transform facingTransform,
            float interactionDistance,
            float interactionAngleThreshold,
            LayerMask interactionLayerMask)
        {
            this.inputAction = inputAction;
            this.interactionPoint = interactionPoint;
            this.facingTransform = facingTransform;
            this.interactionDistance = interactionDistance;
            this.interactionAngleThreshold = interactionAngleThreshold;
            this.interactionLayerMask = interactionLayerMask;
        }

        public bool IsInputPressed => isInputPressed;
        public float InputHoldDuration => inputHoldDuration;
        public int InputPressSequence => inputPressSequence;
        public int InputReleaseSequence => inputReleaseSequence;
        public bool HasCandidate => currentBestInteractable != null;
        public IPlayerInteractable CurrentBestInteractable => currentBestInteractable;
        public IPlayerInteractable ActiveInteractable => activeInteractable;
        public float ActiveHoldProgress => CalculateHoldProgress(activeInteractable, activeHoldDuration);
        public IReadOnlyList<PlayerInteractionCandidate> Candidates => candidates;

        public event Action<PlayerInteractionEventData> InteractionEvent;

        public void Bind()
        {
            if (isBound || inputAction == null)
                return;

            inputAction.started += OnInputStarted;
            inputAction.canceled += OnInputCanceled;
            isInputPressed = inputAction.IsPressed();
            inputHoldDuration = 0f;
            isBound = true;
        }

        public void Unbind()
        {
            if (!isBound || inputAction == null)
                return;

            inputAction.started -= OnInputStarted;
            inputAction.canceled -= OnInputCanceled;
            isBound = false;
        }

        public void Tick(float deltaTime, bool enableInteraction)
        {
            UpdateInputHoldDuration(deltaTime);

            if (!enableInteraction || interactionPoint == null || facingTransform == null)
            {
                CancelActiveInteraction();
                ClearCandidates();
                ClearOutlines();
                pendingPress = false;
                pendingRelease = false;
                return;
            }

            RefreshCandidates();

            if (pendingPress)
            {
                TryStartInteraction();
                pendingPress = false;
            }

            if (activeInteractable != null)
                TickActiveInteraction(deltaTime);

            pendingRelease = false;
        }

        public void ResetRuntimeState()
        {
            CancelActiveInteraction();
            ClearCandidateState();
            pendingPress = false;
            pendingRelease = false;
            isInputPressed = false;
            inputHoldDuration = 0f;
        }

        public void ClearCandidateState()
        {
            ClearCandidates();
            ClearOutlines();
        }

        public void Dispose()
        {
            Unbind();
            ResetRuntimeState();
        }

        private void UpdateInputHoldDuration(float deltaTime)
        {
            if (isInputPressed)
                inputHoldDuration += Mathf.Max(0f, deltaTime);
            else
                inputHoldDuration = 0f;
        }

        private void RefreshCandidates()
        {
            candidates.Clear();
            candidateKeys.Clear();
            currentBestInteractable = null;

            int hitCount = Physics.OverlapSphereNonAlloc(
                interactionPoint.position,
                interactionDistance,
                interactionHits,
                interactionLayerMask,
                QueryTriggerInteraction.Collide);

            float bestScore = float.MaxValue;
            Vector3 facingForward = facingTransform.forward;
            facingForward.y = 0f;

            if (facingForward.sqrMagnitude > 0.0001f)
                facingForward.Normalize();

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = interactionHits[i];

                if (hit == null)
                    continue;

                IPlayerInteractable interactable = ResolveInteractable(hit);

                if (interactable == null)
                    continue;

                UnityEngine.Object key = GetInteractableKey(interactable);

                if (key == null || !candidateKeys.Add(key))
                    continue;

                PlayerInteractionQuery query = new PlayerInteractionQuery(
                    interactionPoint.position,
                    facingTransform.position,
                    facingTransform.forward,
                    facingForward,
                    interactionDistance,
                    interactionAngleThreshold,
                    hit);

                if (!interactable.TryGetCandidateScore(query, out float score))
                    continue;

                bool isBest = score < bestScore;

                if (isBest)
                {
                    bestScore = score;
                    currentBestInteractable = interactable;
                }

                candidates.Add(new PlayerInteractionCandidate(interactable, score, false));
            }

            if (candidates.Count > 0)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    PlayerInteractionCandidate candidate = candidates[i];
                    bool isBest = ReferenceEquals(candidate.Interactable, currentBestInteractable);
                    candidates[i] = new PlayerInteractionCandidate(candidate.Interactable, candidate.Score, isBest);
                }
            }

            ApplyCandidateOutlines();
        }

        private void TickActiveInteraction(float deltaTime)
        {
            if (!IsActiveInteractableStillValid())
            {
                CancelActiveInteraction();
                return;
            }

            if (!isInputPressed || pendingRelease)
            {
                CancelActiveInteraction();
                return;
            }

            activeHoldDuration += Mathf.Max(0f, deltaTime);
            DispatchInteractionEvent(PlayerInteractionEventType.Updated, activeInteractable, activeHoldDuration);

            if (activeInteractable.RequiredHoldDuration <= 0f ||
                activeHoldDuration >= activeInteractable.RequiredHoldDuration)
            {
                CompleteActiveInteraction();
            }
        }

        private void TryStartInteraction()
        {
            if (activeInteractable != null || currentBestInteractable == null)
                return;

            activeInteractable = currentBestInteractable;
            activeHoldDuration = 0f;

            DispatchInteractionEvent(PlayerInteractionEventType.Started, activeInteractable, activeHoldDuration);

            if (activeInteractable.RequiredHoldDuration <= 0f)
                CompleteActiveInteraction();
        }

        private void CompleteActiveInteraction()
        {
            if (activeInteractable == null)
                return;

            IPlayerInteractable completedInteractable = activeInteractable;
            float completedHoldDuration = activeHoldDuration;

            activeInteractable = null;
            activeHoldDuration = 0f;

            DispatchInteractionEvent(PlayerInteractionEventType.Completed, completedInteractable, completedHoldDuration);
        }

        private void CancelActiveInteraction()
        {
            if (activeInteractable == null)
                return;

            IPlayerInteractable canceledInteractable = activeInteractable;
            float canceledHoldDuration = activeHoldDuration;

            activeInteractable = null;
            activeHoldDuration = 0f;

            DispatchInteractionEvent(PlayerInteractionEventType.Canceled, canceledInteractable, canceledHoldDuration);
        }

        private void DispatchInteractionEvent(
            PlayerInteractionEventType eventType,
            IPlayerInteractable interactable,
            float holdDuration)
        {
            if (interactable == null)
                return;

            PlayerInteractionEventData eventData = new PlayerInteractionEventData(
                this,
                interactable,
                eventType,
                holdDuration,
                CalculateHoldProgress(interactable, holdDuration));

            switch (eventType)
            {
                case PlayerInteractionEventType.Started:
                    interactable.OnInteractionStarted(eventData);
                    break;
                case PlayerInteractionEventType.Updated:
                    interactable.OnInteractionUpdated(eventData);
                    break;
                case PlayerInteractionEventType.Canceled:
                    interactable.OnInteractionCanceled(eventData);
                    break;
                case PlayerInteractionEventType.Completed:
                    interactable.OnInteractionCompleted(eventData);
                    break;
            }

            InteractionEvent?.Invoke(eventData);
        }

        private bool IsActiveInteractableStillValid()
        {
            if (activeInteractable == null)
                return false;

            UnityEngine.Object key = GetInteractableKey(activeInteractable);
            return key != null && candidateKeys.Contains(key);
        }

        private void ApplyCandidateOutlines()
        {
            outlineRevision++;

            for (int i = 0; i < candidates.Count; i++)
            {
                PlayerInteractionCandidate candidate = candidates[i];
                PickupOutlineTargetMB target = candidate.Interactable != null
                    ? candidate.Interactable.OutlineTarget
                    : null;

                if (target == null)
                    continue;

                PickupOutlineKind kind = candidate.IsBest
                    ? PickupOutlineKind.Best
                    : PickupOutlineKind.Candidate;

                if (outlinedTargets.TryGetValue(target, out OutlineState previousState) &&
                    previousState.Revision == outlineRevision &&
                    previousState.Kind >= kind)
                {
                    continue;
                }

                if (outlinedTargets.TryGetValue(target, out previousState) &&
                    previousState.Kind != kind)
                {
                    target.ClearOutline();
                }

                target.SetOutline(kind);
                outlinedTargets[target] = new OutlineState(outlineRevision, kind);
            }

            staleOutlinedTargets.Clear();

            foreach (KeyValuePair<PickupOutlineTargetMB, OutlineState> pair in outlinedTargets)
            {
                if (pair.Value.Revision == outlineRevision)
                    continue;

                if (pair.Key != null)
                    pair.Key.ClearOutline();

                staleOutlinedTargets.Add(pair.Key);
            }

            for (int i = 0; i < staleOutlinedTargets.Count; i++)
            {
                outlinedTargets.Remove(staleOutlinedTargets[i]);
            }
        }

        private void ClearCandidates()
        {
            candidates.Clear();
            candidateKeys.Clear();
            currentBestInteractable = null;
        }

        private void ClearOutlines()
        {
            foreach (KeyValuePair<PickupOutlineTargetMB, OutlineState> pair in outlinedTargets)
            {
                if (pair.Key != null)
                    pair.Key.ClearOutline();
            }

            outlinedTargets.Clear();
            staleOutlinedTargets.Clear();
        }

        private IPlayerInteractable ResolveInteractable(Collider hit)
        {
            IPlayerInteractable interactable = hit.GetComponentInParent<IPlayerInteractable>();

            if (interactable != null)
                return interactable;

            ICarryableItem carryableItem = hit.GetComponentInParent<ICarryableItem>();

            if (carryableItem is not MonoBehaviour owner)
                return null;

            if (!carryableAdapters.TryGetValue(owner, out CarryableItemInteractableAdapter adapter))
            {
                adapter = new CarryableItemInteractableAdapter(owner, carryableItem);
                carryableAdapters.Add(owner, adapter);
            }

            return adapter;
        }

        private static UnityEngine.Object GetInteractableKey(IPlayerInteractable interactable)
        {
            if (interactable is CarryableItemInteractableAdapter adapter)
                return adapter.KeyObject;

            return interactable as UnityEngine.Object;
        }

        private float CalculateHoldProgress(IPlayerInteractable interactable, float holdDuration)
        {
            if (interactable == null)
                return 0f;

            float requiredHoldDuration = interactable.RequiredHoldDuration;

            if (requiredHoldDuration <= 0f)
                return 1f;

            return Mathf.Clamp01(holdDuration / requiredHoldDuration);
        }

        private void OnInputStarted(InputAction.CallbackContext context)
        {
            isInputPressed = true;
            inputHoldDuration = 0f;
            inputPressSequence++;
            pendingPress = true;
        }

        private void OnInputCanceled(InputAction.CallbackContext context)
        {
            isInputPressed = false;
            inputHoldDuration = 0f;
            inputReleaseSequence++;
            pendingRelease = true;
        }
    }
}
