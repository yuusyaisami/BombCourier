以下は、**URP向け・環境用 Stylized Lit Shader 仕様書 v0.1** として作ります。
目的は、既存の Toon Shader の代替ではなく、**地面・壁・建築・大型オブジェクト用の「ライト表現に強い Lit 系 Shader」**です。

---

# Environment Stylized Lit Shader 仕様書 v0.1

## 1. Shader 名称

仮称：

```text
EnvironmentStylizedLit
```

または、もう少し性格を出すなら：

```text
ClayDioramaLit
QuantizedClayLit
StylizedEnvironmentLit
```

現時点では汎用性を優先して、仕様上の正式名は以下とします。

```text
BC/EnvironmentStylizedLit
```

BombCourier では `GameLib` 配下を使わない。
Shader 名は既存 Shader と同じ `BC/` 接頭辞に揃え、アセット配置も既存の `Assets/Art` / `Assets/Scenes` 規約に従う。

---

# 2. 目的

この Shader は、地面・壁・建築物・大型ステージメッシュに使用する **環境用 Stylized Lit Shader** である。

通常の Toon Shader のようにアウトラインやキャラクター的なセル表現を主目的にせず、以下を重視する。

```text
- x段階に整理されたライト表現
- 黒く潰れない色付きの影
- 粘土、石膏、マットプラスチック、陶器、おもちゃ的な質感
- 壁・床に使っても単調にならない表面ムラ
- ProBuilder / モジュール式レベル形状との相性
- URP Lit に近い基本機能
- Outline なし
```

最終的な絵作りの方向性は：

```text
手作りの粘土ジオラマを、
少しゲーム的で整理されたライトで照らした表現
```

である。

---

# 3. 非目的

この Shader では以下を主目的にしない。

```text
- キャラクター用アウトライン表現
- アニメ調の完全なセルルック
- フォトリアル PBR の完全再現
- 透明マテリアル表現
- 水、ガラス、金属鏡面のような特殊 Shader
- スクリーンスペースポストエフェクト
```

特に重要なのは、**Outline を Shader 側で持たない**こと。
輪郭演出が必要な場合は、別の ScreenFx / RendererFeature / Mask ベースの仕組みで扱う。

---

# 4. 対象レンダーパイプライン

対象は **Unity 6 系 + URP**。

実装方式は、最終的には **手書き HLSL Shader** を推奨する。
理由は、Unity 公式も Custom HLSL Shader では Render Pass、入出力、内部関数をより細かく制御できるとしており、今回のように Lit をベースにしつつ独自ライトモデルを作る用途では Shader Graph より制御しやすいからです。Shader Graph でも Custom Function は使えますが、Render Pass や細かい挙動まで詰めるなら HLSL が堅いです。([Unity マニュアル][1])

---

# 5. 基本方針

## 5.1 URP Lit の基本機能を持つ

URP Lit Shader は、Base Map、Metallic / Specular、Smoothness、Normal Map、Occlusion、Emission などの標準的な Lit 表現を持つ Shader です。今回の Shader も、環境用として必要な範囲でこれらを継承する方針にします。([Unity マニュアル][2])

ただし、完全な PBR の再現を目的にするのではなく、**Lit の入力項目を持ちつつ、最終的なライト評価を Stylized に置き換える**。

---

# 6. 使用対象

## 6.1 主対象

```text
- 床
- 壁
- 天井
- 柱
- 階段
- 遺跡パーツ
- ProBuilder 製のレベルメッシュ
- モジュール式ステージブロック
- ジオラマ風の建築物
- 非キャラクター系の大型オブジェクト
```

## 6.2 非推奨対象

```text
- 顔のあるキャラクター
- 髪
- 目
- 水
- ガラス
- 金属鏡面
- UI
- VFX Particle
```

キャラクターには別 Shader を使うべきです。
この Shader は、画面の大部分を占める背景・レベル形状のためのものです。

---

# 7. 表現コンセプト

## 7.1 基本見た目

目指す質感は以下。

```text
- 完全なリアルではない
- 完全な Toon でもない
- 影は段階化されている
- 影境界は少しざらつく
- 黒潰れしない
- 表面はわずかに手作り感がある
- 色はやや玩具的
- 反射は弱く、広く、柔らかい
- 壁や床に使っても単調ではない
```

---

# 8. Material 基本プロパティ

## 8.1 Surface

```text
_SurfaceType
  Opaque のみを正式対応とする。
  Transparent は v1 では非対応。

_Cull
  Front / Back / Both を明示的に持つ。
  Unity 標準 URP Lit と意味を合わせる。

_AlphaClip
  有効 / 無効

_Cutoff
  Alpha Clip 閾値
```

重要：
有料 Toon Shader のように Front / Back の意味が特殊化されると、ProBuilder の内向き法線や室内壁で混乱する。
この Shader では、**Front / Back の意味を Unity 標準系と一致させる**。

---

## 8.2 Base

```text
_BaseMap
_BaseColor
_VertexColorMode
_AlbedoNoiseStrength
_WorldColorVariationStrength
```

### Base Color の扱い

```text
finalBaseColor = BaseMap * BaseColor
```

その後、必要に応じて World Space Noise / Vertex Color / AO / Light Band によって補正する。

---

## 8.3 Normal

```text
_NormalMap
_NormalScale
_DetailNormalMap
_DetailNormalScale
_WorldNormalNoiseStrength
```

### 方針

粘土・石膏・塗装壁のような質感では、強い Normal は不要。
むしろ弱い Normal を広く入れる方が良い。

```text
推奨:
  NormalScale = 0.1 ～ 0.5

非推奨:
  強すぎる岩肌 Normal
  ギラギラした高周波 Normal
```

---

## 8.4 Metallic / Specular

```text
_WorkflowMode
  Metallic
  Specular

_Metallic
_MetallicGlossMap
_SpecColor
_SpecGlossMap
_Smoothness
```

### 方針

URP Lit に近い入力は持つ。
ただし、この Shader の主用途では Metallic は基本 0 に近い。

```text
Default Metallic = 0
Default Smoothness = 0.35
Default Specular Strength = 0.15
```

金属表現を主目的にしない。
あくまで、マットプラスチック、陶器、粘土、石膏のための弱い反射として使う。

---

## 8.5 Occlusion

```text
_OcclusionMap
_OcclusionStrength
_OcclusionColor
_CavityStrength
_CavityColor
```

### 方針

AO を単なる黒乗算にしない。

悪い例：

```text
color *= ao
```

良い例：

```text
color = lerp(CavityColor, color, ao)
```

または、

```text
color = lerp(color * OcclusionTint, color, ao)
```

暗部に色を持たせる。
粘土・石膏表現では、隅が黒くなるよりも、**青灰・赤茶・紫灰に沈む**方が良い。

---

## 8.6 Emission

```text
_EmissionMap
_EmissionColor
_EmissionStrength
```

### 方針

URP Lit の基本機能として持つ。
ただし、v1 では Bloom との連携までは Shader 側で特殊化しない。

---

# 9. ライトモデル

この Shader の中核。

---

## 9.1 基本入力

Lighting 計算に使用する主な値。

```text
N = normalWS
L = lightDirectionWS
V = viewDirectionWS
H = normalize(L + V)

NdotL = saturate(dot(N, L))
NdotV = saturate(dot(N, V))
NdotH = saturate(dot(N, H))
```

