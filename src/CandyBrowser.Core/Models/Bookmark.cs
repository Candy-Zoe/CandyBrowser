namespace CandyBrowser.Core.Models;

public class Bookmark
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? FaviconUrl { get; set; }
    public int Position { get; set; }
    public bool IsFolder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? SyncId { get; set; }

    public Bookmark? Parent { get; set; }
    public List<Bookmark> Children { get; set; } = new();
}
