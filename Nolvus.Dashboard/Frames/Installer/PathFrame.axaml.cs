using Avalonia.Interactivity;
using Avalonia.Controls;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Avalonia.Platform.Storage;
using Nolvus.Dashboard.Controls;
using Nolvus.Core.Enums;
using Nolvus.Core.Misc;

namespace Nolvus.Dashboard.Frames.Installer
{
    public partial class PathFrame : DashboardFrame
    {
        public PathFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();

            BtnBrowseInstance.Click += BtnBrowseInstance_Click;
            BtnBrowseArchive.Click += BtnBrowseArchive_Click;

            BtnPrevious.Click += BtnPrevious_Click;
            BtnContinue.Click += BtnContinue_Click;

            TglArchive.IsCheckedChanged += ToggleArchiving;
        }

        protected override async Task OnLoadedAsync()
        {
            try
            {
                ServiceSingleton.Dashboard.Info("Instance Paths");

                var Instance = ServiceSingleton.Instances.WorkingInstance;

                TxtInstanceDir.Text = Instance.InstallDir ?? "";
                TxtArchiveDir.Text = Instance.ArchiveDir ?? "";

                TglArchive.IsChecked = Instance.Settings.EnableArchiving;
            }
            catch (Exception ex)
            {
                await ServiceSingleton.Dashboard.Error("PathFrame Load Error", ex.Message, ex.StackTrace);
            }
        }

        private void ToggleArchiving(object? sender, RoutedEventArgs e)
        {
            var Instance = ServiceSingleton.Instances.WorkingInstance;
            Instance.Settings.EnableArchiving = TglArchive.IsChecked == true;
        }

        private async void BtnBrowseInstance_Click(object? sender, RoutedEventArgs e)
        {

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner == null)
                return;

            var result = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions {
                Title = "Select Instance Installation Directory",
                AllowMultiple = false
            });
            if (result.Count == 0)
                return; //user cancelled dialog

            var folder = result[0];
            var folderPath = folder.Path.LocalPath;

            if (ServiceSingleton.Files.IsDirectoryEmpty(folderPath))
            {
                TxtInstanceDir.Text = folderPath;
                var Instance = ServiceSingleton.Instances.WorkingInstance;

                Instance.InstallDir = TxtInstanceDir.Text;
                Instance.StockGame = Path.Combine(Instance.InstallDir, "STOCK GAME");
            }
            else
            {
                NolvusMessageBox.Show(owner, "Invalid Installation Directory", "The specified directory is not empty. Please select another directory.", MessageBoxType.Error);
            }
        }

        private async void BtnBrowseArchive_Click(object? sender, RoutedEventArgs e)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner == null)
                return;

            var result = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions {
                Title = "Select Archive Directory",
                AllowMultiple = false
            });

            if (result.Count == 0)
                return; //user cancelled dialog

            var folder = result[0];
            var folderPath = folder.Path.LocalPath;

            TxtArchiveDir.Text = folderPath;
            ServiceSingleton.Instances.WorkingInstance.ArchiveDir = folderPath;
        }

        private async void BtnPrevious_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Dashboard.LoadFrameAsync<SelectInstanceFrame>();
        }

        private async void BtnContinue_Click(object? sender, RoutedEventArgs e)
        {
            switch (ServiceSingleton.Instances.WorkingInstance.Name)
            {
                case Strings.NolvusAscension:
                    //ServiceSingleton.Dashboard.LoadFrameAsync<v5.PerformanceFrame>();
                    break;
                case Strings.NolvusAwakening:
                    //ServiceSingleton.Dashboard.LoadFrameAsync<v6.PerformanceFrame>();
                    break;
            }
            ServiceSingleton.Logger.Log("Performance Frame Unimplemented Exception");
        }
    }
}