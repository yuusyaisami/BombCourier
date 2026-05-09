using UnityEngine;

namespace BombCourier.CameraIntro
{
    /// <summary>
    /// イントロカメラが通過するチェックポイント。
    /// 
    /// Transform.position:
    ///     カメラ位置
    /// 
    /// Transform.rotation:
    ///     lookAtTarget が未指定の場合のカメラ向き
    /// 
    /// lookAtTarget:
    ///     指定されている場合、この対象を見る。
    /// </summary>
    public sealed class IntroCameraPoint : MonoBehaviour
    {
        [Header("Ordering")]
        [SerializeField] private int order;

        [Header("Timing")]
        [SerializeField, Min(0.01f)]
        private float secondsFromPrevious = 2.0f;

        [SerializeField, Min(0f)]
        private float holdSeconds = 0.0f;

        [Header("Aim")]
        [SerializeField]
        private Transform lookAtTarget;

        [SerializeField]
        private float fallbackLookDistance = 10f;

        public int Order => order;

        /// <summary>
        /// 前のポイントからこのポイントまでの移動時間。
        /// 最初のポイントでは基本的に無視される。
        /// </summary>
        public float SecondsFromPrevious => Mathf.Max(0.01f, secondsFromPrevious);

        /// <summary>
        /// このポイント到達後に停止する時間。
        /// </summary>
        public float HoldSeconds => Mathf.Max(0f, holdSeconds);

        public bool HasLookAtTarget => lookAtTarget != null;

        public Vector3 GetLookAtPosition()
        {
            if (lookAtTarget != null)
            {
                return lookAtTarget.position;
            }

            return transform.position + transform.forward * fallbackLookDistance;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(transform.position, 0.25f);

            Gizmos.color = Color.yellow;
            Vector3 target = GetLookAtPosition();
            Gizmos.DrawLine(transform.position, target);
            Gizmos.DrawSphere(target, 0.15f);
        }
#endif
    }
}