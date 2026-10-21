using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Time_Tracker.Dialogs;
using Time_Tracker.ViewModels;

namespace Time_Tracker.Views;

public partial class MainWindow : Window
{
    private MainViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        Opened += MainWindow_Opened;
        ActivitiesDataGrid.AddHandler(InputElement.DoubleTappedEvent, ActivitiesDataGrid_DoubleTapped, RoutingStrategies.Bubble, true);
    }

    public MainWindow(MainViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
        AttachViewModel(viewModel);
    }

    private async void AddParentCategory_Click(object? sender, RoutedEventArgs e)
    {
        await ExecuteAddParentCategoryAsync();
    }

    private async void RenameParentCategory_Click(object? sender, RoutedEventArgs e)
    {
        await ExecuteRenameParentCategoryAsync();
    }

    private async void DeleteParentCategory_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        if (viewModel.SelectedParentCategory is null)
        {
            viewModel.ShowStatus("Select a parent/category first.");
            return;
        }

        if (!viewModel.CanDeleteSelectedParentCategoryNow())
        {
            viewModel.ShowStatus("At least one parent/category must remain.");
            return;
        }

        var parentName = viewModel.SelectedParentCategory.Name;
        var confirmed = await ConfirmParentDeleteAsync(parentName);
        if (!confirmed)
        {
            viewModel.ShowStatus("Parent/category delete canceled.");
            return;
        }

        await viewModel.DeleteParentCategoryCommand.ExecuteAsync(null);
    }

    private async Task ExecuteAddParentCategoryAsync()
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var parentName = await ShowParentCategoryNameDialogAsync(
            title: "New Parent/Category",
            prompt: "Enter a name for the new parent/category.",
            confirmText: "Create");

        if (parentName is null)
        {
            return;
        }

        viewModel.NewParentCategoryName = parentName;
        await viewModel.AddParentCategoryCommand.ExecuteAsync(null);
    }

    private async Task ExecuteRenameParentCategoryAsync()
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        if (viewModel.SelectedParentCategory is null)
        {
            viewModel.ShowStatus("Select a parent/category first.");
            return;
        }

        var parentName = await ShowParentCategoryNameDialogAsync(
            title: "Rename Parent/Category",
            prompt: "Enter a new name for the selected parent/category.",
            confirmText: "Rename",
            initialValue: viewModel.SelectedParentCategory.Name);

        if (parentName is null)
        {
            return;
        }

        viewModel.NewParentCategoryName = parentName;
        if (string.IsNullOrWhiteSpace(parentName))
        {
            viewModel.ShowStatus("Enter a parent/category name.");
            return;
        }

        if (string.Equals(parentName, viewModel.SelectedParentCategory.Name, StringComparison.Ordinal))
        {
            return;
        }

        await viewModel.RenameParentCategoryCommand.ExecuteAsync(null);
    }

    private async void NewActivityName_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await ExecuteCreateActivityAsync();
            e.Handled = true;
        }
    }

    private async void CreateActivity_Click(object? sender, RoutedEventArgs e)
    {
        await ExecuteCreateActivityAsync();
    }

    private async void DeleteActivity_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var selectedActivities = ActivitiesDataGrid.SelectedItems?
            .OfType<ActivityItemViewModel>()
            .DistinctBy(activity => activity.Id)
            .ToList() ?? new List<ActivityItemViewModel>();

        if (selectedActivities.Count == 0 && viewModel.SelectedActivity is not null)
        {
            selectedActivities.Add(viewModel.SelectedActivity);
        }

        if (selectedActivities.Count == 0)
        {
            viewModel.ShowStatus("Select an activity first.");
            return;
        }

        if (selectedActivities.Count > 1)
        {
            var confirmed = await ConfirmActivityDeleteAsync(selectedActivities.Count);
            if (!confirmed)
            {
                viewModel.ShowStatus("Activity delete canceled.");
                return;
            }
        }

        await viewModel.DeleteActivitiesAsync(selectedActivities);
    }

    private async Task ExecuteCreateActivityAsync()
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        viewModel.NewActivityName = NewActivityNameTextBox.Text ?? string.Empty;
        await viewModel.CreateActivityCommand.ExecuteAsync(null);
        NewActivityNameTextBox.Focus();
    }

    private async void History_Click(object? sender, RoutedEventArgs e)
    {
        await ExecuteEditSelectedActivityAsync();
    }

    private async void ActivitiesDataGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is not Control sourceControl || sourceControl.DataContext is not ActivityItemViewModel)
        {
            return;
        }

        await ExecuteEditSelectedActivityAsync();
    }

    private async Task ExecuteEditSelectedActivityAsync()
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        if (viewModel.SelectedActivity is null)
        {
            viewModel.ShowStatus("Select an activity first.");
            return;
        }

        var model = viewModel.CreateEditModelForSelectedEntry();
        if (model is null)
        {
            viewModel.ShowStatus("No time entry available to edit for this activity.");
            return;
        }

        var dialog = new EditSessionWindow
        {
            DataContext = new EditSessionDialogViewModel(model),
        };

        var saved = await dialog.ShowDialog<bool>(this);
        if (saved && dialog.Result is not null)
        {
            await viewModel.ApplyEditAsync(dialog.Result);
        }
    }

    private async void ExportCsv_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var exportThisJobOnly = await ShowExportScopeDialogAsync();
        if (exportThisJobOnly is null)
        {
            return;
        }

        long? parentCategoryId = null;
        if (exportThisJobOnly.Value)
        {
            if (viewModel.SelectedParentCategory is null)
            {
                viewModel.ShowStatus("Select a job first.");
                return;
            }

            parentCategoryId = viewModel.SelectedParentCategory.Id;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export time entries to CSV",
            SuggestedFileName = "time-tracker-export.csv",
            DefaultExtension = "csv",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new("CSV files")
                {
                    Patterns = new[] { "*.csv" },
                },
            },
        });

        if (file is null || file.Path.LocalPath is null)
        {
            return;
        }

        await viewModel.ExportCsvAsync(file.Path.LocalPath, parentCategoryId);
    }

    private async void About_Click(object? sender, RoutedEventArgs e)
    {
        await new AboutWindow().ShowDialog(this);
    }

    private void AttachViewModel(MainViewModel viewModel)
    {
        if (ReferenceEquals(_viewModel, viewModel))
        {
            return;
        }

        if (_viewModel is not null)
        {
            _viewModel.Activities.CollectionChanged -= Activities_CollectionChanged;
        }

        _viewModel = viewModel;
        _viewModel.Activities.CollectionChanged += Activities_CollectionChanged;
    }

    private void MainWindow_Opened(object? sender, EventArgs e)
    {
        ScrollActivitiesToBottom();
    }

    private void Activities_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ScrollActivitiesToBottom();
    }

    private void ScrollActivitiesToBottom()
    {
        if (_viewModel is null || _viewModel.Activities.Count == 0)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (_viewModel.Activities.Count == 0)
            {
                return;
            }

            ActivitiesDataGrid.ScrollIntoView(_viewModel.Activities[^1], null);
        }, DispatcherPriority.Background);
    }

    private async Task<string?> ShowParentCategoryNameDialogAsync(string title, string prompt, string confirmText, string initialValue = "")
    {
        var dialog = new ParentCategoryNameWindow(title, prompt, confirmText, initialValue)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        var confirmed = await dialog.ShowDialog<bool>(this);
        return confirmed ? dialog.EnteredName : null;
    }

    private async Task<bool?> ShowExportScopeDialogAsync()
    {
        var dialog = new ExportScopeWindow
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        var confirmed = await dialog.ShowDialog<bool>(this);
        return confirmed ? dialog.ExportThisJobOnly : null;
    }

    private async Task<bool> ConfirmActivityDeleteAsync(int activityCount)
    {
        var dialog = new ConfirmDeleteWindow(
            title: "Delete selected activities?",
            heading: "Delete selected activities?",
            message: "This will permanently delete all selected activities and their time entries.",
            targetText: $"Selected activities: {activityCount}",
            confirmText: "Yes, delete")
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        return await dialog.ShowDialog<bool>(this);
    }

    private async Task<bool> ConfirmParentDeleteAsync(string parentName)
    {
        var dialog = new ConfirmDeleteWindow(parentName)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        return await dialog.ShowDialog<bool>(this);
    }
}