---

# 10. x段階 Diffuse Lighting

## 10.1 目的

通常の連続的な Diffuse を、指定段階数に整理する。

```text
_LightStepCount
_LightStepSmoothness
_LightBandContrast
_LightBandOffset
_LightBandNoiseStrength
```

例：

```text
StepCount = 4
```

の場合、

```text
0: Deep Shadow
1: Shadow
2: Mid
3: Light
```

のように整理される。

---

## 10.2 通常 Toon との差別化

単純な Toon はこうなりやすい。

```text
NdotL を階段状にする
境界が硬い
影色が単調
```

この Shader では以下を加える。

```text
- Wrap Lighting
- Band Smoothness
- Band Noise
- Color Ramp
- Shadow Color
- Bounce Light
- Ambient Direction Color
```

つまり、**段階化はするが、アニメセル的なパキパキ感を少し崩す**。

---

## 10.3 基本計算

疑似式：

```text
wrapped = saturate((NdotL + WrapStrength) / (1.0 + WrapStrength))

bandInput = wrapped + BandOffset + bandNoise

stepped = QuantizeWithSmoothness(
    bandInput,
    LightStepCount,
    LightStepSmoothness
)
```

### WrapStrength

```text
_WrapLighting
```

意味：

```text
0.0 = 通常の Lambert
0.3 = 少し光が回り込む
0.6 = 粘土・ソフビ的
1.0 = かなりフラット
```

推奨初期値：

```text
_WrapLighting = 0.35
```

---

# 11. Light Band Ramp

## 11.1 Procedural Ramp

最低限、以下の色を持つ。

```text
_DeepShadowColor
_ShadowColor
_MidColor
_LightColor
_HighlightColor
```

基本は `stepped` に応じて補間する。

```text
bandColor = EvaluateBandColor(stepped)
```

---

## 11.2 Ramp Texture 対応

将来的には Ramp Texture をサポートする。

```text
_UseRampTexture
_RampTexture
_RampStrength
```

ただし、v1 実装では Procedural Ramp を優先する。
Ramp Texture は便利だが、Material ごとの制御が曖昧になりやすい。

---

# 12. 影色の仕様

## 12.1 黒影禁止

この Shader では、基本的に影を黒にしない。

```text
DeepShadowColor = 暗い青紫 / 赤茶 / 青灰
ShadowColor     = 少し彩度のある暗色
MidColor        = BaseColor に近い色
LightColor      = 暖色寄り
HighlightColor  = クリーム / 薄黄色
```

## 12.2 Shadow Attenuation の扱い

メインライトの影は以下の順で扱う。

```text
1. NdotL を計算
2. Wrap Lighting
3. Shadow Attenuation を反映
4. Light Band 化
5. Ramp Color に変換
```

ただし、影が硬すぎる場合に備えて、以下を持つ。

```text
_ShadowInfluence
_ShadowSoftFill
_ShadowColorBlend
```

疑似式：

```text
litAmount = wrapped * lerp(1.0, shadowAttenuation, ShadowInfluence)
litAmount = max(litAmount, ShadowSoftFill)
```

これにより、完全に真っ黒な影を避ける。

---

# 13. Ambient / Bounce Lighting

## 13.1 方向性 Ambient

箱の中や室内ステージでは、Directional Light だけでは見た目が死にやすい。
そのため、Shader 側に簡易的な方向性 Ambient を持たせる。

```text
_AmbientTopColor
_AmbientSideColor
_AmbientBottomColor
_AmbientStrength
```

Normal の向きで色を混ぜる。

```text
normal.y > 0 なら Top
normal.y ~= 0 なら Side
normal.y < 0 なら Bottom
```

用途：

```text
上向きの床:
  空・天井光の影響

横向きの壁:
  横方向の環境色

下向きの面:
  床からの反射・暗部色
```

---

## 13.2 Bounce Light

```text
_BounceColor
_BounceStrength
_BounceDirection
_BounceWrap
```

目的：

```text
- 床からの暖色反射
- 壁からの色反射
- 影側にわずかに入る反射光
```

これはリアルな GI ではない。
**絵作り用の疑似バウンス**である。

---

# 14. Baked GI / Lightmap / Light Probe

## 14.1 対応方針

環境用 Shader なので、Lightmap / Light Probe / Baked GI は対応対象に含める。

```text
- Lightmap
- Directional Lightmap
- Light Probe
- SH
```

ただし、最終色への混ぜ方は通常 Lit より Stylized にする。

```text
bakedGI をそのまま足すのではなく、
Ambient / Shadow Fill として使用する
```

### 基本方針

```text
finalIndirect = bakedGI * IndirectStrength
finalIndirect = ApplyIndirectStylize(finalIndirect)
```

---

# 15. Additional Lights

## 15.1 基本対応

URP の Additional Lights に対応する。

ただし、すべてのライトをメインライトと同じように段階化すると、見た目がノイズっぽくなったり、複数ライトでバンドが破綻しやすい。

そのため、Additional Lights にはモードを持たせる。

```text
_AdditionalLightMode
  Off
  FillOnly
  Quantized
  Continuous
```

## 15.2 推奨初期設定

```text
AdditionalLightMode = FillOnly
```

理由：

```text
メインライト:
  段階化された主陰影を作る

追加ライト:
  影側を少し持ち上げる
  色味を足す
  局所的な演出に使う
```

複数ライトまで全部バキバキに段階化すると、環境 Shader としてはうるさくなりやすい。

---

# 16. Specular / Highlight

## 16.1 基本方針

Specular は持つ。
ただし、フォトリアルな鋭い反射ではなく、以下を目指す。

```text
- 広い
- 弱い
- 少し段階化されている
- 粘土・陶器・マットプラスチック向け
```

## 16.2 プロパティ

```text
_SpecularStrength
_SpecularSmoothness
_SpecularStepCount
_SpecularStepSmoothness
_SpecularColor
_SpecularNoiseStrength
```

## 16.3 モード

```text
_SpecularMode
  Off
  Soft
  Quantized
  Ceramic
  Plastic
```

### Soft

粘土・石膏向け。

```text
弱く広いハイライト
```

### Quantized

少しゲーム的。

```text
2～4段階のハイライト
```

### Ceramic

陶器向け。

```text
やや狭く、しかし強すぎない
```

### Plastic

マットプラスチック向け。

```text
広めで、少し玩具的
```

---

# 17. Fresnel / Edge Sheen

## 17.1 Outline ではない

輪郭線は出さない。
ただし、面の端にわずかな光を乗せる **Edge Sheen** は許可する。

```text
_EdgeSheenStrength
_EdgeSheenPower
_EdgeSheenColor
_EdgeSheenLightDependency
```

用途：

```text
- 壁の端を少し読みやすくする
- 粘土の丸みを出す
- おもちゃ感を足す
```

ただし、強すぎると Rim Light っぽくなり、キャラ Toon に寄るためデフォルトは弱め。

```text
Default EdgeSheenStrength = 0.05
```

---

# 18. Surface Noise

## 18.1 World Space Noise

地面・壁用では UV 依存ノイズだけでは不十分。
ProBuilder の面やモジュール壁では UV 継ぎ目が目立つため、World Space Noise を正式機能にする。

