using Microsoft.EntityFrameworkCore;
using CandyBrowser.Data.Contexts;
using CandyBrowser.Data.Entities;
using CandyBrowser.Shared.Abstractions;
using Models = CandyBrowser.Core.Models;

namespace CandyBrowser.Services.Tabs;

public class TabService : ITabManager
{
    private readonly BrowserDbContext _db;

    public TabService(BrowserDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Models.TabInfo>> GetAllAsync()
    {
        return await _db.Tabs
            .OrderBy(t => t.Position)
            .Select(t => MapToModel(t))
            .ToListAsync();
    }

    public async Task<Models.TabInfo?> GetByIdAsync(long id)
    {
        var entity = await _db.Tabs.FindAsync(id);
        return entity == null ? null : MapToModel(entity);
    }

    public async Task<Models.TabInfo> CreateAsync(string url, string? windowId = null)
    {
        var maxPosition = await _db.Tabs
            .Where(t => t.WindowId == (windowId ?? "default"))
            .MaxAsync(t => (int?)t.Position) ?? 0;

        var entity = new TabEntity
        {
            WindowId = windowId ?? "default",
            Position = maxPosition + 1,
            Url = url,
            Title = null,
            IsPinned = false,
            IsIncognito = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Tabs.Add(entity);
        await _db.SaveChangesAsync();

        return MapToModel(entity);
    }

    public async Task CloseAsync(long id)
    {
        var entity = await _db.Tabs.FindAsync(id);
        if (entity == null) return;

        _db.Tabs.Remove(entity);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Models.TabInfo tab)
    {
        var entity = await _db.Tabs.FindAsync(tab.Id);
        if (entity == null) return;

        entity.WindowId = tab.WindowId;
        entity.Position = tab.Position;
        entity.Url = tab.Url;
        entity.Title = tab.Title;
        entity.FaviconUrl = tab.FaviconUrl;
        entity.IsPinned = tab.IsPinned;
        entity.IsIncognito = tab.IsIncognito;
        entity.ParentId = tab.ParentId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task SaveStateAsync(string windowId)
    {
        var tabs = await _db.Tabs
            .Where(t => t.WindowId == windowId)
            .OrderBy(t => t.Position)
            .ToListAsync();

        foreach (var tab in tabs)
        {
            tab.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    public async Task RestoreStateAsync(string windowId)
    {
        var tabs = await _db.Tabs
            .Where(t => t.WindowId == windowId)
            .OrderBy(t => t.Position)
            .ToListAsync();

        foreach (var tab in tabs)
        {
            tab.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    private static Models.TabInfo MapToModel(TabEntity entity)
    {
        return new Models.TabInfo
        {
            Id = entity.Id,
            WindowId = entity.WindowId,
            Position = entity.Position,
            Url = entity.Url,
            Title = entity.Title,
            FaviconUrl = entity.FaviconUrl,
            IsPinned = entity.IsPinned,
            IsIncognito = entity.IsIncognito,
            ParentId = entity.ParentId,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
