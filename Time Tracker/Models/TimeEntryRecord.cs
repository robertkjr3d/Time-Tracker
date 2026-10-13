using System;

namespace Time_Tracker.Models;

public class TimeEntryRecord
{
    public long Id { get; set; }

    public long ActivityId { get; set; }

    public DateTime StartUtc { get; set; }

    public DateTime? EndUtc { get; set; }

    public string Notes { get; set; } = string.Empty;

    public TimeSpan Duration => (EndUtc ?? DateTime.UtcNow) - StartUtc;
}
