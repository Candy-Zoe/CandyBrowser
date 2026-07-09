namespace CandyBrowser.Core.Models;

public class Setting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string ValueType { get; set; } = "string";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
