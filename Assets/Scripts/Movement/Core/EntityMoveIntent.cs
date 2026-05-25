using UnityEngine;

namespace BC.Base
{
    public struct EntityMoveIntent
    {
        public Vector3 WorldMoveDirection;
        public bool HasMoveInput;
        public bool SprintHeld;
        public bool JumpPressed;
        public bool JumpHeld;
        public bool IsAutoMove;

        public void Clear()
        {
            WorldMoveDirection = Vector3.zero;
            HasMoveInput = false;
            SprintHeld = false;
            JumpPressed = false;
            JumpHeld = false;
            IsAutoMove = false;
        }
    }
}