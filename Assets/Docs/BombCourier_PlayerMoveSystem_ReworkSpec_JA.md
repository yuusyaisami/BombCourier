# BombCourier Player / Entity Movement Rework Specification

- Document ID: `BombCourier_PlayerEntityMovement_ReworkSpec_JA`
- Status: Draft for implementation planning
- Target: BombCourier current `main`
- Scope: Player / Entity movement motor, ground probe, slope policy, step assist, collision classification, moving platform support, external momentum, public API compatibility
- Non-goal: animation asset redesign, camera redesign, moving platform rail authoring redesign, ragdoll internals redesign

---

## 0. 結論

現在の `EntityMoveMotorMB` は、単に「少し肥大化している」のではなく、**Player / Entity の移動に関するほぼ全責務を 1 クラスへ詰め込んだ God Class** である。

現状の主な責務は以下である。

- Rigidbody / CapsuleCollider の自動解決と runtime 設定
- 入力意図の保持
- 自動移動 `MoveToAsync`
- 接地 SphereCast
- 斜面角度判定
- 水平速度更新
- 垂直速度更新
- 重力 / ジャンプ / coyote time / jump buffer
- moving platform 速度追従
- platform 速度継承
- external velocity / impulse
- cushion bounce / high jump
- OnCollisionEnter / OnCollisionStay
- 接触法線による壁速度除去
- 接触押し返し
- 小段差 Step Assist
- motion lock / death / revive
- ValueStore への runtime value publish

これ以上このクラスに `FootSnap` や `PlatformLaunch` を直接追加するのは、短期的には動くが、長期的には破綻する。  
必要なのは「機能追加」ではなく、**単一の Tick Orchestrator と複数 Solver への分解**である。

重要な設計判断:

> クラス分割は行うが、各機能を安易に MonoBehaviour 化して個別 `FixedUpdate` を持たせてはいけない。  
> Player movement は順序依存が強い。`EntityMoveMotorMB` は薄い Facade / Orchestrator として残し、内部 Solver を明示順序で呼ぶ。

---

## 1. 現行コード調査結果

### 1.1 `EntityMoveMotorMB`

現在の中心クラス。  
`Rigidbody` と `CapsuleCollider` を要求し、`EntityMoveController`, `IEntityMoveAnimationSource`, `IEntityVelocitySource`, `ICushionImpactSource` を兼ねる。

主要設定は以下に分かれている。

```text
References
Speed
Acceleration
Jump / Gravity
Ground Probe
Step Assist
Moving Platform
External Momentum
Cushion
Contact Push
Runtime Debug
```

これ自体が、責務が広すぎる証拠である。

現在の Tick は概ね以下である。

```text
FixedUpdate
  ├─ gate / lock 判定
  ├─ ProbeGround
  ├─ platform leave 判定
  ├─ UpdatePlatformMotion
  ├─ landing rebase
  ├─ UpdatePlanarVelocity
  ├─ UpdateVerticalVelocity
  ├─ UpdateExternalVelocity
  ├─ TryStepUp
  ├─ ApplyCurrentVelocityToBody
  ├─ StorePlatformPose
  └─ PublishRuntimeValues
```

問題は、接地、足場、速度、段差、最終 Rigidbody 反映が同一メソッド群内で密結合していること。

### 1.2 `EntityMoveController`

`EntityMoveController` は ValueStore から `CanMoveByInput`, `CanMoveBySystem`, `Move.BaseSpeed`, `SprintMultiplier`, `JumpHeightMultiplier` を読む基底クラスである。

これは残すべき。  
ただし今後は `EntityMoveMotorMB` が直接すべての速度解決を持つのではなく、`MoveRuntimeContext` / `MoveStatSnapshot` として Solver へ渡す。

### 1.3 `PlayerMoveController`

`PlayerMoveController` は入力を読み、camera-relative な移動方向へ変換し、`moveMotor.SetMoveIntent(...)` へ渡す。  
また `AddImpulse`, `SetPlanarVelocity`, `SetVerticalVelocity`, `MoveToAsync`, `EnterMotionLock`, `ReviveFromCheckpoint` などを外部向けに再公開している。

ここは基本的に残す。  
ただし `PlayerMoveController` が依存する `EntityMoveMotorMB` の public API は互換維持が必要。

### 1.4 `EntityMovementContracts`

既存の `IEntityVelocitySource` / `IEntityMoveAnimationSource` は重要な公開契約である。

特に `IEntityMoveAnimationSource.CurrentPlanarSpeed` は「移動床の速度や吹っ飛び速度を基本的に含めない」前提になっている。  
これは今後の速度チャンネル設計でも守る必要がある。

### 1.5 `MovingPlatformMB` / `SupportMotionUtility`

Moving Platform 側は悪くない。  
`SupportMotionSnapshot` は以下を持っている。

