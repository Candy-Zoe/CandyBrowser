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
        StartupContinueCheck.IsChecked = App.Settings.RestoreOnStartup;
        StartupHomeCheck.IsChecked = !App.Settings.RestoreOnStartup;
        ThemeLight.IsChecked = App.Settings.Theme == "light";
        ThemeDark.IsChecked = App.Settings.Theme == "dark";
        DoNotTrackCheck.IsChecked = App.Settings.DoNotTrack;
        BlockPopupsCheck.IsChecked = App.Settings.BlockPopups;
        ClearOnExitCheck.IsChecked = App.Settings.ClearOnExit;
    }

    private void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        App.Settings.Homepage = HomepageBox.Text.Trim();
        App.Settings.SearchEngine = SearchBox.Text.Trim();
        App.Settings.ShowBookmarksBar = BookmarksCheck.IsChecked == true;
        App.Settings.RestoreOnStartup = StartupContinueCheck.IsChecked == true;
        App.Settings.Theme = ThemeDark.IsChecked == true ? "dark" : "light";
        App.Settings.DoNotTrack = DoNotTrackCheck.IsChecked == true;
        App.Settings.BlockPopups = BlockPopupsCheck.IsChecked == true;
        App.Settings.ClearOnExit = ClearOnExitCheck.IsChecked == true;
        App.SaveSettings();
        MessageBox.Show("设置已保存", "提示");
        DialogResult = true;
    }

    private void ClearDataBtn_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("确定要清除所有浏览数据吗？\n（历史记录、缓存将被清除，书签保留）",
            "清除浏览数据", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            App.ClearHistory();
            MessageBox.Show("浏览数据已清除", "提示");
        }
    }
}
