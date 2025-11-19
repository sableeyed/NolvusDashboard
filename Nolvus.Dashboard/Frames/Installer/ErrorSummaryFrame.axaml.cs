using Nolvus.Dashboard.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Frames;
using Nolvus.Core.Services;
using Nolvus.Core.Misc;
using Nolvus.Core.Enums;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Nolvus.Dashboard.Controls;

namespace Nolvus.Dashboard.Frames.Installer
{
    public partial class ErrorSummaryFrame : DashboardFrame
    {
        public ErrorSummaryFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
        }

        protected override async Task OnLoadedAsync()
        {
            ServiceSingleton.Dashboard.ClearInfo();

            ServiceSingleton.Dashboard.Title("Nolvus Dashboard - [Installation Failed]");
            ServiceSingleton.Dashboard.Status("Installation is not completed, please review errors.");
            ServiceSingleton.Dashboard.Info(string.Format("Error(s) : {0}", ServiceSingleton.Packages.ErrorHandler.ErrorsCount));

            if (ServiceSingleton.Packages.ErrorHandler.ThresholdEnabled)
            {
                LblMessage.Text = string.Format("The installation has not been completed because {0} error(s) on {1} maximum error(s) allowed occured", ServiceSingleton.Packages.ErrorHandler.ErrorsCount, ServiceSingleton.Settings.ErrorsThreshold);
                ServiceSingleton.Dashboard.AdditionalInfo(string.Format("Error threshold : {0}", ServiceSingleton.Settings.ErrorsThreshold));
            }
            else
            {
                LblMessage.Text = string.Format("The installation has not been completed because {0} error(s) occured", ServiceSingleton.Packages.ErrorHandler.ErrorsCount);
                ServiceSingleton.Dashboard.AdditionalInfo("Error threshold : Disabled");
            }

            LoadErrors();

        }

        private void BtnRetry_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Dashboard.ClearInfo();            
            ServiceSingleton.Dashboard.LoadFrame<ResumeFrame>();
        }

        private void BtnFix_Click(object? sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.nolvus.net/appendix/installer/faq");
        }

        private void LoadErrors()
        {
            ErrorsList.Children.Clear();

            foreach (var faulty in ServiceSingleton.Packages.ErrorHandler.List)
            {
                var panel = new ErrorPanel
                {
                    ModName = faulty.Mod.Name,
                    ErrorText = faulty.Error.Message
                };

                try
                {
                    if (!string.IsNullOrWhiteSpace(faulty.Mod.ImagePath))
                    {
                        var img = ServiceSingleton.Lib.GetImageFromWebStream(faulty.Mod.ImagePath);
                        panel.SetImage(img);
                    }
                    else
                    {
                        var fallback = ServiceSingleton.Lib.GetImageFromWebStream("default");
                        panel.SetImage(fallback);
                    }
                }
                catch { }
                ErrorsList.Children.Add(panel);
            }
        }
    }
}