using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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

    private async void NewParentCategory_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await ExecuteAddParentCategoryAsync();
            e.Handled = true;
        }
    }

    private async void AddParentCategory_Click(object? sender, RoutedEventArgs e)
    {
        await ExecuteAddParentCategoryAsync();
    }

    private async Task ExecuteAddParentCategoryAsync()
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        viewModel.NewParentCategoryName = NewParentCategoryTextBox.Text ?? string.Empty;
        await viewModel.AddParentCategoryCommand.ExecuteAsync(null);
        NewParentCategoryTextBox.Focus();
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
}
