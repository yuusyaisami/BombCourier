using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BC.Base
{
    public static class MovementColliderPolicyValidator
    {
        public static bool TryValidate(
            Transform root,
            Collider bodyCollider,
            Collider footCollider,
            out string errorMessage)
        {
            errorMessage = null;

            if (root == null)
                return true;

            List<string> violations = new List<string>();

            if (footCollider != null && !footCollider.isTrigger)
            {
                violations.Add($"Foot collider must be trigger: {BuildHierarchyPath(footCollider.transform, root)} ({footCollider.GetType().Name})");
            }

            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];

                if (collider == null || !collider.enabled)
                    continue;

                if (collider == bodyCollider || collider == footCollider)
                    continue;

                if (collider.isTrigger)
                    continue;

                violations.Add($"Unregistered non-trigger collider: {BuildHierarchyPath(collider.transform, root)} ({collider.GetType().Name})");
            }

            if (violations.Count == 0)
                return true;

            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < violations.Count; i++)
            {
                builder.AppendLine(violations[i]);
            }

            errorMessage = builder.ToString().TrimEnd();
            return false;
        }

        private static string BuildHierarchyPath(Transform target, Transform root)
        {
            if (target == null || root == null)
                return "<unknown>";

            if (target == root)
                return root.name;

            Stack<string> names = new Stack<string>();
            Transform cursor = target;

            while (cursor != null)
            {
                names.Push(cursor.name);

                if (cursor == root)
                    break;

                cursor = cursor.parent;
            }

            return string.Join("/", names);
        }
    }
}