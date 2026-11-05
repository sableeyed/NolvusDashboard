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
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using System.Linq.Expressions;
using Nolvus.Dashboard.Controls;
using System.Diagnostics;

namespace Nolvus.Dashboard.Frames.Installer.v6
{
    public partial class PerformanceFrame : DashboardFrame
    {
        private List<string> IniSettings = new List<string>();

        private IEnumerable<INolvusVariantRequirementDTO> MinRequirements;
        private IEnumerable<INolvusVariantRequirementDTO> RecRequirements;
        public PerformanceFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
            BtnPrevious.Click += BtnPrevious_Click;
            BtnContinue.Click += BtnContinue_Click;
            TglBtnDownScale.IsCheckedChanged += OnDownscaleChanged;
            TglBtnSREX.IsCheckedChanged += OnSREXChanged;
            TglBtnFrameGen.IsCheckedChanged += OnFrameGenerationChanged;
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

        private int RatioIndex(IEnumerable<INolvusOptionChoiceDTO> Ratios)
        {
            var Index = Ratios.ToList().FindIndex(x => x.Value == ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("Ratio"));
            return Index == -1 ? 0 : Index;
        }

        private int ResolutionIndex(List<string> Resolutions)
        {
            INolvusInstance WorkingInstance = ServiceSingleton.Instances.WorkingInstance;
            var Index = Resolutions.FindIndex(x => x == WorkingInstance.SettingsManager.Width + "x" + WorkingInstance.SettingsManager.Height);
            return Index == -1 ? 0 : Index;
        }

        private int DownscalingResolutionIndex(List<string> Resolutions)
        {
            INolvusInstance WorkingInstance = ServiceSingleton.Instances.WorkingInstance;
            var Index = Resolutions.FindIndex(x => x == WorkingInstance.SettingsManager.DownWidth + "x" + WorkingInstance.SettingsManager.DownHeight);
            return Index == -1 ? 0 : Index;
        }

        private int VariantIndex(IEnumerable<INolvusVariantDTO> Variants)
        {
            INolvusInstance WorkingInstance = ServiceSingleton.Instances.WorkingInstance;
            var Index = Variants.ToList().FindIndex(x => x.Name == WorkingInstance.SettingsManager.Variant);
            return Index == -1 ? 0 : Index;
        }

        private int AntiAliasingIndex(IEnumerable<INolvusOptionChoiceDTO> AntiAliasing)
        {
            var Index = AntiAliasing.ToList().FindIndex(x => x.Value == ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("AntiAliasing"));
            return Index == -1 ? 0 : Index;
        }

        private int LODsIndex(IEnumerable<INolvusOptionChoiceDTO> LODs)
        {
            var Index = LODs.ToList().FindIndex(x => x.Value == ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("LODs"));
            return Index == -1 ? 0 : Index;
        }

