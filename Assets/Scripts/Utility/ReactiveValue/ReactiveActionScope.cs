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
        private readonly ILocalValueStoreService localValueStore;
        private readonly List<IReactiveBinding> bindings = new();
        private bool isDisposed;

        public ReactiveActionScope(
            ReactiveValueResolverService resolver,
            SceneKernel sceneKernel,
            ActionExecutionHandle executionHandle,
            EntityRef actor,
            EntityRef trigger,
            ILocalValueStoreService localValueStore)
        {
            this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            this.sceneKernel = sceneKernel;
            this.executionHandle = executionHandle;
            this.localValueStore = localValueStore;
            Actor = actor;
            Trigger = trigger;
        }

        public EntityRef Actor { get; }
        public EntityRef Trigger { get; }

        public ReactiveFloatBinding Bind(in ReactiveFloat value)
            => Bind(value, ReactiveBindingPolicy.GetDefaultBindingMode(value.SourceKind));

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
            return new ReactiveEvalContext(sceneKernel, Actor, Trigger, localValueStore);
        }

        private void ThrowIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(ReactiveActionScope));
        }
    }
}