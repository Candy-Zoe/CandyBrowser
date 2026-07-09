using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CandyBrowser.Windows.ViewModels;
using CandyBrowser.Core.Models;
using CandyBrowser.Shared.Abstractions;
using CandyBrowser.Windows.Views;

namespace CandyBrowser.Windows;

public partial class MainWindow : Window
{
    private readonly IBookmarkService _bookmarkService;
    private readonly IDownloadService _downloadService;
    private readonly IHistoryService _historyService;
    private readonly Dictionary<long, BrowserView> _tabViews = new();
    private readonly Dictionary<long, NewTabPage> _newTabPages = new();
    private BrowserView? _activeBrowserView;
    private NewTabPage? _activeNewTabPage;
    private readonly DispatcherTimer _suggestionTimer;
    private bool _isSuggestionActive;

    public MainWindow(MainViewModel viewModel, IBookmarkService bookmarkService, IDownloadService downloadService, IHistoryService historyService)
    {
        _bookmarkService = bookmarkService;
        _downloadService = downloadService;
        _historyService = historyService;
        DataContext = viewModel;
        InitializeComponent();

        _suggestionTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _suggestionTimer.Tick += SuggestionTimer_Tick;

        Loaded += MainWindow_Loaded;
        Views.SettingsWindow.SettingsChanged += SettingsWindow_SettingsChanged;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.PropertyChanged += Vm_PropertyChanged;
            vm.TabCreated += Vm_TabCreated;
            vm.TabClosed += Vm_TabClosed;
            vm.BookmarksChanged += async (_, _) => await LoadBookmarksBarAsync();

            // Subscribe BEFORE InitializeAsync so TabCreated events are caught
            await vm.InitializeAsync();

            // Show the selected tab
            if (vm.SelectedTab != null)
            {
                ShowTabView(vm.SelectedTab.Id);
            }
        }

