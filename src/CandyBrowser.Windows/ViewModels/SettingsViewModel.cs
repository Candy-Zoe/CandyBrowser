using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CandyBrowser.Shared.Abstractions;

namespace CandyBrowser.Windows.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _searchEngine = string.Empty;

    [ObservableProperty]
    private string _homepage = string.Empty;

    [ObservableProperty]
    private string _newTabUrl = string.Empty;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task LoadSettingsAsync()
    {
        SearchEngine = await _settingsService.GetSearchEngineAsync();
        Homepage = await _settingsService.GetHomepageAsync();
        NewTabUrl = await _settingsService.GetNewTabUrlAsync();
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        await _settingsService.SetAsync("search_engine", SearchEngine);
        await _settingsService.SetAsync("homepage", Homepage);
        await _settingsService.SetAsync("new_tab_url", NewTabUrl);
    }
}
