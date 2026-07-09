using System.Threading.Tasks;

namespace CandyBrowser.Shared.Abstractions
{
    public interface ISettingsService
    {
        Task<string?> GetAsync(string key);
        Task<T?> GetAsync<T>(string key, T defaultValue);
        Task SetAsync(string key, string value, string valueType = "string");
        Task<string> GetSearchEngineAsync();
        Task<string> GetHomepageAsync();
        Task<string> GetNewTabUrlAsync();
    }
}
