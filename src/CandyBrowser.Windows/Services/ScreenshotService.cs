using System.IO;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace CandyBrowser.Windows.Services;

public class ScreenshotService
{
    private WebView2? _webView;
    public ScreenshotService() { }
    public void SetWebView(WebView2 webView) { _webView = webView; }

    public async Task<string?> CaptureScreenshotAsync(string? outputPath = null)
    {
        try
        {
            var wv = _webView;
            if (wv?.CoreWebView2 == null) return null;
            if (string.IsNullOrEmpty(outputPath))
            {
                var picsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Pictures", "CandyBrowser Screenshots");
                Directory.CreateDirectory(picsPath);
                outputPath = Path.Combine(picsPath, $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            using var ms = new MemoryStream();
            await wv.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, ms);
            ms.Seek(0, SeekOrigin.Begin);
            await File.WriteAllBytesAsync(outputPath, ms.ToArray());
            return outputPath;
        }
        catch { return null; }
    }
}
