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

            WebView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
            WebView.CoreWebView2.ProcessFailed += CoreWebView2_ProcessFailed;

            _isInitialized = true;
            LoadingOverlay.Visibility = Visibility.Collapsed;

            if (!string.IsNullOrEmpty(_pendingUrl))
            {
                WebView.CoreWebView2.Navigate(_pendingUrl);
                _pendingUrl = null;
            }
        }
        catch (Exception ex)
        {
            try { StatusText.Text = $"初始化失败: {ex.Message}"; } catch { }
        }
    }

    private void WebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (_isDisposed) return;
        try
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            LoadingBar.IsIndeterminate = true;
            StatusText.Text = $"正在加载: {e.Uri}";
        }
        catch { }
    }

    private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (_isDisposed) return;

        try
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            LoadingBar.IsIndeterminate = false;
        }
        catch { }

        try
        {
            if (WebView.CoreWebView2 == null) return;

            var url = WebView.Source?.ToString() ?? string.Empty;
            var title = string.Empty;
            try { title = WebView.CoreWebView2.DocumentTitle ?? string.Empty; } catch { }

            StatusText.Text = e.IsSuccess ? "就绪" : "加载失败";

            if (FindParentWindow() is MainWindow mainWindow && mainWindow.DataContext is ViewModels.MainViewModel mainVm)
            {
                mainVm.UpdateCurrentTabInfo(url, title);
            }
        }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
        catch { }
    }

    private void WebView_SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
    {
        if (_isDisposed) return;
    }

    private void WebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        // ignored
    }

    private void CoreWebView2_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        if (_isDisposed) return;
        try
        {
            e.Handled = true;
            if (FindParentWindow() is MainWindow mainWindow && mainWindow.DataContext is ViewModels.MainViewModel mainVm)
            {
                mainVm.CreateNewTabWithUrl(e.Uri);
            }
        }
        catch { }
    }

    private void CoreWebView2_ProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
    {
        if (_isDisposed) return;
        try
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try { StatusText.Text = "页面加载异常"; } catch { }
            }));
        }
        catch { }
    }

    private MainWindow? FindParentWindow()
    {
        try
        {
            var parent = VisualTreeHelper.GetParent(this);
            while (parent != null && parent is not MainWindow)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as MainWindow;
        }
        catch { return null; }
    }

    public void NavigateTo(string url)
    {
        if (_isDisposed || string.IsNullOrEmpty(url)) return;

        try
        {
            if (_isInitialized && WebView.CoreWebView2 != null)
            {
                WebView.CoreWebView2.Navigate(url);
            }
            else
            {
                // WebView2 not ready yet, queue the navigation
                _pendingUrl = url;
            }
        }
        catch (Exception ex)
        {
            try { StatusText.Text = $"导航失败: {ex.Message}"; } catch { }
        }
    }

    public void GoBack()
    {
        try
        {
            if (WebView.CoreWebView2?.CanGoBack == true)
                WebView.CoreWebView2.GoBack();
        }
        catch { }
    }

    public void GoForward()
    {
        try
        {
            if (WebView.CoreWebView2?.CanGoForward == true)
                WebView.CoreWebView2.GoForward();
        }
        catch { }
    }

    public void Refresh()
    {
        try
        {
            if (WebView.CoreWebView2 != null)
                WebView.Reload();
        }
        catch { }
    }

    public void Stop()
    {
        try { WebView.CoreWebView2?.Stop(); } catch { }
    }

    public void OpenDevTools()
    {
        try { WebView.CoreWebView2?.OpenDevToolsWindow(); } catch { }
    }

    private void GoBack_Click(object sender, RoutedEventArgs e) => GoBack();
    private void GoForward_Click(object sender, RoutedEventArgs e) => GoForward();
    private void Refresh_Click(object sender, RoutedEventArgs e) => Refresh();

    private void OpenInNewTab_Click(object sender, RoutedEventArgs e)
    {
        // TODO: get link from context menu target
    }

    private void CopyLink_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var url = WebView.Source?.ToString();
            if (!string.IsNullOrEmpty(url))
            {
                Clipboard.SetText(url);
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
            }
            WebView.Dispose();
        }
        catch { }
    }
}
