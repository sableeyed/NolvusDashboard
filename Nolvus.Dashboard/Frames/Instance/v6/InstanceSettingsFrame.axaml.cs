using Avalonia.Controls;
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
using Avalonia.Interactivity;

namespace Nolvus.Dashboard.Frames.Instance.v6
{
    public partial class InstanceSettingsFrame : DashboardFrame
    {
        private bool Initializing = true;

        private int ResolutionIndex(List<string> Resolutions, out bool Error)
        {
            int Index = Resolutions.Count - 1;

            Error = false;

            bool Found = false;

            string Resolution = ServiceSingleton.Instances.WorkingInstance.Settings.Width + "x" + ServiceSingleton.Instances.WorkingInstance.Settings.Height;

            if (ServiceSingleton.Instances.WorkingInstance.Settings.Height != string.Empty && ServiceSingleton.Instances.WorkingInstance.Settings.Width != string.Empty)
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

            if (ServiceSingleton.Instances.WorkingInstance.Performance.DownWidth != string.Empty && ServiceSingleton.Instances.WorkingInstance.Performance.DownHeight != string.Empty)
            {

                string Resolution = ServiceSingleton.Instances.WorkingInstance.Performance.DownWidth + "x" + ServiceSingleton.Instances.WorkingInstance.Performance.DownHeight;

                if (ServiceSingleton.Instances.WorkingInstance.Performance.DownHeight != string.Empty && ServiceSingleton.Instances.WorkingInstance.Performance.DownWidth != string.Empty)
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
            var Index = Locations.FindIndex(x => x == ServiceSingleton.Instances.WorkingInstance.Settings.CDN);

            return Index == -1 ? 0 : Index;            
        }

        private int AntiAliasingIndex(List<string> AntiAliasing)
        {
            var Index = AntiAliasing.FindIndex(x => x == ServiceSingleton.Instances.WorkingInstance.Performance.AntiAliasing);
            return Index == -1 ? 0 : Index;            
        }

        private void EnableFlatButton(Button Button, Boolean Enabled)
        {
            if (Button == null)
                return;
            Button.IsEnabled = Enabled;
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

                LblHeader.Text = "Settings for " + string.Format("{0} - {1} v{2}{3}", Instance.Name, Instance.Performance.Variant, Instance.Version, Instance.Tag != string.Empty ? string.Format(" - ({0})", Instance.Tag) : string.Empty);
                List<string> Resolutions = ServiceSingleton.Globals.WindowsResolutions;
                DrpDwnLstScreenRes.ItemsSource = Resolutions;
                DrpDwnLstScreenRes.SelectedIndex = ResolutionIndex(Resolutions, out ResError);

                if (ResError)
                {
                    ApplyResolution();
                }

                LblRatio.Text = ServiceSingleton.Instances.WorkingInstance.Settings.Ratio;

                if (ResError)
                {
                    ApplyDownscalingResolution();
                }

                TxtBxInstancePath.Text = Instance.InstallDir;
                TxtBxArchivePath.Text = Instance.ArchiveDir;

                TglBtnEnableArchive.IsChecked = false;

                if (Instance.Settings.EnableArchiving)
                {
                    TglBtnDownScale.IsChecked = true;
                }

                TglBtnDownScale.IsChecked = false;

                if (Instance.Performance.DownScaling == "TRUE")
                {
                    TglBtnDownScale.IsChecked = true;
                    DrpDwnLstDownRes.IsEnabled = TglBtnDownScale.IsChecked == true;
                }

                DrpDwnLstDownLoc.ItemsSource = CDN.Get();
                DrpDwnLstDownLoc.SelectedIndex = DownloadLocationIndex(CDN.Get());

                EnableFlatButton(BtnApplyRes, false);
                EnableFlatButton(BtnApplyDownScaling, false);

                LblVariant.Text = Instance.Performance.Variant;
                LblAntiAliasing.Text = Instance.Performance.AntiAliasing;

                if (Instance.Performance.Variant == "Redux")
                {
                    LblLODs.Text = "Redux";
                }
                else
                {
                    LblLODs.Text = Instance.Performance.LODs;
                }

                LblSREX.Text = Instance.Performance.SREX == "TRUE" ? "Yes" : "No";
                LblFrameGen.Text = Instance.Performance.FrameGeneration == "TRUE" ? "Yes" : "No";
                LblCombatAnims.Text = Instance.Options.CombatAnimation;

                LblLeveling.Text = Instance.Options.AlternateLeveling == "TRUE" ? "Yes" : "No";
                LblNude.Text = Instance.Options.Nudity == "TRUE" ? "Yes" : "No";
                LblGore.Text = Instance.Options.Gore == "TRUE" ? "Yes" : "No";

                LblENB.Text = ENBs.GetENBByCode(Instance.Options.AlternateENB);

                LblCombatScaling.Text = Instance.Options.CombatScaling;
                LblExhaustion.Text = Instance.Options.Exhaustion == "TRUE" ? "Yes" : "No";
                LblNerfPA.Text = Instance.Options.NerfPA;
                LblBoss.Text = Instance.Options.Boss == "TRUE" ? "Yes" : "No";
                LblController.Text = Instance.Options.Controller == "TRUE" ? "Yes" : "No";
                LblUI.Text = Instance.Options.UI;

                List<string> IniSettings = new List<string>();

                IniSettings.Add("Low");
                IniSettings.Add("Medium");
                IniSettings.Add("High");

                DrpDwnLstIni.ItemsSource = IniSettings;

                DrpDwnLstIni.SelectedIndex = System.Convert.ToInt16(Instance.Performance.IniSettings);

                GrpBxDownscaling.IsEnabled = Instance.Performance.AntiAliasing != "DLAA" && Instance.Performance.AntiAliasing != "FSR";

                Initializing = false;   

            }
            catch (Exception ex)
            {
                await ServiceSingleton.Dashboard.Error("Error during instance settings loading", ex.Message);
            }
        }

