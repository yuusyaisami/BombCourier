namespace BC.Base
{
    public sealed class ReactiveSnapshotBoolBinding : ReactiveBindingBase<bool>
    {
        private readonly ReactiveSnapshotBool spec;

        public ReactiveSnapshotBoolBinding(
            ReactiveValueResolverService resolver,
            in ReactiveEvalContext context,
            in ReactiveSnapshotBool spec)
            : base(resolver, context)
        {
            this.spec = spec;
        }

        protected override ReactiveEvaluationMode EvaluationMode => ReactiveEvaluationMode.Snapshot;

        protected override ReactiveFailurePolicy FailurePolicy => spec.FailurePolicy;

        protected override ReactiveResult<bool> ResolveCore()
        {
            return Resolver.ResolveSnapshotBool(Context, spec);
        }

        protected override bool TryGetFallbackValue(out bool value)
        {
            value = spec.FallbackValue;
            return true;
        }
    }
}
