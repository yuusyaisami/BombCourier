# Particle Material Performance Guide

## 目的

このガイドは、Particle Material System の performance budget と shipping rule をまとめる。最適化は表現実験の代わりではなく、standard path と opt-in path を混同しないための基準として使う。

## ParticleUnlit 標準予算

```text
BaseMap Sample: 1
MaskMap Sample: 0-1
NoiseMap Sample: 0-1
Depth Sample: 0 for standard path
Opaque Texture Sample: 0
Lighting: なし
```

原則:

```text
- standard WebGL path は Low / Medium まで
- High-tier boundary feature は opt-in review path
- ParticleLit / ParticleDistortion は Ultra / non-WebGL family boundary として扱う
```

## overdraw の考え方

```text
- 画面全体を覆う半透明 particle を常時重ねない
- Additive と emission は silhouette が読める範囲で止める
- queue offset を強く振る前に particle count と size を見直す
- Smoke / Magic は Noise と Dissolve を足す前に alpha / lifetime で破綻を抑える
```

## WebGL standard path

```text
許容:
  ParticleUnlit Low
  ParticleUnlit Medium

非許容:
  Soft Particles
  Camera Fade
  ParticleLit family
  ParticleDistortion family
```

WebGL build guard:

```text
Tools/BC/Particles/ParticleUnlit/Run M16 WebGL Validation Build
```

## High-tier boundary feature

```text
- Soft Particles
- Camera Fade
```

これらは canonical High tier inference とは別に、build validator が standard WebGL path から弾く boundary として扱う。

## family 別メモ

### ParticleUnlit

```text
- keyword を増やさず hidden render-state property で制御する
- reserved property は実装しない機能の将来枠として維持し、場当たり的に流用しない
- preset baseline と quality tier baseline を分けて考える
```

### ParticleLit

```text
- mesh particle と light response の必要性がある場合だけ使う
- main light review を Future Lit Test Area で行う
- WebGL standard path へは入れない
```

### ParticleDistortion

```text
- Opaque Texture prerequisite を満たす camera でのみ使う
- 背景を歪ませる reference wall で見え方を確認する
- WebGL standard path へは入れない
```

## optimization checklist

```text
- 不要な shader_feature / multi_compile を増やしていない
- sample budget が docs と source contract に一致している
- generated material に Debug Mode が残っていない
- prefab preview と validation anchor の drift cleanup が維持されている
- marker child-set contract を壊す scene object 追加をしていない
```

## split 判断の再確認

```text
ParticleUnlitLite を切る条件:
  validator/build guard だけでは standard WebGL path を維持できなくなった時

ParticleUnlitAdvanced を切る条件:
  High-tier optional features が inspector / validator / test を過度に肥大化させた時
```

M16 では split せず、判断条件と review budget だけを固定する。