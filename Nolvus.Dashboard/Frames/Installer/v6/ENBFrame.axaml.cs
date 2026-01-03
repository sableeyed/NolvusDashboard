using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Misc;
using Nolvus.Core.Services;

namespace Nolvus.Dashboard.Frames.Installer.v6
{
    public partial class ENBFrame : DashboardFrame
    {
        public ENBFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
            BtnContinue.Click += BtnContinue_Click;
            BtnPrevious.Click += BtnPrevious_Click;
        }

        protected override async Task OnLoadedAsync()
        {
            ServiceSingleton.Dashboard.Info("ENB Selection");
            
            DrpDwnLstENB.ItemsSource = ENBs.GetAvailableENBsForV6();
            DrpDwnLstENB.SelectedIndex = 0;
        }

        private void BtnPrevious_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Dashboard.LoadFrame<v6.DifficultyFrame>();
        }

        private void BtnContinue_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Dashboard.LoadFrame<PageFileFrame>(); 
        }

        private void OnENBChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DrpDwnLstENB.SelectedItem is null)
                return;

            var ENB = ENBs.GetAvailableENBsForV6().Where(x => x.Name == DrpDwnLstENB.SelectedItem.ToString()).FirstOrDefault();

            if (ENB == null)
            {
                Console.WriteLine("NULL");
                return;
            }

            LblENBDesc.Text = ENB.Description;

            ServiceSingleton.Instances.WorkingInstance.Options.AlternateENB = ENB.Code;

            switch (ENB.Code)
            {
                case "CABBAGE":
                    PicBoxENB.Source = LoadImage("avares://NolvusDashboard/Assets/Cabbage_ENB_01.jpg");
                    break;
                case "CABBAVAL":
                    PicBoxENB.Source = LoadImage("avares://NolvusDashboard/Assets/Cabbaval-ENB.jpg");
                    break;
                case "KAUZ":
                    PicBoxENB.Source = LoadImage("avares://NolvusDashboard/Assets/Kauz-ENB.jpg");
                    break;
                case "PICHO":
                    PicBoxENB.Source = LoadImage("avares://NolvusDashboard/Assets/PiCho-ENB.jpg");
                    break;
                case "AMON":
                    PicBoxENB.Source = LoadImage("avares://NolvusDashboard/Assets/Amon-ENB.jpg");
                    break;
            } 
        }

        private IImage LoadImage(string path)
        {
            using var stream = AssetLoader.Open(new Uri(path));
            return new Bitmap(stream);
        }
    }
}