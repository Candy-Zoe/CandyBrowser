using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Web.WebView2.Wpf;
using CandyBrowser.Shared.Abstractions;
using Models = CandyBrowser.Core.Models;

namespace CandyBrowser.Windows.Views;

public partial class FavoritesWindow : Window
{
    private readonly IBookmarkService _bookmarkService;
    private readonly WebView2 _webView;
    private readonly IBookmarkImportExportService _bookmarkImportExport;
    private TreeViewItem? _dragItem;
    private readonly Dictionary<TreeViewItem, CheckBox> _checkBoxes = new();

    public FavoritesWindow(IBookmarkService bookmarkService, WebView2 webView, IBookmarkImportExportService bookmarkImportExport)
    {
        InitializeComponent();
        _bookmarkService = bookmarkService;
        _webView = webView;
        _bookmarkImportExport = bookmarkImportExport;
        Loaded += async (s, e) => await LoadFavorites();
    }

    private void NavigateInWebView(string url)
    {
        if (_webView?.CoreWebView2 != null && !string.IsNullOrEmpty(url))
            _webView.CoreWebView2.Navigate(url);
    }

    #region Load

    private async Task LoadFavorites()
    {
        FavoritesTree.Items.Clear();
        _checkBoxes.Clear();
        try
        {
            var items = await _bookmarkService.GetChildrenAsync(null);
            foreach (var item in items)
                FavoritesTree.Items.Add(await BuildTreeItem(item));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载书签失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        UpdateSelectedCount();
    }

    private async Task<TreeViewItem> BuildTreeItem(Models.Bookmark item)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };

