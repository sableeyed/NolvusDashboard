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

                TglBtnDownScale.IsChecked = false;

                if (Instance.Performance.DownScaling == "TRUE")
                {
                    TglBtnDownScale.IsChecked = true;
                }

                #endregion

                DrpDwnLstScreenRes.ItemsSource = ServiceSingleton.Globals.WindowsResolutions;
                DrpDwnLstScreenRes.SelectedIndex = DownResolutionIndex(ServiceSingleton.Globals.WindowsResolutions);
                DrpDwnLstScreenRes.IsEnabled = false;

                TglBtnPhysics.IsChecked = false;

                if (Instance.Performance.AdvancedPhysics == "TRUE")
                {
                    TglBtnPhysics.IsChecked = true;
                }

                TglBtnRayTracing.IsChecked = false;
                
                if (Instance.Performance.RayTracing == "TRUE")
                {
                    TglBtnRayTracing.IsChecked = true;
                }

                TglBtnDownScale.IsChecked = false;

                if (Instance.Performance.DownScaling == "TRUE")
                {
                    TglBtnDownScale.IsChecked = true;
                }

                TglBtnFPSStabilizer.IsChecked = false;

                if (Instance.Performance.FPSStabilizer == "TRUE")
                {
                    TglBtnFPSStabilizer.IsChecked = true;
                }

                DrpDwnLstAntiAliasing.ItemsSource = AntiAliasing;

                LblCPU.Text = await ServiceSingleton.Globals.GetCPUInfo();

                var Ram = await ServiceSingleton.Globals.GetRamCount();

                if (Ram != "RAM count not found")
                {
                    LblRAM.Text = Ram + " GB";
                }
                else
                {
                    LblRAM.Text = Ram;
                }

                var GPU = string.Join(Environment.NewLine, ServiceSingleton.Globals.GetVideoAdapters().ToArray());

                if (!ServiceSingleton.Settings.ForceAA)
                {
                    LblGPU.Text = GPU;

                    if (!IsNvidiaRTX())
                    {
                        ServiceSingleton.Instances.WorkingInstance.Performance.AntiAliasing = "TAA";
                        DrpDwnLstAntiAliasing.IsEnabled = false;
                    }
                }
                else
                {
                    LblGPU.Text = GPU + " (CHECK BYPASSED)";
                }

                DrpDwnLstAntiAliasing.SelectedIndex = AntiAliasingIndex(AntiAliasing);

                DrpDwnLstIni.ItemsSource = IniSettings;

                DrpDwnLstIni.SelectedIndex = System.Convert.ToInt16(Instance.Performance.IniSettings);

                DrpDwnLstVariant.ItemsSource = Variants;

                DrpDwnLstVariant.SelectedIndex = VariantIndex(Variants);

                DrpDwnLstLODs.ItemsSource = LODs;

                DrpDwnLstLODs.SelectedIndex = LODsIndex(LODs);

                ServiceSingleton.Dashboard.Info("Performance Settings");
            }
            catch (Exception ex)
            {
                await ServiceSingleton.Dashboard.Error("Error during performance options loading", ex.Message, ex.StackTrace);
            }
        }

        private int RatioIndex(List<string> Ratios)
        {
            var Index = Ratios.FindIndex(x => x == ServiceSingleton.Instances.WorkingInstance.Settings.Ratio);

            return Index == -1 ? 0 : Index;
        }

        private int ResolutionIndex(List<string> Resolutions)
        {
            INolvusInstance WorkingInstance = ServiceSingleton.Instances.WorkingInstance;

            var Index = Resolutions.FindIndex(x => x == WorkingInstance.Settings.Width + "x" + WorkingInstance.Settings.Height);

            return Index == -1 ? 0 : Index;
        }
        
        private int DownResolutionIndex(List<string> Resolutions)
        {
            INolvusInstance WorkingInstance = ServiceSingleton.Instances.WorkingInstance;

            var Index = Resolutions.FindIndex(x => x == WorkingInstance.Performance.DownWidth + "x" + WorkingInstance.Performance.DownHeight);

            return Index == -1 ? 0 : Index;   
        }

        private int AntiAliasingIndex(List<string> AntiAliasing)
        {
            var Index = AntiAliasing.FindIndex(x => x == ServiceSingleton.Instances.WorkingInstance.Performance.AntiAliasing);
            return Index == -1 ? 0 : Index;     
        }

        private int VariantIndex(List<string> Variants)
        {
            var Index = Variants.FindIndex(x => x == ServiceSingleton.Instances.WorkingInstance.Performance.Variant);
            return Index == -1 ? 0 : Index;      
        }

        private int LODsIndex(List<string> LODs)
        {
            var Index = LODs.FindIndex(x => x == ServiceSingleton.Instances.WorkingInstance.Performance.LODs);
            return Index == -1 ? 0 : Index;
        }

        private bool IsNvidiaRTX()
        {
            return ServiceSingleton.Globals.GetVideoAdapters().Where(x => x.Contains("NVIDIA") && x.Contains("RTX")).FirstOrDefault() != null;
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