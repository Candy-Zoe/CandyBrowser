using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CandyBrowser.Shared.Abstractions;
using Models = CandyBrowser.Core.Models;

namespace CandyBrowser.Windows.ViewModels;

public partial class TabViewModel : ObservableObject
{
    private readonly ITabManager _tabManager;

    [ObservableProperty]
    private Models.TabInfo? _tab;

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public TabViewModel(ITabManager tabManager)
    {
        _tabManager = tabManager;
    }

    public void LoadTab(Models.TabInfo tab)
    {
        Tab = tab;
        Url = tab.Url;
        Title = tab.Title ?? "New Tab";
    }

    [RelayCommand]
    private async Task UpdateTabAsync()
    {
        if (Tab == null) return;

        Tab.Url = Url;
        Tab.Title = Title;
        await _tabManager.UpdateAsync(Tab);
    }
}
