using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Diagnostics;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using CandyBrowser.Shared.Abstractions;
using CandyBrowser.Core.Models;
using CandyBrowser.Windows.Views;

namespace CandyBrowser.Windows;

public partial class MainWindow : Window
{
    private readonly IBookmarkService _bookmarkService;
    private readonly IHistoryService _historyService;
    private readonly IDownloadService _downloadService;
    private readonly INavigationService _navigationService;
    private readonly ISettingsService _settingsService;
    private readonly IPdfService _pdfService;
    private readonly IReadingModeService _readingModeService;
    private readonly JsonSettingsProvider _jsonSettings;

    private readonly Dictionary<int, TabState> _tabs = new();
    private readonly List<int> _tabOrder = new();
    private readonly List<ClosedTab> _recentlyClosed = new();
    private int _nextTabId = 1;
    private int _activeTabId = -1;
    private DateTime? _tabStartTime;

    // 标签分组颜色
    private static readonly SolidColorBrush[] GroupColors =
    {
        new(Color.FromRgb(0x4C,0x9C,0xE0)),
        new(Color.FromRgb(0x56,0xB8,0x70)),
        new(Color.FromRgb(0xE8,0x6C,0x60)),
        new(Color.FromRgb(0xF5,0xA6,0x23)),
        new(Color.FromRgb(0x9B,0x59,0xB6)),
        new(Color.FromRgb(0x1A,0xBC,0x9C)),
    };

    // Edge 风格颜色
    private static readonly SolidColorBrush ActiveTabBrush = new(Color.FromRgb(0xFF, 0xFF, 0xFF));
    private static readonly SolidColorBrush InactiveTabBrush = new(Color.FromRgb(0xCC, 0xD0, 0xD6));
    private static readonly SolidColorBrush HoverTabBrush = new(Color.FromRgb(0xD8, 0xDC, 0xE2));
    private static readonly SolidColorBrush TabTextBrush = new(Color.FromRgb(0x33, 0x33, 0x33));

    // 鼠标手势
    private Point _gestureStart;
    private bool _isGesture;

