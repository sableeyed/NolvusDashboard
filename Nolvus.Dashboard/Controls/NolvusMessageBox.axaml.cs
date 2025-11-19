using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Media;
using Avalonia.Platform;
using System;
using System.Threading.Tasks;
using Nolvus.Core.Enums;

namespace Nolvus.Dashboard.Controls
{

    public partial class NolvusMessageBox : Window
    {
        private TaskCompletionSource<bool?> _tcs;
        private bool? _dialogResult = null;
        public int CustomWidth { get; set; } = 700;
        public int CustomHeight { get; set; } = 300;
        public Avalonia.Media.Color HighlightColor { get; set; } = Avalonia.Media.Colors.Red;

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
            BtnYes.Click += (_, __) => CloseWithResult(true);
            BtnNo.Click += (_, __) => CloseWithResult(false);
           
        }

        public void CloseWithResult(bool? result)
        {
            _dialogResult = result;
            Close(result);
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
            return await msgBox.ShowDialog<bool?>(owner);
        }

        public static async Task<bool?> Show(Window owner, string title, string message, MessageBoxType type, int height, int width, Color color)
        {
            var msgBox = new NolvusMessageBox(title, message, type)
            {
                CustomHeight = height,
                CustomWidth = width,
                HighlightColor = color
            };

            return await msgBox.ShowDialog<bool?>(owner);
        }

        public static async Task<bool?> ShowConfirmation(Window owner, string title, string message)
        {
            var msgBox = new NolvusMessageBox(title, message, MessageBoxType.Question);
            return await msgBox.ShowDialog<bool?>(owner);
        }

        public static async Task<bool?> ShowConfirmation(Window owner, string title, string message, int height, int width)
        {
            var msgBox = new NolvusMessageBox(title, message, MessageBoxType.Question)
            {
                Height = height,
                Width = width
            };

            return await msgBox.ShowDialog<bool?>(owner);
        }
    }
}