```text
_WorldNoiseScale
_WorldNoiseStrength
_WorldNoiseContrast
_WorldNoiseMode
```

```text
WorldNoiseMode:
  Off
  ObjectSpace
  WorldSpace
  Triplanar
```

## 18.2 Triplanar

Triplanar は高品質だが重い。
そのため、Feature Toggle にする。

```text
_USE_TRIPLANAR
```

使用推奨：

```text
- 大きな壁
- 床
- UV が雑な ProBuilder Mesh
```

非推奨：

```text
- 小物
- モバイル低負荷向け
- 大量の細かいオブジェクト
```

---

# 19. Light Band Noise

## 19.1 目的

段階影の境界を、完全な直線・完全なセル境界にしない。

```text
_LightBandNoiseStrength
_LightBandNoiseScale
_LightBandNoiseSpeed
_LightBandNoiseMode
```

基本は静的ノイズ。

```text
LightBandNoiseSpeed = 0
```

動かすとチラつくため、基本的にアニメーションさせない。

---

## 19.2 距離減衰

遠くでノイズが目立つと画面が汚くなる。

```text
_NoiseDistanceFadeStart
_NoiseDistanceFadeEnd
```

仕様：

```text
近距離:
  ノイズあり

遠距離:
  ノイズ弱め
```

---

# 20. Vertex Color 対応

## 20.1 用途

ProBuilder / モジュールレベル制作では Vertex Color が非常に便利。

```text
Vertex Color R:
  汚れ / Cavity / AO 補正

Vertex Color G:
  Light Band 補正

Vertex Color B:
  Color Variation

Vertex Color A:
  Wetness / Smoothness / Special Mask
```

## 20.2 モード

```text
_VertexColorMode
  Off
  MultiplyBase
  AddVariation
  MaskOnly
```

最初から複雑にしすぎると破綻するため、v1 では以下だけでもよい。

```text
R = Cavity Mask
G = Band Offset Mask
B = Color Variation Mask
A = Effect Mask
```

---

# 21. Height / World Gradient

## 21.1 World Y Gradient

地面・壁・柱にかなり有効。

```text
_UseWorldYGradient
_WorldYGradientColorTop
_WorldYGradientColorBottom
_WorldYGradientStart
_WorldYGradientEnd
_WorldYGradientStrength
```

用途：

```text
床に近い壁を少し暗くする
上部を少し明るくする
洞窟や室内で高さ方向の雰囲気を出す
```

---

# 22. Cavity / Contact 感

## 22.1 Shader 単体でできる範囲

Shader 単体では本物の接地 AO は難しい。
ただし以下は可能。

```text
- AO Map
- Vertex Color
- Height Gradient
- Noise Mask
- SSAO 連携
```

URP の SSAO と連携する場合、Custom Shader 側で DepthNormals Pass と `_SCREEN_SPACE_OCCLUSION` keyword が必要になる点は注意です。Unity の URP 17 アップグレードガイドでも、Custom Shader で SSAO をサポートするには DepthNormals Pass と `_SCREEN_SPACE_OCCLUSION` keyword が必要とされています。([Unity マニュアル][3])

---

# 23. Render Pass 構成

## 23.1 必須 Pass

```text
ForwardLit
ShadowCaster
DepthOnly
DepthNormals
Meta
```

### ForwardLit

本体描画。

```text
- Main Light
- Additional Lights
- Baked GI
- Fog
- Shadow
- Stylized Lighting
```

### ShadowCaster

影描画用。

```text
- Alpha Clip 対応
- Cull 設定対応
```

### DepthOnly

Depth Prepass / Depth Texture 用。

Unity 公式にも URP Custom Shader で DepthOnly Pass を書く例があります。Depth Texture や Depth Prepass 系の動作を考えるなら、この Pass は軽視しない方がいいです。([Unity マニュアル][4])

### DepthNormals

SSAO / DepthNormals Texture 用。

```text
- Normal 出力
- Alpha Clip 対応
```

### Meta

Lightmap Bake 用。

```text
- Albedo
- Emission
```

---

# 24. Shadow 設計

## 24.1 Receive Shadows

```text
_ReceiveShadows
```

有効 / 無効。

## 24.2 Cast Shadows

Cast Shadows は MeshRenderer 側の責務。
ただし Shader Pass は ShadowCaster を正しく持つ。

## 24.3 室内箱での注意

完全な箱の内側にマップを作る場合：

```text
- 法線は内側
- Cull は Front
- Cast Shadows は用途に応じて調整
- 内部ライトを置く
```

外からの Directional Light だけで完全密閉空間を照らそうとしない。
Shader 側で多少補正しても、空間設計として不自然になる。

---

# 25. Material Preset

## 25.1 Clay Diorama

```text
用途:
  粘土、手作り地形、柔らかい壁

LightStepCount: 4
WrapLighting: 0.45
SpecularStrength: 0.08
Smoothness: 0.25
WorldNoiseStrength: 0.15
ShadowColor: 紫灰
LightColor: クリーム
```

---

## 25.2 Painted Plaster

```text
用途:
  壁、祠、遺跡、漆喰

LightStepCount: 5
WrapLighting: 0.3
SpecularStrength: 0.05
NormalNoiseStrength: 0.2
CavityStrength: 0.4
WorldYGradient: On
```

---

## 25.3 Matte Toy Plastic

```text
用途:
  ブロック床、ギミック、玩具的構造物

LightStepCount: 3
WrapLighting: 0.25
SpecularStrength: 0.25
Smoothness: 0.45
WorldNoiseStrength: 0.05
ColorVariation: Low
```

---

## 25.4 Ceramic Toy

```text
用途:
  タイル、柱、装飾パーツ

LightStepCount: 4
WrapLighting: 0.2
SpecularStrength: 0.35
Smoothness: 0.6
SpecularMode: Ceramic
Noise: Low
```

---

## 25.5 Chalk / Pastel

```text
用途:
  夢っぽい空間、柔らかい背景

LightStepCount: 5
WrapLighting: 0.5
SpecularStrength: 0.0
BandNoiseStrength: 0.2
AlbedoNoiseStrength: 0.25
Saturation: 少し低め
```

---

# 26. Inspector 設計

Material Inspector は必ず整理する。
この Shader はプロパティが多くなるため、雑に並べると使い物にならない。

## 26.1 Sections

```text
Surface
Base
Lighting
Shadow
Ambient / Bounce
Specular
Noise / Texture
Cavity / AO
Vertex / World Gradient
Advanced
Debug
```

---

## 26.2 Surface

```text
Surface Type
Cull Mode
Alpha Clip
Cutoff
Receive Shadows
```

---

## 26.3 Lighting

```text
Light Step Count
Light Step Smoothness
Wrap Lighting
Band Contrast
Band Offset
Band Noise Strength
Deep Shadow Color
Shadow Color
Mid Color
Light Color
Highlight Color
```

---

## 26.4 Ambient / Bounce

```text
Ambient Strength
Ambient Top Color
Ambient Side Color
Ambient Bottom Color
Bounce Strength
Bounce Color
Bounce Direction
Indirect Strength
```

---

## 26.5 Specular

```text
Specular Mode
Specular Strength
Smoothness
Specular Step Count
Specular Smoothness
Specular Color
```

---

## 26.6 Noise

