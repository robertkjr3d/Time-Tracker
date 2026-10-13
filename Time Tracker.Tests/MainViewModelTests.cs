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
        var viewModel = new MainViewModel(repository);

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
        var viewModel = new MainViewModel(repository)
        {
            NewParentCategoryName = "Client A",
        };

        await viewModel.InitializeAsync();
        await viewModel.AddParentCategoryCommand.ExecuteAsync(null);

        Assert.Contains(viewModel.ParentCategories, x => x.Name == "Client A");
        Assert.Equal("Client A", viewModel.SelectedParentCategory?.Name);
        Assert.True(viewModel.IsStatusVisible);
        Assert.Equal("Parent 'Client A' is ready.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task CreateActivityCommand_CreatesRunningActivityUnderSelectedParent()
    {
        var repository = new FakeTimeTrackerRepository();
        var parentId = await repository.CreateParentCategoryAsync("Client A");
        var viewModel = new MainViewModel(repository)
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
}
