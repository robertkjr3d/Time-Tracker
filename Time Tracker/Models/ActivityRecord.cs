using System;

namespace Time_Tracker.Models;

public class ActivityRecord
{
    public long Id { get; set; }

    public long ParentCategoryId { get; set; }

    public string ParentCategoryName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public bool IsRunning { get; set; }

    public DateTime? CurrentSessionStartUtc { get; set; }

    public TimeSpan AccumulatedDuration { get; set; }
}
