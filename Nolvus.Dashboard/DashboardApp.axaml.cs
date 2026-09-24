using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Nolvus.Core.Services;

namespace Nolvus.Dashboard;

public partial class DashboardApp : Application
{
    public override void Initialize() =>
        AvaloniaXamlLoader.Load(this);

    // The PDF report sets its text in the Open Sans bundled with Nolvus.Components (see
    // ReportService). QuestPDF only discovers fonts on disk, so the bundled files are handed to it
    // here - the first point at which Avalonia can open its assets, and well before any report.
    private static void RegisterReportFonts()
    {
        foreach (var File in new[] { "OpenSans-Regular.ttf", "OpenSans-SemiBold.ttf", "OpenSans-Bold.ttf" })
        {
            try
            {
                using var Stream = AssetLoader.Open(new Uri($"avares://Nolvus.Components/Assets/Fonts/OpenSans/{File}"));
                QuestPDF.Drawing.FontManager.RegisterFont(Stream);
            }
            catch (Exception ex)
            {
                // The report still generates, in whatever font QuestPDF falls back to.
                ServiceSingleton.Logger?.Log($"[REPORT] Could not register {File} for the PDF report : {ex.Message}");
            }
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        RegisterReportFonts();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new DashboardWindow();

        base.OnFrameworkInitializationCompleted();
    }
}
