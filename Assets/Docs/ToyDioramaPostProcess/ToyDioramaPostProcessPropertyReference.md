# ToyDiorama PostProcess Property Reference

この文書は current implementation の参照表です。
source of truth は ToyDioramaPostProcessSettings.cs と ToyDioramaPostProcessInspectorUtility.cs です。

## Pipeline

| Property | Type / Range | Default | Notes |
| --- | --- | --- | --- |
| Enabled | bool | true | Feature 全体の on/off。preset 適用では上書きされません。 |
| QualityTier | Low / Medium / High / Cinematic | Medium | 実行コストと有効 effect を切り替えます。 |
| ForceLowQualityTier | bool | false | true の時、authored QualityTier を mutate せず resolved runtime tier だけを Low に固定します。Mobile_Renderer では true、PC_Renderer では false が前提で、違反時は build validator が停止します。 |
| DebugView | enum | Off | Non-development build では Off 以外を残せません。AfterColorGrade は base color grade 後 / Pastel 前の pre-bloom debug stage です。 |

## Core Color Grade

| Property | Range | Default | Meaning |
| --- | --- | --- | --- |
| Exposure | -4.0 to 4.0 | 0.00 | 全体の明るさ。最初に触る基準値です。 |
| Contrast | 0.0 to 3.0 | 1.00 | 中間調の締まり。上げすぎると ESL の面変化を潰します。 |
| Saturation | 0.0 to 2.0 | 1.00 | 彩度全体。Pastel 系より先に極端に上げない方が安定します。 |
| BlackLift | 0.0 to 1.0 | 0.08 | 黒の持ち上げ。暗部を完全な黒に落とさないための値です。 |
| WhiteSoftClamp | 0.0 to 1.0 | 0.25 | 白飛びを柔らかく抑える量です。 |

## Tints

| Property | Range | Default | Meaning |
| --- | --- | --- | --- |
| ShadowTint | Color | 0.46, 0.50, 0.68 | 暗部色。冷たく寄せると玩具感が出ます。 |
| ShadowTintStrength | 0.0 to 1.0 | 0.35 | 暗部 tint の効き量です。 |
| MidTint | Color | 1.00, 1.00, 1.00 | 中間調色。基準はニュートラルです。 |
| MidTintStrength | 0.0 to 1.0 | 0.00 | 中間調 tint の効き量です。 |
| HighlightTint | Color | 1.00, 0.95, 0.86 | 明部色。暖色寄りでクリーム感を補強します。 |
| HighlightTintStrength | 0.0 to 1.0 | 0.20 | 明部 tint の効き量です。 |

## Pastel Compression

| Property | Range | Default | Meaning |
| --- | --- | --- | --- |
| PastelStrength | 0.0 to 1.0 | 0.30 | 彩度をパステル寄りに圧縮する主量です。 |
| HighSaturationCompress | 0.0 to 1.0 | 0.50 | 高彩度部分だけを抑える量です。 |
| PastelLuminanceBias | -1.0 to 1.0 | 0.15 | 圧縮時の明度寄りを調整します。 |

## Cream Highlight

| Property | Range | Default | Meaning |
| --- | --- | --- | --- |
| CreamHighlightColor | Color | 1.00, 0.98, 0.93 | 柔らかい明部色です。 |
| CreamHighlightStrength | 0.0 to 1.0 | 0.25 | 明部のクリーム感の量です。 |
| CreamHighlightThreshold | 0.0 to 1.0 | 0.70 | どの明るさから effect を掛けるかを決めます。 |
| CreamHighlightSoftness | 0.001 to 1.0 | 0.10 | threshold の立ち上がりを柔らかくします。 |

## Edge Tone

| Property | Range | Default | Meaning |
| --- | --- | --- | --- |
| EdgeToneEnabled | bool | true | 輪郭付近の色味整理を有効化します。 |
| EdgeToneColor | Color | 1.00, 0.98, 0.95 | edge の色味です。 |
| EdgeToneStrength | 0.0 to 1.0 | 0.12 | effect の総量です。 |
| EdgeToneRadius | 0.0 to 1.0 | 0.62 | edge 検出の広がりです。 |
| EdgeToneSoftness | 0.001 to 1.0 | 0.22 | edge mask の柔らかさです。 |
| EdgeSaturationFade | 0.0 to 1.0 | 0.18 | edge で彩度を少し抜く量です。 |
| EdgeBrightnessOffset | -0.5 to 0.5 | 0.00 | edge 付近だけ明るさをずらします。 |

## Depth Haze

