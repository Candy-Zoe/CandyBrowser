using System.Text;
using HtmlAgilityPack;
using CandyBrowser.Core.Models;
using CandyBrowser.Shared.Abstractions;

namespace CandyBrowser.Services.Bookmarks.ImportExport;

/// <summary>
/// Bookmark import/export service supporting Netscape HTML format (compatible with Chrome/Edge/Firefox).
/// </summary>
public class BookmarkImportExportService : IBookmarkImportExportService
{
    private readonly IBookmarkService _bookmarkService;

    public BookmarkImportExportService(IBookmarkService bookmarkService)
    {
        _bookmarkService = bookmarkService;
    }

    /// <summary>
    /// Export bookmarks to Netscape HTML format.
    /// </summary>
    public async Task<string> ExportToHtmlAsync()
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE NETSCAPE-Bookmark-file-1>");
        sb.AppendLine("<META HTTP-EQUIV=\"Content-Type\" CONTENT=\"text/html; charset=UTF-8\">");
        sb.AppendLine("<TITLE>Bookmarks</TITLE>");
        sb.AppendLine("<H1>Bookmarks</H1>");
        sb.AppendLine("<DL><p>");

        var rootBookmarks = await _bookmarkService.GetChildrenAsync(null);
        await WriteBookmarksRecursive(sb, rootBookmarks, 0);

        sb.AppendLine("</DL><p>");
        return sb.ToString();
    }

    private async Task WriteBookmarksRecursive(StringBuilder sb, IReadOnlyList<Bookmark> bookmarks, int indent)
    {
        foreach (var bm in bookmarks.OrderBy(b => b.Position))
        {
            var indentStr = new string(' ', indent * 4);
            if (bm.IsFolder)
            {
                sb.AppendLine($"{indentStr}<DT><H3 ADD_DATE=\"{(long)(bm.CreatedAt - new DateTime(1970, 1, 1)).TotalSeconds}\" LAST_MODIFIED=\"{(long)(bm.UpdatedAt - new DateTime(1970, 1, 1)).TotalSeconds}\">{EscapeHtml(bm.Title)}</H3>");
                sb.AppendLine($"{indentStr}<DL><p>");
                var children = await _bookmarkService.GetChildrenAsync(bm.Id);
                await WriteBookmarksRecursive(sb, children, indent + 1);
                sb.AppendLine($"{indentStr}</DL><p>");
            }
            else
            {
                sb.AppendLine($"{indentStr}<DT><A HREF=\"{EscapeHtml(bm.Url)}\" ADD_DATE=\"{(long)(bm.CreatedAt - new DateTime(1970, 1, 1)).TotalSeconds}\" ICON=\"{EscapeHtml(bm.FaviconUrl ?? "")}\">{EscapeHtml(bm.Title)}</A>");
            }
        }
    }

    /// <summary>
    /// Import bookmarks from Netscape HTML format.
    /// </summary>
    public async Task<int> ImportFromHtmlAsync(string htmlContent, long? parentId = null)
    {
        var doc = new HtmlAgilityPack.HtmlDocument();
        doc.LoadHtml(htmlContent);

        var importedCount = 0;
        var rootNodes = doc.DocumentNode.SelectNodes("//DL//DT");
        if (rootNodes != null)
        {
            foreach (var dt in rootNodes)
            {
                importedCount += await ProcessImportNode(dt, parentId, importedCount);
            }
        }

        return importedCount;
    }

    private async Task<int> ProcessImportNode(HtmlNode dt, long? parentId, int position)
    {
        var count = 0;
        var h3 = dt.SelectSingleNode(".//H3");
        if (h3 != null)
        {
            // Folder
            var folder = new Bookmark
            {
                Title = h3.InnerText.Trim(),
                Url = "",
                ParentId = parentId,
                IsFolder = true,
                Position = position
            };
            var added = await _bookmarkService.AddAsync(folder);
            count++;

            // Look for child DL
            var dl = dt.SelectSingleNode(".//DL");
            if (dl != null)
            {
                var childDts = dl.SelectNodes("DT");
                if (childDts != null)
                {
                    int childPos = 0;
                    foreach (var childDt in childDts)
                    {
                        count += await ProcessImportNode(childDt, added.Id, childPos++);
                    }
                }
            }
        }
        else
        {
            var anchor = dt.SelectSingleNode(".//A");
            if (anchor != null)
            {
                var bm = new Bookmark
                {
                    Title = anchor.GetAttributeValue("TEXT", anchor.InnerText).Trim(),
                    Url = anchor.GetAttributeValue("HREF", "").Trim(),
                    ParentId = parentId,
                    IsFolder = false,
                    Position = position
                };
                if (!string.IsNullOrEmpty(bm.Url))
                {
                    await _bookmarkService.AddAsync(bm);
                    count++;
                }
            }
        }
        return count;
    }

    private static string EscapeHtml(string s)
    {
        return s.Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
    }
}
