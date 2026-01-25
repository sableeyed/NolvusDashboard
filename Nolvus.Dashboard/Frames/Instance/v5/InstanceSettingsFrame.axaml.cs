using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Nolvus.Core.Enums;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Misc;
using Nolvus.Core.Services;
using Nolvus.Package.Mods;
using Nolvus.Dashboard.Controls;
using Nolvus.Dashboard.Frames.Instance;

namespace Nolvus.Dashboard.Frames.Instance.v5
{
    public partial class InstanceSettingsFrame : DashboardFrame
    {
        private bool _initializing = true;

        public InstanceSettingsFrame(IDashboard dashboard, FrameParameters parameters)
            : base(dashboard, parameters)
        {
            InitializeComponent();
            HookEvents();
        }

        private void HookEvents()
        {
            // Buttons
            BtnApplyRes.Click += BtnApplyRes_Click;
            BtnApplyDownScaling.Click += BtnApplyDownScaling_Click;
            BtnBack.Click += BtnBack_Click;

            // ComboBoxes
            DrpDwnLstScreenRes.SelectionChanged += DrpDwnLstScreenRes_SelectionChanged;
            DrpDwnLstDownRes.SelectionChanged += DrpDwnLstDownRes_SelectionChanged;
            DrpDwnLstRatios.SelectionChanged += DrpDwnLstRatios_SelectionChanged;
            DrpDwnLstDownLoc.SelectionChanged += DrpDwnLstDownLoc_SelectionChanged;
            DrpDwnLstIni.SelectionChanged += DrpDwnLstIni_SelectionChanged;

            // Toggles
            // Assume these are ToggleSwitch or CheckBox in XAML
            TglBtnEnableArchive.Checked += (_, __) => TglBtnEnableArchive_Toggled(true);
            TglBtnEnableArchive.Unchecked += (_, __) => TglBtnEnableArchive_Toggled(false);

            TglBtnDownScale.Checked += (_, __) => TglBtnDownScale_Toggled(true);
            TglBtnDownScale.Unchecked += (_, __) => TglBtnDownScale_Toggled(false);
        }

        #region Index helpers

        private int RatioIndex(List<string> ratios)
        {
            int index = 0;

            if (!string.IsNullOrEmpty(ServiceSingleton.Instances.WorkingInstance.Settings.Ratio))
            {
                foreach (var ratio in ratios)
                {
                    if (ratio == ServiceSingleton.Instances.WorkingInstance.Settings.Ratio)
                    {
                        break;
                    }

                    index++;
                }
            }

            return index;
        }

        private int ResolutionIndex(List<string> resolutions, out bool error)
        {
            int index = resolutions.Count - 1;

            error = false;
            bool found = false;

            var instance = ServiceSingleton.Instances.WorkingInstance;

            string resolution = $"{instance.Settings.Width}x{instance.Settings.Height}";

            if (!string.IsNullOrEmpty(instance.Settings.Width) &&
                !string.IsNullOrEmpty(instance.Settings.Height))
            {
                index = 0;

                foreach (var reso in resolutions)
                {
                    if (resolution == reso)
                    {
                        found = true;
                        break;
                    }

                    index++;
                }
            }

            if (found)
            {
                return index;
            }

            error = true;
            ShowMessage(
                "Error",
                $"The resolution you set up ({resolution}) is no more compatible with your current available resolutions. It can happen if you changed your monitor or modified the instancesdata.xml file manually. The dashboard will revert to the first available resolution. Be sure to change it.",
                MessageBoxType.Error);
            return 0;
        }

        private int DownScaledResolutionIndex(List<string> resolutions, out bool error)
        {
            int index = resolutions.Count - 1;

            error = false;
            bool found = false;

            var instance = ServiceSingleton.Instances.WorkingInstance;

            if (!string.IsNullOrEmpty(instance.Performance.DownWidth) &&
                !string.IsNullOrEmpty(instance.Performance.DownHeight))
            {
                string resolution = $"{instance.Performance.DownWidth}x{instance.Performance.DownHeight}";

                index = 0;

                foreach (var reso in resolutions)
                {
                    if (resolution == reso)
                    {
                        found = true;
                        break;
                    }

                    index++;
                }

                if (found)
                {
                    return index;
                }

                error = true;
                ShowMessage(
                    "Error",
                    $"The downscale resolution you set up ({resolution}) is no more compatible with your current available resolutions. It can happen if you changed your monitor or modified the instancesdata.xml file manually. The dashboard will revert to the first available resolution. Be sure to change it.",
                    MessageBoxType.Error);
                return 0;
            }

            return 0;
        }

