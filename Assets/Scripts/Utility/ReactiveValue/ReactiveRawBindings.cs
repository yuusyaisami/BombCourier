using System;

namespace BC.Base
{
    public sealed class ReactiveStringBinding : ReactiveBindingBase<string>
    {
        private readonly ReactiveString spec;
        private readonly ReactiveEvaluationMode evaluationMode;
        private readonly ReactiveFailurePolicy failurePolicy;
        private ValueWatchHandle<string> watchedHandle;
        private ReactiveResult<string> watchedResult;
        private int watchedVersion;
        private bool hasWatchedResult;

        public ReactiveStringBinding(
            ReactiveValueResolverService resolver,
            in ReactiveEvalContext context,
            in ReactiveString spec)
            : this(resolver, context, spec, spec.EvaluationMode, spec.FailurePolicy)
        {
        }

        public ReactiveStringBinding(
            ReactiveValueResolverService resolver,
            in ReactiveEvalContext context,
            in ReactiveString spec,
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

        public override ReactiveResult<string> Read()
        {
            if (EvaluationMode != ReactiveEvaluationMode.Watched)
                return base.Read();

            if (!IsValid)
                throw new ObjectDisposedException(GetType().Name);

            return ReadWatched();
        }

        protected override ReactiveResult<string> ResolveCore()
        {
            return Resolver.ResolveString(Context, spec);
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

            ReactiveResult<ValueWatchHandle<string>> result = Resolver.ResolveStringWatch(Context, spec);

            if (result.Success)
                watchedHandle = result.Value;

            return result;
        }
    }

    public sealed class ReactiveFaceExpressionIdBinding : ReactiveBindingBase<FaceExpressionId>
    {
        private readonly ReactiveFaceExpressionId spec;
        private readonly ReactiveEvaluationMode evaluationMode;
        private readonly ReactiveFailurePolicy failurePolicy;
        private ValueWatchHandle<FaceExpressionId> watchedHandle;
        private ReactiveResult<FaceExpressionId> watchedResult;
        private int watchedVersion;
        private bool hasWatchedResult;

        public ReactiveFaceExpressionIdBinding(
            ReactiveValueResolverService resolver,
            in ReactiveEvalContext context,
            in ReactiveFaceExpressionId spec)
            : this(resolver, context, spec, spec.EvaluationMode, spec.FailurePolicy)
        {
        }

        public ReactiveFaceExpressionIdBinding(
            ReactiveValueResolverService resolver,
            in ReactiveEvalContext context,
            in ReactiveFaceExpressionId spec,
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

        public override ReactiveResult<FaceExpressionId> Read()
        {
            if (EvaluationMode != ReactiveEvaluationMode.Watched)
                return base.Read();

            if (!IsValid)
                throw new ObjectDisposedException(GetType().Name);

            return ReadWatched();
        }

        protected override ReactiveResult<FaceExpressionId> ResolveCore()
        {
            return Resolver.ResolveFaceExpressionId(Context, spec);
        }

        protected override bool TryGetFallbackValue(out FaceExpressionId value)
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

        private ReactiveResult<FaceExpressionId> ReadWatched()
        {
            ReactiveResult<ValueWatchHandle<FaceExpressionId>> handleResult = EnsureWatchedHandle();

            if (!handleResult.Success)
            {
                DirtyState = true;
                return ApplyFailurePolicy(ReactiveResult<FaceExpressionId>.Fail(handleResult.Error));
            }

            if (!hasWatchedResult || watchedHandle.Version != watchedVersion)
            {
                watchedVersion = watchedHandle.Version;
                watchedResult = ReactiveResult<FaceExpressionId>.Ok(watchedHandle.CurrentValue, watchedVersion);
                hasWatchedResult = true;
            }

            DirtyState = false;
            return watchedResult;
        }

        private ReactiveResult<ValueWatchHandle<FaceExpressionId>> EnsureWatchedHandle()
        {
            if (watchedHandle != null)
                return ReactiveResult<ValueWatchHandle<FaceExpressionId>>.Ok(watchedHandle, watchedHandle.Version);

            ReactiveResult<ValueWatchHandle<FaceExpressionId>> result = Resolver.ResolveFaceExpressionIdWatch(Context, spec);

            if (result.Success)
                watchedHandle = result.Value;

            return result;
        }
    }

    public sealed class ReactiveEntityMoveStateBinding : ReactiveBindingBase<EntityMoveState>
    {
        private readonly ReactiveEntityMoveState spec;
        private readonly ReactiveEvaluationMode evaluationMode;
        private readonly ReactiveFailurePolicy failurePolicy;
        private ValueWatchHandle<EntityMoveState> watchedHandle;
        private ReactiveResult<EntityMoveState> watchedResult;
        private int watchedVersion;
        private bool hasWatchedResult;

        public ReactiveEntityMoveStateBinding(
            ReactiveValueResolverService resolver,
            in ReactiveEvalContext context,
            in ReactiveEntityMoveState spec)
            : this(resolver, context, spec, spec.EvaluationMode, spec.FailurePolicy)
        {
        }

        public ReactiveEntityMoveStateBinding(
            ReactiveValueResolverService resolver,
            in ReactiveEvalContext context,
            in ReactiveEntityMoveState spec,
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

        public override ReactiveResult<EntityMoveState> Read()
        {
            if (EvaluationMode != ReactiveEvaluationMode.Watched)
                return base.Read();

            if (!IsValid)
                throw new ObjectDisposedException(GetType().Name);

            return ReadWatched();
        }

        protected override ReactiveResult<EntityMoveState> ResolveCore()
        {
            return Resolver.ResolveEntityMoveState(Context, spec);
        }

        protected override bool TryGetFallbackValue(out EntityMoveState value)
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

        private ReactiveResult<EntityMoveState> ReadWatched()
        {
            ReactiveResult<ValueWatchHandle<EntityMoveState>> handleResult = EnsureWatchedHandle();

            if (!handleResult.Success)
            {
                DirtyState = true;
                return ApplyFailurePolicy(ReactiveResult<EntityMoveState>.Fail(handleResult.Error));
            }

            if (!hasWatchedResult || watchedHandle.Version != watchedVersion)
            {
                watchedVersion = watchedHandle.Version;
                watchedResult = ReactiveResult<EntityMoveState>.Ok(watchedHandle.CurrentValue, watchedVersion);
                hasWatchedResult = true;
            }

            DirtyState = false;
            return watchedResult;
        }

        private ReactiveResult<ValueWatchHandle<EntityMoveState>> EnsureWatchedHandle()
        {
            if (watchedHandle != null)
                return ReactiveResult<ValueWatchHandle<EntityMoveState>>.Ok(watchedHandle, watchedHandle.Version);

            ReactiveResult<ValueWatchHandle<EntityMoveState>> result = Resolver.ResolveEntityMoveStateWatch(Context, spec);

            if (result.Success)
                watchedHandle = result.Value;

            return result;
        }
    }
}