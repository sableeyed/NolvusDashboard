using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Interactivity;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using System;
using System.Threading.Tasks;

namespace Nolvus.Dashboard.Frames
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
        }

        // Optional parameterless ctor (handy for design-time)
        public GameFrame() : this(ServiceSingleton.Dashboard, new FrameParameters())
        {
        }

        private async void BtnBrowse_Click(object? sender, RoutedEventArgs e)
        {
            // var dialog = new OpenFolderDialog
            // {
            //     Title = "Select Skyrim Special Edition Directory"
            // };

            // if (ServiceSingleton.Dashboard is Window win)
            // {
            //     var result = await dialog.ShowAsync(win);
            //     if (!string.IsNullOrWhiteSpace(result))
            //         TxtPath.Text = result;
            // }
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
                LblError.IsVisible = true;
            }
        }

        private async void BtnNext_Click(object? sender, RoutedEventArgs e)
        {

        }
    }
}
