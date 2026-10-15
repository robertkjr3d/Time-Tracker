using Time_Tracker.Services;

namespace Time_Tracker.Tests;

public class SqliteTimeTrackerRepositoryTests : IDisposable
{
    private readonly string _testDirectory;

    public SqliteTimeTrackerRepositoryTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "TimeTrackerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public async Task InitializeAsync_CreatesDefaultParentCategory()
    {
        var repository = CreateRepository();

        await repository.InitializeAsync();
        var parents = await repository.GetParentCategoriesAsync();

        var parent = Assert.Single(parents);
        Assert.Equal("General", parent.Name);
    }

    [Fact]
    public async Task CreateActivityAsync_PersistsParentAndRunningState()
    {
        var repository = CreateRepository();
        await repository.InitializeAsync();
        var parentId = await repository.CreateParentCategoryAsync("Client A");
        var createdAt = new DateTime(2024, 1, 10, 9, 0, 0, DateTimeKind.Utc);

        var activityId = await repository.CreateActivityAsync("Draft report", parentId, createdAt);
        await repository.StartActivityAsync(activityId, createdAt.AddMinutes(15));

        var activities = await repository.GetActivitiesAsync(parentId);
        var activity = Assert.Single(activities);
        Assert.Equal("Client A", activity.ParentCategoryName);
        Assert.Equal("Draft report", activity.Name);
        Assert.True(activity.IsRunning);
        Assert.Equal(createdAt.AddMinutes(15), activity.CurrentSessionStartUtc);
    }

    [Fact]
    public async Task DeleteParentCategoryAsync_WhenOnlyOneParentExists_Throws()
    {
        var repository = CreateRepository();
        await repository.InitializeAsync();
        var parent = Assert.Single(await repository.GetParentCategoriesAsync());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => repository.DeleteParentCategoryAsync(parent.Id));

