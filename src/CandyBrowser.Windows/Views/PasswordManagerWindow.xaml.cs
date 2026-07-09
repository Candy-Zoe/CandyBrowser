using System.Windows;
using System.Windows.Controls;
using CandyBrowser.Shared.Abstractions;
using CandyBrowser.Core.Models;

namespace CandyBrowser.Windows.Views;

public partial class PasswordManagerWindow : Window
{
    private readonly IPasswordService _passwordService;
    private PasswordEntry? _selectedPassword;

    public PasswordManagerWindow(IPasswordService passwordService)
    {
        InitializeComponent();
        _passwordService = passwordService;
        Loaded += PasswordManagerWindow_Loaded;
    }

    private async void PasswordManagerWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadPasswordsAsync();
    }

    private async Task LoadPasswordsAsync()
    {
        var passwords = await _passwordService.GetAllAsync();
        PasswordGrid.ItemsSource = passwords;
    }

    private void PasswordGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedPassword = PasswordGrid.SelectedItem as PasswordEntry;
    }

    private async void AddPassword_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PasswordEditDialog(_passwordService)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadPasswordsAsync();
        }
    }

    private async void EditPassword_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPassword == null) return;

        var dialog = new PasswordEditDialog(_passwordService, _selectedPassword)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadPasswordsAsync();
        }
    }

    private async void DeletePassword_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPassword == null) return;

        var result = MessageBox.Show(
            $"确定要删除 {_selectedPassword.Domain} 的密码吗？",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            await _passwordService.DeleteAsync(_selectedPassword.Id);
            await LoadPasswordsAsync();
        }
    }

    private void CopyUsername_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPassword != null)
        {
            Clipboard.SetText(_selectedPassword.Username);
        }
    }

    private async void CopyPassword_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPassword != null)
        {
            var decryptedPassword = await _passwordService.DecryptAsync(_selectedPassword.Password);
            Clipboard.SetText(decryptedPassword);
        }
    }
}

// 密码编辑对话框
public class PasswordEditDialog : Window
{
    private readonly IPasswordService _passwordService;
    private readonly PasswordEntry? _existingPassword;
    private readonly TextBox _domainBox;
    private readonly TextBox _usernameBox;
    private readonly TextBox _passwordBox;
    private readonly TextBox _urlBox;

    public PasswordEditDialog(IPasswordService passwordService, PasswordEntry? existingPassword = null)
    {
        _passwordService = passwordService;
        _existingPassword = existingPassword;

        Title = existingPassword == null ? "添加密码" : "编辑密码";
        Width = 400;
        Height = 280;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var panel = new StackPanel { Margin = new Thickness(16) };

        panel.Children.Add(new TextBlock { Text = "网站：", Margin = new Thickness(0, 0, 0, 4) });
        _domainBox = new TextBox { Text = existingPassword?.Domain ?? string.Empty, Margin = new Thickness(0, 0, 0, 12) };
        panel.Children.Add(_domainBox);

        panel.Children.Add(new TextBlock { Text = "用户名：", Margin = new Thickness(0, 0, 0, 4) });
        _usernameBox = new TextBox { Text = existingPassword?.Username ?? string.Empty, Margin = new Thickness(0, 0, 0, 12) };
        panel.Children.Add(_usernameBox);

        panel.Children.Add(new TextBlock { Text = "密码：", Margin = new Thickness(0, 0, 0, 4) });
        var passwordPanel = new StackPanel { Orientation = Orientation.Horizontal };
        _passwordBox = new TextBox { Width = 280, Margin = new Thickness(0, 0, 8, 0) };
        var generateButton = new Button
        {
            Content = "生成",
            Padding = new Thickness(8, 4, 8, 4)
        };
        generateButton.Click += GeneratePassword_Click;
        passwordPanel.Children.Add(_passwordBox);
        passwordPanel.Children.Add(generateButton);
        panel.Children.Add(passwordPanel);
        panel.Children.Add(new TextBlock { Height = 12 });

        panel.Children.Add(new TextBlock { Text = "网址：", Margin = new Thickness(0, 0, 0, 4) });
        _urlBox = new TextBox { Text = existingPassword?.Url ?? string.Empty, Margin = new Thickness(0, 0, 0, 16) };
        panel.Children.Add(_urlBox);

        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

        var saveButton = new Button
        {
            Content = "保存",
            Width = 80,
            Margin = new Thickness(0, 0, 8, 0),
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x6B, 0x9D)),
            Foreground = System.Windows.Media.Brushes.White
        };
        saveButton.Click += SaveButton_Click;
        buttonPanel.Children.Add(saveButton);

        var cancelButton = new Button
        {
            Content = "取消",
            Width = 80,
            IsCancel = true
        };
        buttonPanel.Children.Add(cancelButton);

        panel.Children.Add(buttonPanel);
        Content = panel;
    }

    private async void GeneratePassword_Click(object sender, RoutedEventArgs e)
    {
        var password = await _passwordService.GeneratePasswordAsync(16, true);
        _passwordBox.Text = password;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_domainBox.Text) ||
            string.IsNullOrWhiteSpace(_usernameBox.Text) ||
            string.IsNullOrWhiteSpace(_passwordBox.Text))
        {
            MessageBox.Show("请填写所有必填字段", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var password = new PasswordEntry
        {
            Id = _existingPassword?.Id ?? 0,
            Domain = _domainBox.Text,
            Username = _usernameBox.Text,
            Password = _passwordBox.Text,
            Url = _urlBox.Text
        };

        await _passwordService.SaveAsync(password);
        DialogResult = true;
    }
}
