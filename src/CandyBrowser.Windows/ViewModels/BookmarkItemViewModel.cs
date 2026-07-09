using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CandyBrowser.Core.Models;

namespace CandyBrowser.Windows.ViewModels;

public partial class BookmarkItemViewModel : ObservableObject
{
    [ObservableProperty]
    private long _id;

    [ObservableProperty]
    private long? _parentId;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private string _faviconUrl = string.Empty;

    [ObservableProperty]
    private bool _isFolder;

    [ObservableProperty]
    private string _icon = "📄";

    public ObservableCollection<BookmarkItemViewModel> Children { get; } = new();

    public BookmarkItemViewModel() { }

    public BookmarkItemViewModel(Bookmark bookmark)
    {
        Id = bookmark.Id;
        ParentId = bookmark.ParentId;
        Title = bookmark.Title;
        Url = bookmark.Url;
        FaviconUrl = bookmark.FaviconUrl ?? string.Empty;
        IsFolder = bookmark.IsFolder;
        Icon = IsFolder ? "📁" : GetIconForUrl(bookmark.Url);
    }

    private static string GetIconForUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return "📄";

        var lowerUrl = url.ToLower();
        if (lowerUrl.Contains("youtube.com") || lowerUrl.Contains("youtu.be"))
            return "▶️";
        if (lowerUrl.Contains("github.com"))
            return "🐙";
        if (lowerUrl.Contains("twitter.com") || lowerUrl.Contains("x.com"))
            return "🐦";
        if (lowerUrl.Contains("facebook.com"))
            return "👤";
        if (lowerUrl.Contains("instagram.com"))
            return "📷";
        if (lowerUrl.Contains("reddit.com"))
            return "🤖";
        if (lowerUrl.Contains("wikipedia.org"))
            return "📚";
        if (lowerUrl.Contains("google.com"))
            return "🔍";
        if (lowerUrl.Contains("bing.com"))
            return "🔎";

        return "📄";
    }
}
