using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Interactivity;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Dashboard.Core;
using System;
using System.Threading.Tasks;

namespace Nolvus.Dashboard.Frames.Settings
{
    public partial class GameFrame : DashboardFrame
    {
        public GameFrame(IDashboard dashboard, FrameParameters parameters)
            : base(dashboard, parameters)
        {
            InitializeComponent();

            BtnBrowse.Click += BtnBrowse_Click;
            BtnAutoDetect.Click += BtnAutoDetect_Click;
            BtnNext.Click += BtnNext_Click;

            this.Loaded += GameFrame_Loaded;
        }

        // Optional parameterless ctor (handy for design-time)
        public GameFrame() : this(ServiceSingleton.Dashboard, new FrameParameters())
        {
        }

        private async void BtnBrowse_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is null )
                return;
            
            var result = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions {
                Title = "Select Skyrim Installation Folder",
                AllowMultiple = false
            });

            if (result.Count > 0) {
                TxtPath.Text = result[0].Path.LocalPath;
            }
        }

        private async void BtnAutoDetect_Click(object? sender, RoutedEventArgs e)
        {
            if (ServiceSingleton.Game.IsGameInstalled()) {
                TxtPath.Text = ServiceSingleton.Game.GetSkyrimSEDirectory();
                Console.WriteLine(ServiceSingleton.Game.GetSkyrimSEDirectory());
            }
            else {
                LblError.Text = "Skyrim Anniversary Edition not found! Check if your game is installed. If it is installed, trying browsing to the installation directory manually";
                LblError.IsVisible = true;
            }
        }

        private async void BtnNext_Click(object? sender, RoutedEventArgs e)
        {
            /* DEBUGGING REMOVE LATER */
            if(Program.DebugMode)
            {
                ServiceSingleton.Dashboard.LoadFrame<NexusFrame>();
            }
            if (string.IsNullOrEmpty(TxtPath.Text))
            {
                LblError.Text = "You must select a directory";
                LblError.IsVisible = true;
            }
            else
            {
                //next frame
                SettingsCache.GameDirectory = TxtPath.Text;
                ServiceSingleton.Dashboard.LoadFrame<NexusFrame>();
            }
        }

        private void GameFrame_Loaded(object? sender, RoutedEventArgs e)
        {
            if (TopLevel.GetTopLevel(this) is DashboardWindow dw)
                dw.DisableSettings();
        }
    }
}
