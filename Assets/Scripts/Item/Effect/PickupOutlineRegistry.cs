using System.Collections.Generic;
using UnityEngine;

namespace BC.Rendering
{
    public enum PickupOutlineKind
    {
        Candidate = 1,
        Best = 2
    }
    public readonly struct PickupOutlineEntry
    {
        public readonly Renderer Renderer;
        public readonly PickupOutlineKind Kind;

        public PickupOutlineEntry(Renderer renderer, PickupOutlineKind kind)
        {
            Renderer = renderer;
            Kind = kind;
        }
    }
    public static class PickupOutlineRegistry
    {
        private static readonly Dictionary<Renderer, PickupOutlineKind> Entries = new(128);
        private static readonly List<Renderer> DeadRenderers = new(32);

        public static void Set(Renderer renderer, PickupOutlineKind kind)
        {
            if (renderer == null)
                return;

            // Best を Candidate で上書きしない。
            if (Entries.TryGetValue(renderer, out var current))
            {
                if (current == PickupOutlineKind.Best && kind == PickupOutlineKind.Candidate)
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

        public static PickupOutlineEntry[] CreateSnapshotArray()
        {
            DeadRenderers.Clear();

            foreach (var pair in Entries)
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
                return System.Array.Empty<PickupOutlineEntry>();

            var result = new PickupOutlineEntry[Entries.Count];

            int index = 0;
            foreach (var pair in Entries)
            {
                result[index++] = new PickupOutlineEntry(pair.Key, pair.Value);
            }

            return result;
        }
    }
}