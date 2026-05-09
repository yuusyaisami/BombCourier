using System.Collections.Generic;
using UnityEngine;

namespace BombCourier.CameraIntro
{
    /// <summary>
    /// ステージごとのイントロカメラパス。
    /// 
    /// 基本は、このコンポーネントの子から IntroCameraPoint を集める。
    /// 生成マップの場合は、生成完了後に explicitPoints を差し替える設計にしてもよい。
    /// </summary>
    public sealed class IntroCameraPathAuthoring : MonoBehaviour
    {
        [SerializeField]
        private Transform pointRoot;

        [SerializeField]
        private IntroCameraPoint[] explicitPoints;

        public List<IntroCameraPoint> BuildOrderedPoints()
        {
            var result = new List<IntroCameraPoint>(16);

            if (explicitPoints != null && explicitPoints.Length > 0)
            {
                for (int i = 0; i < explicitPoints.Length; i++)
                {
                    if (explicitPoints[i] != null)
                    {
                        result.Add(explicitPoints[i]);
                    }
                }
            }
            else
            {
                Transform root = pointRoot != null ? pointRoot : transform;
                var found = root.GetComponentsInChildren<IntroCameraPoint>(true);

                for (int i = 0; i < found.Length; i++)
                {
                    if (found[i] != null)
                    {
                        result.Add(found[i]);
                    }
                }
            }

            result.Sort((a, b) => a.Order.CompareTo(b.Order));
            return result;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var points = BuildOrderedPoints();

            Gizmos.color = Color.green;

            for (int i = 0; i < points.Count - 1; i++)
            {
                if (points[i] == null || points[i + 1] == null)
                {
                    continue;
                }

                Gizmos.DrawLine(points[i].transform.position, points[i + 1].transform.position);
            }
        }
#endif
    }
}