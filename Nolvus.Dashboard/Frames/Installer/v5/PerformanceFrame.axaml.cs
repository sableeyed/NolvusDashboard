using Nolvus.Core.Interfaces;
using Nolvus.Core.Frames;
using Nolvus.Core.Enums;
using Nolvus.Core.Services;
using Vcc.Nolvus.Api.Installer.Services;
using Vcc.Nolvus.Api.Installer.Library;
using Nolvus.Instance.Core;
using Nolvus.Dashboard.Core;
using System.Diagnostics;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Controls;
using Nolvus.Dashboard.Controls;

namespace Nolvus.Dashboard.Frames.Installer.v5
{
    public partial class PerformanceFrame : DashboardFrame
    {
        private List<string> IniSettings = new List<string>();

        public PerformanceFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();

            BtnPrevious.Click += BtnPrevious_Click;
            BtnContinue.Click += BtnContinue_Click;
            TglBtnPhysics.IsCheckedChanged += OnPhysicsChanged;
            TglBtnDownScale.IsCheckedChanged += OnDownscaleChanged;
            TglBtnFPSStabilizer.IsCheckedChanged += OnFPSStabilizerChanged;
            TglBtnRayTracing.IsCheckedChanged += OnRayTracingChanged;
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

        private int DownResolutionIndex(List<string> Resolutions)
        {
            INolvusInstance WorkingInstance = ServiceSingleton.Instances.WorkingInstance;

            var Index = Resolutions.FindIndex(x => x == WorkingInstance.SettingsManager.DownWidth + "x" + WorkingInstance.SettingsManager.DownHeight);

            return Index == -1 ? 0 : Index;
        }

        private int AntiAliasingIndex(IEnumerable<INolvusOptionChoiceDTO> AntiAliasing)
        {
            var Index = AntiAliasing.ToList().FindIndex(x => x.Value == ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("AntiAliasing"));
            return Index == -1 ? 0 : Index;
        }

        private int VariantIndex(IEnumerable<INolvusVariantDTO> Variants)
        {
            INolvusInstance WorkingInstance = ServiceSingleton.Instances.WorkingInstance;

            var Index = Variants.ToList().FindIndex(x => x.Name == WorkingInstance.SettingsManager.Variant);

            return Index == -1 ? 0 : Index;
        }

        private int LODsIndex(IEnumerable<INolvusOptionChoiceDTO> LODs)
        {
            var Index = LODs.ToList().FindIndex(x => x.Value == ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("LODs"));
            return Index == -1 ? 0 : Index;
        }

        private bool IsNvidiaRTX()
        {
            return ServiceSingleton.Globals.GetVideoAdapters().Where(x => x.Contains("NVIDIA") && x.Contains("RTX")).FirstOrDefault() != null;
        }

