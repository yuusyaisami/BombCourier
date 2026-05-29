# BombCourier ReactiveValue System Spec v0.1

## 1. 目的

BombCourier の ReactiveValue は、Action / Gimmick / Camera 演出などの設定値を、固定値だけでなく実行時状態から取得できるようにするための小型 binding / resolver 層である。

この仕様の主目的は以下。

1. 固定値と動的値を同じ authoring データとして扱えるようにする。
2. ValueStore の `ValueWatchHandle<T>` を活用し、毎フレーム `Get<T>()` を繰り返さない。
3. `SelfEntity` / `TriggerEntity` / `EntityTargetReference` / `ValueStore` / `Transform` を Action 実行中に安全に参照できるようにする。
4. 監視やキャッシュの寿命を Action 実行単位に閉じる。
5. 解決失敗を `default(T)` で黙殺せず、明示的な失敗として扱う。
6. 現在の SceneKernel / ActionService / ValueStore / EntityRef 構造を大きく壊さずに導入できる形に留める。

## 2. 現状コード前提

この仕様は、現時点の BombCourier 実装を前提に調整している。

### 2.1 Kernel 初期化

- `BaseKernel` は `ITickable[] Tickables` を順に `Tick(float deltaTime)` するだけの最小構成である。
- `SceneKernel` constructor で直接生成しているのは現状 `EntityComponentResolverService`、`ActionService`、`CameraPathPlayerService` である。
- `EntitiesRegistry`、`Events`、`EntityLifecycle`、`EntityValueStore`、`KernelValueStore`、`Spawner` は `IKernelInstaller` を実装した MB 側から注入される。
- したがって、ReactiveValue 仕様は「SceneKernel が常に constructor 内で全 service を組み立てる」前提にはしない。

### 2.2 Action 実行系

- `ActionExecutionContext` は現在 `SceneKernel`、`ActionService`、`ActorEntity`、`TriggerEntity` を持ち、`EntityComponents`、`EntityValueStore`、`KernelValueStore`、`Events`、`SelfEntity` への convenience accessors を提供している。
- `ActionService` は actor ごとに単一の `ActionExecution` を走らせる構造で、`CreateExecution()` から `ActionExecutionContext` を作る。
- `ActionExecution` は `rootRuntime.Tick()` の戻り値が `Continue / Running / Failed` のいずれかであることを前提に完了判定する。

### 2.3 ValueStore 監視

- `ValueStoreService` と `KernelValueStoreService` はどちらも `GetHandle<T>()` を持つ。
- `ValueWatchHandle<T>` は `CurrentValue`、`Version`、`TryGetChanged()`、`Subscribe()` を提供する。
- watch node の `Version` は 1 始まりで、値変更時に `Refresh()` される。
- `EntityMoveController` はすでに `ValueWatchHandle<bool/float>` をキャッシュして `CurrentValue` を読む構造を取っている。

### 2.4 Entity / Target 解決

- Action 実行系には `BC.ActionSystem.ActionTargetResolver` がすでに存在する。
- Wiring 系には別途 `BC.Base.EntityTargetResolver` があり、似た責務を別 context で持っている。
- ReactiveValue 導入時に 3 本目の resolver を増やすのではなく、Action 用は既存 `ActionTargetResolver` を再利用するか、後で共通 utility に寄せる。

### 2.5 Transform 解決

- `EntityComponentResolverService` は `TryGetTransform()` を含み、EntityRef から `GameObject` / `Transform` / `EntityMB` / 任意 component をキャッシュ付きで解決できる。
- Transform 自体に ValueStore のような version 監視は存在しない。

### 2.6 CameraPath の現状

- `CameraPathPlayerService` は `ITickable` ではなく、`UniTask` ベースでカメラパスを再生する。
- 各ポイント到達時に `InlineAction` を `sceneKernel.Actions.ExecuteAsync()` で起動できる。
- よって ReactiveValue の初期導入先は CameraPath 本体よりも Action 引数側が優先である。

## 3. 非目的

初期段階では以下をやらない。

- 任意の MonoBehaviour field / property 参照
- Reflection ベースの public member 読み取り
- GameObject 名検索
- hierarchy path 検索
- `FindObjectOfType` 系参照
- 任意の C# 式評価
- ノードグラフ式エディタ
- 本格的な TransformTracker
- 汎用 subscribe ベース reactive framework の構築