```text
World Noise Mode
World Noise Scale
World Noise Strength
World Noise Contrast
Triplanar Enabled
Band Noise Scale
Band Noise Strength
Distance Fade
```

---

## 26.7 Debug

必ず Debug View を持たせる。

```text
_DebugView
  Off
  NdotL
  WrappedLight
  SteppedLight
  ShadowAttenuation
  BandNoise
  WorldNoise
  AO
  Cavity
  BakedGI
  Specular
  VertexColor
```

Shader 開発では Debug View がないと、原因切り分けが地獄になります。

---

# 27. Shader Variant 方針

## 27.1 shader_feature_local

Material ごとの機能切り替えは `shader_feature_local` を使う。

候補：

```text
_USE_NORMALMAP
_USE_DETAIL_NORMAL
_USE_OCCLUSIONMAP
_USE_EMISSION
_USE_TRIPLANAR
_USE_VERTEX_COLOR
_USE_RAMP_TEXTURE
_USE_WORLD_Y_GRADIENT
_USE_BAND_NOISE
_USE_STYLIZED_SPECULAR
```

## 27.2 multi_compile

URP ライト・影・Fog など、Pipeline 側に必要なものは URP 標準に合わせる。

```text
_MAIN_LIGHT_SHADOWS
_ADDITIONAL_LIGHTS
_ADDITIONAL_LIGHT_SHADOWS
_SHADOWS_SOFT
_MIXED_LIGHTING_SUBTRACTIVE
_LIGHTMAP_ON
_DIRLIGHTMAP_COMBINED
_FOG
_SCREEN_SPACE_OCCLUSION
```

---

# 28. Performance Tier

## 28.1 Low

```text
- Main Light のみ
- No Triplanar
- No Additional Lights
- No Detail Normal
- No Band Noise
- Simple Specular
```

用途：

```text
大量配置
低負荷環境
遠景
```

---

## 28.2 Medium

```text
- Main Light
- Additional Light FillOnly
- World Noise
- Normal Map
- AO
- Stylized Specular
```

標準推奨。

---

## 28.3 High

```text
- Triplanar
- Band Noise
- Detail Normal
- Directional Ambient
- Bounce
- Quantized Specular
- Vertex Color Mask
- SSAO support
```

近景や重要な部屋用。

---

# 29. ProBuilder / レベル制作ガイド

## 29.1 内側から見る箱

```text
- 法線を内側に向ける
- Material Cull は Front
- Shader 側の Front / Back 意味を標準化する
```

## 29.2 壁・床は少しベベルする

完全な直角面だけだと光が乗りにくい。

```text
推奨:
  ほんの少し Bevel する

理由:
  エッジに光が乗る
  おもちゃ感が出る
  面の境界が読みやすい
```

## 29.3 UV が雑でも成立させる

ProBuilder では UV が雑になりやすいため、

```text
- World Space Noise
- Triplanar
- Vertex Color
```

を使って破綻を減らす。

---

# 30. 実装フェーズ

## Phase 0: 最小 Lit Shader

実装状態: 未着手

```text
- URP ForwardLit
- BaseMap
- BaseColor
- NormalMap
- Smoothness
- Specular
- ShadowCaster
- DepthOnly
- DepthNormals
- Meta
```

この段階では見た目は普通の Lit に近くてよい。

---

## Phase 1: x段階 Diffuse

実装状態: 未着手

```text
- NdotL
- WrapLighting
- LightStepCount
- LightStepSmoothness
- ShadowColor / MidColor / LightColor
- DebugView: NdotL / SteppedLight
```

ここが最初の完成ライン。

---

## Phase 2: 色付き影と Ambient

実装状態: 未着手

```text
- DeepShadowColor
- Ambient Top / Side / Bottom
- Bounce Light
- ShadowSoftFill
```

この段階で「黒く潰れない室内」を作る。

---

## Phase 3: Surface Noise

実装状態: 未着手

```text
- World Space Noise
- Albedo Noise
- Band Noise
- Noise Distance Fade
```

ここで「ただの Toon」から抜ける。

---

## Phase 4: Specular

実装状態: 未着手

```text
- Soft Specular
- Quantized Specular
- Ceramic / Plastic mode
```

おもちゃ感を追加する。

---

## Phase 5: AO / Cavity / Vertex Color

実装状態: 未着手

```text
- AO Map
- Cavity Color
- Vertex Color Mask
- World Y Gradient
```

レベル制作向けに実用化する。

---

## Phase 6: Inspector / Preset / Debug

実装状態: 未着手

```text
- Custom ShaderGUI
- Preset 保存
- Debug View
- Material Validation
```

ここまでやって、ようやく運用可能。

---

# 31. 受け入れ基準

## 31.1 見た目

以下を満たすこと。

```text
- 通常 Lit よりもライト段階が整理されている
- Toon Shader ほどアニメ調に寄りすぎない
- 影が黒く潰れない
- 壁・床の広い面が単調にならない
- 粘土 / 石膏 / マット玩具的な質感が出る
- Outline なしでも形が読みやすい
```

---

## 31.2 機能

```text
- BaseMap / BaseColor 対応
- NormalMap 対応
- Metallic / Specular 入力対応
- Smoothness 対応
- Occlusion 対応
- Emission 対応
- Main Light 対応
- Additional Lights 対応
- Shadows 対応
- Lightmap / Probe 対応
- DepthOnly 対応
- DepthNormals 対応
- SSAO 連携可能
- Fog 対応
```

---

## 31.3 ProBuilder 適性

```text
- 内向き法線の部屋で破綻しない
- Front / Back の意味が標準と一致する
- UV が多少雑でも World Noise で成立する
- 大きな Plane / 壁 / 床で使える
```

---

## 31.4 Performance

```text
Medium 設定で、通常の URP Lit より極端に重くならないこと。
High 設定では重くなってよいが、使用箇所を限定できること。
```

---

# 32. 実装上の厳しい注意点

## 32.1 Lit 完全互換を目指しすぎない

これは危険です。

```text
URP Lit の全機能
+
独自ライトモデル
+
Triplanar
+
Noise
+
Specular段階化
+
SSAO
+
BakedGI
```

を全部完璧にやろうとすると、Shader が肥大化します。

方針はこれ。

```text
入力は Lit に近い
出力は Stylized
互換性より絵作りを優先
```

---

## 32.2 地形用 Shader とキャラ用 Shader を混ぜない

この Shader にキャラクター表現まで背負わせると失敗します。

```text
キャラ:
  別 Toon / Character Shader

環境:
  EnvironmentStylizedLit
```

役割分離するべきです。

---

## 32.3 Triplanar は標準 ON にしない

便利ですが重いです。

```text
Default:
  Off

必要な大面積壁・床:
  On
```

---

## 32.4 影の段階化を強くしすぎない

`LightStepCount = 2` や境界硬めは、すぐに普通の Toon になります。

推奨初期値：

```text
LightStepCount = 4
LightStepSmoothness = 0.15
WrapLighting = 0.35
BandNoiseStrength = 0.08
```

---

# 33. 初期デフォルト値

