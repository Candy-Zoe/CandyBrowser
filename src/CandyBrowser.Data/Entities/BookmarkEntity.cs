using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CandyBrowser.Data.Entities;

[Table("bookmarks")]
public class BookmarkEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Column("parent_id")]
    public long? ParentId { get; set; }

    [Required]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Column("url")]
    public string Url { get; set; } = string.Empty;

    [Column("favicon_url")]
    public string? FaviconUrl { get; set; }

    [Column("position")]
    public int Position { get; set; }

    [Column("is_folder")]
    public bool IsFolder { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("sync_id")]
    public string? SyncId { get; set; }

    [ForeignKey(nameof(ParentId))]
    public BookmarkEntity? Parent { get; set; }

    public List<BookmarkEntity> Children { get; set; } = new();
}