特に、参照範囲を無制限に広げない。BombCourier 版 ReactiveValue が解決対象にしてよいのは以下に限定する。

- Literal
- `ActionExecutionContext`
- `EntityTargetReference`
- `EntityValueStore`
- `KernelValueStore`
- `EntityComponentResolverService`
- `Transform`

## 4. 設計原則

### 4.1 ReactiveValue は authoring data

Inspector 上の値は runtime object ではなく、あくまで serializable な authoring data とする。

監視、キャッシュ、再評価は runtime 側の binding object が担当する。

```csharp
[Serializable]
public struct ReactiveFloat
{
    [SerializeField] private ReactiveFloatSourceKind sourceKind;
    [SerializeField] private ReactiveEvaluationMode evaluationMode;
    [SerializeField] private ReactiveFailurePolicy failurePolicy;
    [SerializeField] private float literal;
    [SerializeField] private ReactiveEntityFloatValueSource entityValue;
    [SerializeField] private ReactiveKernelFloatValueSource kernelValue;
    [SerializeField] private ReactiveDistanceFloatSource distance;
}
```

### 4.2 generic authoring は避ける

Unity serialization と Inspector 互換性を考え、最初は型別 wrapper を使う。

- `ReactiveFloat`
- `ReactiveInt`
- `ReactiveBool`
- `ReactiveVector3`
- `ReactiveEntityRef`

### 4.3 Continuous は binding.Read() で評価する

現行コードでは `ValueWatchHandle<T>` の更新は書き込み時に発生し、Transform 系には中央監視がない。

そのため、初期実装の `Continuous` は resolver service の中央 tick 更新ではなく、各 binding の `Read()` 時に都度評価する方式を標準とする。

`ReactiveValueResolverService` を将来 `ITickable` に拡張する余地は残すが、初期版で必須にはしない。

## 5. 配置方針

runtime 側は以下を第一候補とする。

```text
Assets/Scripts/Utility/ReactiveValue/
```

editor 側は以下を第一候補とする。

```text
Assets/Scripts/Editor/ReactiveValue/
```

補足:

- 現在の editor drawer は `Assets/Scripts/Editor` 直下にも置かれている。
- ReactiveValue 用に subfolder を切ってよいが、既存 editor assembly のまま運用する。

namespace は基本 `BC.Base` を使う。Action 実行専用の薄い adapter だけ `BC.ActionSystem` に置くのは可。

## 6. Core API

### 6.1 評価モード

```csharp
public enum ReactiveEvaluationMode
{
    Snapshot = 0,
    Watched = 1,
    Continuous = 2,
}
```

- `Snapshot`: 初回に 1 回だけ評価してキャッシュする。
- `Watched`: ValueStore 系 source に対して `ValueWatchHandle<T>` を保持し、`Version` 変化だけを見る。
- `Continuous`: `Read()` のたびに再評価する。Transform や距離計算はこちら。

制約:

- ValueStore 以外の source に `Watched` は適用しない。
- ValueStore source に `Continuous` を許可しない。ValueStore は `Watched` か `Snapshot` に寄せる。

### 6.2 失敗ポリシー

```csharp
public enum ReactiveFailurePolicy
{
    FailAction = 0,
    UseFallback = 1,
}
```

初期版では `IgnoreNode` は入れない。

理由:

- 現在の `IActionNodeRuntime` には「失敗したが node をスキップして続行する」共通契約がない。
- `ActionNodeStatus` は `Continue / Running / Failed` の 3 値であり、Ignore 系の挙動は node 実装側に大きな変更を要求する。

したがって、M1-M5 の標準挙動は `FailAction` を基本とする。

`UseFallback` は fallback 値をその spec 自体が持つ場合のみ許可し、binding 側で明示的に処理する。

### 6.3 Result 型

```csharp
public readonly struct ReactiveResult<T>
{
    public readonly bool Success;
    public readonly T Value;
    public readonly ReactiveError Error;
    public readonly int Version;

    public bool Failed => !Success;

    public static ReactiveResult<T> Ok(T value, int version = 0)
    {
        return new ReactiveResult<T>(true, value, default, version);
    }

    public static ReactiveResult<T> Fail(ReactiveError error)
    {
        return new ReactiveResult<T>(false, default, error, 0);
    }
}
```

