using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Time_Tracker.Models;
using Time_Tracker.Services;

namespace Time_Tracker.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly ITimeTrackerRepository _repository;
    private readonly DispatcherTimer _refreshTimer;
    private readonly DispatcherTimer _notificationTimer;
    private string _newParentCategoryName = string.Empty;
    private ParentCategoryRecord? _selectedParentCategory;
    private string _newActivityName = string.Empty;
    private string _selectedActivityName = string.Empty;
    private ActivityItemViewModel? _selectedActivity;
    private TimeEntryItemViewModel? _selectedTimeEntry;
    private string _statusMessage = string.Empty;
    private bool _isStatusVisible;

    public MainViewModel(ITimeTrackerRepository repository)
    {
        _repository = repository;
        ParentCategories = new ObservableCollection<ParentCategoryRecord>();
        Activities = new ObservableCollection<ActivityItemViewModel>();
        TimeEntries = new ObservableCollection<TimeEntryItemViewModel>();

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        _refreshTimer.Tick += (_, _) => RefreshDisplayedDurations();
        _refreshTimer.Start();

        _notificationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3),
        };
        _notificationTimer.Tick += (_, _) => ClearStatus();

        AddParentCategoryCommand = new AsyncRelayCommand(AddParentCategoryAsync);
        CreateActivityCommand = new AsyncRelayCommand(CreateActivityAsync);
        RenameActivityCommand = new AsyncRelayCommand(RenameSelectedActivityAsync, CanRenameSelectedActivity);
        StartActivityCommand = new AsyncRelayCommand(StartSelectedActivityAsync, CanStartSelectedActivity);
        StopActivityCommand = new AsyncRelayCommand(StopSelectedActivityAsync, CanStopSelectedActivity);
        RefreshCommand = new AsyncRelayCommand(RefreshAllAsync);
        DeleteTimeEntryCommand = new AsyncRelayCommand(DeleteSelectedTimeEntryAsync, CanDeleteSelectedTimeEntry);
    }

    public ObservableCollection<ParentCategoryRecord> ParentCategories { get; }

    public ObservableCollection<ActivityItemViewModel> Activities { get; }

    public ObservableCollection<TimeEntryItemViewModel> TimeEntries { get; }

    public IAsyncRelayCommand AddParentCategoryCommand { get; }

    public IAsyncRelayCommand CreateActivityCommand { get; }

    public IAsyncRelayCommand RenameActivityCommand { get; }

    public IAsyncRelayCommand StartActivityCommand { get; }

    public IAsyncRelayCommand StopActivityCommand { get; }

    public IAsyncRelayCommand RefreshCommand { get; }

    public IAsyncRelayCommand DeleteTimeEntryCommand { get; }

    public string NewParentCategoryName
    {
        get => _newParentCategoryName;
        set => SetProperty(ref _newParentCategoryName, value);
    }

    public ParentCategoryRecord? SelectedParentCategory
    {
        get => _selectedParentCategory;
        set
        {
            if (SetProperty(ref _selectedParentCategory, value))
            {
                _ = LoadAsync();
            }
        }
    }

    public string NewActivityName
    {
        get => _newActivityName;
        set => SetProperty(ref _newActivityName, value);
    }

    public string SelectedActivityName
    {
        get => _selectedActivityName;
        set
        {
            if (SetProperty(ref _selectedActivityName, value))
            {
                RenameActivityCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public ActivityItemViewModel? SelectedActivity
    {
        get => _selectedActivity;
        set
        {
            if (SetProperty(ref _selectedActivity, value))
            {
                SelectedActivityName = value?.Name ?? string.Empty;
                StartActivityCommand.NotifyCanExecuteChanged();
                StopActivityCommand.NotifyCanExecuteChanged();
                _ = LoadTimeEntriesForSelectionAsync();
            }
        }
    }

    public TimeEntryItemViewModel? SelectedTimeEntry
    {
        get => _selectedTimeEntry;
        set
        {
            if (SetProperty(ref _selectedTimeEntry, value))
            {
                DeleteTimeEntryCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsStatusVisible
    {
        get => _isStatusVisible;
        private set => SetProperty(ref _isStatusVisible, value);
    }

    public async Task InitializeAsync()
    {
        await _repository.InitializeAsync();
        await LoadParentCategoriesAsync();
        await LoadAsync();
    }

    public EditSessionModel? CreateEditModelForSelectedEntry()
    {
        return SelectedTimeEntry?.ToEditModel();
    }

    public async Task ApplyEditAsync(EditSessionModel model)
    {
        await _repository.UpdateTimeEntryAsync(model);
        ShowStatus("Time entry updated.");
        await LoadAsync(model.ActivityId);
    }

    public async Task ExportCsvAsync(string filePath)
    {
        var rows = await _repository.GetExportRowsAsync();
        var builder = new StringBuilder();
        builder.AppendLine("Parent,Activity,Status,Created,Start,End,Duration,Notes");

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(",",
                EscapeCsv(row.ParentCategory),
                EscapeCsv(row.Activity),
                EscapeCsv(row.Status),
                EscapeCsv(row.CreatedAtLocal.ToString("yyyy-MM-dd HH:mm:ss")),
                EscapeCsv(row.StartLocal.ToString("yyyy-MM-dd HH:mm:ss")),
                EscapeCsv(row.EndLocal?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty),
                EscapeCsv(row.Duration.ToString(@"hh\:mm\:ss")),
                EscapeCsv(row.Notes)));
        }

        await File.WriteAllTextAsync(filePath, builder.ToString(), Encoding.UTF8);
        ShowStatus($"Exported {rows.Count} rows to CSV.");
    }

    public void ShowStatus(string message)
    {
        StatusMessage = message;
        IsStatusVisible = !string.IsNullOrWhiteSpace(message);

        _notificationTimer.Stop();
        if (IsStatusVisible)
        {
            _notificationTimer.Start();
        }
    }

    private void ClearStatus()
    {
        _notificationTimer.Stop();
        IsStatusVisible = false;
        StatusMessage = string.Empty;
    }

    private async Task RefreshAllAsync()
    {
        await LoadParentCategoriesAsync(SelectedParentCategory?.Id);
        await LoadAsync(SelectedActivity?.Id);
    }

    private async Task LoadParentCategoriesAsync(long? selectedParentCategoryId = null)
    {
        var records = await _repository.GetParentCategoriesAsync();
        var previousSelectedId = selectedParentCategoryId ?? SelectedParentCategory?.Id;

        ParentCategories.Clear();
        foreach (var record in records)
        {
            ParentCategories.Add(record);
        }

        SelectedParentCategory = previousSelectedId is null
            ? ParentCategories.FirstOrDefault()
            : ParentCategories.FirstOrDefault(parent => parent.Id == previousSelectedId) ?? ParentCategories.FirstOrDefault();
    }

    private async Task LoadAsync(long? selectedActivityId = null)
    {
        if (SelectedParentCategory is null)
        {
            Activities.Clear();
            TimeEntries.Clear();
            SelectedActivity = null;
            SelectedTimeEntry = null;
            return;
        }

        var records = await _repository.GetActivitiesAsync(SelectedParentCategory.Id);
        var previousSelectedId = selectedActivityId ?? SelectedActivity?.Id;

        Activities.Clear();
        foreach (var record in records)
        {
            Activities.Add(new ActivityItemViewModel(record));
        }

        SelectedActivity = previousSelectedId is null
            ? Activities.FirstOrDefault()
            : Activities.FirstOrDefault(activity => activity.Id == previousSelectedId) ?? Activities.FirstOrDefault();

        RefreshDisplayedDurations();

        if (SelectedActivity is null)
        {
            TimeEntries.Clear();
            SelectedTimeEntry = null;
        }
    }

    private async Task LoadTimeEntriesForSelectionAsync()
    {
        if (SelectedActivity is null)
        {
            TimeEntries.Clear();
            SelectedTimeEntry = null;
            return;
        }

        var records = await _repository.GetTimeEntriesAsync(SelectedActivity.Id);
        var items = records
            .Select(record => new TimeEntryItemViewModel(record, SelectedActivity.Name))
            .ToList();

        TimeEntries.Clear();
        foreach (var item in items)
        {
            TimeEntries.Add(item);
        }

        SelectedTimeEntry = TimeEntries.FirstOrDefault();
        RefreshDisplayedDurations();
    }

    private async Task AddParentCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(NewParentCategoryName))
        {
            ShowStatus("Enter a parent/category name.");
            return;
        }

        var parentName = NewParentCategoryName.Trim();
        var parentId = await _repository.CreateParentCategoryAsync(parentName);
        NewParentCategoryName = string.Empty;
        await LoadParentCategoriesAsync(parentId);
        ShowStatus($"Parent '{parentName}' is ready.");
    }

    private async Task CreateActivityAsync()
    {
        if (SelectedParentCategory is null)
        {
            ShowStatus("Create a parent/category first.");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewActivityName))
        {
            ShowStatus("Enter an activity name.");
            return;
        }

        try
        {
            var activityName = NewActivityName.Trim();
            var nowUtc = DateTime.UtcNow;
            var activityId = await _repository.CreateActivityAsync(activityName, SelectedParentCategory.Id, nowUtc);
            await _repository.StartActivityAsync(activityId, nowUtc);

            NewActivityName = string.Empty;
            ShowStatus($"Created and started '{activityName}'.");
            await LoadAsync(activityId);
        }
        catch (Exception ex)
        {
            ShowStatus($"Could not create activity: {ex.Message}");
        }
    }

    private bool CanRenameSelectedActivity()
    {
        return SelectedActivity is not null
            && !string.IsNullOrWhiteSpace(SelectedActivityName)
            && !string.Equals(SelectedActivityName.Trim(), SelectedActivity.Name, StringComparison.Ordinal);
    }

    private async Task RenameSelectedActivityAsync()
    {
        if (SelectedActivity is null)
        {
            return;
        }

        await _repository.RenameActivityAsync(SelectedActivity.Id, SelectedActivityName);
        ShowStatus("Activity renamed.");
        await LoadAsync(SelectedActivity.Id);
    }

    private bool CanStartSelectedActivity()
    {
        return SelectedActivity is not null && !SelectedActivity.IsRunning;
    }

    private async Task StartSelectedActivityAsync()
    {
        if (SelectedActivity is null)
        {
            return;
        }

        await _repository.StartActivityAsync(SelectedActivity.Id, DateTime.UtcNow);
        ShowStatus($"Started '{SelectedActivity.Name}'.");
        await LoadAsync(SelectedActivity.Id);
    }

    private bool CanStopSelectedActivity()
    {
        return SelectedActivity is not null && SelectedActivity.IsRunning;
    }

    private async Task StopSelectedActivityAsync()
    {
        if (SelectedActivity is null)
        {
            return;
        }

        await _repository.StopActivityAsync(SelectedActivity.Id, DateTime.UtcNow);
        ShowStatus($"Stopped '{SelectedActivity.Name}'.");
        await LoadAsync(SelectedActivity.Id);
    }

    private bool CanDeleteSelectedTimeEntry()
    {
        return SelectedTimeEntry is not null;
    }

    private async Task DeleteSelectedTimeEntryAsync()
    {
        if (SelectedTimeEntry is null)
        {
            return;
        }

        var activityId = SelectedTimeEntry.ActivityId;
        await _repository.DeleteTimeEntryAsync(SelectedTimeEntry.Id);
        ShowStatus("Time entry deleted.");
        await LoadAsync(activityId);
    }

    private void RefreshDisplayedDurations()
    {
        var nowUtc = DateTime.UtcNow;

        foreach (var activity in Activities)
        {
            activity.Refresh(nowUtc);
        }

        foreach (var entry in TimeEntries)
        {
            entry.Refresh(nowUtc);
        }

        RenameActivityCommand.NotifyCanExecuteChanged();
        StartActivityCommand.NotifyCanExecuteChanged();
        StopActivityCommand.NotifyCanExecuteChanged();
    }

    private static string EscapeCsv(string value)
    {
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