```text
BaseColor = White

LightStepCount = 4
LightStepSmoothness = 0.15
WrapLighting = 0.35
BandContrast = 1.0
BandOffset = 0.0
BandNoiseStrength = 0.06

DeepShadowColor = blue-gray / purple-gray
ShadowColor = soft cool gray
MidColor = neutral
LightColor = warm cream
HighlightColor = pale yellow

AmbientStrength = 0.25
BounceStrength = 0.15

SpecularMode = Soft
SpecularStrength = 0.12
Smoothness = 0.35
SpecularStepCount = 3

WorldNoiseMode = WorldSpace
WorldNoiseScale = 2.0
WorldNoiseStrength = 0.08

OcclusionStrength = 0.5
CavityStrength = 0.25

EdgeSheenStrength = 0.04
```

---

# 34. 最小実装時の完成イメージ

最初の完成版は、以下のような Shader でよい。

```text
BC/EnvironmentStylizedLit

特徴:
  - Opaque
  - Front / Back / Both Culling
  - BaseMap
  - NormalMap
  - x段階 Diffuse
  - Wrap Lighting
  - 色付き Shadow
  - Ambient Top / Side / Bottom
  - Soft Specular
  - World Space Noise
  - ShadowCaster
  - DepthOnly
  - DepthNormals
  - Meta
  - Debug View
```

これだけでも、普通の URP Lit とは明確に違う見た目になります。

---

# 35. 最終的な方向性

この Shader の価値は、単なる Toon ではなく、

```text
ライトの段階化
+
色付き影
+
疑似バウンス
+
ざらついた面
+
弱い反射
+
環境メッシュ向けの実用性
```

をまとめて扱えることです。

最終的に目指すべき見た目はこれです。

```text
リアルではない。
でも安い Toon でもない。
玩具、粘土、石膏、マットプラスチックの中間。
ライトの当たり方が整理されていて、
床や壁の広い面にも表情がある。
```

この方向は、BombCourier のような 3D アクション寄りのポートフォリオ作品にもかなり相性が良いです。
特に ProBuilder で作ったマップに使うなら、**形状が多少シンプルでも、光と質感で見栄えを底上げできる**のが強いです。

はい。これは必須です。
この規模の Shader を `.shader` 1枚に全部書くと、**確実に保守不能**になります。

以下を仕様書に追加する形で入れるのが良いです。

---

# 36. ファイル分割設計

## 36.1 基本方針

`EnvironmentStylizedLit` は、`.shader` ファイルに全ロジックを書かない。

`.shader` は以下だけを持つ。

```text
- Properties
- SubShader / Pass 定義
- Tags
- Blend / Cull / ZWrite / ZTest
- pragma
- include の接続
```

実際の計算処理は `.hlsl` に分割する。

理由：

```text
- ForwardLit / ShadowCaster / DepthOnly / DepthNormals / Meta の責務が違う
- Lighting / Noise / Triplanar / Debug / SurfaceData を分けないと肥大化する
- URP のバージョン差分対応が難しくなる
- Debug View や Quality Tier の追加で壊れやすくなる
- AI や人間が局所修正しにくくなる
```

---

# 37. 推奨フォルダ構成

```text
Assets/
  Art/
    Shader/
      EnvironmentStylizedLit/
        EnvironmentStylizedLit.shader

        HLSL/
          EnvironmentStylizedLit_Input.hlsl
          EnvironmentStylizedLit_Common.hlsl
          EnvironmentStylizedLit_Surface.hlsl
          EnvironmentStylizedLit_Lighting.hlsl
          EnvironmentStylizedLit_StylizedDiffuse.hlsl
          EnvironmentStylizedLit_Specular.hlsl
          EnvironmentStylizedLit_Ambient.hlsl
          EnvironmentStylizedLit_Noise.hlsl
          EnvironmentStylizedLit_Triplanar.hlsl
          EnvironmentStylizedLit_Debug.hlsl

          Passes/
            EnvironmentStylizedLit_ForwardLitPass.hlsl
            EnvironmentStylizedLit_ShadowCasterPass.hlsl
            EnvironmentStylizedLit_DepthOnlyPass.hlsl
            EnvironmentStylizedLit_DepthNormalsPass.hlsl
            EnvironmentStylizedLit_MetaPass.hlsl

        Editor/
          EnvironmentStylizedLitShaderGUI.cs
          EnvironmentStylizedLitMaterialValidator.cs
          EnvironmentStylizedLitPresetUtility.cs

        Presets/

        Documentation/
          README.md

    Materials/
        EnvironmentStylizedLit/
        ESL_Test_Default.mat
        ESL_Test_Interior.mat

  Scenes/
    EnvironmentStylizedLit/
      ESL_TestRoom.unity
      ESL_LightingLab.unity
```

`Presets` は最終プリセットの置き場だけ M0 で確保し、実プリセットの量産は M12 以降に行う。
`Documentation` はモジュール内の案内のみを置き、仕様の正は `Assets/Docs/ShaderSpec.md` と `Assets/Docs/ShaderMilestoneSpec.md` に残す。

---

# 38. 各ファイルの責務

## 38.1 EnvironmentStylizedLit.shader

`.shader` 本体。

ここには **ロジックを書かない**。

責務：

```text
- Material Properties の宣言
- RenderType / Queue / UniversalMaterialType などの Tags
- SubShader 定義
- ForwardLit / ShadowCaster / DepthOnly / DepthNormals / Meta Pass 定義
- pragma / keyword 定義
- HLSL include の入口
```

禁止：

```text
- Lighting 計算を直接書く
- Noise 関数を書く
- SurfaceData の構築処理を書く
- Debug View の分岐を書く
- Triplanar 計算を書く
```

`.shader` はあくまで **配線ファイル** として扱う。

---

## 38.2 EnvironmentStylizedLit_Input.hlsl

Material Property と CBUFFER を定義する。

責務：

```text
- UnityPerMaterial CBUFFER
- Texture / Sampler 宣言
- keyword 依存の入力定義
- Material Property の名前を一元管理
```

例：

```hlsl
#ifndef ENVIRONMENT_STYLIZED_LIT_INPUT_INCLUDED
#define ENVIRONMENT_STYLIZED_LIT_INPUT_INCLUDED

CBUFFER_START(UnityPerMaterial)

float4 _BaseColor;
float4 _BaseMap_ST;

float _Cutoff;
float _Surface;
float _Cull;

float _LightStepCount;
float _LightStepSmoothness;
float _WrapLighting;
float _BandContrast;
float _BandOffset;
float _LightBandNoiseStrength;

float4 _DeepShadowColor;
float4 _ShadowColor;
float4 _MidColor;
float4 _LightColor;
float4 _HighlightColor;

float _AmbientStrength;
float4 _AmbientTopColor;
float4 _AmbientSideColor;
float4 _AmbientBottomColor;

float _BounceStrength;
float4 _BounceColor;
float3 _BounceDirection;
float _BounceWrap;

float _SpecularStrength;
float _Smoothness;
float _SpecularStepCount;
float _SpecularStepSmoothness;
float4 _SpecularColor;

float _WorldNoiseScale;
float _WorldNoiseStrength;
float _WorldNoiseContrast;

float _OcclusionStrength;
float4 _OcclusionColor;
float _CavityStrength;
float4 _CavityColor;

float _EdgeSheenStrength;
float _EdgeSheenPower;
float4 _EdgeSheenColor;

float _DebugView;

CBUFFER_END

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);

TEXTURE2D(_OcclusionMap);
SAMPLER(sampler_OcclusionMap);

TEXTURE2D(_EmissionMap);
SAMPLER(sampler_EmissionMap);

#endif
```

