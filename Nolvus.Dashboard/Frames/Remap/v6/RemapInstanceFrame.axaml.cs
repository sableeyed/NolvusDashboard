using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Nolvus.Core.Enums;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Dashboard.Frames.Instance;
using Nolvus.Dashboard.Controls;

namespace Nolvus.Dashboard.Frames.Remap.v6
{
    public partial class RemapInstanceFrame : DashboardFrame
    {
        public RemapInstanceFrame(IDashboard Dashboard, FrameParameters Params):base(Dashboard, Params)
        {
            InitializeComponent();

            ServiceSingleton.Dashboard.Title("Nolvus Dashboard - [Remap Instance]");
            ServiceSingleton.Dashboard.Info("Choose a new path to move your instance to");

            BtnCancel.Click += BtnCancel_Click;
            BtnRemap.Click += BtnRemap_Click;
            BtnBrowseInstancePath.Click += BtnBrowseInstancePath_Click;
        }

        protected override async Task OnLoadedAsync()
        {
            LblInstance.Text  = string.Format("{0} - {1} v{2}{3}", ServiceSingleton.Instances.WorkingInstance.Name, ServiceSingleton.Instances.WorkingInstance.Performance.Variant, ServiceSingleton.Instances.WorkingInstance.Version, ServiceSingleton.Instances.WorkingInstance.Tag != string.Empty ? string.Format(" - ({0})", ServiceSingleton.Instances.WorkingInstance.Tag) : string.Empty);
            LblCurrentInstallPath.Text = ServiceSingleton.Instances.WorkingInstance.InstallDir;
        }

        private void CheckMO2Executables()
        {
            //STUB - The only thing that should need updated is the path in X:. Support picking their own prefix path coming Soon(TM) because we now have instance tagging
        }

        private void ModifyMO2Executables(string NewInstallPath, string StockGamePath)
        {
            //STUB
        }

        private async Task Remap()
        {
            CheckMO2Executables();

            var CurrentInstallPath = ServiceSingleton.Instances.WorkingInstance.InstallDir;
            var NewInstallPath = TxtBxInstancePath.Text;
            var StockGamePath = Path.Combine(NewInstallPath, "STOCK GAME");
            
            await Task.Run(() =>
            {
                var Files = ServiceSingleton.Files.GetFiles(CurrentInstallPath);

                Directory.CreateDirectory(Path.Combine(NewInstallPath, "MODS", "mods", "0. MASTER FILES_separator"));

                int Counter = 0;

                foreach (var File in Files)
                {
                    var FileCurrentFullName = File.DirectoryName!;
                    var FileCurrentDirectoryToCreate = Path.GetRelativePath(CurrentInstallPath, FileCurrentFullName);

                    var DestPath = Path.Combine(NewInstallPath, FileCurrentDirectoryToCreate, File.Name);

                    var Info = new FileInfo(File.FullName);

                    if (Info.LinkTarget != null)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(DestPath)!);

                        if (System.IO.File.Exists(DestPath))
                        {
                            System.IO.File.Delete(DestPath);
                        }

                        System.IO.File.CreateSymbolicLink(DestPath, Info.LinkTarget);
                        System.IO.File.Delete(Info.FullName);
                    }
                    else
                    {
                        ServiceSingleton.Files.MoveFile(File.FullName, DestPath);
                    }

                    int PercentDone = System.Convert.ToInt16(((double)++Counter / Files.Count) * 100);

                    Dispatcher.UIThread.Post(() =>
                    {
                        ServiceSingleton.Dashboard.Progress(PercentDone);
                        ServiceSingleton.Dashboard.Status(string.Format("Moving file {0}", File.Name));
                        ServiceSingleton.Dashboard.AdditionalInfo(string.Format("Moving instance ({0}%)", PercentDone)); 
                    });
                }

                ModifyMO2Executables(NewInstallPath, StockGamePath);

                ServiceSingleton.Instances.WorkingInstance.InstallDir = NewInstallPath;
                ServiceSingleton.Instances.WorkingInstance.StockGame = StockGamePath;

                ServiceSingleton.Instances.Save();

                ServiceSingleton.Files.RemoveDirectory(CurrentInstallPath, true);
            });
        }

        private async void BtnBrowseInstancePath_Click(object? sender, RoutedEventArgs e)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner == null)
                return;

            var result = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions {
                Title = "Select Instance Installation Directory",
                AllowMultiple = false
            });

            if (result.Count == 0)
                return;

            var folder = result[0];
            var folderPath = folder.Path.LocalPath;

            if (ServiceSingleton.Files.IsDirectoryEmpty(folderPath))
            {
                TxtBxInstancePath.Text = folderPath;
            }
            else
            {
                await NolvusMessageBox.Show(owner, "Invalid Installation Directory", "The specified directory is not empty. Please select another directory.", MessageBoxType.Error);
            }
        }

        private void BtnCancel_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Instances.UnloadWorkingIntance();
            ServiceSingleton.Dashboard.LoadFrame<InstancesFrame>();
        }

        private async void BtnRemap_Click(object? sender, RoutedEventArgs e)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            if (TxtBxInstancePath.Text.Trim() != string.Empty)
            {
                try
                {
                    try
                    {
                        DisableButtons();
                        ServiceSingleton.Dashboard.DisableSettings();

                        await Remap();

                        ServiceSingleton.Instances.UnloadWorkingIntance();
                        ServiceSingleton.Dashboard.LoadFrame<InstancesFrame>();
                    }
                    finally
                    {
                        ServiceSingleton.Dashboard.EnableSettings();
                        ServiceSingleton.Dashboard.NoStatus();
                        ServiceSingleton.Dashboard.ProgressCompleted();
                        ServiceSingleton.Dashboard.ClearInfo();
                    }
                }
                catch (Exception ex)
                {
                    EnableButtons();

                    await NolvusMessageBox.Show(owner, "Error", string.Format("Error occured with message : {0}", ex.Message), MessageBoxType.Error);
                }
            }
        }
    }
}