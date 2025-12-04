using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Core.Misc;
using Nolvus.Core.Enums;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Nolvus.Dashboard.Frames.Installer
{
    public partial class CDNFrame : DashboardFrame
    {
        public CDNFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();

            BtnPrevious.Click += BtnPrevious_Click;
            BtnContinue.Click += BtnContinue_Click;
        }

        protected override async Task OnLoadedAsync()
        {
            DrpDwnLstDownLoc.ItemsSource = CDN.Get();
            DrpDwnLstDownLoc.SelectedIndex = DownloadLocationIndex(CDN.Get());
            ServiceSingleton.Dashboard.Info("CDN Location");

        }

        private void OnCDNLocationChanged(object? sender, SelectionChangedEventArgs e)
        {
            ServiceSingleton.Instances.WorkingInstance.Settings.CDN = DrpDwnLstDownLoc.SelectedItem.ToString();
        }

        private int DownloadLocationIndex(List<string> Locations)
        {
            if (ServiceSingleton.Instances.WorkingInstance.Settings.CDN != string.Empty)
            {
                var Index = Locations.FindIndex(x => x == ServiceSingleton.Instances.WorkingInstance.Settings.CDN);

                return Index == -1 ? 0 : Index;
            }

            return 0;
        }

        private void BtnPrevious_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Dashboard.LoadFrame<PageFileFrame>();
        }

        private void BtnContinue_Click(object? sender, RoutedEventArgs e)
        {
            switch (ServiceSingleton.Instances.WorkingInstance.Name)
            {
                case Strings.NolvusAscension:
                    ServiceSingleton.Dashboard.LoadFrame<v5.SummaryFrame>();
                    break;
                case Strings.NolvusAwakening:
                    ServiceSingleton.Dashboard.LoadFrame<v6.SummaryFrame>();
                    break;
            }
        }
    }
}