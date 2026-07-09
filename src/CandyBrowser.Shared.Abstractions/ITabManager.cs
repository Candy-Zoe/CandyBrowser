using System.Collections.Generic;
using System.Threading.Tasks;
using CandyBrowser.Core.Models;

namespace CandyBrowser.Shared.Abstractions
{
    public interface ITabManager
    {
        Task<IReadOnlyList<TabInfo>> GetAllAsync();
        Task<TabInfo?> GetByIdAsync(long id);
        Task<TabInfo> CreateAsync(string url, string? windowId = null);
        Task CloseAsync(long id);
        Task UpdateAsync(TabInfo tab);
        Task SaveStateAsync(string windowId);
        Task RestoreStateAsync(string windowId);
    }
}
