using Avalonia.Controls;
using Avalonia.Threading;
using System;
using Nolvus.Dashboard.Services;

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
            await Task.Yield();

            StatusText.Text = "Initializing wine prefix...";
            await Task.Delay(150); // Allows UI to update visibly

            await WinePrefix.InitializeAsync((message, percent) =>
            {
                StatusText.Text = message;
                LoadingBar.Width = (percent / 100.0) * 380; // or use ActualWidth
            });

            StatusText.Text = "Launching dashboard...";
            LoadingBar.Value = 100;

            await Task.Delay(200);

            var dash = new DashboardWindow();
            dash.Show();

            Close();
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
                this.Opacity -= step;

                if (this.Opacity <= 0.85)
                    _fadeOut = false;
            }
            else
            {
                this.Opacity += step;

                if (this.Opacity >= 1.00)
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
