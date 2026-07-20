namespace CandyBrowser.Shared.Abstractions;

/// <summary>
/// Service for capturing screenshots of WebView2 content.
/// </summary>
public interface IScreenshotService
{
    Task<string?> CaptureScreenshotAsync(string? outputPath = null);
}
