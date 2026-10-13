using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Time_Tracker.Dialogs;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
