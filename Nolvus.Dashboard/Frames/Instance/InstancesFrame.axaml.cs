using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Vcc.Nolvus.Api.Installer.Services;
using Vcc.Nolvus.Api.Installer.Library;
using Nolvus.Core.Enums;
using Nolvus.Dashboard.Frames.Installer;

namespace Nolvus.Dashboard.Frames.Instance
{
    public partial class InstancesFrame : DashboardFrame
    {
        public InstancesFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
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

        private async void BtnNewInstance_Click(object sender, EventArgs e)
        {
            await ServiceSingleton.Dashboard.LoadFrameAsync<SelectInstanceFrame>(new FrameParameters(new FrameParameter() { Key = "Cancel", Value = true }));
        }

        private void BtnDiscord_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://discord.gg/Zkh5PwD");
        }

        private void BtnDonate_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.paypal.com/paypalme/nolvus");
        }

        private void BtnPatreon_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.patreon.com/nolvus");
        }   
    }
}