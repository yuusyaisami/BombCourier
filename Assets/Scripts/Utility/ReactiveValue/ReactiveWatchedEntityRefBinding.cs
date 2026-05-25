using System;

namespace BC.Base
{
    public sealed class ReactiveWatchedEntityRefBinding : ReactiveBindingBase<EntityRef>
    {
        private readonly ReactiveWatchedEntityRef spec;
        private ValueWatchHandle<EntityRef> watchedHandle;
        private ReactiveResult<EntityRef> watchedResult;
        private int watchedVersion;
        private bool hasWatchedResult;

        public ReactiveWatchedEntityRefBinding(
            ReactiveValueResolverService resolver,
            in ReactiveEvalContext context,
            in ReactiveWatchedEntityRef spec)
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

        public override ReactiveResult<EntityRef> Read()
        {
            if (!IsValid)
                throw new ObjectDisposedException(GetType().Name);

            return ReadWatched();
        }

        protected override ReactiveResult<EntityRef> ResolveCore()
        {
            return Resolver.ResolveWatchedEntityRef(Context, spec);
        }

        protected override bool TryGetFallbackValue(out EntityRef value)
        {
            switch (spec.FallbackKind)
            {
                case ReactiveEntityFallbackKind.Self:
                    value = Context.ActorEntity;
                    return value.IsValid;

                case ReactiveEntityFallbackKind.TriggerEntity:
                    value = Context.TriggerEntity;
                    return value.IsValid;

                default:
                    value = default;
                    return false;
            }
        }

        protected override void OnDispose()
        {
            watchedHandle = null;
            watchedResult = default;
            watchedVersion = 0;
            hasWatchedResult = false;
        }

        private ReactiveResult<EntityRef> ReadWatched()
        {
            ReactiveResult<ValueWatchHandle<EntityRef>> handleResult = EnsureWatchedHandle();

            if (!handleResult.Success)
            {
                DirtyState = true;
                return ApplyFailurePolicy(ReactiveResult<EntityRef>.Fail(handleResult.Error));
            }

            if (!hasWatchedResult || watchedHandle.Version != watchedVersion)
            {
                watchedVersion = watchedHandle.Version;
                watchedResult = ReactiveResult<EntityRef>.Ok(watchedHandle.CurrentValue, watchedVersion);
                hasWatchedResult = true;
            }

            DirtyState = false;
            return watchedResult;
        }

        private ReactiveResult<ValueWatchHandle<EntityRef>> EnsureWatchedHandle()
        {
            if (watchedHandle != null)
                return ReactiveResult<ValueWatchHandle<EntityRef>>.Ok(watchedHandle, watchedHandle.Version);

            ReactiveResult<ValueWatchHandle<EntityRef>> result = Resolver.ResolveWatchedEntityRefHandle(Context, spec);

            if (result.Success)
                watchedHandle = result.Value;

            return result;
        }
    }
}
