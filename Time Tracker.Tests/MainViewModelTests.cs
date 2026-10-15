using Time_Tracker.Tests.Fakes;
using Time_Tracker.ViewModels;

namespace Time_Tracker.Tests;

public class MainViewModelTests
{
    [Fact]
    public async Task InitializeAsync_LoadsParentCategories_AndSelectsFirstParent()
    {
        var repository = new FakeTimeTrackerRepository();
        await repository.CreateParentCategoryAsync("Beta");
        await repository.CreateParentCategoryAsync("Alpha");
        var viewModel = new MainViewModel(repository, CreateStatePath());

        await viewModel.InitializeAsync();

        Assert.True(repository.InitializeCalled);
        Assert.Equal(2, viewModel.ParentCategories.Count);
        Assert.Equal("Alpha", viewModel.SelectedParentCategory?.Name);
    }

    [Fact]
    public async Task AddParentCategoryCommand_AddsAndSelectsNewParent()
    {
        var repository = new FakeTimeTrackerRepository();
        await repository.CreateParentCategoryAsync("General");
        var viewModel = new MainViewModel(repository, CreateStatePath());

        await viewModel.InitializeAsync();
        viewModel.NewParentCategoryName = "Client A";
        await viewModel.AddParentCategoryCommand.ExecuteAsync(null);

        Assert.Contains(viewModel.ParentCategories, x => x.Name == "Client A");
        Assert.Equal("Client A", viewModel.SelectedParentCategory?.Name);
        Assert.Equal(string.Empty, viewModel.NewParentCategoryName);
        Assert.True(viewModel.IsStatusVisible);
        Assert.Equal("Parent 'Client A' is ready.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task RenameParentCategoryCommand_RenamesSelectedParent()
    {
        var repository = new FakeTimeTrackerRepository();
        var parentId = await repository.CreateParentCategoryAsync("Client A");
        var viewModel = new MainViewModel(repository, CreateStatePath());

        await viewModel.InitializeAsync();
        viewModel.SelectedParentCategory = viewModel.ParentCategories.Single(x => x.Id == parentId);
        viewModel.NewParentCategoryName = "Client B";

        await viewModel.RenameParentCategoryCommand.ExecuteAsync(null);

        Assert.Equal("Client B", viewModel.SelectedParentCategory?.Name);
        Assert.Contains(viewModel.ParentCategories, x => x.Name == "Client B");
        Assert.Equal(string.Empty, viewModel.NewParentCategoryName);
        Assert.Equal("Parent renamed to 'Client B'.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task CreateActivityCommand_CreatesRunningActivityUnderSelectedParent()
    {
        var repository = new FakeTimeTrackerRepository();
        var parentId = await repository.CreateParentCategoryAsync("Client A");
        var viewModel = new MainViewModel(repository, CreateStatePath())
        {
            NewActivityName = "Write proposal",
        };

        await viewModel.InitializeAsync();
        viewModel.SelectedParentCategory = viewModel.ParentCategories.Single(x => x.Id == parentId);

        await viewModel.CreateActivityCommand.ExecuteAsync(null);

        var activity = Assert.Single(viewModel.Activities);
        Assert.Equal("Write proposal", activity.Name);
        Assert.Equal("Running", activity.Status);
        Assert.Equal("Client A", viewModel.SelectedParentCategory?.Name);
        Assert.Equal(string.Empty, viewModel.NewActivityName);
        Assert.Contains("Created and started 'Write proposal'.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task ExportCsvAsync_WithSelectedJobScope_ExportsOnlyThatJob()
    {
        var repository = new FakeTimeTrackerRepository();
        var clientAId = await repository.CreateParentCategoryAsync("Client A");
        var clientBId = await repository.CreateParentCategoryAsync("Client B");
        var createdAt = new DateTime(2024, 1, 10, 9, 0, 0, DateTimeKind.Utc);

        var activityAId = await repository.CreateActivityAsync("Draft report", clientAId, createdAt);
        await repository.StartActivityAsync(activityAId, createdAt.AddMinutes(10));
        await repository.StopActivityAsync(activityAId, createdAt.AddMinutes(40));

        var activityBId = await repository.CreateActivityAsync("Budget review", clientBId, createdAt);
        await repository.StartActivityAsync(activityBId, createdAt.AddMinutes(20));
        await repository.StopActivityAsync(activityBId, createdAt.AddMinutes(45));

        var viewModel = new MainViewModel(repository, CreateStatePath());
        await viewModel.InitializeAsync();

        var csvPath = Path.Combine(Path.GetTempPath(), $"time-tracker-{Guid.NewGuid():N}.csv");

        try
        {
            await viewModel.ExportCsvAsync(csvPath, clientAId);
            var csv = await File.ReadAllTextAsync(csvPath);

            Assert.Contains("\"Client A\"", csv);
            Assert.DoesNotContain("\"Client B\"", csv);
            Assert.Equal("Exported 1 rows to CSV.", viewModel.StatusMessage);
        }
        finally
        {
            if (File.Exists(csvPath))
            {
                File.Delete(csvPath);
            }
        }
    }

    [Fact]
    public async Task InitializeAsync_RestoresPreviouslySelectedJob()
    {
        var repository = new FakeTimeTrackerRepository();
        await repository.CreateParentCategoryAsync("Alpha");
        var betaId = await repository.CreateParentCategoryAsync("Beta");
        var statePath = Path.Combine(Path.GetTempPath(), $"selected-job-{Guid.NewGuid():N}.txt");

        try
        {
            var firstViewModel = new MainViewModel(repository, statePath);
            await firstViewModel.InitializeAsync();
            firstViewModel.SelectedParentCategory = firstViewModel.ParentCategories.Single(x => x.Id == betaId);

            var secondViewModel = new MainViewModel(repository, statePath);
            await secondViewModel.InitializeAsync();

            Assert.Equal("Beta", secondViewModel.SelectedParentCategory?.Name);
        }
        finally
        {
            if (File.Exists(statePath))
            {
                File.Delete(statePath);
            }
        }
    }

    private static string CreateStatePath()
    {
        return Path.Combine(Path.GetTempPath(), $"selected-job-{Guid.NewGuid():N}.txt");
    }
}
