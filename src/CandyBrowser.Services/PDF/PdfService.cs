using System.Text;
using CandyBrowser.Shared.Abstractions;

namespace CandyBrowser.Services.PDF;

public class PdfService : IPdfService
{
    public async Task OpenPdfAsync(string pathOrUrl)
    {
        // PDF viewing is delegated to WebView2's built-in PDF viewer
        // The browser navigates to the PDF URL, and WebView2 renders it natively
        await Task.CompletedTask;
    }

    public async Task<string> ExtractTextAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("PDF file not found", filePath);

        try
        {
            var bytes = await File.ReadAllBytesAsync(filePath);
            
            // Check PDF magic number
            if (bytes.Length < 5 || bytes[0] != '%' || bytes[1] != 'P' || bytes[2] != 'D' || bytes[3] != 'F')
                throw new InvalidOperationException("Not a valid PDF file");

            var text = ExtractTextFromPdfBytes(bytes);
            return string.IsNullOrWhiteSpace(text) ? "[No extractable text found in PDF]" : text;
        }
        catch (IOException) when (filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            // File is locked by another process (e.g., WebView2 is displaying it)
            return "[File is currently in use]";
        }
    }

    /// <summary>
    /// Basic PDF text extraction by scanning for TD/Tj/TJ operators in raw PDF streams.
    /// This is a simplified approach; production use should employ a proper PDF library.
    /// </summary>
    private static string ExtractTextFromPdfBytes(byte[] bytes)
    {
        var textBuilder = new StringBuilder();
        
        try
        {
            // Convert to string for text extraction (most PDFs contain ASCII/UTF8 text)
            var pdfText = Encoding.ASCII.GetString(bytes);
            
            // Method 1: Extract text from Tj and TJ operators
            // Pattern: [(text)] TJ or (text) Tj
            var tjMatches = System.Text.RegularExpressions.Regex.Matches(pdfText, @"\(([^)]*)\)\s*Tj");
            foreach (System.Text.RegularExpressions.Match match in tjMatches)
            {
                var content = match.Groups[1].Value;
                // Unescape PDF string escapes
                content = content
                    .Replace("\\n", "\n")
                    .Replace("\\r", "\r")
                    .Replace("\\t", "\t")
                    .Replace("\\(", "(")
                    .Replace("\\)", ")")
                    .Replace("\\\\", "\\");
                textBuilder.Append(content);
            }

            // Method 2: Extract from TJ operator arrays [text1 text2 ...] TJ
            var tjArrayMatches = System.Text.RegularExpressions.Regex.Matches(pdfText, @"\[((?:\([^)]*\)\s*)+)\]\s*TJ");
            foreach (System.Text.RegularExpressions.Match match in tjArrayMatches)
            {
                var arrayContent = match.Groups[1].Value;
                var itemMatches = System.Text.RegularExpressions.Regex.Matches(arrayContent, @"\(([^)]*)\)");
                foreach (System.Text.RegularExpressions.Match itemMatch in itemMatches)
                {
                    var content = itemMatch.Groups[1].Value;
                    content = content
                        .Replace("\\(", "(")
                        .Replace("\\)", ")")
                        .Replace("\\\\", "\\");
                    textBuilder.Append(content);
                }
            }

            // Method 3: Extract from stream content (between stream...endstream)
            var streamMatches = System.Text.RegularExpressions.Regex.Matches(
                pdfText, @"stream\r?\n(.*?)endstream", 
                System.Text.RegularExpressions.RegexOptions.Singleline);
            
            foreach (System.Text.RegularExpressions.Match streamMatch in streamMatches)
            {
                var streamContent = streamMatch.Groups[1].Value;
                var streamTjMatches = System.Text.RegularExpressions.Regex.Matches(streamContent, @"\(([^)]*)\)\s*Tj");
                foreach (System.Text.RegularExpressions.Match match in streamTjMatches)
                {
                    var content = match.Groups[1].Value;
                    content = content
                        .Replace("\\n", "\n")
                        .Replace("\\(", "(")
                        .Replace("\\)", ")")
                        .Replace("\\\\", "\\");
                    textBuilder.AppendLine(content);
                }
            }

            return textBuilder.ToString().Trim();
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Checks if a file is likely a PDF by examining its magic bytes.
    /// </summary>
    public static bool IsPdfFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return false;
            using var fs = File.OpenRead(filePath);
            if (fs.Length < 5) return false;
            var header = new byte[5];
            fs.ReadExactly(header, 0, 5);
            return header[0] == '%' && header[1] == 'P' && header[2] == 'D' && header[3] == 'F';
        }
        catch
        {
            return false;
        }
    }
}
