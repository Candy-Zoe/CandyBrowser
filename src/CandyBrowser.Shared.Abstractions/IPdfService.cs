using System.Threading.Tasks;

namespace CandyBrowser.Shared.Abstractions
{
    public interface IPdfService
    {
        Task OpenPdfAsync(string pathOrUrl);
        Task<string> ExtractTextAsync(string filePath);
    }
}
