using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Dashboard.Controls;
using Nolvus.Core.Enums;
using Nolvus.Components.Controls;
using Nolvus.Core.Misc;
using Nolvus.Package.Mods;
using Nolvus.Dashboard.Core;
using Nolvus.Dashboard.Frames.Installer;
using Nolvus.Dashboard.Frames.Manager.Options.v6;
using Nolvus.Dashboard.Frames.Remap.v6;
using Avalonia.Interactivity;

namespace Nolvus.Dashboard.Frames.Instance
{
    public partial class InstanceSettingsFrame : DashboardFrame
    {
        private bool Initializing = true;

        private int ResolutionIndex(List<string> Resolutions, out bool Error)
        {
            int Index = Resolutions.Count - 1;

            Error = false;

            bool Found = false;

            string Resolution = ServiceSingleton.Instances.WorkingInstance.SettingsManager.Width + "x" + ServiceSingleton.Instances.WorkingInstance.SettingsManager.Height;

            if (ServiceSingleton.Instances.WorkingInstance.SettingsManager.Height != string.Empty && ServiceSingleton.Instances.WorkingInstance.SettingsManager.Width != string.Empty)
            {
                Index = 0;

                foreach (var Reso in Resolutions)
                {
                    if (Resolution == Reso)
                    {
                        Found = true;
                        break;
                    }

                    Index++;
                }
            }

            if (Found)
            {
                return Index;
            }
            else
            {
                var owner = TopLevel.GetTopLevel(this) as Window;
                Error = true;
                NolvusMessageBox.Show(owner, "Error", "The resolution you set up (" + Resolution + ") is no more compatible with your current Windows available resolutions. It can happen if you changed your monitor or modified the instancesdata.xml file manually. The dashboard will revert to the first available resolution. Be sure to change it.", MessageBoxType.Error);
                return 0;
            }

        }

