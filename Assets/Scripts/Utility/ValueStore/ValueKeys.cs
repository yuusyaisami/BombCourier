namespace BC.Base
{
    public enum FaceExpressionId
    {
        Neutral = 0,
        Happy = 1,
        Angry = 2,
        Hurt = 3,
        Dead = 4,
        Surprised = 5,
        CannotMove = 6,
        Falling = 7,
        Running = 8,
        Narrow = 9,
    }

    public enum ShapeExpressionId
    {
        Neutral = 10,
        Happy = 20,
        Open = 30,
        Close = 40,
        Talk = 50,
        Mu = 60,
    }

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
            public static readonly ValueKey<bool> CanMoveByInput =
                new ValueKey<bool>(
                    new ValueKeyId(2001),
                    "Move.CanMoveByInput",
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
            public static readonly ValueKey<float> JumpHeightMultiplier =
                new ValueKey<float>(
                    new ValueKeyId(2004),
                    "Move.JumpHeightMultiplier",
                    1.0f,
                    ValueCompositionMode.NumericAddMul
                );

            public static readonly ValueKey<bool> CanMoveBySystem =
                new ValueKey<bool>(
                    new ValueKeyId(2005),
                    "Move.CanMoveBySystem",
                    true,
                    ValueCompositionMode.BoolAnd
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

        public static class Interaction
        {
            public static readonly ValueKey<bool> CanInteract =
                new ValueKey<bool>(
                    new ValueKeyId(5001),
                    "Interaction.CanInteract",
                    true,
                    ValueCompositionMode.BoolAnd
                );
        }

        public static class Camera
        {
            public static readonly ValueKey<bool> CanLookByInput =
                new ValueKey<bool>(
                    new ValueKeyId(6001),
                    "Camera.CanLookByInput",
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
            public static readonly ValueKey<bool> IsThrowPoseActive =
                new ValueKey<bool>(
                    new ValueKeyId(8008),
                    "Runtime.IsThrowPoseActive",
                    false,
                    ValueCompositionMode.BoolOr
                );
            public static readonly ValueKey<bool> IsItemThrowAiming =
                new ValueKey<bool>(
                    new ValueKeyId(8009),
                    "Runtime.IsItemThrowAiming",
                    false,
                    ValueCompositionMode.BoolOr
                );
            public static readonly ValueKey<FaceExpressionId> FaceExpression =
                new ValueKey<FaceExpressionId>(
                    new ValueKeyId(8010),
                    "Runtime.FaceExpression",
                    FaceExpressionId.Neutral,
                    ValueCompositionMode.Raw
                );
            public static readonly ValueKey<int> ThrowSequence =
                new ValueKey<int>(
                    new ValueKeyId(8011),
                    "Runtime.ThrowSequence",
                    0,
                    ValueCompositionMode.Raw
                );
            public static readonly ValueKey<bool> IsFatigueInteracting =
                new ValueKey<bool>(
                    new ValueKeyId(8012),
                    "Runtime.IsFatigueInteracting",
                    false,
                    ValueCompositionMode.BoolOr
                );
            public static readonly ValueKey<bool> CanMoveByInput =
                new ValueKey<bool>(
                    new ValueKeyId(8013),
                    "Runtime.CanMoveByInput",
                    true,
                    ValueCompositionMode.BoolAnd
                );
            public static readonly ValueKey<bool> CanMoveBySystem =
                new ValueKey<bool>(
                    new ValueKeyId(8014),
                    "Runtime.CanMoveBySystem",
                    true,
                    ValueCompositionMode.BoolAnd
                );

            public static readonly ValueKey<EntityRef> FocusEntity =
                new ValueKey<EntityRef>(
                    new ValueKeyId(8015),
                    "Runtime.FocusEntity",
                    default,
                    ValueCompositionMode.Raw
                );
            public static readonly ValueKey<bool> CanInteract =
                new ValueKey<bool>(
                    new ValueKeyId(8016),
                    "Runtime.CanInteract",
                    true,
                    ValueCompositionMode.BoolAnd
                );

            public static readonly ValueKey<bool> CanLookByInput =
                new ValueKey<bool>(
                    new ValueKeyId(8017),
                    "Runtime.CanLookByInput",
                    true,
                    ValueCompositionMode.BoolAnd
                );

            public static readonly ValueKey<bool> IsTalking =
                new ValueKey<bool>(
                    new ValueKeyId(8018),
                    "Runtime.IsTalking",
                    false,
                    ValueCompositionMode.BoolOr
                );

            public static readonly ValueKey<ShapeExpressionId> ShapeExpression =
                new ValueKey<ShapeExpressionId>(
                    new ValueKeyId(8019),
                    "Runtime.ShapeExpression",
                    ShapeExpressionId.Neutral,
                    ValueCompositionMode.Raw
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

        public static class Kernel
        {
            // Kernel系はScene全体の状態を表す。ValueKeyIdは10000番台以降を使う。
            // AuthoringではValueKeyDropdown(pathPrefix: "Kernel")でEntity系キーと分ける。
            public static class Gimmick
            {
                public static readonly ValueKey<bool> GlobalEnabled =
                    new ValueKey<bool>(
                        new ValueKeyId(10001),
                        "Kernel.Gimmick.GlobalEnabled",
                        true,
                        ValueCompositionMode.BoolAnd
                    );

                public static readonly ValueKey<bool> AnySignalActive =
                    new ValueKey<bool>(
                        new ValueKeyId(10002),
                        "Kernel.Gimmick.AnySignalActive",
                        false,
                        ValueCompositionMode.BoolOr
                    );

                public static readonly ValueKey<int> SequenceIndex =
                    new ValueKey<int>(
                        new ValueKeyId(10003),
                        "Kernel.Gimmick.SequenceIndex",
                        0,
                        ValueCompositionMode.NumericAddMul
                    );

                public static readonly ValueKey<float> LastSignalTime =
                    new ValueKey<float>(
                        new ValueKeyId(10004),
                        "Kernel.Gimmick.LastSignalTime",
                        0.0f,
                        ValueCompositionMode.NumericAddMul
                    );
            }

            // 評価 (今回のプレイの評価を行います。)
            public static class Evaluation
            {
                // 1点 : ゴール, 2点 : ゴール+ボーナスアイテム, 3点 : ゴール+特殊アイテム+早いクリア
                public static readonly ValueKey<bool> IsBonusItem =
                    new ValueKey<bool>(
                        new ValueKeyId(11001),
                        "Kernel.Evaluation.IsBonusItem",
                        false,
                        ValueCompositionMode.BoolOr
                    );
                // 早いクリア
                public static readonly ValueKey<bool> IsFastClear =
                    new ValueKey<bool>(
                        new ValueKeyId(11002),
                        "Kernel.Evaluation.IsFastClear",
                        false,
                        ValueCompositionMode.BoolOr
                    );

                public static readonly ValueKey<float> CountdownTime =
                    new ValueKey<float>(
                        new ValueKeyId(11003),
                        "Kernel.Evaluation.CountdownTime",
                        0.0f,
                        ValueCompositionMode.NumericAddMul
                    );
                public static readonly ValueKey<float> FastClearThreshold =
                    new ValueKey<float>(
                        new ValueKeyId(11004),
                        "Kernel.Evaluation.FastClearThreshold",
                        60.0f,
                        ValueCompositionMode.NumericAddMul
                    );

            }

            public static class Stage
            {
                public static readonly ValueKey<int> SelectedStageIndex =
                    new ValueKey<int>(
                        new ValueKeyId(11005),
                        "Kernel.Stage.SelectedStageIndex",
                        0,
                        ValueCompositionMode.Raw
                    );
            }
        }

        public static class Local
        {
            // ActionExecution 内だけで使う一時キー。ValueKeyRegistry を通して型安全に扱う。
            public static class Choice
            {
                public static readonly ValueKey<bool> HasSelection =
                    new ValueKey<bool>(
                        new ValueKeyId(12001),
                        "Local.Choice.HasSelection",
                        false,
                        ValueCompositionMode.BoolOr
                    );

                public static readonly ValueKey<int> SelectedIndex =
                    new ValueKey<int>(
                        new ValueKeyId(12002),
                        "Local.Choice.SelectedIndex",
                        -1,
                        ValueCompositionMode.Raw
                    );

                public static readonly ValueKey<string> SelectedText =
                    new ValueKey<string>(
                        new ValueKeyId(12003),
                        "Local.Choice.SelectedText",
                        "",
                        ValueCompositionMode.Raw
                    );
            }

            public static class Flags
            {
                public static readonly ValueKey<bool> Flag0 =
                    new ValueKey<bool>(
                        new ValueKeyId(12004),
                        "Local.Flags.Flag0",
                        false,
                        ValueCompositionMode.BoolOr
                    );

                public static readonly ValueKey<bool> Flag1 =
                    new ValueKey<bool>(
                        new ValueKeyId(12005),
                        "Local.Flags.Flag1",
                        false,
                        ValueCompositionMode.BoolOr
                    );
            }

            public static class Values
            {
                public static readonly ValueKey<int> Int0 =
                    new ValueKey<int>(
                        new ValueKeyId(12006),
                        "Local.Values.Int0",
                        0,
                        ValueCompositionMode.Raw
                    );

                public static readonly ValueKey<int> Int1 =
                    new ValueKey<int>(
                        new ValueKeyId(12007),
                        "Local.Values.Int1",
                        0,
                        ValueCompositionMode.Raw
                    );

                public static readonly ValueKey<float> Float0 =
                    new ValueKey<float>(
                        new ValueKeyId(12008),
                        "Local.Values.Float0",
                        0f,
                        ValueCompositionMode.NumericAddMul
                    );

                public static readonly ValueKey<float> Float1 =
                    new ValueKey<float>(
                        new ValueKeyId(12009),
                        "Local.Values.Float1",
                        0f,
                        ValueCompositionMode.NumericAddMul
                    );

                public static readonly ValueKey<string> String0 =
                    new ValueKey<string>(
                        new ValueKeyId(12010),
                        "Local.Values.String0",
                        "",
                        ValueCompositionMode.Raw
                    );

                public static readonly ValueKey<string> String1 =
                    new ValueKey<string>(
                        new ValueKeyId(12011),
                        "Local.Values.String1",
                        "",
                        ValueCompositionMode.Raw
                    );

                public static readonly ValueKey<EntityRef> Entity0 =
                    new ValueKey<EntityRef>(
                        new ValueKeyId(12012),
                        "Local.Values.Entity0",
                        default,
                        ValueCompositionMode.Raw
                    );

                public static readonly ValueKey<FaceExpressionId> FaceExpression0 =
                    new ValueKey<FaceExpressionId>(
                        new ValueKeyId(12013),
                        "Local.Values.FaceExpression0",
                        FaceExpressionId.Neutral,
                        ValueCompositionMode.Raw
                    );

                public static readonly ValueKey<EntityMoveState> MoveState0 =
                    new ValueKey<EntityMoveState>(
                        new ValueKeyId(12014),
                        "Local.Values.MoveState0",
                        EntityMoveState.Idle,
                        ValueCompositionMode.Raw
                    );

                public static readonly ValueKey<ShapeExpressionId> ShapeExpression0 =
                    new ValueKey<ShapeExpressionId>(
                        new ValueKeyId(12015),
                        "Local.Values.ShapeExpression0",
                        ShapeExpressionId.Neutral,
                        ValueCompositionMode.Raw
                    );
            }
        }
        public static class GameLogic
        {
            public static class Talk
            {
                // 会話の回数 (会話開始時に1増える)
                public static readonly ValueKey<int> TalkCount =
                    new ValueKey<int>(
                        new ValueKeyId(90001),
                        "GameLogic.TalkCount",
                        0,
                        ValueCompositionMode.Raw
                    );
            }
            public static class Interaction
            {
                public static readonly ValueKey<bool> IsStateRed =
                    new ValueKey<bool>(
                        new ValueKeyId(90002),
                        "GameLogic.IsStateRed",
                        false,
                        ValueCompositionMode.BoolOr
                    );
                public static readonly ValueKey<bool> IsStateBlue =
                    new ValueKey<bool>(
                        new ValueKeyId(90003),
                        "GameLogic.IsStateBlue",
                        false,
                        ValueCompositionMode.BoolOr
                    );
                public static readonly ValueKey<bool> IsStateGreen =
                    new ValueKey<bool>(
                        new ValueKeyId(90004),
                        "GameLogic.IsStateGreen",
                        false,
                        ValueCompositionMode.BoolOr
                    );
                public static readonly ValueKey<bool> IsStateYellow =
                    new ValueKey<bool>(
                        new ValueKeyId(90005),
                        "GameLogic.IsStateYellow",
                        false,
                        ValueCompositionMode.BoolOr
                    );
            }
        }

        // アプリケーション全体の設定値。ApplicationKernel.KernelValueStore に格納する。
        // UISettingMB から書き込み、AudioSystem / CameraController 等が Read する一方向バス。
        public static class AppSettings
        {
            public static readonly ValueKey<float> MusicVolume =
                new ValueKey<float>(
                    new ValueKeyId(20001),
                    "AppSettings.MusicVolume",
                    0.8f,
                    ValueCompositionMode.Raw
                );

            public static readonly ValueKey<float> SFXVolume =
                new ValueKey<float>(
                    new ValueKeyId(20002),
                    "AppSettings.SFXVolume",
                    0.8f,
                    ValueCompositionMode.Raw
                );

            public static readonly ValueKey<float> CameraSensitivity =
                new ValueKey<float>(
                    new ValueKeyId(20003),
                    "AppSettings.CameraSensitivity",
                    0.08f,
                    ValueCompositionMode.Raw
                );

            public static readonly ValueKey<bool> InvertYAxis =
                new ValueKey<bool>(
                    new ValueKeyId(20004),
                    "AppSettings.InvertYAxis",
                    false,
                    ValueCompositionMode.Raw
                );
        }
    }
}
