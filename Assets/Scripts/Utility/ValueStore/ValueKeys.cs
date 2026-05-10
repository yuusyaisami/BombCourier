namespace BC.Base
{
    public static class ValueKeys
    {
        public static class Health
        {
            public static readonly ValueKey<int> CurrentHP =
                new ValueKey<int>(
                    new ValueKeyId(1001),
                    "Health.CurrentHP",
                    100,
                    ValueCompositionMode.Raw
                );

            public static readonly ValueKey<int> MaxHP =
                new ValueKey<int>(
                    new ValueKeyId(1002),
                    "Health.MaxHP",
                    100,
                    ValueCompositionMode.NumericAddMul
                );
        }

        public static class Move
        {
            public static readonly ValueKey<bool> CanMove =
                new ValueKey<bool>(
                    new ValueKeyId(2001),
                    "Move.CanMove",
                    true,
                    ValueCompositionMode.BoolAnd
                );

            public static readonly ValueKey<float> BaseSpeed =
                new ValueKey<float>(
                    new ValueKeyId(2002),
                    "Move.BaseSpeed",
                    5.0f,
                    ValueCompositionMode.NumericAddMul
                );
            public static readonly ValueKey<float> SprintMultiplier =
                new ValueKey<float>(
                    new ValueKeyId(2003),
                    "Move.SprintMultiplier",
                    1.5f,
                    ValueCompositionMode.NumericAddMul
                );
        }

        public static class Bomb
        {
            public static readonly ValueKey<float> FuseTime =
                new ValueKey<float>(
                    new ValueKeyId(3001),
                    "Bomb.FuseTime",
                    3.0f,
                    ValueCompositionMode.NumericAddMul
                );

            public static readonly ValueKey<int> Power =
                new ValueKey<int>(
                    new ValueKeyId(3002),
                    "Bomb.Power",
                    1,
                    ValueCompositionMode.NumericAddMul
                );
        }
        public static class Item
        {
            public static readonly ValueKey<bool> CanCarry =
                new ValueKey<bool>(
                    new ValueKeyId(4001),
                    "Item.CanCarry",
                    true,
                    ValueCompositionMode.BoolAnd
                );
        }
        public static class Runtime
        {
            public static readonly ValueKey<EntityMoveState> MoveState =
                new ValueKey<EntityMoveState>(
                    new ValueKeyId(8001),
                    "Runtime.MoveState",
                    EntityMoveState.Idle,
                    ValueCompositionMode.Raw
                );

            public static readonly ValueKey<float> CurrentPlanarSpeed =
                new ValueKey<float>(
                    new ValueKeyId(8002),
                    "Runtime.CurrentPlanarSpeed",
                    0f,
                    ValueCompositionMode.NumericAddMul
                );

            public static readonly ValueKey<float> VerticalVelocity =
                new ValueKey<float>(
                    new ValueKeyId(8003),
                    "Runtime.VerticalVelocity",
                    0f,
                    ValueCompositionMode.NumericAddMul
                );

            public static readonly ValueKey<bool> IsGrounded =
                new ValueKey<bool>(
                    new ValueKeyId(8004),
                    "Runtime.IsGrounded",
                    false,
                    ValueCompositionMode.BoolOr
                );

            public static readonly ValueKey<bool> IsSprinting =
                new ValueKey<bool>(
                    new ValueKeyId(8005),
                    "Runtime.IsSprinting",
                    false,
                    ValueCompositionMode.BoolOr
                );

            public static readonly ValueKey<bool> IsDead =
                new ValueKey<bool>(
                    new ValueKeyId(8006),
                    "Runtime.IsDead",
                    false,
                    ValueCompositionMode.BoolOr
                );
            public static readonly ValueKey<bool> IsHandlingItem =
                new ValueKey<bool>(
                    new ValueKeyId(8007),
                    "Runtime.IsHandlingItem",
                    false,
                    ValueCompositionMode.BoolOr
                );
        }

        public static class Identity
        {
            public static readonly ValueKey<string> DisplayName =
                new ValueKey<string>(
                    new ValueKeyId(9001),
                    "Identity.DisplayName",
                    "",
                    ValueCompositionMode.Raw
                );
        }
    }
}