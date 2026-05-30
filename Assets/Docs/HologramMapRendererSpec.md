# Hologram Map Renderer 仕様書

## 1. 目的
本仕様は、Unity上で3Dモデルまたはマップモデルを、近未来的なホログラム表示として描画するための描画システムを定義する。

目標とする表現は以下である。

- 黒または暗色背景上に、発光するワイヤーフレームを表示する
- メッシュ面は薄く半透明に表示する
- 線は一定の太さを持ち、視認性が高い
- スキャンライン、Fresnel、レーダー波、出現アニメーションによってSF的な演出を加える
- 通常のゲーム用モデルとは別に、表示用モデルとして安全に運用できる
- Shader Graphは使用せず、HLSL ShaderとC#で構成する

## 2. 表現コンセプト
本システムの見た目は、単なるWireframe表示ではなく、以下の要素を合成した「ホログラム表示」とする。

```text
Hologram Map =
  Transparent Fill
+ Wireframe Lines
+ Emission
+ Fresnel Rim
+ Scan Lines
+ Radar Pulse
+ Reveal Animation
+ Optional Glitch Noise
```

描画の主役はワイヤーフレームであるが、線だけでは立体の面構造が読みにくいため、極薄の半透明面を併用する。

## 3. 実装方針

### 3.1 Shader Graphは使用しない
本システムではShader Graphを使用しない。

理由は以下である。

- 任意メッシュの三角形エッジをShader Graph単体で正確に扱うのが難しい
- バリセントリック座標を使ったWireframe表現はHLSLの方が明快
- `fwidth` による画面上一定幅の線制御が必要
- 拡張演出を増やす場合、ノードベースよりコードの方が保守しやすい

## 4. 全体構成

### 4.1 推奨オブジェクト構成

```text
HologramMapRoot
 ├ SourceModel
 │   └ MeshFilter / MeshRenderer
 │
 └ HologramModel
     ├ MeshFilter
     ├ MeshRenderer
     └ HologramMapRendererMB
```

`SourceModel` は通常表示用または元データとする。
`HologramModel` はホログラム表示専用に変換されたメッシュを使用する。

## 5. メッシュ仕様

### 5.1 バリセントリック座標
ワイヤーフレーム描画にはバリセントリック座標を使用する。

各三角形の3頂点に対して、以下の値を割り当てる。

```text
Vertex 0: (1, 0, 0)
Vertex 1: (0, 1, 0)
Vertex 2: (0, 0, 1)
```

この値を頂点カラー `COLOR.rgb` に格納する。

Shader側では、ピクセル位置のバリセントリック値の最小成分を使い、三角形の辺への近さを判定する。

### 5.2 頂点複製
通常のMeshは頂点を共有しているため、三角形ごとに異なるバリセントリック座標を持たせられない。

そのため、ホログラム表示用メッシュは以下のように変換する。

```text
元メッシュ:
共有頂点あり

変換後:
三角形ごとに頂点を複製
頂点カラーにバリセントリック座標を格納
```

### 5.3 頂点数増加
変換後の頂点数は、最大で以下になる。

```text
変換後頂点数 = 三角形数 × 3
```

例:

```text
10,000 triangles → 30,000 vertices
50,000 triangles → 150,000 vertices
```

頂点数が65,535を超える場合、MeshのIndex Formatは `UInt32` に切り替える。

## 6. 表示用メッシュの設計方針

### 6.1 元モデルをそのまま使わない
映画的なホログラム表示では、実際のゲーム用モデルをそのままワイヤー化すると、線が多すぎて視認性が落ちる。

そのため、以下を推奨する。

```text
ゲーム用モデル
↓
表示用に簡略化したモデル
↓
Hologram Meshに変換
```

### 6.2 推奨ポリゴン密度

| 用途 | 推奨三角形数 |
| --- | --- |
| 小型オブジェクト | 500 ～ 3,000 |
| 建物単体 | 2,000 ～ 10,000 |
| 小規模マップ | 10,000 ～ 50,000 |
| 大規模マップ | 50,000 ～ 150,000 |

150,000三角形以上のワイヤーフレーム表示は、線が密集して読みづらくなるため、基本的にはLODまたは分割表示を使う。

## 7. Shader仕様

### 7.1 Shader名

```text
BC/Hologram/HologramWireMap
```

### 7.2 Render Pipeline