        private int DownScaledResolutionIndex(List<string> Resolutions, out bool Error)
        {
            int Index = Resolutions.Count - 1;

            Error = false;

            bool Found = false;

            if (ServiceSingleton.Instances.WorkingInstance.SettingsManager.DownWidth != string.Empty && ServiceSingleton.Instances.WorkingInstance.SettingsManager.DownHeight != string.Empty)
            {

                string Resolution = ServiceSingleton.Instances.WorkingInstance.SettingsManager.DownWidth + "x" + ServiceSingleton.Instances.WorkingInstance.SettingsManager.DownHeight;

                if (ServiceSingleton.Instances.WorkingInstance.SettingsManager.DownHeight != string.Empty && ServiceSingleton.Instances.WorkingInstance.SettingsManager.DownWidth != string.Empty)
                {
                    Index = 0;

                    foreach (var Reso in Resolutions)
                    {
                        if (Resolution == Reso)
                        {
                            Found = true;
                            break;
                        }

                        Index++;
                    }
                }

                if (Found)
                {
                    return Index;
                }
                else
                {
                    var owner = TopLevel.GetTopLevel(this) as Window;
                    Error = true;
                    NolvusMessageBox.Show(owner, "Error", "The downscale resolution you set up (" + Resolution + ") is no more compatible with your current Windows available resolutions. It can happen if you changed your monitor or modified the instancesdata.xml file manually. The dashboard will revert to the first available resolution. Be sure to change it.", MessageBoxType.Error);
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }

        private int DownloadLocationIndex(List<string> Locations)
        {
            var Index = Locations.FindIndex(x => x == ServiceSingleton.Instances.WorkingInstance.SettingsManager.CDN);

            return Index == -1 ? 0 : Index;
        }

        private int AntiAliasingIndex(List<string> AntiAliasing)
        {
            var Index = AntiAliasing.FindIndex(x => x == ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("AntiAliasing"));
            return Index == -1 ? 0 : Index;
        }

        public InstanceSettingsFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
            TglBtnEnableArchive.IsCheckedChanged += OnArchiveChanged;
            TglBtnDownScale.IsCheckedChanged += OnDownScaleChanged;
            BtnBack.Click += BtnBack_Click;
            BtnApplyRes.Click += BtnApplyRes_Click;
            BtnApplyDownScaling.Click += BtnApplyDownScaling_Click;
        }

        protected override async Task OnLoadedAsync()
        {
            try
            {
                bool ResError = false;

                INolvusInstance Instance = ServiceSingleton.Instances.WorkingInstance;

                LblHeader.Text = string.Format("Settings for {0}", Instance.FullName);

                ServiceSingleton.Dashboard.Info(string.Format("Instance settings for {0}", Instance.FullName));

                List<string> Resolutions = ServiceSingleton.Globals.WindowsResolutions;
                List<string> DownResolutions = ServiceSingleton.Globals.GetDownscaleResolutions();
                DrpDwnLstScreenRes.ItemsSource = Resolutions;
                DrpDwnLstScreenRes.SelectedIndex = ResolutionIndex(Resolutions, out ResError);

                if (ResError)
                {
                    ApplyResolution();
                }

                LblLanguage.Text = string.Format("{0} - ({1})", Instance.SettingsManager.LgName, Instance.SettingsManager.LgCode);

                DrpDwnLstDownRes.ItemsSource = DownResolutions;
                DrpDwnLstDownRes.SelectedIndex = DownScaledResolutionIndex(DownResolutions, out ResError);

                if (ResError)
                {
                    ApplyDownscalingResolution();
                }

                LblInstallDir.Text = Instance.SettingsManager.InstallDir;
                LblArchiveDir.Text = Instance.SettingsManager.ArchiveDir;

                TglBtnEnableArchive.IsChecked = false;

                if (Instance.SettingsManager.EnableArchiving)
                {
                    TglBtnEnableArchive.IsChecked = true;
                }

                TglBtnDownScale.IsChecked = false;

                if (Instance.SettingsManager.DownScaling == "TRUE")
                {
                    TglBtnDownScale.IsChecked = true;
                    DrpDwnLstDownRes.IsEnabled = TglBtnDownScale.IsChecked == true;
                }

                DrpDwnLstDownLoc.ItemsSource = CDN.Get();
                DrpDwnLstDownLoc.SelectedIndex = DownloadLocationIndex(CDN.Get());

                BtnApplyRes.IsEnabled = false;
                BtnApplyDownScaling.IsEnabled = false;

                List<string> IniSettings = new List<string>();

                IniSettings.Add("Low");
                IniSettings.Add("Medium");
                IniSettings.Add("High");

                DrpDwnLstIni.ItemsSource = IniSettings;

                DrpDwnLstIni.SelectedIndex = System.Convert.ToInt16(Instance.SettingsManager.IniSettings);

                // Avalonia's DataGrid groups through its collection view, and the groups start
                // expanded, which stands in for upstream's ExpandAllGroup().
                var Options = new DataGridCollectionView(Instance.OptionsManager.GetOptions());
                Options.GroupDescriptions.Add(new DataGridPathGroupDescription("Category"));

                OptionsGrid.ItemsSource = Options;

                TglBtnDownScale.IsEnabled = Instance.OptionsManager.GetOptionValue("AntiAliasing") != "DLAA" && Instance.OptionsManager.GetOptionValue("AntiAliasing") != "FSR";

                if (!TglBtnDownScale.IsEnabled)
                {
                    LblDownscalingInfo.Text = " (Disabled when using DLAA/FSR/Frame Generation)";
                }
                else
                {
                    LblDownscalingInfo.Text = string.Empty;
                }

                Initializing = false;

            }
            catch (Exception ex)
            {
                await ServiceSingleton.Dashboard.Error("Error during instance settings loading", ex.Message);
            }
        }

        private void OnArchiveChanged(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Instances.WorkingInstance.SettingsManager.EnableArchiving = TglBtnEnableArchive.IsChecked == true;

            ServiceSingleton.Instances.Save();
        }

        private void OnDownScaleChanged(object? sender, RoutedEventArgs e)
        {
            if (!Initializing)
            {
                DrpDwnLstDownRes.IsEnabled = TglBtnDownScale.IsChecked == true;
                BtnApplyDownScaling.IsEnabled = true;
            }
        }

        private void OnScreenResolutionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!Initializing)
            {
                BtnApplyRes.IsEnabled = true;
            }
        }

        private void OnDownScaleResolutionChanged(object? sender, SelectionChangedEventArgs e)
        {

        }

        private void OnDownloadLocationChanged(object? sender, SelectionChangedEventArgs e)
        {
            ServiceSingleton.Instances.WorkingInstance.SettingsManager.CDN = DrpDwnLstDownLoc.SelectedItem.ToString(); //might crash

            ServiceSingleton.Instances.Save();
        }

