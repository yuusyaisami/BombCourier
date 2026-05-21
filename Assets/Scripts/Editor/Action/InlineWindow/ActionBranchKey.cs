using System;

namespace BC.Editor.Action
{
    public readonly struct ActionBranchKey : IEquatable<ActionBranchKey>
    {
        public ActionBranchKey(string rootPropertyPath, string branchPath)
        {
            RootPropertyPath = rootPropertyPath ?? string.Empty;
            BranchPath = branchPath ?? string.Empty;
        }

        public string RootPropertyPath { get; }
        public string BranchPath { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(RootPropertyPath) && !string.IsNullOrWhiteSpace(BranchPath);

        public ActionBranchKey Append(params string[] segments)
        {
            string resolvedPath = BranchPath;

            for (int i = 0; i < segments.Length; i++)
            {
                string segment = segments[i];

                if (string.IsNullOrWhiteSpace(segment))
                    continue;

                resolvedPath = string.IsNullOrEmpty(resolvedPath)
                    ? segment
                    : $"{resolvedPath}/{segment}";
            }

            return new ActionBranchKey(RootPropertyPath, resolvedPath);
        }

        public bool Equals(ActionBranchKey other)
        {
            return string.Equals(RootPropertyPath, other.RootPropertyPath, StringComparison.Ordinal) &&
                   string.Equals(BranchPath, other.BranchPath, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActionBranchKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                StringComparer.Ordinal.GetHashCode(RootPropertyPath ?? string.Empty),
                StringComparer.Ordinal.GetHashCode(BranchPath ?? string.Empty));
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(BranchPath) ? RootPropertyPath : $"{RootPropertyPath}:{BranchPath}";
        }
    }
}