    public MainWindow(
        IBookmarkService bookmarkService,
        IHistoryService historyService,
        IDownloadService downloadService,
        INavigationService navigationService,
        ISettingsService settingsService,
        IPdfService pdfService,
        IReadingModeService readingModeService,
        JsonSettingsProvider jsonSettings)
    {
        InitializeComponent();
        _bookmarkService = bookmarkService;
        _historyService = historyService;
        _downloadService = downloadService;
        _navigationService = navigationService;
        _settingsService = settingsService;
        _pdfService = pdfService;
        _readingModeService = readingModeService;
        _jsonSettings = jsonSettings;
        
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Load settings
            var showBookmarksBar = _jsonSettings.Get("show_bookmarks_bar", "true").Value == "true";
            BookmarksBarBorder.Visibility = showBookmarksBar ? Visibility.Visible : Visibility.Collapsed;

            StatusText.Text = "正在初始化 WebView2...";

            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CandyBrowser", "WebView2_v3");
            Directory.CreateDirectory(userDataFolder);

            var options = new CoreWebView2EnvironmentOptions
            {
                AdditionalBrowserArguments = "--disable-gpu --enable-features=WebView2"
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
                        
                        var url = MainWebView.CoreWebView2?.Source ?? "";
                        StatusText.Text = ev.IsSuccess ? $"就绪 - {title}" : $"加载失败({ev.WebErrorStatus})";
                        RefreshTabStrip();

                        // Persist history entry
                        if (ev.IsSuccess && !url.StartsWith("about:") && !url.StartsWith("edge://"))
                        {
                            var duration = (_tabStartTime.HasValue) 
                                ? (long?)(DateTime.UtcNow - _tabStartTime.Value).TotalMilliseconds 
                                : null;
                            _ = duration.HasValue 
                                ? _historyService.AddAsync(url, title, faviconUrl: null, durationMs: duration.Value)
                                : _historyService.AddAsync(url, title);
                            _tabStartTime = DateTime.UtcNow;
                        }
                    }
                });
            };

            MainWebView.CoreWebView2.NewWindowRequested += (s, ev) =>
            {
                ev.Handled = true;
                Dispatcher.Invoke(() => CreateNewTab(ev.Uri));
            };

            // 下载处理 - 使用服务层
            MainWebView.CoreWebView2.DownloadStarting += (s, ev) =>
            {
                Dispatcher.Invoke(() =>
                {
                    var uri = ev.DownloadOperation.Uri;
                    var fileName = Path.GetFileName(uri) ?? "download";
                    
                    var request = new DownloadRequest
                    {
                        Url = uri,
                        FileName = fileName,
                        FilePath = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                            "Downloads", fileName),
                        ShowSaveDialog = true
                    };

                    // Ask user where to save
                    var dlg = new Microsoft.Win32.SaveFileDialog
                    {
                        Filter = "所有文件|*.*",
                        FileName = fileName,
                        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                    };

                    if (dlg.ShowDialog() == true)
                    {
                        request.FilePath = dlg.FileName;
                        request.ShowSaveDialog = false;
                        _ = _downloadService.StartDownloadAsync(request);
                    }
                    else
                    {
                        ev.DownloadOperation.Cancel();
                    }
                });
            };

            // Listen for download progress events
            _downloadService.DownloadProgressChanged += (s, item) =>
            {
                Dispatcher.Invoke(() =>
                {
                    StatusText.Text = $"下载中: {item.FileName} - {item.ProgressText}";
                    RefreshDownloadList();
                });
            };

            _downloadService.DownloadCompleted += (s, item) =>
            {
                Dispatcher.Invoke(() =>
                {
                    StatusText.Text = $"下载完成: {item.FileName}";
                    RefreshDownloadList();
                });
            };

            _downloadService.DownloadFailed += (s, item) =>
            {
                Dispatcher.Invoke(() =>
                {
                    StatusText.Text = $"下载失败: {item.FileName} - {item.ErrorMessage}";
                    RefreshDownloadList();
                });
            };

            CreateNewTab();
            UpdateBookmarksBar();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"初始化错误: {ex.Message}";
            MessageBox.Show($"WebView2 初始化失败:\n{ex.Message}\n\n请确保已安装 Microsoft Edge WebView2 Runtime。\n\n详情: {ex.InnerException?.Message}", 
                "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #region Tab Management

    private void CreateNewTab(string? url = null)
    {
        var tabId = _nextTabId++;
        var targetUrl = url ?? _jsonSettings.Get("homepage", "https://www.baidu.com").Value;

        _tabs[tabId] = new TabState { Url = targetUrl, Title = "新标签页" };
        _tabOrder.Add(tabId);

        SelectTab(tabId);
        _tabStartTime = DateTime.UtcNow;
        
        if (MainWebView.CoreWebView2 != null)
            MainWebView.CoreWebView2.Navigate(targetUrl);
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
            if (title.Length > 24) title = title[..24] + "\u2026";
            var isActive = tabId == _activeTabId;
            int capturedTabId = tabId;
    
            var outerPanel = new StackPanel { Orientation = Orientation.Horizontal };
    
            if (tab.GroupId >= 0 && tab.GroupId < GroupColors.Length)
            {
                var groupBar = new Border
                {
                    Width = 3, Background = GroupColors[tab.GroupId],
                    CornerRadius = new CornerRadius(2, 0, 0, 0),
                    Margin = new Thickness(0, 2, 0, 0)
                };
                outerPanel.Children.Add(groupBar);
            }
    
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
                Content = "\u2715",
                FontSize = 9,
                Width = 18,
                Height = 18,
                Margin = new Thickness(4, 0, 0, 0),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0)
            };
            closeBtn.Template = CreateCloseButtonTemplate();
    
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
                if (e.ChangedButton == MouseButton.Left && e.ClickCount == 1)
                {
                    SelectTab(capturedTabId);
                    if (_tabs.TryGetValue(capturedTabId, out var t))
                        MainWebView.CoreWebView2?.Navigate(t.Url);
                }
            };
    
            tabBorder.PreviewMouseDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Middle) { e.Handled = true; CloseTab(capturedTabId); }
            };
    
            var ctxMenu = new ContextMenu();
            var groupHeader = new MenuItem { Header = "\u5206\u7EC4\u989C\u8272", IsEnabled = false };
            ctxMenu.Items.Add(groupHeader);
            var noGroup = new MenuItem { Header = "\u65E0\u5206\u7EC4" };
            noGroup.Click += (s, e) => { if (_tabs.ContainsKey(capturedTabId)) { _tabs[capturedTabId].GroupId = -1; RefreshTabStrip(); } };
            ctxMenu.Items.Add(noGroup);
            ctxMenu.Items.Add(new Separator());
            string[] groupNames = { "\u84DD\u8272", "\u7EFF\u8272", "\u7EA2\u8272", "\u6A59\u8272", "\u7D2B\u8272", "\u9752\u8272" };
            for (int g = 0; g < GroupColors.Length; g++)
            {
                int groupId = g;
                var mi = new MenuItem { Header = groupNames[g] };
                mi.Click += (s, e) => { if (_tabs.ContainsKey(capturedTabId)) { _tabs[capturedTabId].GroupId = groupId; RefreshTabStrip(); } };
                ctxMenu.Items.Add(mi);
            }
            ctxMenu.Items.Add(new Separator());
            var closeMi = new MenuItem { Header = "\u5173\u95ED\u6807\u7B7E" };
            closeMi.Click += (s, e) => CloseTab(capturedTabId);
            ctxMenu.Items.Add(closeMi);
            var closeOthersMi = new MenuItem { Header = "\u5173\u95ED\u5176\u4ED6\u6807\u7B7E" };
            closeOthersMi.Click += (s, e) =>
            {
                var toClose = _tabOrder.Where(id => id != capturedTabId).ToList();
                foreach (var id in toClose) CloseTab(id);
            };
            ctxMenu.Items.Add(closeOthersMi);
            tabBorder.ContextMenu = ctxMenu;
    
            outerPanel.Children.Add(tabBorder);
            TabStrip.Children.Add(outerPanel);
        }
    }
    
    private static ControlTemplate CreateCloseButtonTemplate()
    {
        var xaml = @"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                                       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                                       TargetType='Button'>
            <Border x:Name='Bd' Background='{TemplateBinding Background}' CornerRadius='9' Padding='0'>
                <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
            </Border>
            <ControlTemplate.Triggers>
                <Trigger Property='IsMouseOver' Value='True'>
                    <Setter TargetName='Bd' Property='Background' Value='#DD4444'/>
                </Trigger>
            </ControlTemplate.Triggers>
        </ControlTemplate>";
        using var reader = new System.IO.StringReader(xaml);
        using var xmlReader = System.Xml.XmlReader.Create(reader);
        return (ControlTemplate)System.Windows.Markup.XamlReader.Load(xmlReader);
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
                url = string.Format(_jsonSettings.Get("search_engine", "https://www.baidu.com/s?wd={0}").Value, Uri.EscapeDataString(url));
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
            SecurityIcon.Text = "\uE72E";
            SecurityIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x80, 0x00));
            SecurityIcon.ToolTip = "安全连接 (HTTPS)";
        }
        else if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            SecurityIcon.Text = "\uE7BA";
            SecurityIcon.Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0x66, 0x00));
            SecurityIcon.ToolTip = "不安全连接 (HTTP)";
        }
        else
        {
            SecurityIcon.Text = "\uE946";
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
        _ = MainWebView.CoreWebView2?.ExecuteScriptAsync("window.getSelection().removeAllRanges();");
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
        var escaped = text.Replace("'", "\\'").Replace("/", "\\/");
        var js = $@"
            (function() {{
                window.getSelection().removeAllRanges();
                var text = '{escaped}';
                if (!text) return 0;
                var count = 0;
                var body = document.body;
                var regex = new RegExp(text.replace(/[.*+?^${{{{|()\\[\\]\\\\]]/g, '\\\\$&'), 'gi');
                var matches = body.innerText.match(regex);
                count = matches ? matches.length : 0;
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
        
        Task.Delay(300).ContinueWith(_ =>
        {
            Dispatcher.Invoke(async () =>
            {
                if (MainWebView.CoreWebView2 == null) return;
                var escapedForJs = text.Replace("'", "\\'").Replace("/", "\\/");
                var result = await MainWebView.CoreWebView2.ExecuteScriptAsync(
                    $"document.body.innerText.match(/{escapedForJs}/gi)?.length || 0") ?? "0";
                try
                {
                    var count = int.Parse(result.Trim());
                    FindMatchCount.Text = $"{count} 个匹配";
                }
                catch
                {
                    FindMatchCount.Text = result.Trim('"');
                }
            });
        });
    }

    private void FindBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { FindNext(); e.Handled = true; }
        else if (e.Key == Key.Escape) { CloseFindBar(); e.Handled = true; }
    }

    private void FindNext() { /* Already handled in TextChanged */ }
    private void FindPrev() { /* Already handled in TextChanged */ }
    private void FindNext_Click(object sender, RoutedEventArgs e) => FindNext();
    private void FindPrev_Click(object sender, RoutedEventArgs e) => FindPrev();
    private void CloseFindBar_Click(object sender, RoutedEventArgs e) => CloseFindBar();

    #endregion

    #region Downloads

    private void RefreshDownloadList()
    {
        DownloadList.Children.Clear();
        var downloads = _downloadService.GetAllDownloads();
        
        // Show all downloads, sorted by start time (newest first)
        var sortedDownloads = downloads.OrderByDescending(d => d.StartTime).ToList();
        
        // Update count text
        DownloadCountText.Text = $"{sortedDownloads.Count} 项";
        
        // Show "View All" button if more than 5 downloads
        ViewAllBtn.Visibility = sortedDownloads.Count > 5 ? Visibility.Visible : Visibility.Collapsed;
        
        // Show "Open Downloads Folder" button if any completed downloads
        var hasCompleted = sortedDownloads.Any(d => d.Status == DownloadStatus.Completed);
        OpenDownloadsFolderBtn.Visibility = hasCompleted ? Visibility.Visible : Visibility.Collapsed;
        
        foreach (var d in sortedDownloads)
        {
            var border = new Border
            {
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(2, 1, 2, 1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                CornerRadius = new CornerRadius(4)
            };
            
            var panel = new StackPanel();
            
            // File name row
            var namePanel = new DockPanel();
            namePanel.Children.Add(new TextBlock
            {
                Text = d.FileName, FontSize = 12, FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            
            // Status icon and text
            var statusIcon = new TextBlock
            {
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0)
            };
            DockPanel.SetDock(statusIcon, Dock.Right);
            
            var statusText = new TextBlock
            {
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0)
            };
            DockPanel.SetDock(statusText, Dock.Right);
            
            switch (d.Status)
            {
                case DownloadStatus.InProgress:
                    statusIcon.Text = "\uE7BA"; // Download icon
                    statusIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4));
                    statusText.Text = $"{d.ProgressText} - {d.SpeedText}";
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4));
                    break;
                case DownloadStatus.Completed:
                    statusIcon.Text = "\uE73E"; // Checkmark icon
                    statusIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x10, 0x7C, 0x10));
                    statusText.Text = "已完成";
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(0x10, 0x7C, 0x10));
                    break;
                case DownloadStatus.Failed:
                    statusIcon.Text = "\uE77F"; // Error icon
                    statusIcon.Foreground = new SolidColorBrush(Color.FromRgb(0xD1, 0x34, 0x34));
                    statusText.Text = $"失败: {d.ErrorMessage}";
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(0xD1, 0x34, 0x34));
                    break;
                case DownloadStatus.Paused:
                    statusIcon.Text = "\uE71C"; // Pause icon
                    statusIcon.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x8C, 0x00));
                    statusText.Text = "已暂停";
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x8C, 0x00));
                    break;
                case DownloadStatus.Cancelled:
                    statusIcon.Text = "\uE778"; // Cancel icon
                    statusIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0x8E, 0x8E));
                    statusText.Text = "已取消";
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0x8E, 0x8E));
                    break;
                default:
                    statusIcon.Text = "\uE8E9"; // Clock icon
                    statusIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0x8E, 0x8E));
                    statusText.Text = "等待中...";
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0x8E, 0x8E));
                    break;
            }
            
            namePanel.Children.Add(statusIcon);
            namePanel.Children.Add(statusText);
            panel.Children.Add(namePanel);
            
            // Progress bar for in-progress downloads
            if (d.Status == DownloadStatus.InProgress && d.TotalBytes > 0)
            {
                var progressBar = new ProgressBar
                {
                    Value = d.ProgressPercentage,
                    Minimum = 0,
                    Maximum = 100,
                    Height = 4,
                    Margin = new Thickness(0, 4, 0, 0),
                    Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)),
                    Background = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0))
                };
                panel.Children.Add(progressBar);
            }
            
            // Action buttons for completed downloads
            if (d.Status == DownloadStatus.Completed)
            {
                var actionPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 4, 0, 0)
                };
                
                var openFileBtn = new Button
                {
                    Content = "\uE8A7 打开",
                    FontSize = 10,
                    Padding = new Thickness(6, 2, 6, 2),
                    Margin = new Thickness(0, 0, 4, 0),
                    Background = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)),
                    Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand
                };
                openFileBtn.Click += (s, e) => OpenFile(d.FilePath);
                
                var openFolderBtn = new Button
                {
                    Content = "\uE8F4 文件夹",
                    FontSize = 10,
                    Padding = new Thickness(6, 2, 6, 2),
                    Background = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)),
                    Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand
                };
                openFolderBtn.Click += (s, e) => OpenFileFolder(d.FilePath);
                
                actionPanel.Children.Add(openFileBtn);
                actionPanel.Children.Add(openFolderBtn);
                panel.Children.Add(actionPanel);
            }
            
            border.Child = panel;
            DownloadList.Children.Add(border);
        }
    }

    private void OpenFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        else
        {
            MessageBox.Show($"文件不存在: {filePath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenFileFolder(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
        {
            Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true });
        }
        else
        {
            MessageBox.Show($"文件夹不存在: {directory}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenDownloadsFolderBtn_Click(object sender, RoutedEventArgs e)
    {
        var downloadsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");
        if (Directory.Exists(downloadsPath))
        {
            Process.Start(new ProcessStartInfo(downloadsPath) { UseShellExecute = true });
        }
    }

    private void ViewAllBtn_Click(object sender, RoutedEventArgs e)
    {
        // Scroll to the bottom of the download list
        var scrollViewer = FindVisualChild<ScrollViewer>(DownloadPanel);
        if (scrollViewer != null)
        {
            scrollViewer.ScrollToBottom();
        }
    }

    private void DownloadBtn_Click(object sender, RoutedEventArgs e)
    {
        DownloadPanel.Visibility = DownloadPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed : Visibility.Visible;
        if (DownloadPanel.Visibility == Visibility.Visible)
            RefreshDownloadList();
    }

    private void CloseDownloadPanel_Click(object sender, RoutedEventArgs e)
    {
        DownloadPanel.Visibility = Visibility.Collapsed;
    }

    // Make the download panel draggable by title bar
    private void DownloadPanel_TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    #endregion

    #region Bookmarks Bar

    private async void UpdateBookmarksBar()
    {
        BookmarksBar.Children.Clear();
        try
        {
            var bookmarks = await _bookmarkService.GetChildrenAsync(null);
            var webBookmarks = bookmarks.Where(b => !b.IsFolder).Take(20).ToList();
            foreach (var b in webBookmarks)
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
        catch { /* bookmarks bar is non-critical */ }
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
        else if (e.Key == Key.F5 && shift) { MainWebView.Reload(); e.Handled = true; }
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

    private void HomeBtn_Click(object sender, RoutedEventArgs e)
    {
        var homepage = _jsonSettings.Get("homepage", "https://www.baidu.com").Value;
        Navigate(homepage);
    }

    private async void BookmarkBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_activeTabId < 0 || !_tabs.ContainsKey(_activeTabId)) return;
        var tab = _tabs[_activeTabId];
        if (string.IsNullOrEmpty(tab.Url) || tab.Url.StartsWith("about:")) return;

        try
        {
            var bookmark = new Bookmark
            {
                Title = tab.Title,
                Url = tab.Url,
                ParentId = null,
                IsFolder = false,
                Position = 0
            };
            await _bookmarkService.AddAsync(bookmark);
            StatusText.Text = $"已收藏: {tab.Title}";
            UpdateBookmarksBar();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"收藏失败: {ex.Message}";
        }
    }

    private async void FavoritesBtn_Click(object sender, RoutedEventArgs e)
    {
        var win = new FavoritesWindow(_bookmarkService, MainWebView);
        win.Owner = this;
        win.ShowDialog();
        await Task.Delay(100); // allow window to close
        UpdateBookmarksBar();
    }

    private async void HistoryBtn_Click(object sender, RoutedEventArgs e)
    {
        var win = new HistoryWindow(_historyService, MainWebView);
        win.Owner = this;
        win.ShowDialog();
        // When HistoryWindow closes, if DialogResult is true, refresh the current page
        if (win.DialogResult == true)
        {
            // The history window navigated to a URL, so reload the current tab
            if (_activeTabId >= 0 && _tabs.TryGetValue(_activeTabId, out var tab))
            {
                MainWebView.CoreWebView2?.Navigate(tab.Url);
            }
        }
    }

    private async void SettingsBtn_Click(object sender, RoutedEventArgs e)
    {
        var win = new SettingsWindow(_settingsService, _jsonSettings);
        win.Owner = this;
        if (win.ShowDialog() == true)
        {
            var showBookmarksBar = _jsonSettings.Get("show_bookmarks_bar", "true").Value == "true";
            BookmarksBarBorder.Visibility = showBookmarksBar ? Visibility.Visible : Visibility.Collapsed;
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

    #region Helper Methods

    private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
                return typedChild;
            var result = FindVisualChild<T>(child);
            if (result != null)
                return result;
        }
        return null;
    }

    #endregion
}

public class TabState
{
    public string Url { get; set; } = "";
    public string Title { get; set; } = "新标签页";
    public int GroupId { get; set; } = -1;
}

public class ClosedTab
{
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
}
