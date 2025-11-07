using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace Nolvus.Components.Controls
{
    public partial class FlatButton : Button
    {
        public FlatButton()
        {
            Background = new SolidColorBrush(Color.FromArgb(255, 54, 54, 54));
            Foreground = new SolidColorBrush(Colors.Orange);
            FontFamily = new FontFamily("Segoe UI Semibold");
            FontSize = 9;
            _currentBackColor = ((SolidColorBrush)Background).Color;
        }

        private Color _currentBackColor;

        private Color _onHoverBackColor = Color.FromArgb(255, 83, 83, 83);
        public Color OnHoverBackColor
        {
            get { return _onHoverBackColor; }
            set { _onHoverBackColor = value; InvalidateVisual(); }
        }

        private Color _borderColor = Colors.White;
        public Color BorderColor
        {
            get { return _borderColor; }
            set { _borderColor = value; InvalidateVisual(); }
        }

        protected override void OnPointerEntered(PointerEventArgs e)
        {
            base.OnPointerEntered(e);
            _currentBackColor = _onHoverBackColor;
            InvalidateVisual();
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            _currentBackColor = ((SolidColorBrush)Background).Color;
            InvalidateVisual();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            _currentBackColor = Color.FromArgb(255, 120, 120, 120);
            InvalidateVisual();
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            _currentBackColor = ((SolidColorBrush)Background).Color;
            InvalidateVisual();
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var fill = new SolidColorBrush(_currentBackColor);
            var rect = new Rect(0, 0, Bounds.Width, Bounds.Height);

            // Fill and 1px border
            context.FillRectangle(fill, rect);
            var pen = new Pen(new SolidColorBrush(_borderColor), 1);
            context.DrawRectangle(null, pen, new Rect(0.5, 0.5, Bounds.Width - 1, Bounds.Height - 1));

            // Text: use Content (not Text) in Avalonia
            var text = Content as string ?? "";
            var typeface = new Typeface(FontFamily, FontStyle.Normal, FontWeight);
            var layout = new TextLayout(
                text,
                typeface,
                FontSize,
                Foreground
            );

            // Center text using TextLayout width/height
            var x = (Bounds.Width - layout.Width) / 2;
            var y = (Bounds.Height - layout.Height) / 2;
            layout.Draw(context, new Point(x, y));
        }
    }
}
