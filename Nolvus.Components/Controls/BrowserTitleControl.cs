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
        private readonly Button BtnClose;

        public event EventHandler? CloseRequested;

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
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.NoWrap
            };

            BtnClose = new Button
            {
                //Content = "✕",
                Content = new TextBlock
                {
                    Text = "✕",
                    FontSize = 22,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                },
                FontSize = 22,
                Padding = new Thickness(0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = new Cursor(StandardCursorType.Hand),
                Margin = new Thickness(0, 0, 8, 0)
            };

            // Set up layout container
            var panel = new Grid
            {
                Background = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 40,
                ColumnDefinitions = new ColumnDefinitions("*,Auto")
            };
            panel.Children.Add(LblTitle);
            panel.Children.Add(BtnClose);

            Grid.SetColumn(LblTitle, 0);
            Grid.SetColumn(BtnClose, 1);

            Content = panel;

            // Mouse event handler
            LblTitle.PointerPressed += LblTitle_PointerPressed;
            BtnClose.PointerEntered += (_, __) => BtnClose.Foreground = new SolidColorBrush(Color.Parse("#F28F1A"));
            BtnClose.PointerExited += (_, __) => BtnClose.Foreground = Brushes.White;
            BtnClose.Click += (_, __) => CloseRequested.Invoke(this, EventArgs.Empty);

        }

        private void LblTitle_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // Simply forward the call to your parent’s handler
            OnPointerPressed(e);
        }

    }
}
