using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Nolvus.Components.Controls;
using Nolvus.Core.Enums;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Package.Mods;
using Nolvus.Dashboard.Controls;
using Nolvus.Dashboard.Frames.Installer;
using Nolvus.Dashboard.Frames.Instance;

namespace Nolvus.Dashboard.Frames.Manager.ENB.v6
{
    public partial class ENBManagerFrame : DashboardFrame
    {
        public ENBManagerFrame(IDashboard Dashboard, FrameParameters Params) : base(Dashboard, Params)
        {
            InitializeComponent();

            ServiceSingleton.Dashboard.Title("Nolvus Dashboard - [ENB Manager]");
            ServiceSingleton.Dashboard.Info("Select an ENB to install");
        }

        private async Task SetDataSource(IEnumerable<IENBPreset> Source)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ENBListBox.Items = Source;
                ENBListBox.SelectedIndex = 0;
                LoadingOverlay.IsVisible = false;
                ENBListBox.IsVisible = true;

            });
        }

        protected override async Task OnLoadedAsync()
        {
            await SetDataSource(await ServiceSingleton.EnbManager.GetEnbPresets());
            ServiceSingleton.Dashboard.NoStatus();
            ServiceSingleton.Dashboard.ProgressCompleted();
        }

        private void BtnCancel_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Instances.UnloadWorkingIntance();
            ServiceSingleton.Dashboard.LoadFrame<InstancesFrame>();
        }

        private async void BtnInstall_Click(object? sender, RoutedEventArgs e)
        {
            var window = TopLevel.GetTopLevel(this) as Window;

            if (!ModOrganizer.IsRunning)
            {
                var Preset = ENBListBox.SelectedItem as IENBPreset;

                if (Preset.GetFieldValueByKey("EnbCode") == ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("AlternateENB"))
                {
                    NolvusMessageBox.Show(window, "Error", "This ENB is already installed!", MessageBoxType.Error);
                }
                else
                {
                    bool? result = await NolvusMessageBox.ShowConfirmation(window, "Confirmation", string.Format("You are about to change ENB from {0} to {1}.\n\nWarning!!!\n\nIf you customized the list, all added mods will be placed at the end of the loadorder and left unchecked.\n\nAre you sure you want to continue?",
                        ServiceSingleton.EnbManager.CurrentPreset(ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("AlternateENB")), Preset));

                    if (result == true)
                    {
                        try
                        {
                            try
                            {
                                DisableButtons();

                                ServiceSingleton.Dashboard.DisableSettings();

                                var ModsToUpdate = await ServiceSingleton.EnbManager.PrepareModsToUpdate(ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("AlternateENB"), Preset.GetFieldValueByKey("EnbCode"));

                                ServiceSingleton.Instances.WorkingInstance.StatusManager.AddField("OldENB", ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("AlternateENB"));
                                ServiceSingleton.Instances.WorkingInstance.StatusManager.AddField("NewENB", Preset.GetFieldValueByKey("EnbCode"));

                                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("AlternateENB").Value = Preset.GetFieldValueByKey("EnbCode");

                                await ServiceSingleton.EnbManager.DeleteENB((s, p) =>
                                {
                                    ServiceSingleton.Dashboard.Status(string.Format("{0} ({1}%)", s, p));
                                    ServiceSingleton.Dashboard.Progress(p);
                                });

                                ServiceSingleton.Instances.PrepareInstanceForEnb();

                                await ServiceSingleton.Dashboard.LoadFrameAsync<InstallFrame>(new FrameParameters(new FrameParameter() 
                                    { Key = "ModsToInstall", Value = ModsToUpdate.Cast<IInstallableElement>().ToList() }));
                            }
                            catch (Exception ex)
                            {
                                await ServiceSingleton.Dashboard.Error("Error during ENB update initialization", ex.Message, ex.StackTrace);
                            }
                        }
                        finally
                        {
                            ServiceSingleton.Dashboard.EnableSettings();
                            ServiceSingleton.Dashboard.NoStatus();
                            ServiceSingleton.Dashboard.ProgressCompleted();
                        }
                    }
                }
            }
            else
            {
                NolvusMessageBox.Show(window, "Mod Organizer 2", "An instance of Mod Organizer 2 is running! Please close it first.", MessageBoxType.Error);
            }
        }
    }
}