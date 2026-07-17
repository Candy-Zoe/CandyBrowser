using System.Windows;
using System.Text.Json;
using System.IO;
using System.Diagnostics;

namespace CandyBrowser.Windows;

public partial class App : Application
{
    private static readonly string AppDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CandyBrowser");

    private static readonly string SettingsPath = Path.Combine(AppDir, "settings.json");
    private static readonly string BookmarksPath = Path.Combine(AppDir, "bookmarks.json");
    private static readonly string HistoryPath = Path.Combine(AppDir, "history.json");

    private static AppSettings _settings = new();
    private static List<BookmarkItem> _bookmarks = new();
    private static List<HistoryItem> _history = new();

    public static AppSettings Settings => _settings;
    public static List<BookmarkItem> Bookmarks => _bookmarks;
    public static List<HistoryItem> History => _history;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Create directory
            Directory.CreateDirectory(AppDir);
            Debug.WriteLine($"[App] Data directory created: {AppDir}");

            // Load or create settings
            LoadSettings();
            SaveSettings(); // Ensure file exists

            // Load or create bookmarks
            LoadBookmarks();
            SaveBookmarks(); // Ensure file exists

            // Load or create history
            LoadHistory();
            SaveHistory(); // Ensure file exists

            Debug.WriteLine($"[App] Data loaded: {Bookmarks.Count} bookmarks, {History.Count} history");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] Startup error: {ex.Message}");
            MessageBox.Show($"启动错误: {ex.Message}", "错误");
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            SaveSettings();
            SaveBookmarks();
            SaveHistory();
            Debug.WriteLine("[App] Data saved on exit");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] Exit error: {ex.Message}");
        }
        base.OnExit(e);
    }

    #region Settings
    public static void LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                if (!string.IsNullOrEmpty(json))
                    _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Settings] Load error: {ex.Message}");
            _settings = new AppSettings();
        }
    }

    public static void SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
            Debug.WriteLine($"[Settings] Saved to {SettingsPath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Settings] Save error: {ex.Message}");
        }
    }
    #endregion

    #region Bookmarks
    public static void LoadBookmarks()
    {
        try
        {
            if (File.Exists(BookmarksPath))
            {
                var json = File.ReadAllText(BookmarksPath);
                if (!string.IsNullOrEmpty(json))
                    _bookmarks = JsonSerializer.Deserialize<List<BookmarkItem>>(json) ?? new();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Bookmarks] Load error: {ex.Message}");
            _bookmarks = new();
        }
    }

    public static void SaveBookmarks()
    {
        try
        {
            var json = JsonSerializer.Serialize(_bookmarks, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(BookmarksPath, json);
            Debug.WriteLine($"[Bookmarks] Saved {_bookmarks.Count} items to {BookmarksPath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Bookmarks] Save error: {ex.Message}");
        }
    }

    public static void AddBookmark(string title, string url, long? parentId = null)
    {
        _bookmarks.Add(new BookmarkItem
        {
            Id = _bookmarks.Count > 0 ? _bookmarks.Max(b => b.Id) + 1 : 1,
            Title = title,
            Url = url,
            ParentId = parentId,
            CreatedAt = DateTime.Now
        });
        SaveBookmarks();
        Debug.WriteLine($"[Bookmarks] Added: {title} - {url}");
    }

    public static void AddFolder(string name, long? parentId = null)
    {
        _bookmarks.Add(new BookmarkItem
        {
            Id = _bookmarks.Count > 0 ? _bookmarks.Max(b => b.Id) + 1 : 1,
            Title = name,
            Url = "",
            IsFolder = true,
            ParentId = parentId,
            CreatedAt = DateTime.Now
        });
        SaveBookmarks();
    }

    public static void DeleteBookmark(long id)
    {
        var item = _bookmarks.FirstOrDefault(b => b.Id == id);
        if (item != null)
        {
            var children = _bookmarks.Where(b => b.ParentId == id).ToList();
            foreach (var child in children) DeleteBookmark(child.Id);
            _bookmarks.Remove(item);
            SaveBookmarks();
        }
    }

    public static List<BookmarkItem> GetBookmarksByParent(long? parentId)
    {
        return _bookmarks.Where(b => b.ParentId == parentId).OrderBy(b => b.Title).ToList();
    }
    #endregion

    #region History
    public static void LoadHistory()
    {
        try
        {
            if (File.Exists(HistoryPath))
            {
                var json = File.ReadAllText(HistoryPath);
                if (!string.IsNullOrEmpty(json))
                    _history = JsonSerializer.Deserialize<List<HistoryItem>>(json) ?? new();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[History] Load error: {ex.Message}");
            _history = new();
        }
    }

    public static void SaveHistory()
    {
        try
        {
            if (_history.Count > 1000) _history = _history.TakeLast(1000).ToList();
            var json = JsonSerializer.Serialize(_history, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(HistoryPath, json);
            Debug.WriteLine($"[History] Saved {_history.Count} items to {HistoryPath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[History] Save error: {ex.Message}");
        }
    }

    public static void AddHistory(string url, string title)
    {
        if (string.IsNullOrEmpty(url) || url.StartsWith("about:")) return;

        _history.RemoveAll(h => h.Url == url);
        _history.Insert(0, new HistoryItem { Url = url, Title = title, VisitedAt = DateTime.Now });
        SaveHistory();
        Debug.WriteLine($"[History] Added: {url} - {title}");
    }

    public static void ClearHistory()
    {
        _history.Clear();
        SaveHistory();
    }
    #endregion
}

public class AppSettings
{
    public string Homepage { get; set; } = "https://www.baidu.com";
    public string SearchEngine { get; set; } = "https://www.baidu.com/s?wd={0}";
    public bool ShowBookmarksBar { get; set; } = true;
    public bool RestoreOnStartup { get; set; } = false;
    public string Theme { get; set; } = "light";
    public bool DoNotTrack { get; set; } = false;
    public bool BlockPopups { get; set; } = true;
    public bool ClearOnExit { get; set; } = false;
}

public class BookmarkItem
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public bool IsFolder { get; set; }
    public long? ParentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class HistoryItem
{
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public DateTime VisitedAt { get; set; } = DateTime.Now;
}