        private async void OnIniChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!Initializing)
            {
                try
                {
                    INolvusInstance Instance = ServiceSingleton.Instances.WorkingInstance;

                    Instance.SettingsManager.IniSettings = DrpDwnLstIni.SelectedIndex.ToString();

                    File.WriteAllText(Path.Combine(Instance.SettingsManager.InstallDir, "MODS", "profiles", Instance.Name, "Skyrim.ini"), ModOrganizer.GetIni(false, (IniLevel)System.Convert.ToInt16(Instance.SettingsManager.IniSettings), Instance));
                    File.WriteAllText(Path.Combine(Instance.SettingsManager.InstallDir, "MODS", "profiles", Instance.Name, "SkyrimPrefs.ini"), ModOrganizer.GetIni(true, (IniLevel)System.Convert.ToInt16(Instance.SettingsManager.IniSettings), Instance));

                    ServiceSingleton.Instances.Save();
                }
                catch(Exception ex)
                {
                    await ServiceSingleton.Dashboard.Error("Error during skyrim ini file configuration saving", ex.Message);
                }
            }
        }

        private async void BtnBack_Click(object? sender, RoutedEventArgs e)
        {
            await ServiceSingleton.Dashboard.LoadFrameAsync<InstanceDetailFrame>();
        }

        private void ApplyResolution()
        {
            try
            {
                INolvusInstance Instance = ServiceSingleton.Instances.WorkingInstance;

                string Resolution = DrpDwnLstScreenRes.SelectedValue.ToString();

                string[] Reso = Resolution.Split(new char[] { 'x' });

                Instance.SettingsManager.Width = Reso[0];
                Instance.SettingsManager.Height = Reso[1];

                ServiceSingleton.Instances.Save();

                string FileName = Path.Combine(Instance.SettingsManager.InstallDir, "MODS", "profiles", Instance.Name, "SkyrimPrefs.ini");

                ServiceSingleton.Settings.StoreIniValue(FileName, "Display", "iSize W", Instance.SettingsManager.Width);
                ServiceSingleton.Settings.StoreIniValue(FileName, "Display", "iSize H", Instance.SettingsManager.Height);

                BtnApplyRes.IsEnabled = false;
            }
            catch (Exception ex)
            {
                var owner = TopLevel.GetTopLevel(this) as Window;
                NolvusMessageBox.Show(owner, "Error", "Error applying resolution with message : " + ex.Message, MessageBoxType.Error);
            }
        }

        private void BtnApplyRes_Click(object? sender, RoutedEventArgs e)
        {
            ApplyResolution();
        }

        private void ApplyDownscalingResolution()
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            try
            {
                INolvusInstance Instance = ServiceSingleton.Instances.WorkingInstance;

                string Resolution = DrpDwnLstDownRes.SelectedValue.ToString();

                string[] Reso = Resolution.Split(['x']);

                if (TglBtnDownScale.IsChecked == true)
                {
                    Instance.SettingsManager.DownScaling = "TRUE";
                }
                else
                {
                    Instance.SettingsManager.DownScaling = "FALSE";
                }

                Instance.SettingsManager.DownWidth = Reso[0];
                Instance.SettingsManager.DownHeight = Reso[1];

                ServiceSingleton.Instances.Save();

                #region Apply Settings

                string SettingsFile = Path.Combine(Instance.SettingsManager.InstallDir, "MODS", "mods", "SSE Display Tweaks", "SKSE", "Plugins", "SSEDisplayTweaks.ini"); //Case sensitivity strikes again

                if (File.Exists(SettingsFile))
                {
                    string[] Lines = System.IO.File.ReadAllLines(SettingsFile);

                    List<string> NewLines = new List<string>();

                    bool FoundResolution = false;
                    bool FoundUpscale = false;

                    foreach (string Line in Lines)
                    {
                        string _Line = Line;

                        if (Line.Contains("Resolution") && Line.Substring(0, 1) != "# " && !FoundResolution)
                        {
                            if (Instance.SettingsManager.DownScaling == "TRUE")
                            {
                                _Line = "Resolution" + "=" + Instance.SettingsManager.DownScaledResolution;
                            }
                            else
                            {
                                _Line = "#Resolution" + "=" + Instance.SettingsManager.DownScaledResolution;
                            }

                            FoundResolution = true;
                        }
                        else if (Line.Contains("BorderlessUpscale") && Line.Substring(0, 1) != "# " && !FoundUpscale)
                        {
                            if (Instance.SettingsManager.DownScaling == "TRUE")
                            {
                                _Line = "BorderlessUpscale" + "=true";
                            }
                            else
                            {
                                _Line = "#BorderlessUpscale" + "=false";
                            }

                            FoundUpscale = true;
                        }

                        NewLines.Add(_Line);
                    }

                    System.IO.File.WriteAllLines(SettingsFile, NewLines.ToArray());
                }
                else
                {
                    NolvusMessageBox.Show(owner, "Error", "The needed ini file (SSEDisplayTweaks.ini) is missing", MessageBoxType.Error);
                }

                #endregion

                BtnApplyDownScaling.IsEnabled = false;
            }
            catch (Exception ex)
            {
                NolvusMessageBox.Show(owner, "Error", "Error applying downscaling with message : " + ex.Message, MessageBoxType.Error);
            }
        }

        private void BtnApplyDownScaling_Click(object? sender, RoutedEventArgs e)
        {
            ApplyDownscalingResolution();
        }

        private async void LnkLblOptionsManager_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var Instance = ServiceSingleton.Instances.WorkingInstance;

            if (Instance.Name == Strings.NolvusAwakening)
            {
                await ServiceSingleton.Dashboard.LoadFrameAsync<OptionsManagerFrame>();
            }
            else
            {
                var window = TopLevel.GetTopLevel(this) as Window;
                await NolvusMessageBox.Show(window, "Options Manager", string.Format("This feature is not available for {0}.", Instance.Name), MessageBoxType.Info);
            }
            
        }

        private void LnkLblRemap_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            ServiceSingleton.Dashboard.LoadFrame<RemapInstanceFrame>();
        }

    }
}
