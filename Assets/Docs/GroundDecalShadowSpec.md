# Ground Decal Shadow Specification

## 目的

Ground Decal Shadow は、Player / Bomb / 重要 Entity の真下に投影する gameplay shadow である。

これは物理ライトの影ではない。目的は次の通り。

- 空中の Player がどこへ着地するかを示す
- 投げた Bomb の着地点を読ませる
- キャラクターや重要オブジェクトの接地感を補強する
- 斜面、階段、段差の多いマップでも視認性を落とさない

通常の URP shadow は雰囲気用、Ground Decal Shadow は操作性用として分離する。

---

## 実装コンポーネント

Runtime component:

```text
Assets/Scripts/Effects/GroundShadow/GroundDecalShadowProjectorMB.cs
```

Namespace:

```csharp
BC.Effects.GroundShadow
```

Required component:

```csharp
UnityEngine.Rendering.Universal.DecalProjector
```

---

## 基本構成

推奨 hierarchy:

```text
PlayerRoot
  ├─ Player visual / collider / scripts
  └─ GroundShadowProjector
      ├─ DecalProjector
      └─ GroundDecalShadowProjectorMB
```

Bomb も同じ構成でよい。

`GroundShadowProjector` は Target の子に置いてよい。ただし `GroundDecalShadowProjectorMB` が毎フレーム world position / world rotation を上書きするため、見た目上は ground hit 位置へ独立して追従する。

---

## Renderer / URP 設定

URP Renderer に Decal Renderer Feature を追加する。

推奨初期値:

```text
Technique: Automatic または Screen Space
Surface Data: Albedo only から開始
Normal Blend: Low
Max Draw Distance: 30-60
Use Rendering Layers: Off から開始
```

`Use Rendering Layers` は不要な対象へ投影される問題が出た時だけ有効化する。最初から有効化しない。

---

## Decal Material

DecalProjector には Decal 対応 Material を割り当てる。

推奨 Material:

```text
M_Decal_PlayerGroundShadow
M_Decal_BombGroundShadow
M_Decal_ItemGroundShadow
```

見た目方針:

```text
Player:
  青灰 / 紫灰の柔らかい楕円

Bomb:
  Player より小さく濃い円
  中心にわずかな焦げ茶

Danger Bomb:
  爆発直前だけ赤橙 tint を薄く混ぜる
```

真っ黒な影は禁止。BombCourier の stylized shadow palette に合わせる。

---

## GroundDecalShadowProjectorMB 設定

### Player 推奨値

```text
Update Phase: LateUpdate
Cast Mode: SphereCast
Cast Origin Up Offset: 0.5
Max Ground Distance: 20
Sphere Cast Radius: 0.18 - 0.28
Max Receivable Surface Angle: 68
Surface Offset: 0.025
Lost Ground Grace Time: 0.08

Base Width: 0.9
Base Height: 0.58
Max Width Multiplier: 1.45
Max Height Multiplier: 1.25
Projection Depth: 0.85
Fade Height: 6
Min Fade Factor: 0.22
Max Fade Factor: 0.68

Position Sharpness: 0
Rotation Sharpness: 0
Size Sharpness: 18
Fade Sharpness: 18

Force Scale Invariant: true
Apply Angle Fade: true
Start Angle Fade: 70
End Angle Fade: 88
Draw Distance: 40
Camera Fade Scale: 0.85
```

### Bomb 推奨値

```text
Base Width: 0.48
Base Height: 0.48
Max Width Multiplier: 1.7
Max Height Multiplier: 1.7
Min Fade Factor: 0.25
Max Fade Factor: 0.75
Fade Height: 5
Sphere Cast Radius: 0.12 - 0.18
```

Bomb は投げ軌道の読みやすさが重要なので、Player より濃くしてよい。

---

## 表示条件

Ground Decal Shadow は次の時だけ表示する。

```text
- Target が active
- Ground Mask に hit する
- hit normal が Max Receivable Surface Angle 以下
- DecalProjector に有効な Material がある
```

壁や急斜面には出さない。

---

## 階段 / 斜面対応

階段・斜面では Quad 影ではなく DecalProjector を使う。

理由:

- 踏み面に投影できる
- 傾いた地面に沿う
- 段差が多い場所でも着地点として読める

ただし Projection Depth を深くしすぎると階段の蹴上げ面や壁面へ伸びる。

```text
Projection Depth は 0.6 - 1.2 を基準にする。
```

---

## Performance 方針

対象は絞る。

必須:

```text
- Player
- 持ち運び中の Bomb
- 投げられた Bomb
```

必要に応じて:

```text
- 落下する重要アイテム
- 敵
- 大型可動オブジェクト
```

不要:

```text
- 破片
- 小さい装飾
- 常時接地している静的オブジェクト
```

生成破棄は行わない。Prefab に持たせるか、必要な場合は Pool する。

---

## Runtime API

`GroundDecalShadowProjectorMB` は外部から以下を制御できる。

```csharp
SetTarget(Transform newTarget)
SetExternalFadeMultiplier(float multiplier)
SetExternalSizeMultiplier(float multiplier)
SetManualVisible(bool visible)
ForceRefresh()
TryGetGroundHit(out RaycastHit hit)
```

Bomb の爆発直前演出では `SetExternalFadeMultiplier` や Material 側の tint 制御を使う。

---

## 禁止事項

- 物理ライトの shadow map で真下影を代用しない
- 毎回 Instantiate / Destroy しない
- 毎 Entity に無制限に付けない
- 真っ黒な円影にしない
- Projection Depth を雑に大きくしない
- Rendering Layer を最初から複雑にしない

---

## 今後の拡張

### M1: Player / Bomb への導入

- Player prefab に GroundShadowProjector を追加
- Bomb prefab に GroundShadowProjector を追加
- Decal Material を作成
- URP Renderer に Decal Renderer Feature を追加

### M2: Danger Shadow

- Bomb の残り時間に応じて fade / tint / size を変える
- 爆発直前にわずかに脈動させる

### M3: Shadow Service

対象数が増えた場合のみ導入する。

```text
GroundShadowService
  - active shadow registry
  - pooling
  - distance culling
  - camera visibility culling
```

現段階では過剰設計なので不要。

### M4: Style Integration

EnvironmentStylizedLit の shadow palette と Decal Material の色を統一する。

Player / Bomb / Danger / Item で shadow family を明確に分ける。
