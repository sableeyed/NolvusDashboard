using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Vcc.Nolvus.Api.Installer.Services;
using Vcc.Nolvus.Api.Installer.Library;
using Nolvus.Core.Enums;
using Nolvus.Dashboard.Frames.Installer;
using Avalonia.Interactivity;
using System.Diagnostics;

namespace Nolvus.Dashboard.Frames.Instance
{
    public partial class InstancesFrame : DashboardFrame
    {
        public InstancesFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
            BtnNewInstance.Click += BtnNewInstance_Click;
            BtnDiscord.Click += BtnDiscord_Click;
            BtnPatreon.Click += BtnPatreon_Click;
            BtnDonate.Click += BtnDonate_Click;
        }

        public void LockButtons()
        {
            BtnDiscord.IsEnabled = false;
            BtnDonate.IsEnabled = false;            
            BtnNewInstance.IsEnabled = false;
            BtnPatreon.IsEnabled = false;            
        }

        public void UnLockButtons()
        {
            BtnDiscord.IsEnabled = true;
            BtnDonate.IsEnabled = true;            
            BtnNewInstance.IsEnabled = true;
            BtnPatreon.IsEnabled = true;
        }

        protected override void OnLoad()
        {            
            ServiceSingleton.Dashboard.Title("Nolvus Dashboard");
            ServiceSingleton.Dashboard.Info("Manage your Nolvus instances");

            InstancesPanel.ContainerFrame = this;
            InstancesPanel.LoadInstances(ServiceSingleton.Instances.InstanceList);
        }

        private async void BtnNewInstance_Click(object? sender, RoutedEventArgs e)
        {
            await ServiceSingleton.Dashboard.LoadFrameAsync<SelectInstanceFrame>(new FrameParameters(new FrameParameter() { Key = "Cancel", Value = true }));
        }

        private void BtnDiscord_Click(object? sender, RoutedEventArgs e)
        {
            var url = "https://discord.gg/Zkh5PwD";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void BtnDonate_Click(object? sender, RoutedEventArgs e)
        {
            var url = "https://www.paypal.com/paypalme/nolvus";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void BtnPatreon_Click(object? sender, RoutedEventArgs e)
        {
            var url = "https://www.patreon.com/nolvus";
            try
            {
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