```text
Universal Render Pipeline
```

### 7.3 Render State

```text
Queue       = Transparent
RenderType  = Transparent
ZWrite      = Off
ZTest       = LEqual
Blend       = SrcAlpha OneMinusSrcAlpha
Cull        = Back
```

必要に応じて両面表示を行う場合は `Cull Off` を許可する。

ただし、基本は `Cull Back` とする。
両面表示は見た目が強くなる一方、線が重なって視認性が落ちる可能性がある。

## 8. Shaderプロパティ

### 8.1 基本色

| Property | Type | Default | 説明 |
| --- | --- | --- | --- |
| `_LineColor` | Color | `(0, 1, 0.2, 1)` | ワイヤー線色 |
| `_FillColor` | Color | `(0, 0.8, 0.4, 1)` | 面の色 |
| `_FillAlpha` | Float | `0.06` | 面の透明度 |
| `_EmissionStrength` | Float | `2.0` | 発光強度 |

### 8.2 Wireframe

| Property | Type | Default | 説明 |
| --- | --- | --- | --- |
| `_LineWidth` | Float | `1.2` | 線幅 |
| `_LineFeather` | Float | `1.0` | 線の柔らかさ |
| `_LineAlpha` | Float | `1.0` | 線の透明度 |

線幅は `fwidth` ベースで計算し、画面上でなるべく一定幅に見えるようにする。

### 8.3 Fresnel

| Property | Type | Default | 説明 |
| --- | --- | --- | --- |
| `_FresnelPower` | Float | `3.0` | Fresnelの鋭さ |
| `_FresnelStrength` | Float | `0.6` | Fresnelの強度 |

Fresnelは輪郭部を強調するために使用する。

### 8.4 Scan Line

| Property | Type | Default | 説明 |
| --- | --- | --- | --- |
| `_ScanScale` | Float | `12.0` | スキャン線密度 |
| `_ScanSpeed` | Float | `1.0` | スキャン線速度 |
| `_ScanStrength` | Float | `0.35` | スキャン線強度 |
| `_ScanDirection` | Vector | `(0, 1, 0, 0)` | スキャン方向 |

スキャン線はWorld PositionとTimeを使って生成する。

### 8.5 Radar Pulse

| Property | Type | Default | 説明 |
| --- | --- | --- | --- |
| `_PulseCenter` | Vector | `(0,0,0,0)` | レーダー波の中心 |
| `_PulseSpeed` | Float | `4.0` | 波の進行速度 |
| `_PulseWidth` | Float | `0.6` | 波の幅 |
| `_PulseStrength` | Float | `0.8` | 波の明るさ |
| `_PulseInterval` | Float | `8.0` | 波の周期 |

中心点から円形または球状に広がる波を表現する。
マップ表示の場合はXZ平面距離を使う。

### 8.6 Reveal Animation

| Property | Type | Default | 説明 |
| --- | --- | --- | --- |
| `_Reveal` | Float | `1.0` | 表示進行度 |
| `_RevealMinY` | Float | `0.0` | 表示開始高さ |
| `_RevealMaxY` | Float | `10.0` | 表示終了高さ |
| `_RevealSoftness` | Float | `0.5` | 境界の柔らかさ |
| `_RevealGlowStrength` | Float | `2.0` | 境界発光 |

`_Reveal` は `0.0` で非表示、`1.0` で全表示とする。
下から上へ構築されるような表示に使用する。

### 8.7 Optional Glitch

| Property | Type | Default | 説明 |
| --- | --- | --- | --- |
| `_GlitchStrength` | Float | `0.0` | グリッチ強度 |
| `_GlitchScale` | Float | `20.0` | グリッチ密度 |
| `_GlitchSpeed` | Float | `8.0` | グリッチ速度 |

初期状態では無効。
演出時のみ一時的に使用する。

## 9. Shader計算仕様

### 9.1 Wire Line計算
頂点カラーからバリセントリック座標を取得する。

```text
float3 bary = input.color.rgb;
```

線の強度は以下で計算する。

```text
float3 d = fwidth(bary);
float3 s = smoothstep(d * _LineWidth, d * (_LineWidth + _LineFeather), bary);
float line = 1.0 - min(min(s.x, s.y), s.z);
```

`line` は以下の意味を持つ。

```text
0 = 面の中央
1 = 三角形の辺
```

