using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CandyBrowser.Windows.Views;

public partial class FavoritesWindow : Window
{
    private TreeViewItem? _dragItem;
    private readonly Dictionary<TreeViewItem, CheckBox> _checkBoxes = new();

    public FavoritesWindow()
    {
        InitializeComponent();
        LoadFavorites();
    }

    private void LoadFavorites()
    {
        FavoritesTree.Items.Clear();
        _checkBoxes.Clear();
        var items = App.GetBookmarksByParent(null);
        foreach (var item in items)
            FavoritesTree.Items.Add(CreateTreeItem(item));
        UpdateSelectedCount();
    }

    private TreeViewItem CreateTreeItem(BookmarkItem item)
    {
        var mainPanel = new StackPanel { Orientation = Orientation.Horizontal };
        
        // 勾选框
        var checkBox = new CheckBox
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        checkBox.Checked += (s, e) => UpdateSelectedCount();
        checkBox.Unchecked += (s, e) => UpdateSelectedCount();
        mainPanel.Children.Add(checkBox);

        // 图标 - 使用 Segoe MDL2 Assets 避免乱码
        var icon = new TextBlock
        {
            Text = item.IsFolder ? "\uE8F4" : "\uE8A7",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 14,
            Foreground = item.IsFolder 
                ? new SolidColorBrush(Color.FromRgb(0xF5, 0xA6, 0x23)) 
                : new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        mainPanel.Children.Add(icon);

        // 标题
        var title = new TextBlock
        {
            Text = item.Title,
            FontSize = 13,
            FontFamily = new FontFamily("Microsoft YaHei UI"),
            VerticalAlignment = VerticalAlignment.Center
        };
        mainPanel.Children.Add(title);

        // URL (非文件夹时显示)
        if (!item.IsFolder && !string.IsNullOrEmpty(item.Url))
        {
            var url = new TextBlock
            {
                Text = $"  {item.Url}",
                FontSize = 11,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 300
            };
            mainPanel.Children.Add(url);
        }

        var treeItem = new TreeViewItem
        {
            Header = mainPanel,
            Tag = item,
            IsExpanded = true,
            FontSize = 13,
            Padding = new Thickness(4, 3, 4, 3)
        };

        _checkBoxes[treeItem] = checkBox;

        // 右键菜单
        treeItem.ContextMenu = CreateContextMenu(item);

        // 加载子文件夹
        if (item.IsFolder)
        {
            foreach (var child in App.GetBookmarksByParent(item.Id))
                treeItem.Items.Add(CreateTreeItem(child));
        }

        return treeItem;
    }

    private void UpdateSelectedCount()
    {
        var count = _checkBoxes.Values.Count(cb => cb.IsChecked == true);
        SelectedCountText.Text = $"已选 {count} 项";
    }

    private ContextMenu CreateContextMenu(BookmarkItem item)
    {
        var menu = new ContextMenu();

        if (!item.IsFolder)
        {
            var openMi = new MenuItem { Header = "打开" };
            openMi.Click += (s, e) => OpenBookmark(item);
            menu.Items.Add(openMi);
            menu.Items.Add(new Separator());
        }

        var editMi = new MenuItem { Header = "编辑" };
        editMi.Click += (s, e) => EditBookmark(item);
        menu.Items.Add(editMi);

        var moveMi = new MenuItem { Header = "移动到..." };
        var moveSubmenu = CreateMoveSubmenu(item);
        moveMi.Items.Add(moveSubmenu);
        menu.Items.Add(moveMi);

        menu.Items.Add(new Separator());

        var deleteMi = new MenuItem { Header = "删除" };
        deleteMi.Click += (s, e) => DeleteBookmark(item);
        menu.Items.Add(deleteMi);

        return menu;
    }

    private MenuItem CreateMoveSubmenu(BookmarkItem item)
    {
        var moveSubmenu = new MenuItem();
        
        var rootMi = new MenuItem { Header = "根目录" };
        rootMi.Click += (s, e) => MoveBookmarkTo(item, null);
        moveSubmenu.Items.Add(rootMi);
        moveSubmenu.Items.Add(new Separator());

        var folders = App.Bookmarks.Where(b => b.IsFolder && b.Id != item.Id).ToList();
        foreach (var folder in folders)
        {
            var folderMi = new MenuItem { Header = folder.Title };
            folderMi.Click += (s, e) => MoveBookmarkTo(item, folder.Id);
            moveSubmenu.Items.Add(folderMi);
        }

        if (folders.Count == 0)
        {
            var noFolderMi = new MenuItem { Header = "(无文件夹)", IsEnabled = false };
            moveSubmenu.Items.Add(noFolderMi);
        }

        return moveSubmenu;
    }

    private void OpenBookmark(BookmarkItem item)
    {
        if (!item.IsFolder && !string.IsNullOrEmpty(item.Url))
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(item.Url) { UseShellExecute = true });
    }

