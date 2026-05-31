using System;
using System.Collections.Generic;
using System.Threading;
using BC.Base;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BC.ActionSystem
{
    // Action の外部識別子。文字列ベースの登録やデバッグ表示で使う軽量な ID。
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

    // Action 実行の結果区分。完了・中断・失敗を明示的に分ける。
    public enum ActionExecutionStatus
    {
        Completed = 0,
        Canceled = 1,
        Failed = 2,
    }

    // 実行結果のメッセージ付き値オブジェクト。
    // 失敗理由や中断理由を呼び出し側へ返す。
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

    // 1 回の action 実行を表すハンドル。
    // Actor と execution id を組にして、キャンセルや camera override の紐付けに使う。
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

    // node の tick 結果。継続、実行中、失敗を示す。
    public enum ActionNodeStatus
    {
        Continue = 0,
        Running = 1,
        Failed = 2,
    }

    // runtime node の最小契約。
    // それぞれの step は Tick で進み、必要なら Cancel で後片付けする。
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

    // Action 実行中に node が参照する共通コンテキスト。
    // SceneKernel 由来の各 service と、actor/trigger/local store を束ねる。
    public readonly struct ActionExecutionContext
    {
        public readonly ActionExecutionHandle ExecutionHandle;
        public readonly SceneKernel SceneKernel;
        public readonly ActionService Actions;
        public readonly EntityRef ActorEntity;
        public readonly EntityRef TriggerEntity;
        public readonly ILocalValueStoreService LocalValueStore;
        public readonly ReactiveActionScope Reactive;

        public ActionExecutionContext(
            SceneKernel sceneKernel,
            ActionService actions,
            EntityRef actorEntity,
            EntityRef triggerEntity = default,
            ReactiveActionScope reactive = null)
            : this(sceneKernel, actions, new ActionExecutionHandle(0, actorEntity), triggerEntity, null, reactive)
        {
        }

        public ActionExecutionContext(
            SceneKernel sceneKernel,
            ActionService actions,
            EntityRef actorEntity,
            EntityRef triggerEntity,
            ILocalValueStoreService localValueStore,
            ReactiveActionScope reactive = null)
            : this(sceneKernel, actions, new ActionExecutionHandle(0, actorEntity), triggerEntity, localValueStore, reactive)
        {
        }

        public ActionExecutionContext(
            SceneKernel sceneKernel,
            ActionService actions,
            ActionExecutionHandle executionHandle,
            EntityRef triggerEntity = default,
            ReactiveActionScope reactive = null)
            : this(sceneKernel, actions, executionHandle, triggerEntity, null, reactive)
        {
        }

        public ActionExecutionContext(
            SceneKernel sceneKernel,
            ActionService actions,
            ActionExecutionHandle executionHandle,
            EntityRef triggerEntity,
            ILocalValueStoreService localValueStore,
            ReactiveActionScope reactive = null)
        {
            ExecutionHandle = executionHandle;
            SceneKernel = sceneKernel;
            Actions = actions;
            ActorEntity = executionHandle.Actor;
            TriggerEntity = triggerEntity;
            LocalValueStore = localValueStore;
            Reactive = reactive;
        }

        public EntityComponentResolverService EntityComponents => SceneKernel?.EntityComponents;
        public ValueStoreService EntityValueStore => SceneKernel?.EntityValueStore;
        public KernelValueStoreService KernelValueStore => ApplicationKernelMB.Instance?.Kernel?.KernelValueStore;
        public EventService Events => SceneKernel?.Events;
        public EntityRef SelfEntity => ActorEntity;
    }

    // authoring 時の検証結果を溜めるコンテキスト。
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

    // authoring された step を runtime node 群へ変換するための組み立て器。
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

    // compile 済み action のルート。
    // 実行時はこの RootBlock から runtime を生成する。
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

    // 連続実行される node 群を 1 つの block として扱う runtime 定義。
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

        // block 内の node を順番に tick し、途中で Running/Failed が返ればそこで止める。
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

    // 行動を登録名で呼ぶときのコマンド。
    // 直接 compiled action を渡す場合も同じ型で扱えるようにしている。
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

    // actor ごとの action 実行を管理する service。
    // action の開始、継続 tick、キャンセル、解除を一元管理する。
    public sealed class ActionService : ITickable
    {
        private readonly SceneKernel sceneKernel;
        private readonly Dictionary<EntityRef, EntityActionExecutor> executorsByActor = new();
        private readonly List<EntityActionExecutor> executorBuffer = new();
        private readonly List<ActionExecution> detachedExecutions = new();
        private readonly List<ActionExecution> detachedExecutionBuffer = new();
        private ulong nextExecutionId = 1;

        public ActionService(SceneKernel sceneKernel)
        {
            this.sceneKernel = sceneKernel ?? throw new ArgumentNullException(nameof(sceneKernel));
        }

        public int MaxOperationsPerTick { get; set; } = 512;

        // actor/command から action を起動し、execution handle を返す。
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

        // 登録済み action id を起点に実行する。
        public ActionExecutionHandle Execute(EntityRef actor, ActionId actionId, EntityRef triggerEntity = default)
        {
            return Execute(actor, new ActionCommand(actionId, triggerEntity));
        }

        // async 完了を待つ場合の入口。
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

        // compiled action を直接 await したい場合の入口。
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

        // 親 action を置き換えずに、同じ actor 上で独立した子 action を走らせる入口。
        public UniTask<ActionExecutionResult> ExecuteDetachedAsync(
            EntityRef actor,
            ActionCommand command,
            CancellationToken cancellationToken = default)
        {
            if (!TryResolveDefinition(actor, command, out CompiledAction definition))
                return UniTask.FromResult(ActionExecutionResult.Failed("Action could not be started."));

            ActionExecution execution = CreateExecution(actor, command.TriggerEntity, definition);
            execution.AttachCancellation(cancellationToken);
            detachedExecutions.Add(execution);
            return execution.Task;
        }

        public UniTask<ActionExecutionResult> ExecuteDetachedAsync(
            EntityRef actor,
            CompiledAction definition,
            EntityRef triggerEntity = default,
            CancellationToken cancellationToken = default)
        {
            return ExecuteDetachedAsync(actor, new ActionCommand(definition, triggerEntity), cancellationToken);
        }

        public UniTask<ActionExecutionResult> ExecuteDetachedAsync(
            EntityRef actor,
            ActionId actionId,
            EntityRef triggerEntity = default,
            CancellationToken cancellationToken = default)
        {
            return ExecuteDetachedAsync(actor, new ActionCommand(actionId, triggerEntity), cancellationToken);
        }

        // actor ごとに action を事前登録しておく場合に使う。
        public bool RegisterAction(EntityRef actor, ActionId actionId, CompiledAction definition)
        {
            if (!actor.IsValid || !actionId.IsValid || definition == null)
                return false;

            EntityActionExecutor executor = GetOrCreateExecutor(actor);
            executor.Register(actionId, definition);
            return true;
        }

        // 登録済み action の解除。
        public bool UnregisterAction(EntityRef actor, ActionId actionId)
        {
            if (!executorsByActor.TryGetValue(actor, out EntityActionExecutor executor))
                return false;

            return executor.Unregister(actionId);
        }

        // actor へ紐づく進行中 action をまとめてキャンセルする。
        public void Cancel(EntityRef actor, string reason = null)
        {
            if (!executorsByActor.TryGetValue(actor, out EntityActionExecutor executor))
                return;

            executor.Cancel(reason ?? "Action was canceled.");
        }

        // actor がシーンから外れたときに、その actor の実行状態を整理する。
        public void ClearEntity(EntityRef actor)
        {
            if (!executorsByActor.TryGetValue(actor, out EntityActionExecutor executor))
            {
                CancelDetached(actor, "Entity was unregistered.");
                return;
            }

            executor.Cancel("Entity was unregistered.");
            executorsByActor.Remove(actor);
            CancelDetached(actor, "Entity was unregistered.");
        }

        // シーン終了や再ロード時にすべての action を止める。
        public void Clear()
        {
            foreach (EntityActionExecutor executor in executorsByActor.Values)
            {
                executor.Cancel("Action service was cleared.");
            }

            for (int i = 0; i < detachedExecutions.Count; i++)
            {
                detachedExecutions[i].Cancel("Action service was cleared.");
            }

            executorsByActor.Clear();
            detachedExecutions.Clear();
        }

        // 外部から execution handle で実行中 action を引けるようにする。
        public bool TryGetExecution(ActionExecutionHandle handle, out ActionExecution execution)
        {
            execution = null;

            if (!handle.IsValid || !executorsByActor.TryGetValue(handle.Actor, out EntityActionExecutor executor))
                return false;

            return executor.TryGetExecution(handle, out execution);
        }

        // 1 tick で処理する operation 数に上限を設け、長大な action でフレームを潰しにくくする。
        public void Tick(float deltaTime)
        {
            if (executorsByActor.Count == 0 && detachedExecutions.Count == 0)
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

            if (remainingOperations <= 0 || detachedExecutions.Count == 0)
                return;

            detachedExecutionBuffer.Clear();
            detachedExecutionBuffer.AddRange(detachedExecutions);

            for (int i = 0; i < detachedExecutionBuffer.Count; i++)
            {
                if (remainingOperations <= 0)
                    break;

                ActionExecution execution = detachedExecutionBuffer[i];
                execution.Tick(ref remainingOperations);

                if (execution.IsFinished)
                {
                    detachedExecutions.Remove(execution);
                }
            }
        }

        private void CancelDetached(EntityRef actor, string reason)
        {
            for (int i = detachedExecutions.Count - 1; i >= 0; i--)
            {
                ActionExecution execution = detachedExecutions[i];

                if (!execution.Handle.Actor.Equals(actor))
                    continue;

                execution.Cancel(reason);
                detachedExecutions.RemoveAt(i);
            }
        }

        // actor に対する executor を lazily 作る。
        private EntityActionExecutor GetOrCreateExecutor(EntityRef actor)
        {
            if (!executorsByActor.TryGetValue(actor, out EntityActionExecutor executor))
            {
                executor = new EntityActionExecutor(this, actor);
                executorsByActor.Add(actor, executor);
            }

            return executor;
        }

        // command に直接 definition があればそれを使い、なければ登録済み action を引く。
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

        // action 実行 1 件分を生成する。Reactive scope と local value store もここで用意する。
        private ActionExecution CreateExecution(EntityRef actor, EntityRef triggerEntity, CompiledAction definition)
        {
            ActionExecutionHandle handle = new(nextExecutionId++, actor);
            ActionLocalValueStoreService localValueStore = new();
            ReactiveActionScope reactiveScope = sceneKernel.ReactiveValues?.CreateActionScope(handle, actor, triggerEntity);
            ActionExecutionContext context = new(sceneKernel, this, handle, triggerEntity, localValueStore, reactiveScope);

            try
            {
                return new ActionExecution(handle, context, definition.CreateRuntime());
            }
            catch
            {
                reactiveScope?.Dispose();
                throw;
            }
        }
    }

    // 1 actor に対して 1 実行中 action を保持する executor。
    // 新しい action が来たら既存の action を置き換える。
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

        // 新しい action を受けたら、既存の実行を止めてから差し替える。
        public ActionExecutionHandle Start(ActionExecution execution)
        {
            Cancel("Action was replaced.");
            activeExecution = execution;
            return execution.Handle;
        }

        // actor に対して後で呼び出せる action 定義を登録する。
        public void Register(ActionId actionId, CompiledAction definition)
        {
            definitionsById[actionId] = definition;
        }

        // 登録解除。
        public bool Unregister(ActionId actionId)
        {
            return definitionsById.Remove(actionId);
        }

        // 現在の action id から compiled action を引く。
        public bool TryGetDefinition(ActionId actionId, out CompiledAction definition)
        {
            return definitionsById.TryGetValue(actionId, out definition);
        }

        // execution handle が現在の activeExecution と一致するかを確認する。
        public bool TryGetExecution(ActionExecutionHandle handle, out ActionExecution execution)
        {
            execution = null;

            if (activeExecution == null || !activeExecution.Handle.Equals(handle))
                return false;

            execution = activeExecution;
            return true;
        }

        // tick は active execution 1 件だけへ流す。
        public void Tick(ref int remainingOperations)
        {
            if (activeExecution == null)
                return;

            activeExecution.Tick(ref remainingOperations);

            if (activeExecution.IsFinished)
            {
                activeExecution = null;
            }
        }

        // active execution を止める。
        public void Cancel(string reason)
        {
            if (activeExecution == null)
                return;

            activeExecution.Cancel(reason);
            activeExecution = null;
        }
    }

    // 実行中の action 1 件。
    // Tick で root runtime を進め、完了/失敗/中断時に後始末を行う。
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

        // 外部 cancellation token と結びつける。
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

        // root runtime を 1 tick 進める。
        public void Tick(ref int remainingOperations)
        {
            if (IsFinished)
                return;

            if (remainingOperations <= 0 || IsFinished)
                return;

            ActionNodeStatus status;

            try
            {
                status = rootRuntime.Tick(Context, ref remainingOperations);
            }
            catch (Exception exception)
            {
                FailFromException("tick", exception);
                return;
            }

            if (status == ActionNodeStatus.Running)
                return;

            if (status == ActionNodeStatus.Failed)
            {
                Complete(ActionExecutionResult.Failed("Action node failed."));
                return;
            }

            Complete(ActionExecutionResult.Completed());
        }

        // action をキャンセルする。
        public void Cancel(string reason)
        {
            if (IsFinished)
                return;

            try
            {
                rootRuntime.Cancel(Context);
                Complete(ActionExecutionResult.Canceled(reason));
            }
            catch (Exception exception)
            {
                FailFromException("cancel", exception);
            }
        }

        // 完了・中断・失敗の共通後処理。
        private void Complete(ActionExecutionResult result)
        {
            if (IsFinished)
                return;

            IsFinished = true;
            // Action-scoped camera overrides must be released even when the action exits early.
            Context.SceneKernel?.Cameras?.ClearActionCamera(Handle);
            Context.Reactive?.Dispose();
            Context.LocalValueStore?.Clear();

            if (cancellationAttached)
                cancellationRegistration.Dispose();

            completionSource.TrySetResult(result);
        }

        // runtime node から exception が上がってきたときの共通失敗処理。
        private void FailFromException(string phase, Exception exception)
        {
            Debug.LogException(exception);
            Complete(ActionExecutionResult.Failed($"Action node threw during {phase}: {exception.Message}"));
        }
    }

    // entity / tag / selection をもとに action target を解決する helper。
    public static class ActionTargetResolver
    {
        public static int Resolve(
            in ActionExecutionContext context,
            EntityTargetReference target,
            List<EntityRef> results)
        {
            return ScopedEntityResolveUtility.ResolveTargets(context, EntityResolveScope.Entity, target, results);
        }
    }
}
