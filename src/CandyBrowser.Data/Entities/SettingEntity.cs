using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CandyBrowser.Data.Entities;

[Table("settings")]
public class SettingEntity
{
    [Key]
    [Column("key")]
    public string Key { get; set; } = string.Empty;

    [Required]
    [Column("value")]
    public string Value { get; set; } = string.Empty;

    [Column("value_type")]
    public string ValueType { get; set; } = "string";

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
