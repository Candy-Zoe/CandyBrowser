using System.Threading.Tasks;
using CandyBrowser.Core.Enums;

namespace CandyBrowser.Shared.Abstractions
{
    public interface IReadingModeService
    {
        Task<string> ExtractContentAsync(string url);
        string RenderAsCleanHtml(string title, string author, string htmlContent, ReadingTheme theme, int fontSize = 18);
    }
}
