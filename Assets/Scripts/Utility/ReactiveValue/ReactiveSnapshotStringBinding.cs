namespace BC.Base
{
    public sealed class ReactiveSnapshotStringBinding : ReactiveBindingBase<string>
    {
        private readonly ReactiveSnapshotString spec;

        public ReactiveSnapshotStringBinding(
            ReactiveValueResolverService resolver,
            in ReactiveEvalContext context,
            in ReactiveSnapshotString spec)
            : base(resolver, context)
        {
            this.spec = spec;
        }

        protected override ReactiveEvaluationMode EvaluationMode => ReactiveEvaluationMode.Snapshot;

        protected override ReactiveFailurePolicy FailurePolicy => spec.FailurePolicy;

        protected override ReactiveResult<string> ResolveCore()
        {
            return Resolver.ResolveSnapshotString(Context, spec);
        }

        protected override bool TryGetFallbackValue(out string value)
        {
            value = spec.FallbackValue;
            return true;
        }
    }
}
