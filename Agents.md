これから ShaderMilestoneSpec.md に基づいて、M4 から M14 まで順番に実装を進めます。



最重要方針:
- ShaderMilestoneSpec.md を唯一のマイルストーン基準として扱うこと
- 仕様に書かれていない機能を勝手に追加しないこと
- ただし、実装上必要な最小限の補助型・補助メソッド・テスト用コードは提案してよい
- 既存コードの設計思想を壊さないこと
- 一時しのぎ、暗黙フォールバック、雑な例外握りつぶしは禁止
- 将来の M14 まで拡張できる構造を維持すること
- 実装範囲は常に現在の Mx に限定すること
- 次以降のマイルストーンに必要な布石は作ってよいが、機能の先取り実装はしないこと
- 変更したファイル、理由、テスト方法、未解決リスクを必ず報告すること

特に重視すること:
- URP / Shader / PostProcess の実装として破綻しないこと
- Unity バージョン差分や URP API 差分に弱い実装を避けること
- RenderGraph / ScriptableRendererFeature / ScriptableRenderPass まわりの責務を混ぜないこと
- Runtime と Editor の責務を分離すること
- 設定データ、実行処理、検証処理、プレビュー処理を混在させないこと
- パフォーマンス上の無駄な per-frame allocation を避けること
- shader property ID、material lifecycle、RT lifecycle、pass ordering を慎重に扱うこと
- テスト可能な単位を意識すること
- 妥協なしで実装とコードレビューを繰り返すこと


