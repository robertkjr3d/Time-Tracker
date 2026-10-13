using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Time_Tracker.Dialogs;

public partial class ConfirmDeleteWindow : Window
{
    public ConfirmDeleteWindow()
    {
        InitializeComponent();
        Opened += (_, _) => ConfirmationTextBox.Focus();
    }

    public ConfirmDeleteWindow(string parentName)
        : this()
    {
        TargetNameTextBlock.Text = $"Selected parent/category: {parentName}";
    }

    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
        var confirmation = ConfirmationTextBox.Text?.Trim();
        if (string.Equals(confirmation, "DELETE", System.StringComparison.Ordinal))
        {
            Close(true);
            return;
        }

        ConfirmationTextBox.Text = string.Empty;
        TargetNameTextBlock.Text = "Type DELETE exactly to confirm this parent/category deletion.";
        ConfirmationTextBox.Focus();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
