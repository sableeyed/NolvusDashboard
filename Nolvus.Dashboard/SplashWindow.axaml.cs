using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Platform;
using Avalonia.Media.Imaging;
using System;
using Nolvus.Dashboard.Services;
using Nolvus.Dashboard.Services.Wine;
using Nolvus.Core.Utils;

namespace Nolvus.Dashboard;

public partial class SplashWindow : Window
{
    private DispatcherTimer _pulseTimer;
    private bool _fadeOut = true;
    public SplashWindow()
    {
        InitializeComponent();
        StartWindowPulse();
        Opened += SplashWindow_Opened;
    }

    private async void SplashWindow_Opened(object? sender, EventArgs e)
    {
        SetWindowIcon();
        await Task.Yield();

        var winePath = PathResolver.FindExecutable("wine");
        if (winePath == null)
        {
            StatusText.Text = "Select your wine binary";

            var dialog = new OpenFileDialog
            {
                Title = "Select Wine executable",
                AllowMultiple = false
            };

            var result = await dialog.ShowAsync(this);

            if (result == null || result.Length == 0)
            {
                StatusText.Text = "Wine selection cancelled. Dashboard cannot continue.";
                await Task.Delay(1500);
                Close();
                return;
            }

            winePath = result[0];
        }

        WineRunner.WinePath = winePath;

        StatusText.Text = "Initializing wine prefix...";
        await Task.Delay(150);

        await WinePrefix.InitializeAsync((message, percent) =>
        {
            StatusText.Text = message;
            LoadingBar.Width = (percent / 100.0) * 380;
        });

        StatusText.Text = "Launching dashboard...";
        LoadingBar.Value = 100;

        await Task.Delay(200);

        var dash = new DashboardWindow();
        dash.Show();
        Close();
    }

    private void SetWindowIcon()
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://NolvusDashboard/Assets/nolvus-ico.jpg"));
            Icon = new WindowIcon(new Bitmap(stream));
        }
        catch { }
    }


    private void StartWindowPulse()
    {
        _pulseTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(60)
        };

        _pulseTimer.Tick += (_, __) =>
        {
            double step = 0.01;

            if (_fadeOut)
            {
                Opacity -= step;

                if (Opacity <= 0.85)
                    _fadeOut = false;
            }
            else
            {
                Opacity += step;

                if (Opacity >= 1.00)
                    _fadeOut = true;
            }
        };

        _pulseTimer.Start();
    }

    public void UpdateStatus(string text)
    {
        Dispatcher.UIThread.Post(() =>
        {
                StatusText.Text = text;
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        _pulseTimer?.Stop();
        base.OnClosed(e);
    }

    public void SetProgress(double percent)
    {
        Dispatcher.UIThread.Post(() =>
        {
            percent = Math.Clamp(percent, 0, 100);
            double totalWidth = this.Bounds.Width - 40; // Margin accounted
            double barWidth = totalWidth * (percent / 100.0);

            LoadingBar.Width = barWidth;
        });
    }

    public void SetStatus(string text)
    {
        Dispatcher.UIThread.Post(() =>
        {
            StatusText.Text = text;
        });
    }
}