        private async Task LoadLODs()
        {
            var Instance = ServiceSingleton.Instances.WorkingInstance;

            var LODs = await ApiManager.Service.Installer.GetOptionChoices("LODs", Instance.SettingsManager.Variant, Instance.Id);

            DrpDwnLstLODs.ItemsSource = null;
            DrpDwnLstLODs.ItemsSource = LODs;

            DrpDwnLstLODs.SelectedIndex = LODsIndex(LODs);
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

                DrpDwnLstResolution.ItemsSource = ServiceSingleton.Globals.WindowsResolutions;

                DrpDwnLstResolution.SelectedIndex = ResolutionIndex(ServiceSingleton.Globals.WindowsResolutions);

                DrpDwnLstScreenRes.ItemsSource = ServiceSingleton.Globals.GetDownscaleResolutions();
                DrpDwnLstScreenRes.SelectedIndex = DownResolutionIndex(ServiceSingleton.Globals.GetDownscaleResolutions());
                DrpDwnLstScreenRes.IsEnabled = false;

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

                if (!ServiceSingleton.Settings.ForceAA)
                {
                    LblGPU.Text = GPU;

                    if (!IsNvidiaRTX())
                    {
                        ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("AntiAliasing").Value = "TAA";
                        DrpDwnLstAntiAliasing.IsEnabled = false;
                    }
                }
                else
                {
                    LblGPU.Text = GPU + " (CHECK BYPASSED)";
                }

                #endregion

                #region Variant

                var Variants = await ApiManager.Service.Installer.GetNolvusVariants(Instance.Id);

                DrpDwnLstVariant.ItemsSource = Variants;

                DrpDwnLstVariant.SelectedIndex = VariantIndex(Variants);

                TglBtnPhysics.IsChecked = false;

                if (Instance.OptionsManager.GetOptionValue("AdvancedPhysics") == "TRUE")
                {
                    TglBtnPhysics.IsChecked = true;
                }

                TglBtnRayTracing.IsChecked = false;

                if (Instance.OptionsManager.GetOptionValue("RayTracing") == "TRUE")
                {
                    TglBtnRayTracing.IsChecked = true;
                }

                TglBtnFPSStabilizer.IsChecked = false;

                if (Instance.OptionsManager.GetOptionValue("FPSStabilizer") == "TRUE")
                {
                    TglBtnFPSStabilizer.IsChecked = true;
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
            var OptionsManager = Instance.OptionsManager;

            if (SettingsManager.DownScaling == "TRUE" && ((SettingsManager.Height == SettingsManager.DownHeight || System.Convert.ToInt32(SettingsManager.DownHeight) > System.Convert.ToInt32(SettingsManager.Height)) && (SettingsManager.Width == SettingsManager.DownWidth || System.Convert.ToInt32(SettingsManager.DownWidth) > System.Convert.ToInt32(SettingsManager.Width))))
            {
                NolvusMessageBox.Show(owner, "Invalid Downscaling setting", "If downscaling is enabled, the downscaled resolution must be less than the monitor resolution!", MessageBoxType.Error);
            }
            else
            {
                bool? result = await NolvusMessageBox.ShowConfirmation(owner, "Confirmation", "Some of the options you selected (like the variant, LODs quality, Advanced physics, Global Illumination or FPS stabilizer) can not be changed after installation. Are you sure you want to continue?");
                if (result == true)
                {
                    if ((SettingsManager.Variant == "Redux") && (OptionsManager.GetOptionValue("AdvancedPhysics") == "TRUE" || OptionsManager.GetOptionValue("RayTracing") == "TRUE" || OptionsManager.GetOptionValue("AntiAliasing") == "DLAA"))
                    {
                        bool? redux = await NolvusMessageBox.ShowConfirmation(owner, "Confirmation", "You selected the Redux variant with other effects that are normally disabled by default with this variant. Be sure you have more than the minimum requirement. Are you sure you want to continue?");
                        if (redux == true)
                        {
                            await ServiceSingleton.Dashboard.LoadFrameAsync<v5.OptionsFrame>();
                        }
                    }
                    else
                    {
                        await ServiceSingleton.Dashboard.LoadFrameAsync<v5.OptionsFrame>();
                    }
                }
            }
        }

        private void OnDownscaleChanged(object? sender, RoutedEventArgs e)
        {
            if (ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("AntiAliasing") != "DLAA")
            {
                if (TglBtnDownScale.IsChecked == true)
                {
                    DrpDwnLstScreenRes.IsEnabled = TglBtnDownScale.IsChecked == true;
                    ServiceSingleton.Instances.WorkingInstance.SettingsManager.DownScaling = "TRUE";
                    DrpDwnLstScreenRes.SelectedIndex = ResolutionIndex(ServiceSingleton.Globals.GetDownscaleResolutions());
                }
                else
                {
                    DrpDwnLstScreenRes.IsEnabled = TglBtnDownScale.IsChecked == true;
                    ServiceSingleton.Instances.WorkingInstance.SettingsManager.DownScaling = "FALSE";
                    DrpDwnLstAntiAliasing.IsEnabled = true;
                }
            }
            else
            {
                var owner = TopLevel.GetTopLevel(this) as Window;

                DrpDwnLstAntiAliasing.SelectedIndex = 0;
                DrpDwnLstAntiAliasing.IsEnabled = false;
                NolvusMessageBox.Show(owner, "Antialiasing", "This option is not compatible with DLAA. Antialiasing has been forced to TAA.", MessageBoxType.Info);
                DrpDwnLstScreenRes.IsEnabled = true;
                ServiceSingleton.Instances.WorkingInstance.SettingsManager.DownScaling = "TRUE";
                DrpDwnLstScreenRes.SelectedIndex = DownResolutionIndex(ServiceSingleton.Globals.GetDownscaleResolutions());
            }
        }

        private void OnPhysicsChanged(object? sender, RoutedEventArgs e)
        {
            if (TglBtnPhysics.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("AdvancedPhysics").Value = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("AdvancedPhysics").Value = "FALSE";
            }
        }

        private void OnRayTracingChanged(object? sender, RoutedEventArgs e)
        {
            if (TglBtnRayTracing.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("RayTracing").Value = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("RayTracing").Value = "FALSE";
            }
        }

        private void OnFPSStabilizerChanged(object? sender, RoutedEventArgs e)
        {
            if (TglBtnFPSStabilizer.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("FPSStabilizer").Value = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("FPSStabilizer").Value = "FALSE";
            }
        }

        private void OnIniChanged(object? sender, SelectionChangedEventArgs e)
        {
            ServiceSingleton.Instances.WorkingInstance.SettingsManager.IniSettings = DrpDwnLstIni.SelectedIndex.ToString();
        }

        private async void OnVariantChanged(object? sender, SelectionChangedEventArgs e)
        {
            ServiceSingleton.Instances.WorkingInstance.SettingsManager.Variant = (DrpDwnLstVariant.SelectedItem as INolvusVariantDTO).Name;

            await LoadLODs();

            if (ServiceSingleton.Instances.WorkingInstance.SettingsManager.Variant == "Ultra")
            {
                LblLods.Foreground = Brushes.White;
                DrpDwnLstLODs.IsEnabled = true;
            }
            else
            {
                LblLods.Foreground = Brushes.Gray;
                DrpDwnLstLODs.IsEnabled = false;
                DrpDwnLstLODs.SelectedIndex = 0;
            }

            DisplayHardwareRequirement();
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
            }

            DisplayHardwareRequirement();
        }

