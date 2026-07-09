using CommunityToolkit.Mvvm.ComponentModel;

namespace CandyBrowser.Windows.ViewModels;

public partial class SuggestionViewModel : ObservableObject
{
    [ObservableProperty]
    private string _icon = "🔍";

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private bool _showUrl = true;

    [ObservableProperty]
    private int _type; // 0=history, 1=bookmark, 2=search

    public SuggestionViewModel() { }

    public SuggestionViewModel(string icon, string title, string url, int type, bool showUrl = true)
    {
        Icon = icon;
        Title = title;
        Url = url;
        Type = type;
        ShowUrl = showUrl;
    }
}
