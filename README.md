# Manidoc MCP Server (Windows)

ドキュメント管理アプリ [Manidoc](https://github.com/ichiroabe/manidoc) のデータを、Claude Desktop などの AI エージェントから読み書きするための MCP (Model Context Protocol) サーバーです。

**English:** An MCP server that lets AI agents (e.g. Claude Desktop) read and write data of the Manidoc document-management app — browse/edit projects and articles, bulk-import Markdown, full-text search, and generate narrated videos. Full English manual: [UserManual.md](UserManual.md).

## できること

- Manidoc のプロジェクト・記事の閲覧・編集
- Markdown テキストからのプロジェクト一括インポート
- 全文検索
- テキストから音声ナレーション付き動画の生成（FFmpeg 必須）

AIエージェントの「永続的な知識置き場・成果物置き場」として機能します。API キー不要で、ローカル LLM とも連携できます。

## 必要なもの

| 項目 | 内容 |
| --- | --- |
| OS | Windows 10/11 |
| ランタイム | .NET 8.0 以上 |
| Manidoc | インストール済みであること |
| FFmpeg | 動画生成機能を使う場合のみ |
| AIクライアント | Claude Desktop など MCP 対応クライアント |

## セットアップ

### 方法1: インストーラ（推奨）

[`dist/ManidocMCP_Setup_1.0.0.exe`](dist/) を実行します（既定で `C:\Program Files\ManidocMCP` にインストールされます）。

### 方法2: ソースからビルド

```powershell
git clone https://github.com/ichiroabe/manidocMCP_CS
cd manidocMCP_CS
dotnet build -c Release
```

### Claude Desktop との接続

`claude_desktop_config.json`（Claude Desktop の Settings → Developer → Edit Config）に追記してアプリを再起動します:

```json
{
  "mcpServers": {
    "manidoc": {
      "command": "C:\\Program Files\\ManidocMCP\\ManidocMCP.exe",
      "env": {
        "MANIDOC_WORKSPACE": "C:\\Users\\yourname\\Documents\\ManidocData"
      }
    }
  }
}
```

- `MANIDOC_WORKSPACE` には Manidoc のワークスペースフォルダを指定します。
- ソースからビルドした場合は `command` に `dotnet`、`args` に `ManidocMCP.dll` のフルパスを指定する形でも動作します。

### 動画生成を使う場合

`appsettings.json` の `Video:FfmpegPath` に `ffmpeg.exe` のフルパスを設定してください:

```json
{
  "Video": {
    "FfmpegPath": "C:\\Tools\\ffmpeg\\bin\\ffmpeg.exe",
    "OutDir": ""
  }
}
```

## 使い方

接続後、Claude にそのまま日本語で話しかけるだけです:

- 「Manidoc のプロジェクト一覧を見せて」 → `list_projects`
- 「◯◯について全文検索して」 → `search_fulltext`
- 「この会話の内容を記事『◯◯』として保存して」 → `save_article_by_title`
- 「この Markdown をプロジェクトとして取り込んで」 → `import_markdown_as_project`
- 「この記事から解説動画を作って」 → `generate_video_from_text`

## MCP ツール一覧

| ツール | 機能 |
| --- | --- |
| `get_server_status` | サーバー状態の確認（ワークスペース・プロジェクト数・動画設定） |
| `list_projects` | プロジェクト一覧 |
| `list_nodes` | プロジェクト内のノード一覧 |
| `get_article` / `save_article` | ID 指定で記事の取得・保存 |
| `get_article_by_title` / `save_article_by_title` | タイトル指定で記事の取得・保存 |
| `import_markdown_as_project` | Markdown をプロジェクトとして一括インポート |
| `search_fulltext` | 全文検索 |
| `generate_video_from_text` | テキストから音声ナレーション付き動画を生成 |
| `get_video_status` / `reset_video_status` | 動画生成の状態確認・リセット |

詳細な仕様・注意事項は [UserManual.md](UserManual.md)（日本語 / English）を参照してください。

## 関連リンク

- **Manidoc 本体（Windows アプリ）**: [GitHub](https://github.com/ichiroabe/manidoc) / [Microsoft Store](https://apps.microsoft.com/detail/9n578k2wqxqn)
- **macOS 版 MCP サーバー**: [manidocMCP_MAC](https://github.com/ichiroabe/manidocMCP_MAC)

## サポート

個人開発のため手厚いサポートは難しく、返信は主に週末・休日となります。
お問い合わせ: manidoc@fusion.upper.jp
