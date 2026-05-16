using System;

namespace BC.Base
{
    public sealed class ReactiveFloatBinding : ReactiveBindingBase<float>
    {
        private readonly ReactiveFloat spec;
        private readonly ReactiveEvaluationMode evaluationMode;
        private readonly ReactiveFailurePolicy failurePolicy;
        private ValueWatchHandle<float> watchedHandle;
        private ReactiveResult<float> watchedResult;
        private int watchedVersion;
        private bool hasWatchedResult;

        public ReactiveFloatBinding(
            ReactiveValueResolverService resolver,
            in ReactiveEvalContext context,
            in ReactiveFloat spec)
            : this(resolver, context, spec, spec.EvaluationMode, spec.FailurePolicy)
        {
        }

        public ReactiveFloatBinding(
            ReactiveValueResolverService resolver,
            in ReactiveEvalContext context,
            in ReactiveFloat spec,
            ReactiveEvaluationMode evaluationMode,
            ReactiveFailurePolicy failurePolicy)
            : base(resolver, context)
        {
            this.spec = spec;
            this.evaluationMode = evaluationMode;
            this.failurePolicy = failurePolicy;
        }

        protected override ReactiveEvaluationMode EvaluationMode => evaluationMode;

        protected override ReactiveFailurePolicy FailurePolicy => failurePolicy;

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

        public override ReactiveResult<float> Read()
        {
            if (EvaluationMode != ReactiveEvaluationMode.Watched)
                return base.Read();

            if (!IsValid)
                throw new ObjectDisposedException(GetType().Name);

            return ReadWatched();
        }

        protected override ReactiveResult<float> ResolveCore()
        {
            return Resolver.ResolveFloat(Context, spec);
        }

        protected override bool TryGetFallbackValue(out float value)
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

        private ReactiveResult<float> ReadWatched()
        {
            ReactiveResult<ValueWatchHandle<float>> handleResult = EnsureWatchedHandle();

            if (!handleResult.Success)
            {
                DirtyState = true;
                return ApplyFailurePolicy(ReactiveResult<float>.Fail(handleResult.Error));
            }

            // Watched bindings only adopt a new value when the source version advances.
            if (!hasWatchedResult || watchedHandle.Version != watchedVersion)
            {
                watchedVersion = watchedHandle.Version;
                watchedResult = ReactiveResult<float>.Ok(watchedHandle.CurrentValue, watchedVersion);
                hasWatchedResult = true;
            }

            DirtyState = false;
            return watchedResult;
        }

        private ReactiveResult<ValueWatchHandle<float>> EnsureWatchedHandle()
        {
            if (watchedHandle != null)
                return ReactiveResult<ValueWatchHandle<float>>.Ok(watchedHandle, watchedHandle.Version);

            ReactiveResult<ValueWatchHandle<float>> result = Resolver.ResolveFloatWatch(Context, spec);

            if (result.Success)
                watchedHandle = result.Value;

            return result;
        }
    }
}