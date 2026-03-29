# ManidocMCP_CS — プロジェクト設定（開発憲法 Ver. 9.7 準拠）

## プロジェクト情報
- **技術スタック:** .NET 8 / C# / MCP（Model Context Protocol）
- **規模プロファイル:** Solo
- **対話レベル:** Lv.2（二人三脚） ← デフォルト
- **AI間引き継ぎファイル:** `C:\Project\AI\ManidocMCP_CS\作業メモ.md`
  → セッション開始時に必ず読み込む
  → セッション終了時に必ずこのファイルへ上書き保存する

---

## ⚠️ このプロジェクト固有のルール

```
🔴 GitHubへのpushは指令者の明示的な許可なしに行ってはならない

🔴 Claude Desktop設定の "command" は ManidocMCP.exe（DLLではない）

🔴 インストーラービルドは必ず publish → ISCC.exe の順で実行すること
```

---

## 変更規模分類表（Process Tier）

| クラス | 条件 | 適用プロセス |
|-------|------|-------------|
| **A（軽量）** | ファイル数 ≤ 3、破壊的変更なし | 簡易確認 → 実装 |
| **B（標準）** | ファイル数 4〜10、新規モジュール追加 | 仕様案提示 → 合意 → 実装 |
| **C（重大）** | ファイル数 > 10、破壊的変更 | ADR → 合意 → 実装 → テスト |

---

## コマンド集

| コマンド | 内容 |
|---------|------|
| **publishビルド** | `dotnet publish ManidocMCP.csproj -c Release -r win-x64 --self-contained false -o publish` |
| **インストーラービルド** | `"/c/Program Files (x86)/Inno Setup 6/ISCC.exe" installer/ManidocMCP_Setup.iss` |
| **セッション終了** | `第三条に基づき作業メモを生成せよ` |

---

*ManidocMCP_CS CLAUDE.md — 開発憲法 Ver. 9.7 / 2026-03-30*
