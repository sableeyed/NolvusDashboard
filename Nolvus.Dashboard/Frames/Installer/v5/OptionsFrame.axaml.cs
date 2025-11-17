using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Enums;
using Nolvus.Core.Services;
using Nolvus.Instance.Core;
using Nolvus.Dashboard.Core;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Controls;
using Nolvus.Dashboard.Controls;

namespace Nolvus.Dashboard.Frames.Installer.v5
{
    public partial class OptionsFrame : DashboardFrame
    {
        public OptionsFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();

            BtnContinue.Click += BtnContinue_Click;
            BtnPrevious.Click += BtnPrevious_Click;
        }

        private void BtnContinue_Click(object? sender, RoutedEventArgs e)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            NolvusMessageBox.Show(owner, "Error", "Unimplemented - do not report as a bug", MessageBoxType.Error);
            return;
        }

        private async void BtnPrevious_Click(object? sender, RoutedEventArgs e)
        {
            await ServiceSingleton.Dashboard.LoadFrameAsync<v5.PerformanceFrame>();
        }
    }
}