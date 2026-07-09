using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CandyBrowser.Shared.Abstractions;
using Models = CandyBrowser.Core.Models;

namespace CandyBrowser.Windows.ViewModels;

public partial class BookmarkViewModel : ObservableObject
{
    private readonly IBookmarkService _bookmarkService;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _newBookmarkUrl = string.Empty;

    [ObservableProperty]
    private string _newBookmarkTitle = string.Empty;

    [ObservableProperty]
    private BookmarkItemViewModel? _selectedBookmark;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editTitle = string.Empty;

    [ObservableProperty]
    private string _editUrl = string.Empty;

    public ObservableCollection<BookmarkItemViewModel> Bookmarks { get; } = new();
    public ObservableCollection<BookmarkItemViewModel> SearchResults { get; } = new();

    public BookmarkViewModel(IBookmarkService bookmarkService)
    {
        _bookmarkService = bookmarkService;
    }

    public async Task LoadBookmarksAsync()
    {
        Bookmarks.Clear();
        var bookmarks = await _bookmarkService.GetTreeAsync();
        foreach (var bookmark in bookmarks)
        {
            Bookmarks.Add(ConvertToViewModel(bookmark));
        }
    }

    private BookmarkItemViewModel ConvertToViewModel(Models.Bookmark bookmark)
    {
        var vm = new BookmarkItemViewModel(bookmark);
        foreach (var child in bookmark.Children)
        {
            vm.Children.Add(ConvertToViewModel(child));
        }
        return vm;
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        SearchResults.Clear();
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            await LoadBookmarksAsync();
            return;
        }

        var results = await _bookmarkService.SearchAsync(SearchQuery);
        foreach (var result in results)
        {
            SearchResults.Add(new BookmarkItemViewModel(result));
        }
    }

    [RelayCommand]
    private async Task AddBookmarkAsync()
    {
        if (string.IsNullOrWhiteSpace(NewBookmarkUrl)) return;

        var bookmark = new Models.Bookmark
        {
            Title = string.IsNullOrWhiteSpace(NewBookmarkTitle) ? NewBookmarkUrl : NewBookmarkTitle,
            Url = NewBookmarkUrl,
            ParentId = SelectedBookmark?.IsFolder == true ? SelectedBookmark.Id : null
        };

        await _bookmarkService.AddAsync(bookmark);
        NewBookmarkUrl = string.Empty;
        NewBookmarkTitle = string.Empty;
        await LoadBookmarksAsync();
    }

    [RelayCommand]
    private async Task AddBookmarkWithInfoAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        var bookmark = new Models.Bookmark
        {
            Title = url,
            Url = url
        };

        await _bookmarkService.AddAsync(bookmark);
        await LoadBookmarksAsync();
    }

    [RelayCommand]
    private async Task DeleteBookmarkAsync(BookmarkItemViewModel? bookmark)
    {
        if (bookmark == null) return;

        await _bookmarkService.DeleteAsync(bookmark.Id);
        await LoadBookmarksAsync();
    }

    [RelayCommand]
    private async Task UpdateBookmarkAsync()
    {
        if (SelectedBookmark == null || IsEditing == false) return;

        var bookmark = new Models.Bookmark
        {
            Id = SelectedBookmark.Id,
            ParentId = SelectedBookmark.ParentId,
            Title = EditTitle,
            Url = EditUrl,
            IsFolder = SelectedBookmark.IsFolder
        };

        await _bookmarkService.UpdateAsync(bookmark);
        IsEditing = false;
        await LoadBookmarksAsync();
    }

    [RelayCommand]
    private void StartEdit(BookmarkItemViewModel? bookmark)
    {
        if (bookmark == null) return;

        SelectedBookmark = bookmark;
        EditTitle = bookmark.Title;
        EditUrl = bookmark.Url;
        IsEditing = true;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        EditTitle = string.Empty;
        EditUrl = string.Empty;
    }

    [RelayCommand]
    private async Task CreateFolderAsync(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName)) return;

        var folder = new Models.Bookmark
        {
            Title = folderName,
            Url = string.Empty,
            IsFolder = true,
            ParentId = SelectedBookmark?.IsFolder == true ? SelectedBookmark.Id : null
        };

        await _bookmarkService.AddAsync(folder);
        await LoadBookmarksAsync();
    }

    public async Task MoveBookmarkAsync(long bookmarkId, long? newParentId)
    {
        var bookmark = await _bookmarkService.GetByIdAsync(bookmarkId);
        if (bookmark == null) return;

        bookmark.ParentId = newParentId;
        await _bookmarkService.UpdateAsync(bookmark);
        await LoadBookmarksAsync();
    }
}