### 9.2 Fill計算
面は常に薄く表示する。

```text
float fill = _FillAlpha;
```

ただし、RevealやGlitchで表示制御される。

### 9.3 Fresnel計算

```text
float fresnel =
    pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower)
    * _FresnelStrength;
```

Fresnelは線と面の両方に加算する。

### 9.4 Scan Line計算

```text
float scanCoord = dot(positionWS, normalize(_ScanDirection.xyz));
float scan = sin(scanCoord * _ScanScale + _Time.y * _ScanSpeed) * 0.5 + 0.5;
scan *= _ScanStrength;
```

### 9.5 Radar Pulse計算
XZ平面で中心からの距離を取る。

```text
float dist = distance(positionWS.xz, _PulseCenter.xz);
float t = fmod(_Time.y * _PulseSpeed, _PulseInterval);
float pulse = 1.0 - smoothstep(0.0, _PulseWidth, abs(dist - t));
pulse *= _PulseStrength;
```

### 9.6 Reveal計算
World Yを使い、下から上へ表示する。

```text
float revealHeight = lerp(_RevealMinY, _RevealMaxY, _Reveal);
float revealMask = 1.0 - smoothstep(
    revealHeight,
    revealHeight + _RevealSoftness,
    positionWS.y
);
```

`_Reveal = 0` のときほぼ非表示。
`_Reveal = 1` のとき全体表示。

境界部分には追加発光を入れる。

```text
float revealEdge = 1.0 - abs(positionWS.y - revealHeight) / _RevealSoftness;
revealEdge = saturate(revealEdge);
```

### 9.7 最終色

```text
float effect =
    1.0
    + fresnel
    + scan
    + pulse
    + revealEdge * _RevealGlowStrength;

float3 fillColor = _FillColor.rgb * _EmissionStrength * effect;
float3 lineColor = _LineColor.rgb * _EmissionStrength * effect;

float3 finalColor = lerp(fillColor, lineColor, line);
```

### 9.8 最終Alpha

```text
float alpha = max(_FillAlpha, line * _LineAlpha);
alpha *= revealMask;
```

必要に応じてGlitchで局所的にAlphaを落とす。

## 10. C#構成

### 10.1 必要クラス

```text
HologramMeshBuilder
HologramMapRendererMB
HologramMapController
HologramMaterialPropertyBinder
```

## 11. HologramMeshBuilder仕様

### 11.1 役割
通常Meshを、バリセントリック座標付きのホログラム表示用Meshに変換する。

### 11.2 入力

```text
Mesh sourceMesh
```

### 11.3 出力

```text
Mesh hologramMesh
```

### 11.4 処理内容

- 三角形ごとに頂点を複製
- 頂点座標をコピー
- 法線をコピー
- UVをコピー
- Tangentをコピー
- 頂点カラーにバリセントリック座標を格納
- 頂点数が65,535を超える場合はIndexFormat.UInt32を使用
- Boundsを再計算

### 11.5 注意
SkinnedMeshRendererには初期対応しない。
必要になった場合はBakeMesh後に変換する。

## 12. HologramMapRendererMB仕様

### 12.1 役割
対象MeshFilterのMeshをホログラム表示用Meshに変換し、指定Materialを適用する。

### 12.2 Inspector項目

```csharp
[SerializeField] private MeshFilter sourceMeshFilter;
[SerializeField] private MeshRenderer targetRenderer;
[SerializeField] private Material hologramMaterial;
[SerializeField] private bool generateOnAwake = true;
[SerializeField] private bool hideSourceRenderer = true;
```

### 12.3 Context Menu

```csharp
[ContextMenu("Generate Hologram Mesh")]
```

Editor上で手動生成できる。

### 12.4 実行時処理

```text
Awake
↓
sourceMeshFilter.sharedMesh を取得
↓
HologramMeshBuilder.Build(source)
↓
自分のMeshFilter.sharedMeshへ設定
↓
Material適用
↓
必要なら元Rendererを非表示
```

## 13. HologramMapController仕様

### 13.1 役割
ホログラムの表示状態、出現アニメーション、スキャン演出、レーダー波を制御する。

### 13.2 主なAPI

```csharp
public void Show();
public void Hide();
public void SetReveal(float value);
public void PlayReveal(float duration);
public void SetPulseCenter(Vector3 worldPosition);
public void TriggerPulse();
public void SetEmission(float strength);
```

