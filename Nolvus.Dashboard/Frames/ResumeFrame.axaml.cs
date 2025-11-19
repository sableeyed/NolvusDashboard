using Avalonia.Controls;
using Avalonia.Interactivity;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Core.Misc;
using Nolvus.NexusApi;
using Nolvus.Dashboard.Controls;
using Nolvus.Core.Enums;
using Nolvus.Dashboard.Core;

namespace Nolvus.Dashboard.Frames
{
    public partial class ResumeFrame : DashboardFrame
    {
        public ResumeFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
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

        protected override async Task OnLoadedAsync()
        {
            ServiceSingleton.Dashboard.Title("Nolvus Dashboard - [Instance Auto Installer]");
            ServiceSingleton.Dashboard.Status(string.Empty);

            DrpDwnLstInstances.ItemsSource = ServiceSingleton.Instances.InstancesToResume;
            //DrpDwnLstInstances.DisplayMember = "Name";
            DrpDwnLstInstances.SelectedIndex = 0;

            if (!ApiManager.AccountInfo.IsPremium)
            {
                LblDownLoc.IsVisible = false;
                DrpDwnLstDownLoc.IsVisible = false;
            }

            if (ServiceSingleton.Settings.ErrorsThreshold == 1 || !NexusApi.ApiManager.AccountInfo.IsPremium)
            {
                RdBtnOneError.IsChecked = true;
                RdBtnThreshold.Content = string.Format("Stop the installation when {0} errors occured and display the error messages (max errors can be set up in the Nolvus Dashboard.ini file)", 50);
            }
            else if (ServiceSingleton.Settings.ErrorsThreshold == 0)
            {
                RdBtnNoThreshold.IsChecked = true;
                RdBtnThreshold.Content = string.Format("Stop the installation when {0} errors occured and display the error messages (max errors can be set up in the Nolvus Dashboard.ini file)", 50);
            }
            else
            {
                RdBtnThreshold.IsChecked = true;
                RdBtnThreshold.Content = string.Format("Stop the installation when {0} errors occured and display the error messages (max errors can be set up in the Nolvus Dashboard.ini file)", ServiceSingleton.Settings.ErrorsThreshold);
            }
        }

        private void DrpDwnLstInstances_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var Instance = DrpDwnLstInstances.SelectedItem as INolvusInstance;
            ServiceSingleton.Instances.WorkingInstance = Instance;

            if (ApiManager.AccountInfo.IsPremium)
            {
                DrpDwnLstDownLoc.ItemsSource = CDN.Get();
                DrpDwnLstDownLoc.SelectedIndex = DownloadLocationIndex(CDN.Get());
            }
        }


        private void DrpDwnLstDownLoc_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var Instance = DrpDwnLstInstances.SelectedItem as INolvusInstance;

            Instance.Settings.CDN = DrpDwnLstDownLoc.SelectedItem.ToString();

            ServiceSingleton.Instances.Save();   
        }

        private async void BtnCancel_Click(object? sender, RoutedEventArgs e)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            bool? result = await NolvusMessageBox.ShowConfirmation(owner, "Cancel Install?", "Are you sure you want to cancel installation? Your current installation will be deleted.");
            if (result == true)
            {
                // ServiceSingleton.Dashboard.LoadFrame<DeleteFrame>(
                //     new FrameParameters(
                //         new FrameParameter()
                //         {
                //             Key ="Instance", Value= DrpDwnLstInstances.SelectedItem as INolvusInstance
                //         }, 
                //         new FrameParameter()
                //         {
                //             Key ="Action", Value=InstanceAction.Cancel
                //         }
                //     ));   
            }
        }

        private void BtnResume_Click(object? sender, RoutedEventArgs e)
        {
            if (RdBtnOneError.IsChecked == true)
            {
                ServiceSingleton.Settings.StoreIniValue("Process", "ErrorsThreshold", "1");
            }
            else if (RdBtnNoThreshold.IsChecked == true)
            {
                ServiceSingleton.Settings.StoreIniValue("Process", "ErrorsThreshold", "0");
            }
            else
            {
                if (ServiceSingleton.Settings.ErrorsThreshold == 1 || ServiceSingleton.Settings.ErrorsThreshold == 0)
                {
                    ServiceSingleton.Settings.StoreIniValue("Process", "ErrorsThreshold", "50");
                }
            }

            ServiceSingleton.Dashboard.LoadFrameAsync<PackageFrame>(); 
        }
    }
}