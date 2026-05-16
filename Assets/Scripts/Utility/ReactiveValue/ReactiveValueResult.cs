namespace BC.Base
{
    public readonly struct ReactiveError
    {
        public readonly ReactiveErrorCode Code;
        public readonly string Message;
        public readonly EntityRef Actor;
        public readonly EntityRef Trigger;

        public ReactiveError(
            ReactiveErrorCode code,
            string message,
            EntityRef actor,
            EntityRef trigger)
        {
            Code = code;
            Message = message;
            Actor = actor;
            Trigger = trigger;
        }
    }

    public readonly struct ReactiveResult<T>
    {
        public readonly bool Success;
        public readonly T Value;
        public readonly ReactiveError Error;
        public readonly int Version;

        private ReactiveResult(bool success, T value, ReactiveError error, int version)
        {
            Success = success;
            Value = value;
            Error = error;
            Version = version;
        }

        public bool Failed => !Success;

        public static ReactiveResult<T> Ok(T value, int version = 0)
        {
            return new ReactiveResult<T>(true, value, default, version);
        }

        public static ReactiveResult<T> Fail(ReactiveError error)
        {
            return new ReactiveResult<T>(false, default, error, 0);
        }
    }
}