using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using System.Diagnostics;
using Nolvus.Core.Services;
using Nolvus.Core.Misc;
using Nolvus.Dashboard.Controls;

namespace Nolvus.Dashboard.Frames.Installer
{
    public partial class PageFileFrame : DashboardFrame
    {
        public PageFileFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();

            BtnPrevious.Click += BtnPrevious_Click;
            BtnContinue.Click += BtnContinue_Click;
        }

        protected override async Task OnLoadedAsync()
        {
            ServiceSingleton.Dashboard.Info("Swap size configuration");
        }

        private void BtnPrevious_Click(object? sender, RoutedEventArgs e)
        {
            switch (ServiceSingleton.Instances.WorkingInstance.Name)
            {
                case Strings.NolvusAscension:
                    ServiceSingleton.Dashboard.LoadFrame<v5.ENBFrame>();
                    break;
                case Strings.NolvusAwakening:
                    Console.WriteLine("v6 not implemented");
                    break;
            }
        }

        private async void BtnContinue_Click(object? sender, RoutedEventArgs e)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            bool? response = await NolvusMessageBox.ShowConfirmation(owner, "Confirmation", "Swap configuration is REALLY IMPORTANT to avoid crashes, be sure you set it up correctly. Do you want to continue?");
            if(response == true)
            {
                if (NexusApi.ApiManager.AccountInfo.IsPremium)
                {
                    ServiceSingleton.Dashboard.LoadFrame<CDNFrame>();
                }
                else
                {
                    switch (ServiceSingleton.Instances.WorkingInstance.Name)
                    {
                        case Strings.NolvusAscension:
                            ServiceSingleton.Dashboard.LoadFrame<v5.SummaryFrame>();
                            break;
                        case Strings.NolvusAwakening:
                            //ServiceSingleton.Dashboard.LoadFrame<v6.SummaryFrame<>();
                            break;
                    }
                }
            }
        }

        private void LnkPageFile_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            try
            {
                var url = "https://wiki.archlinux.org/title/Swap";
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }
}