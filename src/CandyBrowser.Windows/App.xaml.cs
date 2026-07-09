using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using CandyBrowser.Data.Contexts;
using CandyBrowser.Shared.Abstractions;
using CandyBrowser.Services.Bookmarks;
using CandyBrowser.Services.History;
using CandyBrowser.Services.Passwords;
using CandyBrowser.Services.Tabs;
using CandyBrowser.Services.Settings;
using CandyBrowser.Services.Navigation;
using CandyBrowser.Services.Extensions;
using CandyBrowser.Services.Downloads;
using CandyBrowser.Windows.Services;

namespace CandyBrowser.Windows;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Database - use absolute path in AppData
                var dbPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CandyBrowser", "candybrowser.db");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dbPath)!);
                services.AddDbContext<BrowserDbContext>(options =>
                    options.UseSqlite($"Data Source={dbPath}"),
                    ServiceLifetime.Singleton);

                // Services - use Singleton to avoid scope issues
                services.AddSingleton<IBookmarkService, BookmarkService>();
                services.AddSingleton<IHistoryService, HistoryService>();
                services.AddSingleton<IPasswordService>(sp =>
                {
                    var db = sp.GetRequiredService<BrowserDbContext>();
                    var masterKey = CryptoService.GetOrCreateMasterKey();
                    return new PasswordService(db, masterKey);
                });
                services.AddSingleton<ITabManager, TabService>();
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IExtensionService, ExtensionService>();
                services.AddSingleton<IDownloadService, DownloadService>();

                // ViewModels
                services.AddTransient<ViewModels.MainViewModel>();

                // Windows
                services.AddTransient<MainWindow>(sp =>
                {
                    var vm = sp.GetRequiredService<ViewModels.MainViewModel>();
                    var bookmarkService = sp.GetRequiredService<IBookmarkService>();
                    var downloadService = sp.GetRequiredService<IDownloadService>();
                    var historyService = sp.GetRequiredService<IHistoryService>();
                    return new MainWindow(vm, bookmarkService, downloadService, historyService);
                });
            })
            .Build();
    }

    public static T GetService<T>() where T : class
    {
        var app = (App)Current;
        return app._host.Services.GetRequiredService<T>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        // Initialize database
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BrowserDbContext>();
        await db.Database.EnsureCreatedAsync();

        // Create and show main window
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        using var scope = _host.Services.CreateScope();
        var tabManager = scope.ServiceProvider.GetRequiredService<ITabManager>();
        await tabManager.SaveStateAsync("default");

        await _host.StopAsync();
        _host.Dispose();

        base.OnExit(e);
    }
}
