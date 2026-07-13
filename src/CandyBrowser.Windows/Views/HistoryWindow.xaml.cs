using System.Windows;

namespace CandyBrowser.Windows.Views;

public partial class HistoryWindow : Window
{
    public HistoryWindow()
    {
        InitializeComponent();
        LoadHistory();
    }

    private void LoadHistory()
    {
        HistoryList.Items.Clear();
        foreach (var h in App.History.Take(100))
        {
            HistoryList.Items.Add($"{h.VisitedAt:MM-dd HH:mm} | {h.Title} | {h.Url}");
        }
    }

    private void ClearBtn_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("确定清除?", "确认", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            App.ClearHistory();
            LoadHistory();
        }
    }
}