```text
SourceTransform
SourceRigidbody
SourceOrigin
PassengerPoint
SourcePositionDelta
SourceRotationDelta
PassengerDelta
SourceLinearVelocity
SourceAngularVelocity
PassengerVelocity
```

これは Player 側の慣性設計に流用できる。

`MovingPlatformMB` は `accumulatedPositionDelta`, `accumulatedRotationDelta`, `accumulatedMotionOrigin` を持ち、`TryGetSupportMotion` から乗客用の移動量を渡せる。  
この設計は維持するべき。

### 1.6 `RigidbodySupportRiderMB`

汎用 Rigidbody 用の moving platform 追従補助。  
接触している support collider を拾い、`SupportMotionUtility.TryGetSupportMotion` から `PassengerDelta` / `PassengerVelocity` を取得し、位置補正と速度補正を適用する。

ただし Player は `EntityMoveMotorMB` があるため `EntityMB.ShouldSkipSupportRider` により自動追加対象から外れている。  
つまり Player の support motion は `EntityMoveMotorMB` 内で独自処理される。

### 1.7 `Player.prefab`

Player root は以下を持つ。

- `Rigidbody`
- `CapsuleCollider`
- `EntityMoveMotorMB`
- `PlayerMoveController`
- `EntityLandingImpactMB`
- `PlayerMB`
- `PlayerRagdollControllerMB`
- 追加の `BoxCollider`

重要な問題:

```text
CapsuleCollider:
  radius = 0.5
  height = 2
  center = (0, 0, 0)

BoxCollider:
  isTrigger = false
  size = (0.75, 0.75, 1)
  center = (0, -0.625, 0)
```

この BoxCollider は通常衝突に参加している。  
しかし `EntityMoveMotorMB` の接地判定・カプセル占有判定・Step Assist は `bodyCollider` として解決した `CapsuleCollider` 前提で動く。

これはかなり危険。  
Motor の「想定形状」と Unity Physics の「実衝突形状」が一致していない。

結論:

> Player root の追加 BoxCollider は削除するか、正式な FootCollider として仕様化し、Motor 側の Body Geometry に登録すること。  
> 未登録の非 Trigger Collider を Player root に置いてはいけない。

### 1.8 `EntityLandingImpactMB`

`EntityLandingImpactMB` は `moveMotor.IsGrounded`, `moveMotor.VerticalVelocity`, `moveMotor.GroundPoint`, `moveMotor.CurrentVelocity` に依存して hard landing を判定している。  
移動システム再設計時も、これらの意味は壊してはいけない。

特に `VerticalVelocity` は着地フレームで補正済みになるため、`EntityLandingImpactMB` は `previousVerticalVelocity` を保持して衝撃量を計算している。  
新システムでも「着地前の下向き速度」を取れる API を提供するのが望ましい。

---

## 2. 現行設計の問題

### 2.1 God Class 化

`EntityMoveMotorMB` は単一の Player Motor ではなく、以下を同時に抱えている。

```text
Input Buffer
Runtime Gate
Ground Probe
Slope Detection
Velocity Solver
Jump Solver
Step Solver
Support Motion Solver
External Momentum
Collision Resolver
Cushion Handler
Contact Push Handler
AutoMove Driver
Death / MotionLock Controller
Runtime Value Publisher
Body Bootstrapper
Physics Material Factory
```

これは設計上アウト。  
変更範囲が広すぎ、テスト対象が曖昧で、バグの原因が追いにくい。

### 2.2 「接地」と「衝突」が混ざっている

`ProbeGround` で接地判定している一方、`ResolveCollisionVelocityConstraints` でも接触法線から `lastGroundedTime` や `verticalVelocity` を変えている。

つまり接地の authority が二重化している。

```text
Ground Probe
  - SphereCast で接地

Collision Contact
  - normal.y で接地扱い
  - normal.y が低いと壁扱い
```

これにより、崖際、段差角、斜面で「足元なのに壁」「壁なのに接地」などが起こりうる。

### 2.3 Capsule 下端の丸みを設計が吸収していない

CapsuleCollider の下端は丸い。  
崖際や段差角で接触すると、接触法線は斜めになる。

現状の `ResolveCollisionVelocityConstraints` は `Mathf.Abs(upDot) < minGroundDot` を壁扱いして `RemoveIntoWallVelocity` を実行する。  
これは「足元の斜め接触」まで壁として処理する危険がある。

### 2.4 Foot Snap が存在しない

現状は `groundedStickVelocity = -3` で接地中に下向き速度を入れている。  
しかしこれは「近い床へ位置を吸着する処理」ではない。

必要なのは velocity ではなく、**ground probe の hit distance を使った制御された snap correction** である。

### 2.5 Step Assist が Motor 内部で直接 Rigidbody 位置を書き換えている

`TryStepUp` は条件成立時に `bodyRigidbody.position = snappedPosition` する。  
この書き換え自体は短期的には動くが、設計としては悪い。

