namespace ManidocMCP;

/// <summary>
/// 保存前の Markdown 正規化。
/// Manidoc 本体の HTML 出力(Markdig)はパイプテーブルを段落単位で判定するため、
/// 表ブロックの前後に空行がないと表として描画されない（パイプが平文表示される）。
/// エディタ(Tiptap)は空行なしでも表として表示するため乖離が生じる。
/// MCP 経由で書き込む Markdown をここで補正し、乖離の発生源を断つ。
/// (Manidoc 本体 Core/MarkdownNormalizer.cs と同一ロジック)
/// </summary>
public static class MarkdownNormalizer
{
    /// <summary>パイプテーブルの前後に空行を補う。フェンスコードブロック内は変更しない。</summary>
    public static string NormalizeTables(string? markdown)
    {
        if (string.IsNullOrEmpty(markdown) || !markdown.Contains('|')) return markdown ?? "";

        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var result = new List<string>(lines.Length + 8);
        bool inFence = false;
        string fenceMarker = "```";

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string trimmed = line.TrimStart();

            // フェンスコードブロックはそのまま通す
            if (!inFence && (trimmed.StartsWith("```") || trimmed.StartsWith("~~~")))
            {
                inFence = true;
                fenceMarker = trimmed[..3];
                result.Add(line);
                continue;
            }
            if (inFence)
            {
                if (trimmed.StartsWith(fenceMarker)) inFence = false;
                result.Add(line);
                continue;
            }

            // 表ヘッダ（次行が区切り行）の直前に空行を補う
            bool nextIsHeader = i + 2 < lines.Length && IsTableRow(lines[i + 1]) && IsDelimiterRow(lines[i + 2]);
            if (!IsTableRow(line) && line.Trim().Length > 0 && nextIsHeader)
            {
                result.Add(line);
                result.Add(string.Empty);
                continue;
            }

            result.Add(line);

            // 表の最終行の直後に空行を補う（直前行も表行のときのみ＝孤立したパイプ行は対象外）
            if (IsTableRow(line)
                && i + 1 < lines.Length
                && !IsTableRow(lines[i + 1])
                && lines[i + 1].Trim().Length > 0
                && result.Count >= 2
                && IsTableRow(result[^2]))
            {
                result.Add(string.Empty);
            }
        }

        return string.Join("\n", result);
    }

    /// <summary>ノードツリー全体の article / comment を正規化する（インポート用）。</summary>
    public static void NormalizeTree(IEnumerable<ManidocNode>? nodes)
    {
        if (nodes == null) return;
        foreach (var n in nodes)
        {
            if (!string.IsNullOrEmpty(n.Article)) n.Article = NormalizeTables(n.Article);
            if (!string.IsNullOrEmpty(n.Comment)) n.Comment = NormalizeTables(n.Comment);
            NormalizeTree(n.Children);
        }
    }

    private static bool IsTableRow(string line)
    {
        string t = line.Trim();
        return t.Length >= 2 && t.StartsWith('|') && t.EndsWith('|');
    }

    private static bool IsDelimiterRow(string line)
    {
        string t = line.Trim();
        if (!t.StartsWith('|')) return false;
        bool hasDash = false;
        foreach (char c in t)
        {
            if (c == '-') { hasDash = true; continue; }
            if (c != '|' && c != ':' && c != ' ') return false;
        }
        return hasDash;
    }
}
