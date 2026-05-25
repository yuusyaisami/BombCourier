using System;
using UnityEngine;

namespace BC.Base
{
    public static class CollisionConstraintSolver
    {
        public static void Resolve(
            MoveContactBuffer contactBuffer,
            EntityMoveRuntimeState runtimeState,
            float groundedStickVelocity,
            float currentTime,
            Action<Vector3> removeIntoWallVelocity)
        {
            if (contactBuffer == null || runtimeState == null)
                return;

            for (int i = 0; i < contactBuffer.Count; i++)
            {
                MoveContactInfo info = contactBuffer.Get(i);

                switch (info.Kind)
                {
                    case MoveContactKind.FootGround:
                    case MoveContactKind.FootEdge:
                        runtimeState.LastGroundedTime = currentTime;
                        if (runtimeState.VerticalVelocity < groundedStickVelocity)
                            runtimeState.VerticalVelocity = groundedStickVelocity;
                        break;

                    case MoveContactKind.BodyWall:
                    case MoveContactKind.SteepSlope:
                        removeIntoWallVelocity?.Invoke(info.Normal);
                        break;

                    case MoveContactKind.Ceiling:
                        if (runtimeState.VerticalVelocity > 0.0f)
                            runtimeState.VerticalVelocity = 0.0f;
                        break;
                }
            }
        }
    }
}
