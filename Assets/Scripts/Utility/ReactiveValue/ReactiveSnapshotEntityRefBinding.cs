namespace BC.Base
{
    public sealed class ReactiveSnapshotEntityRefBinding : ReactiveBindingBase<EntityRef>
    {
        private readonly ReactiveSnapshotEntityRef spec;

        public ReactiveSnapshotEntityRefBinding(
            ReactiveValueResolverService resolver,
            in ReactiveEvalContext context,
            in ReactiveSnapshotEntityRef spec)
            : base(resolver, context)
        {
            this.spec = spec;
        }

        protected override ReactiveEvaluationMode EvaluationMode => ReactiveEvaluationMode.Snapshot;

        protected override ReactiveFailurePolicy FailurePolicy => spec.FailurePolicy;

        protected override ReactiveResult<EntityRef> ResolveCore()
        {
            return Resolver.ResolveSnapshotEntityRef(Context, spec);
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
    }
}
