using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Time_Tracker.Dialogs;

public partial class ExportScopeWindow : Window
{
    public ExportScopeWindow()
    {
        InitializeComponent();
    }

    public bool? ExportThisJobOnly { get; private set; }

    private void AllJobs_Click(object? sender, RoutedEventArgs e)
    {
        ExportThisJobOnly = false;
        Close(true);
    }

    private void ThisJob_Click(object? sender, RoutedEventArgs e)
    {
        ExportThisJobOnly = true;
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        ExportThisJobOnly = null;
        Close(false);
    }
}
