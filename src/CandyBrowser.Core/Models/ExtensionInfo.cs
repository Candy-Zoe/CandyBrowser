namespace CandyBrowser.Core.Models;

public class ExtensionInfo
{
    public long Id { get; set; }
    public string ExtensionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ManifestJson { get; set; } = string.Empty;
    public string InstallPath { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public string? Permissions { get; set; }
    public string? IconPath { get; set; }
    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
