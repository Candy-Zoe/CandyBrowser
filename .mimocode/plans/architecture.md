# CandyZoe浏览器 - Comprehensive Architecture Plan

## Overview

CandyZoe浏览器 is a cross-platform browser targeting Windows Desktop (WPF + .NET 8 + WebView2) and Android (Kotlin + Jetpack Compose + Android WebView). The architecture is designed for incremental delivery across 5 phases.

---

## 1. Solution Structure

### Solution Layout

```
CandyBrowser/
├── CandyBrowser.sln
├── Directory.Build.props                    # Shared MSBuild props (version, analyzers)
├── Directory.Build.targets
├── .editorconfig
├── .gitignore
│
├── src/
│   ├── CandyBrowser.Core/                   # Shared .NET standard library
│   │   ├── CandyBrowser.Core.csproj
│   │   ├── Models/
│   │   ├── Services/
│   │   ├── Interfaces/
│   │   └── Enums/
│   │
│   ├── CandyBrowser.Data/                   # EF Core / SQLite data layer
│   │   ├── CandyBrowser.Data.csproj
│   │   ├── Contexts/
│   │   ├── Entities/
│   │   ├── Repositories/
│   │   ├── Migrations/
│   │   └── Configurations/
│   │
│   ├── CandyBrowser.Services/               # Business logic services
│   │   ├── CandyBrowser.Services.csproj
│   │   ├── Navigation/
│   │   ├── Bookmarks/
│   │   ├── History/
│   │   ├── Passwords/
│   │   ├── Extensions/
│   │   ├── Tabs/
│   │   ├── PDF/
│   │   ├── Reading/
│   │   └── Settings/
│   │
│   ├── CandyBrowser.Windows/                # WPF Desktop App
│   │   ├── CandyBrowser.Windows.csproj
│   │   ├── App.xaml
│   │   ├── App.xaml.cs
│   │   ├── MainWindow.xaml
│   │   ├── MainWindow.xaml.cs
│   │   ├── Views/
│   │   ├── ViewModels/
│   │   ├── Controls/
│   │   ├── Converters/
│   │   ├── Services/
│   │   ├── Themes/
│   │   └── Resources/
│   │
│   ├── CandyBrowser.Android/                # Android App
│   │   ├── CandyBrowser.Android.csproj (or build.gradle.kts)
│   │   ├── MainActivity.kt
│   │   ├── ui/
│   │   │   ├── screens/
│   │   │   ├── components/
│   │   │   ├── theme/
│   │   │   └── navigation/
│   │   ├── viewmodels/
│   │   ├── data/
│   │   │   ├── dao/
│   │   │   ├── entities/
│   │   │   ├── database/
│   │   │   └── repository/
│   │   ├── services/
│   │   └── di/
│   │
│   └── CandyBrowser.Shared.Abstractions/    # Shared interfaces (netstandard2.0)
│       ├── CandyBrowser.Shared.Abstractions.csproj
│       ├── IBookmarkService.cs
│       ├── IHistoryService.cs
│       ├── IPasswordService.cs
│       ├── IExtensionService.cs
│       ├── ITabManager.cs
│       ├── INavigationService.cs
│       ├── IPdfService.cs
│       ├── IReadingModeService.cs
│       ├── ISettingsService.cs
│       └── Models/
│
├── tests/
│   ├── CandyBrowser.Core.Tests/
│   ├── CandyBrowser.Services.Tests/
│   └── CandyBrowser.Data.Tests/
│
└── docs/
    └── architecture.md
```

### Project References

```
CandyBrowser.Windows ──→ CandyBrowser.Services ──→ CandyBrowser.Data ──→ CandyBrowser.Core
                              │                                                ↑
                              └──────────────────────────────────────────────────┘
                         CandyBrowser.Windows ──→ CandyBrowser.Shared.Abstractions
                         CandyBrowser.Services ──→ CandyBrowser.Shared.Abstractions

CandyBrowser.Data ──→ CandyBrowser.Shared.Abstractions
```

### .csproj Details

**CandyBrowser.Core.csproj** — net8.0 class library
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>CandyBrowser.Core</RootNamespace>
  </PropertyGroup>
</Project>
```

**CandyBrowser.Data.csproj** — net8.0 with EF Core + SQLite
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>CandyBrowser.Data</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.*" />
  </ItemGroup>
</Project>
```

**CandyBrowser.Windows.csproj** — net8.0-windows WPF
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <OutputType>WinExe</OutputType>
    <UseWPF>true</UseWPF>
    <ApplicationIcon>Resources\candybrowser.ico</ApplicationIcon>
    <AssemblyName>CandyBrowser</AssemblyName>
    <RootNamespace>CandyBrowser.Windows</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Web.WebView2" Version="1.0.*" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.*" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.*" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.*" />
  </ItemGroup>
