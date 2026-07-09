namespace CandyBrowser.Core.Models;

public enum DownloadStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Cancelled,
    Paused
}

public class DownloadItem
{
    public long Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long TotalBytes { get; set; }
    public long ReceivedBytes { get; set; }
    public DownloadStatus Status { get; set; } = DownloadStatus.Pending;
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsPaused { get; set; }

    public double ProgressPercentage => TotalBytes > 0 ? (double)ReceivedBytes / TotalBytes * 100 : 0;
    public string ProgressText => TotalBytes > 0 ? $"{FormatBytes(ReceivedBytes)} / {FormatBytes(TotalBytes)}" : "未知大小";
    public string SpeedText { get; set; } = string.Empty;

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}
