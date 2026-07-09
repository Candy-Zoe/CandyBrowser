using Microsoft.EntityFrameworkCore;
using CandyBrowser.Data.Contexts;
using CandyBrowser.Data.Entities;
using CandyBrowser.Shared.Abstractions;
using Models = CandyBrowser.Core.Models;

namespace CandyBrowser.Services.History;

public class HistoryService : IHistoryService
{
    private readonly BrowserDbContext _db;

    public HistoryService(BrowserDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(string url, string title, string? faviconUrl = null)
    {
        var existing = await _db.History.FirstOrDefaultAsync(h => h.Url == url);

        if (existing != null)
        {
            existing.VisitCount++;
            existing.LastVisit = DateTime.UtcNow;
            existing.Title = title;
            existing.FaviconUrl = faviconUrl;
        }
        else
        {
            _db.History.Add(new HistoryEntity
            {
                Url = url,
                Title = title,
                FaviconUrl = faviconUrl,
                VisitCount = 1,
                LastVisit = DateTime.UtcNow,
                FirstVisit = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Models.HistoryEntry>> GetAllAsync(int limit = 100, int offset = 0)
    {
        return await _db.History
            .OrderByDescending(h => h.LastVisit)
            .Skip(offset)
            .Take(limit)
            .Select(h => MapToModel(h))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Models.HistoryEntry>> SearchAsync(string query, int limit = 10)
    {
        return await _db.History
            .Where(h => h.Title.Contains(query) || h.Url.Contains(query))
            .OrderByDescending(h => h.LastVisit)
            .Take(limit)
            .Select(h => MapToModel(h))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Models.HistoryEntry>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        return await _db.History
            .Where(h => h.LastVisit >= from && h.LastVisit <= to)
            .OrderByDescending(h => h.LastVisit)
            .Select(h => MapToModel(h))
            .ToListAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var entity = await _db.History.FindAsync(id);
        if (entity == null) return;

        _db.History.Remove(entity);
        await _db.SaveChangesAsync();
    }

    public async Task ClearAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = _db.History.AsQueryable();

        if (from.HasValue)
            query = query.Where(h => h.LastVisit >= from.Value);
        if (to.HasValue)
            query = query.Where(h => h.LastVisit <= to.Value);

        var items = await query.ToListAsync();
        _db.History.RemoveRange(items);
        await _db.SaveChangesAsync();
    }

    private static Models.HistoryEntry MapToModel(HistoryEntity entity)
    {
        return new Models.HistoryEntry
        {
            Id = entity.Id,
            Url = entity.Url,
            Title = entity.Title,
            FaviconUrl = entity.FaviconUrl,
            VisitCount = entity.VisitCount,
            LastVisit = entity.LastVisit,
            FirstVisit = entity.FirstVisit,
            DurationMs = entity.DurationMs,
            SyncId = entity.SyncId
        };
    }
}
