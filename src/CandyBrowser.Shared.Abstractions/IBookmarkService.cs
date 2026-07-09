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
        Task<IReadOnlyList<Bookmark>> GetTreeAsync();
    }
}