        private int DownloadLocationIndex(List<string> locations)
        {
            var index = locations.FindIndex(x => x == ServiceSingleton.Instances.WorkingInstance.Settings.CDN);
            return index == -1 ? 0 : index;
        }

        private int AntiAliasingIndex(List<string> antiAliasing)
        {
            var index = antiAliasing.FindIndex(x => x == ServiceSingleton.Instances.WorkingInstance.Performance.AntiAliasing);
            return index == -1 ? 0 : index;
        }

        #endregion

        private void EnableFlatButton(Button button, bool enabled)
        {
            if (button == null) return;
            button.IsEnabled = enabled;
        }

        protected override async Task OnLoadedAsync()
        {
            try
            {
                bool resError;

                var instance = ServiceSingleton.Instances.WorkingInstance;

                LblHeader.Text = $"Settings for {instance.Name} v{instance.Version}";

                // Screen resolutions
                List<string> resolutions = ServiceSingleton.Globals.WindowsResolutions;
                DrpDwnLstScreenRes.ItemsSource = resolutions;
                DrpDwnLstScreenRes.SelectedIndex = ResolutionIndex(resolutions, out resError);

                if (resError)
                    ApplyResolution();

                // Ratios
                List<string> ratios = new()
                {
                    "16:9",
                    "21:9"
                };
                DrpDwnLstRatios.ItemsSource = ratios;
                DrpDwnLstRatios.SelectedIndex = RatioIndex(ratios);

                // Downscale resolutions
                DrpDwnLstDownRes.ItemsSource = resolutions;
                DrpDwnLstDownRes.SelectedIndex = DownScaledResolutionIndex(resolutions, out resError);

                if (resError)
                    ApplyDowncalingResolution();

                // Paths
                TxtBxInstancePath.Text = instance.InstallDir;
                TxtBxArchivePath.Text = instance.ArchiveDir;

                // Archiving toggle
                TglBtnEnableArchive.IsChecked = instance.Settings.EnableArchiving;

                // Downscale toggle
                TglBtnDownScale.IsChecked = instance.Performance.DownScaling == "TRUE";
                DrpDwnLstDownRes.IsEnabled = TglBtnDownScale.IsChecked == true;

                // Download locations
                var cdnList = CDN.Get();
                DrpDwnLstDownLoc.ItemsSource = cdnList;
                DrpDwnLstDownLoc.SelectedIndex = DownloadLocationIndex(cdnList);

                EnableFlatButton(BtnApplyRes, false);
                EnableFlatButton(BtnApplyDownScaling, false);

                // Labels
                LblVariant.Text = instance.Performance.Variant;
                LblAntiAliasing.Text = instance.Performance.AntiAliasing;

                if (instance.Performance.Variant == "Redux")
                    LblLODs.Text = "Redux";
                else
                    LblLODs.Text = instance.Performance.LODs;

                LblPhysics.Text = "No";
                LblRayTracing.Text = "No";
                LblFPS.Text = "No";
                LblHC.Text = "No";
                LblLeveling.Text = "No";
                LblAltStart.Text = "No";
                LblFantasyMode.Text = "No";
                LblNude.Text = "No";
                LblSkinType.Text = instance.Options.SkinType;

                if (instance.Performance.AdvancedPhysics == "TRUE")
                    LblPhysics.Text = "Yes";

                if (instance.Performance.RayTracing == "TRUE")
                    LblRayTracing.Text = "Yes";

                if (instance.Performance.FPSStabilizer == "TRUE")
                    LblFPS.Text = "Yes";

                if (instance.Options.HardcoreMode == "TRUE")
                    LblHC.Text = "Yes";

                if (instance.Options.AlternateLeveling == "TRUE")
                    LblLeveling.Text = "Yes";

                if (instance.Options.AlternateStart == "TRUE")
                    LblAltStart.Text = "Yes";

                if (instance.Options.FantasyMode == "TRUE")
                    LblFantasyMode.Text = "Yes";

                if (instance.Options.Nudity == "TRUE")
                    LblNude.Text = "Yes";

                LblENB.Text = ENBs.GetENBByCode(instance.Options.AlternateENB);

                // Ini profile settings
                List<string> iniSettings = new()
                {
                    "Low",
                    "Medium",
                    "High"
                };
                DrpDwnLstIni.ItemsSource = iniSettings;

                if (int.TryParse(instance.Performance.IniSettings, out var iniIndex) &&
                    iniIndex >= 0 && iniIndex < iniSettings.Count)
                {
                    DrpDwnLstIni.SelectedIndex = iniIndex;
                }
                else
                {
                    DrpDwnLstIni.SelectedIndex = 1; // Default "Medium"
                }

                // Disable downscaling when Anti Aliasing is DLAA
                DrpDwnLstDownRes.IsEnabled = instance.Performance.AntiAliasing != "DLAA";
                BtnApplyDownScaling.IsEnabled = instance.Performance.AntiAliasing != "DLAA";

                _initializing = false;
            }
            catch (Exception ex)
            {
                await ServiceSingleton.Dashboard.Error("Error during instance settings loading", ex.Message);
            }
        }

