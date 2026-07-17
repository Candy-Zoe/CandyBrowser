using System.Windows;
using System.Windows.Controls;

namespace CandyBrowser.Windows.Views;

public partial class HistoryWindow : Window
{
    public HistoryWindow()
    {
        InitializeComponent();
        LoadHistory();
    }

    private void LoadHistory(string? filter = null)
    {
        HistoryList.Items.Clear();
        var items = App.History.Take(200).AsEnumerable();
        if (!string.IsNullOrEmpty(filter) && filter != "搜索历史记录...")
        {
            items = items.Where(h =>
                h.Title.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                h.Url.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }
        foreach (var h in items)
            HistoryList.Items.Add(h);
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
}