理由:

- Step Assist が最終 apply の一部になっていない
- Platform carry と競合する
- Collision resolution と順序が曖昧
- GroundSnap と統合できない
- テストしにくい

Step Assist は `PositionCorrection` を返し、最終的な `RigidbodyMotionApplier` が一括反映するべき。

### 2.6 Moving Platform 慣性が「速度継承」止まり

現在は platform 速度を接地中に加算し、離地/ジャンプ時に `inheritedPlatformVelocity` へ入れる構造。  
しかし「高速上昇 Platform が急停止した瞬間に Player が上へ吹っ飛ぶ」には不十分。

必要なのは velocity ではなく **support acceleration / lost support velocity** の検出である。

### 2.7 Player の実 Collider が Motor から見えていない

Prefab 上の非 Trigger `BoxCollider` は `EntityMoveMotorMB` の `bodyCollider` ではない。  
にもかかわらず通常物理に参加する。

これは絶対に整理する。

---

## 3. 設計方針

### 3.1 `EntityMoveMotorMB` は残す

外部から見た API を大きく壊すべきではない。  
以下の既存利用があるため。

- `PlayerMoveController`
- `PlayerMB`
- `EntityLandingImpactMB`
- `PlayerAnimationMB`
- `PlayerRagdollControllerMB`
- Cushion / Bomb / Impact 系
- ValueStore runtime values

ただし、`EntityMoveMotorMB` は実装本体ではなくなる。

新しい責務:

```text
EntityMoveMotorMB
  - Unity lifecycle
  - serialized settings の保持または参照
  - public API 互換
  - Solver 初期化
  - FixedUpdate の順序制御
  - OnCollision callbacks の contact buffer への転送
  - Rigidbody への最終 apply
  - Runtime values publish
```

### 3.2 Solver は原則 Pure C# Class

`GroundProbeSolver`, `StepAssistSolver`, `SupportInertiaSolver` などは MonoBehaviour にしない。  
理由は FixedUpdate 順序を分散させないため。

例外:

- Gizmo 表示用 Debug MonoBehaviour
- Authoring / Validator MonoBehaviour
- 外部システムとの Unity event 接続が必要なもの

### 3.3 Authority を明確化する

```text
Ground authority:
  GroundProbeSolver + ContactClassifier の統合結果

Velocity authority:
  VelocityChannelSolver + MomentumSolver

Rigidbody apply authority:
  RigidbodyMotionApplier

Platform authority:
  SupportMotionTracker

Collision authority:
  ContactBuffer + CollisionConstraintSolver
```

各 Solver が勝手に Rigidbody を書き換えない。  
最終 apply 以外で `bodyRigidbody.position`, `linearVelocity`, `rotation` を触らない。

---

## 4. 推奨フォルダ / ファイル構成

```text
Assets/Scripts/Movement/
├─ Contracts/
│  ├─ EntityMovementContracts.cs              // 既存。維持・必要なら拡張
│  ├─ EntityMoveCommandContracts.cs           // AddImpulse/SetVelocity/Lock/Revive 等
│  ├─ EntityGroundContracts.cs                // IGroundInfoSource
│  └─ EntitySupportMotionContracts.cs         // Support tracker view
│
├─ Core/
│  ├─ EntityMoveController.cs                 // 既存。ValueStore handle provider として維持
│  ├─ EntityMoveMotorMB.cs                    // Facade / Orchestrator
│  ├─ EntityMoveSettings.cs                   // Serializable aggregate settings
│  ├─ EntityMoveRuntimeState.cs               // runtime mutable state
│  ├─ EntityMoveIntent.cs                     // input / auto move intent
│  ├─ EntityMoveFrameContext.cs               // 1 tick の入力束
│  ├─ EntityMoveSolveResult.cs                // 1 tick の解決結果
│  └─ EntityMoveStateResolver.cs              // Idle/Moving/Jumping/Falling 決定
│
├─ Body/
│  ├─ MovementBodyResolver.cs                 // Rigidbody / Capsule / optional foot collider 解決
│  ├─ MovementBodyGeometry.cs                 // feet/head/capsule points
│  ├─ MovementBodyGeometryUtility.cs
│  ├─ MovementColliderPolicyValidator.cs      // Player root の未登録 Collider 検出
│  └─ MovementPhysicsMaterialFactory.cs       // low friction material
│
├─ Ground/
│  ├─ GroundProbeSolver.cs
│  ├─ GroundProbeSettings.cs
│  ├─ GroundHitInfo.cs
│  ├─ GroundSurfaceKind.cs
│  ├─ GroundSnapSolver.cs
│  ├─ GroundSnapSettings.cs
│  ├─ SlopePolicySolver.cs
│  └─ SlopePolicySettings.cs
│
├─ Collision/
│  ├─ MoveContactBuffer.cs
│  ├─ MoveContactInfo.cs
│  ├─ MoveContactKind.cs
│  ├─ ContactClassifier.cs
│  ├─ CollisionConstraintSolver.cs
│  └─ ContactPushEmitter.cs
│
├─ Velocity/
│  ├─ VelocityChannels.cs
│  ├─ PlanarVelocitySolver.cs
│  ├─ VerticalVelocitySolver.cs
│  ├─ ExternalMomentumSolver.cs
│  ├─ JumpSolver.cs
│  └─ VelocityComposer.cs
│
├─ Step/
│  ├─ StepAssistSolver.cs
│  ├─ StepAssistSettings.cs
│  ├─ StepProbeResult.cs
│  └─ StepCandidate.cs
│
├─ Support/
│  ├─ SupportMotionTracker.cs
│  ├─ SupportMotionState.cs
│  ├─ SupportInertiaSolver.cs
│  ├─ SupportInertiaSettings.cs
│  └─ SupportSnapPolicy.cs
│
├─ AutoMove/
│  ├─ AutoMoveDriver.cs
│  └─ AutoMoveState.cs
│
├─ Reactions/
│  ├─ CushionImpactHandler.cs
│  ├─ CushionHighJumpBuffer.cs
│  └─ MotionLockController.cs
│
├─ Runtime/
│  ├─ MoveRuntimeValuePublisher.cs
│  └─ MoveDebugSnapshot.cs
│
└─ Debug/
   ├─ MovementGizmoDrawerMB.cs
   └─ MovementDebugOverlayMB.cs
```

