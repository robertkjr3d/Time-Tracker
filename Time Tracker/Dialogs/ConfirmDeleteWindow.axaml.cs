using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Time_Tracker.Dialogs;

public partial class ConfirmDeleteWindow : Window
{
    public ConfirmDeleteWindow()
    {
        InitializeComponent();
    }

    public ConfirmDeleteWindow(string parentName)
        : this(
            title: "Delete this Job?",
            heading: "Delete this Job?",
            message: "This will permanently delete the selected job/category and every activity and time entry under it.",
            targetText: $"Selected job/category: {parentName}")
    {
    }

    public ConfirmDeleteWindow(string title, string heading, string message, string targetText, string confirmText = "Delete")
        : this()
    {
        Title = title;
        this.FindControl<TextBlock>("HeadingTextBlock")!.Text = heading;
        this.FindControl<TextBlock>("MessageTextBlock")!.Text = message;
        this.FindControl<TextBlock>("TargetNameTextBlock")!.Text = targetText;
        this.FindControl<Button>("ConfirmButton")!.Content = confirmText;
    }

    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
