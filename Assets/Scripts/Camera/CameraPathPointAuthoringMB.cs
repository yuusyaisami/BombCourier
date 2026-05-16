using BC.ActionSystem;
using UnityEngine;

namespace BC.Camera
{
    public class CameraPathPointAuthoringMB : MonoBehaviour, ICameraPathPointSource
    {
        [SerializeField] private string label;
        [SerializeField, Min(0.0f)] private float holdSeconds;
        [SerializeField] private CameraPathTransitionSettings transitionFromPrevious;
        [SerializeField] private CameraPathLensSettings lens;
        [SerializeField] private InlineAction onArriveAction;

        public bool TryBuildPoint(out CameraPathPointDefinition point)
        {
            point = new CameraPathPointDefinition(
                label,
                transform.position,
                transform.rotation,
                holdSeconds,
                transitionFromPrevious,
                lens,
                onArriveAction);

            return true;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(transform.position, 0.22f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.25f);
        }
#endif
    }
}