        private async Task LoadLODs()
        {
            var Instance = ServiceSingleton.Instances.WorkingInstance;

            var LODs = await ApiManager.Service.Installer.GetOptionChoices("LODs", Instance.SettingsManager.Variant, Instance.Id);

            DrpDwnLstLODs.ItemsSource = null;
            DrpDwnLstLODs.ItemsSource = LODs;

            DrpDwnLstLODs.SelectedIndex = LODsIndex(LODs);
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

                var Ratios = await ApiManager.Service.Installer.GetOptionChoices("Ratio", Instance.SettingsManager.Variant, Instance.Id);

                DrpDwnLstRatios.ItemsSource = Ratios;

                DrpDwnLstRatios.SelectedIndex = RatioIndex(Ratios);
                var primary = top?.Screens?.Primary;
                if (primary == null)
                {
                    ServiceSingleton.Logger.Log("No primary screen detected... Falling back to 1920x1080");
                    Instance.SettingsManager.Height = "1080";
                    Instance.SettingsManager.Width = "1920";
                }
                else
                {
                    Instance.SettingsManager.Height = primary.WorkingArea.Height.ToString();
                    Instance.SettingsManager.Width = primary.WorkingArea.Width.ToString();
                }

                DrpDwnLstScreenRes.ItemsSource = ServiceSingleton.Globals.WindowsResolutions;

                DrpDwnLstScreenRes.SelectedIndex = ResolutionIndex(ServiceSingleton.Globals.WindowsResolutions);

                DrpDwnLstDownscalingScreenRes.IsEnabled = false;

                DrpDwnLstDownscalingScreenRes.ItemsSource = ServiceSingleton.Globals.GetDownscaleResolutions();

                DrpDwnLstDownscalingScreenRes.SelectedIndex = DownscalingResolutionIndex(ServiceSingleton.Globals.GetDownscaleResolutions());

                TglBtnDownScale.IsChecked = false;

                if (Instance.SettingsManager.DownScaling == "TRUE")
                {
                    TglBtnDownScale.IsChecked = true;
                }

                #endregion

                #region Graphics

                IniSettings.Add("Low");
                IniSettings.Add("Medium");
                IniSettings.Add("High");

                DrpDwnLstIni.ItemsSource = IniSettings;
                DrpDwnLstIni.SelectedIndex = System.Convert.ToInt16(Instance.SettingsManager.IniSettings);

                var AAs = await ApiManager.Service.Installer.GetOptionChoices("Antialiasing", Instance.SettingsManager.Variant, Instance.Id);

                DrpDwnLstAntiAliasing.ItemsSource = AAs;

                DrpDwnLstAntiAliasing.SelectedIndex = AntiAliasingIndex(AAs);

                await LoadLODs();

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

                LblGPUs.Text = GPU;

                if (ServiceSingleton.Settings.ForceAA)
                {
                    LblGPUs.Text = GPU + " (CHECK BYPASSED)";
                }

                #endregion

                #region Variants

                var Variants = await ApiManager.Service.Installer.GetNolvusVariants(Instance.Id);

                DrpDwnLstVariant.ItemsSource = Variants;
                DrpDwnLstVariant.SelectedIndex = VariantIndex(Variants);

                // Detached like upstream's ToggleStateChanging, or restoring a saved value would ask
                // the "are you sure" question that only applies when the user turns it on.
                TglBtnSREX.IsCheckedChanged -= OnSREXChanged;

                TglBtnSREX.IsChecked = false;

                if (Instance.OptionsManager.GetOptionValue("SREX") == "TRUE")
                {
                    TglBtnSREX.IsChecked = true;
                }

                TglBtnSREX.IsCheckedChanged += OnSREXChanged;

                TglBtnFrameGen.IsChecked = false;

                if (Instance.OptionsManager.GetOptionValue("FrameGeneration") == "TRUE")
                {
                    TglBtnFrameGen.IsChecked = true;
                }

                #endregion

                ServiceSingleton.Dashboard.Info("Graphics and Performance Settings");
            }
            catch (Exception ex)
            {
                await ServiceSingleton.Dashboard.Error("Error during performance options loading", ex.Message, ex.StackTrace);
            }
        }

