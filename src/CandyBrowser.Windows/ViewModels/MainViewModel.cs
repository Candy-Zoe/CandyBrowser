using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CandyBrowser.Shared.Abstractions;
using Models = CandyBrowser.Core.Models;

namespace CandyBrowser.Windows.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ITabManager _tabManager;
    private readonly IBookmarkService _bookmarkService;
    private readonly IHistoryService _historyService;
    private readonly INavigationService _navigationService;
    private readonly ISettingsService _settingsService;
    private readonly IPasswordService _passwordService;
    private readonly IExtensionService _extensionService;
    private readonly IDownloadService _downloadService;

    [ObservableProperty]
    private string _addressBarText = string.Empty;

    [ObservableProperty]
    private string _currentUrl = string.Empty;

    [ObservableProperty]
    private string _currentTitle = "新标签页";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _canGoBack;

    [ObservableProperty]
    private bool _canGoForward;

    [ObservableProperty]
    private Models.TabInfo? _selectedTab;

    [ObservableProperty]
    private bool _isBookmarkSidebarVisible;

    [ObservableProperty]
    private bool _isHistoryPanelVisible;

    [ObservableProperty]
    private bool _isSettingsVisible;

    [ObservableProperty]
    private string _windowTitle = "CandyZoe浏览器";

    public ObservableCollection<Models.TabInfo> Tabs { get; } = new();

    // Events for View layer to react to tab changes
    public event EventHandler<Models.TabInfo>? TabCreated;
    public event EventHandler<Models.TabInfo>? TabClosed;
    public event EventHandler? BookmarksChanged;

    public MainViewModel(
        ITabManager tabManager,
        IBookmarkService bookmarkService,
        IHistoryService historyService,
        INavigationService navigationService,
        ISettingsService settingsService,
        IPasswordService passwordService,
        IExtensionService extensionService,
        IDownloadService downloadService)
    {
        _tabManager = tabManager;
        _bookmarkService = bookmarkService;
        _historyService = historyService;
        _navigationService = navigationService;
        _settingsService = settingsService;
        _passwordService = passwordService;
        _extensionService = extensionService;
        _downloadService = downloadService;

        _navigationService.NavigationRequested += (_, url) => CurrentUrl = url;
        _navigationService.IsLoadingChanged += (_, loading) => IsLoading = loading;
        _navigationService.CanGoBackChanged += (_, canGo) => CanGoBack = canGo;
        _navigationService.CanGoForwardChanged += (_, canGo) => CanGoForward = canGo;
    }

    public async Task InitializeAsync()
    {
        var tabs = await _tabManager.GetAllAsync();
        foreach (var tab in tabs)
        {
            Tabs.Add(tab);
        }

        if (Tabs.Count == 0)
        {
            await NewTabAsync();
        }
        else
        {
            SelectedTab = Tabs.First();
            AddressBarText = SelectedTab.Url;
        }
    }

    [RelayCommand]
    private async Task NewTabAsync()
    {
        var homepage = await _settingsService.GetHomepageAsync();
        var tab = await _tabManager.CreateAsync(homepage);
        Tabs.Add(tab);
        SelectedTab = tab;
        AddressBarText = homepage;
        CurrentUrl = homepage;
        UpdateWindowTitle();
        TabCreated?.Invoke(this, tab);
    }

    public async Task CreateNewTabWithUrl(string url)
    {
        var tab = await _tabManager.CreateAsync(url);
        Tabs.Add(tab);
        SelectedTab = tab;
        AddressBarText = url;
        CurrentUrl = url;
        UpdateWindowTitle();
        TabCreated?.Invoke(this, tab);
    }

    [RelayCommand]
    private async Task CloseTabAsync(Models.TabInfo tab)
    {
        await _tabManager.CloseAsync(tab.Id);
        Tabs.Remove(tab);
        TabClosed?.Invoke(this, tab);

        if (Tabs.Count == 0)
        {
            await NewTabAsync();
        }
        else if (SelectedTab == tab)
        {
            SelectedTab = Tabs.Last();
            AddressBarText = SelectedTab.Url;
        }
        UpdateWindowTitle();
    }

    [RelayCommand]
    private void NavigateToUrl()
    {
        if (string.IsNullOrWhiteSpace(AddressBarText)) return;

        var input = AddressBarText.Trim();
        var url = NormalizeUrl(input);

        AddressBarText = url;
        CurrentUrl = url;

        if (SelectedTab != null)
        {
            SelectedTab.Url = url;
            _ = _tabManager.UpdateAsync(SelectedTab);
        }

        _ = _historyService.AddAsync(url, CurrentTitle);

        // Tell MainWindow to navigate the active BrowserView
        if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
        {
            mainWindow.NavigateActiveTab(url);
        }
    }

    private async Task<string> NormalizeUrlAsync(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        input = input.Trim();

        // Already has protocol
        if (input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            input.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            input.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
        {
            return input;
        }

        // Looks like a domain (contains dot and no spaces)
        if (input.Contains('.') && !input.Contains(' '))
        {
            return "https://" + input;
        }

        // Otherwise search
        var searchEngine = await _settingsService.GetSearchEngineAsync();
        return string.Format(searchEngine, Uri.EscapeDataString(input));
    }

    private string NormalizeUrl(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        input = input.Trim();

        // Already has protocol
        if (input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            input.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            input.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
        {
            return input;
        }

        // Looks like a domain (contains dot and no spaces)
        if (input.Contains('.') && !input.Contains(' '))
        {
            return "https://" + input;
        }

        // Otherwise search with Bing as fallback
        return "https://www.bing.com/search?q=" + Uri.EscapeDataString(input);
    }

    [RelayCommand]
    private void GoBack()
    {
        if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
            mainWindow.GetActiveBrowserView()?.GoBack();
    }

    [RelayCommand]
    private void GoForward()
    {
        if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
            mainWindow.GetActiveBrowserView()?.GoForward();
    }

    [RelayCommand]
    private void Reload()
    {
        if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
            mainWindow.GetActiveBrowserView()?.Refresh();
    }

    [RelayCommand]
    private void Stop()
    {
        if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
            mainWindow.GetActiveBrowserView()?.Stop();
    }

    [RelayCommand]
    private void ToggleBookmarkSidebar() => IsBookmarkSidebarVisible = !IsBookmarkSidebarVisible;

    [RelayCommand]
    private void ToggleHistoryPanel() => IsHistoryPanelVisible = !IsHistoryPanelVisible;

    [RelayCommand]
    private void OpenSettings()
    {
        var settingsWindow = new Views.SettingsWindow(_settingsService, _historyService, _passwordService)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        settingsWindow.ShowDialog();
    }

    [RelayCommand]
    private void OpenPasswordManager()
    {
        var passwordWindow = new Views.PasswordManagerWindow(_passwordService)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        passwordWindow.ShowDialog();
    }

    [RelayCommand]
    private void OpenExtensionManager()
    {
        var extensionWindow = new Views.ExtensionManagerWindow(_extensionService)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        extensionWindow.ShowDialog();
    }

    [RelayCommand]
    private void OpenDownloadManager()
    {
        var downloadWindow = new Views.DownloadManagerWindow(_downloadService)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        downloadWindow.ShowDialog();
    }

    [RelayCommand]
    private async Task AddBookmarkAsync()
    {
        if (string.IsNullOrEmpty(CurrentUrl) || CurrentUrl == "about:blank") return;

        await _bookmarkService.AddAsync(new Models.Bookmark
        {
            Title = CurrentTitle,
            Url = CurrentUrl
        });

        BookmarksChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task OpenHomepageAsync()
    {
        var homepage = await _settingsService.GetHomepageAsync();
        AddressBarText = homepage;
        CurrentUrl = homepage;

        if (SelectedTab != null)
        {
            SelectedTab.Url = homepage;
            _ = _tabManager.UpdateAsync(SelectedTab);
        }

        if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
        {
            mainWindow.NavigateActiveTab(homepage);
        }
    }

    public void UpdateCurrentTabInfo(string url, string title)
    {
        CurrentUrl = url;
        CurrentTitle = title;
        AddressBarText = url;

        if (SelectedTab != null)
        {
            SelectedTab.Url = url;
            SelectedTab.Title = title;
            _ = _tabManager.UpdateAsync(SelectedTab);
        }

        UpdateWindowTitle();
        _ = _historyService.AddAsync(url, title);
    }

    private void UpdateWindowTitle()
    {
        var title = string.IsNullOrEmpty(CurrentTitle) || CurrentTitle == "新标签页"
            ? "CandyZoe浏览器"
            : $"{CurrentTitle} - CandyZoe浏览器";
        WindowTitle = title;
    }

    [RelayCommand]
    private void SelectTab(Models.TabInfo tab)
    {
        SelectedTab = tab;
        AddressBarText = tab.Url;
        CurrentUrl = tab.Url;
        CurrentTitle = tab.Title ?? "新标签页";
        UpdateWindowTitle();
    }

    [RelayCommand]
    private void FocusAddressBar()
    {
        if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
        {
            mainWindow.FocusAddressBar();
        }
    }

    [RelayCommand]
    private void OpenDevTools()
    {
        if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
        {
            mainWindow.GetActiveBrowserView()?.OpenDevTools();
        }
    }
}
