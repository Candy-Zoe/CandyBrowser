using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CandyBrowser.Windows.ViewModels;
using CandyBrowser.Core.Models;
using CandyBrowser.Shared.Abstractions;

namespace CandyBrowser.Windows;

public partial class MainWindow : Window
{
    private readonly IBookmarkService _bookmarkService;
    private readonly IDownloadService _downloadService;

    public MainWindow(MainViewModel viewModel, IBookmarkService bookmarkService, IDownloadService downloadService)
    {
        _bookmarkService = bookmarkService;
        _downloadService = downloadService;
        DataContext = viewModel;
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            await vm.InitializeAsync();
            vm.PropertyChanged += Vm_PropertyChanged;
        }

        await LoadBookmarksBarAsync();
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedTab))
        {
            if (DataContext is MainViewModel vm && vm.SelectedTab != null)
            {
                BrowserViewControl.NavigateTo(vm.SelectedTab.Url);
            }
        }
    }

    private async Task LoadBookmarksBarAsync()
    {
        BookmarksBarItems.Children.Clear();

        var bookmarks = await _bookmarkService.GetAllAsync();
        var barBookmarks = bookmarks.Where(b => !b.IsFolder).Take(20);

        foreach (var bookmark in barBookmarks)
        {
            var button = new Button
            {
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = GetIconForUrl(bookmark.Url),
                            FontSize = 12,
                            Margin = new Thickness(0, 0, 6, 0),
                            VerticalAlignment = VerticalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = bookmark.Title,
                            FontSize = 12,
                            MaxWidth = 120,
                            TextTrimming = TextTrimming.CharacterEllipsis,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                },
                ToolTip = $"{bookmark.Title}\n{bookmark.Url}",
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Transparent,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(2, 0, 2, 0),
                Cursor = Cursors.Hand,
                Tag = bookmark
            };

            button.Click += BookmarkBar_Click;
            button.MouseEnter += BookmarkBar_MouseEnter;
            button.MouseLeave += BookmarkBar_MouseLeave;

            BookmarksBarItems.Children.Add(button);
        }
    }

    private void BookmarkBar_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Bookmark bookmark)
        {
            var mainVm = DataContext as MainViewModel;
            mainVm?.CreateNewTabWithUrl(bookmark.Url);
        }
    }

    private void BookmarkBar_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Button button)
        {
            button.BorderBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
            button.Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));
        }
    }

    private void BookmarkBar_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Button button)
        {
            button.BorderBrush = Brushes.Transparent;
            button.Background = Brushes.Transparent;
        }
    }

    private static string GetIconForUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return "📄";

        var lowerUrl = url.ToLower();
        if (lowerUrl.Contains("youtube.com") || lowerUrl.Contains("youtu.be")) return "▶️";
        if (lowerUrl.Contains("github.com")) return "🐙";
        if (lowerUrl.Contains("twitter.com") || lowerUrl.Contains("x.com")) return "🐦";
        if (lowerUrl.Contains("facebook.com")) return "👤";
        if (lowerUrl.Contains("instagram.com")) return "📷";
        if (lowerUrl.Contains("reddit.com")) return "🤖";
        if (lowerUrl.Contains("wikipedia.org")) return "📚";
        if (lowerUrl.Contains("google.com")) return "🔍";
        if (lowerUrl.Contains("bing.com")) return "🔎";

        return "📄";
    }

    private void AddressBar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is MainViewModel vm)
        {
            vm.NavigateToUrlCommand.Execute(null);
            BrowserViewControl.Focus();
        }
    }

    private void AddressBar_GotFocus(object sender, RoutedEventArgs e)
    {
        AddressBar.SelectAll();
    }

    private void AddressBar_LostFocus(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.AddressBarText = vm.CurrentUrl;
        }
    }

    private void Tab_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border &&
            border.DataContext is TabInfo tab &&
            DataContext is MainViewModel vm)
        {
            vm.SelectTabCommand.Execute(tab);
        }
    }

    private void Tab_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.MiddleButton == MouseButtonState.Pressed &&
            sender is Border border &&
            border.DataContext is TabInfo tab &&
            DataContext is MainViewModel vm)
        {
            vm.CloseTabCommand.Execute(tab);
        }
    }

    private void TabClose_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void Logo_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.OpenHomepageCommand.Execute(null);
        }
    }

    public void FocusAddressBar()
    {
        AddressBar.Focus();
        AddressBar.SelectAll();
    }

    public async void RefreshBookmarksBar()
    {
        await LoadBookmarksBarAsync();
    }
}
