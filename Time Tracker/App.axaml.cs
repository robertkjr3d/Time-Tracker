using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Time_Tracker.Services;
using Time_Tracker.ViewModels;
using Time_Tracker.Views;

namespace Time_Tracker;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var repository = new SqliteTimeTrackerRepository();
            var mainViewModel = new MainViewModel(repository);

            desktop.MainWindow = new MainWindow(mainViewModel);

            _ = mainViewModel.InitializeAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }
}