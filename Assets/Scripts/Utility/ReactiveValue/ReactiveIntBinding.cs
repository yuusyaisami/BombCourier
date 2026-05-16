using System;

namespace BC.Base
{
    public sealed class ReactiveIntBinding : ReactiveBindingBase<int>
    {
        private readonly ReactiveInt spec;
        private ValueWatchHandle<int> watchedHandle;
        private ReactiveResult<int> watchedResult;
        private int watchedVersion;
        private bool hasWatchedResult;

        public ReactiveIntBinding(
            ReactiveValueResolverService resolver,
            in ReactiveEvalContext context,
            in ReactiveInt spec)
            : base(resolver, context)
        {
            this.spec = spec;
        }

        protected override ReactiveEvaluationMode EvaluationMode => spec.EvaluationMode;

        protected override ReactiveFailurePolicy FailurePolicy => spec.FailurePolicy;

        public override bool IsDirty
        {
            get
            {
                if (EvaluationMode != ReactiveEvaluationMode.Watched)
                    return base.IsDirty;

                if (!IsValid)
                    return false;

                if (watchedHandle == null || !hasWatchedResult)
                    return true;

                return watchedHandle.Version != watchedVersion;
            }
        }

        public override ReactiveResult<int> Read()
        {
            if (EvaluationMode != ReactiveEvaluationMode.Watched)
                return base.Read();

            if (!IsValid)
                throw new ObjectDisposedException(GetType().Name);

            return ReadWatched();
        }

        protected override ReactiveResult<int> ResolveCore()
        {
            return Resolver.ResolveInt(Context, spec);
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

            ReactiveResult<ValueWatchHandle<int>> result = Resolver.ResolveIntWatch(Context, spec);

            if (result.Success)
                watchedHandle = result.Value;

            return result;
        }
    }
}