        private void OnArchiveChanged(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Instances.WorkingInstance.Settings.EnableArchiving = TglBtnEnableArchive.IsChecked == true;
            ServiceSingleton.Instances.Save();
        }

        private void OnDownScaleChanged(object? sender, RoutedEventArgs e)
        {
            if (!Initializing)
            {
                DrpDwnLstDownRes.IsEnabled = TglBtnDownScale.IsChecked == true;
                EnableFlatButton(BtnApplyDownScaling, true);
            }
        }
        
        private void OnScreenResolutionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!Initializing)
            {
                EnableFlatButton(BtnApplyRes, true);
            }
        }

        private void OnDownScaleResolutionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!Initializing)
            {
                EnableFlatButton(BtnApplyDownScaling, true);
            }
        }

        private void OnDownloadLocationChanged(object? sender, SelectionChangedEventArgs e)
        {
            ServiceSingleton.Instances.WorkingInstance.Settings.CDN = DrpDwnLstDownLoc.SelectedItem.ToString(); //might crash

            ServiceSingleton.Instances.Save();
        }

        private async void OnIniChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!Initializing)
            {
                try
                {
                    INolvusInstance Instance = ServiceSingleton.Instances.WorkingInstance;

                    Instance.Performance.IniSettings = DrpDwnLstIni.SelectedIndex.ToString();

                    File.WriteAllText(Path.Combine(Instance.InstallDir, "MODS", "profiles", Instance.Name, "Skyrim.ini"), ModOrganizer.GetIni(false, (IniLevel)System.Convert.ToInt16(Instance.Performance.IniSettings), Instance));
                    File.WriteAllText(Path.Combine(Instance.InstallDir, "MODS", "profiles", Instance.Name, "SkyrimPrefs.ini"), ModOrganizer.GetIni(true, (IniLevel)System.Convert.ToInt16(Instance.Performance.IniSettings), Instance));

                    ServiceSingleton.Instances.Save();
                }
                catch(Exception ex)
                {                    
                    await ServiceSingleton.Dashboard.Error("Error during skyrim ini file configuration saving", ex.Message);
                }
            }
        }

        private void BtnBack_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Dashboard.LoadFrame<InstanceDetailFrame>();
        }

        private void ApplyResolution()
        {
            try
            {
                INolvusInstance Instance = ServiceSingleton.Instances.WorkingInstance;

                string Resolution = DrpDwnLstScreenRes.SelectedValue.ToString();

                string[] Reso = Resolution.Split(new char[] { 'x' });

                Instance.Settings.Width = Reso[0];
                Instance.Settings.Height = Reso[1];

                ServiceSingleton.Instances.Save();

                string FileName = Path.Combine(Instance.InstallDir, "MODS", "profiles", Instance.Name, "SkyrimPrefs.ini");

                ServiceSingleton.Settings.StoreIniValue(FileName, "Display", "iSize W", Instance.Settings.Width);
                ServiceSingleton.Settings.StoreIniValue(FileName, "Display", "iSize H", Instance.Settings.Height);

                EnableFlatButton(BtnApplyRes, false);
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
                    Instance.Performance.DownScaling = "TRUE";
                }
                else
                {
                    Instance.Performance.DownScaling = "FALSE";
                }

                Instance.Performance.DownWidth = Reso[0];
                Instance.Performance.DownHeight = Reso[1];

                ServiceSingleton.Instances.Save();

                #region Apply Settings

                string SettingsFile = Path.Combine(Instance.InstallDir, "MODS", "mods", "SSE Display Tweaks", "SKSE", "Plugins", "SSEDisplayTweaks.ini"); //Case sensitivity strikes again

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
                            if (Instance.Performance.DownScaling == "TRUE")
                            {
                                _Line = "Resolution" + "=" + Instance.Performance.DownScaledResolution;
                            }
                            else
                            {
                                _Line = "#Resolution" + "=" + Instance.Performance.DownScaledResolution;
                            }

                            FoundResolution = true;
                        }
                        else if (Line.Contains("BorderlessUpscale") && Line.Substring(0, 1) != "# " && !FoundUpscale)
                        {
                            if (Instance.Performance.DownScaling == "TRUE")
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

                EnableFlatButton(BtnApplyDownScaling, false);
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

    }
}