## 14. MaterialPropertyBlock運用
Materialインスタンスを増やさないため、ランタイムで変化する値は `MaterialPropertyBlock` で渡す。

対象プロパティ:

```text
_Reveal
_PulseCenter
_EmissionStrength
_GlitchStrength
_ScanSpeed
```

ただし、Materialごとに共通でよい値はMaterial側に保持する。

## 15. 推奨初期パラメータ

### 15.1 緑系ホログラム

```text
LineColor        = (0.0, 1.0, 0.15, 1.0)
FillColor        = (0.0, 0.75, 0.25, 1.0)
FillAlpha        = 0.055
LineWidth        = 1.25
LineFeather      = 1.0
LineAlpha        = 1.0
EmissionStrength = 2.0
FresnelPower     = 3.0
FresnelStrength  = 0.6
ScanScale        = 12.0
ScanSpeed        = 1.0
ScanStrength     = 0.35
PulseSpeed       = 4.0
PulseWidth       = 0.6
PulseStrength    = 0.8
```

### 15.2 シアン系ホログラム

```text
LineColor        = (0.0, 0.85, 1.0, 1.0)
FillColor        = (0.0, 0.55, 0.9, 1.0)
FillAlpha        = 0.045
EmissionStrength = 2.4
```

## 16. Post Processing要件

### 16.1 Bloom
ホログラム感を出すため、Bloomはほぼ必須とする。

推奨値:

```text
Bloom Intensity = 0.4 ～ 1.2
Bloom Threshold = 0.8 ～ 1.2
```

強すぎるBloomは線を潰すため避ける。

### 16.2 背景
背景は黒または暗色を推奨する。

```text
Background Color = near black
```

明るい背景上ではホログラム線の視認性が大きく落ちる。

## 17. 描画順と透明問題

### 17.1 ZWrite
基本は `ZWrite Off` とする。

理由:

- 半透明面として描画したい
- 他の透明物との干渉を避ける

ただし、複数ホログラムモデルが重なる場合、描画順問題が発生する可能性がある。

### 17.2 対策
以下のいずれかを採用する。

```text
1. Hologram同士を大きく重ねない
2. Renderer.sortingOrder / renderQueueを調整する
3. 部位ごとに分割して描画順を制御する
4. 必要ならDepth Prepassを追加する
```

## 18. Depth Prepass拡張
必要になった場合のみ、Depth Prepassを導入する。

### 18.1 目的

- ホログラム内部の奥側の線を抑える
- 半透明モデルの見た目を安定させる
- 複雑なマップ表示時の情報量を整理する

### 18.2 注意
Depth Prepassを入れると、奥の線が見えにくくなる。
SF的な「透けて全部見える」表現とは相性が悪い場合もある。

本仕様では初期実装には含めない。

## 19. 出現アニメーション仕様

### 19.1 基本挙動
ホログラム表示開始時、下から上へスキャンされながら構築される。

```text
Reveal = 0.0 → 非表示
Reveal = 1.0 → 全表示
```

### 19.2 アニメーション時間
推奨:

```text
0.6秒 ～ 1.5秒
```

### 19.3 カーブ
線形ではなくEaseOutを推奨する。

```text
t = 1 - pow(1 - t, 3)
```

## 20. グリッチ演出仕様
グリッチは常時使用しない。

使用タイミング:

```text
表示開始時
表示終了時
マップ更新時
ターゲット選択時
エラー演出時
```

推奨値:

```text
GlitchStrength = 0.0 通常
GlitchStrength = 0.2 ～ 0.5 演出時
```

過度なグリッチは視認性を破壊するため、短時間に限定する。

## 21. レーダー波仕様

### 21.1 用途

- 現在位置の強調
- 目的地の探索
- マップ読み込み演出
- 敵や重要地点の検出演出

### 21.2 挙動
`_PulseCenter` からXZ平面上に円形波を広げる。

```text
中心 → 外側へリングが広がる
リング付近のLine/Fillが一時的に明るくなる
```

## 22. パフォーマンス方針

### 22.1 主な負荷
負荷の中心は以下。

```text
1. 透明描画によるOverdraw
2. 頂点複製による頂点数増加
3. Bloom
4. 大量のホログラムオブジェクト
```

### 22.2 最適化方針