---

## 5. 主要クラス責務

### 5.1 `EntityMoveMotorMB`

残す public API:

```csharp
public Vector3 PlanarVelocity { get; }
public float VerticalVelocity { get; }
public Vector3 ExternalVelocity { get; }
public Vector3 PlatformVelocity { get; }
public Vector3 CurrentVelocity { get; }
public Vector3 ControlledPlanarVelocity { get; }
public float CurrentPlanarSpeed { get; }
public float NormalizedPlanarSpeed { get; }

public bool IsGrounded { get; }
public bool IsSprinting { get; }
public bool IsDead { get; }
public bool CanMoveByInput { get; }
public bool CanMoveBySystem { get; }
public bool CanProcessMoveInput { get; }
public bool IsAutoMoveActive { get; }

public Vector3 GroundNormal { get; }
public Vector3 GroundPoint { get; }
public Transform GroundTransform { get; }

public void SetMoveIntent(...);
public void ClearMoveIntent();
public void AddImpulse(Vector3 impulseVelocity);
public void SetExternalVelocity(Vector3 velocity);
public void ClearExternalVelocity();
public void SetPlanarVelocity(Vector3 velocity);
public void SetVerticalVelocity(float velocity);
public void EnterMotionLock(EntityMoveState lockedState);
public void ExitMotionLock(Vector3 releaseImpulse, float preservedVelocityRate = 0.35f);
public void EnterDeadState(ValueModifierTagId moveLockTag);
public void ReviveFromCheckpoint();
public UniTask<bool> MoveToAsync(...);
public void CancelAutoMove();
```

内部ではこれらを `EntityMoveRuntimeState` / `VelocityChannels` / `AutoMoveDriver` へ委譲する。

### 5.2 `EntityMoveRuntimeState`

1 tick をまたいで保持する mutable state。

```csharp
public sealed class EntityMoveRuntimeState
{
    public EntityMoveState MoveState;

    public bool IsGrounded;
    public bool WasGrounded;
    public float LastGroundedTime;
    public float LastJumpTime;
    public float LastSupportLaunchTime;

    public Vector3 PlanarVelocity;
    public float VerticalVelocity;
    public Vector3 ExternalVelocity;
    public Vector3 InheritedSupportVelocity;
    public Vector3 PlatformVelocity;

    public GroundHitInfo Ground;
    public SupportMotionState Support;

    public bool MotionLocked;
    public bool IsDead;
}
```

### 5.3 `EntityMoveIntent`

入力と自動移動を同一形式に正規化する。

```csharp
public struct EntityMoveIntent
{
    public Vector3 WorldMoveDirection;
    public bool HasMoveInput;
    public bool SprintHeld;
    public bool JumpPressed;
    public bool JumpHeld;
    public bool IsAutoMove;
}
```

### 5.4 `MovementBodyResolver`

責務:

- Rigidbody 解決
- CapsuleCollider 解決
- optional FootCollider 解決
- CharacterController 無効化
- Rigidbody runtime 設定
- 未登録 Collider 検査

必須警告:

```text
Player root または子に未登録の非 Trigger Collider が存在する場合、
MovementColliderPolicyValidator は Warning ではなく Error 扱いにする。
```

現状の Player root `BoxCollider` はここで検出されるべき。

### 5.5 `MovementBodyGeometry`

毎 tick 計算する身体形状。

