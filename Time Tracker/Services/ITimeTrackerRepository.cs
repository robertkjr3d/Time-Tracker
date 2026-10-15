using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Time_Tracker.Models;

namespace Time_Tracker.Services;

public interface ITimeTrackerRepository
{
    Task InitializeAsync();

    Task<IReadOnlyList<ParentCategoryRecord>> GetParentCategoriesAsync();

    Task<long> CreateParentCategoryAsync(string name);

    Task RenameParentCategoryAsync(long parentCategoryId, string name);

    Task DeleteParentCategoryAsync(long parentCategoryId);

    Task<IReadOnlyList<ActivityRecord>> GetActivitiesAsync(long? parentCategoryId);

    Task<IReadOnlyList<TimeEntryRecord>> GetTimeEntriesAsync(long activityId);

    Task<long> CreateActivityAsync(string name, long parentCategoryId, DateTime createdAtUtc);

    Task<long> StartActivityAsync(long activityId, DateTime startedAtUtc);

    Task StopActivityAsync(long activityId, DateTime stoppedAtUtc);

    Task RenameActivityAsync(long activityId, string name);

    Task DeleteActivityAsync(long activityId);

    Task UpdateTimeEntryAsync(EditSessionModel model);

    Task DeleteTimeEntryAsync(long timeEntryId);

    Task<IReadOnlyList<ActivityExportRow>> GetExportRowsAsync(long? parentCategoryId = null);
}
