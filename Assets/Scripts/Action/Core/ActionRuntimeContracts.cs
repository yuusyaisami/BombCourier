using System;
using System.Collections.Generic;
using System.Threading;
using BC.Base;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public struct ActionId : IEquatable<ActionId>
    {
        [SerializeField] private string value;

        public ActionId(string value)
        {
            this.value = value;
        }

        public string Value => value;
        public bool IsValid => !string.IsNullOrWhiteSpace(value);

        public bool Equals(ActionId other)
        {
            return string.Equals(value, other.value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActionId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return value != null ? StringComparer.Ordinal.GetHashCode(value) : 0;
        }

        public override string ToString()
        {
            return IsValid ? value : "(None)";
        }
    }

    public enum ActionExecutionStatus
    {
        Completed = 0,
        Canceled = 1,
        Failed = 2,
    }

    public readonly struct ActionExecutionResult
    {
        public readonly ActionExecutionStatus Status;
        public readonly string Message;

        private ActionExecutionResult(ActionExecutionStatus status, string message)
        {
            Status = status;
            Message = message;
        }

        public bool IsCompleted => Status == ActionExecutionStatus.Completed;
        public bool IsCanceled => Status == ActionExecutionStatus.Canceled;
        public bool IsFailed => Status == ActionExecutionStatus.Failed;

        public static ActionExecutionResult Completed()
        {
            return new ActionExecutionResult(ActionExecutionStatus.Completed, string.Empty);
        }

        public static ActionExecutionResult Canceled(string message = null)
        {
            return new ActionExecutionResult(ActionExecutionStatus.Canceled, message ?? string.Empty);
        }

        public static ActionExecutionResult Failed(string message)
        {
            return new ActionExecutionResult(ActionExecutionStatus.Failed, message ?? string.Empty);
        }
    }

    public readonly struct ActionExecutionHandle : IEquatable<ActionExecutionHandle>
    {
        public readonly ulong Id;
        public readonly EntityRef Actor;

        public ActionExecutionHandle(ulong id, EntityRef actor)
        {
            Id = id;
            Actor = actor;
        }

        public bool IsValid => Id != 0 && Actor.IsValid;

        public bool Equals(ActionExecutionHandle other)
        {
            return Id == other.Id && Actor.Equals(other.Actor);
        }

        public override bool Equals(object obj)
        {
            return obj is ActionExecutionHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Actor);
        }
    }

    public enum ActionNodeStatus
    {
        Continue = 0,
        Running = 1,
        Failed = 2,
    }

    public interface IActionNodeDefinition
    {
        IActionNodeRuntime CreateRuntime();
    }

    public interface IActionNodeRuntime
    {
        ActionNodeStatus Tick(in ActionExecutionContext context, ref int remainingOperations);

        void Cancel(in ActionExecutionContext context)
        {
        }
    }

    public readonly struct ActionExecutionContext
    {
        public readonly SceneKernel SceneKernel;
        public readonly ActionService Actions;
        public readonly EntityRef ActorEntity;
        public readonly EntityRef TriggerEntity;

        public ActionExecutionContext(
            SceneKernel sceneKernel,
            ActionService actions,
            EntityRef actorEntity,
            EntityRef triggerEntity = default)
        {
            SceneKernel = sceneKernel;
            Actions = actions;
            ActorEntity = actorEntity;
            TriggerEntity = triggerEntity;
        }

        public EntityComponentResolverService EntityComponents => SceneKernel?.EntityComponents;
        public ValueStoreService EntityValueStore => SceneKernel?.EntityValueStore;
        public KernelValueStoreService KernelValueStore => SceneKernel?.KernelValueStore;
        public EventService Events => SceneKernel?.Events;
        public EntityRef SelfEntity => ActorEntity;
    }

    public sealed class ActionValidationContext
    {
        private readonly List<string> errors = new();

        public IReadOnlyList<string> Errors => errors;
        public bool IsValid => errors.Count == 0;

        public void AddError(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                errors.Add(message);
        }

        public void ValidateEntityTarget(EntityTargetReference target)
        {
            if (target.Mode == EntityTargetResolveMode.TagSearch && !target.Tag.IsAssigned)
                AddError("Entity target tag is not assigned.");
        }
    }

    public sealed class ActionCompileContext
    {
        private readonly List<IActionNodeDefinition> nodes = new();

        public IReadOnlyList<IActionNodeDefinition> Nodes => nodes;

        public void AddNode(IActionNodeDefinition node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            nodes.Add(node);
        }

        public void AddStep(IActionNodeDefinition node)
        {
            AddNode(node);
        }

        public ActionBlockDefinition BuildBlock()
        {
            return new ActionBlockDefinition(nodes);
        }

        public CompiledAction Build()
        {
            return new CompiledAction(BuildBlock());
        }
    }

    public sealed class CompiledAction
    {
        public CompiledAction(ActionBlockDefinition rootBlock)
        {
            RootBlock = rootBlock ?? throw new ArgumentNullException(nameof(rootBlock));
        }

        public ActionBlockDefinition RootBlock { get; }

        public IActionNodeRuntime CreateRuntime()
        {
            return RootBlock.CreateRuntime();
        }
    }

    public sealed class ActionBlockDefinition : IActionNodeDefinition
    {
        private readonly IActionNodeDefinition[] nodes;

        public ActionBlockDefinition(IReadOnlyList<IActionNodeDefinition> nodes)
        {
            if (nodes == null)
                throw new ArgumentNullException(nameof(nodes));

            this.nodes = new IActionNodeDefinition[nodes.Count];

            for (int i = 0; i < nodes.Count; i++)
            {
                this.nodes[i] = nodes[i];
            }
        }

        public int Count => nodes.Length;

        public IActionNodeRuntime CreateRuntime()
        {
            return new Runtime(nodes);
        }

        private sealed class Runtime : IActionNodeRuntime
        {
            private readonly IActionNodeRuntime[] nodes;
            private int index;

            public Runtime(IActionNodeDefinition[] definitions)
            {
                nodes = new IActionNodeRuntime[definitions.Length];

                for (int i = 0; i < definitions.Length; i++)
                {
                    nodes[i] = definitions[i]?.CreateRuntime();
                }
            }

            public ActionNodeStatus Tick(in ActionExecutionContext context, ref int remainingOperations)
            {
                while (index < nodes.Length && remainingOperations > 0)
                {
                    remainingOperations--;
                    IActionNodeRuntime node = nodes[index];

                    if (node == null)
                    {
                        index++;
                        continue;
                    }

                    ActionNodeStatus status = node.Tick(context, ref remainingOperations);

                    if (status == ActionNodeStatus.Running || status == ActionNodeStatus.Failed)
                        return status;

                    index++;
                }

                return index >= nodes.Length ? ActionNodeStatus.Continue : ActionNodeStatus.Running;
            }

            public void Cancel(in ActionExecutionContext context)
            {
                for (int i = index; i < nodes.Length; i++)
                {
                    nodes[i]?.Cancel(context);
                }
            }
        }
    }

    public readonly struct ActionCommand
    {
        public readonly ActionId ActionId;
        public readonly CompiledAction Definition;
        public readonly EntityRef TriggerEntity;

        public ActionCommand(ActionId actionId, EntityRef triggerEntity = default)
        {
            ActionId = actionId;
            Definition = null;
            TriggerEntity = triggerEntity;
        }

        public ActionCommand(CompiledAction definition, EntityRef triggerEntity = default)
        {
            ActionId = default;
            Definition = definition;
            TriggerEntity = triggerEntity;
        }
    }

    public sealed class ActionService : ITickable
    {
        private readonly SceneKernel sceneKernel;
        private readonly Dictionary<EntityRef, EntityActionExecutor> executorsByActor = new();
        private readonly List<EntityActionExecutor> executorBuffer = new();
        private ulong nextExecutionId = 1;

        public ActionService(SceneKernel sceneKernel)
        {
            this.sceneKernel = sceneKernel ?? throw new ArgumentNullException(nameof(sceneKernel));
        }

        public int MaxOperationsPerTick { get; set; } = 512;

        public ActionExecutionHandle Execute(EntityRef actor, ActionCommand command)
        {
            if (!TryResolveDefinition(actor, command, out CompiledAction definition))
                return default;

            EntityActionExecutor executor = GetOrCreateExecutor(actor);
            return executor.Start(CreateExecution(actor, command.TriggerEntity, definition));
        }

        public ActionExecutionHandle Execute(EntityRef actor, CompiledAction definition, EntityRef triggerEntity = default)
        {
            return Execute(actor, new ActionCommand(definition, triggerEntity));
        }

        public ActionExecutionHandle Execute(EntityRef actor, ActionId actionId, EntityRef triggerEntity = default)
        {
            return Execute(actor, new ActionCommand(actionId, triggerEntity));
        }

        public UniTask<ActionExecutionResult> ExecuteAsync(
            EntityRef actor,
            ActionCommand command,
            CancellationToken cancellationToken = default)
        {
            ActionExecutionHandle handle = Execute(actor, command);

            if (!TryGetExecution(handle, out ActionExecution execution))
                return UniTask.FromResult(ActionExecutionResult.Failed("Action could not be started."));

            execution.AttachCancellation(cancellationToken);
            return execution.Task;
        }

        public UniTask<ActionExecutionResult> ExecuteAsync(
            EntityRef actor,
            CompiledAction definition,
            EntityRef triggerEntity = default,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(actor, new ActionCommand(definition, triggerEntity), cancellationToken);
        }

        public UniTask<ActionExecutionResult> ExecuteAsync(
            EntityRef actor,
            ActionId actionId,
            EntityRef triggerEntity = default,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(actor, new ActionCommand(actionId, triggerEntity), cancellationToken);
        }

        public bool RegisterAction(EntityRef actor, ActionId actionId, CompiledAction definition)
        {
            if (!actor.IsValid || !actionId.IsValid || definition == null)
                return false;

            EntityActionExecutor executor = GetOrCreateExecutor(actor);
            executor.Register(actionId, definition);
            return true;
        }

        public bool UnregisterAction(EntityRef actor, ActionId actionId)
        {
            if (!executorsByActor.TryGetValue(actor, out EntityActionExecutor executor))
                return false;

            return executor.Unregister(actionId);
        }

        public void Cancel(EntityRef actor, string reason = null)
        {
            if (!executorsByActor.TryGetValue(actor, out EntityActionExecutor executor))
                return;

            executor.Cancel(reason ?? "Action was canceled.");
        }

        public void ClearEntity(EntityRef actor)
        {
            if (!executorsByActor.TryGetValue(actor, out EntityActionExecutor executor))
                return;

            executor.Cancel("Entity was unregistered.");
            executorsByActor.Remove(actor);
        }

        public void Clear()
        {
            foreach (EntityActionExecutor executor in executorsByActor.Values)
            {
                executor.Cancel("Action service was cleared.");
            }

            executorsByActor.Clear();
        }

        public bool TryGetExecution(ActionExecutionHandle handle, out ActionExecution execution)
        {
            execution = null;

            if (!handle.IsValid || !executorsByActor.TryGetValue(handle.Actor, out EntityActionExecutor executor))
                return false;

            return executor.TryGetExecution(handle, out execution);
        }

        public void Tick(float deltaTime)
        {
            if (executorsByActor.Count == 0)
                return;

            executorBuffer.Clear();
            executorBuffer.AddRange(executorsByActor.Values);
            int remainingOperations = Mathf.Max(1, MaxOperationsPerTick);

            for (int i = 0; i < executorBuffer.Count; i++)
            {
                if (remainingOperations <= 0)
                    break;

                executorBuffer[i].Tick(ref remainingOperations);
            }
        }

        private EntityActionExecutor GetOrCreateExecutor(EntityRef actor)
        {
            if (!executorsByActor.TryGetValue(actor, out EntityActionExecutor executor))
            {
                executor = new EntityActionExecutor(this, actor);
                executorsByActor.Add(actor, executor);
            }

            return executor;
        }

        private bool TryResolveDefinition(EntityRef actor, ActionCommand command, out CompiledAction definition)
        {
            definition = command.Definition;

            if (definition != null)
                return true;

            if (!command.ActionId.IsValid)
                return false;

            if (!executorsByActor.TryGetValue(actor, out EntityActionExecutor executor))
                return false;

            return executor.TryGetDefinition(command.ActionId, out definition);
        }

        private ActionExecution CreateExecution(EntityRef actor, EntityRef triggerEntity, CompiledAction definition)
        {
            ActionExecutionHandle handle = new(nextExecutionId++, actor);
            ActionExecutionContext context = new(sceneKernel, this, actor, triggerEntity);
            return new ActionExecution(handle, context, definition.CreateRuntime());
        }
    }

    public sealed class EntityActionExecutor
    {
        private readonly ActionService service;
        private readonly EntityRef actor;
        private readonly Dictionary<ActionId, CompiledAction> definitionsById = new();
        private ActionExecution activeExecution;

        public EntityActionExecutor(ActionService service, EntityRef actor)
        {
            this.service = service;
            this.actor = actor;
        }

        public ActionExecutionHandle Start(ActionExecution execution)
        {
            Cancel("Action was replaced.");
            activeExecution = execution;
            return execution.Handle;
        }

        public void Register(ActionId actionId, CompiledAction definition)
        {
            definitionsById[actionId] = definition;
        }

        public bool Unregister(ActionId actionId)
        {
            return definitionsById.Remove(actionId);
        }

        public bool TryGetDefinition(ActionId actionId, out CompiledAction definition)
        {
            return definitionsById.TryGetValue(actionId, out definition);
        }

        public bool TryGetExecution(ActionExecutionHandle handle, out ActionExecution execution)
        {
            execution = null;

            if (activeExecution == null || !activeExecution.Handle.Equals(handle))
                return false;

            execution = activeExecution;
            return true;
        }

        public void Tick(ref int remainingOperations)
        {
            if (activeExecution == null)
                return;

            activeExecution.Tick(ref remainingOperations);

            if (activeExecution.IsFinished)
                activeExecution = null;
        }

        public void Cancel(string reason)
        {
            if (activeExecution == null)
                return;

            activeExecution.Cancel(reason);
            activeExecution = null;
        }
    }

    public sealed class ActionExecution
    {
        private readonly IActionNodeRuntime rootRuntime;
        private readonly UniTaskCompletionSource<ActionExecutionResult> completionSource = new();
        private CancellationTokenRegistration cancellationRegistration;
        private bool cancellationAttached;

        public ActionExecution(ActionExecutionHandle handle, ActionExecutionContext context, IActionNodeRuntime rootRuntime)
        {
            Handle = handle;
            Context = context;
            this.rootRuntime = rootRuntime ?? throw new ArgumentNullException(nameof(rootRuntime));
        }

        public ActionExecutionHandle Handle { get; }
        public ActionExecutionContext Context { get; }
        public bool IsFinished { get; private set; }
        public UniTask<ActionExecutionResult> Task => completionSource.Task;

        public void AttachCancellation(CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled || IsFinished)
                return;

            if (cancellationToken.IsCancellationRequested)
            {
                Cancel("Action was canceled by token.");
                return;
            }

            cancellationRegistration = cancellationToken.Register(static state =>
            {
                ((ActionExecution)state).Cancel("Action was canceled by token.");
            }, this);
            cancellationAttached = true;
        }

        public void Tick(ref int remainingOperations)
        {
            if (IsFinished)
                return;

            if (remainingOperations <= 0 || IsFinished)
                return;

            ActionNodeStatus status = rootRuntime.Tick(Context, ref remainingOperations);

            if (status == ActionNodeStatus.Running)
                return;

            if (status == ActionNodeStatus.Failed)
            {
                Complete(ActionExecutionResult.Failed("Action node failed."));
                return;
            }

            Complete(ActionExecutionResult.Completed());
        }

        public void Cancel(string reason)
        {
            if (IsFinished)
                return;

            rootRuntime.Cancel(Context);
            Complete(ActionExecutionResult.Canceled(reason));
        }

        private void Complete(ActionExecutionResult result)
        {
            if (IsFinished)
                return;

            IsFinished = true;

            if (cancellationAttached)
                cancellationRegistration.Dispose();

            completionSource.TrySetResult(result);
        }
    }

    public static class ActionTargetResolver
    {
        public static int Resolve(
            in ActionExecutionContext context,
            EntityTargetReference target,
            List<EntityRef> results)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            results.Clear();

            switch (target.Mode)
            {
                case EntityTargetResolveMode.Self:
                    AddIfValid(context.SelfEntity, target.Selection, results);
                    break;

                case EntityTargetResolveMode.TriggerEntity:
                    AddIfValid(context.TriggerEntity, target.Selection, results);
                    break;

                case EntityTargetResolveMode.TagSearch:
                    ResolveByTag(context.SceneKernel, target.Tag.Id, target.Selection, results);
                    break;
            }

            return results.Count;
        }

        private static bool AddIfValid(EntityRef entity, EntityTargetSelection selection, List<EntityRef> results)
        {
            if (!entity.IsValid)
                return false;

            results.Add(entity);
            return selection == EntityTargetSelection.First;
        }

        private static void ResolveByTag(
            SceneKernel sceneKernel,
            EntityTagId tag,
            EntityTargetSelection selection,
            List<EntityRef> results)
        {
            if (!tag.IsValid)
                return;

            if (sceneKernel?.EntitiesRegistry != null &&
                AddFromRegistry(sceneKernel.EntitiesRegistry, tag, selection, results))
            {
                return;
            }

            ApplicationKernel applicationKernel = ApplicationKernelMB.Instance != null
                ? ApplicationKernelMB.Instance.Kernel
                : null;

            if (applicationKernel?.ApplicationEntityRegistry != null)
                AddFromRegistry(applicationKernel.ApplicationEntityRegistry, tag, selection, results);
        }

        private static bool AddFromRegistry(
            ScopedEntityRegistry registry,
            EntityTagId tag,
            EntityTargetSelection selection,
            List<EntityRef> results)
        {
            IReadOnlyList<EntityRef> entities = registry.GetEntitiesByTag(tag);

            for (int i = 0; i < entities.Count; i++)
            {
                EntityRef entity = entities[i];

                if (!entity.IsValid)
                    continue;

                results.Add(entity);

                if (selection == EntityTargetSelection.First)
                    return true;
            }

            return false;
        }
    }
}
