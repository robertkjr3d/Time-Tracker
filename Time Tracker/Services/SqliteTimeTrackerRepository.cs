using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Time_Tracker.Models;

namespace Time_Tracker.Services;

public class SqliteTimeTrackerRepository : ITimeTrackerRepository
{
    private const string TimestampFormat = "O";
    private const string DefaultParentCategoryName = "General";
    private readonly string _connectionString;
    private readonly SemaphoreSlim _initializeLock = new(1, 1);
    private bool _isInitialized;

    public SqliteTimeTrackerRepository()
        : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Time Tracker", "time-tracker.db"))
    {
    }

    public SqliteTimeTrackerRepository(string databasePath)
    {
        string? directoryPath = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
        }.ToString();
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        await _initializeLock.WaitAsync();
        try
        {
            if (_isInitialized)
            {
                return;
            }

            await using var connection = await OpenConnectionAsync();

            var command = connection.CreateCommand();
            command.CommandText = """
                PRAGMA journal_mode = WAL;
                PRAGMA foreign_keys = ON;

                CREATE TABLE IF NOT EXISTS ParentCategories (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL COLLATE NOCASE UNIQUE
                );

                CREATE TABLE IF NOT EXISTS Activities (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ParentCategoryId INTEGER NOT NULL DEFAULT 1,
                    Name TEXT NOT NULL,
                    CreatedAtUtc TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS TimeEntries (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ActivityId INTEGER NOT NULL,
                    StartUtc TEXT NOT NULL,
                    EndUtc TEXT NULL,
                    Notes TEXT NOT NULL DEFAULT '',
                    FOREIGN KEY(ActivityId) REFERENCES Activities(Id) ON DELETE CASCADE
                );
                """;
            await command.ExecuteNonQueryAsync();

            await EnsureActivityParentCategoryColumnAsync(connection);

            var defaultParentCategoryId = await EnsureDefaultParentCategoryAsync(connection);

            var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = """
                UPDATE Activities
                SET ParentCategoryId = $defaultParentCategoryId
                WHERE ParentCategoryId IS NULL OR ParentCategoryId = 0;
                """;
            updateCommand.Parameters.AddWithValue("$defaultParentCategoryId", defaultParentCategoryId);
            await updateCommand.ExecuteNonQueryAsync();

            _isInitialized = true;
        }
        finally
        {
            _initializeLock.Release();
        }
    }

    public async Task<IReadOnlyList<ParentCategoryRecord>> GetParentCategoriesAsync()
    {
        await InitializeAsync();

        const string sql = """
            SELECT Id, Name
            FROM ParentCategories
            ORDER BY Name;
            """;

        var records = new List<ParentCategoryRecord>();

        await using var connection = await OpenConnectionAsync();
        var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            records.Add(new ParentCategoryRecord
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
            });
        }

        return records;
    }

    public async Task<long> CreateParentCategoryAsync(string name)
    {
        await InitializeAsync();

        var normalizedName = name.Trim();

        await using var connection = await OpenConnectionAsync();

        var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = """
            INSERT OR IGNORE INTO ParentCategories (Name)
            VALUES ($name);
            """;
        insertCommand.Parameters.AddWithValue("$name", normalizedName);
        await insertCommand.ExecuteNonQueryAsync();

        var selectCommand = connection.CreateCommand();
        selectCommand.CommandText = """
            SELECT Id
            FROM ParentCategories
            WHERE Name = $name COLLATE NOCASE;
            """;
        selectCommand.Parameters.AddWithValue("$name", normalizedName);

        var result = await selectCommand.ExecuteScalarAsync();
        return Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }

    public async Task<IReadOnlyList<ActivityRecord>> GetActivitiesAsync(long? parentCategoryId)
    {
        await InitializeAsync();

        const string sql = """
            SELECT
                a.Id,
                a.ParentCategoryId,
                p.Name,
                a.Name,
                a.CreatedAtUtc,
                SUM(CASE
                    WHEN t.EndUtc IS NOT NULL THEN unixepoch(t.EndUtc) - unixepoch(t.StartUtc)
                    ELSE 0
                END) AS CompletedSeconds,
                MIN(CASE WHEN t.EndUtc IS NULL THEN t.StartUtc END) AS CurrentSessionStartUtc
            FROM Activities a
            INNER JOIN ParentCategories p ON p.Id = a.ParentCategoryId
            LEFT JOIN TimeEntries t ON t.ActivityId = a.Id
            WHERE $parentCategoryId IS NULL OR a.ParentCategoryId = $parentCategoryId
            GROUP BY a.Id, a.ParentCategoryId, p.Name, a.Name, a.CreatedAtUtc
            ORDER BY a.CreatedAtUtc DESC;
            """;

        var records = new List<ActivityRecord>();

        await using var connection = await OpenConnectionAsync();
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$parentCategoryId", parentCategoryId is null ? DBNull.Value : parentCategoryId.Value);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            DateTime? currentSessionStart = reader.IsDBNull(6)
                ? null
                : ParseUtc(reader.GetString(6));

            records.Add(new ActivityRecord
            {
                Id = reader.GetInt64(0),
                ParentCategoryId = reader.GetInt64(1),
                ParentCategoryName = reader.GetString(2),
                Name = reader.GetString(3),
                CreatedAtUtc = ParseUtc(reader.GetString(4)),
                AccumulatedDuration = TimeSpan.FromSeconds(reader.IsDBNull(5) ? 0 : reader.GetInt64(5)),
                CurrentSessionStartUtc = currentSessionStart,
                IsRunning = currentSessionStart is not null,
            });
        }

        return records;
    }

    public async Task<IReadOnlyList<TimeEntryRecord>> GetTimeEntriesAsync(long activityId)
    {
        await InitializeAsync();

        const string sql = """
            SELECT Id, ActivityId, StartUtc, EndUtc, Notes
            FROM TimeEntries
            WHERE ActivityId = $activityId
            ORDER BY StartUtc DESC;
            """;

        var records = new List<TimeEntryRecord>();

        await using var connection = await OpenConnectionAsync();
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$activityId", activityId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            records.Add(new TimeEntryRecord
            {
                Id = reader.GetInt64(0),
                ActivityId = reader.GetInt64(1),
                StartUtc = ParseUtc(reader.GetString(2)),
                EndUtc = reader.IsDBNull(3) ? null : ParseUtc(reader.GetString(3)),
                Notes = reader.GetString(4),
            });
        }

        return records;
    }

    public async Task<long> CreateActivityAsync(string name, long parentCategoryId, DateTime createdAtUtc)
    {
        await InitializeAsync();

        const string sql = """
            INSERT INTO Activities (ParentCategoryId, Name, CreatedAtUtc)
            VALUES ($parentCategoryId, $name, $createdAtUtc);
            SELECT last_insert_rowid();
            """;

        await using var connection = await OpenConnectionAsync();
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$parentCategoryId", parentCategoryId);
        command.Parameters.AddWithValue("$name", name.Trim());
        command.Parameters.AddWithValue("$createdAtUtc", FormatUtc(createdAtUtc));

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }

    public async Task<long> StartActivityAsync(long activityId, DateTime startedAtUtc)
    {
        await InitializeAsync();

        const string sql = """
            INSERT INTO TimeEntries (ActivityId, StartUtc)
            VALUES ($activityId, $startedAtUtc);
            SELECT last_insert_rowid();
            """;

        await using var connection = await OpenConnectionAsync();
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$activityId", activityId);
        command.Parameters.AddWithValue("$startedAtUtc", FormatUtc(startedAtUtc));

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }

    public async Task StopActivityAsync(long activityId, DateTime stoppedAtUtc)
    {
        await InitializeAsync();

        const string sql = """
            UPDATE TimeEntries
            SET EndUtc = $stoppedAtUtc
            WHERE Id = (
                SELECT Id
                FROM TimeEntries
                WHERE ActivityId = $activityId AND EndUtc IS NULL
                ORDER BY StartUtc DESC
                LIMIT 1
            );
            """;

        await using var connection = await OpenConnectionAsync();
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$activityId", activityId);
        command.Parameters.AddWithValue("$stoppedAtUtc", FormatUtc(stoppedAtUtc));
        await command.ExecuteNonQueryAsync();
    }

    public async Task RenameActivityAsync(long activityId, string name)
    {
        await InitializeAsync();

        const string sql = """
            UPDATE Activities
            SET Name = $name
            WHERE Id = $activityId;
            """;

        await using var connection = await OpenConnectionAsync();
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$activityId", activityId);
        command.Parameters.AddWithValue("$name", name.Trim());
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateTimeEntryAsync(EditSessionModel model)
    {
        await InitializeAsync();

        if (model.ActivityId is null || model.TimeEntryId is null)
        {
            throw new InvalidOperationException("Activity and time entry identifiers are required.");
        }

        await using var connection = await OpenConnectionAsync();
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync();

        var updateActivity = connection.CreateCommand();
        updateActivity.Transaction = transaction;
        updateActivity.CommandText = """
            UPDATE Activities
            SET Name = $name
            WHERE Id = $activityId;
            """;
        updateActivity.Parameters.AddWithValue("$activityId", model.ActivityId.Value);
        updateActivity.Parameters.AddWithValue("$name", model.ActivityName.Trim());
        await updateActivity.ExecuteNonQueryAsync();

        var updateEntry = connection.CreateCommand();
        updateEntry.Transaction = transaction;
        updateEntry.CommandText = """
            UPDATE TimeEntries
            SET StartUtc = $startUtc,
                EndUtc = $endUtc,
                Notes = $notes
            WHERE Id = $timeEntryId;
            """;
        updateEntry.Parameters.AddWithValue("$timeEntryId", model.TimeEntryId.Value);
        updateEntry.Parameters.AddWithValue("$startUtc", FormatUtc(model.StartLocal.ToUniversalTime()));
        updateEntry.Parameters.AddWithValue("$endUtc", model.EndLocal is null ? DBNull.Value : FormatUtc(model.EndLocal.Value.ToUniversalTime()));
        updateEntry.Parameters.AddWithValue("$notes", model.Notes.Trim());
        await updateEntry.ExecuteNonQueryAsync();

        await transaction.CommitAsync();
    }

    public async Task DeleteTimeEntryAsync(long timeEntryId)
    {
        await InitializeAsync();

        const string sql = """
            DELETE FROM TimeEntries
            WHERE Id = $timeEntryId;
            """;

        await using var connection = await OpenConnectionAsync();
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$timeEntryId", timeEntryId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<ActivityExportRow>> GetExportRowsAsync()
    {
        await InitializeAsync();

        const string sql = """
            SELECT
                p.Name,
                a.Name,
                a.CreatedAtUtc,
                t.StartUtc,
                t.EndUtc,
                t.Notes
            FROM Activities a
            INNER JOIN ParentCategories p ON p.Id = a.ParentCategoryId
            INNER JOIN TimeEntries t ON t.ActivityId = a.Id
            ORDER BY p.Name, a.Name, t.StartUtc;
            """;

        var rows = new List<ActivityExportRow>();

        await using var connection = await OpenConnectionAsync();
        var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var startUtc = ParseUtc(reader.GetString(3));
            DateTime? endUtc = reader.IsDBNull(4) ? null : ParseUtc(reader.GetString(4));

            rows.Add(new ActivityExportRow
            {
                ParentCategory = reader.GetString(0),
                Activity = reader.GetString(1),
                CreatedAtLocal = ParseUtc(reader.GetString(2)).ToLocalTime(),
                StartLocal = startUtc.ToLocalTime(),
                EndLocal = endUtc?.ToLocalTime(),
                Duration = (endUtc ?? DateTime.UtcNow) - startUtc,
                Notes = reader.GetString(5),
                Status = endUtc is null ? "Running" : "Stopped",
            });
        }

        return rows;
    }

    private async Task EnsureActivityParentCategoryColumnAsync(SqliteConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(Activities);";

        var hasParentCategoryId = false;
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), "ParentCategoryId", StringComparison.OrdinalIgnoreCase))
            {
                hasParentCategoryId = true;
                break;
            }
        }

        if (!hasParentCategoryId)
        {
            var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = "ALTER TABLE Activities ADD COLUMN ParentCategoryId INTEGER NULL;";
            await alterCommand.ExecuteNonQueryAsync();
        }
    }

    private static async Task<long> EnsureDefaultParentCategoryAsync(SqliteConnection connection)
    {
        var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = """
            INSERT OR IGNORE INTO ParentCategories (Name)
            VALUES ($name);
            """;
        insertCommand.Parameters.AddWithValue("$name", DefaultParentCategoryName);
        await insertCommand.ExecuteNonQueryAsync();

        var selectCommand = connection.CreateCommand();
        selectCommand.CommandText = """
            SELECT Id
            FROM ParentCategories
            WHERE Name = $name COLLATE NOCASE;
            """;
        selectCommand.Parameters.AddWithValue("$name", DefaultParentCategoryName);

        var result = await selectCommand.ExecuteScalarAsync();
        return Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }

    private async Task<SqliteConnection> OpenConnectionAsync()
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static string FormatUtc(DateTime value)
    {
        return value.ToUniversalTime().ToString(TimestampFormat, CultureInfo.InvariantCulture);
    }

    private static DateTime ParseUtc(string value)
    {
        return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }
}
