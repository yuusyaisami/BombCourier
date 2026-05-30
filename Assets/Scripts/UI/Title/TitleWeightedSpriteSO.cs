using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BC.UI.Title
{
    /// <summary>重み付き Sprite 候補リストを保持し、確率的な抽選を提供する SO。</summary>
    [CreateAssetMenu(menuName = "BombCourier/UI/Title Weighted Sprites", fileName = "TitleWeightedSprites")]
    public sealed class TitleWeightedSpriteSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [Tooltip("表示する Sprite。")]
            public Sprite sprite;
            [Tooltip("選ばれやすさの重み。大きいほど高頻度。")]
            [Min(0f)]
            public float weight;
        }

        [SerializeField] private List<Entry> entries = new();

        /// <summary>重み付きランダム抽選で Sprite を 1 つ返す。エントリーが空の場合は null。</summary>
        public Sprite PickRandom()
        {
            if (entries == null || entries.Count == 0) return null;

            float totalWeight = 0f;
            foreach (Entry e in entries) totalWeight += Mathf.Max(0f, e.weight);

            if (totalWeight <= 0f) return entries[Random.Range(0, entries.Count)].sprite;

            float pick = Random.Range(0f, totalWeight);
            float accumulated = 0f;
            foreach (Entry e in entries)
            {
                accumulated += Mathf.Max(0f, e.weight);
                if (pick <= accumulated) return e.sprite;
            }

            return entries[entries.Count - 1].sprite;
        }
    }
}
