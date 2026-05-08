using System.Collections.Generic;

namespace BC.Base
{
    internal sealed class NumericModifierSet
    {
        private readonly Dictionary<ValueModifierTagId, float> adds = new();
        private readonly Dictionary<ValueModifierTagId, float> muls = new();
        private float cashedValue;
        private bool dirty = true;

        public bool SetAdd(ValueModifierTagId tag, float value)
        {
            if (adds.TryGetValue(tag, out float current) && current.Equals(value))
                return false;

            adds[tag] = value;
            dirty = true;
            return true;
        }

        public bool SetMul(ValueModifierTagId tag, float value)
        {
            if (muls.TryGetValue(tag, out float current) && current.Equals(value))
                return false;

            muls[tag] = value;
            dirty = true;
            return true;
        }

        public bool RemoveAdd(ValueModifierTagId tag)
        {
            dirty = true;
            return adds.Remove(tag);
        }

        public bool RemoveMul(ValueModifierTagId tag)
        {
            dirty = true;
            return muls.Remove(tag);
        }

        public bool Clear()
        {
            bool changed = adds.Count > 0 || muls.Count > 0;
            dirty = true;
            adds.Clear();
            muls.Clear();
            return changed;
        }

        public float Evaluate(float baseValue)
        {
            if (!dirty) return cashedValue;
            float result = baseValue;

            foreach (float add in adds.Values)
            {
                result += add;
            }

            foreach (float mul in muls.Values)
            {
                result *= mul;
            }
            cashedValue = result;
            dirty = false;
            return result;
        }
    }
}