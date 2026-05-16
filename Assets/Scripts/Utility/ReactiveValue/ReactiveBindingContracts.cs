using System;

namespace BC.Base
{
    public interface IReactiveBinding : IDisposable
    {
        bool IsValid { get; }
        bool IsDirty { get; }
    }

    internal static class ReactiveErrorUtility
    {
        public static ReactiveError Create(
            ReactiveErrorCode code,
            string message,
            in ReactiveEvalContext context)
        {
            return new ReactiveError(code, message ?? string.Empty, context.ActorEntity, context.TriggerEntity);
        }

        public static ReactiveResult<T> Fail<T>(
            ReactiveErrorCode code,
            string message,
            in ReactiveEvalContext context)
        {
            return ReactiveResult<T>.Fail(Create(code, message, context));
        }
    }
}