    private void EditBookmark(BookmarkItem item)
    {
        if (item.IsFolder)
        {
            var dialog = new InputDialog("编辑文件夹", new[] { "名称:" }, new[] { item.Title });
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                var name = dialog.Values[0].Trim();
                if (!string.IsNullOrEmpty(name))
                {
                    App.UpdateBookmark(item.Id, name);
                    LoadFavorites();
                }
            }
        }
        else
        {
            var dialog = new InputDialog("编辑书签", new[] { "名称:", "网址:" }, new[] { item.Title, item.Url });
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                var name = dialog.Values[0].Trim();
                var url = dialog.Values[1].Trim();
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(url))
                {
                    App.UpdateBookmark(item.Id, name, url);
                    LoadFavorites();
                }
            }
        }
    }

    private void MoveBookmarkTo(BookmarkItem item, long? newParentId)
    {
        App.MoveBookmark(item.Id, newParentId);
        LoadFavorites();
    }

    private void DeleteBookmark(BookmarkItem item)
    {
        var msg = item.IsFolder 
            ? $"确定删除文件夹 '{item.Title}' 及其所有内容？" 
            : $"确定删除 '{item.Title}'？";
        
        if (MessageBox.Show(msg, "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            App.DeleteBookmark(item.Id);
            LoadFavorites();
        }
    }

    #region Batch Operations

    private void SelectAll_Changed(object sender, RoutedEventArgs e)
    {
        var isChecked = SelectAllCheckBox.IsChecked == true;
        foreach (var cb in _checkBoxes.Values)
            cb.IsChecked = isChecked;
        UpdateSelectedCount();
    }

    private void BatchMoveBtn_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedItems();
        if (selected.Count == 0)
        {
            MessageBox.Show("请先勾选要移动的书签", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var moveWin = new MoveToFolderDialog();
        moveWin.Owner = this;
        if (moveWin.ShowDialog() == true && moveWin.SelectedFolderId.HasValue)
        {
            foreach (var item in selected)
                App.MoveBookmark(item.Id, moveWin.SelectedFolderId.Value);
            LoadFavorites();
            MessageBox.Show($"已移动 {selected.Count} 项", "完成");
        }
    }

    private void BatchDeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedItems();
        if (selected.Count == 0)
        {
            MessageBox.Show("请先勾选要删除的书签", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"确定删除选中的 {selected.Count} 项？\n（文件夹将连同其内容一起删除）",
            "确认批量删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            foreach (var item in selected)
                App.DeleteBookmark(item.Id);
            LoadFavorites();
            MessageBox.Show($"已删除 {selected.Count} 项", "完成");
        }
    }

    private List<BookmarkItem> GetSelectedItems()
    {
        var result = new List<BookmarkItem>();
        foreach (var kvp in _checkBoxes)
        {
            if (kvp.Value.IsChecked == true && kvp.Key.Tag is BookmarkItem item)
                result.Add(item);
        }
        return result;
    }

    #endregion

    #region Search

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var query = SearchBox.Text?.Trim().ToLower() ?? "";
        if (string.IsNullOrEmpty(query)) { LoadFavorites(); return; }
        
        FavoritesTree.Items.Clear();
        _checkBoxes.Clear();
        var all = App.Bookmarks.Where(b => 
            (b.Title.ToLower().Contains(query) || b.Url.ToLower().Contains(query)));
        
        foreach (var item in all)
        {
            var mainPanel = new StackPanel { Orientation = Orientation.Horizontal };
            
            var checkBox = new CheckBox
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            checkBox.Checked += (s, e) => UpdateSelectedCount();
            checkBox.Unchecked += (s, e) => UpdateSelectedCount();
            mainPanel.Children.Add(checkBox);

            var icon = new TextBlock
            {
                Text = item.IsFolder ? "\uE8F4" : "\uE8A7",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 14,
                Foreground = item.IsFolder 
                    ? new SolidColorBrush(Color.FromRgb(0xF5, 0xA6, 0x23)) 
                    : new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            mainPanel.Children.Add(icon);

            var title = new TextBlock
            {
                Text = item.Title,
                FontSize = 12,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                VerticalAlignment = VerticalAlignment.Center
            };
            mainPanel.Children.Add(title);

            if (!item.IsFolder && !string.IsNullOrEmpty(item.Url))
            {
                var url = new TextBlock
                {
                    Text = $"  -  {item.Url}",
                    FontSize = 11,
                    FontFamily = new FontFamily("Microsoft YaHei UI"),
                    Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                mainPanel.Children.Add(url);
            }

            var treeItem = new TreeViewItem
            {
                Header = mainPanel,
                Tag = item,
                FontSize = 12
            };
            _checkBoxes[treeItem] = checkBox;
            FavoritesTree.Items.Add(treeItem);
        }
    }

    #endregion

    #region Add / New Folder

    private void AddCurrentBtn_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new InputDialog("添加收藏", new[] { "名称:", "网址:" });
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            var name = dialog.Values[0].Trim();
            var url = dialog.Values[1].Trim();
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url))
            {
                MessageBox.Show("名称和网址不能为空。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            App.AddBookmark(name, url);
            LoadFavorites();
        }
    }

    private void NewFolderBtn_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new InputDialog("新建文件夹", new[] { "文件夹名称:" });
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            var name = dialog.Values[0].Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("文件夹名称不能为空。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            App.AddFolder(name);
            LoadFavorites();
        }
    }

    #endregion

    #region Bottom Buttons

    private void OpenBtn_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedBookmark() is { } item && !item.IsFolder)
            OpenBookmark(item);
    }

    private void EditBtn_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedBookmark() is { } item)
            EditBookmark(item);
    }

    private void DeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedBookmark() is { } item)
            DeleteBookmark(item);
    }

    private BookmarkItem? GetSelectedBookmark()
    {
        if (FavoritesTree.SelectedItem is TreeViewItem treeItem && treeItem.Tag is BookmarkItem item)
            return item;
        return null;
    }

    #endregion

    #region Drag & Drop

    private void TreeItem_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && sender is TreeViewItem item && item.Tag is BookmarkItem)
        {
            _dragItem = item;
            DragDrop.DoDragDrop(item, item, DragDropEffects.Move);
        }
    }

    private void Tree_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.None;
        
        if (sender is TreeViewItem targetItem && targetItem.Tag is BookmarkItem target)
        {
            if (_dragItem?.Tag is BookmarkItem source && target.IsFolder && source.Id != target.Id)
            {
                e.Effects = DragDropEffects.Move;
            }
        }
        else if (sender is TreeView)
        {
            if (_dragItem?.Tag is BookmarkItem source && source.ParentId != null)
            {
                e.Effects = DragDropEffects.Move;
            }
        }
        
        e.Handled = true;
    }

    private void TreeItem_Drop(object sender, DragEventArgs e)
    {
        if (sender is TreeViewItem targetItem && targetItem.Tag is BookmarkItem target && target.IsFolder)
        {
            if (_dragItem?.Tag is BookmarkItem source && source.Id != target.Id)
            {
                App.MoveBookmark(source.Id, target.Id);
                LoadFavorites();
            }
        }
        _dragItem = null;
        e.Handled = true;
    }

    private void Tree_Drop(object sender, DragEventArgs e)
    {
        if (sender is TreeView && _dragItem?.Tag is BookmarkItem source && source.ParentId != null)
        {
            App.MoveBookmark(source.Id, null);
            LoadFavorites();
        }
        _dragItem = null;
        e.Handled = true;
    }

    private void Tree_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 允许右键选择项目
    }

    #endregion
}