        private void OnLodsChanged(object? sender, SelectionChangedEventArgs e)
        {
            // LoadLODs swaps the list, and Avalonia raises this with nothing selected while it does.
            if (DrpDwnLstLODs.SelectedItem == null)
                return;

            ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("LODs").Value = (DrpDwnLstLODs.SelectedItem as INolvusOptionChoiceDTO).Value;

            DisplayHardwareRequirement();
        }

        private void OnListRatiosChanged(object? sender, SelectionChangedEventArgs e)
        {
            ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("Ratio").Value = (DrpDwnLstRatios.SelectedItem as INolvusOptionChoiceDTO).Value;
        }

        private void OnResolutionChanged(object? sender, SelectionChangedEventArgs e)
        {
            INolvusInstance WorkingInstance = ServiceSingleton.Instances.WorkingInstance;

            string Resolution = DrpDwnLstResolution.SelectedItem.ToString();

            string[] Reso = Resolution.Split(new char[] { 'x' });

            WorkingInstance.SettingsManager.Width = Reso[0];
            WorkingInstance.SettingsManager.Height = Reso[1];
        }

        private void DisplayHardwareRequirement()
        {
            if (ServiceSingleton.Instances.WorkingInstance.SettingsManager.Variant == "Ultra")
            {
                if (ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("LODs") == "Ultra")
                {
                    LblReqGpu.Text = "Recommended : RTX 3080 Ti";
                    LblReqVram.Text = "Recommended : 12Gb @1440p (HIGHER GPU with HIGHER VRAM needed beyond 1440p)";
                    LblReqStorageTotal.Text = "Mods: 277 Gb, Archives: 115Gb, Total: 392Gb";
                }
                else if (ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("LODs") == "Performance")
                {
                    LblReqGpu.Text = "Recommended : RTX 3080";
                    LblReqVram.Text = "Recommended : 10Gb @1440p (HIGHER GPU with HIGHER VRAM needed beyond 1440p)";
                    LblReqStorageTotal.Text = "Mods: 254 Gb, Archives: 115Gb, Total: 369Gb";
                }
                else if (ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("LODs") == "Ultra Performance")
                {
                    LblReqGpu.Text = "Recommended : RTX 2080 Ti";
                    LblReqVram.Text = "Recommended : 10Gb @1440p (HIGHER GPU with HIGHER VRAM needed beyond 1440p)";
                    LblReqStorageTotal.Text = "Mods: 251 Gb, Archives: 115Gb, Total: 366Gb";
                }
            }
            else
            {
                LblReqGpu.Text = "Minimum : GTX 1080, Recommended : RTX 2070";
                LblReqVram.Text = "Recommended : 8Gb @1080p (HIGHER GPU with HIGHER VRAM needed beyond 1080p)";
                LblReqStorageTotal.Text = "Mods: 240 Gb, Archives: 105Gb, Total: 345Gb";
            }
        }

        private void LnkLblInfo_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://www.nolvus.net/appendix/installer/tech") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ServiceSingleton.Logger.Log("Failed to open link: " + ex.Message);
            }
        }
    }
}
