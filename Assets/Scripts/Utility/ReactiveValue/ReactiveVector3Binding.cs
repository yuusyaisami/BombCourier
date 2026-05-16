using UnityEngine;

namespace BC.Base
{
    public sealed class ReactiveVector3Binding : ReactiveBindingBase<Vector3>
    {
        private readonly ReactiveVector3 spec;
        private readonly ReactiveEvaluationMode evaluationMode;
        private readonly ReactiveFailurePolicy failurePolicy;

        public ReactiveVector3Binding(
            ReactiveValueResolverService resolver,
            in ReactiveEvalContext context,
            in ReactiveVector3 spec)
            : this(resolver, context, spec, spec.EvaluationMode, spec.FailurePolicy)
        {
        }

        public ReactiveVector3Binding(
            ReactiveValueResolverService resolver,
            in ReactiveEvalContext context,
            in ReactiveVector3 spec,
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

        protected override ReactiveResult<Vector3> ResolveCore()
        {
            return Resolver.ResolveVector3(Context, spec);
        }

        protected override bool TryGetFallbackValue(out Vector3 value)
        {
            value = spec.FallbackValue;
            return true;
        }
    }
}