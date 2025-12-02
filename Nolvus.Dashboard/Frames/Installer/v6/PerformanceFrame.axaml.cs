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
using Nolvus.Dashboard.Controls;
using System.Diagnostics;

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
            TglBtnDownScale.IsCheckedChanged += OnDownscaleChanged;
            TglBtnSREX.IsCheckedChanged += OnSREXChanged;
            DrpDwnLstVariant.SelectionChanged += OnVariantChanged;
            DrpDwnLstAntiAliasing.SelectionChanged += OnAntiAliasingChanged;
            DrpDwnLstScreenRes.SelectionChanged += OnResolutionChanged;
            DrpDwnLstDownscalingScreenRes.SelectionChanged += OnDownscaledResolutionChanged;
            DrpDwnLstRatios.SelectionChanged += OnRatioChanged;
            DrpDwnLstIni.SelectionChanged += OnIniChanged;
            DrpDwnLstLODs.SelectionChanged += OnLODsChanged;
            BtnSupportedGPUs.Click += VerifyGPU;
            BtnVariantPreview.Click += VariantPreview;
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
            if (ServiceSingleton.Instances.WorkingInstance.Performance.AntiAliasing != "DLAA" && ServiceSingleton.Instances.WorkingInstance.Performance.AntiAliasing != "FSR")
            {
                if (TglBtnDownScale.IsChecked == true)
                {
                    DrpDwnLstDownscalingScreenRes.IsEnabled = TglBtnDownScale.IsChecked == true;
                    ServiceSingleton.Instances.WorkingInstance.Performance.DownScaling = "TRUE";
                    DrpDwnLstDownscalingScreenRes.SelectedIndex = DownscalingResolutionIndex(ServiceSingleton.Globals.WindowsResolutions);
                }
                else
                {
                    DrpDwnLstDownscalingScreenRes.IsEnabled = TglBtnDownScale.IsChecked == true;
                    ServiceSingleton.Instances.WorkingInstance.Performance.DownScaling = "FALSE";
                    DrpDwnLstAntiAliasing.IsEnabled = true;
                }
            }
            else
            {
                DrpDwnLstAntiAliasing.SelectedIndex = 0;
                DrpDwnLstAntiAliasing.IsEnabled = false;
                DrpDwnLstDownscalingScreenRes.IsEnabled = true;
                ServiceSingleton.Instances.WorkingInstance.Performance.DownScaling = "TRUE";
                DrpDwnLstDownscalingScreenRes.SelectedIndex = DownscalingResolutionIndex(ServiceSingleton.Globals.WindowsResolutions);
            }

            UpdateHardwareConfiguration();
        }

        private async void OnSREXChanged(object? sender, RoutedEventArgs e)
        {
            //maybe use a guard
            if (TglBtnSREX.IsChecked == true)
            {
                var owner = TopLevel.GetTopLevel(this) as Window;
                bool? result = await NolvusMessageBox.ShowConfirmation(owner, "Warning", "The SR Exterior Cities option is very performance heavy. Don't try to run it at 4k without and tools like Framegen or Lossless Scaling. Are you sure?");
                if (result == false)
                {
                    TglBtnSREX.IsChecked = false;
                    ServiceSingleton.Instances.WorkingInstance.Performance.SREX = "FALSE";
                }
                else
                {
                    ServiceSingleton.Instances.WorkingInstance.Performance.SREX = "TRUE";
                }
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.Performance.SREX = "FALSE";
            }

            UpdateHardwareConfiguration();
        }

        private async void OnVariantChanged(object? sender, SelectionChangedEventArgs e)
        {
            var Variant = (INolvusVariantDTO)DrpDwnLstVariant.SelectedItem!;
            ServiceSingleton.Instances.WorkingInstance.Performance.Variant = Variant.Name;

            LblTextures.Text = Variant.Textures;
            LblTrees.Text = Variant.Trees;
            LblCities.Text = Variant.Cities;

            if (ServiceSingleton.Instances.WorkingInstance.Performance.Variant == "Ultra")
            {
                DrpDwnLstLODs.IsEnabled = true;
            }
            else
            {
                DrpDwnLstLODs.SelectedIndex = 0;
                DrpDwnLstLODs.IsEnabled = false;
            }

            MinRequirements = await ApiManager.Service.Installer.GetNolvusVariantMinimumRequirements(Variant.Id.ToString()!);
            RecRequirements = await ApiManager.Service.Installer.GetNolvusVariantRecommendedRequirements(Variant.Id.ToString()!);

            UpdateHardwareConfiguration();
        }

        private void ForceTAA(string Message)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            NolvusMessageBox.Show(owner, "Anti Aliasing", Message, MessageBoxType.Error);
            DrpDwnLstAntiAliasing.SelectionChanged -= OnAntiAliasingChanged;
            DrpDwnLstAntiAliasing.SelectedIndex = 0;
            ServiceSingleton.Instances.WorkingInstance.Performance.AntiAliasing = "TAA";
            DrpDwnLstAntiAliasing.SelectionChanged += OnAntiAliasingChanged;
        }

        private void OnAntiAliasingChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DrpDwnLstAntiAliasing.SelectedValue != null)
            {
                ServiceSingleton.Instances.WorkingInstance.Performance.AntiAliasing = DrpDwnLstAntiAliasing.SelectedValue.ToString();

                if (DrpDwnLstAntiAliasing.SelectedValue.ToString() == "DLAA")
                {
                    if (!IsNvidiaRTX() && !ServiceSingleton.Settings.ForceAA)
                    {
                        ForceTAA("DLAA is only compatible with NVIDIA graphics cards!");
                    }
                    else if (ServiceSingleton.Instances.WorkingInstance.Performance.DownScaling == "TRUE")
                    {
                        ForceTAA("DLAA is not compatible with downscaling");
                    }
                }
                else if (DrpDwnLstAntiAliasing.SelectedValue.ToString() == "FSR")
                {
                    if (ServiceSingleton.Instances.WorkingInstance.Performance.DownScaling == "TRUE")
                    {
                        ForceTAA("FSR is not compatible with downscaling");
                    }
                }
            }
        }

        private void OnResolutionChanged(object? sender, SelectionChangedEventArgs e)
        {
            INolvusInstance WorkingInstance = ServiceSingleton.Instances.WorkingInstance;

            string Resolution = DrpDwnLstScreenRes.SelectedValue.ToString();

            string[] Reso = Resolution.Split(new char[] { 'x' });

            WorkingInstance.Settings.Width = Reso[0];
            WorkingInstance.Settings.Height = Reso[1];

            UpdateHardwareConfiguration();
        }

        private void OnDownscaledResolutionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DrpDwnLstDownscalingScreenRes.SelectedValue != null)
            {
                string Resolution = DrpDwnLstDownscalingScreenRes.SelectedValue.ToString();

                string[] Reso = Resolution.Split(new char[] {'x'});

                ServiceSingleton.Instances.WorkingInstance.Performance.DownWidth = Reso[0];
                ServiceSingleton.Instances.WorkingInstance.Performance.DownHeight = Reso[1];

                UpdateHardwareConfiguration();
            }
        }

        private void OnRatioChanged(object? sender, SelectionChangedEventArgs e)
        {
            ServiceSingleton.Instances.WorkingInstance.Settings.Ratio = DrpDwnLstRatios.SelectedValue!.ToString()!;
        }

        private void OnIniChanged(object? sender, SelectionChangedEventArgs e)
        {
            ServiceSingleton.Instances.WorkingInstance.Performance.IniSettings = DrpDwnLstIni.SelectedIndex!.ToString()!;
        }

        private void OnLODsChanged(object? sender, SelectionChangedEventArgs e)
        {
            ServiceSingleton.Instances.WorkingInstance.Performance.LODs = DrpDwnLstLODs.SelectedValue!.ToString()!;

            UpdateHardwareConfiguration();
        }

        private void UpdateHardwareConfiguration()
        {
            INolvusInstance WorkingInstance = ServiceSingleton.Instances.WorkingInstance;

            LblSelRes.Text = WorkingInstance.GetSelectedResolution();

            if (MinRequirements != null)
            {
                INolvusVariantRequirementDTO MinRequirement = MinRequirements.Where(x => x.Height.ToString() == WorkingInstance.GetSelectedHeight() &&
                                                                                         x.Width.ToString() == WorkingInstance.GetSelectedWidth() &&
                                                                                         x.SREX.ToString().ToUpper() == WorkingInstance.Performance.SREX &&
                                                                                         x.Lods == WorkingInstance.Performance.LODs).FirstOrDefault()!;                

                if (MinRequirement != null)
                {
                    LblMinCPU.Text = MinRequirement.CPU;
                    LblMinGPU.Text = string.Format("{0} {1}", MinRequirement.GPUVendor, MinRequirement.GPUName);
                    LblMinVRAM.Text = string.Format("{0} GB", MinRequirement.VRAM.ToString());
                    LblMinInstallSize.Text = string.Format("{0} GB", MinRequirement.InstallationSize.ToString());
                    LblMinDownloadSize.Text = string.Format("{0} GB (Optional)", MinRequirement.DownloadSize.ToString());
                }
                else
                {

                    MinRequirement = MinRequirements.Where(x => x.SREX.ToString().ToUpper() == WorkingInstance.Performance.SREX && x.Lods == WorkingInstance.Performance.LODs).OrderBy(x => Math.Abs(System.Convert.ToInt32(WorkingInstance.GetSelectedWidth()) - x.Width)).FirstOrDefault()!;

                    if ( MinRequirement != null)
                    {
                        LblMinCPU.Text = MinRequirement.CPU;
                        LblMinGPU.Text = string.Format("{0} {1}", MinRequirement.GPUVendor, MinRequirement.GPUName);
                        LblMinVRAM.Text = string.Format("{0} GB", MinRequirement.VRAM.ToString());
                        LblMinInstallSize.Text = string.Format("{0} GB", MinRequirement.InstallationSize.ToString());
                        LblMinDownloadSize.Text = string.Format("{0} GB (Optional)", MinRequirement.DownloadSize.ToString());
                    }
                    else
                    {
                        LblMinCPU.Text = "NA";
                        LblMinGPU.Text = "NA";
                        LblMinVRAM.Text = "NA";
                        LblMinInstallSize.Text = "NA";
                        LblMinDownloadSize.Text = "NA";
                    }                    
                }
            }

            if (RecRequirements != null)
            {
                INolvusVariantRequirementDTO RecRequirement = RecRequirements.Where(x => x.Height.ToString() == WorkingInstance.GetSelectedHeight() &&
                                                                                         x.Width.ToString() == WorkingInstance.GetSelectedWidth() &&
                                                                                         x.SREX.ToString().ToUpper() == WorkingInstance.Performance.SREX &&
                                                                                         x.Lods == WorkingInstance.Performance.LODs).FirstOrDefault()!;

                if (RecRequirement != null)
                {
                    LblMaxCPU.Text = RecRequirement.CPU;
                    LblMaxGPU.Text = string.Format("{0} {1}", RecRequirement.GPUVendor, RecRequirement.GPUName);
                    LblMaxVRAM.Text = string.Format("{0} GB", RecRequirement.VRAM.ToString());
                    LblMinInstallSize.Text = string.Format("{0} GB", RecRequirement.InstallationSize.ToString());
                    LblMinDownloadSize.Text = string.Format("{0} GB (Optional)", RecRequirement.DownloadSize.ToString());
                }
                else
                {
                    RecRequirement = RecRequirements.Where(x => x.SREX.ToString().ToUpper() == WorkingInstance.Performance.SREX && x.Lods == WorkingInstance.Performance.LODs).OrderBy(x => Math.Abs(System.Convert.ToInt32(WorkingInstance.GetSelectedWidth()) - x.Width)).FirstOrDefault()!;

                    if (RecRequirement != null)
                    {
                        LblMaxCPU.Text = RecRequirement.CPU;
                        LblMaxGPU.Text = string.Format("{0} {1}", RecRequirement.GPUVendor, RecRequirement.GPUName);
                        LblMaxVRAM.Text = string.Format("{0} GB", RecRequirement.VRAM.ToString());
                        LblMinInstallSize.Text = string.Format("{0} GB", RecRequirement.InstallationSize.ToString());
                        LblMinDownloadSize.Text = string.Format("{0} GB (Optional)", RecRequirement.DownloadSize.ToString());
                    }
                    else
                    {
                        LblMaxCPU.Text = "NA";
                        LblMaxGPU.Text = "NA";
                        LblMaxVRAM.Text = "NA";
                        LblMinInstallSize.Text = "NA";
                        LblMinDownloadSize.Text = "NA";
                    }                    
                }
            }
        }

        private void VerifyGPU(object? sender, RoutedEventArgs e)
        {        
            // INolvusInstance WorkingInstance = ServiceSingleton.Instances.WorkingInstance;

            // ServiceSingleton.Dashboard.LoadFrameAsync<v6.GPUFrame>(
            //     new FrameParameters(new FrameParameter()
            //     {
            //         Key = "VariantRequirement",
            //         Value = MinRequirements.Where(x => x.SREX.ToString().ToUpper() == WorkingInstance.Performance.SREX &&
            //                                       x.Lods == WorkingInstance.Performance.LODs
            //                                      ).OrderBy(x => Math.Abs(System.Convert.ToInt32(WorkingInstance.GetSelectedWidth()) - x.Width)).FirstOrDefault()
            //     })
            // );
        }

        private void VariantPreview(object? sender, RoutedEventArgs e)
        {
            var url = "https://www.youtube.com/watch?v=y-Xis9XuETk";
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