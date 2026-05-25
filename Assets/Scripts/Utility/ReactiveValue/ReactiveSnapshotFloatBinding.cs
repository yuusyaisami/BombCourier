namespace BC.Base
{
    public sealed class ReactiveSnapshotFloatBinding : ReactiveBindingBase<float>
    {
        private readonly ReactiveSnapshotFloat spec;

        public ReactiveSnapshotFloatBinding(
            ReactiveValueResolverService resolver,
            in ReactiveEvalContext context,
            in ReactiveSnapshotFloat spec)
            : base(resolver, context)
        {
            this.spec = spec;
        }

        protected override ReactiveEvaluationMode EvaluationMode => ReactiveEvaluationMode.Snapshot;

        protected override ReactiveFailurePolicy FailurePolicy => spec.FailurePolicy;

        protected override ReactiveResult<float> ResolveCore()
        {
            return Resolver.ResolveSnapshotFloat(Context, spec);
        }

        protected override bool TryGetFallbackValue(out float value)
        {
            value = spec.FallbackValue;
            return true;
        }
    }
}
