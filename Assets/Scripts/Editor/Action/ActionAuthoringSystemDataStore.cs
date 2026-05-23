using System;
using System.Collections.Generic;
using BC.ActionSystem;
using UnityEditor;
using UnityEngine;

namespace BC.Editor.ActionSystem
{
    internal static class ActionAuthoringSystemDataStore
    {
        private const string AssetPath = "Assets/Settings/Editor/ActionAuthoringSystemData.asset";
        private static ActionAuthoringSystemData cachedData;

        internal static ActionAuthoringSystemData GetOrCreate()
        {
            if (cachedData != null)
                return cachedData;

            cachedData = AssetDatabase.LoadAssetAtPath<ActionAuthoringSystemData>(AssetPath);

            if (cachedData != null)
            {
                cachedData.NormalizeEntries();
                return cachedData;
            }

            EnsureFolders("Assets/Settings/Editor");
            cachedData = ScriptableObject.CreateInstance<ActionAuthoringSystemData>();
            cachedData.name = nameof(ActionAuthoringSystemData);
            AssetDatabase.CreateAsset(cachedData, AssetPath);
            AssetDatabase.SaveAssets();
            return cachedData;
        }

        internal static void RecordStepSelection(Type stepType)
        {
            if (stepType == null)
                return;

            ActionAuthoringSystemData data = GetOrCreate();

            if (data == null)
                return;

            data.RecordStepSelection(stepType.AssemblyQualifiedName, DateTime.UtcNow.Ticks);
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }

        internal static IReadOnlyList<Type> GetRecentStepTypes(int maxCount)
        {
            if (maxCount <= 0)
                return Array.Empty<Type>();

            ActionAuthoringSystemData data = GetOrCreate();

            if (data == null)
                return Array.Empty<Type>();

            IReadOnlyList<ActionAuthoringSystemData.StepSelectionEntry> entries = data.GetRecentStepSelections(Math.Max(maxCount, 1));
            List<Type> result = new(entries.Count);
            bool removedInvalid = false;

            for (int i = 0; i < entries.Count; i++)
            {
                ActionAuthoringSystemData.StepSelectionEntry entry = entries[i];

                if (entry == null || string.IsNullOrWhiteSpace(entry.StepTypeName))
                    continue;

                Type stepType = ResolveStepType(entry.StepTypeName);

                if (stepType == null || !typeof(ActionStepAuthoring).IsAssignableFrom(stepType))
                {
                    removedInvalid = true;
                    continue;
                }

                result.Add(stepType);
            }

            if (removedInvalid)
            {
                data.NormalizeEntries();
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssets();
            }

            return result;
        }

        private static Type ResolveStepType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return null;

            Type byAssemblyQualifiedName = Type.GetType(typeName, false);

            if (byAssemblyQualifiedName != null)
                return byAssemblyQualifiedName;

            var discoveredTypes = TypeCache.GetTypesDerivedFrom<ActionStepAuthoring>();

            for (int i = 0; i < discoveredTypes.Count; i++)
            {
                Type candidate = discoveredTypes[i];

                if (candidate == null)
                    continue;

                if (string.Equals(candidate.FullName, typeName, StringComparison.Ordinal) ||
                    string.Equals(candidate.AssemblyQualifiedName, typeName, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void EnsureFolders(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string[] parts = folderPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";

                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);

                current = next;
            }
        }
    }
}
