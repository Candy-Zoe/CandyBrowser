using System.Windows;
using System.Windows.Controls;
using CandyBrowser.Shared.Abstractions;

namespace CandyBrowser.Windows.Views;

public partial class SettingsWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly IHistoryService _historyService;
    private readonly IPasswordService _passwordService;

    public SettingsWindow(ISettingsService settingsService, IHistoryService historyService, IPasswordService passwordService)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _historyService = historyService;
        _passwordService = passwordService;
        Loaded += SettingsWindow_Loaded;
    }

    private async void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        // 加载常规设置
        var homepage = await _settingsService.GetHomepageAsync();
        HomepageTextBox.Text = homepage;

        var newTabUrl = await _settingsService.GetNewTabUrlAsync();
        NewTabTextBox.Text = newTabUrl;

        // 加载搜索引擎设置
        var searchEngine = await _settingsService.GetSearchEngineAsync();
        foreach (ComboBoxItem item in SearchEngineComboBox.Items)
        {
            if (item.Tag?.ToString() == searchEngine)
            {
                item.IsSelected = true;
                break;
            }
        }

        // 加载外观设置
        var fontSize = await _settingsService.GetAsync("font_size", "16");
        if (int.TryParse(fontSize, out int size))
        {
            FontSizeSlider.Value = size;
            FontSizeLabel.Text = $"{size}px";
        }

        var showBookmarksBar = await _settingsService.GetAsync("show_bookmarks_bar", "true");
        ShowBookmarksBarCheckBox.IsChecked = showBookmarksBar?.ToLower() == "true";

        var showStatusBar = await _settingsService.GetAsync("show_status_bar", "true");
        ShowStatusBarCheckBox.IsChecked = showStatusBar?.ToLower() == "true";
    }

    private void SettingsMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SettingsMenu.SelectedItem is ListBoxItem item)
        {
            var tag = item.Tag?.ToString();
            HideAllSettings();

            switch (tag)
            {
                case "General":
                    GeneralSettings.Visibility = Visibility.Visible;
                    break;
                case "Appearance":
                    AppearanceSettings.Visibility = Visibility.Visible;
                    break;
                case "Privacy":
                    PrivacySettings.Visibility = Visibility.Visible;
                    break;
                case "Search":
                    SearchSettings.Visibility = Visibility.Visible;
                    break;
                case "About":
                    AboutSettings.Visibility = Visibility.Visible;
                    break;
            }
        }
    }

    private void HideAllSettings()
    {
        GeneralSettings.Visibility = Visibility.Collapsed;
        AppearanceSettings.Visibility = Visibility.Collapsed;
        PrivacySettings.Visibility = Visibility.Collapsed;
        SearchSettings.Visibility = Visibility.Collapsed;
        AboutSettings.Visibility = Visibility.Collapsed;
    }

    private async void SaveGeneralSettings_Click(object sender, RoutedEventArgs e)
    {
        await _settingsService.SetAsync("homepage", HomepageTextBox.Text);
        await _settingsService.SetAsync("new_tab_url", NewTabTextBox.Text);

        MessageBox.Show("设置已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void SaveAppearanceSettings_Click(object sender, RoutedEventArgs e)
    {
        await _settingsService.SetAsync("font_size", ((int)FontSizeSlider.Value).ToString());
        await _settingsService.SetAsync("show_bookmarks_bar", ShowBookmarksBarCheckBox.IsChecked.ToString() ?? "true");
        await _settingsService.SetAsync("show_status_bar", ShowStatusBarCheckBox.IsChecked.ToString() ?? "true");

        MessageBox.Show("设置已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void SaveSearchSettings_Click(object sender, RoutedEventArgs e)
    {
        if (SearchEngineComboBox.SelectedItem is ComboBoxItem item)
        {
            var searchEngineUrl = item.Tag?.ToString();
            if (!string.IsNullOrEmpty(searchEngineUrl))
            {
                await _settingsService.SetAsync("search_engine", searchEngineUrl);
                MessageBox.Show("设置已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    private async void ClearBrowsingData_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("确定要清除浏览数据吗？此操作不可撤销。",
            "确认清除", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            await _historyService.ClearAsync();
            MessageBox.Show("浏览数据已清除", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ManagePasswords_Click(object sender, RoutedEventArgs e)
    {
        // TODO: 打开密码管理器窗口
        MessageBox.Show("密码管理器功能即将推出", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BrowseDownloadPath_Click(object sender, RoutedEventArgs e)
    {
        // 使用默认下载文件夹
        var downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
        DownloadPathTextBox.Text = downloadsPath;
    }

    private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FontSizeLabel != null)
        {
            FontSizeLabel.Text = $"{(int)e.NewValue}px";
        }
    }
}