### 6.4 Error 型

```csharp
public readonly struct ReactiveError
{
    public readonly ReactiveErrorCode Code;
    public readonly string Message;
    public readonly EntityRef Actor;
    public readonly EntityRef Trigger;
}

public enum ReactiveErrorCode
{
    None = 0,
    MissingSceneKernel,
    MissingValueStore,
    MissingEntityComponentResolver,
    InvalidEntity,
    EntityNotAlive,
    TargetNotFound,
    MultipleTargetsNotAllowed,
    ValueKeyNotAssigned,
    ValueKeyTypeMismatch,
    ValueStoreReadFailed,
    TransformNotFound,
    UnsupportedSource,
    UnsupportedEvaluationMode,
}
```

### 6.5 EvalContext

```csharp
public readonly struct ReactiveEvalContext
{
    public readonly SceneKernel SceneKernel;
    public readonly EntityRef ActorEntity;
    public readonly EntityRef TriggerEntity;

    public ReactiveEvalContext(SceneKernel sceneKernel, EntityRef actorEntity, EntityRef triggerEntity)
    {
        SceneKernel = sceneKernel;
        ActorEntity = actorEntity;
        TriggerEntity = triggerEntity;
    }

    public ReactiveEvalContext(in ActionExecutionContext actionContext)
        : this(actionContext.SceneKernel, actionContext.ActorEntity, actionContext.TriggerEntity)
    {
    }

    public EntityRef SelfEntity => ActorEntity;
}
```

## 7. Source モデル

### 7.1 ReactiveEntityRef

```csharp
public enum ReactiveEntitySourceKind
{
    Self = 0,
    TriggerEntity = 1,
    TargetReference = 2,
    EntityValueStore = 3,
    KernelValueStore = 4,
}
```

`Explicit` は初期版では採用しない。

理由:

- `EntityRef` は registry で払い出される runtime ID + version であり、authoring 時に固定値として持つ前提ではない。
- inspector に生の `EntityRef` を置く設計は現行 registry モデルと相性が悪い。

`TargetReference` は既存 `EntityTargetReference` を使う。

制約:

- `Selection = First` のみ許可する。
- `Selection = All` は `ReactiveEntityRef` では不許可とし、必要なら将来 `ReactiveEntityList` を別設計する。

### 7.2 ReactiveFloat

```csharp
public enum ReactiveFloatSourceKind
{
    Literal = 0,
    EntityValueStore = 1,
    KernelValueStore = 2,
    Distance = 3,
}
```

### 7.3 ReactiveInt

```csharp
public enum ReactiveIntSourceKind
{
    Literal = 0,
    EntityValueStore = 1,
    KernelValueStore = 2,
}
```

### 7.4 ReactiveBool

```csharp
public enum ReactiveBoolSourceKind
{
    Literal = 0,
    EntityValueStore = 1,
    KernelValueStore = 2,
    EntityAlive = 3,
    CompareNumber = 4,
}
```

### 7.5 ReactiveVector3

```csharp
public enum ReactiveVector3SourceKind
{
    Literal = 0,
    EntityTransformPosition = 1,
    EntityTransformForward = 2,
    AddPosition = 3,
    AddForward = 4,
    Direction = 5,
}
```

`Add` は初期版では基準ベクトルを明示した `AddPosition` / `AddForward` に分割する。

## 8. ValueStore source 設計

ValueStore source は `ValueKeyReference` を使う。

```csharp
[Serializable]
public struct ReactiveEntityFloatValueSource
{
    [SerializeField] private ReactiveEntityRef entity;
    [SerializeField] private ValueKeyReference key;
}
```

注意点:

- `ValueKeyReference` は `id` / `path` / `valueTypeName` を持ち、`TryResolve<T>()` で `ValueKey<T>` に解決される。
- 型不一致や composition mode 不一致は `ValueStoreScope` 内で `InvalidOperationException` になり得る。
- ReactiveValue 側ではこれを catch し、`ReactiveResult.Fail` に変換する。

`Watched` の評価では `GetHandle<T>()` を使う。

