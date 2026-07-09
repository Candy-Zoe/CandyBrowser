using CandyBrowser.Shared.Abstractions;

namespace CandyBrowser.Services.Navigation;

public class NavigationService : INavigationService
{
    private string _currentUrl = string.Empty;
    private string _currentTitle = string.Empty;
    private bool _canGoBack;
    private bool _canGoForward;
    private readonly Stack<string> _backStack = new();
    private readonly Stack<string> _forwardStack = new();

    public event EventHandler<string>? NavigationRequested;
    public event EventHandler<string>? TitleChanged;
    public event EventHandler<string>? FaviconChanged;
    public event EventHandler<bool>? IsLoadingChanged;
    public event EventHandler<bool>? CanGoBackChanged;
    public event EventHandler<bool>? CanGoForwardChanged;

    public string CurrentUrl => _currentUrl;
    public string CurrentTitle => _currentTitle;

    public void Navigate(string url)
    {
        if (!string.IsNullOrEmpty(_currentUrl))
        {
            _backStack.Push(_currentUrl);
            _canGoBack = true;
            CanGoBackChanged?.Invoke(this, true);
        }

        _forwardStack.Clear();
        _canGoForward = false;
        CanGoForwardChanged?.Invoke(this, false);

        _currentUrl = url;
        IsLoadingChanged?.Invoke(this, true);
        NavigationRequested?.Invoke(this, url);
    }

    public void GoBack()
    {
        if (_backStack.Count == 0) return;

        _forwardStack.Push(_currentUrl);
        _canGoForward = true;
        CanGoForwardChanged?.Invoke(this, true);

        _currentUrl = _backStack.Pop();
        _canGoBack = _backStack.Count > 0;
        CanGoBackChanged?.Invoke(this, _canGoBack);

        NavigationRequested?.Invoke(this, _currentUrl);
    }

    public void GoForward()
    {
        if (_forwardStack.Count == 0) return;

        _backStack.Push(_currentUrl);
        _canGoBack = true;
        CanGoBackChanged?.Invoke(this, true);

        _currentUrl = _forwardStack.Pop();
        _canGoForward = _forwardStack.Count > 0;
        CanGoForwardChanged?.Invoke(this, _canGoForward);

        NavigationRequested?.Invoke(this, _currentUrl);
    }

    public void Reload()
    {
        IsLoadingChanged?.Invoke(this, true);
        NavigationRequested?.Invoke(this, _currentUrl);
    }

    public void Stop()
    {
        IsLoadingChanged?.Invoke(this, false);
    }

    public void GoHome()
    {
        Navigate("about:blank");
    }
}