        await LoadBookmarksBarAsync();
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedTab))
        {
            if (DataContext is MainViewModel vm && vm.SelectedTab != null)
            {
                ShowTabView(vm.SelectedTab.Id);
            }
        }
    }

    private void Vm_TabCreated(object? sender, TabInfo tab)
    {
        CreateTabView(tab);
        ShowTabView(tab.Id);
    }

    private void Vm_TabClosed(object? sender, TabInfo tab)
    {
        RemoveTabView(tab.Id);
    }

    private void CreateTabView(TabInfo tab)
    {
        // Check if this is a new blank tab → show NewTabPage
        if (string.IsNullOrEmpty(tab.Url) || tab.Url == "about:blank")
        {
            if (_newTabPages.ContainsKey(tab.Id)) return;

            var newTab = new NewTabPage();
            newTab.NavigateRequested += (_, url) =>
            {
                // Replace NewTabPage with BrowserView when user navigates
                ReplaceNewTabWithBrowser(tab.Id, url);
            };
            _newTabPages[tab.Id] = newTab;
            TabContentContainer.Children.Add(newTab);
            newTab.Visibility = Visibility.Collapsed;
            return;
        }

        // Regular tab → create BrowserView
        if (_tabViews.ContainsKey(tab.Id)) return;

        var browserView = new BrowserView();
        browserView.SetDownloadService(_downloadService);
        _tabViews[tab.Id] = browserView;
        TabContentContainer.Children.Add(browserView);
        browserView.Visibility = Visibility.Collapsed;
        browserView.NavigateTo(tab.Url);
    }

    private void ReplaceNewTabWithBrowser(long tabId, string url)
    {
        // Remove NewTabPage
        if (_newTabPages.TryGetValue(tabId, out var newTabPage))
        {
            TabContentContainer.Children.Remove(newTabPage);
            _newTabPages.Remove(tabId);
        }

        // Create BrowserView
        var browserView = new BrowserView();
        browserView.SetDownloadService(_downloadService);
        _tabViews[tabId] = browserView;
        TabContentContainer.Children.Add(browserView);

        // Show it
        HideActiveView();
        _activeBrowserView = browserView;
        _activeNewTabPage = null;
        browserView.Visibility = Visibility.Visible;
        browserView.NavigateTo(url);

        // Update ViewModel
        if (DataContext is MainViewModel vm)
        {
            vm.AddressBarText = url;
            if (vm.SelectedTab != null && vm.SelectedTab.Id == tabId)
            {
                vm.SelectedTab.Url = url;
            }
        }
    }

    private void ShowTabView(long tabId)
    {
        HideActiveView();

        // Check if it's a NewTabPage
        if (_newTabPages.TryGetValue(tabId, out var newTab))
        {
            _activeNewTabPage = newTab;
            _activeBrowserView = null;
            newTab.Visibility = Visibility.Visible;
            return;
        }

        // Check if it's a BrowserView
        if (_tabViews.TryGetValue(tabId, out var view))
        {
            _activeBrowserView = view;
            _activeNewTabPage = null;
            view.Visibility = Visibility.Visible;
        }
    }

    private void HideActiveView()
    {
        if (_activeBrowserView != null)
            _activeBrowserView.Visibility = Visibility.Collapsed;
        if (_activeNewTabPage != null)
            _activeNewTabPage.Visibility = Visibility.Collapsed;
    }

    public void RemoveTabView(long tabId)
    {
        if (_tabViews.TryGetValue(tabId, out var view))
        {
            TabContentContainer.Children.Remove(view);
            _tabViews.Remove(tabId);
            if (_activeBrowserView == view) _activeBrowserView = null;
            view.Dispose();
        }

        if (_newTabPages.TryGetValue(tabId, out var newTab))
        {
            TabContentContainer.Children.Remove(newTab);
            _newTabPages.Remove(tabId);
            if (_activeNewTabPage == newTab) _activeNewTabPage = null;
        }
    }

    public BrowserView? GetActiveBrowserView() => _activeBrowserView;

    public void NavigateActiveTab(string url)
    {
        if (_activeBrowserView != null)
        {
            _activeBrowserView.NavigateTo(url);
        }
        else if (_activeNewTabPage != null)
        {
            // If current tab is a NewTabPage, replace it with BrowserView
            if (DataContext is MainViewModel vm && vm.SelectedTab != null)
            {
                ReplaceNewTabWithBrowser(vm.SelectedTab.Id, url);
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
        if (e.Key == Key.Enter)
        {
            if (_isSuggestionActive && SuggestionsList.SelectedItem is SuggestionViewModel suggestion)
            {
                ApplySuggestion(suggestion);
            }
            else if (DataContext is MainViewModel vm)
            {
                SuggestionsPopup.IsOpen = false;
                _isSuggestionActive = false;
                vm.NavigateToUrlCommand.Execute(null);
                _activeBrowserView?.Focus();
            }
        }
    }

    private void AddressBar_GotFocus(object sender, RoutedEventArgs e)
    {
        AddressBar.SelectAll();
        // Re-trigger suggestions if there's text
        if (!string.IsNullOrEmpty(AddressBar.Text) && AddressBar.Text.Length >= 2)
        {
            _ = LoadSuggestionsAsync();
        }
    }

    private void AddressBar_LostFocus(object sender, RoutedEventArgs e)
    {
        // Delay closing popup to allow click on suggestion
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (!SuggestionsList.IsMouseOver)
            {
                SuggestionsPopup.IsOpen = false;
                _isSuggestionActive = false;
                if (DataContext is MainViewModel vm)
                {
                    vm.AddressBarText = vm.CurrentUrl;
                }
            }
        }), DispatcherPriority.Background);
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

    private void NewTab_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.NewTabCommand.Execute(null);
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

    // --- Search Suggestions ---

    private void AddressBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        _suggestionTimer.Stop();
        _suggestionTimer.Start();
    }

    private void SuggestionTimer_Tick(object? sender, EventArgs e)
    {
        _suggestionTimer.Stop();
        _ = LoadSuggestionsAsync();
    }

    private async Task LoadSuggestionsAsync()
    {
        var query = AddressBar.Text?.Trim();
        if (string.IsNullOrEmpty(query) || query.Length < 2)
        {
            SuggestionsPopup.IsOpen = false;
            return;
        }

        var suggestions = new List<SuggestionViewModel>();

        // Search history
        var historyResults = await _historyService.SearchAsync(query, 5);
        foreach (var h in historyResults)
        {
            suggestions.Add(new SuggestionViewModel("🕐", h.Title, h.Url, 0));
        }

        // Search bookmarks
        var bookmarkResults = await _bookmarkService.SearchAsync(query, 5);
        foreach (var b in bookmarkResults)
        {
            // Skip duplicates from history
            if (!suggestions.Any(s => s.Url == b.Url))
            {
                suggestions.Add(new SuggestionViewModel("⭐", b.Title, b.Url, 1));
            }
        }

        // Add search suggestion
        suggestions.Add(new SuggestionViewModel("🔍", $"搜索 \"{query}\"", query, 2, false));

        SuggestionsList.ItemsSource = suggestions;
        SuggestionsPopup.IsOpen = suggestions.Count > 0;
        _isSuggestionActive = true;
    }

    private void AddressBar_KeyUp(object sender, KeyEventArgs e)
    {
        if (!SuggestionsPopup.IsOpen) return;

        if (e.Key == Key.Down)
        {
            var idx = SuggestionsList.SelectedIndex;
            if (idx < SuggestionsList.Items.Count - 1)
                SuggestionsList.SelectedIndex = idx + 1;
        }
        else if (e.Key == Key.Up)
        {
            var idx = SuggestionsList.SelectedIndex;
            if (idx > 0)
                SuggestionsList.SelectedIndex = idx - 1;
        }
        else if (e.Key == Key.Escape)
        {
            SuggestionsPopup.IsOpen = false;
            _isSuggestionActive = false;
            AddressBar.Focus();
        }
    }

    private void SuggestionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SuggestionsList.SelectedItem is SuggestionViewModel suggestion)
        {
            AddressBar.Text = suggestion.Url;
        }
    }

    private void SuggestionsList_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (SuggestionsList.SelectedItem is SuggestionViewModel suggestion)
        {
            ApplySuggestion(suggestion);
        }
    }

    private void ApplySuggestion(SuggestionViewModel suggestion)
    {
        SuggestionsPopup.IsOpen = false;
        _isSuggestionActive = false;

        if (DataContext is MainViewModel vm)
        {
            // Always navigate in current tab
            vm.AddressBarText = suggestion.Url;
            vm.NavigateToUrlCommand.Execute(null);
        }

        _activeBrowserView?.Focus();
    }

    private async void SettingsWindow_SettingsChanged(object? sender, EventArgs e)
    {
        // Apply bookmarks bar visibility
        if (DataContext is MainViewModel vm)
        {
            var showBookmarksBar = await App.GetService<ISettingsService>().GetAsync("show_bookmarks_bar", "true");
            BookmarksBarBorder.Visibility = showBookmarksBar?.ToLower() == "true"
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }
}