注意点：

```text
- CBUFFER は複数ファイルに分散させない
- Material Property 名はここを正とする
- shader_feature によって未使用になる値も、基本はここで一元管理する
```

---

## 38.3 EnvironmentStylizedLit_Common.hlsl

汎用関数・共通型を定義する。

責務：

```text
- 共通 struct
- saturate 系 helper
- remap
- safe normalize
- quantize
- smooth quantize
- color utility
- debug utility の基礎
```

ここには Shader 固有の強い意味を持つ処理は入れすぎない。

良い例：

```hlsl
float Remap01(float value, float inMin, float inMax)
{
    return saturate((value - inMin) / max(inMax - inMin, 1e-5));
}

float Quantize01(float value, float steps)
{
    steps = max(steps, 1.0);
    return floor(saturate(value) * steps) / max(steps - 1.0, 1.0);
}
```

悪い例：

```hlsl
float ComputeClayDioramaLighting(...)
```

これは `Lighting` 側に置くべき。

---

## 38.4 EnvironmentStylizedLit_Surface.hlsl

SurfaceData を構築する。

責務：

```text
- BaseMap sampling
- BaseColor 適用
- NormalMap 適用
- OcclusionMap sampling
- EmissionMap sampling
- VertexColor 適用
- AlphaClip 判定に必要な値の構築
```

ここで作るべき構造体：

```hlsl
struct ESL_SurfaceData
{
    float3 albedo;
    float alpha;

    float3 normalTS;
    float metallic;
    float smoothness;
    float occlusion;

    float3 emission;

    float cavity;
    float colorVariationMask;
    float bandOffsetMask;
    float specialMask;
};
```

`ESL_` prefix を使う。
Unity / URP 側の `SurfaceData` と名前衝突しないようにする。

---

## 38.5 EnvironmentStylizedLit_Lighting.hlsl

最終 Lighting の統合処理。

責務：

```text
- Main Light の取得
- Shadow Attenuation の反映
- Additional Lights の合成
- Baked GI / Light Probe の合成
- Stylized Diffuse / Ambient / Specular / Edge Sheen の統合
- Fog 適用前の final color 作成
```

ここは「司令塔」にする。
細かい計算は別ファイルへ逃がす。

依存先：

```text
- StylizedDiffuse
- Specular
- Ambient
- Noise
- Debug
```

---

## 38.6 EnvironmentStylizedLit_StylizedDiffuse.hlsl

x段階ライトの中核。

責務：

```text
- NdotL 計算
- Wrap Lighting
- Light Band Quantize
- Band Smoothness
- Band Noise 適用
- Light Ramp 評価
- Shadow Color / Light Color の補間
```

ここに入れる関数例：

```hlsl
float ESL_ComputeWrappedNdotL(float ndotl, float wrap)
{
    return saturate((ndotl + wrap) / (1.0 + wrap));
}

float ESL_ComputeSteppedLight(float value, float stepCount, float smoothness)
{
    value = saturate(value);
    stepCount = max(stepCount, 1.0);

    float scaled = value * stepCount;
    float lower = floor(scaled) / stepCount;
    float upper = ceil(scaled) / stepCount;
    float t = smoothstep(0.5 - smoothness, 0.5 + smoothness, frac(scaled));

    return lerp(lower, upper, t);
}
```

---

## 38.7 EnvironmentStylizedLit_Ambient.hlsl

方向性 Ambient / Bounce を担当。

責務：

```text
- Top / Side / Bottom Ambient
- Normal 方向による環境色補間
- Bounce Light
- BakedGI の stylized 補正
```

関数例：

```hlsl
float3 ESL_EvaluateDirectionalAmbient(float3 normalWS)
{
    float up = saturate(normalWS.y);
    float down = saturate(-normalWS.y);
    float side = saturate(1.0 - abs(normalWS.y));

    float3 ambient =
        _AmbientTopColor.rgb * up +
        _AmbientSideColor.rgb * side +
        _AmbientBottomColor.rgb * down;

    return ambient * _AmbientStrength;
}
```

---

## 38.8 EnvironmentStylizedLit_Specular.hlsl

Stylized Specular を担当。

責務：

```text
- Soft Specular
- Quantized Specular
- Ceramic / Plastic 風ハイライト
- Specular Noise
- Smoothness 反映
```

注意：

```text
- PBR の完全な BRDF 再現を目指さない
- ただし入力は URP Lit 的に Smoothness / Specular を持つ
- 結果は絵作り優先
```

---

## 38.9 EnvironmentStylizedLit_Noise.hlsl

Noise 関連。

責務：

```text
- Hash Noise
- Value Noise
- Simple Noise
- World Space Noise
- Object Space Noise
- Band Noise
- Distance Fade
```

注意：

```text
- ノイズは重くなりやすい
- 最初から高品質ノイズを入れすぎない
- v1 では軽量 Value Noise 程度でよい
- 必要なら後から Texture Noise / Blue Noise に対応する
```

禁止：

```text
- Lighting 計算をここに置かない
- Triplanar Sampling をここに混ぜない
```

Triplanar は別ファイルにする。

---

## 38.10 EnvironmentStylizedLit_Triplanar.hlsl

Triplanar 専用。

責務：

```text
- World Space Triplanar UV 計算
- BaseMap Triplanar Sampling
- NormalMap Triplanar Sampling
- Noise Triplanar Sampling
```

注意：

```text
- Triplanar は重い
- shader_feature_local で完全に切れるようにする
- 通常 UV sampling と処理を混ぜない
```

重要：

```text
_USE_TRIPLANAR が OFF のとき、このファイル内の重い処理が実質使われないこと
```

---

## 38.11 EnvironmentStylizedLit_Debug.hlsl

Debug View 専用。

責務：

```text
- DebugView enum 相当の分岐
- NdotL 表示
- WrappedLight 表示
- SteppedLight 表示
- ShadowAttenuation 表示
- BandNoise 表示
- WorldNoise 表示
- AO 表示
- Cavity 表示
- Specular 表示
- VertexColor 表示
```

Debug 処理を ForwardLitPass に直書きしない。
Debug は後から必ず増えるため、専用ファイルに隔離する。

---

# 39. Pass ファイル設計

## 39.1 ForwardLitPass

```text
HLSL/Passes/EnvironmentStylizedLit_ForwardLitPass.hlsl
```

責務：

```text
- Attributes 定義
- Varyings 定義
- Vertex Shader
- Fragment Shader
- SurfaceData 構築
- InputData 構築
- Lighting 評価の呼び出し
- DebugView 適用
- Fog 適用
```

ForwardLitPass は太くなるが、**計算ロジックは持たない**。

イメージ：

```hlsl
half4 frag(Varyings input) : SV_Target
{
    ESL_SurfaceData surfaceData = ESL_InitializeSurfaceData(input);

    ESL_InputData inputData = ESL_InitializeInputData(input, surfaceData);

    float3 color = ESL_EvaluateLighting(inputData, surfaceData);

    color = ESL_ApplyDebugView(color, inputData, surfaceData);

    color = MixFog(color, inputData.fogCoord);

    return half4(color, surfaceData.alpha);
}
```

