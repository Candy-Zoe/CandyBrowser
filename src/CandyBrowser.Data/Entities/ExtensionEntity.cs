using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CandyBrowser.Data.Entities;

[Table("extensions")]
public class ExtensionEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [Column("extension_id")]
    public string ExtensionId { get; set; } = string.Empty;

    [Required]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("version")]
    public string Version { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [Column("manifest_json")]
    public string ManifestJson { get; set; } = string.Empty;

    [Required]
    [Column("install_path")]
    public string InstallPath { get; set; } = string.Empty;

    [Column("is_enabled")]
    public bool IsEnabled { get; set; } = true;

    [Column("permissions")]
    public string? Permissions { get; set; }

    [Column("icon_path")]
    public string? IconPath { get; set; }

    [Column("installed_at")]
    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
