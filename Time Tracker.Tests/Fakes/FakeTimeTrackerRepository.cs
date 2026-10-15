using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Time_Tracker.Models;
using Time_Tracker.Services;

namespace Time_Tracker.Tests.Fakes;

internal sealed class FakeTimeTrackerRepository : ITimeTrackerRepository
{
    private readonly List<ParentCategoryRecord> _parentCategories = new();
    private readonly List<ActivityRecord> _activities = new();
    private readonly Dictionary<long, List<TimeEntryRecord>> _timeEntries = new();
    private long _nextParentId = 1;
    private long _nextActivityId = 1;
    private long _nextTimeEntryId = 1;

    public bool InitializeCalled { get; private set; }

    public Task InitializeAsync()
    {
        InitializeCalled = true;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ParentCategoryRecord>> GetParentCategoriesAsync()
    {
        return Task.FromResult<IReadOnlyList<ParentCategoryRecord>>(_parentCategories.OrderBy(x => x.Name).ToList());
    }

    public Task<long> CreateParentCategoryAsync(string name)
    {
        var existing = _parentCategories.FirstOrDefault(x => string.Equals(x.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return Task.FromResult(existing.Id);
        }

        var record = new ParentCategoryRecord
        {
            Id = _nextParentId++,
            Name = name.Trim(),
        };
        _parentCategories.Add(record);
        return Task.FromResult(record.Id);
    }

    public Task RenameParentCategoryAsync(long parentCategoryId, string name)
    {
        var parent = _parentCategories.Single(x => x.Id == parentCategoryId);
        var trimmedName = name.Trim();
        parent.Name = trimmedName;

        foreach (var activity in _activities.Where(x => x.ParentCategoryId == parentCategoryId))
        {
            activity.ParentCategoryName = trimmedName;
        }

        return Task.CompletedTask;
    }

    public Task DeleteParentCategoryAsync(long parentCategoryId)
    {
        if (_parentCategories.Count <= 1)
        {
            throw new InvalidOperationException("At least one parent/category must remain.");
        }

        _parentCategories.RemoveAll(x => x.Id == parentCategoryId);
        var activityIds = _activities.Where(x => x.ParentCategoryId == parentCategoryId).Select(x => x.Id).ToList();
        _activities.RemoveAll(x => x.ParentCategoryId == parentCategoryId);
        foreach (var activityId in activityIds)
        {
            _timeEntries.Remove(activityId);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ActivityRecord>> GetActivitiesAsync(long? parentCategoryId)
    {
        var results = _activities
            .Where(x => parentCategoryId is null || x.ParentCategoryId == parentCategoryId.Value)
            .OrderBy(x => _timeEntries.TryGetValue(x.Id, out var entries) && entries.Count > 0 ? entries.Min(entry => entry.StartUtc) : x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .Select(x =>
            {
                var displayCreatedAtUtc = _timeEntries.TryGetValue(x.Id, out var entries) && entries.Count > 0
                    ? entries.Min(entry => entry.StartUtc)
                    : x.CreatedAtUtc;
                return new ActivityRecord
                {
                    Id = x.Id,
                    ParentCategoryId = x.ParentCategoryId,
                    ParentCategoryName = x.ParentCategoryName,
                    Name = x.Name,
                    CreatedAtUtc = displayCreatedAtUtc,
                    IsRunning = x.IsRunning,
                    CurrentSessionStartUtc = x.CurrentSessionStartUtc,
                    AccumulatedDuration = x.AccumulatedDuration,
                };
            })
            .ToList();
        return Task.FromResult<IReadOnlyList<ActivityRecord>>(results);
    }

    public Task<IReadOnlyList<TimeEntryRecord>> GetTimeEntriesAsync(long activityId)
    {
        _timeEntries.TryGetValue(activityId, out var entries);
        return Task.FromResult<IReadOnlyList<TimeEntryRecord>>(entries?.OrderByDescending(x => x.StartUtc).ToList() ?? new List<TimeEntryRecord>());
    }

    public Task<long> CreateActivityAsync(string name, long parentCategoryId, DateTime createdAtUtc)
    {
        var parentName = _parentCategories.Single(x => x.Id == parentCategoryId).Name;
        var record = new ActivityRecord
        {
            Id = _nextActivityId++,
            ParentCategoryId = parentCategoryId,
            ParentCategoryName = parentName,
            Name = name.Trim(),
            CreatedAtUtc = createdAtUtc,
            AccumulatedDuration = TimeSpan.Zero,
        };
        _activities.Add(record);
        return Task.FromResult(record.Id);
    }

    public Task<long> StartActivityAsync(long activityId, DateTime startedAtUtc)
    {
        var entry = new TimeEntryRecord
        {
            Id = _nextTimeEntryId++,
            ActivityId = activityId,
            StartUtc = startedAtUtc,
        };

        if (!_timeEntries.TryGetValue(activityId, out var entries))
        {
            entries = new List<TimeEntryRecord>();
            _timeEntries[activityId] = entries;
        }

        entries.Add(entry);

        var activity = _activities.Single(x => x.Id == activityId);
        activity.CurrentSessionStartUtc = startedAtUtc;
        activity.IsRunning = true;

        return Task.FromResult(entry.Id);
    }

    public Task StopActivityAsync(long activityId, DateTime stoppedAtUtc)
    {
        var activity = _activities.Single(x => x.Id == activityId);
        var entry = _timeEntries[activityId].Last(x => x.EndUtc is null);
        entry.EndUtc = stoppedAtUtc;
        activity.AccumulatedDuration += stoppedAtUtc - entry.StartUtc;
        activity.CurrentSessionStartUtc = null;
        activity.IsRunning = false;
        return Task.CompletedTask;
    }

    public Task RenameActivityAsync(long activityId, string name)
    {
        _activities.Single(x => x.Id == activityId).Name = name.Trim();
        return Task.CompletedTask;
    }

    public Task DeleteActivityAsync(long activityId)
    {
        _activities.RemoveAll(x => x.Id == activityId);
        _timeEntries.Remove(activityId);
        return Task.CompletedTask;
    }

    public Task UpdateTimeEntryAsync(EditSessionModel model)
    {
        var activity = _activities.Single(x => x.Id == model.ActivityId);
        activity.Name = model.ActivityName.Trim();

        var entry = _timeEntries[model.ActivityId!.Value].Single(x => x.Id == model.TimeEntryId);
        entry.StartUtc = model.StartLocal.ToUniversalTime();
        entry.EndUtc = model.EndLocal?.ToUniversalTime();
        entry.Notes = model.Notes.Trim();
        activity.CurrentSessionStartUtc = entry.EndUtc is null ? entry.StartUtc : null;
        activity.IsRunning = entry.EndUtc is null;
        return Task.CompletedTask;
    }

    public Task DeleteTimeEntryAsync(long timeEntryId)
    {
        foreach (var pair in _timeEntries)
        {
            var entry = pair.Value.SingleOrDefault(x => x.Id == timeEntryId);
            if (entry is not null)
            {
                pair.Value.Remove(entry);
                break;
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ActivityExportRow>> GetExportRowsAsync(long? parentCategoryId = null)
    {
        var rows = new List<ActivityExportRow>();
        foreach (var activity in _activities
                     .Where(x => parentCategoryId is null || x.ParentCategoryId == parentCategoryId.Value)
                     .OrderBy(x => x.CreatedAtUtc)
                     .ThenBy(x => x.Id))
        {
            if (!_timeEntries.TryGetValue(activity.Id, out var entries))
            {
                continue;
            }

            rows.AddRange(entries
                .OrderBy(entry => entry.StartUtc)
                .ThenBy(entry => entry.Id)
                .Select(entry => new ActivityExportRow
                {
                    ParentCategory = activity.ParentCategoryName,
                    Activity = activity.Name,
                    Status = entry.EndUtc is null ? "Running" : "Stopped",
                    CreatedAtLocal = (_timeEntries.TryGetValue(activity.Id, out var activityEntries) && activityEntries.Count > 0
                        ? activityEntries.Min(x => x.StartUtc)
                        : activity.CreatedAtUtc).ToLocalTime(),
                    StartLocal = entry.StartUtc.ToLocalTime(),
                    EndLocal = entry.EndUtc?.ToLocalTime(),
                    Duration = (entry.EndUtc ?? DateTime.UtcNow) - entry.StartUtc,
                    Notes = entry.Notes,
                }));
        }

        return Task.FromResult<IReadOnlyList<ActivityExportRow>>(rows);
    }
}
