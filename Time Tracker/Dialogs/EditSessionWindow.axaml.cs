using Avalonia.Controls;
using Avalonia.Interactivity;
using Time_Tracker.Models;
using Time_Tracker.ViewModels;

namespace Time_Tracker.Dialogs;

public partial class EditSessionWindow : Window
{
    public EditSessionWindow()
    {
        InitializeComponent();
    }

    public EditSessionModel? Result { get; private set; }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is EditSessionDialogViewModel viewModel && viewModel.TryBuildResult(out var result))
        {
            Result = result;
            Close(true);
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
