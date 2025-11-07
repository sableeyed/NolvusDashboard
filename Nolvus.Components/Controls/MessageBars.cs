using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Nolvus.Components.Controls
{
    public partial class MessageBar : UserControl
    {
        private readonly Label LblTitle;

        public string Title
        {
            get
            {
                return LblTitle.Content as string ?? "";
            }
            set
            {
                LblTitle.Content = value;
            }
        }

        public MessageBar()
        {
            // Root container similar to WinForms UserControl background + padding
            var panel = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(52, 52, 52)),
                Padding = new Thickness(6),
                Child = (LblTitle = new Label
                {
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontFamily = new FontFamily("Segoe UI Semibold"),
                    FontSize = 12
                })
            };

            Content = panel;

            LblTitle.PointerPressed += LblTitle_PointerPressed;
        }

        private void LblTitle_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            this.OnPointerPressed(e);
        }
    }
}