</Project>
```

---

## 2. Shared Code Strategy

### What is shared (via CandyBrowser.Core + CandyBrowser.Data):
- **Database schema**: SQLite tables via EF Core entities (Windows) and Room entities (Android) — same schema, different ORMs
- **Models/DTOs**: Bookmark, HistoryEntry, PasswordEntry, TabState, ExtensionManifest, Settings — plain C# POCOs in Core
- **Business logic interfaces**: Defined in Shared.Abstractions, implemented per platform
- **Search engine config**: JSON config files for search engine URLs

### What is NOT shared:
- UI layer (WPF vs Jetpack Compose)
- WebView management (WebView2 vs Android WebView)
- Platform-specific navigation
- Extension host (WebView2 extension API vs Android WebView.addJavascriptInterface)

### Android Strategy:
Android uses Kotlin natively. The shared schema/contract is defined in `Shared.Abstractions` as documentation; Android implements equivalent Kotlin data classes and Room entities that mirror the EF Core schema exactly. A schema comparison test can validate both sides match.

---

## 3. Windows WPF Project Structure (MVVM)

### Architecture Pattern: MVVM with CommunityToolkit.Mvvm

```
CandyBrowser.Windows/
├── App.xaml / App.xaml.cs                    # DI container setup, app lifecycle
├── MainWindow.xaml                           # Main browser window with tab strip
├│
├── Views/
│   ├── BrowserView.xaml                      # Main tab content (WebView2 + toolbar)
│   ├── BookmarkSidebar.xaml                  # Bookmark tree panel
│   ├── HistoryPanel.xaml                     # History list/search
│   ├── SettingsWindow.xaml                   # Settings dialog
│   ├── ExtensionManagerWindow.xaml           # Extension management
│   ├── PasswordManagerWindow.xaml            # Password vault
│   ├── DevToolsPanel.xaml                    # DevTools host
│   └── ReadingModeView.xaml                  # Clean reading view
│
├── ViewModels/
│   ├── MainViewModel.cs                      # Tab management, window state
│   ├── BrowserTabViewModel.cs                # Per-tab: URL, title, can-go-back
│   ├── AddressBarViewModel.cs                # URL input, autocomplete, search
│   ├── BookmarkViewModel.cs                  # Bookmark CRUD, tree structure
│   ├── HistoryViewModel.cs                   # History list, search, delete
│   ├── SettingsViewModel.cs                  # Settings read/write
│   ├── ExtensionManagerViewModel.cs          # Extension list, enable/disable
│   ├── PasswordManagerViewModel.cs           # Password CRUD, autofill
│   └── ReadingModeViewModel.cs              # Extracted content display
│
├── Controls/
│   ├── BrowserTabHost.xaml                   # WebView2 wrapper per tab
│   ├── AddressBar.xaml                       # Auto-suggest address bar
│   ├── NavigationButtons.xaml                # Back/Forward/Refresh/Home
│   ├── TabStrip.xaml                         # Horizontal tab list
│   ├── TabItem.xaml                          # Single tab control
│   ├── BookmarkTreeView.xaml                 # Tree view for bookmarks
│   ├── SearchBox.xaml                        # Search with suggestions
│   └── WebView2Tab.xaml                      # WebView2 lifecycle wrapper
│
├── Converters/
│   ├── BoolToVisibilityConverter.cs
│   ├── UrlToIconConverter.cs
│   ├── NullToVisibilityConverter.cs
│   └── BoolToInverseConverter.cs
│
├── Services/
│   ├── WpfNavigationService.cs               # INavigationService impl
│   ├── WpfBookmarkService.cs                 # IBookmarkService impl
│   ├── WpfHistoryService.cs                  # IHistoryService impl
│   ├── WpfPasswordService.cs                 # IPasswordService impl
│   ├── WpfExtensionService.cs                # IExtensionService impl
│   ├── WpfPdfService.cs                      # IPdfService impl
│   ├── WpfReadingModeService.cs              # IReadingModeService impl
│   ├── WpfSettingsService.cs                 # ISettingsService impl
│   ├── WpfTabManager.cs                      # ITabManager impl
│   ├── WebView2Manager.cs                    # WebView2 environment + lifecycle
│   └── NativeThemeService.cs                 # Light/dark theme sync
│
├── Themes/
│   ├── LightTheme.xaml                       # ResourceDictionary
│   ├── DarkTheme.xaml
│   ├── CandyBrowser.xaml                     # Base styles
│   └── ControlStyles/
│       ├── TabStrip.Styles.xaml
│       ├── AddressBar.Styles.xaml
│       └── Menu.Styles.xaml
│
└── Resources/
    ├── Icons/                                # App icons, toolbar icons
    ├── Images/
    ├── Fonts/
    └── candybrowser.ico
```

### Key ViewModel Pattern (CommunityToolkit.Mvvm)

```csharp
// Example: MainViewModel.cs
public partial class MainViewModel : ObservableObject
{
    private readonly ITabManager _tabManager;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private ObservableCollection<BrowserTabViewModel> _tabs = new();

    [ObservableProperty]
    private BrowserTabViewModel? _activeTab;

    [RelayCommand]
    private void NewTab() { /* create tab */ }

