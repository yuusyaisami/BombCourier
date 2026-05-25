using System;

namespace BC.Base
{
    public sealed class ReactiveWatchedStringBinding : ReactiveBindingBase<string>
    {
        private readonly ReactiveWatchedString spec;
        private ValueWatchHandle<string> watchedHandle;
        private ReactiveResult<string> watchedResult;
        private int watchedVersion;
        private bool hasWatchedResult;

        public ReactiveWatchedStringBinding(
            ReactiveValueResolverService resolver,
            in ReactiveEvalContext context,
            in ReactiveWatchedString spec)
            : base(resolver, context)
        {
            this.spec = spec;
        }

        protected override ReactiveEvaluationMode EvaluationMode => ReactiveEvaluationMode.Watched;

        protected override ReactiveFailurePolicy FailurePolicy => spec.FailurePolicy;

        public override bool IsDirty
        {
            get
            {
                if (!IsValid)
                    return false;

                if (watchedHandle == null || !hasWatchedResult)
                    return true;

                return watchedHandle.Version != watchedVersion;
            }
        }

        public override ReactiveResult<string> Read()
        {
            if (!IsValid)
                throw new ObjectDisposedException(GetType().Name);

            return ReadWatched();
        }

        protected override ReactiveResult<string> ResolveCore()
        {
            return Resolver.ResolveWatchedString(Context, spec);
        }

        protected override bool TryGetFallbackValue(out string value)
        {
            value = spec.FallbackValue;
            return true;
        }

        protected override void OnDispose()
        {
            watchedHandle = null;
            watchedResult = default;
            watchedVersion = 0;
            hasWatchedResult = false;
        }

        private ReactiveResult<string> ReadWatched()
        {
            ReactiveResult<ValueWatchHandle<string>> handleResult = EnsureWatchedHandle();

            if (!handleResult.Success)
            {
                DirtyState = true;
                return ApplyFailurePolicy(ReactiveResult<string>.Fail(handleResult.Error));
            }

            if (!hasWatchedResult || watchedHandle.Version != watchedVersion)
            {
                watchedVersion = watchedHandle.Version;
                watchedResult = ReactiveResult<string>.Ok(watchedHandle.CurrentValue, watchedVersion);
                hasWatchedResult = true;
            }

            DirtyState = false;
            return watchedResult;
        }

        private ReactiveResult<ValueWatchHandle<string>> EnsureWatchedHandle()
        {
            if (watchedHandle != null)
                return ReactiveResult<ValueWatchHandle<string>>.Ok(watchedHandle, watchedHandle.Version);

            ReactiveResult<ValueWatchHandle<string>> result = Resolver.ResolveWatchedStringHandle(Context, spec);

            if (result.Success)
                watchedHandle = result.Value;

            return result;
        }
    }
}
