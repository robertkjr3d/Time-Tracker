using System;

namespace Time_Tracker.Models;

public class EditSessionModel
{
    public long? ActivityId { get; set; }

    public long? TimeEntryId { get; set; }

    public string ActivityName { get; set; } = string.Empty;

    public DateTime StartLocal { get; set; }

    public DateTime? EndLocal { get; set; }

    public string Notes { get; set; } = string.Empty;

    public bool IsOpenSession => EndLocal is null;
}