        // Checkbox - positioned before the expand/collapse button
        var cb = new CheckBox
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 2, 0),
            Padding = new Thickness(0)
        };
        cb.Checked += (_, _) => UpdateSelectedCount();
        cb.Unchecked += (_, _) => UpdateSelectedCount();
        panel.Children.Add(cb);

        // Arrow icon for expand/collapse
        var arrow = new TextBlock
        {
            Text = "\uE76B",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
            VerticalAlignment = VerticalAlignment.Center,
            Width = 14,
            Margin = new Thickness(0, 0, 2, 0)
        };
        panel.Children.Add(arrow);

        // Icon
        var icon = new TextBlock
        {
            Text = item.IsFolder ? "\uE8F4" : "\uE8A7",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 14,
            Foreground = item.IsFolder
                ? new SolidColorBrush(Color.FromRgb(0xF5, 0xA6, 0x23))
                : new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        };
        panel.Children.Add(icon);

        // Title
        var title = new TextBlock
        {
            Text = item.Title,
            FontSize = 13,
            FontFamily = new FontFamily("Microsoft YaHei UI"),
            VerticalAlignment = VerticalAlignment.Center
        };
        panel.Children.Add(title);

        // URL for leaf items
        if (!item.IsFolder && !string.IsNullOrEmpty(item.Url))
        {
            var urlText = new TextBlock
            {
                Text = $"  {item.Url}",
                FontSize = 11,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 300
            };
            panel.Children.Add(urlText);
        }

        var tvi = new TreeViewItem
        {
            Header = panel,
            Tag = item,
            IsExpanded = item.IsFolder,
            FontSize = 13,
            Padding = new Thickness(4, 3, 4, 3)
        };

        _checkBoxes[tvi] = cb;
        tvi.ContextMenu = CreateContextMenu(item, tvi);

        // Lazy-load children on expand
        if (item.IsFolder)
        {
            tvi.Expanded += async (s, e) =>
            {
                // Only load once
                if (tvi.Items.Count == 1 && tvi.Items[0] is ProgressBar)
                {
                    tvi.Items.Clear();
                    try
                    {
                        var children = await _bookmarkService.GetChildrenAsync(item.Id);
                        foreach (var child in children)
                            tvi.Items.Add(await BuildTreeItem(child));
                    }
                    catch { }
                }
            };
            // Add loading indicator
            tvi.Items.Add(new ProgressBar { IsIndeterminate = true, Width = 60, Height = 4 });
        }

        return tvi;
    }

    #endregion

    #region Context Menu

    private ContextMenu CreateContextMenu(Models.Bookmark item, TreeViewItem ownerTvi)
    {
        var menu = new ContextMenu();

        if (!item.IsFolder)
        {
            var url = item.Url;
            var openMi = new MenuItem { Header = "打开" };
            openMi.Click += (s, e) => NavigateInWebView(url);
            menu.Items.Add(openMi);
            menu.Items.Add(new Separator());
        }

        var editNode = item;
        var editMi = new MenuItem { Header = "编辑" };
        editMi.Click += (s, e) => EditBookmark(editNode);
        menu.Items.Add(editMi);

        if (item.IsFolder)
        {
            var fid = item.Id;
            var addBm = new MenuItem { Header = "添加书签" };
            addBm.Click += (s, e) => AddBookmarkToFolder(fid);
            menu.Items.Add(addBm);

            var addF = new MenuItem { Header = "添加文件夹" };
            addF.Click += (s, e) => AddFolderToFolder(fid);
            menu.Items.Add(addF);

            menu.Items.Add(new Separator());
        }

        var moveNode = item;
        var moveMi = new MenuItem { Header = "移动至.." };
        moveMi.Click += (s, e) => MoveNode(moveNode);
        menu.Items.Add(moveMi);

        menu.Items.Add(new Separator());

        var deleteNode = item;
        var deleteMi = new MenuItem { Header = "删除" };
        deleteMi.Click += (s, e) => DeleteBookmark(deleteNode);
        menu.Items.Add(deleteMi);

        return menu;
    }

    private void TreeViewItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is TreeViewItem tvi && tvi.Tag is Models.Bookmark)
        {
            tvi.Focus();
        }
    }

    #endregion

    #region Actions

    private void EditBookmark(Models.Bookmark item)
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
                    item.Title = name;
                    _ = _bookmarkService.UpdateAsync(item);
                    _ = LoadFavorites();
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
                    item.Title = name;
                    item.Url = url;
                    _ = _bookmarkService.UpdateAsync(item);
                    _ = LoadFavorites();
                }
            }
        }
    }

    private async void DeleteBookmark(Models.Bookmark item)
    {
        var msg = item.IsFolder
            ? $"确定删除文件夹 '{item.Title}' 及其所有内容？"
            : $"确定删除 '{item.Title}'？";
        if (MessageBox.Show(msg, "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            await _bookmarkService.DeleteRecursiveAsync(item.Id);
            await LoadFavorites();
        }
    }

    private async void MoveNode(Models.Bookmark item)
    {
        var moveWin = new MoveToFolderDialog(_bookmarkService);
        moveWin.Owner = this;
        if (moveWin.ShowDialog() == true)
        {
            item.ParentId = moveWin.SelectedFolderId;
            await _bookmarkService.UpdateAsync(item);
            await LoadFavorites();
            if (moveWin.SelectedFolderId.HasValue)
                MessageBox.Show("已移动", "完成");
            else
                MessageBox.Show("已移动到根目录", "完成");
        }
    }

    private void AddBookmarkToFolder(long folderId)
    {
        var dialog = new InputDialog("添加书签", new[] { "名称:", "网址:" });
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            var name = dialog.Values[0].Trim();
            var url = dialog.Values[1].Trim();
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url))
            {
                MessageBox.Show("名称和网址不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var bm = new Models.Bookmark { Title = name, Url = url, ParentId = folderId, IsFolder = false, Position = 0 };
            _ = _bookmarkService.AddAsync(bm);
            _ = LoadFavorites();
        }
    }

    private void AddFolderToFolder(long folderId)
    {
        var dialog = new InputDialog("新建文件夹", new[] { "文件夹名称:" });
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            var name = dialog.Values[0].Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("文件夹名称不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var folder = new Models.Bookmark { Title = name, Url = "", ParentId = folderId, IsFolder = true, Position = 0 };
            _ = _bookmarkService.AddAsync(folder);
            _ = LoadFavorites();
        }
    }

    #endregion

    #region Batch / Search / Top-level

    private void SelectAll_Changed(object sender, RoutedEventArgs e)
    {
        var isChecked = SelectAllCheckBox.IsChecked == true;
        foreach (var cb in _checkBoxes.Values) cb.IsChecked = isChecked;
        UpdateSelectedCount();
    }

    private async void BatchMoveBtn_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedItems();
        if (selected.Count == 0) { MessageBox.Show("请先勾选要移动的书签", "提示", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        var moveWin = new MoveToFolderDialog(_bookmarkService);
        moveWin.Owner = this;
        if (moveWin.ShowDialog() == true)
        {
            foreach (var item in selected) { item.ParentId = moveWin.SelectedFolderId; await _bookmarkService.UpdateAsync(item); }
            await LoadFavorites();
            MessageBox.Show($"已移动 {selected.Count} 项", "完成");
        }
    }

    private async void BatchDeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedItems();
        if (selected.Count == 0) { MessageBox.Show("请先勾选要删除的书签", "提示", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        if (MessageBox.Show($"确定删除选中 {selected.Count} 项？\n（文件夹将连同其内容一起删除）",
            "确认批量删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            foreach (var item in selected) await _bookmarkService.DeleteRecursiveAsync(item.Id);
            await LoadFavorites();
            MessageBox.Show($"已删除 {selected.Count} 项", "完成");
        }
    }

    private List<Models.Bookmark> GetSelectedItems()
    {
        var result = new List<Models.Bookmark>();
        foreach (var kvp in _checkBoxes)
            if (kvp.Value.IsChecked == true && kvp.Key.Tag is Models.Bookmark item) result.Add(item);
        return result;
    }

    private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var query = SearchBox.Text?.Trim().ToLower() ?? "";
        if (string.IsNullOrEmpty(query)) { await LoadFavorites(); return; }
        FavoritesTree.Items.Clear();
        _checkBoxes.Clear();
        try
        {
            var all = await _bookmarkService.GetAllAsync();
            var filtered = all.Where(b => b.Title.ToLower().Contains(query) || b.Url.ToLower().Contains(query)).ToList();
            foreach (var item in filtered) FavoritesTree.Items.Add(await BuildTreeItem(item));
        }
        catch { }
    }

    private async void AddCurrentBtn_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new InputDialog("添加收藏", new[] { "名称:", "网址:" });
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            var name = dialog.Values[0].Trim();
            var url = dialog.Values[1].Trim();
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url))
            { MessageBox.Show("名称和网址不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            var bm = new Models.Bookmark { Title = name, Url = url, ParentId = null, IsFolder = false, Position = 0 };
            await _bookmarkService.AddAsync(bm);
            await LoadFavorites();
        }
    }

    private async void NewFolderBtn_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new InputDialog("新建文件夹", new[] { "文件夹名称:" });
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            var name = dialog.Values[0].Trim();
            if (string.IsNullOrEmpty(name)) { MessageBox.Show("文件夹名称不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            var folder = new Models.Bookmark { Title = name, Url = "", ParentId = null, IsFolder = true, Position = 0 };
            await _bookmarkService.AddAsync(folder);
            await LoadFavorites();
        }
    }

    private async void ExportBookmarksBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var html = await _bookmarkImportExport.ExportToHtmlAsync();
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "书签文件 (*.html)|*.html",
                FileName = $"bookmarks_{DateTime.Now:yyyyMMdd_HHmmss}.html"
            };
            if (dlg.ShowDialog() == true)
            {
                await File.WriteAllTextAsync(dlg.FileName, html, System.Text.Encoding.UTF8);
                MessageBox.Show("书签导出成功", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ImportBookmarksBtn_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "书签文件 (*.html)|*.html|所有文件|*.*"
        };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                var html = await File.ReadAllTextAsync(dlg.FileName, System.Text.Encoding.UTF8);
                var count = await _bookmarkImportExport.ImportFromHtmlAsync(html);
                MessageBox.Show($"成功导入 {count} 个书签", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadFavorites();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    #endregion

    #region Bottom Buttons

    private async void OpenBtn_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedBookmark() is { } item && !item.IsFolder)
            NavigateInWebView(item.Url);
    }

    private void EditBtn_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedBookmark() is { } item) EditBookmark(item);
    }

    private async void DeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedBookmark() is { } item) await DeleteBookmarkAsync(item);
    }

    private async Task DeleteBookmarkAsync(Models.Bookmark item)
    {
        var msg = item.IsFolder ? $"确定删除文件夹 '{item.Title}' 及其所有内容？" : $"确定删除 '{item.Title}'？";
        if (MessageBox.Show(msg, "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            await _bookmarkService.DeleteRecursiveAsync(item.Id);
            await LoadFavorites();
        }
    }

    private Models.Bookmark? GetSelectedBookmark()
    {
        if (FavoritesTree.SelectedItem is TreeViewItem tvi && tvi.Tag is Models.Bookmark item) return item;
        return null;
    }

    #endregion

    #region Drag & Drop

    private void FavoritesTree_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            if (GetSelectedBookmark() is { } item)
            {
                var msg = item.IsFolder ? $"确定删除文件夹 '{item.Title}' 及其所有内容？" : $"确定删除 '{item.Title}'？";
                if (MessageBox.Show(msg, "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _ = _bookmarkService.DeleteRecursiveAsync(item.Id);
                    _ = LoadFavorites();
                    e.Handled = true;
                }
            }
        }
    }

    private void TreeItem_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && sender is TreeViewItem tvi && tvi.Tag is Models.Bookmark)
        {
            _dragItem = tvi;
            // Highlight source during drag
            tvi.Background = new SolidColorBrush(Color.FromRgb(0xE0, 0xF0, 0xFF));
            DragDrop.DoDragDrop(tvi, tvi.Tag, DragDropEffects.Move);
            // Reset highlight after drag
            tvi.Background = null;
        }
    }

    private void Tree_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.None;
        if (_dragItem?.Tag is Models.Bookmark source)
        {
            if (sender is TreeViewItem targetTvi && targetTvi.Tag is Models.Bookmark target
                && target.IsFolder && target.Id != source.Id)
                e.Effects = DragDropEffects.Move;
            else if (sender is TreeView && source.ParentId != null)
                e.Effects = DragDropEffects.Move;
        }
        e.Handled = true;
    }

    private async void TreeItem_Drop(object sender, DragEventArgs e)
    {
        if (sender is TreeViewItem targetTvi && targetTvi.Tag is Models.Bookmark target && target.IsFolder
            && _dragItem?.Tag is Models.Bookmark source && source.Id != target.Id)
        {
            source.ParentId = target.Id;
            await _bookmarkService.UpdateAsync(source);
            await LoadFavorites();
        }
        _dragItem = null;
        e.Handled = true;
    }

    private async void Tree_Drop(object sender, DragEventArgs e)
    {
        if (sender is TreeView && _dragItem?.Tag is Models.Bookmark source && source.ParentId != null)
        {
            source.ParentId = null;
            await _bookmarkService.UpdateAsync(source);
            await LoadFavorites();
        }
        _dragItem = null;
        e.Handled = true;
    }

    #endregion

    private void UpdateSelectedCount()
    {
        var count = _checkBoxes.Values.Count(cb => cb.IsChecked == true);
        SelectedCountText.Text = $"已选 {count} 项";
    }
}