---

## 39.2 ShadowCasterPass

```text
HLSL/Passes/EnvironmentStylizedLit_ShadowCasterPass.hlsl
```

責務：

```text
- ShadowCaster 用 Vertex / Fragment
- AlphaClip 対応
- Cull 設定反映
```

Stylized Lighting はここに入れない。

必要なのは：

```text
- Position
- AlphaClip
- Shadow Bias
```

だけ。

---

## 39.3 DepthOnlyPass

```text
HLSL/Passes/EnvironmentStylizedLit_DepthOnlyPass.hlsl
```

責務：

```text
- Depth Texture 用
- AlphaClip 対応
```

Lighting / Noise / Specular は不要。
AlphaClip に関係する BaseMap sampling だけは必要。

---

## 39.4 DepthNormalsPass

```text
HLSL/Passes/EnvironmentStylizedLit_DepthNormalsPass.hlsl
```

責務：

```text
- DepthNormals Texture 出力
- NormalMap 反映
- SSAO 対応
- AlphaClip 対応
```

この Pass はかなり重要です。
URP の SSAO を使うなら、DepthNormals が壊れると見た目が破綻します。

---

## 39.5 MetaPass

```text
HLSL/Passes/EnvironmentStylizedLit_MetaPass.hlsl
```

責務：

```text
- Lightmap Bake 用
- Albedo 出力
- Emission 出力
```

注意：

```text
- Stylized Lighting を MetaPass に入れない
- Lightmap に段階影を焼き込みすぎない
- 基本は Albedo / Emission を正しく出す
```

---

# 40. Include 依存ルール

## 40.1 依存方向

依存方向は固定する。

```text
Input
  ↓
Common
  ↓
Surface / Noise / Triplanar
  ↓
StylizedDiffuse / Ambient / Specular
  ↓
Lighting
  ↓
Pass
```

禁止される依存：

```text
Surface が Lighting を include する
Noise が Lighting を include する
Common が Shader 固有の Lighting を include する
Debug が Pass に依存する
```

これを守らないと、すぐ循環依存に近い状態になります。

---

## 40.2 Include Guard

全 `.hlsl` に必ず include guard を入れる。

```hlsl
#ifndef ENVIRONMENT_STYLIZED_LIT_LIGHTING_INCLUDED
#define ENVIRONMENT_STYLIZED_LIT_LIGHTING_INCLUDED

// content

#endif
```

---

# 41. 命名規則

## 41.1 HLSL 関数 Prefix

すべての独自関数に `ESL_` prefix を付ける。

```text
ESL_ = Environment Stylized Lit
```

例：

```hlsl
ESL_InitializeSurfaceData
ESL_EvaluateLighting
ESL_ComputeWrappedNdotL
ESL_EvaluateBandColor
ESL_EvaluateDirectionalAmbient
ESL_EvaluateStylizedSpecular
ESL_ApplyDebugView
```

理由：

```text
- URP の関数名と衝突しにくい
- AI が編集するときに独自関数を見分けやすい
- grep しやすい
```

---

## 41.2 Struct Prefix

独自 struct も `ESL_` prefix。

```hlsl
struct ESL_SurfaceData
{
};

struct ESL_InputData
{
};

struct ESL_LightingData
{
};

struct ESL_DebugData
{
};
```

URP 側にも `SurfaceData` / `InputData` があるため、素の名前は使わない。

---

# 42. Editor ファイル分割

Shader 本体だけではなく、Editor 側も分ける。

## 42.1 EnvironmentStylizedLitShaderGUI.cs

Material Inspector 本体。

責務：

```text
- セクション表示
- 折りたたみ
- Property 描画
- keyword 更新の呼び出し
- Validation の呼び出し
```

ここに Validation ロジックを直書きしない。

---

## 42.2 EnvironmentStylizedLitMaterialValidator.cs

Material 設定の検証。

責務：

```text
- 不正な値の補正
- keyword 整合性確認
- Cull / Surface / AlphaClip の整合性確認
- Triplanar 使用時の警告
- DebugView 使用中の警告
```

例：

```text
- Transparent は非対応なので Opaque に戻す
- StepCount が 1 未満なら 1 に補正
- Triplanar ON かつ NormalMap ON の場合、負荷警告
- DebugView が Off 以外なら Inspector に警告表示
```

---

## 42.3 EnvironmentStylizedLitPresetUtility.cs

Preset 作成・適用用。

責務：

```text
- ClayDiorama preset 適用
- PaintedPlaster preset 適用
- MatteToyPlastic preset 適用
- CeramicToy preset 適用
- ChalkPastel preset 適用
```

Material Inspector から直接 preset 値を大量に持たない。
プリセット値は Utility 側に逃がす。

---

# 43. Property Reference を別ファイル化する

仕様書とは別に、以下を作る。

```text
Documentation/
  EnvironmentStylizedLitPropertyReference.md
```

ここには、全 Material Property の意味を書く。

例：

```text
_LightStepCount
  型:
    Float

  範囲:
    1 - 8

  推奨:
    3 - 5

  説明:
    Diffuse Lighting の段階数。
    2以下は Toon 感が強くなりやすい。
    6以上は通常 Lit に近くなり、段階化の意味が弱くなる。

  Default:
    4
```

本体仕様書に全プロパティ説明を詰め込むと読みにくくなるため、**リファレンスを分ける**。

---

# 44. Authoring Guide を別ファイル化する

```text
Documentation/
  EnvironmentStylizedLitAuthoringGuide.md
```

ここには、アーティスト・レベル制作者向けの使い方を書く。

内容：

```text
- ProBuilder の内向き法線で使う方法
- 壁・床での推奨設定
- Clay / Plaster / Plastic の使い分け
- Triplanar を使うべきケース
- LightStepCount の調整指針
- 影が黒いときの対処
- ノイズが汚いときの対処
- 室内ライト設計
```

実装仕様書と運用ガイドは分ける。
混ぜると、エンジニアにもアーティストにも読みにくい文書になります。

---

# 45. 最小構成と完全構成

最初から全ファイルを作ってもよいが、初期実装では段階的に増やす。

## 45.1 Phase 0 最小構成

```text
EnvironmentStylizedLit.shader

HLSL/
  EnvironmentStylizedLit_Input.hlsl
  EnvironmentStylizedLit_Common.hlsl
  EnvironmentStylizedLit_Surface.hlsl
  EnvironmentStylizedLit_Lighting.hlsl
  EnvironmentStylizedLit_StylizedDiffuse.hlsl

  Passes/
    EnvironmentStylizedLit_ForwardLitPass.hlsl
    EnvironmentStylizedLit_ShadowCasterPass.hlsl
    EnvironmentStylizedLit_DepthOnlyPass.hlsl
```

この段階では、以下はまだなくてよい。

```text
- Triplanar
- DepthNormals
- Meta
- Advanced Specular
- VertexColor
- ShaderGUI
- Presets
```

ただし、後から追加できる構造にしておく。

---

## 45.2 Phase 1 実用最小構成

