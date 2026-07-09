using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CandyBrowser.Windows.ViewModels;

namespace CandyBrowser.Windows.Views;

public partial class NewTabPage : UserControl
{
    public event EventHandler<string>? NavigateRequested;

    public NewTabPage()
    {
        InitializeComponent();
        Loaded += NewTabPage_Loaded;
    }

    private async void NewTabPage_Loaded(object sender, RoutedEventArgs e)
    {
        SearchBox.Focus();

        // Load quick links from bookmarks
        try
        {
            var bookmarkService = App.GetService<Shared.Abstractions.IBookmarkService>();
            var bookmarks = await bookmarkService.GetAllAsync();
            var quickLinks = bookmarks.Where(b => !b.IsFolder).Take(8).ToList();

            QuickLinksPanel.Children.Clear();
            foreach (var bookmark in quickLinks)
            {
                var border = new Border
                {
                    Width = 100,
                    Height = 80,
                    Margin = new Thickness(6),
                    CornerRadius = new CornerRadius(8),
                    Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5)),
                    Cursor = Cursors.Hand,
                    ToolTip = $"{bookmark.Title}\n{bookmark.Url}",
                    Tag = bookmark.Url
                };

                var stack = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                stack.Children.Add(new TextBlock
                {
                    Text = GetIconForUrl(bookmark.Url),
                    FontSize = 24,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 6)
                });

                stack.Children.Add(new TextBlock
                {
                    Text = bookmark.Title,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = 80
                });

                border.Child = stack;
                border.MouseLeftButtonDown += QuickLink_Click;
                border.MouseEnter += (_, _) =>
                {
                    border.Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8));
                };
                border.MouseLeave += (_, _) =>
                {
                    border.Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));
                };

                QuickLinksPanel.Children.Add(border);
            }

            // If no bookmarks, show default quick links
            if (quickLinks.Count == 0)
            {
                var defaults = new[]
                {
                    ("🔍", "百度", "https://www.baidu.com"),
                    ("🔍", "必应", "https://www.bing.com"),
                    ("▶️", "YouTube", "https://www.youtube.com"),
                    ("🐙", "GitHub", "https://github.com"),
                };

                foreach (var (icon, name, url) in defaults)
                {
                    var border = new Border
                    {
                        Width = 100,
                        Height = 80,
                        Margin = new Thickness(6),
                        CornerRadius = new CornerRadius(8),
                        Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5)),
                        Cursor = Cursors.Hand,
                        ToolTip = url,
                        Tag = url
                    };

                    var stack = new StackPanel
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    stack.Children.Add(new TextBlock
                    {
                        Text = icon,
                        FontSize = 24,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 6)
                    });

                    stack.Children.Add(new TextBlock
                    {
                        Text = name,
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
                        HorizontalAlignment = HorizontalAlignment.Center
                    });

                    border.Child = stack;
                    border.MouseLeftButtonDown += QuickLink_Click;
                    border.MouseEnter += (_, _) =>
                    {
                        border.Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8));
                    };
                    border.MouseLeave += (_, _) =>
                    {
                        border.Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));
                    };

                    QuickLinksPanel.Children.Add(border);
                }
            }
        }
        catch { }
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Navigate(SearchBox.Text);
        }
    }

    private void SearchButton_Click(object sender, MouseButtonEventArgs e)
    {
        Navigate(SearchBox.Text);
    }

    private void QuickLink_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is string url)
        {
            Navigate(url);
        }
    }

    private void Navigate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return;

        var url = input;
        if (!url.StartsWith("http://") && !url.StartsWith("https://") && !url.StartsWith("about:"))
        {
            if (url.Contains('.') && !url.Contains(' '))
                url = "https://" + url;
            else
                url = "https://www.bing.com/search?q=" + Uri.EscapeDataString(url);
        }

        NavigateRequested?.Invoke(this, url);
    }

    private static string GetIconForUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return "📄";
        var lower = url.ToLower();
        if (lower.Contains("youtube.com") || lower.Contains("youtu.be")) return "▶️";
        if (lower.Contains("github.com")) return "🐙";
        if (lower.Contains("twitter.com") || lower.Contains("x.com")) return "🐦";
        if (lower.Contains("baidu.com")) return "🔍";
        if (lower.Contains("google.com")) return "🔍";
        if (lower.Contains("bing.com")) return "🔍";
        if (lower.Contains("bilibili.com")) return "📺";
        if (lower.Contains("zhihu.com")) return "💡";
        return "📄";
    }
}
