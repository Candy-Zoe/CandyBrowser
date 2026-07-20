using System.IO;
using System.Diagnostics;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CandyBrowser.Shared.Abstractions;
using CandyBrowser.Services.Bookmarks;
using CandyBrowser.Services.Downloads;
using CandyBrowser.Services.Extensions;
using CandyBrowser.Services.History;
using CandyBrowser.Services.Navigation;
using CandyBrowser.Services.PDF;
using CandyBrowser.Services.Passwords;
using CandyBrowser.Services.Reading;
using CandyBrowser.Services.Settings;
using CandyBrowser.Services.Tabs;
using CandyBrowser.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace CandyBrowser.Windows;

public partial class App : System.Windows.Application
{
    private IHost? _host;
    private IServiceScope? _appScope;

    public static IHost Host => GetCurrentHost();
    public static IServiceScope Scope => GetCurrentScope();

    private static IHost GetCurrentHost()
    {
        var app = (App)Current!;
        if (app._host == null)
            throw new InvalidOperationException("App hasn't been started yet.");
        return app._host;
    }

    private static IServiceScope GetCurrentScope()
    {
        var app = (App)Current!;
        if (app._appScope == null)
            throw new InvalidOperationException("App scope hasn't been created yet.");
        return app._appScope;
    }

    public static T Resolve<T>() where T : notnull
    {
        return Scope.ServiceProvider.GetRequiredService<T>();
    }

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices((context, services) =>
            {
                var dataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CandyBrowser");
                Directory.CreateDirectory(dataDir);

                var dbPath = Path.Combine(dataDir, "candybrowser.db");
                services.AddDbContext<BrowserDbContext>(options =>
                    options.UseSqlite($"Data Source={dbPath}"));

                // Register all services as Singleton so they share one DbContext for the app lifetime
                services.AddSingleton<IBookmarkService, BookmarkService>();
                services.AddSingleton<IHistoryService, HistoryService>();
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IDownloadService, DownloadService>();
                services.AddSingleton<IPasswordService, PasswordService>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<ITabManager, TabService>();
                services.AddSingleton<IExtensionService, ExtensionService>();
                services.AddSingleton<IReadingModeService, ReadingModeService>();
                services.AddSingleton<IPdfService, PdfService>();

                // JSON settings provider (backward compat)
                services.AddSingleton<JsonSettingsProvider>();
            })
            .Build();

        _host.Start();

        // Create a single app-wide scope that lives for the entire application lifetime
        _appScope = _host.Services.CreateScope();

        // Initialize database using the app scope
        var db = _appScope.ServiceProvider.GetRequiredService<BrowserDbContext>();
        db.Database.EnsureCreated();

        // Create and show main window via DI
        var sp = _appScope.ServiceProvider;
        var mainWindow = new MainWindow(
            sp.GetRequiredService<IBookmarkService>(),
            sp.GetRequiredService<IHistoryService>(),
            sp.GetRequiredService<IDownloadService>(),
            sp.GetRequiredService<INavigationService>(),
            sp.GetRequiredService<ISettingsService>(),
            sp.GetRequiredService<IPdfService>(),
            sp.GetRequiredService<IReadingModeService>(),
            sp.GetRequiredService<JsonSettingsProvider>());
        mainWindow.Show();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        try
        {
            // Dispose the app scope first (this disposes the DbContext)
            _appScope?.Dispose();
            _appScope = null;
        }
        catch { /* ignore */ }
        finally
        {
            try
            {
                _host?.StopAsync().Wait(TimeSpan.FromSeconds(5));
            }
            catch { /* ignore */ }
            finally
            {
                _host?.Dispose();
            }
        }
        base.OnExit(e);
    }
}

/// <summary>
/// Lightweight JSON-based settings provider for backward compatibility.
/// </summary>
public class JsonSettingsProvider
{
    private static readonly string SettingsPath;

    static JsonSettingsProvider()
    {
        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CandyBrowser");
        Directory.CreateDirectory(dataDir);
        SettingsPath = Path.Combine(dataDir, "settings.json");
    }

    public CandyBrowser.Core.Models.Setting Get(string key, string defaultValue)
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
                if (dict.TryGetValue(key, out var val))
                    return new CandyBrowser.Core.Models.Setting { Key = key, Value = val };
            }
        }
        catch { }
        return new CandyBrowser.Core.Models.Setting { Key = key, Value = defaultValue };
    }

    public void Set(string key, string value)
    {
        Dictionary<string, string> dict;
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            }
            else
            {
                dict = new();
            }
        }
        catch
        {
            dict = new();
        }

        dict[key] = value;
        var jsonOut = System.Text.Json.JsonSerializer.Serialize(dict, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, jsonOut);
    }
}