        Assert.Equal("At least one parent/category must remain.", exception.Message);
    }

    [Fact]
    public async Task GetActivitiesAsync_ReturnsActivitiesOldestToNewest()
    {
        var repository = CreateRepository();
        await repository.InitializeAsync();
        var parentId = await repository.CreateParentCategoryAsync("Client A");
        var later = new DateTime(2024, 1, 10, 11, 0, 0, DateTimeKind.Utc);
        var earlier = new DateTime(2024, 1, 10, 9, 0, 0, DateTimeKind.Utc);

        await repository.CreateActivityAsync("Second", parentId, later);
        await repository.CreateActivityAsync("First", parentId, earlier);

        var activities = await repository.GetActivitiesAsync(parentId);

        Assert.Collection(activities,
            first => Assert.Equal("First", first.Name),
            second => Assert.Equal("Second", second.Name));
    }

    [Fact]
    public async Task DeleteParentCategoryAsync_RemovesActivitiesAndTimeEntries()
    {
        var repository = CreateRepository();
        await repository.InitializeAsync();
        var projectParentId = await repository.CreateParentCategoryAsync("Project");
        await repository.CreateParentCategoryAsync("Archive");
        var createdAt = new DateTime(2024, 1, 10, 9, 0, 0, DateTimeKind.Utc);
        var activityId = await repository.CreateActivityAsync("Draft report", projectParentId, createdAt);
        await repository.StartActivityAsync(activityId, createdAt.AddMinutes(15));

        await repository.DeleteParentCategoryAsync(projectParentId);

        var parents = await repository.GetParentCategoriesAsync();
        Assert.DoesNotContain(parents, x => x.Id == projectParentId);
        Assert.Empty(await repository.GetActivitiesAsync(projectParentId));
        Assert.Empty(await repository.GetTimeEntriesAsync(activityId));
    }

    [Fact]
    public async Task ExistingDatabaseWithoutParentColumn_IsMigratedToDefaultParent()
    {
        var databasePath = Path.Combine(_testDirectory, "migration.db");
        await CreateLegacyDatabaseAsync(databasePath);
        var repository = new SqliteTimeTrackerRepository(databasePath);

        await repository.InitializeAsync();
        var parents = await repository.GetParentCategoriesAsync();
        var parent = Assert.Single(parents);
        var activities = await repository.GetActivitiesAsync(parent.Id);

        var activity = Assert.Single(activities);
        Assert.Equal("General", parent.Name);
        Assert.Equal(parent.Id, activity.ParentCategoryId);
        Assert.Equal("Legacy Task", activity.Name);
    }

    [Fact]
    public async Task InitializeAsync_DoesNotRecreateGeneralAfterDefaultParentIsRenamed()
    {
        var databasePath = Path.Combine(_testDirectory, "renamed-default.db");
        var repository = new SqliteTimeTrackerRepository(databasePath);

        await repository.InitializeAsync();
        var originalParent = Assert.Single(await repository.GetParentCategoriesAsync());
        await repository.RenameParentCategoryAsync(originalParent.Id, "Main");

        var restartedRepository = new SqliteTimeTrackerRepository(databasePath);
        await restartedRepository.InitializeAsync();
        var parents = await restartedRepository.GetParentCategoriesAsync();

        var parent = Assert.Single(parents);
        Assert.Equal("Main", parent.Name);
    }

    [Fact]
    public async Task GetExportRowsAsync_OrdersRowsByActivityCreatedDate()
    {
        var repository = CreateRepository();
        await repository.InitializeAsync();
        var parentId = await repository.CreateParentCategoryAsync("Client A");
        var laterCreated = new DateTime(2024, 1, 10, 11, 0, 0, DateTimeKind.Utc);
        var earlierCreated = new DateTime(2024, 1, 10, 9, 0, 0, DateTimeKind.Utc);

        var laterActivityId = await repository.CreateActivityAsync("Second", parentId, laterCreated);
        var earlierActivityId = await repository.CreateActivityAsync("First", parentId, earlierCreated);

        await repository.StartActivityAsync(laterActivityId, laterCreated.AddMinutes(10));
        await repository.StopActivityAsync(laterActivityId, laterCreated.AddMinutes(40));
        await repository.StartActivityAsync(earlierActivityId, earlierCreated.AddMinutes(10));
        await repository.StopActivityAsync(earlierActivityId, earlierCreated.AddMinutes(30));

        var rows = await repository.GetExportRowsAsync(parentId);

        Assert.Collection(rows,
            first => Assert.Equal("First", first.Activity),
            second => Assert.Equal("Second", second.Activity));
    }

    [Fact]
    public async Task UpdateTimeEntryAsync_UpdatesDisplayedCreatedDateFromEditedStart()
    {
        var repository = CreateRepository();
        await repository.InitializeAsync();
        var parentId = await repository.CreateParentCategoryAsync("Client A");
        var createdAt = new DateTime(2024, 1, 10, 9, 0, 0, DateTimeKind.Utc);
        var activityId = await repository.CreateActivityAsync("Task", parentId, createdAt);
        var originalStart = createdAt.AddMinutes(30);
        var updatedStart = createdAt.AddMinutes(-15);
        var timeEntryId = await repository.StartActivityAsync(activityId, originalStart);
        await repository.StopActivityAsync(activityId, originalStart.AddMinutes(10));

        await repository.UpdateTimeEntryAsync(new Models.EditSessionModel
        {
            ActivityId = activityId,
            TimeEntryId = timeEntryId,
            ActivityName = "Task",
            StartLocal = updatedStart.ToLocalTime(),
            EndLocal = originalStart.AddMinutes(10).ToLocalTime(),
            Notes = string.Empty,
        });

        var activity = Assert.Single(await repository.GetActivitiesAsync(parentId));
        Assert.Equal(updatedStart, activity.CreatedAtUtc);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private SqliteTimeTrackerRepository CreateRepository()
    {
        return new SqliteTimeTrackerRepository(Path.Combine(_testDirectory, $"{Guid.NewGuid():N}.db"));
    }

    private static async Task CreateLegacyDatabaseAsync(string databasePath)
    {
        await using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE Activities (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL
            );

            CREATE TABLE TimeEntries (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ActivityId INTEGER NOT NULL,
                StartUtc TEXT NOT NULL,
                EndUtc TEXT NULL,
                Notes TEXT NOT NULL DEFAULT ''
            );

            INSERT INTO Activities (Name, CreatedAtUtc)
            VALUES ('Legacy Task', '2024-01-01T09:00:00.0000000Z');
            """;

        await command.ExecuteNonQueryAsync();
    }
}