#region MoveToFolderDialog

public class MoveToFolderDialog : Window
{
    public long? SelectedFolderId { get; private set; }

    public MoveToFolderDialog()
    {
        Title = "移动到文件夹";
        Width = 360;
        Height = 400;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        Background = new SolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xFA));

        var mainPanel = new StackPanel { Margin = new Thickness(20) };

        mainPanel.Children.Add(new TextBlock
        {
            Text = "选择目标文件夹",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            FontFamily = new FontFamily("Microsoft YaHei UI"),
            Foreground = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x22)),
            Margin = new Thickness(0, 0, 0, 12)
        });

        var listBox = new ListBox
        {
            Height = 240,
            FontSize = 13,
            FontFamily = new FontFamily("Microsoft YaHei UI")
        };

        // 根目录选项
        listBox.Items.Add(new ListBoxItem { Content = "根目录", Tag = (long?)null });

        // 文件夹选项
        var folders = App.Bookmarks.Where(b => b.IsFolder).ToList();
        foreach (var folder in folders)
        {
            listBox.Items.Add(new ListBoxItem { Content = $"\uE8F4  {folder.Title}", Tag = (long?)folder.Id });
        }

        mainPanel.Children.Add(listBox);

        var btnPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0)
        };

        var okBtn = new Button
        {
            Content = "确定",
            Width = 80,
            Padding = new Thickness(0, 7, 0, 7),
            FontSize = 13,
            FontFamily = new FontFamily("Microsoft YaHei UI"),
            Background = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            IsDefault = true
        };
        okBtn.Click += (_, _) =>
        {
            if (listBox.SelectedItem is ListBoxItem lbi)
            {
                SelectedFolderId = lbi.Tag as long?;
                DialogResult = true;
            }
        };

        var cancelBtn = new Button
        {
            Content = "取消",
            Width = 80,
            Padding = new Thickness(0, 7, 0, 7),
            FontSize = 13,
            FontFamily = new FontFamily("Microsoft YaHei UI"),
            Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8)),
            Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            IsCancel = true,
            Margin = new Thickness(8, 0, 0, 0)
        };

        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        mainPanel.Children.Add(btnPanel);

        Content = mainPanel;
    }
}

