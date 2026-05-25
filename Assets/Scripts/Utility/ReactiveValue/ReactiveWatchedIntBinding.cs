using System;

namespace BC.Base
{
    public sealed class ReactiveWatchedIntBinding : ReactiveBindingBase<int>
    {
        private readonly ReactiveWatchedInt spec;
        private ValueWatchHandle<int> watchedHandle;
        private ReactiveResult<int> watchedResult;
        private int watchedVersion;
        private bool hasWatchedResult;

        public ReactiveWatchedIntBinding(
            ReactiveValueResolverService resolver,
            in ReactiveEvalContext context,
            in ReactiveWatchedInt spec)
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

        public override ReactiveResult<int> Read()
        {
            if (!IsValid)
                throw new ObjectDisposedException(GetType().Name);

            return ReadWatched();
        }

        protected override ReactiveResult<int> ResolveCore()
        {
            return Resolver.ResolveWatchedInt(Context, spec);
        }

        protected override bool TryGetFallbackValue(out int value)
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

        private ReactiveResult<int> ReadWatched()
        {
            ReactiveResult<ValueWatchHandle<int>> handleResult = EnsureWatchedHandle();

            if (!handleResult.Success)
            {
                DirtyState = true;
                return ApplyFailurePolicy(ReactiveResult<int>.Fail(handleResult.Error));
            }

            if (!hasWatchedResult || watchedHandle.Version != watchedVersion)
            {
                watchedVersion = watchedHandle.Version;
                watchedResult = ReactiveResult<int>.Ok(watchedHandle.CurrentValue, watchedVersion);
                hasWatchedResult = true;
            }

            DirtyState = false;
            return watchedResult;
        }

        private ReactiveResult<ValueWatchHandle<int>> EnsureWatchedHandle()
        {
            if (watchedHandle != null)
                return ReactiveResult<ValueWatchHandle<int>>.Ok(watchedHandle, watchedHandle.Version);

            ReactiveResult<ValueWatchHandle<int>> result = Resolver.ResolveWatchedIntHandle(Context, spec);

            if (result.Success)
                watchedHandle = result.Value;

            return result;
        }
    }
}