```csharp
ValueWatchHandle<float> handle =
    context.SceneKernel.EntityValueStore.GetHandle<float>(entity, key);

float value = handle.CurrentValue;
int version = handle.Version;
```

## 9. Transform source 設計

Transform 系 source は `EntityComponentResolverService.TryGetTransform()` 経由で取得する。

```csharp
if (!context.SceneKernel.EntityComponents.TryGetTransform(entity, out Transform transform))
{
    return ReactiveResult<Vector3>.Fail(...);
}
```

制約:

- `EntityTransformPosition` / `EntityTransformForward` は `Snapshot` または `Continuous` のみ。
- `Watched` は不許可。
- 初期版では polling で十分であり、TransformTracker は作らない。

## 10. Runtime サービス設計

### 10.1 ReactiveValueResolverService

`SceneKernel` に以下を追加する。

```csharp
public ReactiveValueResolverService ReactiveValues { get; set; }
```

初期案と異なり、初期版では `ITickable` 実装を必須にしない。

```csharp
public sealed class ReactiveValueResolverService
{
    private readonly SceneKernel sceneKernel;

    public ReactiveValueResolverService(SceneKernel sceneKernel)
    {
        this.sceneKernel = sceneKernel;
    }

    public ReactiveActionScope CreateActionScope(
        ActionExecutionHandle handle,
        EntityRef actor,
        EntityRef trigger)
    {
        return new ReactiveActionScope(this, sceneKernel, handle, actor, trigger);
    }

    public ReactiveResult<float> ResolveFloat(in ReactiveEvalContext context, in ReactiveFloat value) { ... }
    public ReactiveResult<int> ResolveInt(in ReactiveEvalContext context, in ReactiveInt value) { ... }
    public ReactiveResult<bool> ResolveBool(in ReactiveEvalContext context, in ReactiveBool value) { ... }
    public ReactiveResult<Vector3> ResolveVector3(in ReactiveEvalContext context, in ReactiveVector3 value) { ... }
    public ReactiveResult<EntityRef> ResolveEntity(in ReactiveEvalContext context, in ReactiveEntityRef value) { ... }

    public void Clear() { }
}
```

### 10.2 SceneKernel への追加方針

M5 実装では `SceneKernel` constructor owner に統一する。

1. `SceneKernel` constructor で `ReactiveValues = new ReactiveValueResolverService(this);` を生成する。
2. `SceneKernel.Dispose()` で `ReactiveValues?.Clear();` を呼ぶ。

理由:

- `ActionService` や `CameraPathPlayerService` と同様、追加設定なしで成立する scene-local service だから。
- `EntityValueStore` / `KernelValueStore` 自体は installer 注入であるため、resolver 側は null 耐性を持つ必要がある。

初期版の `Tickables` は変更しない。

```csharp
Tickables = new ITickable[]
{
    Actions,
};
```

中央管理の Continuous 更新が必要になった段階でのみ、`ReactiveValues` を `ITickable` 化し `Actions` より前に並べる。

## 11. Action 統合

### 11.1 ActionExecutionContext への追加

```csharp
public readonly ReactiveActionScope Reactive;
```

```csharp
public ActionExecutionContext(
    SceneKernel sceneKernel,
    ActionService actions,
    EntityRef actorEntity,
    EntityRef triggerEntity = default,
    ReactiveActionScope reactive = null)
{
    SceneKernel = sceneKernel;
    Actions = actions;
    ActorEntity = actorEntity;
    TriggerEntity = triggerEntity;
    Reactive = reactive;
}
```

### 11.2 ActionService.CreateExecution の変更

```csharp
private ActionExecution CreateExecution(EntityRef actor, EntityRef triggerEntity, CompiledAction definition)
{
    ActionExecutionHandle handle = new(nextExecutionId++, actor);

    ReactiveActionScope reactiveScope =
        sceneKernel.ReactiveValues?.CreateActionScope(handle, actor, triggerEntity);

    ActionExecutionContext context = new(
        sceneKernel,
        this,
        actor,
        triggerEntity,
        reactiveScope);

    return new ActionExecution(handle, context, definition.CreateRuntime());
}
```

### 11.3 ActionExecution 終了時の破棄

