using System;

namespace BC.Base
{
    public sealed class ReactiveEntityRefBinding : ReactiveBindingBase<EntityRef>
    {
        private readonly ReactiveEntityRef spec;
        private ValueWatchHandle<EntityRef> watchedHandle;
        private ReactiveResult<EntityRef> watchedResult;
        private int watchedVersion;
        private bool hasWatchedResult;

        public ReactiveEntityRefBinding(
            ReactiveValueResolverService resolver,
            in ReactiveEvalContext context,
            in ReactiveEntityRef spec)
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

        public override ReactiveResult<EntityRef> Read()
        {
            if (EvaluationMode != ReactiveEvaluationMode.Watched)
                return base.Read();

            if (!IsValid)
                throw new ObjectDisposedException(GetType().Name);

            return ReadWatched();
        }

        protected override ReactiveResult<EntityRef> ResolveCore()
        {
            return Resolver.ResolveEntity(Context, spec);
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

            ReactiveResult<ValueWatchHandle<EntityRef>> result = Resolver.ResolveEntityWatch(Context, spec);

            if (result.Success)
                watchedHandle = result.Value;

            return result;
        }
    }
}