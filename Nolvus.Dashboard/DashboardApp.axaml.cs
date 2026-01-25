using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Nolvus.Dashboard;

public partial class DashboardApp : Application
{
    public override void Initialize() =>
        AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new DashboardWindow();

        base.OnFrameworkInitializationCompleted();
    }
}