scope の dispose は `Complete()` に集約する。`Cancel()` は `rootRuntime.Cancel(...)` の後に `Complete(ActionExecutionResult.Canceled(...))` を呼ぶため、完了・失敗・キャンセル・置き換えの全経路で同じ dispose path を通る。

```csharp
private void Complete(ActionExecutionResult result)
{
    if (IsFinished)
        return;

    IsFinished = true;
    Context.Reactive?.Dispose();

    if (cancellationAttached)
        cancellationRegistration.Dispose();

    completionSource.TrySetResult(result);
}
```

## 12. ReactiveActionScope

```csharp
public sealed class ReactiveActionScope : IDisposable
{
    private readonly List<IReactiveBinding> bindings = new();

    public ReactiveFloatBinding Bind(in ReactiveFloat value) { ... }
    public ReactiveIntBinding Bind(in ReactiveInt value) { ... }
    public ReactiveBoolBinding Bind(in ReactiveBool value) { ... }
    public ReactiveVector3Binding Bind(in ReactiveVector3 value) { ... }
    public ReactiveEntityRefBinding Bind(in ReactiveEntityRef value) { ... }

    public void Dispose()
    {
        for (int i = 0; i < bindings.Count; i++)
            bindings[i].Dispose();

        bindings.Clear();
    }
}
```

## 13. Binding

### 13.1 共通 interface

```csharp
public interface IReactiveBinding : IDisposable
{
    bool IsValid { get; }
    bool IsDirty { get; }
}
```

### 13.2 初期版 binding の責務

- Snapshot 値のキャッシュ
- ValueStore handle の遅延取得
- `Version` 比較による watched 再読込
- Continuous source の `Read()` 時評価
- failure policy の適用

`Subscribe()` を使う場合だけ `EventSubscription` を保持して `Dispose()` する。

ただし初期版は polling + handle 読みで十分であり、購読必須ではない。

## 14. Entity 解決

### 14.1 Self / Trigger

```csharp
case ReactiveEntitySourceKind.Self:
    return context.ActorEntity.IsValid
        ? ReactiveResult<EntityRef>.Ok(context.ActorEntity)
        : ReactiveResult<EntityRef>.Fail(...);

case ReactiveEntitySourceKind.TriggerEntity:
    return context.TriggerEntity.IsValid
        ? ReactiveResult<EntityRef>.Ok(context.TriggerEntity)
        : ReactiveResult<EntityRef>.Fail(...);
```

### 14.2 TargetReference

Action 実行中の解決では既存 `BC.ActionSystem.ActionTargetResolver.Resolve()` を使う。

```csharp
int count = ActionTargetResolver.Resolve(actionContext, target, buffer);
```

制約:

- 0 件は `TargetNotFound`
- 2 件以上は `MultipleTargetsNotAllowed`

### 14.3 EntityAlive

`ReactiveBoolSourceKind.EntityAlive` はまず `SceneKernel.EntitiesRegistry?.IsAlive(entity)` を使う。

必要なら application registry へのフォールバックも検討するが、初期版は scene registry 優先で十分。

## 15. Editor 表示

基本 UI は source kind を先頭に持つ IMGUI PropertyDrawer とする。

```text
Speed
  Source: Literal / EntityValueStore / KernelValueStore / Distance
  Evaluation: Snapshot / Watched / Continuous
  Failure: FailAction / UseFallback
```

既存の `ValueKeyReferenceDrawer` と `SignalReferenceDrawer` の作りに寄せる。

初期 validation 対象:

- ValueStore source なのに key 未設定
- ValueStore source なのに `Continuous`
- Transform source なのに `Watched`
- `ReactiveEntityRef` で `TargetReference.Selection = All`
- `UseFallback` なのに fallback 未設定

初期版では editor で確定検証しないもの:

- 「TriggerEntity が常に入る action かどうか」の完全判定
- 実行時 registry に存在する target 数の事前保証

これらは runtime failure に委ねる。

## 16. 使用例

