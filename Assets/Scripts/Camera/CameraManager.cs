using UnityEngine;
using BC.Utility;

namespace BC.Camera
{
    public enum CameraState
    {
        Follow,
        Free,
        Cinematic
    }
    public class CameraManager : MonoBehaviour
    {
        private StateMachine<CameraState> _stateMachine = new StateMachine<CameraState>();

        private void Start()
        {
            _stateMachine.ChangeState(CameraState.Follow);

            // Lock cursor for testing
            Cursor.lockState = CursorLockMode.Locked;
        }
        private void Update()
        {
            switch (_stateMachine.CurrentState)
            {
                case CameraState.Follow:
                    //HandleFollowCamera();
                    break;
                case CameraState.Free:
                    //HandleFreeCamera();
                    break;
                case CameraState.Cinematic:
                    //HandleCinematicCamera();
                    break;
            }
        }
    }

}