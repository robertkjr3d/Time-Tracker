using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Threading.Tasks;
using Time_Tracker.ViewModels;

namespace Time_Tracker.Dialogs;

public partial class ActivityHistoryWindow : Window
{
    public ActivityHistoryWindow()
    {
        InitializeComponent();
    }

    private async void EditSelected_Click(object? sender, RoutedEventArgs e)
    {
        await EditSelectedEntryAsync();
    }

    private async void TimeEntries_DoubleTapped(object? sender, TappedEventArgs e)
    {
        await EditSelectedEntryAsync();
    }

    private async Task EditSelectedEntryAsync()
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var model = viewModel.CreateEditModelForSelectedEntry();
        if (model is null)
        {
            viewModel.ShowStatus("Select a time entry to edit.");
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

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
