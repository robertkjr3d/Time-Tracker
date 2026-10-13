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