    [RelayCommand]
    private void CloseTab(BrowserTabViewModel tab) { /* close tab */ }
}
```

### DI Registration (App.xaml.cs)

```csharp
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((ctx, services) =>
            {
                // Core & Data
                services.AddDbContext<CandyBrowserDbContext>(o =>
                    o.UseSqlite("Data Source=candybrowser.db"));
                services.AddScoped<IBookmarkRepository, BookmarkRepository>();
                services.AddScoped<IHistoryRepository, HistoryRepository>();

                // Services
                services.AddScoped<IBookmarkService, WpfBookmarkService>();
                services.AddScoped<IHistoryService, WpfHistoryService>();
                services.AddScoped<IPasswordService, WpfPasswordService>();
                services.AddScoped<ISettingsService, WpfSettingsService>();
                services.AddSingleton<ITabManager, WpfTabManager>();
                services.AddSingleton<INavigationService, WpfNavigationService>();
                services.AddSingleton<WebView2Manager>();

                // ViewModels
                services.AddTransient<MainViewModel>();
                services.AddTransient<BrowserTabViewModel>();
                services.AddTransient<SettingsViewModel>();
            })
            .Build();

        var mainWindow = new MainWindow
        {
            DataContext = host.Services.GetRequiredService<MainViewModel>()
        };
        mainWindow.Show();
    }
}
```

---

## 4. Android Project Structure

### Architecture: MVVM + Repository Pattern + Jetpack Compose

```
CandyBrowser.Android/
├── app/
│   ├── build.gradle.kts
│   ├── src/main/
│   │   ├── AndroidManifest.xml
│   │   ├── java/com/candybrowser/
│   │   │   ├── CandyBrowserApp.kt              # Application class, Hilt init
│   │   │   ├── MainActivity.kt                  # Single-activity, NavHost
│   │   │   │
│   │   │   ├── ui/
│   │   │   │   ├── screens/
│   │   │   │   │   ├── BrowserScreen.kt         # Main tab + WebView
│   │   │   │   │   ├── TabOverviewScreen.kt     # Tab grid/stack view
│   │   │   │   │   ├── BookmarkScreen.kt        # Bookmark list
│   │   │   │   │   ├── HistoryScreen.kt         # History list
│   │   │   │   │   ├── SettingsScreen.kt        # Settings
│   │   │   │   │   ├── PasswordScreen.kt        # Password manager
│   │   │   │   │   ├── ReadingModeScreen.kt     # Reading mode
│   │   │   │   │   └── ExtensionScreen.kt       # Extension management
│   │   │   │   │
│   │   │   │   ├── components/
│   │   │   │   │   ├── AddressBar.kt             # URL input + autocomplete
│   │   │   │   │   ├── TabStrip.kt              # Horizontal scrollable tabs
│   │   │   │   │   ├── TabItem.kt               # Single tab chip
│   │   │   │   │   ├── NavigationBar.kt         # Back/Forward/Refresh
│   │   │   │   │   ├── BookmarkTree.kt          # Bookmark folder tree
│   │   │   │   │   ├── WebViewContainer.kt      # AndroidWebView wrapper
│   │   │   │   │   └── BottomSheet.kt           # Reusable bottom sheet
│   │   │   │   │
│   │   │   │   ├── theme/
│   │   │   │   │   ├── Color.kt
│   │   │   │   │   ├── Theme.kt
│   │   │   │   │   └── Type.kt
│   │   │   │   │
│   │   │   │   └── navigation/
│   │   │   │       ├── NavGraph.kt
│   │   │   │       └── Screen.kt               # Sealed class of routes
│   │   │   │
│   │   │   ├── viewmodels/
│   │   │   │   ├── BrowserViewModel.kt
│   │   │   │   ├── BookmarkViewModel.kt
│   │   │   │   ├── HistoryViewModel.kt
│   │   │   │   ├── SettingsViewModel.kt
│   │   │   │   ├── PasswordViewModel.kt
│   │   │   │   ├── ExtensionViewModel.kt
│   │   │   │   └── ReadingModeViewModel.kt
│   │   │   │
│   │   │   ├── data/
│   │   │   │   ├── database/
│   │   │   │   │   └── CandyBrowserDatabase.kt   # Room database
│   │   │   │   ├── dao/
│   │   │   │   │   ├── BookmarkDao.kt
│   │   │   │   │   ├── HistoryDao.kt
│   │   │   │   │   ├── PasswordDao.kt
│   │   │   │   │   ├── TabDao.kt
│   │   │   │   │   ├── ExtensionDao.kt
│   │   │   │   │   └── SettingsDao.kt
│   │   │   │   ├── entities/
│   │   │   │   │   ├── BookmarkEntity.kt
│   │   │   │   │   ├── HistoryEntity.kt
│   │   │   │   │   ├── PasswordEntity.kt
│   │   │   │   │   ├── TabEntity.kt
│   │   │   │   │   ├── ExtensionEntity.kt
│   │   │   │   │   └── SettingsEntity.kt
│   │   │   │   └── repository/
│   │   │   │       ├── BookmarkRepository.kt
│   │   │   │       ├── HistoryRepository.kt
│   │   │   │       ├── PasswordRepository.kt
│   │   │   │       ├── TabRepository.kt
│   │   │   │       └── ExtensionRepository.kt
│   │   │   │
│   │   │   ├── services/
│   │   │   │   ├── NavigationService.kt
│   │   │   │   ├── PdfService.kt
│   │   │   │   ├── ReadingModeService.kt
│   │   │   │   ├── ExtensionService.kt
│   │   │   │   ├── AutocompleteService.kt
│   │   │   │   └── PasswordAutofillService.kt
│   │   │   │
│   │   │   └── di/
│   │   │       ├── AppModule.kt                 # Hilt app module
│   │   │       ├── DatabaseModule.kt
│   │   │       └── RepositoryModule.kt
│   │   │
│   │   └── res/
│   │       ├── layout/
│   │       ├── values/
│   │       ├── drawable/
│   │       └── xml/
│   │
│   └── build.gradle.kts
│
└── gradle/
    └── libs.versions.toml                      # Version catalog
