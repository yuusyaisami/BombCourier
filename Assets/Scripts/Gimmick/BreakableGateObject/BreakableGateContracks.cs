using Unity.Cinemachine;
using UnityEngine;

namespace BC.Gimmick
{
    public class GoalData
    {
        public CinemachineCamera GoalCamera;
        public Transform GoalTransform;
        public Transform PlayerTargetPoint; // ゴール後Playerが移動する位置
    }
}