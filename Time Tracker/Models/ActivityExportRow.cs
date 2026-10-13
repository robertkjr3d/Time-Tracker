using System;

namespace Time_Tracker.Models;

public class ActivityExportRow
{
    public string ParentCategory { get; set; } = string.Empty;

    public string Activity { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAtLocal { get; set; }

    public DateTime StartLocal { get; set; }

    public DateTime? EndLocal { get; set; }

    public TimeSpan Duration { get; set; }

    public string Notes { get; set; } = string.Empty;
}