- 表示用メッシュは簡略化する
- 遠距離では非表示またはLODを使う
- 巨大マップは分割表示する
- Bloomを強くしすぎない
- 透明面のAlphaを低く保つ
- 不要な両面描画を避ける

## 23. LOD方針
大規模マップではLODを用意する。

```text
LOD0: 近距離・詳細表示
LOD1: 中距離・簡略線
LOD2: 遠距離・主要輪郭のみ
LOD3: 非表示
```

ワイヤーフレーム表示は線が多いほど良いわけではない。
視認性を優先し、遠距離では情報量を減らす。

## 24. アセット方針

### 24.1 表示用モデル
以下のいずれかで用意する。

```text
1. Blender等で簡略化した専用モデル
2. Unity Editor上で自動生成した簡略Mesh
3. ProBuilder等で作成した低ポリモデル
```

### 24.2 元モデルとの分離
ゲームプレイ用Meshとホログラム表示用Meshは分ける。

理由:

- 表示都合でポリゴンを削減できる
- 通常モデルのMaterialやColliderに影響しない
- 演出専用のUVや頂点カラーを自由に使える

## 25. ファイル構成

```text
Assets/
 └ Scripts/
     └ Visual/
         └ Hologram/
             ├ HologramMeshBuilder.cs
             ├ HologramMapRendererMB.cs
             ├ HologramMapController.cs
             └ HologramMaterialPropertyBinder.cs

 └ Art/
     └ Shaders/
         └ Hologram/
             └ HologramWireMap.shader

 └ Materials/
     └ Hologram/
         ├ M_HologramMap_Green.mat
         └ M_HologramMap_Cyan.mat
```

## 26. 実装優先順位

### Phase 1: 最小実装

- Barycentric付きMesh生成
- Wireframe Shader
- 半透明Fill
- Emission
- Bloom適用

完了基準:

```text
モデルが発光ワイヤーフレームとして表示される
面が薄く表示される
線幅がある程度一定に見える
```

### Phase 2: 演出強化

- Fresnel
- Scan Line
- Reveal Animation
- Radar Pulse

完了基準:

```text
表示開始時に下から構築される
線と面がスキャンで明滅する
レーダー波がモデル上を走る
```

### Phase 3: 品質調整

- Glitch
- LOD
- 表示用モデルの簡略化
- Render Queue調整
- パフォーマンス測定

完了基準:

```text
実ゲーム画面で破綻なく見える
線が多すぎず、形が読み取れる
複数オブジェクト表示時も重すぎない
```

## 27. 除外仕様
初期実装では以下を行わない。

```text
Geometry ShaderによるWireframe生成
Shader Graphによる実装
リアルタイムトポロジー変更
SkinnedMeshRendererの直接対応
完全なHidden Line Removal
ボリュームホログラム表現
```

Geometry Shaderはプラットフォーム互換性の問題があるため採用しない。
本仕様ではバリセントリック座標方式を標準とする。

## 28. 品質基準

### 28.1 視覚品質

- 暗背景で線が明確に見える
- 面は主張しすぎない
- Bloomで線が潰れない
- スキャン演出が視認性を邪魔しない
- モデル形状が一目で読める

### 28.2 実装品質

- 元Meshを破壊しない
- 実行時にMaterialインスタンスを無駄に増やさない
- Mesh変換は必要時のみ行う
- 複数オブジェクトでも管理できる
- Inspectorから主要パラメータを調整できる

## 29. 推奨完成イメージ
最終的な見た目は以下を目標とする。

```text
黒背景に緑またはシアンの発光ライン
薄い透明面で立体感を保持
輪郭はFresnelで少し強調
細いスキャンラインが上下に流れる
一定周期でレーダー波が広がる
表示開始時は下からスキャンされて構築される
```

## 30. 開発上の注意
この表現で最も重要なのは、線の多さではなく情報の整理である。

高密度なモデルをそのままワイヤーフレーム化すると、見た目は派手になるが、形状が読めなくなる。
そのため、実装時は必ず以下を意識する。

```text
線を増やすより、読める線を残す
面は濃くしすぎない
Bloomは強くしすぎない
表示用モデルは簡略化する
演出は常時ではなく要所で使う
```

本システムの完成度は、Shaderの派手さよりも、表示用メッシュの設計と演出量の制御で決まる。