```csharp
private sealed class Runtime : IActionNodeRuntime
{
    private readonly ReactiveVector3 targetPositionSpec;
    private readonly ReactiveFloat arriveDistanceSpec;

    private ReactiveVector3Binding targetPosition;
    private ReactiveFloatBinding arriveDistance;
    private bool initialized;

    public ActionNodeStatus Tick(in ActionExecutionContext context, ref int remainingOperations)
    {
        if (!initialized)
        {
            if (context.Reactive == null)
                return ActionNodeStatus.Failed;

            targetPosition = context.Reactive.Bind(targetPositionSpec);
            arriveDistance = context.Reactive.Bind(arriveDistanceSpec);
            initialized = true;
        }

        ReactiveResult<Vector3> positionResult = targetPosition.Read();
        if (positionResult.Failed)
            return ActionNodeStatus.Failed;

        ReactiveResult<float> distanceResult = arriveDistance.Read();
        if (distanceResult.Failed)
            return ActionNodeStatus.Failed;

        return ActionNodeStatus.Running;
    }

    public void Cancel(in ActionExecutionContext context)
    {
        targetPosition?.Dispose();
        arriveDistance?.Dispose();
    }
}
```

## 17. 実装順

### M1: Contracts

作成対象:

- `ReactiveEvaluationMode`
- `ReactiveFailurePolicy`
- `ReactiveErrorCode`
- `ReactiveError`
- `ReactiveResult<T>`
- `ReactiveEvalContext`
- `ReactiveFloat`
- `ReactiveInt`
- `ReactiveBool`
- `ReactiveVector3`
- `ReactiveEntityRef`

この段階では `Literal` / `Self` / `TriggerEntity` だけでよい。

### M2: Resolver / Binding

作成対象:

- `ReactiveValueResolverService`
- `ReactiveActionScope`
- `ReactiveFloatBinding`
- `ReactiveIntBinding`
- `ReactiveBoolBinding`
- `ReactiveVector3Binding`
- `ReactiveEntityRefBinding`

この段階では service の `ITickable` 化は不要。

### M3: ValueStore source

対応対象:

- `ReactiveFloat`
- `ReactiveInt`
- `ReactiveBool`
- `ReactiveEntityRef`

`EntityValueStore` / `KernelValueStore` と `ValueWatchHandle<T>` を使う。

### M4: Entity / Transform source

対応対象:

