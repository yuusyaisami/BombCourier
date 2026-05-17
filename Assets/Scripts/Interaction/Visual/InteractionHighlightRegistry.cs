using System.Collections.Generic;
using UnityEngine;

namespace BC.Rendering
{
    public enum InteractionHighlightKind
    {
        Candidate = 1,
        Best = 2
    }

    public readonly struct InteractionHighlightEntry
    {
        public readonly Renderer Renderer;
        public readonly InteractionHighlightKind Kind;

        public InteractionHighlightEntry(Renderer renderer, InteractionHighlightKind kind)
        {
            Renderer = renderer;
            Kind = kind;
        }
    }

    public static class InteractionHighlightRegistry
    {
        private static readonly Dictionary<Renderer, InteractionHighlightKind> Entries = new(128);
        private static readonly List<Renderer> DeadRenderers = new(32);

        public static void Set(Renderer renderer, InteractionHighlightKind kind)
        {
            if (renderer == null)
                return;

            // Best を Candidate で上書きしない。
            if (Entries.TryGetValue(renderer, out InteractionHighlightKind current))
            {
                if (current == InteractionHighlightKind.Best && kind == InteractionHighlightKind.Candidate)
                    return;
            }

            Entries[renderer] = kind;
        }

        public static void Remove(Renderer renderer)
        {
            if (renderer == null)
                return;

            Entries.Remove(renderer);
        }

        public static void ClearAll()
        {
            Entries.Clear();
        }

        public static InteractionHighlightEntry[] CreateSnapshotArray()
        {
            DeadRenderers.Clear();

            foreach (KeyValuePair<Renderer, InteractionHighlightKind> pair in Entries)
            {
                Renderer renderer = pair.Key;

                if (renderer == null ||
                    !renderer.enabled ||
                    !renderer.gameObject.activeInHierarchy)
                {
                    DeadRenderers.Add(renderer);
                }
            }

            for (int i = 0; i < DeadRenderers.Count; i++)
            {
                Entries.Remove(DeadRenderers[i]);
            }

            if (Entries.Count == 0)
                return System.Array.Empty<InteractionHighlightEntry>();

            InteractionHighlightEntry[] result = new InteractionHighlightEntry[Entries.Count];

            int index = 0;
            foreach (KeyValuePair<Renderer, InteractionHighlightKind> pair in Entries)
            {
                result[index++] = new InteractionHighlightEntry(pair.Key, pair.Value);
            }

            return result;
        }
    }
}