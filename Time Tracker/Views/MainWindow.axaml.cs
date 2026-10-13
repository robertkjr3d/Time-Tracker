using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Time_Tracker.Dialogs;
using Time_Tracker.ViewModels;

namespace Time_Tracker.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
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

        if (viewModel.SelectedActivity is null)
        {
            viewModel.ShowStatus("Select an activity first.");
            return;
        }

        await viewModel.DeleteActivityCommand.ExecuteAsync(null);
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
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        if (viewModel.SelectedActivity is null)
        {
            viewModel.ShowStatus("Select an activity first.");
            return;
        }

        var window = new ActivityHistoryWindow
        {
            DataContext = viewModel,
            Title = $"History - {viewModel.SelectedActivity.Name}",
        };

        await window.ShowDialog(this);
    }

    private async void ExportCsv_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
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

        await viewModel.ExportCsvAsync(file.Path.LocalPath);
    }

    private async void About_Click(object? sender, RoutedEventArgs e)
    {
        await new AboutWindow().ShowDialog(this);
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

    private async Task<bool> ConfirmParentDeleteAsync(string parentName)
    {
        var dialog = new ConfirmDeleteWindow(parentName)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        return await dialog.ShowDialog<bool>(this);
    }
}
