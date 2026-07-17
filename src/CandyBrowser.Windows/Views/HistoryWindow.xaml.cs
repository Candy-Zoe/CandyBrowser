using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CandyBrowser.Windows.Views;

public partial class HistoryWindow : Window
{
    private readonly Dictionary<HistoryItem, CheckBox> _checkBoxes = new();

    public HistoryWindow()
    {
        InitializeComponent();
        LoadHistory();
    }

    private void LoadHistory(string? filter = null)
    {
        HistoryList.Items.Clear();
        _checkBoxes.Clear();
        var items = App.History.Take(200).AsEnumerable();
        if (!string.IsNullOrEmpty(filter) && filter != "搜索历史记录...")
        {
            items = items.Where(h =>
                h.Title.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                h.Url.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }
        foreach (var h in items)
            HistoryList.Items.Add(h);
        UpdateSelectedCount();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        LoadHistory(SearchBox.Text);
    }

    private void ClearBtn_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("确定清除所有历史记录?", "确认", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            App.ClearHistory();
            LoadHistory();
        }
    }

    #region Click to Open

    private void HistoryItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is HistoryItem item)
        {
            OpenHistoryItem(item);
        }
    }

    private void OpenHistoryItem(HistoryItem item)
    {
        if (!string.IsNullOrEmpty(item.Url))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(item.Url) { UseShellExecute = true });
        }
    }

    #endregion

    #region Checkbox Operations

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
        // 由于 DataTemplate 中的 CheckBox 无法直接引用，我们需要通过视觉树查找
        var count = 0;
        foreach (var item in HistoryList.Items)
        {
            var listBoxItem = HistoryList.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
            if (listBoxItem != null)
            {
                var checkBox = FindVisualChild<CheckBox>(listBoxItem);
                if (checkBox?.IsChecked == true)
                    count++;
            }
        }
        SelectedCountText.Text = $"已选 {count} 项";
    }

    private List<HistoryItem> GetSelectedItems()
    {
        var result = new List<HistoryItem>();
        foreach (var item in HistoryList.Items)
        {
            var listBoxItem = HistoryList.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
            if (listBoxItem != null)
            {
                var checkBox = FindVisualChild<CheckBox>(listBoxItem);
                if (checkBox?.IsChecked == true && item is HistoryItem historyItem)
                    result.Add(historyItem);
            }
        }
        return result;
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T result)
                return result;
            var descendant = FindVisualChild<T>(child);
            if (descendant != null)
                return descendant;
        }
        return null;
    }

    #endregion

    #region Batch Operations

    private void BatchOpenBtn_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedItems();
        if (selected.Count == 0)
        {
            MessageBox.Show("请先勾选要打开的记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        foreach (var item in selected)
        {
            if (!string.IsNullOrEmpty(item.Url))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(item.Url) { UseShellExecute = true });
        }
    }

    private void BatchDeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedItems();
        if (selected.Count == 0)
        {
            MessageBox.Show("请先勾选要删除的记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"确定删除选中的 {selected.Count} 条历史记录？",
            "确认批量删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            foreach (var item in selected)
                App.RemoveHistory(item.Url);
            LoadHistory(SearchBox.Text);
            MessageBox.Show($"已删除 {selected.Count} 条记录", "完成");
        }
    }

    #endregion
}
