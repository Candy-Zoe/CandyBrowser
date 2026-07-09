using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using CandyBrowser.Data.Contexts;
using CandyBrowser.Data.Entities;
using CandyBrowser.Shared.Abstractions;
using Models = CandyBrowser.Core.Models;

namespace CandyBrowser.Services.Extensions;

public class ExtensionService : IExtensionService
{
    private readonly BrowserDbContext _db;

    public ExtensionService(BrowserDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Models.ExtensionInfo>> GetAllAsync()
    {
        return await _db.Extensions
            .OrderBy(e => e.Name)
            .Select(e => MapToModel(e))
            .ToListAsync();
    }

    public async Task InstallExtensionAsync(string manifestPath)
    {
        var manifestJson = await File.ReadAllTextAsync(manifestPath);
        var manifest = JsonSerializer.Deserialize<JsonElement>(manifestJson);

        var name = manifest.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "Unknown" : "Unknown";
        var version = manifest.TryGetProperty("version", out var versionProp) ? versionProp.GetString() ?? "1.0" : "1.0";
        var description = manifest.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;
        var permissions = manifest.TryGetProperty("permissions", out var permProp) ? permProp.GetRawText() : null;

        var extensionId = Guid.NewGuid().ToString("N");
        var installPath = Path.GetDirectoryName(manifestPath) ?? string.Empty;

        var entity = new ExtensionEntity
        {
            ExtensionId = extensionId,
            Name = name,
            Version = version,
            Description = description,
            ManifestJson = manifestJson,
            InstallPath = installPath,
            IsEnabled = true,
            Permissions = permissions,
            InstalledAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Extensions.Add(entity);
        await _db.SaveChangesAsync();
    }

    public async Task UninstallExtensionAsync(string extensionId)
    {
        var entity = await _db.Extensions
            .FirstOrDefaultAsync(e => e.ExtensionId == extensionId);

        if (entity == null) return;

        _db.Extensions.Remove(entity);
        await _db.SaveChangesAsync();
    }

    public async Task EnableExtensionAsync(string extensionId)
    {
        var entity = await _db.Extensions
            .FirstOrDefaultAsync(e => e.ExtensionId == extensionId);

        if (entity == null) return;

        entity.IsEnabled = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task DisableExtensionAsync(string extensionId)
    {
        var entity = await _db.Extensions
            .FirstOrDefaultAsync(e => e.ExtensionId == extensionId);

        if (entity == null) return;

        entity.IsEnabled = false;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<Models.ExtensionInfo?> GetByIdAsync(string extensionId)
    {
        var entity = await _db.Extensions
            .FirstOrDefaultAsync(e => e.ExtensionId == extensionId);

        return entity == null ? null : MapToModel(entity);
    }

    public async Task<Models.ExtensionInfo?> GetByManifestAsync(string manifestJson)
    {
        var entity = await _db.Extensions
            .FirstOrDefaultAsync(e => e.ManifestJson == manifestJson);

        return entity == null ? null : MapToModel(entity);
    }

    private static Models.ExtensionInfo MapToModel(ExtensionEntity entity)
    {
        return new Models.ExtensionInfo
        {
            Id = entity.Id,
            ExtensionId = entity.ExtensionId,
            Name = entity.Name,
            Version = entity.Version,
            Description = entity.Description,
            ManifestJson = entity.ManifestJson,
            InstallPath = entity.InstallPath,
            IsEnabled = entity.IsEnabled,
            Permissions = entity.Permissions,
            IconPath = entity.IconPath,
            InstalledAt = entity.InstalledAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
