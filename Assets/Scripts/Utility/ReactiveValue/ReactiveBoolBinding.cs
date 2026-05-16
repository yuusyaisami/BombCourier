using System;

namespace BC.Base
{
    public sealed class ReactiveBoolBinding : ReactiveBindingBase<bool>
    {
        private readonly ReactiveBool spec;
        private ValueWatchHandle<bool> watchedHandle;
        private ReactiveResult<bool> watchedResult;
        private int watchedVersion;
        private bool hasWatchedResult;

        public ReactiveBoolBinding(
            ReactiveValueResolverService resolver,
            in ReactiveEvalContext context,
            in ReactiveBool spec)
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

        public override ReactiveResult<bool> Read()
        {
            if (EvaluationMode != ReactiveEvaluationMode.Watched)
                return base.Read();

            if (!IsValid)
                throw new ObjectDisposedException(GetType().Name);

            return ReadWatched();
        }

        protected override ReactiveResult<bool> ResolveCore()
        {
            return Resolver.ResolveBool(Context, spec);
        }

        protected override bool TryGetFallbackValue(out bool value)
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

        private ReactiveResult<bool> ReadWatched()
        {
            ReactiveResult<ValueWatchHandle<bool>> handleResult = EnsureWatchedHandle();

            if (!handleResult.Success)
            {
                DirtyState = true;
                return ApplyFailurePolicy(ReactiveResult<bool>.Fail(handleResult.Error));
            }

            if (!hasWatchedResult || watchedHandle.Version != watchedVersion)
            {
                watchedVersion = watchedHandle.Version;
                watchedResult = ReactiveResult<bool>.Ok(watchedHandle.CurrentValue, watchedVersion);
                hasWatchedResult = true;
            }

            DirtyState = false;
            return watchedResult;
        }

        private ReactiveResult<ValueWatchHandle<bool>> EnsureWatchedHandle()
        {
            if (watchedHandle != null)
                return ReactiveResult<ValueWatchHandle<bool>>.Ok(watchedHandle, watchedHandle.Version);

            ReactiveResult<ValueWatchHandle<bool>> result = Resolver.ResolveBoolWatch(Context, spec);

            if (result.Success)
                watchedHandle = result.Value;

            return result;
        }
    }
}