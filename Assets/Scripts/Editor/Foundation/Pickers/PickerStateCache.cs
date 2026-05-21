using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;

namespace BC.Editor.Foundation.Pickers
{
    public sealed class PickerStateCache
    {
        private readonly Dictionary<string, AdvancedDropdownState> states = new(StringComparer.Ordinal);

        public AdvancedDropdownState GetOrCreate(string stateKey)
        {
            string key = string.IsNullOrWhiteSpace(stateKey) ? "default" : stateKey;

            if (!states.TryGetValue(key, out AdvancedDropdownState state))
            {
                state = new AdvancedDropdownState();
                states.Add(key, state);
            }

            return state;
        }

        public void Clear()
        {
            states.Clear();
        }
    }
}