```

### Key Dependencies (Android build.gradle.kts)

```kotlin
dependencies {
    // Compose
    implementation(platform("androidx.compose:compose-bom:2024.02.00"))
    implementation("androidx.compose.material3:material3")
    implementation("androidx.compose.ui:ui")
    implementation("androidx.activity:activity-compose:1.8.2")
    implementation("androidx.navigation:navigation-compose:2.7.7")

    // WebView
    implementation("androidx.webkit:webkit:1.10.0")

    // Room
    implementation("androidx.room:room-runtime:2.6.1")
    implementation("androidx.room:room-ktx:2.6.1")
    ksp("androidx.room:room-compiler:2.6.1")

    // Hilt
    implementation("com.google.dagger:hilt-android:2.50")
    ksp("com.google.dagger:hilt-android-compiler:2.50")
    implementation("androidx.hilt:hilt-navigation-compose:1.1.0")

    // Lifecycle
    implementation("androidx.lifecycle:lifecycle-viewmodel-compose:2.7.0")
    implementation("androidx.lifecycle:lifecycle-runtime-compose:2.7.0")

    // PDF (AndroidPdfViewer)
    implementation("com.github.barteksc:android-pdf-viewer:3.2.0-beta.1")
}
```

### DI Module (Hilt)

```kotlin
@Module
@InstallIn(SingletonComponent::class)
class DatabaseModule {
    @Provides @Singleton
    fun provideDatabase(@ApplicationContext ctx: Context): CandyBrowserDatabase =
        Room.databaseBuilder(ctx, CandyBrowserDatabase::class.java, "candybrowser.db")
            .fallbackToDestructiveMigration()
            .build()

    @Provides fun provideBookmarkDao(db: CandyBrowserDatabase) = db.bookmarkDao()
    @Provides fun provideHistoryDao(db: CandyBrowserDatabase) = db.historyDao()
    // ...
}
```

---

## 5. Database Schema

### Shared SQLite Schema (EF Core on Windows, Room on Android)

#### bookmarks
```sql
CREATE TABLE bookmarks (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    parent_id   INTEGER NULL REFERENCES bookmarks(id) ON DELETE CASCADE,
    title       TEXT NOT NULL,
    url         TEXT NOT NULL,
    favicon_url TEXT NULL,
    position    INTEGER NOT NULL DEFAULT 0,
    is_folder   INTEGER NOT NULL DEFAULT 0,
    created_at  TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at  TEXT NOT NULL DEFAULT (datetime('now')),
    sync_id     TEXT NULL UNIQUE  -- for cross-device sync
);
CREATE INDEX idx_bookmarks_parent ON bookmarks(parent_id);
CREATE INDEX idx_bookmarks_url ON bookmarks(url);
```

#### history
```sql
CREATE TABLE history (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    url         TEXT NOT NULL,
    title       TEXT NOT NULL,
    favicon_url TEXT NULL,
    visit_count INTEGER NOT NULL DEFAULT 1,
    last_visit  TEXT NOT NULL DEFAULT (datetime('now')),
    first_visit TEXT NOT NULL DEFAULT (datetime('now')),
    duration_ms INTEGER NULL,           -- time spent on page
    sync_id     TEXT NULL UNIQUE
);
CREATE INDEX idx_history_url ON history(url);
CREATE INDEX idx_history_last_visit ON history(last_visit DESC);
```

#### passwords
```sql
CREATE TABLE passwords (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    domain      TEXT NOT NULL,
    username    TEXT NOT NULL,
    password    TEXT NOT NULL,           -- encrypted with AES-256-GCM
    url         TEXT NOT NULL,
    created_at  TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at  TEXT NOT NULL DEFAULT (datetime('now')),
    sync_id     TEXT NULL UNIQUE
);
CREATE INDEX idx_passwords_domain ON passwords(domain);
```

#### tabs
```sql
CREATE TABLE tabs (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    window_id   TEXT NOT NULL,           -- desktop window identifier
    position    INTEGER NOT NULL,
    url         TEXT NOT NULL,
    title       TEXT NULL,
    favicon_url TEXT NULL,
    is_pinned   INTEGER NOT NULL DEFAULT 0,
    is_incognito INTEGER NOT NULL DEFAULT 0,
    parent_id   INTEGER NULL,           -- for tab groups
    created_at  TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at  TEXT NOT NULL DEFAULT (datetime('now'))
);
CREATE INDEX idx_tabs_window ON tabs(window_id, position);
```

#### extensions
```sql
CREATE TABLE extensions (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    extension_id    TEXT NOT NULL UNIQUE,  -- Chrome extension ID or custom ID
    name            TEXT NOT NULL,
    version         TEXT NOT NULL,
    description     TEXT NULL,
    manifest_json   TEXT NOT NULL,         -- full manifest.json content
    install_path    TEXT NOT NULL,         -- filesystem path to extension
    is_enabled      INTEGER NOT NULL DEFAULT 1,
    permissions     TEXT NULL,             -- JSON array of granted permissions
    icon_path       TEXT NULL,
    installed_at    TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at      TEXT NOT NULL DEFAULT (datetime('now'))
);
```

#### settings
```sql
CREATE TABLE settings (
    key             TEXT PRIMARY KEY,
    value           TEXT NOT NULL,
    value_type      TEXT NOT NULL DEFAULT 'string',  -- string, int, bool, json
    updated_at      TEXT NOT NULL DEFAULT (datetime('now'))
);

