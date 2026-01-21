using Nolvus.Core.Interfaces;
using Nolvus.Core.Frames;
using Nolvus.Core.Enums;
using Nolvus.Core.Services;
using Nolvus.Instance.Core;
using Nolvus.Dashboard.Core;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Controls;
using Nolvus.Dashboard.Controls;

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
            TglBtnPhysics.IsCheckedChanged += OnPhysicsChanged;
            TglBtnDownScale.IsCheckedChanged += OnDownscaleChanged;
            TglBtnFPSStabilizer.IsCheckedChanged += OnFPSStabilizerChanged;
            TglBtnRayTracing.IsCheckedChanged += OnRayTracingChanged;
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

                var primary = top?.Screens?.Primary;
                if (primary == null)
                {
                    ServiceSingleton.Logger.Log("No primary screen detected... Falling back to 1920x1080");
                    Instance.Settings.Height = "1080";
                    Instance.Settings.Width = "1920";
                }
                else
                {
                    Instance.Settings.Height = primary.WorkingArea.Height.ToString();
                    Instance.Settings.Width = primary.WorkingArea.Width.ToString();
                }

                DrpDwnLstResolution.ItemsSource = ServiceSingleton.Globals.WindowsResolutions;

                DrpDwnLstResolution.SelectedIndex = ResolutionIndex(ServiceSingleton.Globals.WindowsResolutions);

                TglBtnDownScale.IsChecked = false;

                if (Instance.Performance.DownScaling == "TRUE")
                {
                    TglBtnDownScale.IsChecked = true;
                }

                #endregion

                DrpDwnLstScreenRes.ItemsSource = ServiceSingleton.Globals.GetDownscaleResolutions();
                DrpDwnLstScreenRes.SelectedIndex = DownResolutionIndex(ServiceSingleton.Globals.GetDownscaleResolutions());
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

        private async void BtnContinue_Click(object? sender, RoutedEventArgs e)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            var Instance = ServiceSingleton.Instances.WorkingInstance;
            var Performance = Instance.Performance;

            if (Performance.DownScaling == "TRUE" && (Performance.DownScaling == "TRUE" && (Instance.Settings.Height == Performance.DownHeight || System.Convert.ToInt32(Performance.DownHeight) > System.Convert.ToInt32(Instance.Settings.Height)) && (Instance.Settings.Width == Performance.DownWidth || System.Convert.ToInt32(Performance.DownWidth) > System.Convert.ToInt32(Instance.Settings.Width))))
            {
                NolvusMessageBox.Show(owner, "Invalid Downscaling setting", "If downscaling is enabled, the downscaled resolution must be less than the monitor resolution!", MessageBoxType.Error);
            }
            else
            {
                bool? result = await NolvusMessageBox.ShowConfirmation(owner, "Confirmation", "Some of the options you selected (like the variant, LODs quality, Advanced physics, Global Illumination or FPS stabilizer) can not be changed after installation. Are you sure you want to continue?");
                if (result == true)
                {
                    if ((Performance.Variant == "Redux") && (Performance.AdvancedPhysics == "TRUE" || Performance.RayTracing == "TRUE" || Performance.AntiAliasing == "DLAA"))
                    {
                        bool? redux = await NolvusMessageBox.ShowConfirmation(owner, "Confirmation", "You selected the Redux variant with other effects that are normally disabled by default with this variant. Be sure you have more than the minimum requirement. Are you sure you want to continue?");
                        if (redux == true)
                        {
                            ServiceSingleton.Dashboard.LoadFrame<v5.OptionsFrame>();
                        }
                    }
                    else
                    {                        
                        ServiceSingleton.Dashboard.LoadFrame<v5.OptionsFrame>();
                    }     
                }
            }
        }

        private void DisplayHardwareRequirement()
        {
            if (ServiceSingleton.Instances.WorkingInstance.Performance.Variant == "Ultra")
            {
                if (ServiceSingleton.Instances.WorkingInstance.Performance.LODs == "Ultra")
                {
                    LblReqGpu.Text = "Recommended : RTX 3080 Ti";
                    LblReqVram.Text = "Recommended : 12Gb @1440p (HIGHER GPU with HIGHER VRAM needed beyond 1440p)";
                    LblReqStorageTotal.Text = "Mods: 277 Gb, Archives: 115Gb, Total: 392Gb";
                }
                else if (ServiceSingleton.Instances.WorkingInstance.Performance.LODs == "Performance")
                {
                    LblReqGpu.Text = "Recommended : RTX 3080";
                    LblReqVram.Text = "Recommended : 10Gb @1440p (HIGHER GPU with HIGHER VRAM needed beyond 1440p)";
                    LblReqStorageTotal.Text = "Mods: 254 Gb, Archives: 115Gb, Total: 369Gb";
                }
                else if (ServiceSingleton.Instances.WorkingInstance.Performance.LODs == "Ultra Performance")
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

        private void OnVariantChanged(object? sender, SelectionChangedEventArgs e)
        {
            ServiceSingleton.Instances.WorkingInstance.Performance.Variant = DrpDwnLstVariant.SelectedItem.ToString();

            if (DrpDwnLstVariant.SelectedItem.ToString() == "Ultra")
            {                
                //LblLods.ForeColor = Color.White;
                DrpDwnLstLODs.IsEnabled = true;
            }
            else
            {                
                //LblLods.ForeColor = Color.Gray;
                DrpDwnLstLODs.IsEnabled = false;
            }

            DisplayHardwareRequirement();
        } 

        private void OnAntiAliasingChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DrpDwnLstAntiAliasing.SelectedValue != null)
            {
                ServiceSingleton.Instances.WorkingInstance.Performance.AntiAliasing = DrpDwnLstAntiAliasing.SelectedItem.ToString();

                if (ServiceSingleton.Instances.WorkingInstance.Performance.AntiAliasing == "DLAA")
                {
                    TglBtnDownScale.IsEnabled = false;
                    TglBtnDownScale.IsChecked = false;
                }
                else
                {
                    TglBtnDownScale.IsEnabled = true;                    
                }
            }

            DisplayHardwareRequirement();
        }

        private void OnLodsChanged(object? sender, SelectionChangedEventArgs e)
        {
            ServiceSingleton.Instances.WorkingInstance.Performance.LODs = DrpDwnLstLODs.SelectedItem.ToString();
            DisplayHardwareRequirement();
        }

        private void OnListRatiosChanged(object? sender, SelectionChangedEventArgs e)
        {
            ServiceSingleton.Instances.WorkingInstance.Settings.Ratio = DrpDwnLstRatios.SelectedItem.ToString();
            //DrpDwnLstScreenRes.ItemsSource = ServiceSingleton.Globals.GetDownscaleResolutions();
            //DrpDwnLstScreenRes.SelectedIndex = DownResolutionIndex(ServiceSingleton.Globals.GetDownscaleResolutions());
        }

        private void OnResolutionChanged(object? sender, SelectionChangedEventArgs e)
        {
            INolvusInstance WorkingInstance = ServiceSingleton.Instances.WorkingInstance;

            string Resolution = DrpDwnLstResolution.SelectedItem.ToString();

            string[] Reso = Resolution.Split(new char[] { 'x' });

            WorkingInstance.Settings.Width = Reso[0];
            WorkingInstance.Settings.Height = Reso[1];
        }

        private void OnDownscaledResolutionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DrpDwnLstScreenRes.SelectedValue != null)
            {
                string Resolution = DrpDwnLstScreenRes.SelectedItem.ToString();

                string[] Reso = Resolution.Split(new char[] { 'x' });

                ServiceSingleton.Instances.WorkingInstance.Performance.DownWidth = Reso[0];
                ServiceSingleton.Instances.WorkingInstance.Performance.DownHeight = Reso[1];
            }
        }

        private void OnIniChanged(object? sender, SelectionChangedEventArgs e)
        {
            ServiceSingleton.Instances.WorkingInstance.Performance.IniSettings = DrpDwnLstIni.SelectedIndex.ToString(); 
        }

        private void OnRayTracingChanged(object? sender, RoutedEventArgs e)
        {
            if (TglBtnRayTracing.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.Performance.RayTracing = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.Performance.RayTracing = "FALSE";
            }
        }

        private void OnFPSStabilizerChanged(object? sender, RoutedEventArgs e)
        {
            if (TglBtnFPSStabilizer.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.Performance.FPSStabilizer = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.Performance.FPSStabilizer = "FALSE";
            }
        }

        private void OnPhysicsChanged(object? sender, RoutedEventArgs e)
        {
            if (TglBtnPhysics.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.Performance.AdvancedPhysics = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.Performance.AdvancedPhysics = "FALSE";
            }
        }

        private void OnDownscaleChanged(object? sender, RoutedEventArgs e)
        {
            if (TglBtnDownScale.IsChecked == true)
            {
                DrpDwnLstScreenRes.IsEnabled = TglBtnDownScale.IsChecked == true;
                ServiceSingleton.Instances.WorkingInstance.Performance.DownScaling = "TRUE";
                DrpDwnLstScreenRes.SelectedIndex = ResolutionIndex(ServiceSingleton.Globals.WindowsResolutions);
            }
            else
            {
                DrpDwnLstScreenRes.IsEnabled = TglBtnDownScale.IsChecked == false;
                ServiceSingleton.Instances.WorkingInstance.Performance.DownScaling = "FALSE";                
            }    
        }

    }
}