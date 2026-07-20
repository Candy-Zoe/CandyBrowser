using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Web.WebView2.Wpf;
using CandyBrowser.Shared.Abstractions;
using Models = CandyBrowser.Core.Models;

namespace CandyBrowser.Windows.Views;

public partial class HistoryWindow : Window
{
    private readonly IHistoryService _historyService;
    private readonly WebView2 _webView;
    private readonly Dictionary<ListBoxItem, CheckBox> _checkBoxes = new();

    public HistoryWindow(IHistoryService historyService, WebView2 webView)
    {
        InitializeComponent();
        _historyService = historyService;
        _webView = webView;
        Loaded += async (s, e) => await LoadHistory();
    }

    private async Task LoadHistory(string? filter = null)
    {
        HistoryList.Items.Clear();
        _checkBoxes.Clear();
        try
        {
            var items = await _historyService.GetAllAsync(limit: 500);
            if (!string.IsNullOrEmpty(filter) && filter != "搜索历史记录...")
            {
                items = items.Where(h =>
                    h.Title.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    h.Url.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            foreach (var h in items)
            {
                var listBoxItem = CreateHistoryListItem(h);
                HistoryList.Items.Add(listBoxItem);
            }
        }
        catch { }
        UpdateSelectedCount();
    }

    private ListBoxItem CreateHistoryListItem(Models.HistoryEntry entry)
    {
        var border = new Border
        {
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 1, 0, 1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Cursor = Cursors.Hand
        };

        var panel = new DockPanel();

        // Checkbox
        var checkBox = new CheckBox
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
            ClickMode = ClickMode.Press
        };
        checkBox.Checked += HistoryCheck_Changed;
        checkBox.Unchecked += HistoryCheck_Changed;
        DockPanel.SetDock(checkBox, Dock.Left);
        panel.Children.Add(checkBox);

        // Title
        var titleText = new TextBlock
        {
            Text = entry.Title,
            FontSize = 12,
            FontFamily = new FontFamily("Microsoft YaHei UI"),
            Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        DockPanel.SetDock(titleText, Dock.Top);
        panel.Children.Add(titleText);

        // URL
        var urlText = new TextBlock
        {
            Text = entry.Url,
            FontSize = 10,
            FontFamily = new FontFamily("Microsoft YaHei UI"),
            Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        DockPanel.SetDock(urlText, Dock.Bottom);
        panel.Children.Add(urlText);

        // Date
        var dateText = new TextBlock
        {
            Text = entry.LastVisit.ToString("yyyy-MM-dd HH:mm"),
            FontSize = 10,
            FontFamily = new FontFamily("Microsoft YaHei UI"),
            Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)),
            VerticalAlignment = VerticalAlignment.Center
        };
        DockPanel.SetDock(dateText, Dock.Right);
        panel.Children.Add(dateText);

        border.Child = panel;

        var clickedEntry = entry;
        border.MouseLeftButtonDown += (s, e) =>
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 1)
                OpenHistoryItem(clickedEntry);
        };

        var listBoxItem = new ListBoxItem
        {
            Content = border,
            Tag = entry
        };
        _checkBoxes[listBoxItem] = checkBox;

        return listBoxItem;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _ = LoadHistory(SearchBox.Text);
    }

    private async void ClearBtn_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("确定清除所有历史记录？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            await _historyService.ClearAsync();
            await LoadHistory();
        }
    }

    /// <summary>
    /// 在当前 WebView 中导航到指定 URL
    /// </summary>
    private void NavigateInWebView(string url)
    {
        if (_webView?.CoreWebView2 != null && !string.IsNullOrEmpty(url))
        {
            _webView.CoreWebView2.Navigate(url);
        }
    }

    private void OpenHistoryItem(Models.HistoryEntry item)
    {
        if (!string.IsNullOrEmpty(item.Url))
        {
            NavigateInWebView(item.Url);
            DialogResult = true;
        }
    }

    private void HistoryCheck_Changed(object sender, RoutedEventArgs e)
    {
        UpdateSelectedCount();
    }

    private void SelectAll_Changed(object sender, RoutedEventArgs e)
    {
        var isChecked = SelectAllCheckBox.IsChecked == true;
        foreach (var cb in _checkBoxes.Values)
            cb.IsChecked = isChecked;
        UpdateSelectedCount();
    }

    private void UpdateSelectedCount()
    {
        var count = _checkBoxes.Count(kvp => kvp.Value.IsChecked == true);
        SelectedCountText.Text = $"已选 {count} 项";
    }

    private List<Models.HistoryEntry> GetSelectedItems()
    {
        var result = new List<Models.HistoryEntry>();
        foreach (var kvp in _checkBoxes)
        {
            if (kvp.Value.IsChecked == true && kvp.Key.Tag is Models.HistoryEntry historyItem)
                result.Add(historyItem);
        }
        return result;
    }

    private async void BatchOpenBtn_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedItems();
        if (selected.Count == 0)
        {
            MessageBox.Show("请先勾选要打开的记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // 打开第一条记录到当前标签页
        if (selected.Count == 1)
        {
            NavigateInWebView(selected[0].Url);
            DialogResult = true;
        }
        else
        {
            MessageBox.Show("批量打开暂不支持多条记录，请逐条点击打开", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void BatchDeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedItems();
        if (selected.Count == 0)
        {
            MessageBox.Show("请先勾选要删除的记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"确定删除选中 {selected.Count} 条历史记录？",
            "确认批量删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            foreach (var item in selected)
                await _historyService.DeleteAsync(item.Id);
            await LoadHistory(SearchBox.Text);
            MessageBox.Show($"已删除 {selected.Count} 条记录", "完成");
        }
    }
}