| Property | Range | Default | Meaning |
| --- | --- | --- | --- |
| DepthHazeEnabled | bool | true | 遠景の空気感 effect を有効化します。Low では無効です。 |
| DepthHazeColor | Color | 0.78, 0.86, 0.92 | haze の色です。 |
| DepthHazeStrength | 0.0 to 1.0 | 0.10 | haze の量です。 |
| DepthHazeStart | 0.0 to 1.0 | 0.45 | haze が見え始める位置です。 |
| DepthHazeEnd | 0.0 to 1.0 | 0.95 | haze が最大に近づく位置です。 |
| DepthHazeSaturationFade | 0.0 to 1.0 | 0.18 | 遠景で彩度を抜く量です。 |
| DepthHazeBrightnessLift | 0.0 to 0.5 | 0.04 | 遠景を少し持ち上げる量です。 |

## Bloom And Halation

| Property | Range | Default | Meaning |
| --- | --- | --- | --- |
| SoftBloomEnabled | bool | true | bloom を有効化します。Low では無効です。 |
| SoftBloomThreshold | 0.0 to 1.0 | 0.82 | bloom 対象の明るさ閾値です。 |
| SoftBloomSoftKnee | 0.0 to 1.0 | 0.18 | bloom の立ち上がりを柔らかくします。 |
| SoftBloomIntensity | 0.0 to 1.0 | 0.14 | bloom の量です。 |
| SoftBloomRadius | 0.0 to 1.0 | 0.65 | bloom の広がりです。 |
| SoftBloomTint | Color | 1.00, 0.96, 0.92 | bloom の色味です。 |
| HalationEnabled | bool | true | halation を有効化します。High 以上でのみ動きます。 |
| HalationStrength | 0.0 to 1.0 | 0.04 | halation の量です。 |
| HalationColor | Color | 1.00, 0.74, 0.62 | halation の色です。 |
| HalationThreshold | 0.0 to 1.0 | 0.88 | halation を掛ける明るさ閾値です。 |
| HalationRadius | 0.0 to 1.0 | 0.55 | halation の広がりです。 |

## Grain

| Property | Range | Default | Meaning |
| --- | --- | --- | --- |
| BlueNoiseTex | Texture2D | null | 旧版の互換バインド用です。現行の grain は procedural で、見た目はこの texture の内容に依存しません。 |
| GrainEnabled | bool | true | film grain を有効化します。Low では無効です。 |
| GrainStrength | 0.0 to 0.2 | 0.02 | film grain の量です。 |
| GrainScale | 0.25 to 8.0 | 1.00 | grain 粒子の大きさです。 |
| GrainResponse | 0.0 to 1.0 | 0.60 | 暗部 / 明部での抑制量です。 |
| GrainTemporalStrength | 0.0 to 1.0 | 0.10 | 時間方向の揺れ量です。 |

## Quality Tier Summary

| Tier | Active Effects | Intended Use |
| --- | --- | --- |
| Low | Color Grade / Pastel / Cream Highlight / Edge Tone only. Depth Haze / Bloom / Halation / Grain は off。 | 最低コスト確認、effect 切り分け |
| Medium | Low + Depth Haze + Grain + Bloom。Bloom は downsample divisor 4、blur pass pair 1。 | 通常 gameplay 基準 |
| High | Medium + Halation。Bloom は downsample divisor 2、blur pass pair 2。 | 演出強めの gameplay / capture |
| Cinematic | High + strongest Bloom blur chain。Bloom は downsample divisor 2、blur pass pair 3。 | 最終画づくり、常時使用は非推奨 |

- QualityTier は effect を強制的に有効化しません。
- authoring 側で off の項目は上位 tier でも off のままです。
- Bloom pass は Bloom / Halation / Bloom debug view のいずれかが必要な時だけ走ります。
- M13 の zero-variant policy により、ToyDiorama shader は shader_feature / multi_compile を増やしません。
- DebugView Off の runtime topology は Low=2、Medium=6、High=8、Cinematic=10 raster pass です。
- Mobile_Renderer は ForceLowQualityTier により resolved runtime tier を Low に固定します。
- ForceLowQualityTier は authored settings を書き換えず、runtime only の tier clamp として扱います。
- inspector は authored Quality Tier と resolved runtime tier を別表示します。

## Debug View Notes

- DebugView は editor と debug build では authoring に使えます。
- release build では Off 以外を残せません。
- feature が enabled のまま DebugView を残すと build validator が停止させます。
- AfterColorGrade は pre-bloom の base color grade 結果を表示し、Pastel / Cream Highlight / Depth Haze / Bloom の後段結果は含みません。

## Preset Apply Contract

- preset apply は visual values だけをコピーします。
- Enabled、QualityTier、DebugView は保持されます。
- preset は starting point であり、apply 後に scene 向けの微調整を続ける前提です。
- Mobile_Renderer では MobileOptimized が supported starting point です。
