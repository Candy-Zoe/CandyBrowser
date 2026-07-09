using CandyBrowser.Core.Enums;
using CandyBrowser.Shared.Abstractions;

namespace CandyBrowser.Services.Reading;

public class ReadingModeService : IReadingModeService
{
    public Task<string> ExtractContentAsync(string url)
    {
        // Content extraction would be done by the platform-specific WebView
        // This service provides the HTML rendering for reading mode
        return Task.FromResult(string.Empty);
    }

    public string RenderAsCleanHtml(string title, string author, string htmlContent, ReadingTheme theme, int fontSize = 18)
    {
        var (bgColor, textColor) = theme switch
        {
            ReadingTheme.Dark => ("#1a1a2e", "#e0e0e0"),
            ReadingTheme.Sepia => ("#f4ecd8", "#5b4636"),
            _ => ("#ffffff", "#333333")
        };

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{EscapeHtml(title)}</title>
    <style>
        body {{
            font-family: 'Georgia', 'Times New Roman', serif;
            font-size: {fontSize}px;
            line-height: 1.8;
            max-width: 700px;
            margin: 0 auto;
            padding: 40px 20px;
            background-color: {bgColor};
            color: {textColor};
        }}
        h1 {{
            font-size: {fontSize + 12}px;
            margin-bottom: 8px;
            line-height: 1.3;
        }}
        .author {{
            font-size: {fontSize - 2}px;
            opacity: 0.7;
            margin-bottom: 30px;
        }}
        p {{
            margin-bottom: 1.2em;
            text-align: justify;
        }}
        img {{
            max-width: 100%;
            height: auto;
        }}
    </style>
</head>
<body>
    <h1>{EscapeHtml(title)}</h1>
    {(string.IsNullOrEmpty(author) ? "" : $"<div class=\"author\">By {EscapeHtml(author)}</div>")}
    {htmlContent}
</body>
</html>";
    }

    private static string EscapeHtml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }
}
