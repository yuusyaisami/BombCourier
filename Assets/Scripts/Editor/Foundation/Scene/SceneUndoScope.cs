using UnityEditor;
using Object = UnityEngine.Object;

namespace BC.Editor.Foundation.Scene
{
    public sealed class SceneUndoScope : System.IDisposable
    {
        private readonly Object target;
        private readonly string undoName;
        private readonly bool recordPrefabOverrides;
        private readonly bool markDirty;
        private bool ended;

        public SceneUndoScope(
            Object target,
            string undoName,
            bool recordPrefabOverrides = true,
            bool markDirty = true)
        {
            this.target = target;
            this.undoName = string.IsNullOrWhiteSpace(undoName) ? "Scene Edit" : undoName;
            this.recordPrefabOverrides = recordPrefabOverrides;
            this.markDirty = markDirty;
            EditorGUI.BeginChangeCheck();
        }

        public bool TryRecordChanges()
        {
            if (ended)
                return false;

            ended = true;

            if (!EditorGUI.EndChangeCheck())
                return false;

            if (target == null)
                return true;

            Undo.RecordObject(target, undoName);

            if (recordPrefabOverrides)
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);

            if (markDirty)
                EditorUtility.SetDirty(target);

            return true;
        }

        public void Dispose()
        {
            if (!ended)
            {
                ended = true;
                EditorGUI.EndChangeCheck();
            }
        }
    }
}
