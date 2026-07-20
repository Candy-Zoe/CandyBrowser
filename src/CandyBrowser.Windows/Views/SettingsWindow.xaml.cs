using System.Windows;
using CandyBrowser.Shared.Abstractions;

namespace CandyBrowser.Windows.Views;

public partial class SettingsWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly JsonSettingsProvider _jsonSettings;

    public SettingsWindow(ISettingsService settingsService, JsonSettingsProvider jsonSettings)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _jsonSettings = jsonSettings;

        // Load current settings
        HomepageBox.Text = _jsonSettings.Get("homepage", "https://www.baidu.com").Value;
        SearchBox.Text = _jsonSettings.Get("search_engine", "https://www.baidu.com/s?wd={0}").Value;
        BookmarksCheck.IsChecked = _jsonSettings.Get("show_bookmarks_bar", "true").Value == "true";
        ThemeLight.IsChecked = _jsonSettings.Get("theme", "light").Value == "light";
        ThemeDark.IsChecked = _jsonSettings.Get("theme", "light").Value == "dark";
        DoNotTrackCheck.IsChecked = _jsonSettings.Get("do_not_track", "false").Value == "true";
        BlockPopupsCheck.IsChecked = _jsonSettings.Get("block_popups", "true").Value == "true";
        ClearOnExitCheck.IsChecked = _jsonSettings.Get("clear_on_exit", "false").Value == "true";
    }

    private void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        _jsonSettings.Set("homepage", HomepageBox.Text.Trim());
        _jsonSettings.Set("search_engine", SearchBox.Text.Trim());
        _jsonSettings.Set("show_bookmarks_bar", (BookmarksCheck.IsChecked == true).ToString().ToLower());
        _jsonSettings.Set("theme", ThemeDark.IsChecked == true ? "dark" : "light");
        _jsonSettings.Set("do_not_track", (DoNotTrackCheck.IsChecked == true).ToString().ToLower());
        _jsonSettings.Set("block_popups", (BlockPopupsCheck.IsChecked == true).ToString().ToLower());
        _jsonSettings.Set("clear_on_exit", (ClearOnExitCheck.IsChecked == true).ToString().ToLower());

        MessageBox.Show("设置已保存", "提示");
        DialogResult = true;
    }

    private async void ClearDataBtn_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("确定要清除所有浏览数据吗？\n（历史记录、缓存将被清除，书签保留）",
            "清除浏览数据", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            var historyService = App.Resolve<IHistoryService>();
            await historyService.ClearAsync();
            MessageBox.Show("浏览数据已清除", "提示");
        }
    }
}
