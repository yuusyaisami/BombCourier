# Particle Material Preset Guide

## 目的

このガイドは、ParticleUnlit preset と quality tier の starting point をまとめる。preset は見た目の出発点、quality tier は runtime/build budget の契約であり、同じものではない。

## Preset と tier の関係

```text
Dust
  既定 tier: Low
  用途: ambient dust、低エネルギーな alpha particle

Glow
  既定 tier: Medium
  用途: premultiply glow、soft light accent

Spark
  既定 tier: Medium
  用途: additive spark、impact accent

Smoke
  既定 tier: Medium
  用途: lingering smoke、soft exhaust

Magic
  既定 tier: Medium
  用途: additive magic particle、noise + dissolve accent
```

## tier の意味

```text
Low
  BaseMap / Vertex Color / Alpha のみ。
  Mask / Noise / Dissolve / Emission / Depth interaction は off。

Medium
  ParticleUnlit の標準 shipping path。
  Mask / Noise / Dissolve / Emission を使う。

High
  Medium に加えて Soft Particles / Camera Fade を持つ重い path。
  WebGL standard path へは入れない。
```

## 推奨の使い分け

### Dust

```text
- 最初に Low tier を維持したまま alpha / brightness / edge fade を整える
- emission を足したくなった時点で Glow や Magic へ寄せるか Medium を再検討する
```

### Glow

```text
- premultiply baseline として使う
- Bloom OFF でも shape が読める brightness に留める
- camera fade を足す場合は High tier boundary として扱う
```

### Spark

```text
- additive baseline として使う
- queue offset と brightness を上げ過ぎると WebGL readability が落ちる
- custom data 演出を足す場合は preview scene で M10 preview を先に確認する
```

### Smoke

```text
- Noise と soft breakup の starting point
- flipbook 化する場合は SmokeFlipbook preview で atlas bleed を確認する
- soft particles を有効化したら High tier boundary として再確認する
```

### Magic

```text
- additive dissolve / emission accent の starting point
- custom data と組み合わせる場合は MagicCustomData preview を基準にする
- scene readability が落ちる場合は Glow へ戻して noise / dissolve を削る
```

## authored tier と inferred tier

```text
- authored tier は material に保存した意図
- inferred tier は現在の material state から validator が推定した結果
- mismatch warning が出たら Apply Selected Tier で baseline を戻すか、機能の足し引きを完了させる
```

## review checklist

```text
- preset と tier が矛盾していない
- Dust が不用意に Medium/High 化していない
- Glow / Spark / Magic が bright background で消えていない
- Smoke / Magic の Noise と Dissolve が overdraw だけ増やしていない
- High-tier boundary feature を使う material が WebGL standard path に混ざっていない
```