```csharp
public readonly struct MovementBodyGeometry
{
    public readonly Vector3 BodyPosition;
    public readonly Vector3 CapsuleCenter;
    public readonly Vector3 CapsuleBottomSphereCenter;
    public readonly Vector3 CapsuleTopSphereCenter;
    public readonly float CapsuleRadius;
    public readonly float FeetY;
    public readonly float HeadY;
    public readonly float FootBandTopY;
}
```

`ContactClassifier`, `GroundProbeSolver`, `StepAssistSolver` はこの geometry を共有する。

### 5.6 `GroundProbeSolver`

責務:

- Foot probe の SphereCast / CapsuleCast
- ground candidate の収集
- walkable / steep / wall / ceiling の分類
- best ground の選択

```csharp
public readonly struct GroundHitInfo
{
    public readonly bool IsValid;
    public readonly Collider Collider;
    public readonly Transform Transform;
    public readonly Vector3 Point;
    public readonly Vector3 Normal;
    public readonly float Distance;
    public readonly float Angle;
    public readonly GroundSurfaceKind SurfaceKind;
    public readonly bool IsWalkable;
}
```

```csharp
public enum GroundSurfaceKind
{
    None,
    Walkable,
    SteepSlope,
    Wall,
    Ceiling,
    LedgeEdge
}
```

### 5.7 `GroundSnapSolver`

責務:

- 接地中または接地直後に、近い walkable ground へ吸着補正を返す
- Rigidbody を直接変更しない
- `PositionCorrection` を返す

適用条件:

```text
- GroundSnapSettings.Enabled
- 現在または直前まで Grounded
- Jump 直後ではない
- PlatformLaunch 直後ではない
- verticalVelocity が明確な上昇中ではない
- GroundProbe が Walkable を検出
- hit.distance <= MaxSnapDistance
- steep slope / wall / ceiling ではない
```

設定例:

```csharp
[Serializable]
public sealed class GroundSnapSettings
{
    public bool Enabled = true;
    public float MaxSnapDistance = 0.28f;
    public float SnapSpeed = 14.0f;
    public float MaxSnapDistancePerTick = 0.18f;
    public float DisableAfterJumpTime = 0.12f;
    public float DisableAfterSupportLaunchTime = 0.16f;
}
```

### 5.8 `SlopePolicySolver`

斜面を Unity Physics 任せにしない。

```csharp
public enum SlopePolicyMode
{
    StickOnWalkableSlope,
    SlideOnSteepSlope,
    FullyPhysical
}
```

仕様:

```text
Walkable:
  - 入力方向を ground tangent plane に投影
  - 無入力時は滑らない
  - 下向き stick velocity は最小限

SteepSlope:
  - Grounded ではなく SteepSlope 状態
  - slide acceleration を明示的に加算
  - slide max speed を持つ

FullyPhysical:
  - 特殊ギミック / debug 用
  - Player 標準では使わない
```

### 5.9 `MoveContactBuffer`

`OnCollisionEnter/Stay` で得た ContactPoint を一時蓄積する。  
`EntityMoveMotorMB` は callback で buffer に積むだけにする。

```csharp
public sealed class MoveContactBuffer
{
    public void ClearForNextPhysicsTick();
    public void Add(Collision collision, MovementBodyGeometry geometry);
    public ReadOnlySpan<MoveContactInfo> Contacts { get; }
}
```

### 5.10 `ContactClassifier`

接触点を normal だけで分類しない。  
必ず接触点の高さと身体 geometry を見る。

```csharp
public enum MoveContactKind
{
    None,
    FootGround,
    FootEdge,
    BodyWall,
    Ceiling,
    SteepSlope,
    SupportCandidate,
    PushTarget
}
```

分類基準:

```text
FootGround:
  contact.point.y <= geometry.FootBandTopY
  normal.y > 0
  walkable angle or ground probe と整合

FootEdge:
  contact.point.y <= geometry.FootBandTopY
  normal.y > 0
  horizontal normal component が強い
  wall push から除外

BodyWall:
  contact.point.y > geometry.FootBandTopY
  normal の水平成分が強い
  進行方向を塞ぐ

Ceiling:
  contact.point.y >= geometry.HeadY - ceilingBand
  normal.y < negative threshold

SteepSlope:
  normal.y > 0
  angle > maxGroundAngle
```

### 5.11 `CollisionConstraintSolver`

責務:

- wall velocity removal
- ceiling vertical clamp
- foot contact による last grounded 更新
- contact push とは分離

重要:

> FootGround / FootEdge contact では `RemoveIntoWallVelocity` を呼ばない。

### 5.12 `StepAssistSolver`

責務:

- 前方の低い障害物検出
- 上側空間の確認
- 上がった先の Walkable ground 検出
- capsule occupancy 確認
- `PositionCorrection` を返す

禁止:

```text
StepAssistSolver 内で Rigidbody.position を直接書き換えない。
```

設定例:

