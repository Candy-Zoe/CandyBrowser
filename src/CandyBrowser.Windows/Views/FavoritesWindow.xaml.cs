using System.Windows;
using System.Windows.Controls;

namespace CandyBrowser.Windows.Views;

public partial class FavoritesWindow : Window
{
    public FavoritesWindow()
    {
        InitializeComponent();
        LoadFavorites();
    }

    private void LoadFavorites()
    {
        FavoritesTree.Items.Clear();
        var items = App.GetBookmarksByParent(null);

        foreach (var item in items)
        {
            var treeItem = new TreeViewItem
            {
                Header = item.IsFolder ? $"📁 {item.Title}" : $"📄 {item.Title}",
                Tag = item,
                IsExpanded = true
            };

            if (item.IsFolder)
            {
                var children = App.GetBookmarksByParent(item.Id);
                foreach (var child in children)
                {
                    var childItem = new TreeViewItem
                    {
                        Header = child.IsFolder ? $"📁 {child.Title}" : $"📄 {child.Title}",
                        Tag = child
                    };
                    treeItem.Items.Add(childItem);
                }
            }

            FavoritesTree.Items.Add(treeItem);
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var query = SearchBox.Text?.Trim().ToLower() ?? "";
        if (string.IsNullOrEmpty(query)) { LoadFavorites(); return; }

        FavoritesTree.Items.Clear();
        var all = App.Bookmarks.Where(b => !b.IsFolder &&
            (b.Title.ToLower().Contains(query) || b.Url.ToLower().Contains(query)));

        foreach (var item in all)
        {
            FavoritesTree.Items.Add(new TreeViewItem
            {
                Header = $"📄 {item.Title} - {item.Url}",
                Tag = item
            });
        }
    }

    private void AddCurrentBtn_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new InputDialog("添加收藏", "名称:", "网址:");
        if (dialog.ShowDialog() == true)
        {
            App.AddBookmark(dialog.Values[0], dialog.Values[1]);
            LoadFavorites();
        }
    }

    private void NewFolderBtn_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new InputDialog("新建文件夹", "文件夹名称:");
        if (dialog.ShowDialog() == true)
        {
            App.AddFolder(dialog.Values[0]);
            LoadFavorites();
        }
    }

    private void OpenBtn_Click(object sender, RoutedEventArgs e)
    {
        if (FavoritesTree.SelectedItem is TreeViewItem item && item.Tag is BookmarkItem bookmark && !bookmark.IsFolder)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(bookmark.Url) { UseShellExecute = true });
        }
    }

    private void DeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        if (FavoritesTree.SelectedItem is TreeViewItem item && item.Tag is BookmarkItem bookmark)
        {
            if (MessageBox.Show($"确定删除 '{bookmark.Title}'?", "确认", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                App.DeleteBookmark(bookmark.Id);
                LoadFavorites();
            }
        }
    }
}

public class InputDialog : Window
{
    public string[] Values { get; private set; } = Array.Empty<string>();
    private readonly List<TextBox> _boxes = new();

    public InputDialog(string title, params string[] prompts)
    {
        Title = title;
        Width = 350;
        Height = 150 + prompts.Length * 40;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var panel = new StackPanel { Margin = new Thickness(16) };
        foreach (var prompt in prompts)
        {
            panel.Children.Add(new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 4) });
            var box = new TextBox { Margin = new Thickness(0, 0, 0, 8) };
            _boxes.Add(box);
            panel.Children.Add(box);
        }

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var okBtn = new Button { Content = "确定", Width = 80, IsDefault = true };
        okBtn.Click += (_, _) => { Values = _boxes.Select(b => b.Text).ToArray(); DialogResult = true; };
        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(new Button { Content = "取消", Width = 80, IsCancel = true });
        panel.Children.Add(btnPanel);

        Content = panel;
        Loaded += (_, _) => _boxes.FirstOrDefault()?.Focus();
    }
}