#endregion

public class InputDialog : Window
{
    public string[] Values { get; private set; } = Array.Empty<string>();
    private readonly List<TextBox> _boxes = new();

    public InputDialog(string title, string[] prompts, string[]? initialValues = null)
    {
        Title = title;
        Width = 400;
        Height = 200 + prompts.Length * 50;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        Background = new SolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xFA));

        var mainPanel = new StackPanel { Margin = new Thickness(24) };

        mainPanel.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            FontFamily = new FontFamily("Microsoft YaHei UI"),
            Foreground = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x22)),
            Margin = new Thickness(0, 0, 0, 16)
        });

        for (int i = 0; i < prompts.Length; i++)
        {
            mainPanel.Children.Add(new TextBlock
            {
                Text = prompts[i],
                FontSize = 12,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
                Margin = new Thickness(0, 0, 0, 4)
            });
            var box = new TextBox
            {
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(10, 7, 10, 7),
                FontSize = 13,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD0)),
                BorderThickness = new Thickness(1)
            };
            if (initialValues != null && i < initialValues.Length)
                box.Text = initialValues[i];
            _boxes.Add(box);
            mainPanel.Children.Add(box);
        }

        var btnPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 8, 0, 0)
        };

        var okBtn = new Button
        {
            Content = "确定",
            Width = 90,
            Padding = new Thickness(0, 8, 0, 8),
            FontSize = 13,
            FontFamily = new FontFamily("Microsoft YaHei UI"),
            Background = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            IsDefault = true
        };
        okBtn.Click += (_, _) => { Values = _boxes.Select(b => b.Text).ToArray(); DialogResult = true; };

        var cancelBtn = new Button
        {
            Content = "取消",
            Width = 90,
            Padding = new Thickness(0, 8, 0, 8),
            FontSize = 13,
            FontFamily = new FontFamily("Microsoft YaHei UI"),
            Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8)),
            Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            IsCancel = true,
            Margin = new Thickness(8, 0, 0, 0)
        };

        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        mainPanel.Children.Add(btnPanel);

        Content = mainPanel;
        Loaded += (_, _) => _boxes.FirstOrDefault()?.Focus();
    }
}