```csharp
[Serializable]
public sealed class StepAssistSettings
{
    public bool Enabled = true;
    public float MaxStepHeight = 0.32f;
    public float ForwardProbeDistance = 0.28f;
    public float LowerProbeHeight = 0.08f;
    public float UpperClearanceSkin = 0.03f;
    public float StepDownProbeDistance = 0.36f;
    public float MinIntentMagnitude = 0.05f;
    public float SnapSpeed = 12.0f;
}
```

### 5.13 `SupportMotionTracker`

責務:

- current support の検出
- `SupportMotionUtility.TryGetSupportMotion` の呼び出し
- current / previous support velocity の保持
- support changed / lost / gained の検出

```csharp
public struct SupportMotionState
{
    public bool HasSupport;
    public bool HadSupport;
    public Collider Collider;
    public Transform Transform;

    public Vector3 PassengerDelta;
    public Vector3 PassengerVelocity;
    public Vector3 PreviousPassengerVelocity;
    public Vector3 PassengerAcceleration;

    public Vector3 SupportPoint;
}
```

### 5.14 `SupportInertiaSolver`

高速上昇 Platform 急停止で Player を上へ飛ばす。

設定例:

```csharp
[Serializable]
public sealed class SupportInertiaSettings
{
    public bool Enabled = true;
    public float UpwardLaunchMinPreviousVelocity = 4.0f;
    public float UpwardLaunchMinLostVelocity = 3.5f;
    public float UpwardLaunchRetainRate = 0.85f;
    public float HorizontalRetainRate = 0.35f;
    public float MaxLaunchVelocity = 22.0f;
    public float DisableGroundSnapTime = 0.16f;
    public float DisableSupportReattachTime = 0.08f;
}
```

発火条件:

```text
- 前 tick で support があった
- 現 tick でも同一または関連 support 上にいる、または support lost 直後
- previousPassengerVelocity.y >= UpwardLaunchMinPreviousVelocity
- previousPassengerVelocity.y - currentPassengerVelocity.y >= UpwardLaunchMinLostVelocity
- player が dead / motion locked ではない
- cushion bounce / explicit jump と競合しない
```

処理:

```text
launchVelocityY = clamp(
    (previousY - currentY) * UpwardLaunchRetainRate,
    0,
    MaxLaunchVelocity
)

VerticalVelocity = max(VerticalVelocity, launchVelocityY)
GroundSnap を DisableGroundSnapTime だけ無効化
Support 再吸着を DisableSupportReattachTime だけ抑制
MoveState は Falling ではなく AirborneLaunch または Jumping 相当にする
```

`EntityMoveState` に `Launched` を追加するかは検討。  
既存 Animator 互換を優先するなら `Jumping` 扱いでよい。

### 5.15 `VelocityChannels`

速度を意味別に分ける。

```csharp
public struct VelocityChannels
{
    public Vector3 InputPlanar;
    public float Vertical;
    public Vector3 External;
    public Vector3 SupportCarry;
    public Vector3 InheritedSupport;
    public Vector3 ConstraintCorrectionVelocity;
}
```

公開値との対応:

```text
PlanarVelocity:
  InputPlanar

ControlledPlanarVelocity:
  InputPlanar の水平成分

CurrentPlanarSpeed:
  InputPlanar の水平速度
  support / external は含めない

ExternalVelocity:
  External

PlatformVelocity:
  SupportCarry

CurrentVelocity:
  InputPlanar + Vertical + External + InheritedSupport + SupportCarry
```

これは既存 `IEntityMoveAnimationSource` のコメントと整合する。

### 5.16 `RigidbodyMotionApplier`

最終 apply 専用。

入力:

```csharp
public struct RigidbodyMoveCommand
{
    public Vector3 FinalVelocity;
    public Vector3 PositionCorrection;
    public bool HasPositionCorrection;
}
```

仕様:

```text
- Solver は Rigidbody を直接変更しない
- 最終的に Applier が linearVelocity と position correction を一括適用
- position correction は GroundSnap / StepAssist / SupportCarry の順序を決めた上で合成
```

---

## 6. 新 FixedUpdate 順序

```text
EntityMoveMotorMB.FixedUpdate
  01. Resolve dt / runtime gates
  02. Refresh ValueStore move gates
  03. Resolve body geometry
  04. Drain contact buffer from previous physics callbacks
  05. Probe ground
  06. Classify contacts
  07. Resolve support motion
  08. Resolve support inertia / launch
  09. Resolve move intent / auto move intent
  10. Resolve jump buffer / coyote
  11. Resolve planar velocity
  12. Resolve slope policy
  13. Resolve vertical velocity
  14. Resolve external momentum
  15. Resolve collision constraints
  16. Resolve step assist
  17. Resolve ground snap
  18. Compose velocity channels
  19. Apply Rigidbody
  20. Resolve move state
  21. Publish runtime values
  22. Store previous frame snapshots
```

