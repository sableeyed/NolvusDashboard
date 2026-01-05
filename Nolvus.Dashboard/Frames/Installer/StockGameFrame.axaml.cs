using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Nolvus.Core.Events;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.StockGame.Core;
using Nolvus.Core.Enums;
using Vcc.Nolvus.Api.Installer.Library;
using Vcc.Nolvus.Api.Installer.Services;
using Avalonia.Threading;

namespace Nolvus.Dashboard.Frames.Installer
{
    public partial class StockGameFrame : DashboardFrame
    {
        string AEMSG1 = "You need to buy Skyrim Anniversary Edition or if you already have Skyrim Special Edition, buy the Anniversary Upgrade";
        string AEMSG2 = "If you already have the Anniversary Edition, be sure you ran the game once from steam and when prompted download all content then close Skyrim";
        string AEMSG3 = "More info here ==> https://www.nolvus.net/appendix/installer/skyrim_setup";

        // Recommended: bind the ListBox to a collection instead of mutating LstBxOutput.Items directly
        private readonly ObservableCollection<string> _output = new();

        public StockGameFrame(IDashboard Dashboard, FrameParameters Params) : base(Dashboard, Params)
        {
            InitializeComponent();
            LstBxOutput.ItemsSource = _output;
        }

        protected override async Task OnLoadedAsync()
        {
            ServiceSingleton.Dashboard.Title("Nolvus Dashboard - [Stock Game Installation]");
            ServiceSingleton.Dashboard.Info("Stock Game Installation");

            IFolderService Folders = ServiceSingleton.Folders;
            INolvusInstance Instance = ServiceSingleton.Instances.WorkingInstance;

            var StockGameManager = new StockGameManager(
                Folders.DownloadDirectory,
                Folders.LibDirectory,
                Folders.PatchDirectory,
                Folders.GameDirectory,
                Instance,
                await ApiManager.Service.Installer.GetGamePackage(Instance.Version),
                true);

            StockGameManager.OnDownload += StockGameManager_OnDownload;
            StockGameManager.OnExtract += StockGameManager_OnExtract;
            StockGameManager.OnItemProcessed += StockGameManager_OnItemProcessed;
            StockGameManager.OnStepProcessed += StockGameManager_OnStepProcessed;

            try
            {
                await StockGameManager.Load();
                ServiceSingleton.Dashboard.Info("Stock Game Installation 1/4 (25%)");
                await StockGameManager.CheckIntegrity();
                ServiceSingleton.Dashboard.Info("Stock Game Installation 2/4 (50%)");
                await StockGameManager.CopyGameFiles();
                ServiceSingleton.Dashboard.Info("Stock Game Installation 3/4 (75%)");
                await StockGameManager.PatchGameFiles();
                ServiceSingleton.Dashboard.Info("Stock Game Installation 4/4 (100%)");

                ServiceSingleton.Dashboard.ProgressCompleted();
                ServiceSingleton.Instances.PrepareInstanceForInstall();

                await ServiceSingleton.Dashboard.LoadFrameAsync<InstallFrame>();
            }
            catch (Exception ex)
            {
                await RollBack();

                if (ex is GameFileMissingException)
                {
                    await ServiceSingleton.Dashboard.Error(
                        "Error during game file checking",
                        "Skyrim Anniversary Edition is not installed",
                        AEMSG1 + Environment.NewLine + AEMSG2 + Environment.NewLine + AEMSG3 + Environment.NewLine + "Original error : " + ex.Message);
                }
                else if (ex is GameFileIntegrityException)
                {
                    await ServiceSingleton.Dashboard.Error("Error during game integrity checking", ex.Message);
                }
                else if (ex is GameFilePatchingException)
                {
                    await ServiceSingleton.Dashboard.Error("Error during game files patching", ex.Message);
                }
                else
                {
                    await ServiceSingleton.Dashboard.Error("Error during stock game installation", ex.Message, ex.StackTrace);
                }
            }
        }

        public Task AddItemToList(string Item)
        {
            ServiceSingleton.Logger.Log(Item);

            return Dispatcher.UIThread.InvokeAsync(() =>
            {
                _output.Add(Item);

                if (_output.Count > 0)
                    LstBxOutput.ScrollIntoView(_output.Count - 1);
            }).GetTask();
        }

