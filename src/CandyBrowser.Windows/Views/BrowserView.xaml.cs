using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using CandyBrowser.Shared.Abstractions;
using CandyBrowser.Core.Models;

namespace CandyBrowser.Windows.Views;

public partial class BrowserView : UserControl
{
    private bool _isInitialized;
    private string? _pendingUrl;
    private IDownloadService? _downloadService;
    private bool _isDisposed;

    // Shared WebView2 environment for all instances
    private static CoreWebView2Environment? _sharedEnvironment;
    private static readonly object _envLock = new();

    public BrowserView()
    {
        InitializeComponent();
        Loaded += BrowserView_Loaded;
    }

    public void SetDownloadService(IDownloadService downloadService)
    {
        _downloadService = downloadService;
    }

    private static async Task<CoreWebView2Environment> GetSharedEnvironmentAsync()
    {
        if (_sharedEnvironment != null) return _sharedEnvironment;

        lock (_envLock)
        {
            if (_sharedEnvironment != null) return _sharedEnvironment;
        }

        var env = await CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CandyBrowser", "WebView2Data"),
            options: new CoreWebView2EnvironmentOptions());

        lock (_envLock)
        {
            _sharedEnvironment = env;
        }

        return env;
    }

    private async void BrowserView_Loaded(object sender, RoutedEventArgs e)
    {
        await InitializeWebViewAsync();
    }

    private async Task InitializeWebViewAsync()
    {
        try
        {
            var env = await GetSharedEnvironmentAsync();
            await WebView.EnsureCoreWebView2Async(env);

            WebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            WebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
            WebView.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = true;
            WebView.CoreWebView2.Settings.IsZoomControlEnabled = true;
            WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;

            _isInitialized = true;
            LoadingOverlay.Visibility = Visibility.Collapsed;

            // 如果有等待的 URL，现在导航
            if (!string.IsNullOrEmpty(_pendingUrl))
            {
                WebView.CoreWebView2.Navigate(_pendingUrl);
                _pendingUrl = null;
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"初始化失败: {ex.Message}";
        }
    }

    private void WebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        LoadingOverlay.Visibility = Visibility.Visible;
        LoadingBar.IsIndeterminate = true;
        StatusText.Text = $"正在加载: {e.Uri}";

        if (DataContext is ViewModels.TabViewModel tabVm)
        {
            tabVm.Url = e.Uri;
        }
    }

    private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (_isDisposed) return;

        try
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            LoadingBar.IsIndeterminate = false;

            if (e.IsSuccess)
            {
                StatusText.Text = "就绪";
            }
            else
            {
                StatusText.Text = $"加载失败: {e.WebErrorStatus}";
            }

            if (WebView.CoreWebView2 == null) return;

            var url = WebView.Source?.ToString() ?? string.Empty;
            var title = WebView.CoreWebView2.DocumentTitle ?? string.Empty;

            if (DataContext is ViewModels.TabViewModel tabVm)
            {
                tabVm.Title = title;
                tabVm.Url = url;
            }

            if (FindParentWindow() is MainWindow mainWindow)
            {
                var mainVm = mainWindow.DataContext as ViewModels.MainViewModel;
                mainVm?.UpdateCurrentTabInfo(url, title);
            }
        }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
    }

    private void WebView_SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
    {
        if (_isDisposed) return;
        try
        {
            if (DataContext is ViewModels.TabViewModel tabVm)
            {
                tabVm.Url = WebView.Source?.ToString() ?? string.Empty;
            }
        }
        catch { }
    }

    private void WebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var message = e.WebMessageAsJson;
        Debug.WriteLine($"WebView2 message: {message}");
    }

    private void WebView_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
        {
            StatusText.Text = $"WebView2 初始化失败: {e.InitializationException?.Message}";
            return;
        }

        WebView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
        WebView.CoreWebView2.ProcessFailed += CoreWebView2_ProcessFailed;
        WebView.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;
    }

    private void CoreWebView2_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        if (_isDisposed) return;

        try
        {
            e.Handled = true;

            if (FindParentWindow() is MainWindow mainWindow)
            {
                var mainVm = mainWindow.DataContext as ViewModels.MainViewModel;
                mainVm?.CreateNewTabWithUrl(e.Uri);
            }
        }
        catch { }
    }

    private void CoreWebView2_ProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
    {
        if (_isDisposed) return;

        Dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                StatusText.Text = $"进程异常: {e.Reason}";
            }
            catch { }
        }));
    }

    private async void CoreWebView2_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
    {
        var downloadOperation = e.DownloadOperation;
        var uri = downloadOperation.Uri;
        var suggestedFileName = Path.GetFileName(new Uri(uri).LocalPath);

        if (string.IsNullOrEmpty(suggestedFileName))
        {
            suggestedFileName = "download";
        }

        var saveDialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = suggestedFileName,
            Filter = "所有文件|*.*"
        };

        if (saveDialog.ShowDialog() == true)
        {
            e.ResultFilePath = saveDialog.FileName;
            StatusText.Text = $"正在下载: {suggestedFileName}";
        }
        else
        {
            e.Cancel = true;
        }
    }

    private MainWindow? FindParentWindow()
    {
        var parent = VisualTreeHelper.GetParent(this);
        while (parent != null && parent is not MainWindow)
        {
            parent = VisualTreeHelper.GetParent(parent);
        }
        return parent as MainWindow;
    }

    public void NavigateTo(string url)
    {
        if (_isInitialized && WebView.CoreWebView2 != null)
        {
            WebView.CoreWebView2.Navigate(url);
        }
        else
        {
            // 保存 URL，等 WebView2 初始化后再导航
            _pendingUrl = url;
        }
    }

    public void GoBack()
    {
        if (WebView.CoreWebView2?.CanGoBack == true)
        {
            WebView.CoreWebView2.GoBack();
        }
    }

    public void GoForward()
    {
        if (WebView.CoreWebView2?.CanGoForward == true)
        {
            WebView.CoreWebView2.GoForward();
        }
    }

    public void Refresh()
    {
        WebView.Reload();
    }

    public void Stop()
    {
        WebView.CoreWebView2?.Stop();
    }

    public void OpenDevTools()
    {
        WebView.CoreWebView2?.OpenDevToolsWindow();
    }

    private void GoBack_Click(object sender, RoutedEventArgs e) => GoBack();
    private void GoForward_Click(object sender, RoutedEventArgs e) => GoForward();
    private void Refresh_Click(object sender, RoutedEventArgs e) => Refresh();

    private void OpenInNewTab_Click(object sender, RoutedEventArgs e) { }
    private void CopyLink_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!string.IsNullOrEmpty(WebView.Source?.ToString()))
            {
                Clipboard.SetText(WebView.Source.ToString());
                StatusText.Text = "链接已复制";
            }
        }
        catch { }
    }
    private void DevTools_Click(object sender, RoutedEventArgs e) => OpenDevTools();

    public void Dispose()
    {
        _isDisposed = true;
        try
        {
            if (WebView.CoreWebView2 != null)
            {
                WebView.CoreWebView2.NewWindowRequested -= CoreWebView2_NewWindowRequested;
                WebView.CoreWebView2.ProcessFailed -= CoreWebView2_ProcessFailed;
                WebView.CoreWebView2.DownloadStarting -= CoreWebView2_DownloadStarting;
            }
            WebView.Dispose();
        }
        catch { }
    }
}
