using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Diagnostics;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using CandyBrowser.Windows.Views;

namespace CandyBrowser.Windows;

public partial class MainWindow : Window
{
    private readonly Dictionary<int, WebView2> _tabs = new();
    private readonly Dictionary<int, string> _tabUrls = new();
    private readonly Dictionary<int, string> _tabTitles = new();
    private int _nextTabId = 1;
    private int _activeTabId = -1;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            try
            {
                await CreateNewTab();
                UpdateBookmarksBar();
                StatusText.Text = "浏览器就绪";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"初始化错误: {ex.Message}";
            }
        };
    }

    private async Task CreateNewTab(string? url = null)
    {
        var tabId = _nextTabId++;
        var webView = new WebView2 { Visibility = Visibility.Collapsed };

        try
        {
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CandyBrowser", "WebView2");
            Directory.CreateDirectory(userDataFolder);

            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await webView.EnsureCoreWebView2Async(env);

            // Handle new window requests
            webView.CoreWebView2.NewWindowRequested += (_, e) =>
            {
                e.Handled = true;
                Dispatcher.Invoke(() => _ = CreateNewTab(e.Uri));
            };

            // Track navigation
            webView.NavigationStarting += (_, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (!_tabs.ContainsKey(tabId)) return;
                    _tabUrls[tabId] = e.Uri;
                    if (tabId == _activeTabId)
                    {
                        AddressBar.Text = e.Uri;
                        StatusText.Text = $"加载: {e.Uri}";
                    }
                });
            };

            webView.NavigationCompleted += (_, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (!_tabs.ContainsKey(tabId)) return;
                    try
                    {
                        var title = webView.CoreWebView2?.DocumentTitle ?? "新标签页";
                        _tabTitles[tabId] = title;
                        RefreshTabStrip();
                        if (tabId == _activeTabId)
                        {
                            Title = $"CandyZoe - {title}";
                            StatusText.Text = e.IsSuccess ? "就绪" : "加载失败";
                        }

                        // Record history
                        if (e.IsSuccess && !string.IsNullOrEmpty(_tabUrls[tabId]))
                        {
                            var historyUrl = _tabUrls[tabId];
                            if (!historyUrl.StartsWith("about:"))
                            {
                                App.AddHistory(historyUrl, title);
                            }
                        }
                    }
                    catch { }
                });
            };
        }
        catch (Exception ex)
        {
            StatusText.Text = $"标签初始化失败: {ex.Message}";
            return;
        }

        _tabs[tabId] = webView;
        _tabUrls[tabId] = url ?? "";
        _tabTitles[tabId] = "新标签页";
        TabContentContainer.Children.Add(webView);
        RefreshTabStrip();
        SelectTab(tabId);

        if (!string.IsNullOrEmpty(url))
        {
            webView.CoreWebView2?.Navigate(url);
        }
    }

    private void SelectTab(int tabId)
    {
        if (!_tabs.ContainsKey(tabId)) return;

        // Hide all tabs
        foreach (var w in _tabs.Values)
            w.Visibility = Visibility.Collapsed;

        // Show selected tab
        _activeTabId = tabId;
        _tabs[tabId].Visibility = Visibility.Visible;

        // Update UI
        AddressBar.Text = _tabUrls.GetValueOrDefault(tabId, "");
        Title = $"CandyZoe - {_tabTitles.GetValueOrDefault(tabId, "新标签页")}";
        StatusText.Text = "就绪";

        RefreshTabStrip();
    }

    private void CloseTab(int tabId)
    {
        if (!_tabs.ContainsKey(tabId)) return;

        var w = _tabs[tabId];
        TabContentContainer.Children.Remove(w);
        try { w.Dispose(); } catch { }

        _tabs.Remove(tabId);
        _tabUrls.Remove(tabId);
        _tabTitles.Remove(tabId);

        if (_tabs.Count == 0)
        {
            _ = CreateNewTab();
        }
        else if (_activeTabId == tabId)
        {
            SelectTab(_tabs.Keys.Last());
        }
        else
        {
            RefreshTabStrip();
        }
    }

    private void RefreshTabStrip()
    {
        TabStrip.Children.Clear();

        foreach (var tabId in _tabs.Keys)
        {
            var title = _tabTitles.GetValueOrDefault(tabId, "新标签页");
            if (title.Length > 20) title = title[..20] + "...";

            // Create tab border
            var tabBorder = new Border
            {
                Background = tabId == _activeTabId ? Brushes.White : new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0)),
                Padding = new Thickness(8, 4, 4, 4),
                Margin = new Thickness(1, 0, 0, 0),
                Cursor = Cursors.Hand,
                MinWidth = 80,
                MaxWidth = 200
            };

            var panel = new DockPanel();

            // Close button
            var closeBtn = new Button
            {
                Content = "✕",
                FontSize = 10,
                Width = 18,
                Height = 18,
                Margin = new Thickness(4, 0, 0, 0),
                Cursor = Cursors.Hand,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center
            };

            int capturedTabId = tabId;
            closeBtn.Click += (s, e) =>
            {
                e.Handled = true;
                CloseTab(capturedTabId);
            };

            DockPanel.SetDock(closeBtn, Dock.Right);
            panel.Children.Add(closeBtn);

            // Tab title
            var titleBlock = new TextBlock
            {
                Text = title,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            panel.Children.Add(titleBlock);

            tabBorder.Child = panel;

            // Click to select tab
            tabBorder.MouseLeftButtonDown += (s, e) =>
            {
                SelectTab(capturedTabId);
            };

            TabStrip.Children.Add(tabBorder);
        }
    }

    private void Navigate(string url)
    {
        if (string.IsNullOrEmpty(url) || !_tabs.ContainsKey(_activeTabId)) return;

        // Normalize URL
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
        {
            if (url.Contains('.') && !url.Contains(' '))
                url = "https://" + url;
            else
                url = string.Format(App.Settings.SearchEngine, Uri.EscapeDataString(url));
        }

        _tabUrls[_activeTabId] = url;
        AddressBar.Text = url;
        StatusText.Text = $"导航到: {url}";

        _tabs[_activeTabId].CoreWebView2?.Navigate(url);
    }

    private void UpdateBookmarksBar()
    {
        BookmarksBar.Children.Clear();
        var bookmarks = App.GetBookmarksByParent(null).Where(b => !b.IsFolder).Take(15).ToList();

        foreach (var b in bookmarks)
        {
            var btn = new Button
            {
                Content = b.Title,
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(2, 0, 2, 0),
                Cursor = Cursors.Hand,
                FontSize = 11,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.LightGray
            };

            string bookmarkUrl = b.Url;
            btn.Click += (_, _) => Navigate(bookmarkUrl);

            BookmarksBar.Children.Add(btn);
        }
    }

    #region Event Handlers

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.T && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _ = CreateNewTab();
            e.Handled = true;
        }
        else if (e.Key == Key.W && Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (_activeTabId > 0) CloseTab(_activeTabId);
            e.Handled = true;
        }
        else if (e.Key == Key.F5)
        {
            if (_tabs.TryGetValue(_activeTabId, out var w)) w.Reload();
            e.Handled = true;
        }
        else if (e.Key == Key.F12)
        {
            if (_tabs.TryGetValue(_activeTabId, out var w)) w.CoreWebView2?.OpenDevToolsWindow();
            e.Handled = true;
        }
        else if (e.Key == Key.L && Keyboard.Modifiers == ModifierKeys.Control)
        {
            AddressBar.Focus();
            AddressBar.SelectAll();
            e.Handled = true;
        }
        else if (e.Key == Key.OemPlus && Keyboard.Modifiers == ModifierKeys.Control)
        {
            ZoomInBtn_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == Key.OemMinus && Keyboard.Modifiers == ModifierKeys.Control)
        {
            ZoomOutBtn_Click(sender, e);
            e.Handled = true;
        }
    }

    private void AddressBar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Navigate(AddressBar.Text.Trim());
            e.Handled = true;
        }
    }

    private void NewTabBtn_Click(object sender, RoutedEventArgs e) => _ = CreateNewTab();

    private void BackBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_tabs.TryGetValue(_activeTabId, out var w) && w.CoreWebView2?.CanGoBack == true)
            w.CoreWebView2.GoBack();
    }

    private void ForwardBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_tabs.TryGetValue(_activeTabId, out var w) && w.CoreWebView2?.CanGoForward == true)
            w.CoreWebView2.GoForward();
    }

    private void ReloadBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_tabs.TryGetValue(_activeTabId, out var w)) w.Reload();
    }

    private void HomeBtn_Click(object sender, RoutedEventArgs e)
    {
        Navigate(App.Settings.Homepage);
    }

    private void BookmarkBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_activeTabId < 0 || !_tabUrls.ContainsKey(_activeTabId)) return;

        var url = _tabUrls[_activeTabId];
        if (string.IsNullOrEmpty(url)) return;

        var title = _tabTitles.GetValueOrDefault(_activeTabId, url);
        App.AddBookmark(title, url);
        UpdateBookmarksBar();
        StatusText.Text = $"已收藏: {title}";
    }

    private void HistoryBtn_Click(object sender, RoutedEventArgs e)
    {
        var win = new HistoryWindow();
        win.Owner = this;
        win.ShowDialog();
    }

    private void SettingsBtn_Click(object sender, RoutedEventArgs e)
    {
        var win = new SettingsWindow();
        win.Owner = this;
        if (win.ShowDialog() == true)
        {
            UpdateBookmarksBar();
        }
    }

    private void ZoomInBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_tabs.TryGetValue(_activeTabId, out var w) && w.ZoomFactor < 5.0)
        {
            w.ZoomFactor = Math.Min(5.0, w.ZoomFactor + 0.2);
            ZoomText.Text = $"{(int)(w.ZoomFactor * 100)}%";
        }
    }

    private void ZoomOutBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_tabs.TryGetValue(_activeTabId, out var w) && w.ZoomFactor > 0.25)
        {
            w.ZoomFactor = Math.Max(0.25, w.ZoomFactor - 0.2);
            ZoomText.Text = $"{(int)(w.ZoomFactor * 100)}%";
        }
    }

    #endregion
}
