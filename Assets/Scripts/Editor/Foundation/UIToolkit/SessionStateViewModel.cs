using System.Collections.Generic;
using UnityEditor;

namespace BC.Editor.Foundation.UIToolkit
{
    public sealed class SessionStateViewModel
    {
        private readonly string keyPrefix;

        public SessionStateViewModel(string keyPrefix)
        {
            this.keyPrefix = string.IsNullOrWhiteSpace(keyPrefix) ? "BC.Editor" : keyPrefix;
        }

        public string GetString(string key, string defaultValue = "")
        {
            return SessionState.GetString(BuildKey(key), defaultValue);
        }

        public void SetString(string key, string value)
        {
            SessionState.SetString(BuildKey(key), value ?? string.Empty);
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            return SessionState.GetInt(BuildKey(key), defaultValue);
        }

        public void SetInt(string key, int value)
        {
            SessionState.SetInt(BuildKey(key), value);
        }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            return SessionState.GetFloat(BuildKey(key), defaultValue);
        }

        public void SetFloat(string key, float value)
        {
            SessionState.SetFloat(BuildKey(key), value);
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            return SessionState.GetBool(BuildKey(key), defaultValue);
        }

        public void SetBool(string key, bool value)
        {
            SessionState.SetBool(BuildKey(key), value);
        }

        public HashSet<string> GetStringSet(string key)
        {
            string raw = GetString(key, string.Empty);
            HashSet<string> result = new(System.StringComparer.Ordinal);

            if (string.IsNullOrWhiteSpace(raw))
                return result;

            string[] values = raw.Split('|');

            for (int i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i]))
                    result.Add(values[i]);
            }

            return result;
        }

        public void SetStringSet(string key, IEnumerable<string> values)
        {
            if (values == null)
            {
                SetString(key, string.Empty);
                return;
            }

            SetString(key, string.Join("|", values));
        }

        private string BuildKey(string key)
        {
            return $"{keyPrefix}.{key}";
        }
    }
}
