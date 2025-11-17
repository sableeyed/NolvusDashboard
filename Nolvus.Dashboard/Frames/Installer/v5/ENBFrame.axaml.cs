using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Avalonia.Controls;
using System.Diagnostics;
using Avalonia.Input;
using Avalonia.Interactivity;
using Nolvus.Core.Misc;
using Nolvus.Dashboard.Frames.Installer;

namespace Nolvus.Dashboard.Frames.Installer.v5
{
    public partial class ENBFrame : DashboardFrame
    {
        public ENBFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();

            BtnPrevious.Click += BtnPrevious_Click;
            BtnContinue.Click += BtnContinue_Click;
        }

        protected override async Task OnLoadedAsync()
        {
            ServiceSingleton.Dashboard.Info("ENB Selection");
            
            DrpEnbPreset.ItemsSource = ENBs.GetAvailableENBsForV5();
            DrpEnbPreset.SelectedIndex = 0;
        }

        private void BtnPrevious_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Dashboard.LoadFrame<v5.OptionsFrame>();
        }

        private void BtnContinue_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Dashboard.LoadFrame<PageFileFrame>();
        }

        private void OnEnbChanged(object? sender, SelectionChangedEventArgs e)
        {
            ServiceSingleton.Instances.WorkingInstance.Options.AlternateENB = DrpEnbPreset.SelectedItem.ToString();
        }

        private void LnkCabbageTutorial_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            const string url = "https://www.nolvus.net/guide/asc/enb";

            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to open tutorial link: " + ex.Message);
            }
        }
    }
}