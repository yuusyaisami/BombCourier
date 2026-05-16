using Unity.Cinemachine;
using UnityEngine;

namespace BC.Gimmick
{
    [System.Serializable]
    public class GoalData
    {
        [SerializeField] private CinemachineCamera goalCamera;
        [SerializeField] private Vector3 playerTargetPoint; // ゴール後Playerが移動する位置
        public Transform goalTransform; // GoalDataを持つオブジェクトのTransformキャッシュ
        public Vector3 Target => goalTransform != null ? goalTransform.position + playerTargetPoint : playerTargetPoint;
        public CinemachineCamera GoalCamera => goalCamera;
    }
}