重要:

- Support inertia は GroundSnap より前に行う
- PlatformLaunch が起きた tick では GroundSnap を無効化する
- StepAssist と GroundSnap は両方 `PositionCorrection` として合成する
- Collision contact による wall constraint は foot contact を除外する

---

## 7. Player Collider Policy

### 7.1 標準構成

```text
PlayerRoot
  Rigidbody
  CapsuleCollider     // body collision
  EntityMoveMotorMB
  PlayerMoveController
  ...

Optional:
  FootProbeTransform  // collider ではなく probe origin だけ
```

### 7.2 禁止構成

```text
PlayerRoot
  CapsuleCollider
  BoxCollider(non-trigger, unregistered)
```

これは現状の Player prefab に近い。  
この構成は Motor の認識形状と実物理形状がズレるため禁止。

### 7.3 FootCollider を使う場合

どうしても足元 Collider を使うなら、以下を必須にする。

```text
- isTrigger = true
- 専用 Layer
- MovementBodyResolver に登録
- 通常物理衝突には参加させない
- GroundProbe の origin / radius 補助にのみ使う
```

---

## 8. 移行計画

### M0: 現状固定テスト追加

まだ実装変更しない。  
現在の挙動をテスト化し、退行を検出できるようにする。

追加テスト:

```text
PlayerMove_Grounded_OnFlatGround
PlayerMove_DoesNotDoubleApplyPlatformVelocity
PlayerMove_JumpKeepsExistingHeight
PlayerMove_CushionBounceStillWorks
PlayerMove_HardLandingUsesPreviousVerticalVelocity
PlayerMove_BombImpactStillKillsOrRagdolls
```

### M1: Body / Geometry 抽出

- `MovementBodyResolver`
- `MovementBodyGeometry`
- `MovementPhysicsMaterialFactory`
- `MovementColliderPolicyValidator`

を追加。

`EntityMoveMotorMB` の挙動は変えず、内部関数を委譲するだけ。

### M2: Runtime State / Intent 抽出

- `EntityMoveRuntimeState`
- `EntityMoveIntent`
- `AutoMoveDriver`

を追加。

`moveDirection`, `jumpBufferCounter`, `autoMove*` を段階的に移す。

### M3: GroundProbe 抽出

`ProbeGround` と `GetGroundProbeParameters` を `GroundProbeSolver` へ移す。

挙動はまだ変えない。  
同じ SphereCast、同じ `maxGroundAngle`、同じ `groundProbeExtraDistance` で結果一致を確認する。

### M4: ContactBuffer / ContactClassifier 導入

`OnCollisionEnter/Stay` は即処理しない。  
`MoveContactBuffer` に溜めて、FixedUpdate の決まった位置で処理する。

ここで `FootGround`, `FootEdge`, `BodyWall`, `Ceiling` の分類を導入する。

### M5: GroundSnap 導入

最初は弱く入れる。

```text
MaxSnapDistance = 0.12
SnapSpeed = 8
DisableAfterJumpTime = 0.12
DisableAfterSupportLaunchTime = 0.16
```

崖際 idle の横滑りをテストする。

### M6: StepAssist 置換

旧 `TryStepUp` を `StepAssistSolver` へ移す。  
最初はロジックをそのまま移植し、その後で以下を改善する。

- direct Rigidbody.position をやめる
- correction 返却式にする
- low speed でも input intent があれば発動可能にする
- step down と snap を統合する

### M7: SupportMotionTracker / SupportInertiaSolver 導入

既存 `UpdatePlatformMotion`, `CapturePlatformInertiaOnLeaveGround`, `InheritCurrentPlatformVelocity`, `RebaseInheritedPlatformMomentumOnLanding` を段階的に移す。

上昇 Platform 急停止 launch を追加する。

### M8: VelocityChannels / VelocityComposer 導入

`planarVelocity`, `verticalVelocity`, `externalVelocity`, `inheritedPlatformVelocity`, `platformVelocity` を構造化する。

Public API の値は既存互換にする。

### M9: Cushion / ContactPush 分離

- `CushionImpactHandler`
- `CushionHighJumpBuffer`
- `ContactPushEmitter`

へ移す。

### M10: 旧 Motor 内部関数削除

`EntityMoveMotorMB` は Orchestrator と public API だけにする。

---

## 9. 必須テスト仕様

### 9.1 Ground / Snap

```text
Flat ground:
  idle で Y が沈まない
  idle で XZ drift が出ない

Cliff edge:
  capsule 半径の一部だけ接触していても idle 横滑りしない
  input がある場合のみ移動する

Small drop:
  0.05m〜0.20m の床下がりで浮かずに追従する

Jump:
  jump 直後に snap が再接地させない
```

### 9.2 Slope

```text
0°:
  停止時に滑らない

30°:
  walkable として立てる
  入力方向は slope plane に沿う

55°:
  maxGroundAngle 付近でも jitter しない

60°:
  steep slope として明示的に slide する
  Unity physics 任せの勝手な滑りは禁止
```

