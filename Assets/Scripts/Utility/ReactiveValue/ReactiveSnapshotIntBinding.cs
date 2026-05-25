namespace BC.Base
{
    public sealed class ReactiveSnapshotIntBinding : ReactiveBindingBase<int>
    {
        private readonly ReactiveSnapshotInt spec;

        public ReactiveSnapshotIntBinding(
            ReactiveValueResolverService resolver,
            in ReactiveEvalContext context,
            in ReactiveSnapshotInt spec)
            : base(resolver, context)
        {
            this.spec = spec;
        }

        protected override ReactiveEvaluationMode EvaluationMode => ReactiveEvaluationMode.Snapshot;

        protected override ReactiveFailurePolicy FailurePolicy => spec.FailurePolicy;

        protected override ReactiveResult<int> ResolveCore()
        {
            return Resolver.ResolveSnapshotInt(Context, spec);
        }

        protected override bool TryGetFallbackValue(out int value)
        {
            value = spec.FallbackValue;
            return true;
        }
    }
}
