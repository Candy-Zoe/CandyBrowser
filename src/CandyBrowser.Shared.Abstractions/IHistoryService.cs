using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CandyBrowser.Core.Models;

namespace CandyBrowser.Shared.Abstractions
{
    public interface IHistoryService
    {
        Task AddAsync(string url, string title, string? faviconUrl = null);
        Task<IReadOnlyList<HistoryEntry>> GetAllAsync(int limit = 100, int offset = 0);
        Task<IReadOnlyList<HistoryEntry>> SearchAsync(string query, int limit = 10);
        Task<IReadOnlyList<HistoryEntry>> GetByDateRangeAsync(DateTime from, DateTime to);
        Task DeleteAsync(long id);
        Task ClearAsync(DateTime? from = null, DateTime? to = null);
    }
}
