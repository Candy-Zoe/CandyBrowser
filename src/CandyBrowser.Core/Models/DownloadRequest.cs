namespace CandyBrowser.Core.Models;

public class DownloadRequest
{
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public bool ShowSaveDialog { get; set; } = true;
}
