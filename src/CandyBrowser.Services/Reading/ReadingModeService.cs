using System.Text;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using CandyBrowser.Core.Enums;
using CandyBrowser.Shared.Abstractions;

namespace CandyBrowser.Services.Reading;

public class ReadingModeService : IReadingModeService
{
    /// <summary>
    /// Extracts readable content from raw HTML using AngleSharp DOM parsing.
    /// Implements a simplified readability algorithm focusing on article content.
    /// </summary>
    public async Task<string> ExtractContentAsync(string url)
    {
        return await ExtractContentAsync(string.Empty, Path.GetFileName(url) ?? "");
    }

    public async Task<string> ExtractContentAsync(string htmlContent, string title = "")
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
            return string.Empty;

        try
        {
            var parser = new HtmlParser();
            var doc = parser.ParseDocument(htmlContent);

            // Try to find article/main content elements first
            IElement? contentElement = doc.QuerySelector("article")
                ?? doc.QuerySelector("main")
                ?? doc.QuerySelector("[role='main']")
                ?? doc.QuerySelector(".article-content")
                ?? doc.QuerySelector(".post-content")
                ?? doc.QuerySelector(".entry-content")
                ?? doc.QuerySelector("#content")
                ?? doc.QuerySelector(".content")
                ?? doc.Body;

            if (contentElement == null || contentElement.TagName == "BODY")
            {
                // Fallback: scan for best content element
                contentElement = FindBestContentElement(doc);
            }

            if (contentElement == null)
                return string.Empty;

            // Clean up the content
            RemoveElements(contentElement, "script, style, noscript, nav, footer, header, iframe, ad, .ads, .advertisement, .sidebar, .nav, .menu");
            RemoveAttributes(contentElement, "style, onload, onerror, onclick");

            // Extract text and clean HTML
            var innerHtml = contentElement.InnerHtml;
            
            // Sanitize: remove excessive whitespace
            innerHtml = SanitizeHtml(innerHtml);

            return innerHtml;
        }
        catch
        {
            // If parsing fails, return raw body text as fallback
            try
            {
                var parser = new HtmlParser();
                var doc = parser.ParseDocument(htmlContent);
                var text = doc.Body?.TextContent ?? htmlContent;
                return $"<p>{text.Replace("\n", "</p><p>")}</p>";
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    public string RenderAsCleanHtml(string title, string author, string htmlContent, ReadingTheme theme, int fontSize = 18)
    {
        var (bgColor, textColor, linkColor) = theme switch
        {
            ReadingTheme.Dark => ("#1a1a2e", "#e0e0e0", "#7eb8ff"),
            ReadingTheme.Sepia => ("#f4ecd8", "#5b4636", "#8b6914"),
            _ => ("#ffffff", "#333333", "#0066cc")
        };

        var safeTitle = EscapeHtml(title);
        var safeAuthor = string.IsNullOrEmpty(author) ? "" : $"<div class=\"author\">By {EscapeHtml(author)}</div>";
        var safeContent = EscapeHtmlInline(htmlContent);

        return $@"<!DOCTYPE html>
<html lang=""zh-CN"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{safeTitle}</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{
            font-family: 'Georgia', 'Noto Serif SC', 'SimSun', serif;
            font-size: {fontSize}px;
            line-height: 1.8;
            max-width: 720px;
            margin: 0 auto;
            padding: 40px 24px;
            background-color: {bgColor};
            color: {textColor};
        }}
        h1 {{
            font-size: {fontSize + 14}px;
            margin-bottom: 8px;
            line-height: 1.3;
            font-weight: bold;
        }}
        .author {{
            font-size: {fontSize - 2}px;
            opacity: 0.7;
            margin-bottom: 32px;
            font-style: italic;
        }}
        p {{
            margin-bottom: 1.2em;
            text-align: justify;
        }}
        img {{
            max-width: 100%;
            height: auto;
            display: block;
            margin: 1em auto;
        }}
        a {{ color: {linkColor}; text-decoration: underline; }}
        blockquote {{
            border-left: 4px solid {linkColor};
            padding-left: 16px;
            margin: 1em 0;
            opacity: 0.85;
        }}
        pre, code {{
            font-family: 'Consolas', 'Courier New', monospace;
            font-size: {fontSize - 2}px;
            background: {bgColor};
            border: 1px solid {textColor};
            opacity: 0.6;
        }}
        pre {{ padding: 12px; overflow-x: auto; }}
    </style>
</head>
<body>
    <h1>{safeTitle}</h1>
    {safeAuthor}
    {safeContent}
</body>
</html>";
    }

    private static IElement? FindBestContentElement(IHtmlDocument doc)
    {
        if (doc.Body == null) return null;

        var candidates = doc.Body.QuerySelectorAll("div, section, article");
        IElement? best = null;
        int bestScore = -1;

        foreach (var el in candidates)
        {
            var text = el.TextContent ?? "";
            var wordCount = text.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            
            if (wordCount < 50) continue;

            // Penalize elements with nav/sidebar/ad class names
            var className = (el as IElement)?.ClassName?.ToLower() ?? "";
            if (className.Contains("nav") || className.Contains("sidebar") || 
                className.Contains("ad") || className.Contains("menu") || className.Contains("footer"))
                continue;

            // Score: word count * content density
            var html = el.InnerHtml ?? "";
            var tagRatio = html.Length > 0 ? text.Length / (double)html.Length : 0;
            var score = wordCount * (1 + tagRatio);

            if (score > bestScore)
            {
                bestScore = (int)score;
                best = el;
            }
        }

        return best;
    }

    private static void RemoveElements(INode node, string selectors)
    {
        if (node is IElement element)
        {
            foreach (var sel in selectors.Split(','))
            {
                try
                {
                    foreach (var match in element.QuerySelectorAll(sel.Trim()))
                    {
                        match.Remove();
                    }
                }
                catch { /* ignore selector errors */ }
            }

            foreach (var child in element.ChildNodes)
                RemoveElements(child, selectors);
        }
        else if (node is INodeList list)
        {
            foreach (var child in list)
                RemoveElements(child, selectors);
        }
    }

    private static void RemoveAttributes(INode node, string attrNames)
    {
        if (node is IElement element)
        {
            foreach (var attr in attrNames.Split(','))
            {
                try
                {
                    element.RemoveAttribute(attr.Trim());
                }
                catch { }
            }

            foreach (var child in element.ChildNodes)
                RemoveAttributes(child, attrNames);
        }
    }

    private static string SanitizeHtml(string html)
    {
        // Remove consecutive blank lines and excessive whitespace
        html = System.Text.RegularExpressions.Regex.Replace(html, @"\s+", " ");
        html = System.Text.RegularExpressions.Regex.Replace(html, @">\s+<", "><");
        return html;
    }

    private static string EscapeHtml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#x27;");
    }

    private static string EscapeHtmlInline(string html)
    {
        // For already-HTML content, only escape ampersands to avoid double-escaping
        return html.Replace("&", "&amp;");
    }
}
