# Hologram Map Renderer タスク分割

## 1. 目的
この文書は、[HologramMapRendererSpec.md](HologramMapRendererSpec.md) を実装に落とし込むためのタスク分解版である。

仕様の内容をそのまま繰り返すのではなく、実装順、依存関係、完了条件が追いやすい形に整理する。

## 2. 実装方針

- まずは最小構成でワイヤーフレーム表示を成立させる
- 次に表示アニメーションと補助演出を追加する
- 最後に LOD、グリッチ、描画順の安定化を行う
- Shader Graph は使わず、HLSL と C# で構成する
- 表示用メッシュはゲーム用メッシュと分離する

## 3. 全体フェーズ

```text
Phase 0: 事前準備
Phase 1: 最小描画の成立
Phase 2: 演出追加
Phase 3: 制御系と運用整備
Phase 4: 品質調整と拡張
```

## 4. Phase 0: 事前準備

### 4.1 目的
実装の土台を整え、後続作業で迷わないようにする。

### 4.2 タスク

- 仕様書の対象ファイルと配置先を確定する
- Hologram 用の Scripts / Shader / Material の置き場を決める
- 既存の URP 設定と透明描画の前提を確認する
- 表示対象が `MeshFilter` 前提か、`SkinnedMeshRenderer` を将来対応にするかを決める
- 使用する色味を 1 系統以上決める

### 4.3 完了条件

- 実装先のフォルダ構成が確定している
- 最小実装に必要な入出力が整理されている
- 以降のタスクがファイル単位で切り出せる

## 5. Phase 1: 最小描画の成立

### 5.1 目的
ホログラムらしい見た目の最小構成を成立させる。

### 5.2 タスク

#### 5.2.1 Mesh 変換
- `HologramMeshBuilder` を作成する
- 元 Mesh を三角形単位に複製する
- 頂点カラーへバリセントリック座標を格納する
- `UInt32` index が必要な場合に切り替える
- Bounds を再計算する

#### 5.2.2 描画コンポーネント
- `HologramMapRendererMB` を作成する
- `MeshFilter` と `MeshRenderer` を受け取る
- 変換後 Mesh を適用する
- 専用 Material を割り当てる
- 必要なら元 Renderer を非表示にする

#### 5.2.3 Shader の最小版
- `BC/Hologram/HologramWireMap` を作成する
- Wireframe 線の描画を実装する
- 薄い半透明 Fill を実装する
- 発光色と Alpha を Material から制御できるようにする
- URP の Transparent 描画として成立させる

#### 5.2.4 見た目確認
- 暗背景で線が読めることを確認する
- 線幅が極端に細くならないことを確認する
- 面が白飛びしないことを確認する
- 透明物として破綻しないことを確認する

### 5.3 完了条件

- 任意の Mesh をホログラムワイヤーとして表示できる
- 面と線の両方が同時に見える
- Shader Graph なしで動作する
- 最低限の見た目がゲーム内で成立する

## 6. Phase 2: 演出追加

### 6.1 目的
単なるワイヤーフレームではなく、ホログラムらしい動きと情報量を付与する。

### 6.2 タスク

#### 6.2.1 Fresnel
- 視線角度に応じたリムライトを追加する
- 線と面の両方に加算できるようにする
- 強すぎると面が読めなくなるため調整値を持たせる

#### 6.2.2 Scan Line
- World Position ベースのスキャンラインを追加する
- 時間で流れるパターンを作る
- 線密度と強度を Material から調整できるようにする

#### 6.2.3 Reveal Animation
- 下から上へ表示されるマスクを追加する
- 表示境界に発光を足す
- `0 -> 1` の遷移をアニメーション化する

#### 6.2.4 Radar Pulse
- 中心点から広がる波を追加する
- 地図表示で読みやすいよう XZ 平面基準で実装する
- 演出開始や更新時に呼べるようにする

### 6.3 完了条件

- 表示開始時の演出が入る
- 静止状態でも情報量が足りる
- 線だけでなく SF 表現として読める

## 7. Phase 3: 制御系と運用整備

### 7.1 目的
表示制御をランタイムから扱いやすくし、Material 管理を安定させる。

### 7.2 タスク

#### 7.2.1 コントローラ
- `HologramMapController` を作成する
- Show / Hide を公開する
- Reveal の進行度を外部から操作できるようにする
- Pulse 中心点を更新できるようにする
- 発光強度を外部から調整できるようにする

#### 7.2.2 MaterialPropertyBlock
- ランタイム変更値を `MaterialPropertyBlock` で渡す
- Material インスタンスを増やしすぎないようにする
- 共有値と個別値を分ける

#### 7.2.3 エディタ運用
- Inspector から主要パラメータを調整できるようにする
- ContextMenu からメッシュ生成を実行できるようにする
- 実行時と編集時で同じ見た目を追えるようにする

### 7.3 完了条件

- 外部コードから表示制御できる
- Material の乱立が起きない
- 1 回の調整で見た目を追いやすい

## 8. Phase 4: 品質調整と拡張

### 8.1 目的
実ゲーム導入に耐える品質へ持っていく。

### 8.2 タスク

#### 8.2.1 グリッチ
- 一時的な異常表現としてグリッチを追加する
- 常時有効にしない
- 演出タイミングに限定する

#### 8.2.2 LOD
- 遠距離用の簡略表示を用意する
- 三角形数の多い対象を分割または簡略化する
- 情報量が多すぎるケースを抑える

#### 8.2.3 描画順の安定化
- 透明物との重なりを確認する
- renderQueue や sorting の方針を決める
- 必要なら Depth Prepass を追加検討する

#### 8.2.4 パフォーマンス確認
- 透明 Overdraw の影響を確認する
- Bloom の強さを調整する
- 大規模マップで破綻しない範囲を把握する

### 8.3 完了条件

- 複数オブジェクトで使っても重すぎない
- 線が潰れず、形が読み取れる
- 運用時の破綻が見つかっても対処方針がある

## 9. ファイル単位の実装候補

### 9.1 Scripts

- `Assets/Scripts/Rendering/HologramMeshBuilder.cs`
- `Assets/Scripts/Rendering/HologramMapRendererMB.cs`
- `Assets/Scripts/Rendering/HologramMapController.cs`
- `Assets/Scripts/Rendering/HologramMaterialPropertyBinder.cs`

### 9.2 Shaders

- `Assets/Art/Shader/Hologram/HologramWireMap.shader`

### 9.3 Materials

- `Assets/Art/Materials/Hologram/M_HologramMap_Green.mat`
- `Assets/Art/Materials/Hologram/M_HologramMap_Cyan.mat`

## 10. 推奨実装順

```text
1. HologramMeshBuilder
2. HologramWireMap shader
3. HologramMapRendererMB
4. 基本色と透明度の調整
5. Fresnel と Scan Line
6. Reveal Animation
7. Radar Pulse
8. HologramMapController
9. MaterialPropertyBlock 対応
10. LOD / Glitch / 描画順の調整
```

## 11. 受け入れ基準

- 仕様書の Phase 1 が単独で動く
- Phase 2 の演出を足しても Phase 1 の表示を壊さない
- 外部制御の入口が整理されている
- 実装途中でも何が未完了か判別できる

## 12. 今後の分割候補
必要なら次の粒度にも分割できる。

- メッシュ生成だけの詳細タスク表
- Shader 実装だけの詳細タスク表
- C# コンポーネントだけの詳細タスク表
- エディタ対応だけの詳細タスク表
- テスト観点だけのチェックリスト
