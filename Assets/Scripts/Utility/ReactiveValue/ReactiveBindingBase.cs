using System;

namespace BC.Base
{
    public abstract class ReactiveBindingBase<TValue> : IReactiveBinding
    {
        private bool isDisposed;
        private bool isDirty = true;
        private bool hasCachedResult;
        private ReactiveResult<TValue> cachedResult;

        protected ReactiveBindingBase(ReactiveValueResolverService resolver, in ReactiveEvalContext context)
        {
            Resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            Context = context;
        }

        protected ReactiveValueResolverService Resolver { get; }
        protected ReactiveEvalContext Context { get; }

        public bool IsValid => !isDisposed;
        public virtual bool IsDirty => isDirty;

        protected bool DirtyState
        {
            get => isDirty;
            set => isDirty = value;
        }

        public virtual ReactiveResult<TValue> Read()
        {
            if (!IsValid)
                throw new ObjectDisposedException(GetType().Name);

            if (EvaluationMode == ReactiveEvaluationMode.Snapshot)
            {
                if (hasCachedResult)
                    return cachedResult;

                ReactiveResult<TValue> snapshotResult = ApplyFailurePolicy(ResolveCore());

                if (snapshotResult.Success)
                {
                    cachedResult = snapshotResult;
                    hasCachedResult = true;
                    DirtyState = false;
                }

                return snapshotResult;
            }

            ReactiveResult<TValue> result = ApplyFailurePolicy(ResolveCore());
            DirtyState = EvaluationMode == ReactiveEvaluationMode.Continuous || !result.Success;
            return result;
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            DirtyState = false;
            OnDispose();
        }

        protected abstract ReactiveEvaluationMode EvaluationMode { get; }

        protected abstract ReactiveFailurePolicy FailurePolicy { get; }

        protected abstract ReactiveResult<TValue> ResolveCore();

        protected virtual bool TryGetFallbackValue(out TValue value)
        {
            value = default;
            return false;
        }

        protected virtual void OnDispose()
        {
        }

        protected ReactiveResult<TValue> ApplyFailurePolicy(ReactiveResult<TValue> result)
        {
            if (result.Success || FailurePolicy != ReactiveFailurePolicy.UseFallback)
                return result;

            return TryGetFallbackValue(out TValue fallbackValue)
                ? ReactiveResult<TValue>.Ok(fallbackValue, result.Version)
                : result;
        }
    }
}