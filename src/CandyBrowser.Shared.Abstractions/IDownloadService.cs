using CandyBrowser.Core.Models;

namespace CandyBrowser.Shared.Abstractions;

public interface IDownloadService
{
    event EventHandler<DownloadItem>? DownloadStarted;
    event EventHandler<DownloadItem>? DownloadProgressChanged;
    event EventHandler<DownloadItem>? DownloadCompleted;
    event EventHandler<DownloadItem>? DownloadFailed;
    event EventHandler<DownloadItem>? DownloadCancelled;

    IReadOnlyList<DownloadItem> GetAllDownloads();
    DownloadItem? GetById(long id);
    Task<DownloadItem> StartDownloadAsync(DownloadRequest request);
    Task PauseDownloadAsync(long id);
    Task ResumeDownloadAsync(long id);
    Task CancelDownloadAsync(long id);
    Task RemoveDownloadAsync(long id);
    Task ClearCompletedDownloadsAsync();
    Task<string> GetDefaultDownloadPathAsync();
    Task SetDefaultDownloadPathAsync(string path);
}
