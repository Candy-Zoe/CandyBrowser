using System;

namespace CandyBrowser.Shared.Abstractions
{
    public interface INavigationService
    {
        event EventHandler<string>? NavigationRequested;
        event EventHandler<string>? TitleChanged;
        event EventHandler<string>? FaviconChanged;
        event EventHandler<bool>? IsLoadingChanged;
        event EventHandler<bool>? CanGoBackChanged;
        event EventHandler<bool>? CanGoForwardChanged;

        void Navigate(string url);
        void GoBack();
        void GoForward();
        void Reload();
        void Stop();
        void GoHome();
        string CurrentUrl { get; }
        string CurrentTitle { get; }
    }
}
