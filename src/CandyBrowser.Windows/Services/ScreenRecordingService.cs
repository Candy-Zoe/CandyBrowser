using System.IO;
using Microsoft.Web.WebView2.Core;

namespace CandyBrowser.Windows.Services;

public class ScreenRecordingService
{
    private readonly object _lock = new();
    private volatile bool _isRecording;
    private CancellationTokenSource? _cts;
    private readonly List<byte[]> _frames = new();
    private string? _lastRecordingPath;
    private CoreWebView2? _coreWebView2;

    public string? LastRecordingPath => _lastRecordingPath;
    public bool IsRecording => _isRecording;

    public ScreenRecordingService() { }
    public void SetCoreWebView2(CoreWebView2 cv) { _coreWebView2 = cv; }

    public async Task<bool> StartRecordingAsync(int fps = 15, int durationSeconds = 60)
    {
        lock (_lock) { if (_isRecording) return false; _isRecording = true; }
        var cv = _coreWebView2;
        if (cv == null) { lock (_lock) { _isRecording = false; } return false; }
        _cts = new CancellationTokenSource();
        _frames.Clear();
        try
        {
            var totalFrames = fps * durationSeconds;
            var intervalMs = 1000 / fps;
            for (int i = 0; i < totalFrames && !_cts.Token.IsCancellationRequested; i++)
            {
                try
                {
                    using var ms = new MemoryStream();
                    await cv.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    lock (_lock) _frames.Add(ms.ToArray());
                } catch { }
                await Task.Delay(intervalMs, _cts.Token);
            }
            _lastRecordingPath = await SaveToVideoAsync();
            return true;
        }
        catch (OperationCanceledException) { return false; }
        finally { lock (_lock) { _isRecording = false; } }
    }

    public void StopRecording() { _cts?.Cancel(); }

    private async Task<string?> SaveToVideoAsync()
    {
        var recDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Videos", "CandyBrowser Recordings");
        Directory.CreateDirectory(recDir);
        var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var frameDir = Path.Combine(recDir, $"rec_{ts}_frames");
        Directory.CreateDirectory(frameDir);
        for (int i = 0; i < _frames.Count; i++)
            await File.WriteAllBytesAsync(Path.Combine(frameDir, $"frame_{i:D4}.png"), _frames[i]);
        var ffmpegPath = FindFfmpeg();
        if (!string.IsNullOrEmpty(ffmpegPath))
        {
            var videoPath = Path.Combine(recDir, $"recording_{ts}.mp4");
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-framerate 15 -i \"{frameDir}\\frame_%04d.png\" -c:v libx264 -pix_fmt yuv420p -y \"{videoPath}\"",
                UseShellExecute = false, RedirectStandardError = true, CreateNoWindow = true
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            await proc!.WaitForExitAsync();
            if (proc.ExitCode == 0 && File.Exists(videoPath)) return videoPath;
        }
        return frameDir;
    }

    private string? FindFfmpeg()
    {
        foreach (var p in new[] { @"C:\Program Files\ffmpeg\bin\ffmpeg.exe", @"C:\Program Files (x86)\ffmpeg\bin\ffmpeg.exe" })
            if (File.Exists(p)) return p;
        return null;
    }
}
