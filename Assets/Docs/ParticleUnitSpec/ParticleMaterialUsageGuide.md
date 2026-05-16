# Particle Material Usage Guide

## 目的

このガイドは、Particle Material System を日常の effect authoring と review に使うための運用手順をまとめる。仕様書の代わりではなく、どの family / preset / validation scene をどう使うかを短く確認するための文書として使う。

## 対象 family

```text
ParticleUnlit
  Dust / Smoke / Glow / Spark / Magic / Flipbook / Custom Data / Soft Particles の標準 family。

ParticleLit
  Light response が必要な bubble / debris / mesh particle の review 用 family。

ParticleDistortion
  HeatHaze / AirWarp / MagicWarp のような Opaque Texture 前提の歪み family。

ParticleRingUnlit / ParticleGroundUnlit
  M16 時点では review placeholder のみ。functional shader は未実装。
```

## Validation Scene

Scene:

```text
Assets/Scenes/Particles/ParticleMaterialTestScene.unity
```

M16 review harness を再生成する entry:

```text
Tools/BC/Particles/ParticleUnlit/Bootstrap M16 Review Harness
```

確認対象:

```text
- Dust / Smoke / Glow / Spark / Magic preview
- Smoke Flipbook / Magic Custom Data preview
- Quality Tier Test Area
- Future Lit Test Area
- Future Distortion Test Area
- ParticleMaterialReviewHarness
```

## 推奨 workflow

1. 近い preset を先に選ぶ。
2. authored quality tier を決め、Apply Selected Tier で baseline を揃える。
3. ParticleMaterialTestScene で preview / validation anchor を確認する。
4. 明るい背景と暗い背景の両方で readability を見る。
5. Bloom ON/OFF の両方で glow / spark / emission を確認する。
6. WebGL standard path に入る effect は High-tier boundary feature を避ける。
7. Debug Mode は保存前に Final へ戻す。

## family 選択の目安

### ParticleUnlit を使うケース

```text
- 透明 Unlit 粒子
- Dust / Smoke / Glow / Spark / Magic
- Flipbook particle
- Custom Data particle
- Soft Particles / Camera Fade を含む optional heavy path
```

### ParticleLit を使うケース

```text
- ライト応答が見た目の主成分
- Mesh particle を前提にする
- Bubble / Debris のように normal / smoothness が必要
```

### ParticleDistortion を使うケース

```text
- 背景の歪みが主効果
- Opaque Texture が有効な URP camera を使う
- WebGL standard path へは入れない
```

## manual review checklist

```text
- BaseMap / Vertex Color / Alpha が破綻していない
- Blend Mode と queue ordering が読みやすい
- 明るい背景でも silhouette が消えない
- 暗い背景でも emission が白飛びしない
- Bloom OFF でも Glow / Spark が読める
- Flipbook が tile bleed せず再生される
- Custom Data preview で Custom1.x/y/z の差分が見える
- Soft Particles / Camera Fade が depth wall と camera distance で意図通りに変化する
- Lit validation anchor が light reference に反応する
- Distortion validation anchor が opaque backdrop を歪ませる
```

## shipping checklist

```text
- WebGL standard path に High-tier boundary feature が混ざっていない
- Debug Mode が off に戻っている
- generated prefab / validation anchor の drift がない
- Quality Tier authored/inferred summary が意図と一致する
- Ring / Ground placeholder を shipping effect と誤認していない
```