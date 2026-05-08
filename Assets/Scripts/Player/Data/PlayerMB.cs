using BC.Utility;
using UnityEngine;
namespace BC.Base
{
    public enum PlayerState
    {
        Idle,
        Moving,
        Jumping,
        Attacking
    }
    public class PlayerMB : MonoBehaviour
    {
        StateMachine<PlayerState> stateMachine = new StateMachine<PlayerState>();
    }
}