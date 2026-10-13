using System;
using Time_Tracker.Models;

namespace Time_Tracker.ViewModels;

public class ActivityItemViewModel : ViewModelBase
{
    private string _name = string.Empty;
    private bool _isRunning;
    private string _status = "Stopped";
    private string _durationText = "00:00:00";

    public ActivityItemViewModel(ActivityRecord record)
    {
        Id = record.Id;
        _name = record.Name;
        CreatedAtLocal = record.CreatedAtUtc.ToLocalTime();
        AccumulatedDuration = record.AccumulatedDuration;
        CurrentSessionStartUtc = record.CurrentSessionStartUtc;
        Refresh(DateTime.UtcNow);
    }

    public long Id { get; }

    public DateTime CreatedAtLocal { get; }

    public TimeSpan AccumulatedDuration { get; private set; }

    public DateTime? CurrentSessionStartUtc { get; private set; }

    public string Name
    {
        get => _name;
        private set => SetProperty(ref _name, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set => SetProperty(ref _isRunning, value);
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public string DurationText
    {
        get => _durationText;
        private set => SetProperty(ref _durationText, value);
    }

    public void UpdateFrom(ActivityRecord record)
    {
        Name = record.Name;
        AccumulatedDuration = record.AccumulatedDuration;
        CurrentSessionStartUtc = record.CurrentSessionStartUtc;
        Refresh(DateTime.UtcNow);
    }

    public void Refresh(DateTime nowUtc)
    {
        IsRunning = CurrentSessionStartUtc is not null;
        Status = IsRunning ? "Running" : "Stopped";

        var totalDuration = AccumulatedDuration;
        if (CurrentSessionStartUtc is not null)
        {
            totalDuration += nowUtc - CurrentSessionStartUtc.Value;
        }

        DurationText = totalDuration.ToString(@"hh\:mm\:ss");
    }
}
