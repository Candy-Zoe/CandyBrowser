using System.Collections.Concurrent;
using System.Net.Http;
using CandyBrowser.Core.Models;
using CandyBrowser.Shared.Abstractions;

namespace CandyBrowser.Services.Downloads;

public class DownloadService : IDownloadService
{
    private readonly ConcurrentDictionary<long, DownloadItem> _downloads = new();
    private readonly ConcurrentDictionary<long, CancellationTokenSource> _cancellationTokens = new();
    private readonly HttpClient _httpClient;
    private long _nextId = 1;
    private string _defaultDownloadPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Downloads");

    public event EventHandler<DownloadItem>? DownloadStarted;
    public event EventHandler<DownloadItem>? DownloadProgressChanged;
    public event EventHandler<DownloadItem>? DownloadCompleted;
    public event EventHandler<DownloadItem>? DownloadFailed;
    public event EventHandler<DownloadItem>? DownloadCancelled;

    public DownloadService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(30);
    }

    public IReadOnlyList<DownloadItem> GetAllDownloads()
    {
        return _downloads.Values.OrderByDescending(d => d.StartTime).ToList();
    }

    public DownloadItem? GetById(long id)
    {
        _downloads.TryGetValue(id, out var item);
        return item;
    }

    public async Task<DownloadItem> StartDownloadAsync(DownloadRequest request)
    {
        var id = Interlocked.Increment(ref _nextId);
        var filePath = request.FilePath;

        if (string.IsNullOrEmpty(filePath))
        {
            filePath = Path.Combine(_defaultDownloadPath, request.FileName);
        }

        // 确保目录存在
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 如果文件已存在，添加数字后缀
        filePath = GetUniqueFilePath(filePath);

        var item = new DownloadItem
        {
            Id = id,
            Url = request.Url,
            FileName = Path.GetFileName(filePath),
            FilePath = filePath,
            MimeType = request.MimeType,
            Status = DownloadStatus.InProgress,
            StartTime = DateTime.UtcNow
        };

        _downloads.TryAdd(id, item);
        DownloadStarted?.Invoke(this, item);

        var cts = new CancellationTokenSource();
        _cancellationTokens.TryAdd(id, cts);

        _ = Task.Run(() => DownloadFileAsync(item, cts.Token));

        return await Task.FromResult(item);
    }

    private async Task DownloadFileAsync(DownloadItem item, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(item.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            if (item.TotalBytes == 0)
            {
                item.TotalBytes = response.Content.Headers.ContentLength ?? 0;
            }

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(item.FilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;
            var lastProgressUpdate = DateTime.UtcNow;
            long lastBytesReceived = 0;

            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalRead += bytesRead;
                item.ReceivedBytes = totalRead;

                // 更新进度（每200ms更新一次）
                if ((DateTime.UtcNow - lastProgressUpdate).TotalMilliseconds > 200)
                {
                    var elapsed = (DateTime.UtcNow - lastProgressUpdate).TotalSeconds;
                    var bytesSinceLastUpdate = totalRead - lastBytesReceived;
                    item.SpeedText = $"{FormatBytes((long)(bytesSinceLastUpdate / elapsed))}/s";

                    DownloadProgressChanged?.Invoke(this, item);
                    lastProgressUpdate = DateTime.UtcNow;
                    lastBytesReceived = totalRead;
                }
            }

            item.Status = DownloadStatus.Completed;
            item.EndTime = DateTime.UtcNow;
            DownloadCompleted?.Invoke(this, item);
        }
        catch (OperationCanceledException)
        {
            item.Status = DownloadStatus.Cancelled;
            item.EndTime = DateTime.UtcNow;
            DownloadCancelled?.Invoke(this, item);
        }
        catch (Exception ex)
        {
            item.Status = DownloadStatus.Failed;
            item.ErrorMessage = ex.Message;
            item.EndTime = DateTime.UtcNow;
            DownloadFailed?.Invoke(this, item);
        }
        finally
        {
            _cancellationTokens.TryRemove(item.Id, out _);
        }
    }

    public async Task PauseDownloadAsync(long id)
    {
        if (_cancellationTokens.TryRemove(id, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }

        if (_downloads.TryGetValue(id, out var item))
        {
            item.Status = DownloadStatus.Paused;
            item.IsPaused = true;
        }

        await Task.CompletedTask;
    }

    public async Task ResumeDownloadAsync(long id)
    {
        if (_downloads.TryGetValue(id, out var item) && item.Status == DownloadStatus.Paused)
        {
            // 重新开始下载
            item.Status = DownloadStatus.InProgress;
            item.IsPaused = false;

            var cts = new CancellationTokenSource();
            _cancellationTokens.TryAdd(id, cts);

            _ = Task.Run(() => DownloadFileAsync(item, cts.Token));
        }

        await Task.CompletedTask;
    }

    public async Task CancelDownloadAsync(long id)
    {
        if (_cancellationTokens.TryRemove(id, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }

        if (_downloads.TryGetValue(id, out var item))
        {
            item.Status = DownloadStatus.Cancelled;
            item.EndTime = DateTime.UtcNow;
            DownloadCancelled?.Invoke(this, item);
        }

        await Task.CompletedTask;
    }

    public async Task RemoveDownloadAsync(long id)
    {
        if (_downloads.TryRemove(id, out _))
        {
            _cancellationTokens.TryRemove(id, out _);
        }

        await Task.CompletedTask;
    }

    public async Task ClearCompletedDownloadsAsync()
    {
        var completed = _downloads.Values
            .Where(d => d.Status == DownloadStatus.Completed ||
                       d.Status == DownloadStatus.Failed ||
                       d.Status == DownloadStatus.Cancelled)
            .Select(d => d.Id)
            .ToList();

        foreach (var id in completed)
        {
            _downloads.TryRemove(id, out _);
        }

        await Task.CompletedTask;
    }

    public Task<string> GetDefaultDownloadPathAsync()
    {
        return Task.FromResult(_defaultDownloadPath);
    }

    public Task SetDefaultDownloadPathAsync(string path)
    {
        _defaultDownloadPath = path;
        return Task.CompletedTask;
    }

    private string GetUniqueFilePath(string filePath)
    {
        if (!File.Exists(filePath))
            return filePath;

        var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);
        int counter = 1;

        string newFilePath;
        do
        {
            newFilePath = Path.Combine(directory, $"{fileName} ({counter}){extension}");
            counter++;
        } while (File.Exists(newFilePath));

        return newFilePath;
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}