```text
EnvironmentStylizedLit.shader

HLSL/
  EnvironmentStylizedLit_Input.hlsl
  EnvironmentStylizedLit_Common.hlsl
  EnvironmentStylizedLit_Surface.hlsl
  EnvironmentStylizedLit_Lighting.hlsl
  EnvironmentStylizedLit_StylizedDiffuse.hlsl
  EnvironmentStylizedLit_Ambient.hlsl
  EnvironmentStylizedLit_Specular.hlsl
  EnvironmentStylizedLit_Noise.hlsl
  EnvironmentStylizedLit_Debug.hlsl

  Passes/
    EnvironmentStylizedLit_ForwardLitPass.hlsl
    EnvironmentStylizedLit_ShadowCasterPass.hlsl
    EnvironmentStylizedLit_DepthOnlyPass.hlsl
    EnvironmentStylizedLit_DepthNormalsPass.hlsl
    EnvironmentStylizedLit_MetaPass.hlsl

Editor/
  EnvironmentStylizedLitShaderGUI.cs
```

ここまで来ると運用可能。

---

## 45.3 Phase 2 完全構成

```text
HLSL/
  EnvironmentStylizedLit_Triplanar.hlsl

Editor/
  EnvironmentStylizedLitMaterialValidator.cs
  EnvironmentStylizedLitPresetUtility.cs

Presets/
Scenes/EnvironmentStylizedLit/
Documentation/
```

この段階で、品質調整・制作運用・デバッグまで含めた完成形になる。

---

# 46. `.shader` 側の構成イメージ

実際の `.shader` はこの程度の厚みに抑える。

```shaderlab
Shader "BC/EnvironmentStylizedLit"
{
    Properties
    {
        // Surface
        _Cull("Cull", Float) = 2
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5

        // Base
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)

        // Lighting
        _LightStepCount("Light Step Count", Range(1, 8)) = 4
        _LightStepSmoothness("Light Step Smoothness", Range(0, 0.5)) = 0.15
        _WrapLighting("Wrap Lighting", Range(0, 1)) = 0.35

        _DeepShadowColor("Deep Shadow Color", Color) = (0.25,0.25,0.35,1)
        _ShadowColor("Shadow Color", Color) = (0.45,0.45,0.55,1)
        _MidColor("Mid Color", Color) = (1,1,1,1)
        _LightColor("Light Color", Color) = (1.1,1.05,0.9,1)
        _HighlightColor("Highlight Color", Color) = (1.2,1.15,1.0,1)

        // etc...
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _OCCLUSIONMAP
            #pragma shader_feature_local _EMISSION
            #pragma shader_feature_local _USE_BAND_NOISE
            #pragma shader_feature_local _USE_VERTEX_COLOR
            #pragma shader_feature_local _USE_TRIPLANAR

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "HLSL/Passes/EnvironmentStylizedLit_ForwardLitPass.hlsl"

            ENDHLSL
        }

        // ShadowCaster
        // DepthOnly
        // DepthNormals
        // Meta
    }

    CustomEditor "BC.Rendering.EnvironmentStylizedLitShaderGUI"
}
```

重要なのは、`.shader` が **長くても Properties + Pass 配線程度** で収まることです。

---

# 47. 逆にやってはいけない分割

## 47.1 細かすぎる分割

悪い例：

```text
ComputeNdotL.hlsl
ComputeWrap.hlsl
ComputeBandNoise.hlsl
ComputeBandSmoothness.hlsl
ComputeShadowColor.hlsl
ComputeSpecularPower.hlsl
```

これはやりすぎです。
ファイル数が増えすぎて逆に読みにくい。

分割単位は **処理の責務単位** にする。

良い単位：

```text
Surface
Lighting
StylizedDiffuse
Specular
Ambient
Noise
Triplanar
Debug
Passes
```

---

## 47.2 Pass にロジックを詰める

悪い例：

```text
ForwardLitPass.hlsl に
  Surface
  Lighting
  Noise
  Specular
  Debug
  すべてを書く
```

これは `.shader` に全部書くのと大差ありません。

ForwardLitPass は、以下の流れだけを持つ。

```text
入力を受ける
SurfaceData を作る
Lighting を呼ぶ
Debug を適用する
出力する
```

---

## 47.3 Input を複数に分けすぎる

悪い例：

```text
Input_Lighting.hlsl
Input_Noise.hlsl
Input_Specular.hlsl
Input_Ambient.hlsl
```

CBUFFER が分散して管理不能になります。

Material Property は、

```text
EnvironmentStylizedLit_Input.hlsl
```

に集約する。

---

# 48. AI 実装向けの指示方針

Copilot / ChatGPT に実装を投げる前提なら、ファイル単位でタスクを切るべきです。

悪い依頼：

```text
EnvironmentStylizedLit を全部実装して
```

良い依頼：

```text
まず EnvironmentStylizedLit_Input.hlsl と Common.hlsl だけ作成してください。
既存ファイルには触らず、Material Property と共通 helper のみ定義してください。
```

次：

```text
Surface.hlsl を作成してください。
BaseMap / BaseColor / NormalMap / Occlusion / Emission を読み取り、
ESL_SurfaceData を返す関数だけを実装してください。
Lighting はまだ書かないでください。
```

次：

```text
StylizedDiffuse.hlsl を作成してください。
NdotL, WrapLighting, StepCount, StepSmoothness, BandColor 評価までを実装してください。
URP Light 取得処理は書かないでください。
```

このように **依存の下から上に積む**。

順番：

```text
1. Input
2. Common
3. Surface
4. StylizedDiffuse
5. Ambient
6. Specular
7. Noise
8. Lighting
9. ForwardLitPass
10. ShadowCaster / DepthOnly
11. DepthNormals / Meta
12. ShaderGUI
13. Presets / Samples
```

この順番を守るべきです。

---

# 49. 最終的な分割ポリシー

この Shader の分割原則は以下。

```text
.shader
  配線だけ

Input.hlsl
  Material Property だけ

Surface.hlsl
  Texture / Material 入力の解釈だけ

StylizedDiffuse.hlsl
  段階ライトだけ

Ambient.hlsl
  環境光 / バウンスだけ

Specular.hlsl
  反射だけ

Noise.hlsl
  ノイズだけ

Triplanar.hlsl
  Triplanar だけ

Lighting.hlsl
  各要素の統合だけ

Debug.hlsl
  Debug 表示だけ

Passes/*.hlsl
  URP Pass の入出力だけ

Editor/*.cs
  Inspector / Validation / Preset だけ
```

この分割なら、機能追加しても破綻しにくいです。

特に今回の Shader は「基本 Lit + 独自ライト + ノイズ + 環境用機能」なので、普通に書くとすぐ巨大化します。
最初からこの分割で作った方がいいです。


[1]: https://docs.unity3d.com/6000.3/Documentation/Manual/urp/lighting/custom-lighting-introduction.html?utm_source=chatgpt.com "Introduction to custom lighting in URP"
[2]: https://docs.unity3d.com/6000.1/Documentation/Manual/urp/lit-shader.html?utm_source=chatgpt.com "Lit Shader Material Inspector window reference for URP"
[3]: https://docs.unity3d.com/6000.1/Documentation/Manual/urp/upgrade-guide-unity-6.html?utm_source=chatgpt.com "Upgrade to URP 17 (Unity 6.0)"
[4]: https://docs.unity3d.com/6000.3/Documentation/Manual/urp/writing-shaders-urp-depth-only.html?utm_source=chatgpt.com "Write a depth-only pass in a Universal Render Pipeline ..."
