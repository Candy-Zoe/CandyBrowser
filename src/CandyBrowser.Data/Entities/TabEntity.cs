using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CandyBrowser.Data.Entities;

[Table("tabs")]
public class TabEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [Column("window_id")]
    public string WindowId { get; set; } = string.Empty;

    [Column("position")]
    public int Position { get; set; }

    [Required]
    [Column("url")]
    public string Url { get; set; } = string.Empty;

    [Column("title")]
    public string? Title { get; set; }

    [Column("favicon_url")]
    public string? FaviconUrl { get; set; }

    [Column("is_pinned")]
    public bool IsPinned { get; set; }

    [Column("is_incognito")]
    public bool IsIncognito { get; set; }

    [Column("parent_id")]
    public long? ParentId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
