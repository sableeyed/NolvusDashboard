using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Nolvus.Core.Enums;
using Nolvus.Dashboard.Controls;

namespace Nolvus.Dashboard.Controls
{
    public partial class ErrorPanel : UserControl
    {
        public string ModName
        {
            get => LblModName.Text ?? "";
            set => LblModName.Text = value;
        }

        public string ErrorText
        {
            get => LnkError.Text ?? "";
            set => LnkError.Text = value;
        }

        public ErrorPanel()
        {
            InitializeComponent();
            var owner = TopLevel.GetTopLevel(this) as Window;

            // Click handler like LinkLabel
            LnkError.PointerPressed += (_, __) =>
            {
                NolvusMessageBox.Show(
                    owner,
                    ModName,
                    ErrorText,
                    MessageBoxType.Error,
                    300,
                    700,
                    Avalonia.Media.Colors.Red
                );
            };
        }

        public void SetImage(SixLabors.ImageSharp.Image? image)
        {
            if (image == null)
            {
                Img.Source = null;
                return;
            }

            using var ms = new System.IO.MemoryStream();
            image.Save(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
            ms.Position = 0;

            Img.Source = new Bitmap(ms);
        }
    }
}