### 9.3 Step

```text
0.05m step:
  低速入力でも登れる

0.15m step:
  通常入力で登れる

0.30m step:
  maxStepHeight 以下なら登れる

0.40m step:
  maxStepHeight 超過なら登れない

前方上部に壁:
  登らない
```

### 9.4 Moving Platform

```text
Horizontal platform:
  接地中は platform に運ばれる
  idle animation は歩きにならない

Rotating platform:
  passenger point velocity を使う
  中心速度だけで運ばない

Upward fast stop:
  platformVelocity.y = +18 から 0 へ急停止
  Player は jump 入力なしで上へ launch される

Launch after snap:
  launch tick では GroundSnap が無効化され、即再接地しない
```

### 9.5 External / Cushion / Landing

```text
Bomb impact:
  AddImpulse が External channel に入る
  Dead / Ragdoll 既存挙動が壊れない

Cushion bounce:
  bounce velocity が External または Bounce channel に入る
  high jump buffer が維持される

Hard landing:
  EntityLandingImpactMB が previous vertical velocity を正しく読める
  GroundPoint が正しい
```

---

## 10. 実装禁止事項

以下は禁止。

```text
- EntityMoveMotorMB にさらに巨大な private method を追加する
- FootSnap を旧 ProbeGround の末尾に雑に足す
- StepAssistSolver 内で Rigidbody.position を直接変更する
- 接触 normal.y だけで wall / ground を決める
- GroundSnap と PlatformLaunch を同時適用する
- 未登録の非 Trigger Collider を Player root に残す
- Solver ごとに MonoBehaviour + FixedUpdate を持たせる
- CharacterController へ戻す
- PhysicMaterial の摩擦だけで解決しようとする
```

---

## 11. 実装者向け最初の作業指示

最初にやるべき実装はこれ。

```text
M1.1 MovementBodyGeometry を作成
M1.2 EntityMoveMotorMB.GetCapsuleGeometry を移植
M1.3 MovementColliderPolicyValidator を作成
M1.4 Player prefab の追加 BoxCollider を検出して ErrorLog
M1.5 GroundProbeSolver を作成し、現行 ProbeGround と結果一致テスト
```

この段階では挙動を変えない。  
God Class を安全に分解するための足場を作る。

次に:

```text
M2.1 MoveContactBuffer を導入
M2.2 ResolveCollisionVelocityConstraints を ContactClassifier + CollisionConstraintSolver へ分割
M2.3 Foot contact は wall velocity removal 対象外にする
```

その後:

```text
M3.1 GroundSnapSolver を弱い設定で導入
M3.2 Cliff edge idle drift test を追加
M3.3 StepAssistSolver を correction 返却式に置換
M3.4 SupportInertiaSolver を追加
```

---

## 12. 最終的な理想像

最終的な `EntityMoveMotorMB.FixedUpdate` はこの程度まで薄くする。

```csharp
private void FixedUpdate()
{
    if (!TryBeginFrame(out EntityMoveFrameContext context))
        return;

    contactClassifier.Classify(context);
    groundProbe.Probe(context);
    supportTracker.Update(context);
    supportInertia.Resolve(context);
    autoMoveDriver.UpdateIntent(context);

    velocitySolver.Resolve(context);
    slopePolicy.Resolve(context);
    collisionConstraints.Resolve(context);
    stepAssist.Resolve(context);
    groundSnap.Resolve(context);

    velocityComposer.Compose(context, out RigidbodyMoveCommand command);
    rigidbodyApplier.Apply(command);

    stateResolver.Resolve(context);
    runtimePublisher.Publish(context);
    StorePreviousFrame(context);
}
```

この形になれば、今後の機能追加は以下のように局所化できる。

```text
滑り対策:
  GroundSnapSolver / ContactClassifier

小段差:
  StepAssistSolver

斜面:
  SlopePolicySolver

高速 Platform:
  SupportInertiaSolver

爆風 / クッション:
  ExternalMomentumSolver / CushionImpactHandler

アニメーション:
  EntityMoveStateResolver / RuntimeValuePublisher
```

---

## 13. 最重要修正ポイント

優先順位は以下。

1. Player root の未登録 `BoxCollider` を整理する
2. `EntityMoveMotorMB` を Orchestrator 化する
3. Ground authority を `GroundProbeSolver` に寄せる
4. Contact normal を高さ込みで分類する
5. GroundSnap を正式 Solver として入れる
6. StepAssist を direct Rigidbody 書き換えから correction 返却式へ変える
7. SupportInertiaSolver で Platform 急停止 launch を入れる

これをやらずに、今の `EntityMoveMotorMB` に継ぎ足すのは妥協である。  
BombCourier の移動品質を本気で上げるなら、ここは一度きちんと切り直すべき。