        private void BtnPrevious_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Dashboard.LoadFrame<PathFrame>();
        }

        private async void BtnContinue_Click(object? sender, RoutedEventArgs e)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            var Instance = ServiceSingleton.Instances.WorkingInstance;
            var SettingsManager = Instance.SettingsManager;

            if (SettingsManager.DownScaling == "TRUE" && ((SettingsManager.Height == SettingsManager.DownHeight || System.Convert.ToInt32(SettingsManager.DownHeight) > System.Convert.ToInt32(SettingsManager.Height)) && (SettingsManager.Width == SettingsManager.DownWidth || System.Convert.ToInt32(SettingsManager.DownWidth) > System.Convert.ToInt32(SettingsManager.Width))))
            {
                NolvusMessageBox.Show(owner, "Invalid Downscaling setting", "If downscaling is enabled, the downscaled resolution must be less than the monitor resolution!", MessageBoxType.Error);
            }
            else
            {
                bool? result = await NolvusMessageBox.ShowConfirmation(owner, "Confirmation", "Remember, running the list without the right hardware requirement for the variant you choose can make the game unstable. The variant can not be changed after installation. Are you sure you want to continue?");
                if (result == true)
                {                    
                    await ServiceSingleton.Dashboard.LoadFrameAsync<v6.OptionsFrame>();
                }                
            }
        }

        private void OnDownscaleChanged(object? sender, RoutedEventArgs e)
        {
            if (ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("AntiAliasing") != "DLAA" && ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("AntiAliasing") != "FSR" && ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("FrameGeneration") != "TRUE")
            {
                if (TglBtnDownScale.IsChecked == true)
                {
                    DrpDwnLstDownscalingScreenRes.IsEnabled = TglBtnDownScale.IsChecked == true;
                    ServiceSingleton.Instances.WorkingInstance.SettingsManager.DownScaling = "TRUE";
                    DrpDwnLstDownscalingScreenRes.SelectedIndex = DownscalingResolutionIndex(ServiceSingleton.Globals.GetDownscaleResolutions());
                }
                else
                {
                    DrpDwnLstDownscalingScreenRes.IsEnabled = TglBtnDownScale.IsChecked == true;
                    ServiceSingleton.Instances.WorkingInstance.SettingsManager.DownScaling = "FALSE";
                    DrpDwnLstAntiAliasing.IsEnabled = true;
                    TglBtnFrameGen.IsEnabled = true;
                }
            }
            else
            {
                DrpDwnLstAntiAliasing.SelectedIndex = 0;
                DrpDwnLstAntiAliasing.IsEnabled = false;
                TglBtnFrameGen.IsChecked = false;
                TglBtnFrameGen.IsEnabled = false;
                NolvusMessageBox.Show(TopLevel.GetTopLevel(this) as Window, "Antialiasing", "This option is not compatible with DLAA/FSR/Frame Generation. Antialiasing has been forced to TAA and Frame Generation has been disabled.", MessageBoxType.Info);
                DrpDwnLstDownscalingScreenRes.IsEnabled = true;
                ServiceSingleton.Instances.WorkingInstance.SettingsManager.DownScaling = "TRUE";
                DrpDwnLstDownscalingScreenRes.SelectedIndex = DownscalingResolutionIndex(ServiceSingleton.Globals.GetDownscaleResolutions());
            }

            UpdateHardwareConfiguration();
        }

        private async void OnSREXChanged(object? sender, RoutedEventArgs e)
        {
            //maybe use a guard
            if (TglBtnSREX.IsChecked == true)
            {
                var owner = TopLevel.GetTopLevel(this) as Window;
                bool? result = await NolvusMessageBox.ShowConfirmation(owner, "Warning", "The SR Exterior Cities option is very performance heavy. Don't try to run it at 4K without any tools like Framegen or Lossless Scaling. Are you sure?");
                if (result == false)
                {
                    TglBtnSREX.IsChecked = false;
                    ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("SREX").Value = "FALSE";
                }
                else
                {
                    ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("SREX").Value = "TRUE";
                }
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("SREX").Value = "FALSE";
            }

            UpdateHardwareConfiguration();
        }

        private async void OnFrameGenerationChanged(object? sender, RoutedEventArgs e)
        {
            if (TglBtnFrameGen.IsChecked == true)
            {
                var owner = TopLevel.GetTopLevel(this) as Window;
                await NolvusMessageBox.Show(owner, "Frame Generation", "Warning, the ENB FPS counter will still show the normal FPS, to see the interpolated FPS, please use the NVIDIA or AMD integrated overlay.\n\nFramegen will not work out of the box if your monitor doesn't handle 120hz minimum.\n\nTo bypass that in game, open the ENB ui (shift+enter), on the left panel go under performance and check Force Framegen. Hit apply settings then save settings and restart the game.", MessageBoxType.Info);
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("FrameGeneration").Value = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("FrameGeneration").Value = "FALSE";
            }
        }

        private async void OnVariantChanged(object? sender, SelectionChangedEventArgs e)
        {
            var Instance = ServiceSingleton.Instances.WorkingInstance;

            var Variant = (INolvusVariantDTO)DrpDwnLstVariant.SelectedItem!;
            Instance.SettingsManager.Variant = Variant.Name;

            await Instance.OptionsManager.SynchronizeOptions(Instance);

            await LoadLODs();

            LblTextures.Text = Variant.Textures;
            LblTrees.Text = Variant.Trees;
            LblCities.Text = Variant.Cities;

            if (ServiceSingleton.Instances.WorkingInstance.SettingsManager.Variant == "Ultra")
            {
                LblLods.Foreground = Brushes.White;
                DrpDwnLstLODs.IsEnabled = true;
            }
            else if (ServiceSingleton.Instances.WorkingInstance.SettingsManager.Variant == "Ultimate" || ServiceSingleton.Instances.WorkingInstance.SettingsManager.Variant == Strings.GO)
            {
                LblLods.Foreground = Brushes.Gray;
                DrpDwnLstLODs.SelectedIndex = 0;
                DrpDwnLstLODs.IsEnabled = false;
            }
            else
            {
                LblLods.Foreground = Brushes.Gray;
                DrpDwnLstLODs.SelectedIndex = 0;
                DrpDwnLstLODs.IsEnabled = false;
            }

            MinRequirements = await ApiManager.Service.Installer.GetNolvusVariantMinimumRequirements(Variant.Id.ToString()!);
            RecRequirements = await ApiManager.Service.Installer.GetNolvusVariantRecommendedRequirements(Variant.Id.ToString()!);

            LblNote.Text = Variant.Note;

            UpdateHardwareConfiguration();
        }

        private void ForceTAA(string Message)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            NolvusMessageBox.Show(owner, "Anti Aliasing", Message, MessageBoxType.Error);
            DrpDwnLstAntiAliasing.SelectionChanged -= OnAntiAliasingChanged;
            DrpDwnLstAntiAliasing.SelectedIndex = 0;
            ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("AntiAliasing").Value = "TAA";
            DrpDwnLstAntiAliasing.SelectionChanged += OnAntiAliasingChanged;
        }

        private void OnAntiAliasingChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DrpDwnLstAntiAliasing.SelectedValue != null)
            {
                var AAValue = (DrpDwnLstAntiAliasing.SelectedItem as INolvusOptionChoiceDTO).Value;

                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("AntiAliasing").Value = AAValue;

                if (AAValue == "DLAA")
                {
                    if (!IsNvidiaRTX() && !ServiceSingleton.Settings.ForceAA)
                    {
                        ForceTAA("DLAA is only compatible with NVIDIA graphics cards!");
                    }
                    else if (ServiceSingleton.Instances.WorkingInstance.SettingsManager.DownScaling == "TRUE")
                    {
                        ForceTAA("DLAA is not compatible with downscaling");
                    }
                }
                else if (AAValue == "FSR")
                {
                    if (ServiceSingleton.Instances.WorkingInstance.SettingsManager.DownScaling == "TRUE")
                    {
                        ForceTAA("FSR is not compatible with downscaling");
                    }
                }
            }
        }

        private void OnResolutionChanged(object? sender, SelectionChangedEventArgs e)
        {
            INolvusInstance WorkingInstance = ServiceSingleton.Instances.WorkingInstance;

            string Resolution = DrpDwnLstScreenRes.SelectedValue!.ToString()!;

            string[] Reso = Resolution.Split(['x']);

            WorkingInstance.SettingsManager.Width = Reso[0];
            WorkingInstance.SettingsManager.Height = Reso[1];

            if (
                    (WorkingInstance.SettingsManager.Width == "2560" && WorkingInstance.SettingsManager.Height == "1080") ||
                    (WorkingInstance.SettingsManager.Width == "3440" && WorkingInstance.SettingsManager.Height == "1440") ||
                    (WorkingInstance.SettingsManager.Width == "5120" && WorkingInstance.SettingsManager.Height == "2160")
                )
            {
                DrpDwnLstRatios.SelectedIndex = 1;
            }
            else if (
                        (WorkingInstance.SettingsManager.Width == "3840" && WorkingInstance.SettingsManager.Height == "1080") ||
                        (WorkingInstance.SettingsManager.Width == "5120" && WorkingInstance.SettingsManager.Height == "1440") ||
                        (WorkingInstance.SettingsManager.Width == "7680" && WorkingInstance.SettingsManager.Height == "2160")
                    )
            {
                DrpDwnLstRatios.SelectedIndex = 2;
            }


            UpdateHardwareConfiguration();
        }

        private void OnDownscaledResolutionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DrpDwnLstDownscalingScreenRes.SelectedValue != null)
            {
                string Resolution = DrpDwnLstDownscalingScreenRes.SelectedValue!.ToString()!;

                string[] Reso = Resolution.Split(['x']);

                ServiceSingleton.Instances.WorkingInstance.SettingsManager.DownWidth = Reso[0];
                ServiceSingleton.Instances.WorkingInstance.SettingsManager.DownHeight = Reso[1];

                UpdateHardwareConfiguration();
            }
        }

        private void OnRatioChanged(object? sender, SelectionChangedEventArgs e)
        {
            ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("Ratio").Value = (DrpDwnLstRatios.SelectedItem as INolvusOptionChoiceDTO).Value;
        }

        private void OnIniChanged(object? sender, SelectionChangedEventArgs e)
        {
            ServiceSingleton.Instances.WorkingInstance.SettingsManager.IniSettings = DrpDwnLstIni.SelectedIndex!.ToString()!;
        }

        private void OnLODsChanged(object? sender, SelectionChangedEventArgs e)
        {
            // LoadLODs swaps the list, and Avalonia raises this with nothing selected while it does.
            if (DrpDwnLstLODs.SelectedItem == null)
                return;

            ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("LODs").Value = (DrpDwnLstLODs.SelectedItem as INolvusOptionChoiceDTO).Value;

            UpdateHardwareConfiguration();
        }

        private void UpdateHardwareConfiguration()
        {
            INolvusInstance WorkingInstance = ServiceSingleton.Instances.WorkingInstance;

            LblSelRes.Text = WorkingInstance.SettingsManager.GetSelectedResolution();

            if (MinRequirements != null)
            {
                INolvusVariantRequirementDTO MinRequirement = MinRequirements.Where(x => x.Height.ToString() == WorkingInstance.SettingsManager.GetSelectedHeight() &&
                                                                                         x.Width.ToString() == WorkingInstance.SettingsManager.GetSelectedWidth() &&
                                                                                         x.SREX.ToString().ToUpper() == WorkingInstance.OptionsManager.GetOptionValue("SREX") &&
                                                                                         x.Lods == WorkingInstance.OptionsManager.GetOptionValue("LODs")).FirstOrDefault()!;                

                if (MinRequirement != null)
                {
                    LblMinCPU.Text = MinRequirement.CPU;
                    LblMinGPU.Text = string.Format("{0} {1}", MinRequirement.GPUVendor, MinRequirement.GPUName);
                    LblMinVRAM.Text = string.Format("{0} GB", MinRequirement.VRAM.ToString());
                    LblMinInstallSize.Text = string.Format("{0} GB", MinRequirement.InstallationSize.ToString());
                    LblMinDownloadSize.Text = string.Format("{0} GB (Optional, if you don't want to keep archives, disable them in the previous screen)", MinRequirement.DownloadSize.ToString());
                }
                else
                {

                    MinRequirement = MinRequirements.Where(x => x.SREX.ToString().ToUpper() == WorkingInstance.OptionsManager.GetOptionValue("SREX") && x.Lods == WorkingInstance.OptionsManager.GetOptionValue("LODs")).OrderBy(x => Math.Abs(System.Convert.ToInt32(WorkingInstance.SettingsManager.GetSelectedWidth()) - x.Width)).FirstOrDefault()!;

                    if ( MinRequirement != null)
                    {
                        LblMinCPU.Text = MinRequirement.CPU;
                        LblMinGPU.Text = string.Format("{0} {1}", MinRequirement.GPUVendor, MinRequirement.GPUName);
                        LblMinVRAM.Text = string.Format("{0} GB", MinRequirement.VRAM.ToString());
                        LblMinInstallSize.Text = string.Format("{0} GB", MinRequirement.InstallationSize.ToString());
                        LblMinDownloadSize.Text = string.Format("{0} GB (Optional, if you don't want to keep archives, disable them in the previous screen)", MinRequirement.DownloadSize.ToString());
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
                INolvusVariantRequirementDTO RecRequirement = RecRequirements.Where(x => x.Height.ToString() == WorkingInstance.SettingsManager.GetSelectedHeight() &&
                                                                                         x.Width.ToString() == WorkingInstance.SettingsManager.GetSelectedWidth() &&
                                                                                         x.SREX.ToString().ToUpper() == WorkingInstance.OptionsManager.GetOptionValue("SREX") &&
                                                                                         x.Lods == WorkingInstance.OptionsManager.GetOptionValue("LODs")).FirstOrDefault()!;

                if (RecRequirement != null)
                {
                    LblMaxCPU.Text = RecRequirement.CPU;
                    LblMaxGPU.Text = string.Format("{0} {1}", RecRequirement.GPUVendor, RecRequirement.GPUName);
                    LblMaxVRAM.Text = string.Format("{0} GB", RecRequirement.VRAM.ToString());
                    LblMinInstallSize.Text = string.Format("{0} GB", RecRequirement.InstallationSize.ToString());
                    LblMinDownloadSize.Text = string.Format("{0} GB (Optional, if you don't want to keep archives, disable them in the previous screen)", RecRequirement.DownloadSize.ToString());
                }
                else
                {
                    RecRequirement = RecRequirements.Where(x => x.SREX.ToString().ToUpper() == WorkingInstance.OptionsManager.GetOptionValue("SREX") && x.Lods == WorkingInstance.OptionsManager.GetOptionValue("LODs")).OrderBy(x => Math.Abs(System.Convert.ToInt32(WorkingInstance.SettingsManager.GetSelectedWidth()) - x.Width)).FirstOrDefault()!;

                    if (RecRequirement != null)
                    {
                        LblMaxCPU.Text = RecRequirement.CPU;
                        LblMaxGPU.Text = string.Format("{0} {1}", RecRequirement.GPUVendor, RecRequirement.GPUName);
                        LblMaxVRAM.Text = string.Format("{0} GB", RecRequirement.VRAM.ToString());
                        LblMinInstallSize.Text = string.Format("{0} GB", RecRequirement.InstallationSize.ToString());
                        LblMinDownloadSize.Text = string.Format("{0} GB (Optional, if you don't want to keep archives, disable them in the previous screen)", RecRequirement.DownloadSize.ToString());
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
            INolvusInstance WorkingInstance = ServiceSingleton.Instances.WorkingInstance;

            ServiceSingleton.Dashboard.LoadFrameAsync<v6.GPUFrame>(
                new FrameParameters(new FrameParameter()
                {
                    Key = "VariantRequirement",
                    Value = MinRequirements.Where(x => x.SREX.ToString().ToUpper() == WorkingInstance.OptionsManager.GetOptionValue("SREX") &&
                                                  x.Lods == WorkingInstance.OptionsManager.GetOptionValue("LODs")
                                                 ).OrderBy(x => Math.Abs(System.Convert.ToInt32(WorkingInstance.SettingsManager.GetSelectedWidth()) - x.Width)).FirstOrDefault()
                })
            );
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

        private void LnkLblInfo_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            try
            {
                var url = "https://www.nolvus.net/appendix/installer/tech";
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