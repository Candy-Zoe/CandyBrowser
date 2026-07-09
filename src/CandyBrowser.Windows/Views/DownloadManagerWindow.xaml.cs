using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using CandyBrowser.Windows.ViewModels;
using CandyBrowser.Shared.Abstractions;

namespace CandyBrowser.Windows.Views;

public partial class DownloadManagerWindow : Window
{
    private readonly DownloadManagerViewModel _viewModel;

    public DownloadManagerWindow(IDownloadService downloadService)
    {
        InitializeComponent();
        _viewModel = new DownloadManagerViewModel(downloadService);
        DataContext = _viewModel;
        Loaded += DownloadManagerWindow_Loaded;
        Closed += DownloadManagerWindow_Closed;
    }

    private void DownloadManagerWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _viewModel.LoadDownloads();
    }

    private void DownloadManagerWindow_Closed(object? sender, EventArgs e)
    {
        _viewModel.Dispose();
    }

    private void OpenDownloadFolder_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.OpenDownloadFolderCommand.Execute(null);
    }

    private void ClearCompleted_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ClearCompletedCommand.Execute(null);
    }

    private void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is DownloadItemViewModel item)
        {
            _viewModel.OpenFileCommand.Execute(item);
        }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is DownloadItemViewModel item)
        {
            _viewModel.OpenFolderCommand.Execute(item);
        }
    }

    private void PauseResume_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is DownloadItemViewModel item)
        {
            _viewModel.PauseResumeCommand.Execute(item);
        }
    }

    private void CancelRemove_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is DownloadItemViewModel item)
        {
            _viewModel.CancelRemoveCommand.Execute(item);
        }
    }
}
