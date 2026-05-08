namespace BC.Base
{
    public static class ValueKeys
    {
        public static class Health
        {
            public static readonly ValueKey<int> CurrentHP =
                new ValueKey<int>(new ValueKeyId(1001), "Health.CurrentHP", 100);

            public static readonly ValueKey<int> MaxHP =
                new ValueKey<int>(new ValueKeyId(1002), "Health.MaxHP", 100);
        }

        public static class Move
        {
            public static readonly ValueKey<float> Speed =
                new ValueKey<float>(new ValueKeyId(2001), "Move.Speed", 5.0f);
        }

        public static class Bomb
        {
            public static readonly ValueKey<float> FuseTime =
                new ValueKey<float>(new ValueKeyId(3001), "Bomb.FuseTime", 3.0f);

            public static readonly ValueKey<int> Power =
                new ValueKey<int>(new ValueKeyId(3002), "Bomb.Power", 1);
        }

        public static class Identity
        {
            public static readonly ValueKey<string> DisplayName =
                new ValueKey<string>(new ValueKeyId(9001), "Identity.DisplayName", "");
        }
    }
}