        private void StockGameManager_OnDownload(object? sender, DownloadProgress e)
        {
            ServiceSingleton.Dashboard.Status("Downloading file (" + e.ProgressPercentage + "%)...");
            ServiceSingleton.Dashboard.Progress(e.ProgressPercentage);
        }

        private void StockGameManager_OnExtract(object? sender, ExtractProgress e)
        {
            ServiceSingleton.Dashboard.Status("Extracting game meta (" + e.ProgressPercentage + "%)...");
            ServiceSingleton.Dashboard.Progress(e.ProgressPercentage);
        }

        private void StockGameManager_OnItemProcessed(object? sender, ItemProcessedEventArgs e)
        {
            double Percent = ((double)e.Value / (double)e.Total) * 100;

            Percent = Math.Round(Percent, 0);

            switch (e.Step)
            {
                case StockGameProcessStep.GameFileInfoLoading:
                    ServiceSingleton.Dashboard.Status(string.Format("Loading game files info for {0}...", e.ItemName));
                    ServiceSingleton.Dashboard.AdditionalInfo(string.Format("Loading game files info {0}", Percent));
                    ServiceSingleton.Dashboard.Progress(System.Convert.ToInt16(Percent));
                    break;
                case StockGameProcessStep.PatchingInfoLoading:
                    ServiceSingleton.Dashboard.Status(string.Format("Loading patching info for {0}...", e.ItemName));
                    ServiceSingleton.Dashboard.AdditionalInfo(string.Format("Loading patching info ({0}%)", Percent));
                    ServiceSingleton.Dashboard.Progress(System.Convert.ToInt16(Percent));
                    break;
                case StockGameProcessStep.GameFilesChecking:
                    ServiceSingleton.Dashboard.Status(string.Format("Checking game file {0}...", e.ItemName));
                    ServiceSingleton.Dashboard.AdditionalInfo(string.Format("Game files checking ({0}%)", Percent));
                    ServiceSingleton.Dashboard.Progress(System.Convert.ToInt16(Percent));
                    break;
                case StockGameProcessStep.GameFilesCopy:
                    ServiceSingleton.Dashboard.Status(string.Format("Copying game file {0}...", e.ItemName));
                    ServiceSingleton.Dashboard.AdditionalInfo(string.Format("Copying game files ({0}%)", Percent));
                    ServiceSingleton.Dashboard.Progress(System.Convert.ToInt16(Percent));
                    break;
                case StockGameProcessStep.GameFilesPatching:
                    ServiceSingleton.Dashboard.Status("Awaiting game file to patch...");
                    ServiceSingleton.Dashboard.AdditionalInfo(string.Format("Patching game files ({0}%)", Percent));
                    break;
                case StockGameProcessStep.PatchGameFile:
                    ServiceSingleton.Dashboard.Status(string.Format("Patching game files {0}...", e.ItemName));
                    ServiceSingleton.Dashboard.Progress(System.Convert.ToInt16(Percent));
                    break;
                case StockGameProcessStep.CheckPatchedGameFile:
                    ServiceSingleton.Dashboard.Status(string.Format("Checking patched game files {0}...", e.ItemName));
                    ServiceSingleton.Dashboard.Progress(System.Convert.ToInt16(Percent));
                    break;
            }
        }

        private void StockGameManager_OnStepProcessed(object? sender, StepProcessedEventArgs e)
        {
            _ = AddItemToList(e.Step);
        }

        private async Task RollBack()
        {
            await AddItemToList("Error detected, rolling back changes...");
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ServiceSingleton.Dashboard.Status("Error detected, rolling back changes, please wait...");
            });

            try
            {
                await Task.Run(() =>
                {
                    ServiceSingleton.Files.RemoveDirectory(ServiceSingleton.Instances.WorkingInstance.InstallDir, true);
                });

                await AddItemToList("Rollback complete.");
            }
            catch (Exception ex)
            {
                await AddItemToList("Rollback failed: " + ex.Message);
                ServiceSingleton.Logger.Log(ex.ToString());
            }
            finally
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ServiceSingleton.Dashboard.NoStatus();
                    ServiceSingleton.Dashboard.AdditionalInfo(string.Empty);
                    ServiceSingleton.Dashboard.ProgressCompleted();
                });
            }
        }
    }
}
