namespace CandyBrowser.Core.Models;

public class PasswordEntry
{
    public long Id { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? SyncId { get; set; }
}