#region MoveToFolderDialog

public class MoveToFolderDialog : Window
{
    public long? SelectedFolderId { get; private set; }

    public MoveToFolderDialog(IBookmarkService bookmarkService)
    {
        Title = "移动到文件夹";
        Width = 360; Height = 400;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        Background = new SolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xFA));

        var mainPanel = new StackPanel { Margin = new Thickness(20) };
        mainPanel.Children.Add(new TextBlock
        {
            Text = "选择目标文件夹", FontSize = 16, FontWeight = FontWeights.SemiBold,
            FontFamily = new FontFamily("Microsoft YaHei UI"),
            Foreground = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x22)),
            Margin = new Thickness(0, 0, 0, 12)
        });

        var listBox = new ListBox { Height = 240, FontSize = 13, FontFamily = new FontFamily("Microsoft YaHei UI") };
        listBox.Items.Add(new ListBoxItem { Content = "根目录", Tag = (object)null! });

        try
        {
            var folders = bookmarkService.GetChildrenAsync(null).Result;
            foreach (var folder in folders.Where(f => f.IsFolder))
                listBox.Items.Add(new ListBoxItem { Content = $"\uE8F4  {folder.Title}", Tag = (object)folder.Id });
        }
        catch { }

        mainPanel.Children.Add(listBox);

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };

        var okBtn = new Button { Content = "确定", Width = 80, Padding = new Thickness(0, 7, 0, 7), FontSize = 13,
            FontFamily = new FontFamily("Microsoft YaHei UI"), Background = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)),
            Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand, IsDefault = true };
        okBtn.Click += (_, _) =>
        {
            if (listBox.SelectedItem is ListBoxItem lbi) { SelectedFolderId = lbi.Tag as long?; DialogResult = true; }
        };

        var cancelBtn = new Button { Content = "取消", Width = 80, Padding = new Thickness(0, 7, 0, 7), FontSize = 13,
            FontFamily = new FontFamily("Microsoft YaHei UI"), Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8)),
            Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)), BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand, IsCancel = true, Margin = new Thickness(8, 0, 0, 0) };

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
        Title = title; Width = 400;
        Height = 200 + prompts.Length * 50;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        Background = new SolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xFA));

        var mainPanel = new StackPanel { Margin = new Thickness(24) };
        mainPanel.Children.Add(new TextBlock
        {
            Text = title, FontSize = 18, FontWeight = FontWeights.SemiBold,
            FontFamily = new FontFamily("Microsoft YaHei UI"),
            Foreground = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x22)),
            Margin = new Thickness(0, 0, 0, 16)
        });

        for (int i = 0; i < prompts.Length; i++)
        {
            mainPanel.Children.Add(new TextBlock
            {
                Text = prompts[i], FontSize = 12, FontFamily = new FontFamily("Microsoft YaHei UI"),
                Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
                Margin = new Thickness(0, 0, 0, 4)
            });
            var box = new TextBox
            {
                Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(10, 7, 10, 7),
                FontSize = 13, FontFamily = new FontFamily("Microsoft YaHei UI"),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD0)),
                BorderThickness = new Thickness(1)
            };
            if (initialValues != null && i < initialValues.Length) box.Text = initialValues[i];
            _boxes.Add(box);
            mainPanel.Children.Add(box);
        }

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) };
        var okBtn = new Button { Content = "确定", Width = 90, Padding = new Thickness(0, 8, 0, 8), FontSize = 13,
            FontFamily = new FontFamily("Microsoft YaHei UI"), Background = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)),
            Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand, IsDefault = true };
        okBtn.Click += (_, _) => { Values = _boxes.Select(b => b.Text).ToArray(); DialogResult = true; };

        var cancelBtn = new Button { Content = "取消", Width = 90, Padding = new Thickness(0, 8, 0, 8), FontSize = 13,
            FontFamily = new FontFamily("Microsoft YaHei UI"), Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8)),
            Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)), BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand, IsCancel = true, Margin = new Thickness(8, 0, 0, 0) };

        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        mainPanel.Children.Add(btnPanel);
        Content = mainPanel;
        Loaded += (_, _) => _boxes.FirstOrDefault()?.Focus();
    }
}
