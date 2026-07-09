using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CandyBrowser.Windows.ViewModels;

namespace CandyBrowser.Windows.Views;

public partial class BookmarkSidebar : UserControl
{
    private BookmarkItemViewModel? _contextMenuTarget;

    public BookmarkSidebar()
    {
        InitializeComponent();
        Loaded += BookmarkSidebar_Loaded;
    }

    private async void BookmarkSidebar_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is BookmarkViewModel vm)
        {
            await vm.LoadBookmarksAsync();
        }
    }

    private void BookmarksTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is BookmarkViewModel vm && e.NewValue is BookmarkItemViewModel item)
        {
            vm.SelectedBookmark = item;
        }
    }

    private void SearchResult_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is BookmarkItemViewModel item)
        {
            OpenInNewTab(item);
        }
    }

    private void BookmarkItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is BookmarkItemViewModel item)
        {
            _contextMenuTarget = item;
            if (DataContext is BookmarkViewModel vm)
            {
                vm.SelectedBookmark = item;
            }
        }
    }

    private void OpenInNewTab(BookmarkItemViewModel item)
    {
        if (item.IsFolder) return;

        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            var mainVm = mainWindow.DataContext as MainViewModel;
            mainVm?.CreateNewTabWithUrl(item.Url);
        }
    }

    private void OpenInNewTab_Click(object sender, RoutedEventArgs e)
    {
        if (_contextMenuTarget != null)
        {
            OpenInNewTab(_contextMenuTarget);
        }
    }

    private void AddBookmark_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddBookmarkDialog
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true)
        {
            if (DataContext is BookmarkViewModel vm)
            {
                vm.NewBookmarkUrl = dialog.Url;
                vm.NewBookmarkTitle = dialog.BookmarkTitle;
                vm.AddBookmarkCommand.Execute(null);
            }
        }
    }

    private void NewFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new InputDialog("新建文件夹", "请输入文件夹名称：", "新建文件夹")
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true)
        {
            if (DataContext is BookmarkViewModel vm)
            {
                vm.CreateFolderCommand.Execute(dialog.InputValue);
            }
        }
    }

    private void EditBookmark_Click(object sender, RoutedEventArgs e)
    {
        if (_contextMenuTarget == null || _contextMenuTarget.IsFolder) return;

        var dialog = new EditBookmarkDialog(_contextMenuTarget.Title, _contextMenuTarget.Url)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true && DataContext is BookmarkViewModel vm)
        {
            vm.SelectedBookmark = _contextMenuTarget;
            vm.EditTitle = dialog.BookmarkTitle;
            vm.EditUrl = dialog.Url;
            vm.UpdateBookmarkCommand.Execute(null);
        }
    }

    private void DeleteBookmark_Click(object sender, RoutedEventArgs e)
    {
        if (_contextMenuTarget == null) return;

        var result = MessageBox.Show(
            $"确定要删除 '{_contextMenuTarget.Title}' 吗？",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes && DataContext is BookmarkViewModel vm)
        {
            vm.DeleteBookmarkCommand.Execute(_contextMenuTarget);
        }
    }
}

// 添加书签对话框
public class AddBookmarkDialog : Window
{
    public string BookmarkTitle { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;

    private readonly TextBox _titleBox;
    private readonly TextBox _urlBox;

    public AddBookmarkDialog()
    {
        this.Title = "添加书签";
        Width = 400;
        Height = 200;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var panel = new StackPanel { Margin = new Thickness(16) };

        panel.Children.Add(new TextBlock { Text = "名称：", Margin = new Thickness(0, 0, 0, 4) });
        _titleBox = new TextBox { Margin = new Thickness(0, 0, 0, 12) };
        panel.Children.Add(_titleBox);

        panel.Children.Add(new TextBlock { Text = "网址：", Margin = new Thickness(0, 0, 0, 4) });
        _urlBox = new TextBox { Margin = new Thickness(0, 0, 0, 16) };
        panel.Children.Add(_urlBox);

        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

        var okButton = new Button
        {
            Content = "确定",
            Width = 80,
            Margin = new Thickness(0, 0, 8, 0),
            IsDefault = true
        };
        okButton.Click += (s, e) =>
        {
            BookmarkTitle = _titleBox.Text;
            Url = _urlBox.Text;
            DialogResult = true;
        };
        buttonPanel.Children.Add(okButton);

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

    public void SetValues(string title, string url)
    {
        _titleBox.Text = title;
        _urlBox.Text = url;
    }
}

// 编辑书签对话框
public class EditBookmarkDialog : Window
{
    public string BookmarkTitle { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;

    private readonly TextBox _titleBox;
    private readonly TextBox _urlBox;

    public EditBookmarkDialog(string title, string url)
    {
        this.Title = "编辑书签";
        Width = 400;
        Height = 200;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var panel = new StackPanel { Margin = new Thickness(16) };

        panel.Children.Add(new TextBlock { Text = "名称：", Margin = new Thickness(0, 0, 0, 4) });
        _titleBox = new TextBox { Text = title, Margin = new Thickness(0, 0, 0, 12) };
        panel.Children.Add(_titleBox);

        panel.Children.Add(new TextBlock { Text = "网址：", Margin = new Thickness(0, 0, 0, 4) });
        _urlBox = new TextBox { Text = url, Margin = new Thickness(0, 0, 0, 16) };
        panel.Children.Add(_urlBox);

        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

        var okButton = new Button
        {
            Content = "确定",
            Width = 80,
            Margin = new Thickness(0, 0, 8, 0),
            IsDefault = true
        };
        okButton.Click += (s, e) =>
        {
            BookmarkTitle = _titleBox.Text;
            Url = _urlBox.Text;
            DialogResult = true;
        };
        buttonPanel.Children.Add(okButton);

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
}

// 输入对话框
public class InputDialog : Window
{
    public string InputValue { get; private set; } = string.Empty;

    private readonly TextBox _inputBox;

    public InputDialog(string title, string prompt, string defaultValue = "")
    {
        Title = title;
        Width = 400;
        Height = 150;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var panel = new StackPanel { Margin = new Thickness(16) };

        panel.Children.Add(new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 12) });
        _inputBox = new TextBox { Text = defaultValue, Margin = new Thickness(0, 0, 0, 16) };
        panel.Children.Add(_inputBox);

        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

        var okButton = new Button
        {
            Content = "确定",
            Width = 80,
            Margin = new Thickness(0, 0, 8, 0),
            IsDefault = true
        };
        okButton.Click += (s, e) =>
        {
            InputValue = _inputBox.Text;
            DialogResult = true;
        };
        buttonPanel.Children.Add(okButton);

        var cancelButton = new Button
        {
            Content = "取消",
            Width = 80,
            IsCancel = true
        };
        buttonPanel.Children.Add(cancelButton);

        panel.Children.Add(buttonPanel);
        Content = panel;

        Loaded += (s, e) => _inputBox.Focus();
    }
}
