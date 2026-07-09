using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CandyBrowser.Windows.ViewModels;
using CandyBrowser.Core.Models;

namespace CandyBrowser.Windows.Views;

public partial class HistoryPanel : UserControl
{
    private HistoryEntry? _contextMenuTarget;

    public HistoryPanel()
    {
        InitializeComponent();
        Loaded += HistoryPanel_Loaded;
    }

    private async void HistoryPanel_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is HistoryViewModel vm)
        {
            await vm.LoadHistoryAsync();
        }
    }

    private void HistoryItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is HistoryEntry entry)
        {
            OpenInNewTab(entry);
        }
    }

    private void HistoryItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is HistoryEntry entry)
        {
            _contextMenuTarget = entry;
            if (DataContext is HistoryViewModel vm)
            {
                vm.SelectedEntry = entry;
            }
        }
    }

    private void OpenInNewTab(HistoryEntry entry)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            var mainVm = mainWindow.DataContext as MainViewModel;
            mainVm?.CreateNewTabWithUrl(entry.Url);
        }
    }

    private void OpenInNewTab_Click(object sender, RoutedEventArgs e)
    {
        if (_contextMenuTarget != null)
        {
            OpenInNewTab(_contextMenuTarget);
        }
    }

    private void OpenInNewWindow_Click(object sender, RoutedEventArgs e)
    {
        // TODO: 在新窗口中打开
        if (_contextMenuTarget != null)
        {
            OpenInNewTab(_contextMenuTarget);
        }
    }

    private void CopyLink_Click(object sender, RoutedEventArgs e)
    {
        if (_contextMenuTarget != null)
        {
            Clipboard.SetText(_contextMenuTarget.Url);
        }
    }

    private void DeleteSelected_Click(object sender, RoutedEventArgs e)
    {
        if (_contextMenuTarget != null && DataContext is HistoryViewModel vm)
        {
            vm.DeleteEntryCommand.Execute(_contextMenuTarget);
        }
    }

    private void ClearAllHistory_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "确定要清除所有历史记录吗？此操作不可撤销。",
            "确认清除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes && DataContext is HistoryViewModel vm)
        {
            vm.ClearHistoryCommand.Execute(null);
        }
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is HistoryViewModel vm)
        {
            vm.SearchCommand.Execute(null);
        }
    }
}
