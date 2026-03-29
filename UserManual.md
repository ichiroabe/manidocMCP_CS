# Manidoc MCP Server — ユーザーマニュアル / User Manual

---

## 目次 / Table of Contents

- [日本語](#日本語)
  - [概要](#概要)
  - [必要なもの](#必要なもの)
  - [インストール手順](#インストール手順)
  - [appsettings.json の設定](#appsettingsjson-の設定)
  - [機能一覧](#機能一覧)
  - [AIエージェントへのコマンド例](#aiエージェントへのコマンド例)
  - [注意事項](#注意事項)
- [English](#english)
  - [Overview](#overview)
  - [Requirements](#requirements)
  - [Installation](#installation)
  - [appsettings.json Configuration](#appsettingsjson-configuration)
  - [Tool Reference](#tool-reference)
  - [Example Commands for AI Agents](#example-commands-for-ai-agents)
  - [Notes](#notes)

---

## 日本語

### 概要

**Manidoc MCP Server** は、ドキュメント管理アプリ [Manidoc](https://github.com/ichiroabe/manidoc) のデータをAIエージェントから読み書きするための MCP（Model Context Protocol）サーバーです。

Claude Desktop などのMCP対応AIクライアントと連携することで、以下のことが自然言語で行えます。

- Manidoc のプロジェクト・記事の閲覧・編集
- Markdown テキストからのプロジェクト一括インポート
- 全文検索
- テキストから音声付き動画の生成（FFmpeg 必須）

> **テスト環境：** このMCPサーバーは **Claude Desktop（Claude Sonnet 4.5）** でテストされています。

---

### 必要なもの

| 項目 | 内容 |
| --- | --- |
| OS | Windows 10/11 |
| ランタイム | .NET 8.0 以上 |
| Manidoc | インストール済みであること |
| FFmpeg | 動画生成機能を使う場合のみ必要（別途インストール） |
| AIクライアント | Claude Desktop など MCP 対応のもの |

#### FFmpeg のインストール

動画生成機能（`generate_video_from_text`）を使うには **FFmpeg** が必要です。

1. 公式サイトからダウンロード: [https://ffmpeg.org/download.html](https://ffmpeg.org/download.html)
2. Windows 向けビルドを解凍し、`C:\Tools\ffmpeg\` などに配置
3. `ffmpeg.exe` のフルパスを `appsettings.json` の `Video:FfmpegPath` に設定

> FFmpeg は GPL ライセンスで配布されています。ライセンスの都合により本サーバーには同梱していません。

---

### インストール手順

#### 1. インストーラーの実行

`ManidocMCP_Setup_x.x.x.exe` を実行してウィザードに従ってください。

- デフォルトのインストール先: `C:\Program Files\ManidocMCP\`
- .NET 8 ランタイム未インストールの場合は警告が表示されます

#### 2. Claude Desktop の設定

Claude Desktop の設定ファイル（`claude_desktop_config.json`）にサーバーを登録します。

**設定ファイルの場所：**

```text
%APPDATA%\Claude\claude_desktop_config.json
```

**設定例：**

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

| 設定項目 | 説明 |
| --- | --- |
| `command` | `ManidocMCP.exe` のフルパス |
| `MANIDOC_WORKSPACE` | Manidoc のデータフォルダ（`.json` プロジェクトファイルが入っているフォルダ） |

> `MANIDOC_WORKSPACE` が未設定または存在しないパスの場合、ドキュメント操作ツール呼び出し時にエラーになります。`get_server_status` で事前に確認することをお勧めします。

#### 3. Claude Desktop を再起動

設定を保存後、Claude Desktop を再起動するとMCPサーバーが有効になります。

#### 4. 接続確認

AIエージェントに以下を指示して、サーバーが正常に動作しているか確認します。

```text
Manidocサーバーのステータスを確認して
```

正常であればワークスペースのパスとプロジェクト数が返ります。

---

### appsettings.json の設定

インストール先フォルダの `appsettings.json` で動作を調整できます。

```json
{
  "Video": {
    "FfmpegPath": "C:\\Tools\\ffmpeg\\bin\\ffmpeg.exe",
    "OutDir": "C:\\Users\\yourname\\Videos\\ManidocOut"
  }
}
```

| 設定項目 | 説明 | デフォルト |
| --- | --- | --- |
| `Video:FfmpegPath` | `ffmpeg.exe` のフルパス | `C:\Tools\ffmpeg\bin\ffmpeg.exe` |
| `Video:OutDir` | 生成動画の出力先フォルダ | `ManidocMCP.exe` と同じフォルダの `out\` |

`OutDir` を空欄にした場合、`ManidocMCP.exe` と同じフォルダ内に `out\` フォルダが自動作成されそこに保存されます。

---

### 機能一覧

#### サーバー確認

| ツール名 | 説明 |
| --- | --- |
| `get_server_status` | サーバーの動作確認。ワークスペースパス・プロジェクト数を返す。接続確認に使用 |

#### ドキュメント操作

| ツール名 | 説明 |
| --- | --- |
| `list_projects` | ワークスペース内の全プロジェクト一覧を返す |
| `list_nodes` | 指定プロジェクトのノード（見出し）一覧を返す |
| `get_article` | プロジェクトID・ノードID で記事（Markdown）を取得 |
| `save_article` | プロジェクトID・ノードID で記事（Markdown）を上書き保存 |
| `get_article_by_title` | プロジェクト名・ノードタイトルの部分一致で記事を取得 |
| `save_article_by_title` | プロジェクト名・ノードタイトルの部分一致で記事を保存 |
| `import_markdown_as_project` | Markdown テキストを新規プロジェクトとして一括インポート |
| `search_fulltext` | 全プロジェクトを対象にキーワード全文検索 |

#### 動画生成（FFmpeg 必須）

| ツール名 | 説明 |
| --- | --- |
| `generate_video_from_text` | テキストをWindows音声合成（SAPI）で読み上げ、字幕付き動画をバックグラウンドで生成。日本語・英語を自動判定 |
| `get_video_status` | 動画生成の進捗状況を確認（`running` / `done` / `failed`） |
| `reset_video_status` | 動画生成が異常終了した際にロックを解除してリセット |

#### 動画用スプライト（オプション）

`Assets\robot_sprite.png` を配置するとキャラクターアニメーションが動画に表示されます。

| 仕様項目 | 内容 |
| --- | --- |
| ファイル名 | `robot_sprite.png`（固定） |
| 配置場所 | `ManidocMCP.exe` と同じフォルダの `Assets\` フォルダ内 |
| フォーマット | PNG |
| 構成 | 3列 × 3行 = 9フレームのスプライトシート |
| 1フレームサイズ | 459 × 256 px |
| シート全体サイズ | 1377 × 768 px（= 459×3 × 256×3） |
| アニメーション速度 | 約1秒ごとにフレーム切替 |
| 省略した場合 | キャラクターなし・テキスト字幕のみの動画が生成される |

---

### AIエージェントへのコマンド例

#### 接続確認

```text
Manidocサーバーのステータスを確認して
```

#### プロジェクト・記事の閲覧

```text
Manidocのプロジェクト一覧を見せて
「仙台」プロジェクトのノード一覧を表示して
「仙台」プロジェクトの「歴史」というノードの記事を取得して
```

#### 記事の編集・作成

```text
「仙台」プロジェクトの「観光」ノードに以下の内容をMarkdownで保存して：
# 観光スポット
- 青葉城跡
- 瑞鳳殿
```

#### Markdownインポート

```text
以下のMarkdownを新しいプロジェクトとしてManidocにインポートして：
# 東北地方
## 宮城県
仙台市を県庁所在地とする...
```

#### 全文検索

```text
Manidocで「伊達政宗」というキーワードを全文検索して
「仙台」プロジェクトの中だけで「城」を検索して
```

#### 動画生成

```text
以下のテキストから動画を生成して：
「仙台は杜の都として知られる東北最大の都市です。」

動画生成の状況を確認して
```

---

### 注意事項

- **AIエージェントの能力への依存：** このMCPサーバーの動作結果の品質（記事の内容、Markdownの構造、動画のナレーション文など）は、接続するAIエージェントの能力に大きく左右されます。同じ指示でもAIのモデルやバージョンによって結果が異なる場合があります。
- **テスト環境：** Claude Desktop（Claude Sonnet 4.5）でテストしています。他のクライアントや他のモデルでは動作が異なる場合があります。
- **動画生成は非同期：** `generate_video_from_text` はバックグラウンドで動作します。完了確認には `get_video_status` を使ってください。
- **同時生成不可：** 動画生成は同時に1本のみ実行できます。
- **FFmpegは別途必要：** 動画生成機能は FFmpeg を同梱していません。ライセンス上の都合により、ユーザーが公式サイトから別途入手してください。
- **音声はWindowsのみ：** 音声合成（SAPI）はWindows専用です。

---

## English

### Overview

**Manidoc MCP Server** is an MCP (Model Context Protocol) server that allows AI agents to read and write data in [Manidoc](https://github.com/ichiroabe/manidoc), a document management application.

By integrating with MCP-compatible AI clients such as Claude Desktop, you can use natural language to:

- Browse and edit Manidoc projects and articles
- Bulk-import projects from Markdown text
- Perform full-text search
- Generate narrated videos from text (requires FFmpeg)

> **Tested with:** This MCP server has been tested with **Claude Desktop (Claude Sonnet 4.5)**.

---

### Requirements

| Item | Details |
| --- | --- |
| OS | Windows 10/11 |
| Runtime | .NET 8.0 or later |
| Manidoc | Must be installed |
| FFmpeg | Required only for video generation (install separately) |
| AI Client | Claude Desktop or any MCP-compatible client |

#### Installing FFmpeg

The video generation feature (`generate_video_from_text`) requires **FFmpeg**.

1. Download from the official site: [https://ffmpeg.org/download.html](https://ffmpeg.org/download.html)
2. Extract the Windows build and place it in a folder such as `C:\Tools\ffmpeg\`
3. Set the full path to `ffmpeg.exe` in `appsettings.json` under `Video:FfmpegPath`

> FFmpeg is distributed under the GPL license. It is not bundled with this server due to licensing requirements.

---

### Installation

#### 1. Run the Installer

Run `ManidocMCP_Setup_x.x.x.exe` and follow the wizard.

- Default install location: `C:\Program Files\ManidocMCP\`
- A warning will appear if .NET 8 Runtime is not installed

#### 2. Configure Claude Desktop

Register the server in the Claude Desktop configuration file (`claude_desktop_config.json`).

**Location:**

```text
%APPDATA%\Claude\claude_desktop_config.json
```

**Example:**

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

| Setting | Description |
| --- | --- |
| `command` | Full path to `ManidocMCP.exe` |
| `MANIDOC_WORKSPACE` | Path to the Manidoc data folder (the folder containing `.json` project files) |

> If `MANIDOC_WORKSPACE` is not set or the path does not exist, document operation tools will return an error. Use `get_server_status` to verify the configuration before use.

#### 3. Restart Claude Desktop

Save the configuration and restart Claude Desktop to activate the MCP server.

#### 4. Verify the Connection

Ask the AI agent to confirm the server is running:

```text
Check the Manidoc server status.
```

If successful, the workspace path and number of projects will be returned.

---

### appsettings.json Configuration

You can adjust behavior via `appsettings.json` in the install folder.

```json
{
  "Video": {
    "FfmpegPath": "C:\\Tools\\ffmpeg\\bin\\ffmpeg.exe",
    "OutDir": "C:\\Users\\yourname\\Videos\\ManidocOut"
  }
}
```

| Setting | Description | Default |
| --- | --- | --- |
| `Video:FfmpegPath` | Full path to `ffmpeg.exe` | `C:\Tools\ffmpeg\bin\ffmpeg.exe` |
| `Video:OutDir` | Output folder for generated videos | `out\` folder next to `ManidocMCP.exe` |

If `OutDir` is left empty, an `out\` subfolder will be created automatically next to `ManidocMCP.exe`.

---

### Tool Reference

#### Server Status

| Tool | Description |
| --- | --- |
| `get_server_status` | Verifies the server is running. Returns workspace path and project count. Use this to confirm the connection. |

#### Document Operations

| Tool | Description |
| --- | --- |
| `list_projects` | Returns a list of all projects in the workspace |
| `list_nodes` | Returns a list of nodes (headings) in a specified project |
| `get_article` | Retrieves an article (Markdown) by project ID and node ID |
| `save_article` | Overwrites an article by project ID and node ID |
| `get_article_by_title` | Retrieves an article by partial match on project name and node title |
| `save_article_by_title` | Saves an article by partial match on project name and node title |
| `import_markdown_as_project` | Imports Markdown text as a new project (H1 → project name, H2+ → nodes) |
| `search_fulltext` | Full-text keyword search across all projects |

#### Video Generation (requires FFmpeg)

| Tool | Description |
| --- | --- |
| `generate_video_from_text` | Generates a narrated video with subtitles using Windows SAPI TTS and FFmpeg. Language (Japanese/English) is auto-detected from the text. |
| `get_video_status` | Checks video generation progress (`running` / `done` / `failed`) |
| `reset_video_status` | Clears the video lock when a previous generation failed or got stuck |

#### Character Sprite for Video (Optional)

Placing `Assets\robot_sprite.png` next to `ManidocMCP.exe` enables a character animation overlay in generated videos.

| Spec | Details |
| --- | --- |
| File name | `robot_sprite.png` (fixed) |
| Location | `Assets\` folder next to `ManidocMCP.exe` |
| Format | PNG |
| Layout | Sprite sheet: 3 columns × 3 rows = 9 frames |
| Frame size | 459 × 256 px per frame |
| Sheet size | 1377 × 768 px total (459×3 × 256×3) |
| Animation speed | ~1 second per frame |
| If omitted | Video is generated with subtitles only (no character) |

---

### Example Commands for AI Agents

#### Verify connection

```text
Check the Manidoc server status.
```

#### Browsing projects and articles

```text
Show me the list of Manidoc projects.
List the nodes in the "Sendai" project.
Get the article for the "History" node in the "Sendai" project.
```

#### Editing and creating articles

```text
Save the following content as Markdown to the "Tourism" node in the "Sendai" project:
# Tourist Spots
- Aoba Castle Ruins
- Zuihoden Mausoleum
```

#### Importing Markdown

```text
Import the following Markdown as a new Manidoc project:
# Tohoku Region
## Miyagi Prefecture
Home to Sendai, the largest city in Tohoku...
```

#### Full-text search

```text
Search all Manidoc projects for "Date Masamune".
Search only within the "Sendai" project for "castle".
```

#### Video generation

```text
Generate a video from the following text:
"Sendai is the largest city in Tohoku, known as the City of Trees."

Check the status of the video generation.
```

---

### Notes

- **Dependency on AI agent capability:** The quality of results — including article content, Markdown structure, and video narration scripts — depends heavily on the capabilities of the connected AI agent. Results may vary depending on the AI model and version used.
- **Tested environment:** Tested with Claude Desktop (Claude Sonnet 4.5). Behavior may differ with other clients or models.
- **Video generation is asynchronous:** `generate_video_from_text` runs in the background. Use `get_video_status` to check for completion.
- **Only one video at a time:** Concurrent video generation is not supported.
- **FFmpeg must be installed separately:** FFmpeg is not bundled with this server due to licensing requirements. Please obtain it from the official website.
- **Voice synthesis is Windows-only:** The SAPI text-to-speech feature is exclusive to Windows.
