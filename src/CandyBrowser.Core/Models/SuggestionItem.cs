using CandyBrowser.Core.Enums;

namespace CandyBrowser.Core.Models;

public class SuggestionItem
{
    public SuggestionType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