- `ReactiveEntityRef -> TargetReference`
- `ReactiveVector3 -> EntityTransformPosition / EntityTransformForward / Add / Direction`
- `ReactiveFloat -> Distそうしたら
- `ReactiveBool -> EntityAlive / CompareNumber`

### M5: Action 統合

変更対象:

- `SceneKernel`
- `ActionExecutionContext`
- `ActionService.CreateExecution()`
- `ActionExecution.Complete()`

注記:

- `Cancel()` 自体には dispose ロジックを重ねず、`Complete()` 経由で一元化する。

### M6: Editor

作成対象:

- `Assets/Scripts/Editor/ReactiveValue/ReactiveValueDrawerBase.cs`
- `Assets/Scripts/Editor/ReactiveValue/ReactiveFloatDrawer.cs`
- `Assets/Scripts/Editor/ReactiveValue/ReactiveIntDrawer.cs`
- `Assets/Scripts/Editor/ReactiveValue/ReactiveBoolDrawer.cs`
- `Assets/Scripts/Editor/ReactiveValue/ReactiveVector3Drawer.cs`
- `Assets/Scripts/Editor/ReactiveValue/ReactiveEntityRefDrawer.cs`

方針:

- IMGUI の `CustomPropertyDrawer` ベースで実装する。Odin 専用 drawer には寄せない。
- `ValueKeyReference` の選択 UI は既存 drawer を再利用し、ReactiveValue 側で二重実装しない。
- `SourceKind` ごとの `EvaluationMode` 制約は inspector で自動補正する。runtime で reject される `Watched` / `Continuous` の組み合わせを極力保存させない。
- `ReactiveVector3` の Transform 系 payload は内側の `ReactiveTransformSourceKind` を wrapper 側 `SourceKind` に合わせて正規化する。
- `ReactiveEntityRef.TargetReference` は `EntityTargetReference` の serialized shape をそのまま描き、`TagSearch` のときだけ `Selection` / `Tag` を表示する。
- `ReactiveFailurePolicy.UseFallback` のときだけ fallback field を表示する。`ReactiveEntityRef` は `fallbackKind`、それ以外は `fallbackValue` を描く。
- M6 では production 用の確認 host component は追加しない。drawer 自体の完成を優先し、editor automation test は M7 へ送る。

### M7: Test

作成対象:

- `Assets/Tests/EditMode/ReactiveValue/ReactiveValueTestUtility.cs`
- `Assets/Tests/EditMode/ReactiveValue/ReactiveValueM7EditorDrawerTests.cs`
- `Assets/Scripts/Editor/ReactiveValue/ReactiveValueDrawerTestHost.cs`

方針:

- M1-M5 の既存 EditMode runtime fixture は維持し、M7 で同じ runtime ケースを重複追加しない。
- M7 の主対象は M6 editor drawer regression であり、runtime の新機能追加ではない。
- test assembly は参照ゼロのまま保ち、`BC.Base` / `BC.ActionSystem` / `BC.Editor` 型アクセスは reflection helper 経由で行う。
- drawer test は UI automation に寄せず、`SerializedObject` / `SerializedProperty` と `PropertyDrawer` の contract test に留める。
- `ReactiveValueDrawerTestHost` は editor-only の `ScriptableObject` とし、production runtime host component は追加しない。

M7 で固定する保証:

- `ReactiveFloatDrawer` が Literal + Watched を Snapshot へ正規化する。
- `ReactiveIntDrawer` が EntityValueStore + Continuous を Watched へ正規化する。
- `ReactiveBoolDrawer` が EntityAlive + Watched を Snapshot へ正規化する。
- `ReactiveVector3` の Transform payload が wrapper 側 `SourceKind` に合わせて `ReactiveTransformSourceKind` を矯正する。
- `ReactiveEntityRefDrawer` が TargetReference + Watched を Snapshot へ正規化する。
- `Distance` / `CompareNumber` / `Direction` / `TargetReference(TagSearch)` / `UseFallback` の inspector 高さ契約が崩れない。

既存 fixture が引き続き担保する runtime coverage:

- Literal float resolves
- Self entity resolves
- Trigger entity resolves
- EntityValueStore float resolves
- EntityValueStore watch detects version change
- KernelValueStore bool resolves
- Transform position resolves
- Missing trigger fails
- Invalid entity fails
- TargetReference multiple target fails for `ReactiveEntityRef`
- Action scope disposes bindings on completion

検証:

- まず editor drawer/test/helper ファイルの diagnostics を確認する。
- その後 `Tools/Run-UnityTests.ps1 -Platform EditMode -TestFilter BC.Base.Tests.ReactiveValueM7EditorDrawerTests` のように fixture 単位で focused run する。
- Unity batchmode が LicensingClient 初期化で不安定な環境なので、M7 の実運用上の完成条件は diagnostics clean と focused fixture 実行を基本にする。

## 18. 現行コードに合わせた重要修正点

この仕様書は初期案から以下を修正している。

1. `SceneKernel` の全 service が constructor で組み立てられる前提を削除した。`ValueStore` や `Events` は installer 注入である。
2. `ReactiveValueResolverService` の初期版 `ITickable` 必須を外した。`Continuous` は binding の `Read()` で十分である。
3. `ReactiveFailurePolicy.IgnoreNode` を初期版から外した。現行 `ActionNodeStatus` 契約では支えきれない。
4. inspector 固定の `Explicit EntityRef` source を外した。現行 `EntityRef` は runtime ID/version であり authoring 安定性がない。
5. Action の target 解決は既存 `ActionTargetResolver` 再利用を前提にした。resolver の三重化を避ける。
6. CameraPath 本体の reactive 化は初期導入対象から外し、まず Action 引数で成立させる方針にした。

## 19. 最終方針

BombCourier 版 ReactiveValue の最初の完成ラインは以下。

```text
Action の float / int / bool / Vector3 / EntityRef 引数を、
Literal / Self / TriggerEntity / EntityValueStore / KernelValueStore / Transform から取得できる。

ValueStore は ValueWatchHandle<T> による watched 読み。
Transform は Continuous polling。
Action 終了時に binding を破棄。
失敗時は default を返さず Action failed。
```

このラインであれば、現在の BombCourier のコード規模に対して過剰すぎず、後から必要な範囲だけ拡張できる。
