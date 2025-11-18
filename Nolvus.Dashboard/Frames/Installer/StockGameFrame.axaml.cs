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
        string AEMSG3 = "If it still does not work, do an integrity check in Steam";
        string AEMSG4 = "More info here ==> https://www.nolvus.net/appendix/installer/skyrim_setup";  
        public StockGameFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
        }

        protected override async Task OnLoadedAsync()
        {
            ServiceSingleton.Dashboard.Title("Nolvus Dashboard - [Stock Game Installation]");
            ServiceSingleton.Dashboard.Info("Stock Game Installation");

            IFolderService Folders = ServiceSingleton.Folders;
            INolvusInstance Instance = ServiceSingleton.Instances.WorkingInstance;

            //LstBxOutput.Height = (int)Math.Round(Last)
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
                // await StockGameManager.CopyGameFiles();
                // ServiceSingleton.Dashboard.Info("Stock Game Installation 3/4 (75%)");
                // await StockGameManager.PatchGameFiles();
                // ServiceSingleton.Dashboard.Info("Stock Game Installation 4/4 (100%)");

                // ServiceSingleton.Dashboard.ProgressCompleted();
                // ServiceSingleton.Instances.PrepareInstanceForInstall();

                Console.WriteLine("InstallFrame not implemented - but STOCK GAME should be finsihed");
                //await ServiceSingleton.Dashboard.LoadFrameAsync<InstallFrame>();
            }
            catch (Exception ex)
            {
                RollBack();
                    
                if (ex is GameFileMissingException)
                {
                    await ServiceSingleton.Dashboard.Error("Error during game file checking", "Skyrim Anniversary Edition is not installed", AEMSG1 + Environment.NewLine + AEMSG2 + Environment.NewLine + AEMSG3 + Environment.NewLine + AEMSG4 + Environment.NewLine + "Original error : " + ex.Message);
                }
                else if (ex is GameFileIntegrityException)
                {
                    await ServiceSingleton.Dashboard.Error("Error during game integrity checking", ex.Message, "Possible fix is to do an integrity check for Skyrim in Steam");
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

        public async void AddItemToList(string Item)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                LstBxOutput.Items.Add(Item);

                if (LstBxOutput.ItemCount > 0)
                {
                    LstBxOutput.ScrollIntoView(LstBxOutput.ItemCount - 1);
                }
                ServiceSingleton.Logger.Log(Item);
            });
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
            AddItemToList(e.Step);
        }

        private void RollBack()
        {
            AddItemToList("Error detected, rollbacking changes...");
            ServiceSingleton.Dashboard.Status("Error detected, rollbacking changes, please wait...");
            ServiceSingleton.Files.RemoveDirectory(ServiceSingleton.Instances.WorkingInstance.InstallDir, true);
        }
    }
}