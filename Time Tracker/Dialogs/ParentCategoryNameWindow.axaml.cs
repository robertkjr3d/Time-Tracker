using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Time_Tracker.Dialogs;

public partial class ParentCategoryNameWindow : Window
{
    public ParentCategoryNameWindow()
    {
        InitializeComponent();
        Opened += (_, _) =>
        {
            NameTextBox.Focus();
            NameTextBox.SelectAll();
        };
    }

    public ParentCategoryNameWindow(string title, string prompt, string confirmText, string initialValue = "")
        : this()
    {
        Title = title;
        PromptTextBlock.Text = prompt;
        ConfirmButton.Content = confirmText;
        NameTextBox.Text = initialValue;
    }

    public string EnteredName => NameTextBox.Text?.Trim() ?? string.Empty;

    private void Confirm_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void NameTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            Close(true);
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close(false);
        }
    }
}
