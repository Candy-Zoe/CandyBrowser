using CommunityToolkit.Mvvm.ComponentModel;
using CandyBrowser.Core.Models;

namespace CandyBrowser.Windows.ViewModels;

public partial class DownloadItemViewModel : ObservableObject
{
    private readonly DownloadItem _item;

    [ObservableProperty]
    private long _id;

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private long _totalBytes;

    [ObservableProperty]
    private long _receivedBytes;

    [ObservableProperty]
    private DownloadStatus _status;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private string _progressText = string.Empty;

    [ObservableProperty]
    private string _speedText = string.Empty;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private bool _isCompleted;

    [ObservableProperty]
    private bool _isInProgress;

    [ObservableProperty]
    private bool _isPaused;

    public DownloadItemViewModel(DownloadItem item)
    {
        _item = item;
        UpdateFromItem();
    }

    public void UpdateFromItem()
    {
        Id = _item.Id;
        Url = _item.Url;
        FileName = _item.FileName;
        FilePath = _item.FilePath;
        TotalBytes = _item.TotalBytes;
        ReceivedBytes = _item.ReceivedBytes;
        Status = _item.Status;
        ProgressPercentage = _item.ProgressPercentage;
        SpeedText = _item.SpeedText;

        StatusText = _item.Status switch
        {
            DownloadStatus.Pending => "等待中",
            DownloadStatus.InProgress => "下载中",
            DownloadStatus.Completed => "已完成",
            DownloadStatus.Failed => "失败",
            DownloadStatus.Cancelled => "已取消",
            DownloadStatus.Paused => "已暂停",
            _ => "未知"
        };

        ProgressText = _item.Status switch
        {
            DownloadStatus.Completed => FormatBytes(_item.TotalBytes),
            DownloadStatus.Failed => _item.ErrorMessage ?? "下载失败",
            DownloadStatus.Cancelled => "已取消",
            _ => _item.ProgressText
        };

        IsCompleted = _item.Status == DownloadStatus.Completed;
        IsInProgress = _item.Status == DownloadStatus.InProgress;
        IsPaused = _item.Status == DownloadStatus.Paused;
    }

    public DownloadItem GetItem() => _item;

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
