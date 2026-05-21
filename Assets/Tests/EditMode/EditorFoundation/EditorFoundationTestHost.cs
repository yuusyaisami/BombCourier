using System;
using UnityEngine;

namespace BC.Editor.Tests
{
    internal sealed class EditorFoundationTestHost : ScriptableObject
    {
        [SerializeField] internal string label;
        [SerializeField] internal int[] values = Array.Empty<int>();
        [SerializeReference] internal TestManagedReference reference;
    }

    [Serializable]
    public sealed class TestManagedReference
    {
        [SerializeField] internal string name;
        [SerializeField] internal int amount;
    }
}
