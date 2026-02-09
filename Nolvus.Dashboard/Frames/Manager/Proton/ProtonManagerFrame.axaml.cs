using System;
using System.IO;
using System.Linq;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Dashboard.Services.Proton;

namespace Nolvus.Dashboard.Frames.Manager.Proton
{
    public partial class ProtonManagerFrame : DashboardFrame
    {
        public ProtonManagerFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
        }

        protected override async Task OnLoadedAsync()
        {
            await DetectProtonRunners();
            await DetectPrefix();
        }

        private async Task DetectProtonRunners()
        {
            var ProtonPaths = Directory.GetDirectories(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/Steam/compatibilitytools.d"));
            var Runners = ProtonPaths.Select(p => new DirectoryInfo(p).Name).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();

            try
            {
                Runners.Remove("LegacyRuntime");
                //Runners.Remove("GE-Proton10-25");
            }
            catch { }

            DrpDwnLstProtonRunner.ItemsSource = Runners;

            if (Runners.Count > 0)
            {
                DrpDwnLstProtonRunner.SelectedIndex = 0;
            }
            else
            {
                Dispatcher.UIThread.Post(() =>
                {
                    ProtonPathBox.IsEnabled = true;
                    ProtonPathButton.IsEnabled = true;
                    DrpDwnLstProtonRunner.PlaceholderText = "No Protons Found!";
                });
            }
        }

        private async Task DetectPrefix()
        {
            string AppId = "489830";

            var PrefixPath = await Protontricks.GetPrefixPathAsync(AppId);

            if (!string.IsNullOrWhiteSpace(PrefixPath))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    PrefixDirectoryBox.IsEnabled = true;
                    PrefixDirectoryBox.Text = PrefixPath;
                });
            }
            else
            {
                Dispatcher.UIThread.Post(() =>
                {
                    PrefixDirectoryBox.IsEnabled = true;
                    PrefixDirectoryButton.IsEnabled = true;
                    PrefixDirectoryBox.Text = "Prefix not found! Please browse manually.";
                });
            }
        }

        private void BtnBack_Click(object? sender, RoutedEventArgs e)
        {
            Console.WriteLine("Not implemented");
        }

        private void BtnInstall_Click(object? sender, RoutedEventArgs e)
        {
            Console.WriteLine("Not implemented");
        }

        private void PrefixPath_Click(object? sender, RoutedEventArgs e)
        {
            Console.WriteLine("Not implemented");
        }

        private void ProtonPath_Click(object? sender, RoutedEventArgs e)
        {
            Console.WriteLine("Not implemented");
        }
    }
}