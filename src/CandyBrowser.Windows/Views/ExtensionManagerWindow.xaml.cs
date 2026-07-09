using System.Windows;
using System.Windows.Controls;
using CandyBrowser.Shared.Abstractions;
using CandyBrowser.Core.Models;

namespace CandyBrowser.Windows.Views;

public partial class ExtensionManagerWindow : Window
{
    private readonly IExtensionService _extensionService;
    private ExtensionInfo? _selectedExtension;

    public ExtensionManagerWindow(IExtensionService extensionService)
    {
        InitializeComponent();
        _extensionService = extensionService;
        Loaded += ExtensionManagerWindow_Loaded;
    }

    private async void ExtensionManagerWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadExtensionsAsync();
    }

    private async Task LoadExtensionsAsync()
    {
        var extensions = await _extensionService.GetAllAsync();
        ExtensionsList.ItemsSource = extensions;
    }

    private void ExtensionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedExtension = ExtensionsList.SelectedItem as ExtensionInfo;
    }

    private async void LoadExtension_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择扩展 manifest.json",
            Filter = "JSON 文件|manifest.json|所有文件|*.*",
            FileName = "manifest.json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _extensionService.InstallExtensionAsync(dialog.FileName);
                await LoadExtensionsAsync();
                MessageBox.Show("扩展已安装", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"安装失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void ExtensionToggle_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is ExtensionInfo extension)
        {
            await _extensionService.EnableExtensionAsync(extension.ExtensionId);
        }
    }

    private async void ExtensionToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is ExtensionInfo extension)
        {
            await _extensionService.DisableExtensionAsync(extension.ExtensionId);
        }
    }

    private void ExtensionDetails_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedExtension != null)
        {
            MessageBox.Show(
                $"名称：{_selectedExtension.Name}\n" +
                $"版本：{_selectedExtension.Version}\n" +
                $"描述：{_selectedExtension.Description}\n" +
                $"安装路径：{_selectedExtension.InstallPath}",
                "扩展详情",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private async void ExtensionRemove_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedExtension == null) return;

        var result = MessageBox.Show(
            $"确定要移除 {_selectedExtension.Name} 吗？",
            "确认移除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            await _extensionService.UninstallExtensionAsync(_selectedExtension.ExtensionId);
            await LoadExtensionsAsync();
        }
    }
}
