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
    private readonly Dictionary<int, TabState> _tabs = new();
    private readonly List<int> _tabOrder = new();
    private readonly List<ClosedTab> _recentlyClosed = new();
    private int _nextTabId = 1;
    private int _activeTabId = -1;

    // 鼠标手势
    private Point _gestureStart;
    private bool _isGesture;

    // 下载列表
    private readonly List<DownloadItem> _downloads = new();

    // Edge 风格颜色
    private static readonly SolidColorBrush ActiveTabBrush = new(Color.FromRgb(0xFF, 0xFF, 0xFF));
    private static readonly SolidColorBrush InactiveTabBrush = new(Color.FromRgb(0xCC, 0xD0, 0xD6));
    private static readonly SolidColorBrush HoverTabBrush = new(Color.FromRgb(0xD8, 0xDC, 0xE2));
    private static readonly SolidColorBrush TabTextBrush = new(Color.FromRgb(0x33, 0x33, 0x33));

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            BookmarksBarBorder.Visibility = App.Settings.ShowBookmarksBar
                ? Visibility.Visible : Visibility.Collapsed;

            StatusText.Text = "正在初始化 WebView2...";

            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CandyBrowser", "WebView2_v3");
            Directory.CreateDirectory(userDataFolder);

            var options = new CoreWebView2EnvironmentOptions
            {
                AdditionalBrowserArguments = "--disable-gpu"
            };
            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);

            await MainWebView.EnsureCoreWebView2Async(env);

            // 注册 WebView2 事件
            MainWebView.CoreWebView2.NavigationStarting += (s, ev) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (_activeTabId >= 0 && _tabs.ContainsKey(_activeTabId))
                    {
                        _tabs[_activeTabId].Url = ev.Uri;
                        AddressBar.Text = ev.Uri;
                        StatusText.Text = $"加载: {ev.Uri}";
                        UpdateSecurityIcon(ev.Uri);
                    }
                });
            };

            MainWebView.CoreWebView2.NavigationCompleted += (s, ev) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (_activeTabId >= 0 && _tabs.ContainsKey(_activeTabId))
                    {
                        var title = MainWebView.CoreWebView2?.DocumentTitle ?? "新标签页";
                        _tabs[_activeTabId].Title = title;
                        Title = $"CandyZoe - {title}";
                        StatusText.Text = ev.IsSuccess ? $"就绪 - {title}" : $"加载失败({ev.WebErrorStatus})";
                        RefreshTabStrip();
                    }
                    if (ev.IsSuccess)
                    {
                        var url = MainWebView.CoreWebView2?.Source ?? "";
                        var title = MainWebView.CoreWebView2?.DocumentTitle ?? "";
                        if (!url.StartsWith("about:"))
                            App.AddHistory(url, title);
                    }
                });
            };

            MainWebView.CoreWebView2.NewWindowRequested += (s, ev) =>
            {
                ev.Handled = true;
                Dispatcher.Invoke(() => CreateNewTab(ev.Uri));
            };

            // 下载处理
            MainWebView.CoreWebView2.DownloadStarting += (s, ev) =>
            {
                Dispatcher.Invoke(() =>
                {
                    var item = new DownloadItem
                    {
                        FileName = Path.GetFileName(ev.DownloadOperation.Uri.ToString()),
                        Url = ev.DownloadOperation.Uri.ToString(),
                        Status = "下载中..."
                    };
                    _downloads.Insert(0, item);
                    DownloadPanel.Visibility = Visibility.Visible;
                    RefreshDownloadList();
                    StatusText.Text = $"开始下载: {item.FileName}";
                });
            };

            // 自定义右键菜单
            MainWebView.CoreWebView2.ContextMenuRequested += (s, ev) =>
            {
                // 使用默认上下文菜单，但可以通过 ev.MenuItems 自定义
            };

            CreateNewTab();
            UpdateBookmarksBar();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"初始化错误: {ex.Message}";
            MessageBox.Show($"WebView2 初始化失败:\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #region Tab Management

    private void CreateNewTab(string? url = null)
    {
        var tabId = _nextTabId++;
        var targetUrl = !string.IsNullOrEmpty(url) ? url : App.Settings.Homepage;

        _tabs[tabId] = new TabState { Url = targetUrl, Title = "新标签页" };
        _tabOrder.Add(tabId);

        SelectTab(tabId);
        MainWebView.CoreWebView2?.Navigate(targetUrl);
        RefreshTabStrip();
    }

    private void SelectTab(int tabId)
    {
        if (!_tabs.ContainsKey(tabId)) return;
        _activeTabId = tabId;
        var tab = _tabs[tabId];

        AddressBar.Text = tab.Url;
        Title = $"CandyZoe - {tab.Title}";
        StatusText.Text = "就绪";
        ZoomText.Text = $"{(int)(MainWebView.ZoomFactor * 100)}%";
        UpdateSecurityIcon(tab.Url);
        RefreshTabStrip();
    }

    private void CloseTab(int tabId)
    {
        if (!_tabs.ContainsKey(tabId)) return;

        var tab = _tabs[tabId];
        _recentlyClosed.Insert(0, new ClosedTab { Url = tab.Url, Title = tab.Title });
        if (_recentlyClosed.Count > 20) _recentlyClosed.RemoveAt(20);

        _tabs.Remove(tabId);
        _tabOrder.Remove(tabId);

        if (_tabs.Count == 0)
        {
            _activeTabId = -1;
            CreateNewTab();
        }
        else if (_activeTabId == tabId)
        {
            var idx = _tabOrder.IndexOf(tabId);
            if (idx < 0) idx = 0;
            if (idx >= _tabOrder.Count) idx = _tabOrder.Count - 1;
            SelectTab(_tabOrder[idx]);
            if (_tabs.TryGetValue(_activeTabId, out var t))
                MainWebView.CoreWebView2?.Navigate(t.Url);
        }
        else
        {
            RefreshTabStrip();
        }
    }

    private void RestoreClosedTab()
    {
        if (_recentlyClosed.Count == 0) return;
        var closed = _recentlyClosed[0];
        _recentlyClosed.RemoveAt(0);
        CreateNewTab(closed.Url);
    }

    private void RefreshTabStrip()
    {
        TabStrip.Children.Clear();
        foreach (var tabId in _tabOrder)
        {
            if (!_tabs.TryGetValue(tabId, out var tab)) continue;
            var title = tab.Title;
            if (title.Length > 24) title = title[..24] + "…";
            var isActive = tabId == _activeTabId;

            var tabBorder = new Border
            {
                Background = isActive ? ActiveTabBrush : InactiveTabBrush,
                CornerRadius = new CornerRadius(8, 8, 0, 0),
                Padding = new Thickness(10, 5, 6, 5),
                Margin = new Thickness(1, 0, 0, 0),
                Cursor = Cursors.Hand,
                MinWidth = 60,
                MaxWidth = 220
            };

            var panel = new DockPanel();
            var closeBtn = new Button
            {
                Content = "✕", FontSize = 9, Width = 18, Height = 18,
                Margin = new Thickness(4, 0, 0, 0), Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66))
            };
            closeBtn.Template = new ControlTemplate(typeof(Button));
            closeBtn.Background = Brushes.Transparent;
            closeBtn.BorderThickness = new Thickness(0);

            int capturedTabId = tabId;
            closeBtn.Click += (s, e) => { e.Handled = true; CloseTab(capturedTabId); };
            DockPanel.SetDock(closeBtn, Dock.Right);
            panel.Children.Add(closeBtn);

            panel.Children.Add(new TextBlock
            {
                Text = title, FontSize = 12, Foreground = TabTextBrush,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            tabBorder.Child = panel;
            tabBorder.MouseEnter += (s, e) => { if (tabId != _activeTabId) tabBorder.Background = HoverTabBrush; };
            tabBorder.MouseLeave += (s, e) => { tabBorder.Background = tabId == _activeTabId ? ActiveTabBrush : InactiveTabBrush; };
            tabBorder.MouseLeftButtonDown += (s, e) =>
            {
                SelectTab(capturedTabId);
                if (_tabs.TryGetValue(capturedTabId, out var t))
                    MainWebView.CoreWebView2?.Navigate(t.Url);
            };

            // 中键关闭标签
            tabBorder.PreviewMouseDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Middle) { e.Handled = true; CloseTab(capturedTabId); }
            };

            TabStrip.Children.Add(tabBorder);
        }
    }

    #endregion

    #region Navigation

    private void Navigate(string url)
    {
        if (string.IsNullOrEmpty(url) || _activeTabId < 0) return;
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
        {
            if (url.Contains('.') && !url.Contains(' '))
                url = "https://" + url;
            else
                url = string.Format(App.Settings.SearchEngine, Uri.EscapeDataString(url));
        }
        if (_tabs.ContainsKey(_activeTabId))
            _tabs[_activeTabId].Url = url;
        AddressBar.Text = url;
        StatusText.Text = $"导航到: {url}";
        UpdateSecurityIcon(url);
        MainWebView.CoreWebView2?.Navigate(url);
    }

    private void UpdateSecurityIcon(string url)
    {
        if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            SecurityIcon.Text = "\uE72E"; // Lock icon
            SecurityIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x80, 0x00));
            SecurityIcon.ToolTip = "安全连接 (HTTPS)";
        }
        else if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            SecurityIcon.Text = "\uE7BA"; // Warning icon
            SecurityIcon.Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0x66, 0x00));
            SecurityIcon.ToolTip = "不安全连接 (HTTP)";
        }
        else
        {
            SecurityIcon.Text = "\uE946"; // Info icon
            SecurityIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99));
            SecurityIcon.ToolTip = "内部页面";
        }
    }

    #endregion

    #region Find on Page

    private void ShowFindBar()
    {
        FindBar.Visibility = Visibility.Visible;
        FindBox.Focus();
        FindBox.SelectAll();
        Keyboard.Focus(FindBox);
    }

    private void CloseFindBar()
    {
        FindBar.Visibility = Visibility.Collapsed;
        MainWebView.CoreWebView2?.ExecuteScriptAsync("window.getSelection().removeAllRanges();");
        Keyboard.Focus(MainWebView);
    }

    private void FindBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var text = FindBox.Text;
        if (string.IsNullOrEmpty(text))
        {
            FindMatchCount.Text = "";
            return;
        }
        // 使用 JS 高亮查找
        var js = $@"
            (function() {{
                window.getSelection().removeAllRanges();
                var text = '{text.Replace("'", "\\'")}';
                if (!text) return 0;
                var count = 0;
                var body = document.body;
                var regex = new RegExp(text.replace(/[.*+?^${{}}|()\\[\\]\\\\]/g, '\\\\$&'), 'gi');
                var matches = body.innerText.match(regex);
                count = matches ? matches.length : 0;
                // 滚动到第一个匹配
                if (count > 0) {{
                    var range = document.createRange();
                    var walker = document.createTreeWalker(body, NodeFilter.SHOW_TEXT);
                    while (walker.nextNode()) {{
                        var idx = walker.currentNode.nodeValue.toLowerCase().indexOf(text.toLowerCase());
                        if (idx >= 0) {{
                            range.setStart(walker.currentNode, idx);
                            range.setEnd(walker.currentNode, idx + text.length);
                            window.getSelection().addRange(range);
                            walker.currentNode.parentElement.scrollIntoView({{block:'center'}});
                            break;
                        }}
                    }}
                }}
                return count;
            }})()";
        _ = MainWebView.CoreWebView2?.ExecuteScriptAsync(js);
        FindMatchCount.Text = "查找中...";
        // 延迟获取匹配数
        Task.Delay(300).ContinueWith(_ =>
        {
            Dispatcher.Invoke(async () =>
            {
                if (MainWebView.CoreWebView2 == null) return;
                var result = await MainWebView.CoreWebView2.ExecuteScriptAsync(
                    "window.getSelection().rangeCount > 0 ? document.body.innerText.match(/" +
                    text.Replace("'", "\\'").Replace("/", "\\/") +
                    "/gi)?.length || 0 : 0") ?? "\"0\"";
                FindMatchCount.Text = result.Trim('"') + " 个匹配";
            });
        });
    }

    private void FindBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { FindNext(); e.Handled = true; }
        else if (e.Key == Key.Escape) { CloseFindBar(); e.Handled = true; }
    }

    private void FindNext() { /* 已在 TextChanged 中处理 */ }
    private void FindPrev() { /* 已在 TextChanged 中处理 */ }
    private void FindNext_Click(object sender, RoutedEventArgs e) => FindNext();
    private void FindPrev_Click(object sender, RoutedEventArgs e) => FindPrev();
    private void CloseFindBar_Click(object sender, RoutedEventArgs e) => CloseFindBar();

    #endregion

    #region Downloads

    private void RefreshDownloadList()
    {
        DownloadList.Children.Clear();
        foreach (var d in _downloads.Take(10))
        {
            var border = new Border
            {
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 1, 0, 1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
            var panel = new DockPanel();
            panel.Children.Add(new TextBlock
            {
                Text = d.FileName, FontSize = 12, FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });
            panel.Children.Add(new TextBlock
            {
                Text = d.Status, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0)
            });
            border.Child = panel;
            DownloadList.Children.Add(border);
        }
    }

    private void DownloadBtn_Click(object sender, RoutedEventArgs e)
    {
        DownloadPanel.Visibility = DownloadPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed : Visibility.Visible;
    }

    private void CloseDownloadPanel_Click(object sender, RoutedEventArgs e)
    {
        DownloadPanel.Visibility = Visibility.Collapsed;
    }

    #endregion

    #region Bookmarks Bar

    private void UpdateBookmarksBar()
    {
        BookmarksBar.Children.Clear();
        var bookmarks = App.GetBookmarksByParent(null).Where(b => !b.IsFolder).Take(20).ToList();
        foreach (var b in bookmarks)
        {
            var btn = new Button
            {
                Content = b.Title, Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(1, 0, 1, 0), Cursor = Cursors.Hand, FontSize = 11,
                Background = Brushes.Transparent, BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33))
            };
            btn.MouseEnter += (s, e) => btn.Background = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
            btn.MouseLeave += (s, e) => btn.Background = Brushes.Transparent;
            string bookmarkUrl = b.Url;
            btn.Click += (_, _) => Navigate(bookmarkUrl);
            BookmarksBar.Children.Add(btn);
        }
    }

    #endregion

    #region Keyboard & Mouse

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        var ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        var shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

        if (e.Key == Key.T && ctrl && shift) { RestoreClosedTab(); e.Handled = true; }
        else if (e.Key == Key.T && ctrl) { CreateNewTab(); e.Handled = true; }
        else if (e.Key == Key.W && ctrl) { if (_activeTabId > 0) CloseTab(_activeTabId); e.Handled = true; }
        else if (e.Key == Key.F5 && shift) { /* Hard refresh - not implemented */ MainWebView.Reload(); e.Handled = true; }
        else if (e.Key == Key.F5) { MainWebView.Reload(); e.Handled = true; }
        else if (e.Key == Key.F11) { ToggleFullScreen(); e.Handled = true; }
        else if (e.Key == Key.F12) { MainWebView.CoreWebView2?.OpenDevToolsWindow(); e.Handled = true; }
        else if (e.Key == Key.L && ctrl) { AddressBar.Focus(); AddressBar.SelectAll(); Keyboard.Focus(AddressBar); e.Handled = true; }
        else if (e.Key == Key.F && ctrl) { ShowFindBar(); e.Handled = true; }
        else if (e.Key == Key.J && ctrl) { DownloadBtn_Click(sender, e); e.Handled = true; }
        else if (e.Key == Key.D && ctrl) { BookmarkBtn_Click(sender, e); e.Handled = true; }
        else if (e.Key == Key.H && ctrl) { HistoryBtn_Click(sender, e); e.Handled = true; }
        else if (e.Key == Key.Tab && ctrl)
        {
            if (_tabOrder.Count > 1)
            {
                var idx = _tabOrder.IndexOf(_activeTabId);
                idx = shift ? (idx <= 0 ? _tabOrder.Count - 1 : idx - 1) : (idx + 1) % _tabOrder.Count;
                SelectTab(_tabOrder[idx]);
                if (_tabs.TryGetValue(_activeTabId, out var t))
                    MainWebView.CoreWebView2?.Navigate(t.Url);
            }
            e.Handled = true;
        }
        else if (e.Key == Key.OemPlus && ctrl) { ZoomInBtn_Click(sender, e); e.Handled = true; }
        else if (e.Key == Key.OemMinus && ctrl) { ZoomOutBtn_Click(sender, e); e.Handled = true; }
        else if (e.Key == Key.R && ctrl) { MainWebView.Reload(); e.Handled = true; }
    }

    private void Window_KeyUp(object sender, KeyEventArgs e)
    {
        // 鼠标手势结束
        if (e.Key == Key.Right && _isGesture)
        {
            _isGesture = false;
            if (MainWebView.CoreWebView2?.CanGoForward == true)
            {
                MainWebView.CoreWebView2.GoForward();
                StatusText.Text = "手势: 前进";
            }
        }
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonDown(e);
        _gestureStart = e.GetPosition(this);
        _isGesture = true;
    }

    protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonUp(e);
        if (_isGesture)
        {
            var end = e.GetPosition(this);
            var dx = end.X - _gestureStart.X;
            if (dx < -50 && MainWebView.CoreWebView2?.CanGoBack == true)
            {
                MainWebView.CoreWebView2.GoBack();
                StatusText.Text = "手势: 后退";
            }
            _isGesture = false;
        }
    }

    #endregion

    #region FullScreen

    private bool _isFullScreen;
    private WindowState _prevState;
    private WindowStyle _prevStyle;

    private void ToggleFullScreen()
    {
        if (_isFullScreen)
        {
            WindowStyle = _prevStyle;
            WindowState = _prevState;
            _isFullScreen = false;
        }
        else
        {
            _prevStyle = WindowStyle;
            _prevState = WindowState;
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            _isFullScreen = true;
        }
    }

    #endregion

    #region Event Handlers

    private void AddressBar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { Navigate(AddressBar.Text.Trim()); e.Handled = true; }
    }

    private void AddressBar_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => AddressBar.SelectAll();
    private void NewTabBtn_Click(object sender, RoutedEventArgs e) => CreateNewTab();

    private void BackBtn_Click(object sender, RoutedEventArgs e)
    { if (MainWebView.CoreWebView2?.CanGoBack == true) MainWebView.CoreWebView2.GoBack(); }

    private void ForwardBtn_Click(object sender, RoutedEventArgs e)
    { if (MainWebView.CoreWebView2?.CanGoForward == true) MainWebView.CoreWebView2.GoForward(); }

    private void ReloadBtn_Click(object sender, RoutedEventArgs e) => MainWebView.Reload();

    private void HomeBtn_Click(object sender, RoutedEventArgs e) => Navigate(App.Settings.Homepage);

    private void BookmarkBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_activeTabId < 0 || !_tabs.ContainsKey(_activeTabId)) return;
        var tab = _tabs[_activeTabId];
        if (string.IsNullOrEmpty(tab.Url) || tab.Url.StartsWith("about:")) return;
        App.AddBookmark(tab.Title, tab.Url);
        UpdateBookmarksBar();
        StatusText.Text = $"已收藏: {tab.Title}";
    }

    private void FavoritesBtn_Click(object sender, RoutedEventArgs e)
    {
        var win = new FavoritesWindow();
        win.Owner = this;
        win.ShowDialog();
        UpdateBookmarksBar();
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
            BookmarksBarBorder.Visibility = App.Settings.ShowBookmarksBar
                ? Visibility.Visible : Visibility.Collapsed;
            UpdateBookmarksBar();
        }
    }

    private void ZoomInBtn_Click(object sender, RoutedEventArgs e)
    {
        if (MainWebView.ZoomFactor < 5.0)
        {
            MainWebView.ZoomFactor = Math.Min(5.0, MainWebView.ZoomFactor + 0.1);
            ZoomText.Text = $"{(int)(MainWebView.ZoomFactor * 100)}%";
        }
    }

    private void ZoomOutBtn_Click(object sender, RoutedEventArgs e)
    {
        if (MainWebView.ZoomFactor > 0.25)
        {
            MainWebView.ZoomFactor = Math.Max(0.25, MainWebView.ZoomFactor - 0.1);
            ZoomText.Text = $"{(int)(MainWebView.ZoomFactor * 100)}%";
        }
    }

    #endregion
}

public class TabState
{
    public string Url { get; set; } = "";
    public string Title { get; set; } = "新标签页";
}

public class ClosedTab
{
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
}

public class DownloadItem
{
    public string FileName { get; set; } = "";
    public string Url { get; set; } = "";
    public string Status { get; set; } = "";
}
