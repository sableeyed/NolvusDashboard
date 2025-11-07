using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Nolvus.Components.Controls
{
    public partial class BrowserTitleControl : UserControl
    {
        private readonly TextBlock LblTitle;

        public string Title
        {
            get => LblTitle.Text ?? string.Empty;
            set => LblTitle.Text = value;
        }

        public BrowserTitleControl()
        {
            // Create label equivalent
            LblTitle = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 16,
                TextWrapping = TextWrapping.NoWrap
            };

            // Set up layout container
            var panel = new Grid
            {
                Background = Brushes.Transparent
            };
            panel.Children.Add(LblTitle);

            Content = panel;

            // Mouse event handler
            LblTitle.PointerPressed += LblTitle_PointerPressed;
        }

        private void LblTitle_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // Simply forward the call to your parent’s handler
            OnPointerPressed(e);
        }

    }
}