-- Default settings inserts
-- homepage, search_engine, new_tab_url, theme, font_size, etc.
```

### EF Core Entity Example (Windows)

```csharp
// CandyBrowser.Data/Entities/BookmarkEntity.cs
public class BookmarkEntity
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? FaviconUrl { get; set; }
    public int Position { get; set; }
    public bool IsFolder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? SyncId { get; set; }

    public BookmarkEntity? Parent { get; set; }
    public List<BookmarkEntity> Children { get; set; } = new();
}
```

### Room Entity Example (Android)

```kotlin
// data/entities/BookmarkEntity.kt
@Entity(tableName = "bookmarks",
    foreignKeys = [ForeignKey(
        entity = BookmarkEntity::class,
        parentColumns = ["id"],
        childColumns = ["parent_id"],
        onDelete = ForeignKey.CASCADE
    )],
    indices = [
        Index("parent_id"),
        Index("url")
    ]
)
data class BookmarkEntity(
    @PrimaryKey(autoGenerate = true) val id: Long = 0,
    @ColumnInfo(name = "parent_id") val parentId: Long? = null,
    val title: String,
    val url: String,
    @ColumnInfo(name = "favicon_url") val faviconUrl: String? = null,
    val position: Int = 0,
    @ColumnInfo(name = "is_folder") val isFolder: Boolean = false,
    @ColumnInfo(name = "created_at") val createdAt: String = Instant.now().toString(),
    @ColumnInfo(name = "updated_at") val updatedAt: String = Instant.now().toString(),
    @ColumnInfo(name = "sync_id") val syncId: String? = null
)
```

---

## 6. Extension System Design

### Architecture: WebView2 Extension Host

**Windows (WebView2-based):**
WebView2 supports Chrome extensions natively. The extension system wraps Chrome's extension API via WebView2's `CoreWebView2Profile.AddBrowserExtension()`.

```
Extension System Flow:
┌─────────────────────────────────────────────┐
│ CandyBrowser.Windows                         │
│                                              │
│  WpfExtensionService                        │
│    ├── LoadExtensions()                      │
│    │   ├── Scan extension directories       │
│    │   ├── Validate manifest.json            │
│    │   └── Register with WebView2 profiles   │
│    ├── EnableExtension(id)                   │
│    ├── DisableExtension(id)                  │
│    └── UninstallExtension(id)               │
│                                              │
│  WebView2Manager                             │
│    ├── EnsureProfile() → creates profile    │
│    ├── AddBrowserExtension(path)            │
│    └── Extensions collection                 │
│                                              │
│  ExtensionStorage/                           │
│    ├── {extension_id}/                       │
│    │   ├── manifest.json                     │
│    │   ├── background.js                     │
│    │   ├── popup.html                        │
│    │   ├── content_scripts/                  │
│    │   └── icons/                            │
│    └── data/                                 │
│        └── {extension_id}/storage/           │
└─────────────────────────────────────────────┘
```

**Key classes:**

```csharp
// Extension manifest (Chromium-compatible)
public class ExtensionManifest
{
    public string ManifestVersion { get; set; }  // "3"
    public string Name { get; set; }
    public string Version { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, string> Icons { get; set; }
    public BrowserAction? Action { get; set; }
    public string[] Permissions { get; set; }
    public ContentScript[]? ContentScripts { get; set; }
    public string? Background { get; set; }
}

// Service interface
public interface IExtensionService
{
    Task<IReadOnlyList<ExtensionInfo>> GetAllExtensionsAsync();
    Task InstallExtensionAsync(string manifestPath);
    Task UninstallExtensionAsync(string extensionId);
    Task EnableExtensionAsync(string extensionId);
    Task DisableExtensionAsync(string extensionId);
    Task<ExtensionManifest?> GetManifestAsync(string extensionId);
}
```

**Android (Custom JS Bridge):**
Android WebView doesn't support Chrome extensions. Implement a custom extension system:

1. **JavaScript bridge** via `WebView.addJavascriptInterface()`
2. **Content script injection** via `WebView.evaluateJavascript()` on page load
3. **Popup rendering** in a WebView popup window
4. **Background service** as Android Service

```kotlin
// services/ExtensionService.kt
class ExtensionService @Inject constructor(
    private val extensionDao: ExtensionDao
) {
    fun injectContentScripts(webView: WebView, url: String) {
        // Match URL against extension content_scripts patterns
        // Inject JS via webView.evaluateJavascript()
    }

    fun createBridge(webView: WebView) {
        webView.addJavascriptInterface(
            CandyBrowserBridge(webView), "CandyBrowser"
        )
    }
}
```

### Extension Sandboxing

- **Manifest V3 compliance**: Only allow service_worker background scripts
- **Permission system**: Runtime permission prompts for sensitive APIs
- **Content Security Policy**: Enforce CSP in extension pages
- **Storage isolation**: Each extension gets its own storage namespace
- **No native code**: Extensions are pure web technologies (HTML/CSS/JS)

---

## 7. PDF Reader Integration

### Windows: WebView2 Native PDF

WebView2 has built-in PDF rendering. Two approaches:

**Option A: Navigate to PDF URL** (simplest)
```csharp
// WebView2 natively renders PDFs when navigating to a .pdf URL
// Just set the source to the PDF URL — WebView2 handles everything
webView.CoreWebView2.Navigate(pdfUrl);
```

**Option B: PDF.js embedded viewer** (more control)
```
CandyBrowser.Windows/
├── Services/
│   └── WpfPdfService.cs
│       ├── OpenPdfAsync(string pathOrUrl)
│       ├── NavigateToPdf(string url)     // WebView2 built-in
│       └── ExtractTextAsync(string path) // PDF.js text extraction
│
└── Views/
    └── PdfViewerView.xaml               // WebView2 hosting PDF.js
```

For the MVP, use WebView2's native PDF rendering. It provides:
- Built-in PDF toolbar (page navigation, zoom, print, download)
- No external dependencies
- Full PDF.js feature set (Chromium bundles it)

```csharp
// WpfPdfService.cs
public class WpfPdfService : IPdfService
{
    public void OpenPdfInWebView(CoreWebView2 webView, string url)
    {
        // WebView2 renders PDFs natively — no code needed
        webView.Navigate(url);
    }

