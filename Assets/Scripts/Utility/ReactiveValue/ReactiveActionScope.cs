using System;
using System.Collections.Generic;
using BC.ActionSystem;

namespace BC.Base
{
    public sealed class ReactiveActionScope : IDisposable
    {
        private readonly ReactiveValueResolverService resolver;
        private readonly SceneKernel sceneKernel;
        private readonly ActionExecutionHandle executionHandle;
        private readonly List<IReactiveBinding> bindings = new();
        private bool isDisposed;

        public ReactiveActionScope(
            ReactiveValueResolverService resolver,
            SceneKernel sceneKernel,
            ActionExecutionHandle executionHandle,
            EntityRef actor,
            EntityRef trigger)
        {
            this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            this.sceneKernel = sceneKernel;
            this.executionHandle = executionHandle;
            Actor = actor;
            Trigger = trigger;
        }

        public EntityRef Actor { get; }
        public EntityRef Trigger { get; }

        public ReactiveFloatBinding Bind(in ReactiveFloat value)
            => Bind(value, ReactiveBindingPolicy.GetDefaultBindingMode(value.SourceKind));

        public ReactiveSnapshotFloatBinding Bind(in ReactiveSnapshotFloat value)
        {
            ThrowIfDisposed();
            ReactiveSnapshotFloatBinding binding = new ReactiveSnapshotFloatBinding(
                resolver,
                CreateContext(),
                value);
            bindings.Add(binding);
            return binding;
        }

        public ReactiveWatchedFloatBinding Bind(in ReactiveWatchedFloat value)
        {
            ThrowIfDisposed();
            ReactiveWatchedFloatBinding binding = new ReactiveWatchedFloatBinding(
                resolver,
                CreateContext(),
                value);
            bindings.Add(binding);
            return binding;
        }

        public ReactiveFloatBinding Bind(in ReactiveFloat value, ReactiveEvaluationMode evaluationMode)
        {
            ThrowIfDisposed();
            ReactiveFloatBinding binding = new ReactiveFloatBinding(
                resolver,
                CreateContext(),
                value,
                evaluationMode,
                ReactiveFailurePolicy.FailAction);
            bindings.Add(binding);
            return binding;
        }

        public ReactiveIntBinding Bind(in ReactiveInt value)
            => Bind(value, ReactiveBindingPolicy.GetDefaultBindingMode(value.SourceKind));

        public ReactiveSnapshotIntBinding Bind(in ReactiveSnapshotInt value)
        {
            ThrowIfDisposed();
            ReactiveSnapshotIntBinding binding = new ReactiveSnapshotIntBinding(
                resolver,
                CreateContext(),
                value);
            bindings.Add(binding);
            return binding;
        }

        public ReactiveWatchedIntBinding Bind(in ReactiveWatchedInt value)
        {
            ThrowIfDisposed();
            ReactiveWatchedIntBinding binding = new ReactiveWatchedIntBinding(
                resolver,
                CreateContext(),
                value);
            bindings.Add(binding);
            return binding;
        }

        public ReactiveIntBinding Bind(in ReactiveInt value, ReactiveEvaluationMode evaluationMode)
        {
            ThrowIfDisposed();
            ReactiveIntBinding binding = new ReactiveIntBinding(
                resolver,
                CreateContext(),
                value,
                evaluationMode,
                ReactiveFailurePolicy.FailAction);
            bindings.Add(binding);
            return binding;
        }

        public ReactiveBoolBinding Bind(in ReactiveBool value)
            => Bind(value, ReactiveBindingPolicy.GetDefaultBindingMode(value.SourceKind));

        public ReactiveSnapshotBoolBinding Bind(in ReactiveSnapshotBool value)
        {
            ThrowIfDisposed();
            ReactiveSnapshotBoolBinding binding = new ReactiveSnapshotBoolBinding(
                resolver,
                CreateContext(),
                value);
            bindings.Add(binding);
            return binding;
        }

        public ReactiveWatchedBoolBinding Bind(in ReactiveWatchedBool value)
        {
            ThrowIfDisposed();
            ReactiveWatchedBoolBinding binding = new ReactiveWatchedBoolBinding(
                resolver,
                CreateContext(),
                value);
            bindings.Add(binding);
            return binding;
        }

        public ReactiveBoolBinding Bind(in ReactiveBool value, ReactiveEvaluationMode evaluationMode)
        {
            ThrowIfDisposed();
            ReactiveBoolBinding binding = new ReactiveBoolBinding(
                resolver,
                CreateContext(),
                value,
                evaluationMode,
                ReactiveFailurePolicy.FailAction);
            bindings.Add(binding);
            return binding;
        }

