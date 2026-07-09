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
                // Database
                services.AddDbContext<BrowserDbContext>(options =>
                    options.UseSqlite("Data Source=candybrowser.db"));

                // Services
                services.AddScoped<IBookmarkService, BookmarkService>();
                services.AddScoped<IHistoryService, HistoryService>();
                services.AddScoped<IPasswordService>(sp =>
                {
                    var db = sp.GetRequiredService<BrowserDbContext>();
                    var masterKey = CryptoService.GetOrCreateMasterKey();
                    return new PasswordService(db, masterKey);
                });
                services.AddScoped<ITabManager, TabService>();
                services.AddScoped<ISettingsService, SettingsService>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddScoped<IExtensionService, ExtensionService>();
                services.AddSingleton<IDownloadService, DownloadService>();

                // ViewModels
                services.AddTransient<ViewModels.MainViewModel>();

                // Windows
                services.AddTransient<MainWindow>();
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
