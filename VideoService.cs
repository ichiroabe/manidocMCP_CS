using System.Diagnostics;
using System.Speech.Synthesis;
using System.Text.Json;

namespace ManidocMCP;

public static class VideoService
{
    private static string FfmpegPath = @"C:\Project\ffmpeg-8.1\bin\ffmpeg.exe";
    private static readonly string LockFile = Path.Combine(AppContext.BaseDirectory, ".video.lock");
    private static readonly string StatusFile = Path.Combine(AppContext.BaseDirectory, ".video.status.json");
    private static string OutDir = Path.Combine(AppContext.BaseDirectory, "out");

    public static void Configure(string? ffmpegPath, string? outDir)
    {
        if (!string.IsNullOrWhiteSpace(ffmpegPath)) FfmpegPath = ffmpegPath;
        if (!string.IsNullOrWhiteSpace(outDir)) OutDir = outDir;
    }

    public static bool AcquireLock()
    {
        if (File.Exists(LockFile)) return false;
        File.WriteAllText(LockFile, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        return true;
    }

    public static void ReleaseLock()
    {
        if (File.Exists(LockFile)) File.Delete(LockFile);
    }

    public static void WriteStatus(string state, string? output = null, string? error = null)
    {
        var status = new
        {
            state,
            startedAt = File.Exists(LockFile)
                ? DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(File.ReadAllText(LockFile))).ToString("o")
                : null,
            updatedAt = DateTimeOffset.UtcNow.ToString("o"),
            output,
            error,
        };
        File.WriteAllText(StatusFile, JsonSerializer.Serialize(status, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static string GetStatus()
    {
        bool isRunning = File.Exists(LockFile);

        if (!File.Exists(StatusFile))
            return isRunning ? "Status: running (no status file yet)" : "Status: idle (no video generated yet)";

        var json = File.ReadAllText(StatusFile);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var state = root.GetProperty("state").GetString();
        var updatedAt = root.TryGetProperty("updatedAt", out var u) ? u.GetString() : null;
        var output = root.TryGetProperty("output", out var o) && o.ValueKind != JsonValueKind.Null ? o.GetString() : null;
        var error = root.TryGetProperty("error", out var e) && e.ValueKind != JsonValueKind.Null ? e.GetString() : null;

        // ロックファイルがあるのに state が done/failed → 異常終了（プロセスが死んだ）
        if (isRunning && state != "running")
            return $"Status: stale lock detected (last state: {state}). Use reset_video_status to clear.";

        // ロックがないのに state が running → 異常終了
        if (!isRunning && state == "running")
            return $"Status: FAILED (process died unexpectedly)\nLast updated: {updatedAt}\nError: (no error captured — process was killed or crashed)";

        var startedAt = root.TryGetProperty("startedAt", out var sa) ? sa.GetString() : "?";
        return state switch
        {
            "running" => $"Status: running\nStarted: {startedAt}\nUpdated: {updatedAt}",
            "done" => $"Status: done\nOutput: {output}\nCompleted: {updatedAt}",
            "failed" => $"Status: FAILED\nError: {error}\nAt: {updatedAt}",
            _ => $"Status: unknown ({state})",
        };
    }

    public static void ResetStatus()
    {
        ReleaseLock();
        if (File.Exists(StatusFile)) File.Delete(StatusFile);
    }

    private const string FontPath = @"C\:/Windows/Fonts/meiryo.ttc";
    private const int RobotDisplayW = 960;    // 出力上のロボット幅(px)
    private const int TextPadding = 60;       // ロボット幅内側の左右余白

    // 言語別フォントサイズ・文字数設定
    private static int GetFontSize(string lang) => lang == "ja" ? 56 : 40;
    private static int GetCharsPerLine(string lang) =>
        lang == "ja"
            ? (RobotDisplayW - TextPadding * 2) / 56          // 日本語: 全角 = 15文字
            : (int)((RobotDisplayW - TextPadding * 2) / (40 * 0.55)); // 英語: 半角平均幅で近似 ≈ 38文字

    // スプライトシート設定（3×3 = 9フレーム）
    private static readonly string SpritePath = Path.Combine(AppContext.BaseDirectory, "Assets", "robot_sprite.png");
    private const int SpriteFrameW = 459;
    private const int SpriteFrameH = 256;
    private const int SpriteCols = 3;
    private const int SpriteTotalFrames = 9;
    private const double SpriteAnimFps = 1.0; // 1フレーム約1秒

    // charsPerLine 文字ごとに強制折り返し（句読点で早めに切る）
    private static string WrapText(string text, int charsPerLine)
    {
        var lines = new List<string>();
        var current = new System.Text.StringBuilder();
        foreach (char c in text)
        {
            if (c == '\n')
            {
                lines.Add(current.ToString());
                current.Clear();
                continue;
            }
            current.Append(c);
            bool atPunctuation = c == '。' || c == '、' || c == '.' || c == ',' || c == ' ';
            if (current.Length >= charsPerLine && atPunctuation || current.Length >= charsPerLine + 2)
            {
                lines.Add(current.ToString());
                current.Clear();
            }
        }
        if (current.Length > 0) lines.Add(current.ToString());
        return string.Join("\n", lines);
    }

    // FFmpeg drawtext の text= オプション用エスケープ
    private static string EscapeDrawtextText(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("%", "%%")           // FFmpeg の書式指定子を無効化
            .Replace("'", "\\'")
            .Replace(":", "\\:")
            .Replace(",", "\\,")
            .Replace("[", "\\[")
            .Replace("]", "\\]")
            .Replace("\r", "")            // キャリッジリターン除去
            .Replace("\n", " ")           // 万一残っていた改行はスペースに
            .Replace("\v", " ")           // 旧コードの名残り除去
            .Replace("\f", " ");          // Form Feed 除去
    }

    // 入力テキストを正規化（リテラルの\nを実改行に変換）
    private static string NormalizeText(string text)
    {
        return text.Replace("\\n", "\n").Replace("\\r", "").Replace("\r", "");
    }

    // ひらがな・カタカナ・漢字が1文字以上あれば日本語、なければ英語
    private static string DetectLanguage(string text)
    {
        foreach (char c in text)
        {
            if ((c >= '\u3040' && c <= '\u309F') ||  // ひらがな
                (c >= '\u30A0' && c <= '\u30FF') ||  // カタカナ
                (c >= '\u4E00' && c <= '\u9FFF'))     // 漢字
                return "ja";
        }
        return "en";
    }

    private static string BuildOutputPath()
    {
        Directory.CreateDirectory(OutDir);
        var ts = DateTime.Now.ToString("yyyyMMddHHmm");
        return Path.Combine(OutDir, $"{ts}.mp4");
    }

    public static async Task<string> GenerateAsync(string text, string? language = null)
    {
        var wavFile = Path.Combine(Path.GetTempPath(), $"manidoc_{Guid.NewGuid():N}.wav");
        var debugLog = Path.Combine(AppContext.BaseDirectory, "ffmpeg_debug.log");
        var outputPath = BuildOutputPath();

        try
        {
            WriteStatus("running");

            // 入力テキスト正規化（リテラル\nを実改行に）
            text = NormalizeText(text);

            // --- SAPI で WAV 生成 ---
            var lang = language ?? DetectLanguage(text);
            using (var synth = new SpeechSynthesizer())
            {
                var voice = synth.GetInstalledVoices()
                    .FirstOrDefault(v => v.VoiceInfo.Culture.Name.StartsWith(lang, StringComparison.OrdinalIgnoreCase));
                if (voice != null)
                    synth.SelectVoice(voice.VoiceInfo.Name);

                synth.SetOutputToWaveFile(wavFile);
                synth.Speak(text);
                synth.SetOutputToNull();
            }

            WriteStatus("running");  // SAPI完了、FFmpeg開始

            // 行ごとに drawtext フィルターを生成（textfile の改行文字化け回避）
            int fontSize = GetFontSize(lang);
            int charsPerLine = GetCharsPerLine(lang);
            var lines = WrapText(text, charsPerLine)
                .Replace("\r\n", "\n").Replace("\r", "\n")
                .Split('\n');
            int lineH = fontSize + 16; // 行高さ = フォントサイズ + line_spacing
            var drawtext = string.Join(",", lines.Select((line, i) =>
                $"drawtext=fontfile='{FontPath}'" +
                $":text={EscapeDrawtextText(line)}" +
                $":fontcolor=white:fontsize={fontSize}" +
                $":x=60:y=h-80-t*40+{i * lineH}" +
                $":shadowcolor=black:shadowx=3:shadowy=3"));


            string args;
            if (File.Exists(SpritePath))
            {
                // スプライト全体をスケール後にcropしてアニメーション
                // スケール後の1フレームサイズ
                int scaledFrameW = RobotDisplayW;
                int scaledFrameH = (int)(SpriteFrameH * (double)RobotDisplayW / SpriteFrameW); // ≈535
                int scaledSheetW = scaledFrameW * SpriteCols;  // 2880
                int scaledSheetH = scaledFrameH * (SpriteTotalFrames / SpriteCols); // ≈1605

                var cropX = $"mod(trunc(t*{SpriteAnimFps})\\,{SpriteCols})*{scaledFrameW}";
                var cropY = $"floor(mod(trunc(t*{SpriteAnimFps})\\,{SpriteTotalFrames})/{SpriteCols})*{scaledFrameH}";
                var filterComplex =
                    $"[1:v]scale={scaledSheetW}:{scaledSheetH}[sheet];" +
                    $"[sheet]crop={scaledFrameW}:{scaledFrameH}:{cropX}:{cropY}[robot];" +
                    $"[0:v][robot]overlay=(W-w)/2:80[with_robot];" +
                    $"[with_robot]{drawtext}[out]";

                args = $"-y -f lavfi -i color=c=black:s=1080x1920:r=30 " +
                       $"-loop 1 -r 30 -i \"{SpritePath}\" " +
                       $"-i \"{wavFile}\" " +
                       $"-filter_complex \"{filterComplex}\" " +
                       $"-map \"[out]\" -map 2:a " +
                       $"-shortest -c:v libx264 -c:a aac -pix_fmt yuv420p \"{outputPath}\"";
            }
            else
            {
                // スプライトなし：テキストのみ
                args = $"-y -f lavfi -i color=c=black:s=1080x1920:r=30 -i \"{wavFile}\" " +
                       $"-vf \"{drawtext}\" " +
                       $"-shortest -c:v libx264 -c:a aac -pix_fmt yuv420p \"{outputPath}\"";
            }

            // デバッグ: FFmpegに渡す引数をログに書き出す
            File.WriteAllText(debugLog,
                $"=== {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n" +
                $"LINES ({lines.Length}):\n" +
                string.Join("\n", lines.Select((l, i) => $"  [{i}] ({l.Length}chars) {l}")) +
                $"\n\nARGS:\n{args}\n",
                System.Text.Encoding.UTF8);

            var psi = new ProcessStartInfo(FfmpegPath, args)
            {
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var proc = Process.Start(psi)!;
            var stderr = await proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();

            if (proc.ExitCode != 0)
            {
                // 失敗時はデバッグログに stderr を追記して残す
                File.AppendAllText(debugLog, $"\nSTDERR:\n{stderr}\n");
                var msg = $"FFmpeg failed (exit {proc.ExitCode}): {stderr[..Math.Min(stderr.Length, 500)]}";
                WriteStatus("failed", error: msg);
                throw new InvalidOperationException(msg);
            }

            WriteStatus("done", output: outputPath);
            return outputPath;
        }
        catch (Exception ex) when (ex is not InvalidOperationException ioe || !ioe.Message.StartsWith("FFmpeg"))
        {
            WriteStatus("failed", error: ex.Message);
            throw;
        }
        finally
        {
            if (File.Exists(wavFile)) File.Delete(wavFile);
            // 成功時のみデバッグログを削除（失敗時は調査用に残す）
            var status = File.Exists(StatusFile)
                ? File.ReadAllText(StatusFile)
                : "";
            if (status.Contains("\"done\"") && File.Exists(debugLog))
                File.Delete(debugLog);
        }
    }
}
