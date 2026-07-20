using System.Diagnostics;
using CandyBrowser.Shared.Abstractions;

namespace CandyBrowser.Windows.Services;

public class IncognitoWindowService : IIncognitoWindowService
{
    private readonly string _executablePath;

    public IncognitoWindowService(string executablePath) { _executablePath = executablePath; }

    public void LaunchNewWindow()
    {
        try { Process.Start(new ProcessStartInfo { FileName = _executablePath, UseShellExecute = true }); }
        catch { }
    }

    public void LaunchIncognitoWindow()
    {
        try { Process.Start(new ProcessStartInfo { FileName = _executablePath, Arguments = "--incognito", UseShellExecute = true }); }
        catch { }
    }
}
