using System.Collections.Generic;
using System.Threading.Tasks;
using CandyBrowser.Core.Models;

namespace CandyBrowser.Shared.Abstractions
{
    public interface IBookmarkService
    {
        Task<IReadOnlyList<Bookmark>> GetAllAsync();
        Task<IReadOnlyList<Bookmark>> GetChildrenAsync(long? parentId);
        Task<IReadOnlyList<Bookmark>> SearchAsync(string query, int limit = 10);
        Task<Bookmark?> GetByIdAsync(long id);
        Task<Bookmark> AddAsync(Bookmark bookmark);
        Task UpdateAsync(Bookmark bookmark);
        Task DeleteAsync(long id);
        Task DeleteRecursiveAsync(long id);
        Task<IReadOnlyList<Bookmark>> GetTreeAsync();
    }

    /// <summary>
    /// Interface for bookmark import/export service.
    /// </summary>
    public interface IBookmarkImportExportService
    {
        Task<string> ExportToHtmlAsync();
        Task<int> ImportFromHtmlAsync(string htmlContent, long? parentId = null);
    }
}
