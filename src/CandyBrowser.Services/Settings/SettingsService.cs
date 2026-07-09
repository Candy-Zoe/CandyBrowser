using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using CandyBrowser.Data.Contexts;
using CandyBrowser.Data.Entities;

namespace CandyBrowser.Services.Settings;

public class SettingsService : Shared.Abstractions.ISettingsService
{
    private readonly BrowserDbContext _db;
    private const string DefaultSearchEngine = "https://www.bing.com/search?q={0}";
    private const string DefaultHomepage = "about:blank";
    private const string DefaultNewTabUrl = "about:blank";

    public SettingsService(BrowserDbContext db)
    {
        _db = db;
    }

    public async Task<string?> GetAsync(string key)
    {
        var setting = await _db.Settings.FindAsync(key);
        return setting?.Value;
    }

    public async Task<T?> GetAsync<T>(string key, T defaultValue)
    {
        var value = await GetAsync(key);
        if (value == null) return defaultValue;

        try
        {
            return JsonSerializer.Deserialize<T>(value);
        }
        catch
        {
            return defaultValue;
        }
    }

    public async Task SetAsync(string key, string value, string valueType = "string")
    {
        var setting = await _db.Settings.FindAsync(key);

        if (setting == null)
        {
            setting = new SettingEntity
            {
                Key = key,
                Value = value,
                ValueType = valueType,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Settings.Add(setting);
        }
        else
        {
            setting.Value = value;
            setting.ValueType = valueType;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<string> GetSearchEngineAsync()
    {
        return await GetAsync("search_engine") ?? DefaultSearchEngine;
    }

    public async Task<string> GetHomepageAsync()
    {
        return await GetAsync("homepage") ?? DefaultHomepage;
    }

    public async Task<string> GetNewTabUrlAsync()
    {
        return await GetAsync("new_tab_url") ?? DefaultNewTabUrl;
    }
}
