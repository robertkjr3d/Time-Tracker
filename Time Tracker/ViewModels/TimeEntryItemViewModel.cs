using System;
using Time_Tracker.Models;

namespace Time_Tracker.ViewModels;

public class TimeEntryItemViewModel
{
    public TimeEntryItemViewModel(TimeEntryRecord record, string activityName)
    {
        Id = record.Id;
        ActivityId = record.ActivityId;
        ActivityName = activityName;
        StartLocal = record.StartUtc.ToLocalTime();
        EndLocal = record.EndUtc?.ToLocalTime();
        Notes = record.Notes;
        Refresh(DateTime.UtcNow);
    }

    public long Id { get; }

    public long ActivityId { get; }

    public string ActivityName { get; }

    public DateTime StartLocal { get; }

    public DateTime? EndLocal { get; }

    public string Notes { get; }

    public string Status { get; private set; } = "Stopped";

    public string DurationText { get; private set; } = "00:00:00";

    public void Refresh(DateTime nowUtc)
    {
        Status = EndLocal is null ? "Running" : "Stopped";
        DurationText = ((EndLocal ?? nowUtc.ToLocalTime()) - StartLocal).ToString(@"hh\:mm\:ss");
    }

    public EditSessionModel ToEditModel()
    {
        return new EditSessionModel
        {
            ActivityId = ActivityId,
            TimeEntryId = Id,
            ActivityName = ActivityName,
            StartLocal = StartLocal,
            EndLocal = EndLocal,
            Notes = Notes,
        };
    }
}
