namespace CandyBrowser.Shared.Abstractions;

/// <summary>
/// Service for launching new/incognito browser windows.
/// </summary>
public interface IIncognitoWindowService
{
    void LaunchNewWindow();
    void LaunchIncognitoWindow();
}