        public ReactiveVector3Binding Bind(in ReactiveVector3 value)
            => Bind(value, ReactiveBindingPolicy.GetDefaultBindingMode(value.SourceKind));

        public ReactiveVector3Binding Bind(in ReactiveVector3 value, ReactiveEvaluationMode evaluationMode)
        {
            ThrowIfDisposed();
            ReactiveVector3Binding binding = new ReactiveVector3Binding(
                resolver,
                CreateContext(),
                value,
                evaluationMode,
                ReactiveFailurePolicy.FailAction);
            bindings.Add(binding);
            return binding;
        }

        public ReactiveEntityRefBinding Bind(in ReactiveEntityRef value)
            => Bind(value, ReactiveBindingPolicy.GetDefaultBindingMode(value.SourceKind));

        public ReactiveSnapshotEntityRefBinding Bind(in ReactiveSnapshotEntityRef value)
        {
            ThrowIfDisposed();
            ReactiveSnapshotEntityRefBinding binding = new ReactiveSnapshotEntityRefBinding(
                resolver,
                CreateContext(),
                value);
            bindings.Add(binding);
            return binding;
        }

        public ReactiveWatchedEntityRefBinding Bind(in ReactiveWatchedEntityRef value)
        {
            ThrowIfDisposed();
            ReactiveWatchedEntityRefBinding binding = new ReactiveWatchedEntityRefBinding(
                resolver,
                CreateContext(),
                value);
            bindings.Add(binding);
            return binding;
        }

        public ReactiveEntityRefBinding Bind(in ReactiveEntityRef value, ReactiveEvaluationMode evaluationMode)
        {
            ThrowIfDisposed();
            ReactiveEntityRefBinding binding = new ReactiveEntityRefBinding(
                resolver,
                CreateContext(),
                value,
                evaluationMode,
                ReactiveFailurePolicy.FailAction);
            bindings.Add(binding);
            return binding;
        }

        public ReactiveStringBinding Bind(in ReactiveString value)
            => Bind(value, ReactiveBindingPolicy.GetDefaultBindingMode(value.SourceKind));

        public ReactiveSnapshotStringBinding Bind(in ReactiveSnapshotString value)
        {
            ThrowIfDisposed();
            ReactiveSnapshotStringBinding binding = new ReactiveSnapshotStringBinding(
                resolver,
                CreateContext(),
                value);
            bindings.Add(binding);
            return binding;
        }

        public ReactiveWatchedStringBinding Bind(in ReactiveWatchedString value)
        {
            ThrowIfDisposed();
            ReactiveWatchedStringBinding binding = new ReactiveWatchedStringBinding(
                resolver,
                CreateContext(),
                value);
            bindings.Add(binding);
            return binding;
        }

        public ReactiveStringBinding Bind(in ReactiveString value, ReactiveEvaluationMode evaluationMode)
        {
            ThrowIfDisposed();
            ReactiveStringBinding binding = new ReactiveStringBinding(
                resolver,
                CreateContext(),
                value,
                evaluationMode,
                ReactiveFailurePolicy.FailAction);
            bindings.Add(binding);
            return binding;
        }

        public ReactiveFaceExpressionIdBinding Bind(in ReactiveFaceExpressionId value)
            => Bind(value, ReactiveBindingPolicy.GetDefaultBindingMode(value.SourceKind));

        public ReactiveFaceExpressionIdBinding Bind(in ReactiveFaceExpressionId value, ReactiveEvaluationMode evaluationMode)
        {
            ThrowIfDisposed();
            ReactiveFaceExpressionIdBinding binding = new ReactiveFaceExpressionIdBinding(
                resolver,
                CreateContext(),
                value,
                evaluationMode,
                ReactiveFailurePolicy.FailAction);
            bindings.Add(binding);
            return binding;
        }

        public ReactiveEntityMoveStateBinding Bind(in ReactiveEntityMoveState value)
            => Bind(value, ReactiveBindingPolicy.GetDefaultBindingMode(value.SourceKind));

        public ReactiveEntityMoveStateBinding Bind(in ReactiveEntityMoveState value, ReactiveEvaluationMode evaluationMode)
        {
            ThrowIfDisposed();
            ReactiveEntityMoveStateBinding binding = new ReactiveEntityMoveStateBinding(
                resolver,
                CreateContext(),
                value,
                evaluationMode,
                ReactiveFailurePolicy.FailAction);
            bindings.Add(binding);
            return binding;
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            for (int index = 0; index < bindings.Count; index++)
            {
                bindings[index]?.Dispose();
            }

            bindings.Clear();
        }

        private ReactiveEvalContext CreateContext()
        {
            return new ReactiveEvalContext(sceneKernel, Actor, Trigger);
        }

        private void ThrowIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(ReactiveActionScope));
        }
    }
}