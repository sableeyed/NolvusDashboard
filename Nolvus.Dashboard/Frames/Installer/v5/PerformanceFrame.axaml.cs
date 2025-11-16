using Nolvus.Core.Interfaces;
using Nolvus.Core.Frames;
using Nolvus.Core.Enums;
using Nolvus.Core.Services;
using Nolvus.Instance.Core;
using Nolvus.Dashboard.Core;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Controls;

namespace Nolvus.Dashboard.Frames.Installer.v5
{
    public partial class PerformanceFrame : DashboardFrame
    {
        private List<string> Variants = new List<string>();
        private List<string> AntiAliasing = new List<string>();
        private List<string> LODs = new List<string>();
        private List<string> IniSettings = new List<string>();
        public PerformanceFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();

            Variants.Add("Ultra");
            Variants.Add("Redux");

            AntiAliasing.Add("TAA");
            AntiAliasing.Add("DLAA");

            LODs.Add("Ultra");
            LODs.Add("Performance");
            LODs.Add("Ultra Performance");

            IniSettings.Add("Low");
            IniSettings.Add("Medium");
            IniSettings.Add("High");

            BtnPrevious.Click += BtnPrevious_Click;
            BtnContinue.Click += BtnContinue_Click;
        }

        protected override async Task OnLoadedAsync()
        {
            var top = TopLevel.GetTopLevel(this);
            try
            {
                var Instance = ServiceSingleton.Instances.WorkingInstance;

                #region Resolution

                List<string> Ratios = new List<string>();

                Ratios.Add("16:9");
                Ratios.Add("21:9");

                DrpDwnLstRatios.ItemsSource = Ratios;

                DrpDwnLstRatios.SelectedIndex = RatioIndex(Ratios);

                Instance.Settings.Width = top.Screens.Primary.WorkingArea.Width.ToString();
                Instance.Settings.Height = top.Screens.Primary.WorkingArea.Height.ToString();

                DrpDwnLstResolution.ItemsSource = ServiceSingleton.Globals.WindowsResolutions;

                DrpDwnLstResolution.SelectedIndex = ResolutionIndex(ServiceSingleton.Globals.WindowsResolutions);

                

                #endregion
            }
            catch (Exception ex)
            {
                await ServiceSingleton.Dashboard.Error("Error during performance options loading", ex.Message, ex.StackTrace);
            }
        }

        private int RatioIndex(List<string> Ratios)
        {
            return 0;
        }

        private int ResolutionIndex(List<string> Resolutions)
        {
            return 0;
        }
        
        private int DownResolutionIndex(List<string> Resolutions)
        {
            return 0;
        }

        private int AntiAliasingIndex(List<string> AntiAliasing)
        {
            return 0;
        }

        private int VariantIndex(List<string> Variants)
        {
            return 0;
        }

        private int LODsIndex(List<string> LODs)
        {
            return 0;
        }

        private bool IsNvidiaRTX()
        {
            return false;
        }

        private void BtnPrevious_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Dashboard.LoadFrame<PathFrame>();
        }

        private void BtnContinue_Click(object? sender, RoutedEventArgs e)
        {
            return;
        }

    }
}