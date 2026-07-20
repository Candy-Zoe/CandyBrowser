using Microsoft.EntityFrameworkCore;
using CandyBrowser.Data.Contexts;
using CandyBrowser.Data.Entities;
using CandyBrowser.Shared.Abstractions;
using Models = CandyBrowser.Core.Models;

namespace CandyBrowser.Services.Bookmarks;

public class BookmarkService : IBookmarkService
{
    private readonly BrowserDbContext _db;

    public BookmarkService(BrowserDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Models.Bookmark>> GetAllAsync()
    {
        return await _db.Bookmarks
            .OrderBy(b => b.Position)
            .Select(b => MapToModel(b))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Models.Bookmark>> GetChildrenAsync(long? parentId)
    {
        return await _db.Bookmarks
            .Where(b => b.ParentId == parentId)
            .OrderBy(b => b.Position)
            .Select(b => MapToModel(b))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Models.Bookmark>> SearchAsync(string query, int limit = 10)
    {
        return await _db.Bookmarks
            .Where(b => !b.IsFolder && (b.Title.Contains(query) || b.Url.Contains(query)))
            .OrderByDescending(b => b.UpdatedAt)
            .Take(limit)
            .Select(b => MapToModel(b))
            .ToListAsync();
    }

    public async Task<Models.Bookmark?> GetByIdAsync(long id)
    {
        var entity = await _db.Bookmarks.FindAsync(id);
        return entity == null ? null : MapToModel(entity);
    }

    public async Task<Models.Bookmark> AddAsync(Models.Bookmark bookmark)
    {
        var entity = new BookmarkEntity
        {
            ParentId = bookmark.ParentId,
            Title = bookmark.Title,
            Url = bookmark.Url,
            FaviconUrl = bookmark.FaviconUrl,
            Position = bookmark.Position,
            IsFolder = bookmark.IsFolder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Bookmarks.Add(entity);
        await _db.SaveChangesAsync();

        bookmark.Id = entity.Id;
        return bookmark;
    }

    public async Task UpdateAsync(Models.Bookmark bookmark)
    {
        var entity = await _db.Bookmarks.FindAsync(bookmark.Id);
        if (entity == null) return;

        entity.Title = bookmark.Title;
        entity.Url = bookmark.Url;
        entity.FaviconUrl = bookmark.FaviconUrl;
        entity.Position = bookmark.Position;
        entity.IsFolder = bookmark.IsFolder;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        await DeleteRecursiveAsync(id);
    }

    public async Task DeleteRecursiveAsync(long id)
    {
        var children = await _db.Bookmarks.Where(b => b.ParentId == id).ToListAsync();
        foreach (var child in children)
            await DeleteRecursiveAsync(child.Id);

        var entity = await _db.Bookmarks.FindAsync(id);
        if (entity != null)
        {
            _db.Bookmarks.Remove(entity);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<IReadOnlyList<Models.Bookmark>> GetTreeAsync()
    {
        var all = await _db.Bookmarks
            .OrderBy(b => b.Position)
            .ToListAsync();

        var lookup = all.ToLookup(b => b.ParentId);
        var roots = all.Where(b => b.ParentId == null).ToList();

        foreach (var item in all)
        {
            item.Children = lookup[item.Id].OrderBy(c => c.Position).ToList();
        }

        return roots.Select(MapToModel).ToList();
    }

    private static Models.Bookmark MapToModel(BookmarkEntity entity)
    {
        return new Models.Bookmark
        {
            Id = entity.Id,
            ParentId = entity.ParentId,
            Title = entity.Title,
            Url = entity.Url,
            FaviconUrl = entity.FaviconUrl,
            Position = entity.Position,
            IsFolder = entity.IsFolder,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            SyncId = entity.SyncId
        };
    }
}
