using System;
using System.Collections.Generic;
using BC.Base;
using BC.Item;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BC.Player
{
    public sealed class PlayerInteractionController : IInteractionSource, IDisposable
    {
        private const int MaxItemHits = 32;

        private readonly InputAction inputAction;
        private readonly Transform interactionPoint;
        private readonly Transform facingTransform;
        private readonly Func<EntityRef> sourceEntityProvider;
        private readonly float interactionDistance;
        private readonly float interactionAngleThreshold;
        private readonly LayerMask interactionLayerMask;
        private readonly Collider[] interactionHits = new Collider[MaxItemHits];
        private readonly List<InteractionCandidate> candidates = new(16);
        private readonly HashSet<UnityEngine.Object> candidateKeys = new();
        private readonly Dictionary<MonoBehaviour, CarryableItemInteractableAdapter> carryableAdapters = new();
        private readonly InteractionHighlightStateTracker highlightStateTracker = new();

        private bool isBound;
        private bool pendingPress;
        private bool pendingRelease;
        private bool isInputPressed;
        private float inputHoldDuration;
        private int inputPressSequence;
        private int inputReleaseSequence;
        private IInteractionTarget currentBestInteractable;
        private IInteractionTarget activeInteractable;
        private float activeHoldDuration;

        public PlayerInteractionController(
            InputAction inputAction,
            Transform interactionPoint,
            Transform facingTransform,
            Func<EntityRef> sourceEntityProvider,
            float interactionDistance,
            float interactionAngleThreshold,
            LayerMask interactionLayerMask)
        {
            this.inputAction = inputAction;
            this.interactionPoint = interactionPoint;
            this.facingTransform = facingTransform;
            this.sourceEntityProvider = sourceEntityProvider;
            this.interactionDistance = interactionDistance;
            this.interactionAngleThreshold = interactionAngleThreshold;
            this.interactionLayerMask = interactionLayerMask;
        }

        public bool IsInputPressed => isInputPressed;
        public float InputHoldDuration => inputHoldDuration;
        public int InputPressSequence => inputPressSequence;
        public int InputReleaseSequence => inputReleaseSequence;
        public bool HasCandidate => currentBestInteractable != null;
        public IInteractionTarget CurrentBestInteractable => currentBestInteractable;
        public IInteractionTarget ActiveInteractable => activeInteractable;
        public float ActiveHoldProgress => CalculateHoldProgress(activeInteractable, activeHoldDuration);
        public IReadOnlyList<InteractionCandidate> Candidates => candidates;

        public event Action<InteractionEventData> InteractionEvent;

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
                ClearHighlights();
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
            ClearHighlights();
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
            PruneDestroyedCarryableAdapters();
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

                IInteractionTarget interactable = ResolveInteractable(hit);

                if (interactable == null)
                    continue;

                UnityEngine.Object key = GetInteractableKey(interactable);

                if (key == null || !candidateKeys.Add(key))
                    continue;

                InteractionQuery query = new InteractionQuery(
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

                candidates.Add(new InteractionCandidate(interactable, score, false));
            }

            if (candidates.Count > 0)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    InteractionCandidate candidate = candidates[i];
                    bool isBest = ReferenceEquals(candidate.Interactable, currentBestInteractable);
                    candidates[i] = new InteractionCandidate(candidate.Interactable, candidate.Score, isBest);
                }
            }

            ApplyCandidateHighlights();
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
            DispatchInteractionEvent(InteractionEventType.Updated, activeInteractable, activeHoldDuration);

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

            DispatchInteractionEvent(InteractionEventType.Started, activeInteractable, activeHoldDuration);

            if (activeInteractable.RequiredHoldDuration <= 0f)
                CompleteActiveInteraction();
        }

        private void CompleteActiveInteraction()
        {
            if (activeInteractable == null)
                return;

            IInteractionTarget completedInteractable = activeInteractable;
            float completedHoldDuration = activeHoldDuration;

            activeInteractable = null;
            activeHoldDuration = 0f;

            DispatchInteractionEvent(InteractionEventType.Completed, completedInteractable, completedHoldDuration);
        }

        private void CancelActiveInteraction()
        {
            if (activeInteractable == null)
                return;

            IInteractionTarget canceledInteractable = activeInteractable;
            float canceledHoldDuration = activeHoldDuration;

            activeInteractable = null;
            activeHoldDuration = 0f;

            DispatchInteractionEvent(InteractionEventType.Canceled, canceledInteractable, canceledHoldDuration);
        }

        private void DispatchInteractionEvent(
            InteractionEventType eventType,
            IInteractionTarget interactable,
            float holdDuration)
        {
            if (interactable == null)
                return;

            InteractionEventData eventData = new InteractionEventData(
                this,
                sourceEntityProvider != null ? sourceEntityProvider() : default,
                facingTransform,
                interactable,
                eventType,
                holdDuration,
                CalculateHoldProgress(interactable, holdDuration));

            switch (eventType)
            {
                case InteractionEventType.Started:
                    interactable.OnInteractionStarted(eventData);
                    break;
                case InteractionEventType.Updated:
                    interactable.OnInteractionUpdated(eventData);
                    break;
                case InteractionEventType.Canceled:
                    interactable.OnInteractionCanceled(eventData);
                    break;
                case InteractionEventType.Completed:
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

        private void ApplyCandidateHighlights()
        {
            highlightStateTracker.Apply(candidates);
        }

        private void ClearCandidates()
        {
            candidates.Clear();
            candidateKeys.Clear();
            currentBestInteractable = null;
        }

        private void ClearHighlights()
        {
            highlightStateTracker.ClearHighlights();
        }

        private IInteractionTarget ResolveInteractable(Collider hit)
        {
            IInteractionTarget interactable = hit.GetComponentInParent<IInteractionTarget>();

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

        private void PruneDestroyedCarryableAdapters()
        {
            if (carryableAdapters.Count == 0)
                return;

            bool removedAny = false;

            foreach (KeyValuePair<MonoBehaviour, CarryableItemInteractableAdapter> pair in carryableAdapters)
            {
                if (pair.Key == null)
                {
                    removedAny = true;
                    break;
                }
            }

            if (!removedAny)
                return;

            Dictionary<MonoBehaviour, CarryableItemInteractableAdapter> aliveAdapters = new(carryableAdapters.Count);

            foreach (KeyValuePair<MonoBehaviour, CarryableItemInteractableAdapter> pair in carryableAdapters)
            {
                if (pair.Key != null)
                    aliveAdapters.Add(pair.Key, pair.Value);
            }

            carryableAdapters.Clear();

            foreach (KeyValuePair<MonoBehaviour, CarryableItemInteractableAdapter> pair in aliveAdapters)
            {
                carryableAdapters.Add(pair.Key, pair.Value);
            }
        }

        private static UnityEngine.Object GetInteractableKey(IInteractionTarget interactable)
        {
            if (interactable is CarryableItemInteractableAdapter adapter)
                return adapter.OwnerObject;

            return interactable as UnityEngine.Object;
        }

        private float CalculateHoldProgress(IInteractionTarget interactable, float holdDuration)
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
