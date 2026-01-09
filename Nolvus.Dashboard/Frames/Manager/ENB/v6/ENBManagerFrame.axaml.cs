using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Nolvus.Components.Controls;
using Nolvus.Core.Enums;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
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
            var Preset = ENBListBox.SelectedItem as IENBPreset;

            if (Preset.GetFieldValueByKey("EnbCode") == ServiceSingleton.Instances.WorkingInstance.Options.AlternateENB)
            {
                NolvusMessageBox.Show(window, "Error", "This ENB is already installed!", MessageBoxType.Error);
            }
            else
            {
                bool? result = await NolvusMessageBox.ShowConfirmation(window, "Confirmation", string.Format("You are about to change ENB from {0} to {1}. Are you sure you want to continue?",
                    ServiceSingleton.EnbManager.CurrentPreset(ServiceSingleton.Instances.WorkingInstance.Options.AlternateENB), Preset));
                
                if (result == true)
                {
                    try
                    {
                        try
                        {
                            BtnCancel.IsEnabled = false;
                            BtnInstall.IsEnabled = false;

                            var ModsToUpdate = await ServiceSingleton.EnbManager.PrepareModsToUpdate(ServiceSingleton.Instances.WorkingInstance.Options.AlternateENB, Preset.GetFieldValueByKey("EnbCode"));

                            ServiceSingleton.Instances.WorkingInstance.Status.AddField("OldENB", ServiceSingleton.Instances.WorkingInstance.Options.AlternateENB);
                            ServiceSingleton.Instances.WorkingInstance.Status.AddField("NewENB", Preset.GetFieldValueByKey("EnbCode"));

                            ServiceSingleton.Instances.WorkingInstance.Options.AlternateENB = Preset.GetFieldValueByKey("EnbCode");

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
                        ServiceSingleton.Dashboard.NoStatus();
                        ServiceSingleton.Dashboard.ProgressCompleted();
                    }
                }
            }
        }
    }
}