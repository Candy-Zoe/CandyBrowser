namespace CandyBrowser.Core.Models;

public class HistoryEntry
{
    public long Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? FaviconUrl { get; set; }
    public int VisitCount { get; set; } = 1;
    public DateTime LastVisit { get; set; } = DateTime.UtcNow;
    public DateTime FirstVisit { get; set; } = DateTime.UtcNow;
    public long? DurationMs { get; set; }
    public string? SyncId { get; set; }
}