    public async Task<string> ExtractTextAsync(string filePath)
    {
        // Use PdfPig or itext for text extraction if needed
        using var reader = PdfDocument.Open(filePath);
        return string.Join("\n", reader.GetPages().Select(p => p.Text));
    }
}
```

### Android: AndroidPdfViewer / PDF.js in WebView

```kotlin
// services/PdfService.kt
class PdfService @Inject constructor() {

    fun openPdfInWebView(webView: WebView, url: String) {
        // Load bundled PDF.js viewer in WebView
        webView.loadUrl("file:///android_asset/pdfjs/web/viewer.html?url=$url")
    }

    fun openPdfInFragment(context: Context, url: String): Fragment {
        // Use AndroidPdfViewer library for native rendering
        return PdfViewerFragment.newInstance(url)
    }
}
```

---

## 8. Reading Mode Implementation

### Content Extraction Pipeline

```
Web Page Content
    │
    ▼
┌─────────────────────────┐
│  1. Fetch DOM           │  WebView2.ExecuteScriptAsync("document.documentElement.outerHTML")
│                         │  Android: webView.evaluateJavascript("document.documentElement.outerHTML")
└──────────┬──────────────┘
           │
           ▼
┌─────────────────────────┐
│  2. Parse HTML          │  HtmlAgilityPack (.NET) / JSoup (Kotlin)
│                         │  Extract <article>, <main>, <div role="main">
└──────────┬──────────────┘
           │
           ▼
┌─────────────────────────┐
│  3. Extract Content     │  Readability algorithm (Mozilla Readability.js port)
│                         │  Strip nav, sidebar, ads, footer
│                         │  Extract title, author, date, images
└──────────┬──────────────┘
           │
           ▼
┌─────────────────────────┐
│  4. Render Clean View   │  Simple HTML template with user-selected font/theme
│                         │  WebView2 (Windows) / WebView (Android)
└─────────────────────────┘
```

**Key Classes:**

```csharp
// Shared model
public class ReadingContent
{
    public string Title { get; set; }
    public string? Author { get; set; }
    public string? PublishDate { get; set; }
    public string HtmlContent { get; set; }
    public string? TextContent { get; set; }
    public List<string> ImageUrls { get; set; }
    public string SourceUrl { get; set; }
    public string? SiteName { get; set; }
}

// Windows service
public class WpfReadingModeService : IReadingModeService
{
    private readonly HtmlWeb _htmlWeb = new();

    public async Task<ReadingContent> ExtractContentAsync(string url)
    {
        var doc = _htmlWeb.Load(url);
        // Use Readability-like algorithm to extract main content
        // Return cleaned ReadingContent
    }

    public string RenderAsCleanHtml(ReadingContent content, ReadingTheme theme)
    {
        // Inject into a clean HTML template
        // Support light/dark sepia themes
        // Configurable font family and size
    }
}
```

**Reading Mode Template** (embedded resource):

```html
<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <style>
    body { font-family: {{FONT_FAMILY}}; font-size: {{FONT_SIZE}}px;
           max-width: 720px; margin: 0 auto; padding: 24px;
           background: {{BG_COLOR}}; color: {{TEXT_COLOR}}; }
    img { max-width: 100%; height: auto; }
    h1 { font-size: 2em; line-height: 1.3; }
    .meta { color: {{META_COLOR}}; font-size: 0.9em; margin-bottom: 24px; }
  </style>
</head>
<body>
  <h1>{{TITLE}}</h1>
  <div class="meta">{{AUTHOR}} · {{DATE}}</div>
  <article>{{CONTENT}}</article>
</body>
</html>
```

### ReadingModeViewModel

```csharp
public partial class ReadingModeViewModel : ObservableObject
{
    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string _author = "";
    [ObservableProperty] private string _cleanHtml = "";
    [ObservableProperty] private string _sourceUrl = "";
    [ObservableProperty] private ReadingTheme _theme = ReadingTheme.Light;
    [ObservableProperty] private int _fontSize = 18;

    [RelayCommand]
    private async Task LoadContent(string url) { /* extract and render */ }

    [RelayCommand]
    private void ToggleTheme() { /* cycle light/dark/sepia */ }
}

public enum ReadingTheme { Light, Dark, Sepia }
```

---

## 9. DevTools Integration

### Windows (WebView2 DevTools)

WebView2 exposes Chrome DevTools Protocol directly:

```csharp
// Services/WpfDevToolsService.cs
public class WpfDevToolsService
{
    private CoreWebView2 _webView;

    /// <summary>
    /// Open DevTools in a separate window (like Edge)
    /// </summary>
    public void OpenDevToolsWindow(CoreWebView2 webView)
    {
        // Option 1: Use WebView2's built-in OpenDevToolsWindow()
        webView.OpenDevToolsWindow();

        // Option 2: Host DevTools in a separate Window with another WebView2
        // This gives us more control over the DevTools UI
    }

    /// <summary>
    /// Open DevTools in a docked panel (bottom or side)
    /// </summary>
    public void OpenDevToolsPanel(CoreWebView2 webView, DevToolsPosition position)
    {
        // Create a split view with the page on top and DevTools on bottom
        // Use another WebView2 instance for DevTools
        var devToolsWebView = new WebView2();
        devToolsWebView.CoreWebView2-initialized += (s, e) =>
        {
            // Attach DevTools protocol
            devToolsWebView.CoreWebView2.GetDevToolsProtocol().Enable();
        };
    }

