using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CandyBrowser.Shared.Abstractions;
using CandyBrowser.Core.Models;

namespace CandyBrowser.Windows.ViewModels;

public partial class DownloadManagerViewModel : ObservableObject
{
    private readonly IDownloadService _downloadService;

    [ObservableProperty]
    private string _downloadCountText = string.Empty;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private bool _isEmpty = true;

    public ObservableCollection<DownloadItemViewModel> Downloads { get; } = new();

    public DownloadManagerViewModel(IDownloadService downloadService)
    {
        _downloadService = downloadService;

        _downloadService.DownloadStarted += OnDownloadStarted;
        _downloadService.DownloadProgressChanged += OnDownloadProgressChanged;
        _downloadService.DownloadCompleted += OnDownloadCompleted;
        _downloadService.DownloadFailed += OnDownloadFailed;
        _downloadService.DownloadCancelled += OnDownloadCancelled;
    }

    public void LoadDownloads()
    {
        Downloads.Clear();
        foreach (var item in _downloadService.GetAllDownloads())
        {
            Downloads.Add(new DownloadItemViewModel(item));
        }
        UpdateStatus();
    }

    private void OnDownloadStarted(object? sender, DownloadItem item)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            var vm = new DownloadItemViewModel(item);
            Downloads.Insert(0, vm);
            UpdateStatus();
        });
    }

    private void OnDownloadProgressChanged(object? sender, DownloadItem item)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            var vm = Downloads.FirstOrDefault(d => d.Id == item.Id);
            vm?.UpdateFromItem();
        });
    }

    private void OnDownloadCompleted(object? sender, DownloadItem item)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            var vm = Downloads.FirstOrDefault(d => d.Id == item.Id);
            vm?.UpdateFromItem();
            UpdateStatus();
        });
    }

    private void OnDownloadFailed(object? sender, DownloadItem item)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            var vm = Downloads.FirstOrDefault(d => d.Id == item.Id);
            vm?.UpdateFromItem();
            UpdateStatus();
        });
    }

    private void OnDownloadCancelled(object? sender, DownloadItem item)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            var vm = Downloads.FirstOrDefault(d => d.Id == item.Id);
            vm?.UpdateFromItem();
            UpdateStatus();
        });
    }

    private void UpdateStatus()
    {
        var total = Downloads.Count;
        var completed = Downloads.Count(d => d.Status == DownloadStatus.Completed);
        var inProgress = Downloads.Count(d => d.Status == DownloadStatus.InProgress);

        DownloadCountText = $"共 {total} 个下载任务";
        IsEmpty = total == 0;

        if (inProgress > 0)
        {
            StatusText = $"正在下载 {inProgress} 个文件";
        }
        else if (completed > 0)
        {
            StatusText = $"已完成 {completed} 个下载";
        }
        else
        {
            StatusText = "就绪";
        }
    }

    [RelayCommand]
    private void OpenDownloadFolder()
    {
        var path = _downloadService.GetDefaultDownloadPathAsync().Result;
        if (Directory.Exists(path))
        {
            Process.Start("explorer.exe", path);
        }
    }

    [RelayCommand]
    private void OpenFile(DownloadItemViewModel? item)
    {
        if (item == null) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = item.FilePath,
                UseShellExecute = true
            });
        }
        catch { }
    }

    [RelayCommand]
    private void OpenFolder(DownloadItemViewModel? item)
    {
        if (item == null) return;

        var directory = Path.GetDirectoryName(item.FilePath);
        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
        {
            Process.Start("explorer.exe", $"/select,\"{item.FilePath}\"");
        }
    }

    [RelayCommand]
    private async Task PauseResumeAsync(DownloadItemViewModel? item)
    {
        if (item == null) return;

        if (item.Status == DownloadStatus.InProgress)
        {
            await _downloadService.PauseDownloadAsync(item.Id);
        }
        else if (item.Status == DownloadStatus.Paused)
        {
            await _downloadService.ResumeDownloadAsync(item.Id);
        }

        item.UpdateFromItem();
    }

    [RelayCommand]
    private async Task CancelRemoveAsync(DownloadItemViewModel? item)
    {
        if (item == null) return;

        if (item.Status == DownloadStatus.InProgress || item.Status == DownloadStatus.Paused)
        {
            await _downloadService.CancelDownloadAsync(item.Id);
            item.UpdateFromItem();
        }
        else
        {
            await _downloadService.RemoveDownloadAsync(item.Id);
            Downloads.Remove(item);
            UpdateStatus();
        }
    }

    [RelayCommand]
    private async Task ClearCompletedAsync()
    {
        await _downloadService.ClearCompletedDownloadsAsync();
        var completed = Downloads.Where(d =>
            d.Status == DownloadStatus.Completed ||
            d.Status == DownloadStatus.Failed ||
            d.Status == DownloadStatus.Cancelled).ToList();

        foreach (var item in completed)
        {
            Downloads.Remove(item);
        }
        UpdateStatus();
    }

    public void Dispose()
    {
        _downloadService.DownloadStarted -= OnDownloadStarted;
        _downloadService.DownloadProgressChanged -= OnDownloadProgressChanged;
        _downloadService.DownloadCompleted -= OnDownloadCompleted;
        _downloadService.DownloadFailed -= OnDownloadFailed;
        _downloadService.DownloadCancelled -= OnDownloadCancelled;
    }
}
