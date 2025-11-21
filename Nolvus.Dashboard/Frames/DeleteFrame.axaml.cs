using Nolvus.Core.Interfaces;
using Nolvus.Core.Frames;
using Nolvus.Core.Services;
using Nolvus.Core.Enums;
using Nolvus.Package.Mods;
using Nolvus.Dashboard.Controls;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Nolvus.Dashboard.Frames
{
    public partial class DeleteFrame : DashboardFrame
    {
        public DeleteFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
        }

        protected override async Task OnLoadedAsync()
        {
            if (Action == InstanceAction.Delete)
            {
                LblStepText.Text = "Delete Nolvus Instance";
                BtnAction.Content = "Delete";
                LblInfo.Text = "Click on the Delete button to delete your instance. WARNING, this will not be reversible.";

                ServiceSingleton.Dashboard.Title("Nolvus Dashboard - [Delete Instance]");
                ServiceSingleton.Dashboard.Info("Delete your Nolvus instance");                

                LblInstanceInfo.Text = "Instance to delete";
            }            
            else if (Action == InstanceAction.Cancel)
            {
                LblStepText.Text = "Cancel Nolvus Instance installation";
                BtnAction.Content = "Cancel";
                LblInfo.Text = "Click on the Cancel button to cancel your current installation. WARNING, this will not be reversible.";

                ServiceSingleton.Dashboard.Title("Nolvus Dashboard - [Cancel Installation]");
                ServiceSingleton.Dashboard.Info("Cancel your current Nolvus installation");                

                LblInstanceInfo.Text = "Instance to cancel";
            }

            string Version = Instance.Version;

            if (Version == string.Empty)
            {
                Version = Instance.Version;
            }

            LblInstance.Text = this.Instance.Name + " v" + Version;
        }

        public INolvusInstance Instance
        {
            get { return Parameters["Instance"] as INolvusInstance; }
        }

        public InstanceAction Action
        {
            get { return (InstanceAction)Parameters["Action"]; }
        }

        public async void UpdateProgress(int Value)
        {
            await Dispatcher.UIThread.InvokeAsync(() => {
                LblDeleteInfo.Text = "Deleting Instance ( " + Value.ToString() + "%)";
            });
        }

        private async Task DeleteInstance(List<FileInfo> Files)
        {
            LblDeleteInfo.IsVisible = true;
            BtnAction.IsEnabled = false;
            BtnBack.IsEnabled = false;

            int total = Files.Count;

            await Task.Run(() =>
            {
                int counter = 0;

                foreach (var f in Files)
                {
                    try
                    {
                        f.Attributes = FileAttributes.Normal;
                        f.Delete();
                    }
                    catch (Exception ex)
                    {
                        ServiceSingleton.Logger.Log($"Delete failed for {f.FullName}: {ex.Message}");
                    }

                    int percent = (int)(((double)++counter / total) * 100);

                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        UpdateProgress(percent);
                        ServiceSingleton.Dashboard.Progress(percent);
                    });
                }

                try
                {
                    ServiceSingleton.Files.RemoveDirectory(Instance.InstallDir, false);
                }
                catch (Exception ex)
                {
                    ServiceSingleton.Logger.Log($"RemoveDirectory failed: {ex.Message}");
                }
            });
        }


        private async void DoAction()
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            string Message = string.Empty;

            if (Action == InstanceAction.Delete)
            {
                Message = string.Format("Are you sure you want to delete {0} and everything inside {1}?", Instance.Name, Instance.InstallDir);
            }            
            else
            {
                Message = string.Format("Are you sure you want to cancel {0} installation?", Instance.Name);
            }
            bool? result = await NolvusMessageBox.ShowConfirmation(owner, "Confirmation", Message);
            if (result == true)
            {
                try
                {
                    try
                    {                        
                        await DeleteInstance(ServiceSingleton.Files.GetFiles(Instance.InstallDir));

                        ServiceSingleton.Instances.RemoveInstance(Instance);

                        await ServiceSingleton.Dashboard.LoadFrameAsync<StartFrame>();
                    }
                    finally
                    {
                        ServiceSingleton.Dashboard.NoStatus();
                        ServiceSingleton.Dashboard.ProgressCompleted();
                    }
                }
                catch(Exception ex)
                {
                    await ServiceSingleton.Dashboard.Error("Error during deleting instance", ex.Message);
                }
            }
        }

        private void BtnAction_Click(object? sender, RoutedEventArgs e)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            if (!ModOrganizer.IsRunning)
            {
                DoAction();
            }
            else
            {
                NolvusMessageBox.Show(owner, "Mod Organizer 2", "An instance of Mod Organizer 2 is running! Please close it first.", MessageBoxType.Error);
            }
        }

        private async void BtnBack_Click(object? sender, RoutedEventArgs e)
        {
            await ServiceSingleton.Dashboard.LoadFrameAsync<StartFrame>();
        }

    }
}