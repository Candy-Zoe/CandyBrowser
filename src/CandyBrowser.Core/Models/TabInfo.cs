namespace CandyBrowser.Core.Models;

public class TabInfo
{
    public long Id { get; set; }
    public string WindowId { get; set; } = string.Empty;
    public int Position { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? FaviconUrl { get; set; }
    public bool IsPinned { get; set; }
    public bool IsIncognito { get; set; }
    public long? ParentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