    /// <summary>
    /// Execute DevTools Protocol command
    /// </summary>
    public async Task<JsonDocument> ExecuteDevToolsCommandAsync(
        CoreWebView2 webView, string method, object? parameters = null)
    {
        // Use CoreWebView2.CallDevToolsProtocolMethodAsync
        var result = await webView.CoreWebView2.CallDevToolsProtocolMethodAsync(
            method, JsonSerializer.Serialize(parameters ?? new { }));
        return JsonDocument.Parse(result);
    }
}
```

### Android DevTools

```kotlin
// services/DevToolsService.kt
class DevToolsService {
    fun enableDevTools(webView: WebView) {
        WebView.setWebContentsDebuggingEnabled(true)
        // Opens DevTools via chrome://inspect on desktop Chrome
    }

    fun openDevToolsInPanel(activity: Activity, webView: WebView) {
        // Use a bottom sheet with a WebView pointing to
        // chrome-devtools-frontend or a custom inspector
    }
}
```

### DevTools Panel View (Windows)

```
┌─────────────────────────────────────┐
│  Browser Content (WebView2)         │
│  https://example.com                │
├─────────────────────────────────────┤
│  DevTools Panel                     │
│  [Elements] [Console] [Network]     │
│  [Sources] [Performance] [Memory]   │
│                                     │
│  DevTools WebView2 instance         │
│  showing DevTools frontend          │
└─────────────────────────────────────┘
```

---

## 10. Navigation System

### Address Bar & Autocomplete

```
┌─────────────────────────────────────────────────────────┐
│ ←  →  ↻  🏠  │ https://example.com/page?q=search     │ 🔒 │
│ Navigation     │ Address Bar (AutoCompleteTextBox)      │    │
│ Buttons        │                                        │    │
└─────────────────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────┐
│ Suggestions Dropdown                                    │
│  🔖 bookmark: Example Site              example.com     │
│  📄 history:  Example Page              example.com/p1  │
│  🔍 search:   example query             google.com      │
│  🌐 visit:    example.com               (typed)         │
└─────────────────────────────────────────────────────────┘
```

**AddressBarViewModel:**

```csharp
public partial class AddressBarViewModel : ObservableObject
{
    private readonly IBookmarkService _bookmarks;
    private readonly IHistoryService _history;
    private readonly ISettingsService _settings;

    [ObservableProperty] private string _inputText = "";
    [ObservableProperty] private ObservableCollection<SuggestionItem> _suggestions = new();
    [ObservableProperty] private bool _isDropdownOpen;

    [RelayCommand]
    private async Task OnInputChanged()
    {
        if (string.IsNullOrWhiteSpace(InputText)) { IsDropdownOpen = false; return; }

        var bookmarkMatches = await _bookmarks.SearchAsync(InputText, limit: 3);
        var historyMatches = await _history.SearchAsync(InputText, limit: 5);
        var searchSuggestion = GetSearchSuggestion(InputText);

        Suggestions.Clear();
        foreach (var b in bookmarkMatches)
            Suggestions.Add(new SuggestionItem { Type = SuggestionType.Bookmark, Title = b.Title, Url = b.Url });
        foreach (var h in historyMatches)
            Suggestions.Add(new SuggestionItem { Type = SuggestionType.History, Title = h.Title, Url = h.Url });
        if (searchSuggestion != null)
            Suggestions.Add(searchSuggestion);

        // Smart URL detection
        if (IsUrl(InputText))
            Suggestions.Insert(0, new SuggestionItem { Type = SuggestionType.Url, Title = InputText, Url = NormalizeUrl(InputText) });

        IsDropdownOpen = Suggestions.Count > 0;
    }

    [RelayCommand]
    private void NavigateToSelected(SuggestionItem item)
    {
        NavigationRequested?.Invoke(item.Url);
    }

    private bool IsUrl(string input) =>
        Uri.TryCreate(input, UriKind.Absolute, out _) ||
        input.Contains('.') && !input.Contains(' ');

    private string NormalizeUrl(string input)
    {
        if (!input.StartsWith("http://") && !input.StartsWith("https://"))
            input = "https://" + input;
        return input;
    }

    private SuggestionItem? GetSearchSuggestion(string query)
    {
        var engine = _settings.GetSearchEngine(); // e.g., "https://www.google.com/search?q={0}"
        return new SuggestionItem
        {
            Type = SuggestionType.Search,
            Title = $"Search \"{query}\"",
            Url = string.Format(engine, Uri.EscapeDataString(query))
        };
    }
}
```

### Navigation Service

```csharp
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
```

### Windows Navigation (WebView2)

```csharp
public class WpfNavigationService : INavigationService
{
    private CoreWebView2 _webView;

    public void Navigate(string url)
    {
        _webView.Navigate(url);
    }

    public void GoBack() => _webView.GoBack();
    public void GoForward() => _webView.GoForward();
    public void Reload() => _webView.Reload();
    public void Stop() => _webView.Stop();
    public void GoHome() => _webView.Navigate(_settingsService.GetHomepage());

