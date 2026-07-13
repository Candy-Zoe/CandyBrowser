using System.Windows;

namespace CandyBrowser.Windows.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        HomepageBox.Text = App.Settings.Homepage;
        SearchBox.Text = App.Settings.SearchEngine;
        BookmarksCheck.IsChecked = App.Settings.ShowBookmarksBar;
    }

    private void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        App.Settings.Homepage = HomepageBox.Text;
        App.Settings.SearchEngine = SearchBox.Text;
        App.Settings.ShowBookmarksBar = BookmarksCheck.IsChecked == true;
        App.SaveSettings();
        MessageBox.Show("已保存", "提示");
        DialogResult = true;
    }
}
