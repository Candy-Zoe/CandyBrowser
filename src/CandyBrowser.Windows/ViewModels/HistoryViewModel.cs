using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CandyBrowser.Shared.Abstractions;
using Models = CandyBrowser.Core.Models;

namespace CandyBrowser.Windows.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly IHistoryService _historyService;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private Models.HistoryEntry? _selectedEntry;

    [ObservableProperty]
    private bool _isSearchActive;

    public ObservableCollection<Models.HistoryEntry> HistoryEntries { get; } = new();
    public ObservableCollection<Models.HistoryEntry> SearchResults { get; } = new();
    public ObservableCollection<HistoryGroup> GroupedHistory { get; } = new();

    public HistoryViewModel(IHistoryService historyService)
    {
        _historyService = historyService;
    }

    public async Task LoadHistoryAsync()
    {
        HistoryEntries.Clear();
        GroupedHistory.Clear();

        var entries = await _historyService.GetAllAsync(500);
        foreach (var entry in entries)
        {
            HistoryEntries.Add(entry);
        }

        GroupHistoryByDate();
    }

    private void GroupHistoryByDate()
    {
        GroupedHistory.Clear();

        var today = DateTime.Today;
        var yesterday = today.AddDays(-1);
        var weekAgo = today.AddDays(-7);
        var monthAgo = today.AddDays(-30);

        var todayGroup = new HistoryGroup("今天");
        var yesterdayGroup = new HistoryGroup("昨天");
        var thisWeekGroup = new HistoryGroup("本周");
        var thisMonthGroup = new HistoryGroup("本月");
        var olderGroup = new HistoryGroup("更早");

        foreach (var entry in HistoryEntries)
        {
            var visitDate = entry.LastVisit.Date;

            if (visitDate == today)
                todayGroup.Entries.Add(entry);
            else if (visitDate == yesterday)
                yesterdayGroup.Entries.Add(entry);
            else if (visitDate >= weekAgo)
                thisWeekGroup.Entries.Add(entry);
            else if (visitDate >= monthAgo)
                thisMonthGroup.Entries.Add(entry);
            else
                olderGroup.Entries.Add(entry);
        }

        if (todayGroup.Entries.Count > 0) GroupedHistory.Add(todayGroup);
        if (yesterdayGroup.Entries.Count > 0) GroupedHistory.Add(yesterdayGroup);
        if (thisWeekGroup.Entries.Count > 0) GroupedHistory.Add(thisWeekGroup);
        if (thisMonthGroup.Entries.Count > 0) GroupedHistory.Add(thisMonthGroup);
        if (olderGroup.Entries.Count > 0) GroupedHistory.Add(olderGroup);
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        SearchResults.Clear();
        IsSearchActive = !string.IsNullOrWhiteSpace(SearchQuery);

        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            await LoadHistoryAsync();
            return;
        }

        var results = await _historyService.SearchAsync(SearchQuery, 50);
        foreach (var result in results)
        {
            SearchResults.Add(result);
        }
    }

    [RelayCommand]
    private async Task DeleteEntryAsync(Models.HistoryEntry? entry)
    {
        if (entry == null) return;

        await _historyService.DeleteAsync(entry.Id);
        await LoadHistoryAsync();
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (SelectedEntry == null) return;

        await _historyService.DeleteAsync(SelectedEntry.Id);
        await LoadHistoryAsync();
    }

    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        await _historyService.ClearAsync();
        await LoadHistoryAsync();
    }

    [RelayCommand]
    private async Task ClearHistoryByTimeAsync(string timeRange)
    {
        DateTime? from = timeRange switch
        {
            "hour" => DateTime.Now.AddHours(-1),
            "day" => DateTime.Today,
            "week" => DateTime.Today.AddDays(-7),
            "month" => DateTime.Today.AddMonths(-1),
            _ => null
        };

        if (from.HasValue)
        {
            await _historyService.ClearAsync(from.Value);
            await LoadHistoryAsync();
        }
    }
}

public class HistoryGroup
{
    public string Title { get; }
    public ObservableCollection<Models.HistoryEntry> Entries { get; } = new();

    public HistoryGroup(string title)
    {
        Title = title;
    }
}
