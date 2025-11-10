using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Threading.Tasks;
using Nolvus.Core.Enums;

namespace Nolvus.Dashboard.Controls
{

    public partial class NolvusMessageBox : Window
    {
        private TaskCompletionSource<bool?> _tcs;

        public NolvusMessageBox(string title, string message, MessageBoxType type)
        {
            InitializeComponent();

            LblTitle.Text = title;
            LblMessage.Text = message;

            SetIcon(type);

            BtnOK.IsVisible = type != MessageBoxType.Question;
            BtnYes.IsVisible = type == MessageBoxType.Question;
            BtnNo.IsVisible = type == MessageBoxType.Question;

            BtnOK.Click += (_, __) => Close(true);
            BtnYes.Click += (_, __) => Close(true);
            BtnNo.Click += (_, __) => Close(false);
           
        }

        public void SetIcon(MessageBoxType type)
        {
            string path = type switch 
            {
                MessageBoxType.Info => "avares://NolvusDashboard/Assets/Info.png",
                MessageBoxType.Warning => "avares://NolvusDashboard/Assets/Warning-Message.png",
                MessageBoxType.Error => "avares://NolvusDashboard/Assets/Wrong-WF.png",
                MessageBoxType.Question => "avares://NolvusDashboard/Assets/Question.png"
            };

            try 
            {
                var uri = new Uri(path);
                using var stream = AssetLoader.Open(uri);
                ImgIcon.Source = new Bitmap(stream);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to load messagebox icon");
            }
        }

        public static async Task<bool?> Show(Window owner, string title, string message, MessageBoxType type)
        {
            var msgBox = new NolvusMessageBox(title, message, type);
            msgBox.Owner = owner;
            return await msgBox.ShowDialog<bool?>(owner);
        }
    }
}