        #region Toggle handlers

        private void TglBtnEnableArchive_Toggled(bool isOn)
        {
            var instance = ServiceSingleton.Instances.WorkingInstance;
            instance.Settings.EnableArchiving = isOn;
            ServiceSingleton.Instances.Save();
        }

        private void TglBtnDownScale_Toggled(bool isOn)
        {
            if (!_initializing)
            {
                DrpDwnLstDownRes.IsEnabled = isOn;
                EnableFlatButton(BtnApplyDownScaling, true);
            }
        }

        #endregion

        #region ComboBox handlers

        private void DrpDwnLstScreenRes_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_initializing)
            {
                EnableFlatButton(BtnApplyRes, true);
            }
        }

        private void DrpDwnLstDownRes_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_initializing)
            {
                EnableFlatButton(BtnApplyDownScaling, true);
            }
        }

        private void DrpDwnLstRatios_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_initializing) return;

            EnableFlatButton(BtnApplyRes, true);

            var instance = ServiceSingleton.Instances.WorkingInstance;
            var value = DrpDwnLstRatios.SelectedItem as string ?? string.Empty;

            instance.Settings.Ratio = value;
            ServiceSingleton.Instances.Save();
        }

        private void DrpDwnLstDownLoc_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var instance = ServiceSingleton.Instances.WorkingInstance;
            instance.Settings.CDN = DrpDwnLstDownLoc.SelectedItem as string ?? string.Empty;
            ServiceSingleton.Instances.Save();
        }

        private async void DrpDwnLstIni_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_initializing) return;

            try
            {
                var instance = ServiceSingleton.Instances.WorkingInstance;

                int idx = DrpDwnLstIni.SelectedIndex;
                if (idx < 0) return;

                instance.Performance.IniSettings = idx.ToString();

                string iniPath = Path.Combine(instance.InstallDir, "MODS", "profiles", instance.Name);

                Directory.CreateDirectory(iniPath);

                File.WriteAllText(
                    Path.Combine(iniPath, "Skyrim.ini"),
                    ModOrganizer.GetIni(false, (IniLevel)idx, instance));

                File.WriteAllText(
                    Path.Combine(iniPath, "SkyrimPrefs.ini"),
                    ModOrganizer.GetIni(true, (IniLevel)idx, instance));

                ServiceSingleton.Instances.Save();
            }
            catch (Exception ex)
            {
                await ServiceSingleton.Dashboard.Error("Error during skyrim ini file configuration saving", ex.Message);
            }
        }

        #endregion

        #region Button handlers

        private async void BtnBack_Click(object? sender, RoutedEventArgs e)
        {
            await ServiceSingleton.Dashboard.LoadFrameAsync<InstanceDetailFrame>();
        }

        private void BtnApplyRes_Click(object? sender, RoutedEventArgs e)
        {
            ApplyResolution();
        }

        private void BtnApplyDownScaling_Click(object? sender, RoutedEventArgs e)
        {
            ApplyDowncalingResolution();
        }

        #endregion

        #region Apply logic

        private void ApplyResolution()
        {
            try
            {
                var instance = ServiceSingleton.Instances.WorkingInstance;

                var resolution = DrpDwnLstScreenRes.SelectedItem as string;
                if (string.IsNullOrEmpty(resolution))
                    return;

                string[] reso = resolution.Split('x', StringSplitOptions.TrimEntries);

                if (reso.Length != 2)
                    return;

                instance.Settings.Width = reso[0];
                instance.Settings.Height = reso[1];

                ServiceSingleton.Instances.Save();

                string fileName = Path.Combine(
                    instance.InstallDir,
                    "MODS",
                    "profiles",
                    instance.Name,
                    "SkyrimPrefs.ini");

                ServiceSingleton.Settings.StoreIniValue(fileName, "Display", "iSize W", instance.Settings.Width);
                ServiceSingleton.Settings.StoreIniValue(fileName, "Display", "iSize H", instance.Settings.Height);

                EnableFlatButton(BtnApplyRes, false);
            }
            catch (Exception ex)
            {
                ShowMessage(
                    "Error",
                    "Error applying resolution with message : " + ex.Message,
                    MessageBoxType.Error);
            }
        }

        private void ApplyDowncalingResolution()
        {
            try
            {
                var instance = ServiceSingleton.Instances.WorkingInstance;

                var resolution = DrpDwnLstDownRes.SelectedItem as string;
                if (string.IsNullOrEmpty(resolution))
                    return;

                string[] reso = resolution.Split('x', StringSplitOptions.TrimEntries);
                if (reso.Length != 2)
                    return;

                instance.Performance.DownScaling = TglBtnDownScale.IsChecked == true ? "TRUE" : "FALSE";
                instance.Performance.DownWidth = reso[0];
                instance.Performance.DownHeight = reso[1];

                ServiceSingleton.Instances.Save();

                // Apply to SSE Display Tweaks ini
                string settingsFile = Path.Combine(
                    instance.InstallDir,
                    "MODS",
                    "mods", //Case sensitivity strikes again
                    "SSE Display Tweaks",
                    "SKSE",
                    "Plugins",
                    "SSEDisplayTweaks.ini");

                if (File.Exists(settingsFile))
                {
                    string[] lines = File.ReadAllLines(settingsFile);
                    List<string> newLines = new();

                    bool foundResolution = false;
                    bool foundUpscale = false;

                    foreach (string line in lines)
                    {
                        string _line = line;

                        if (!foundResolution &&
                            line.Contains("Resolution") &&
                            !line.StartsWith("# "))
                        {
                            if (instance.Performance.DownScaling == "TRUE")
                                _line = "Resolution=" + instance.Performance.DownScaledResolution;
                            else
                                _line = "#Resolution=" + instance.Performance.DownScaledResolution;

                            foundResolution = true;
                        }
                        else if (!foundUpscale &&
                                 line.Contains("BorderlessUpscale") &&
                                 !line.StartsWith("# "))
                        {
                            if (instance.Performance.DownScaling == "TRUE")
                                _line = "BorderlessUpscale=true";
                            else
                                _line = "#BorderlessUpscale=false";

                            foundUpscale = true;
                        }

                        newLines.Add(_line);
                    }

                    File.WriteAllLines(settingsFile, newLines);
                }
                else
                {
                    ShowMessage(
                        "Error",
                        "The needed ini file (SSEDisplayTweaks.ini) is missing",
                        MessageBoxType.Error);
                }

                EnableFlatButton(BtnApplyDownScaling, false);
            }
            catch (Exception ex)
            {
                ShowMessage(
                    "Error",
                    "Error applying downscaling with message : " + ex.Message,
                    MessageBoxType.Error);
            }
        }

        #endregion

        private void ShowMessage(string title, string message, MessageBoxType type)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            NolvusMessageBox.Show(owner, title, message, type);
        }
    }
}
