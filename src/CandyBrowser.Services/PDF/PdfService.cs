using CandyBrowser.Shared.Abstractions;

namespace CandyBrowser.Services.PDF;

public class PdfService : IPdfService
{
    public Task OpenPdfAsync(string pathOrUrl)
    {
        // PDF viewing is handled by the platform-specific WebView
        // This service provides shared logic
        return Task.CompletedTask;
    }

    public Task<string> ExtractTextAsync(string filePath)
    {
        // Basic text extraction from PDF
        // In production, use a library like iTextSharp or PDFsharp
        if (!File.Exists(filePath))
            throw new FileNotFoundException("PDF file not found", filePath);

        // Placeholder implementation
        return Task.FromResult($"[PDF content of {Path.GetFileName(filePath)}]");
    }
}
