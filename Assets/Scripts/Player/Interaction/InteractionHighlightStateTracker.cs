using System.Collections.Generic;
using BC.Rendering;

namespace BC.Player
{
    public sealed class InteractionHighlightStateTracker
    {
        private readonly struct HighlightState
        {
            public readonly int Revision;
            public readonly InteractionHighlightKind Kind;

            public HighlightState(int revision, InteractionHighlightKind kind)
            {
                Revision = revision;
                Kind = kind;
            }
        }

        private readonly Dictionary<InteractionVisualTargetMB, HighlightState> highlightedTargets = new();
        private readonly List<InteractionVisualTargetMB> staleHighlightedTargets = new();
        private int highlightRevision;

        public void Apply(IReadOnlyList<InteractionCandidate> candidates)
        {
            highlightRevision++;

            if (candidates != null)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    InteractionCandidate candidate = candidates[i];
                    InteractionVisualTargetMB target = candidate.Interactable != null
                        ? candidate.Interactable.VisualTarget
                        : null;

                    if (target == null)
                        continue;

                    InteractionHighlightKind kind = candidate.IsBest
                        ? InteractionHighlightKind.Best
                        : InteractionHighlightKind.Candidate;

                    ApplyTarget(target, kind);
                }
            }

            ClearStaleTargets();
        }

        public void ClearHighlights()
        {
            foreach (KeyValuePair<InteractionVisualTargetMB, HighlightState> pair in highlightedTargets)
            {
                if (pair.Key != null)
                    pair.Key.ClearHighlight();
            }

            highlightedTargets.Clear();
            staleHighlightedTargets.Clear();
        }

        private void ApplyTarget(InteractionVisualTargetMB target, InteractionHighlightKind kind)
        {
            if (highlightedTargets.TryGetValue(target, out HighlightState previousState) &&
                previousState.Revision == highlightRevision &&
                previousState.Kind >= kind)
            {
                return;
            }

            if (highlightedTargets.TryGetValue(target, out previousState) &&
                previousState.Kind != kind)
            {
                target.ClearHighlight();
            }

            target.SetHighlight(kind);
            highlightedTargets[target] = new HighlightState(highlightRevision, kind);
        }

        private void ClearStaleTargets()
        {
            staleHighlightedTargets.Clear();

            foreach (KeyValuePair<InteractionVisualTargetMB, HighlightState> pair in highlightedTargets)
            {
                if (pair.Value.Revision == highlightRevision)
                    continue;

                if (pair.Key != null)
                    pair.Key.ClearHighlight();

                staleHighlightedTargets.Add(pair.Key);
            }

            for (int i = 0; i < staleHighlightedTargets.Count; i++)
            {
                highlightedTargets.Remove(staleHighlightedTargets[i]);
            }
        }
    }
}