using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Layout;

namespace Nolvus.Core.Frames
{
    public partial class DashboardFrame
    {
        private void InitializeComponent()
        {
            // Set the size of the frame
            this.Width = 1011;
            this.Height = 469;

            // Set background color
            this.Background = new SolidColorBrush(Color.FromRgb(54, 54, 54));

            // Optional: Layout
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.VerticalAlignment = VerticalAlignment.Stretch;

            // No components container is needed in Avalonia
        }
    }
}
