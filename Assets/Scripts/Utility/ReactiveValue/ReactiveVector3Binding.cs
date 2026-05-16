using UnityEngine;

namespace BC.Base
{
    public sealed class ReactiveVector3Binding : ReactiveBindingBase<Vector3>
    {
        private readonly ReactiveVector3 spec;

        public ReactiveVector3Binding(
            ReactiveValueResolverService resolver,
            in ReactiveEvalContext context,
            in ReactiveVector3 spec)
            : base(resolver, context)
        {
            this.spec = spec;
        }

        protected override ReactiveEvaluationMode EvaluationMode => spec.EvaluationMode;

        protected override ReactiveFailurePolicy FailurePolicy => spec.FailurePolicy;

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