    // Wire up events
    private void WireEvents()
    {
        _webView.NavigationStarting += (s, e) => IsLoadingChanged?.Invoke(this, true);
        _webView.NavigationCompleted += (s, e) =>
        {
            IsLoadingChanged?.Invoke(this, false);
            TitleChanged?.Invoke(this, _webView.DocumentTitle);
            CanGoBackChanged?.Invoke(this, _webView.CanGoBack);
            CanGoForwardChanged?.Invoke(this, _webView.CanGoForward);
        };
    }
}
```

---

## Phased Implementation Plan

### Phase 1: Foundation (Weeks 1-3) ✅ Milestone: "It browses"

| Week | Task | Files |
|------|------|-------|
| 1 | Solution scaffold, .sln, all .csproj files | CandyBrowser.sln, all .csproj |
| 1 | Core models + Shared.Abstractions interfaces | Models/*.cs, Interfaces/*.cs |
| 1 | Data layer: EF Core DbContext + entities + migrations | Contexts/, Entities/, Configurations/ |
| 2 | Windows WPF shell: MainWindow with tab strip | MainWindow.xaml, TabStrip, TabItem |
| 2 | WebView2 integration (single tab) | WebView2Tab.xaml, WebView2Manager.cs |
| 2 | Address bar with basic navigation | AddressBar.xaml, AddressBarViewModel |
| 3 | Navigation buttons (back/forward/refresh/home) | NavigationButtons.xaml |
| 3 | Tab creation/closing/switching | BrowserTabViewModel, MainViewModel |
| 3 | History recording | WpfHistoryService, HistoryRepository |

### Phase 2: Core Features (Weeks 4-6) ✅ Milestone: "It's useful"

| Week | Task | Files |
|------|------|-------|
| 4 | Bookmark sidebar with CRUD | BookmarkSidebar, BookmarkViewModel, BookmarkService |
| 4 | Bookmark tree view (folders) | BookmarkTreeView, BookmarkRepository |
| 5 | Autocomplete in address bar | AddressBarViewModel (search suggestions) |
| 5 | Search engine integration | SearchEngineConfig, settings table |
| 5 | Browsing history panel | HistoryPanel, HistoryViewModel |
| 6 | Settings window | SettingsWindow, SettingsViewModel |
| 6 | Theme system (light/dark) | LightTheme.xaml, DarkTheme.xaml |
| 6 | Session restore (tabs, positions) | WpfTabManager persistence |

### Phase 3: Power Features (Weeks 7-9) ✅ Milestone: "It's powerful"

| Week | Task | Files |
|------|------|-------|
| 7 | PDF viewer (WebView2 native) | WpfPdfService |
| 7 | Reading mode (content extraction) | ReadingModeView, WpfReadingModeService |
| 8 | Password manager | PasswordManagerWindow, WpfPasswordService |
| 8 | AES-256-GCM encryption for passwords | EncryptionService |
| 9 | DevTools panel (WebView2 DevTools) | DevToolsPanel, WpfDevToolsService |
| 9 | Extension system scaffold | WpfExtensionService, manifest parser |

### Phase 4: Extensions & Polish (Weeks 10-12) ✅ Milestone: "It's extensible"

| Week | Task | Files |
|------|------|-------|
| 10 | Extension loading + enable/disable | ExtensionManagerWindow |
| 10 | Extension permission system | PermissionService |
| 11 | Extension popup rendering | WebView2 popup host |
| 11 | Content script injection | Script injection service |
| 12 | Polish: loading states, error handling | Throughout |
| 12 | Keyboard shortcuts, context menus | Throughout |

### Phase 5: Android (Weeks 13-18) ✅ Milestone: "It's cross-platform"

| Week | Task | Files |
|------|------|-------|
| 13 | Android project scaffold, Hilt DI, Room DB | build.gradle, Hilt modules, DAOs |
| 14 | WebView + tab management | BrowserScreen, BrowserViewModel |
| 14 | Address bar + navigation | AddressBar, NavigationBar |
| 15 | Bookmarks + history | BookmarkScreen, HistoryScreen |
| 15 | Settings | SettingsScreen |
| 16 | Reading mode | ReadingModeScreen, ReadingModeService |
| 16 | PDF viewer | PdfService, PDF.js asset |
| 17 | Password manager | PasswordScreen, PasswordViewModel |
| 17 | Extension system (JS bridge) | ExtensionService, Bridge |
| 18 | Polish, testing, release prep | Throughout |

---

## Key Technology Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Desktop UI Framework | WPF + .NET 8 | Mature, good WebView2 support, MVVM ecosystem |
| WebView | WebView2 (Edge Chromium) | Chromium rendering, Chrome extension compat, actively maintained |
| MVVM Toolkit | CommunityToolkit.Mvvm | Source generators, minimal boilerplate, official Microsoft |
| DI Container | Microsoft.Extensions.DependencyInjection | Standard .NET, works with hosting |
| Desktop ORM | EF Core + SQLite | LINQ queries, migrations, well-documented |
| Android UI | Jetpack Compose + Material 3 | Modern declarative UI, Google's direction |
| Android DI | Hilt | Official, integrates with Compose navigation |
| Android ORM | Room | Official SQLite wrapper, compile-time checks |
| PDF (Desktop) | WebView2 native | Zero dependencies, Chromium-quality rendering |
| PDF (Android) | PDF.js in WebView | Consistent cross-platform, no native lib needed |
| Content Extraction | HtmlAgilityPack (.NET) + JSoup (Kotlin) | Battle-tested HTML parsers |
| Password Encryption | AES-256-GCM | Industry standard, authenticated encryption |
| Search Autocomplete | Local DB + search engine API | Privacy-respecting, fast |
