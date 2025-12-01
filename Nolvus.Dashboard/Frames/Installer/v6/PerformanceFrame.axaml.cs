using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Enums;
using Nolvus.Core.Misc;
using Nolvus.Core.Services;
using Nolvus.Instance.Core;
using Nolvus.Dashboard.Core;
using Vcc.Nolvus.Api.Installer.Services;
using Vcc.Nolvus.Api.Installer.Library;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using System.Linq.Expressions;

namespace Nolvus.Dashboard.Frames.Installer.v6
{
    public partial class PerformanceFrame : DashboardFrame
    {
        private List<string> AntiAliasing = new List<string>();
        private List<string> LODs = new List<string>();
        private List<string> IniSettings = new List<string>();

        private IEnumerable<INolvusVariantRequirementDTO> MinRequirements;
        private IEnumerable<INolvusVariantRequirementDTO> RecRequirements;
        public PerformanceFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
            BtnPrevious.Click += BtnPrevious_Click;
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

        private int DownscalingResolutionIndex(List<string> Resolutions)
        {
            INolvusInstance WorkingInstance = ServiceSingleton.Instances.WorkingInstance;
            var Index = Resolutions.FindIndex(x => x == WorkingInstance.Performance.DownWidth + "x" + WorkingInstance.Performance.DownHeight);
            return Index == -1 ? 0 : Index;
        }

        private int VariantIndex(IEnumerable<INolvusVariantDTO> Variants)
        {
            INolvusInstance WorkingInstance = ServiceSingleton.Instances.WorkingInstance;
            var Index = Variants.ToList().FindIndex(x => x.Name == WorkingInstance.Performance.Variant);
            return Index == -1 ? 0 : Index;
        }

        private int AntiAliasingIndex(List<string> AntiAliasing)
        {
            var Index = AntiAliasing.FindIndex(x => x == ServiceSingleton.Instances.WorkingInstance.Performance.AntiAliasing);
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
                Ratios.Add("32:9");

                DrpDwnLstRatios.ItemsSource = Ratios;

                DrpDwnLstRatios.SelectedIndex = RatioIndex(Ratios);

                Instance.Settings.Height = top.Screens.Primary.WorkingArea.Height.ToString();
                Instance.Settings.Width = top.Screens.Primary.WorkingArea.Width.ToString();

                DrpDwnLstScreenRes.ItemsSource = ServiceSingleton.Globals.WindowsResolutions;

                DrpDwnLstScreenRes.SelectedIndex = ResolutionIndex(ServiceSingleton.Globals.WindowsResolutions);

                DrpDwnLstDownscalingScreenRes.IsEnabled = false;

                DrpDwnLstDownscalingScreenRes.ItemsSource = ServiceSingleton.Globals.WindowsResolutions;

                DrpDwnLstDownscalingScreenRes.SelectedIndex = DownscalingResolutionIndex(ServiceSingleton.Globals.WindowsResolutions);

                TglBtnDownScale.IsChecked = false;

                if (Instance.Performance.DownScaling == "TRUE")
                {
                    TglBtnDownScale.IsChecked = true;
                }

                #endregion

                #region Graphics

                AntiAliasing.Add("TAA");
                AntiAliasing.Add("DLAA");
                AntiAliasing.Add("FSR");

                LODs.Add("Ultra");
                LODs.Add("Performance");
                LODs.Add("Ultra Performance");

                IniSettings.Add("Low");
                IniSettings.Add("Medium");
                IniSettings.Add("High");

                DrpDwnLstAntiAliasing.ItemsSource = AntiAliasing;
                DrpDwnLstAntiAliasing.SelectedIndex = AntiAliasingIndex(AntiAliasing);

                DrpDwnLstIni.ItemsSource = IniSettings;
                DrpDwnLstIni.SelectedIndex = System.Convert.ToInt16(Instance.Performance.IniSettings);

                DrpDwnLstLODs.ItemsSource = LODs;
                DrpDwnLstLODs.SelectedIndex = LODsIndex(LODs);

                LblCPU.Text = await ServiceSingleton.Globals.GetCPUInfo();

                var Ram = await ServiceSingleton.Globals.GetRamCount();

                if (Ram != "Ram count not found")
                {
                    LblRAM.Text = Ram + " GB";
                }
                else
                {
                    LblRAM.Text = Ram;
                }

                var GPU = string.Join(Environment.NewLine, ServiceSingleton.Globals.GetVideoAdapters().ToArray());

                LblGPUs.Text = GPU;

                if (ServiceSingleton.Settings.ForceAA)
                {
                    LblGPUs.Text = GPU + " (CHECK BYPASSED)";
                }

                #endregion

                #region Variants

                var Variants = await ApiManager.Service.Installer.GetNolvusVariants();

                DrpDwnLstVariant.ItemsSource = Variants;
                DrpDwnLstVariant.SelectedIndex = VariantIndex(Variants);

                //TglBtnSREX.ToggleStateChanging -= TglBtnSREX_ToggleStateChanging;

                //TglBtnSREX.ToggleState = ToggleButtonState.Inactive;

                if (Instance.Performance.SREX == "TRUE")
                {
                    TglBtnSREX.IsChecked = true;
                }

                //TglBtnSREX.ToggleStateChanging += TglBtnSREX_ToggleStateChanging;

                #endregion

                ServiceSingleton.Dashboard.Info("Graphics and Performance Settings");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void BtnPrevious_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Dashboard.LoadFrame<PathFrame>();
        }

        private void BtnContinue_Click(object? sender, RoutedEventArgs e)
        {
            
        }

        private void OnDownscaleChanged(object? sender, RoutedEventArgs e)
        {
            
        }

        private void OnSREXChanged(object? sender, RoutedEventArgs e)
        {
            
        }

        private async void OnVariantChanged(object? sender, SelectionChangedEventArgs e)
        {
            
        }



    }
}