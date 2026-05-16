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
        {
            ThrowIfDisposed();
            ReactiveFloatBinding binding = new ReactiveFloatBinding(resolver, CreateContext(), value);
            bindings.Add(binding);
            return binding;
        }

        public ReactiveIntBinding Bind(in ReactiveInt value)
        {
            ThrowIfDisposed();
            ReactiveIntBinding binding = new ReactiveIntBinding(resolver, CreateContext(), value);
            bindings.Add(binding);
            return binding;
        }

        public ReactiveBoolBinding Bind(in ReactiveBool value)
        {
            ThrowIfDisposed();
            ReactiveBoolBinding binding = new ReactiveBoolBinding(resolver, CreateContext(), value);
            bindings.Add(binding);
            return binding;
        }

        public ReactiveVector3Binding Bind(in ReactiveVector3 value)
        {
            ThrowIfDisposed();
            ReactiveVector3Binding binding = new ReactiveVector3Binding(resolver, CreateContext(), value);
            bindings.Add(binding);
            return binding;
        }

        public ReactiveEntityRefBinding Bind(in ReactiveEntityRef value)
        {
            ThrowIfDisposed();
            ReactiveEntityRefBinding binding = new ReactiveEntityRefBinding(resolver, CreateContext(), value);
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