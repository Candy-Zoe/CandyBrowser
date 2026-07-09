using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CandyBrowser.Data.Entities;

[Table("history")]
public class HistoryEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [Column("url")]
    public string Url { get; set; } = string.Empty;

    [Required]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("favicon_url")]
    public string? FaviconUrl { get; set; }

    [Column("visit_count")]
    public int VisitCount { get; set; } = 1;

    [Column("last_visit")]
    public DateTime LastVisit { get; set; } = DateTime.UtcNow;

    [Column("first_visit")]
    public DateTime FirstVisit { get; set; } = DateTime.UtcNow;

    [Column("duration_ms")]
    public long? DurationMs { get; set; }

    [Column("sync_id")]
    public string? SyncId { get; set; }
}
