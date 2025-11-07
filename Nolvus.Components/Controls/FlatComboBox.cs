using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace Nolvus.Components.Controls
{
    public class FlatComboBox : ComboBox, IStyleable
    {
        Type IStyleable.StyleKey => typeof(ComboBox);

        public static readonly StyledProperty<Color> BorderColorProperty =
            AvaloniaProperty.Register<FlatComboBox, Color>(nameof(BorderColor), Colors.Gray);

        public Color BorderColor
        {
            get => GetValue(BorderColorProperty);
            set => SetValue(BorderColorProperty, value);
        }

        public static readonly StyledProperty<Color> ButtonColorProperty =
            AvaloniaProperty.Register<FlatComboBox, Color>(nameof(ButtonColor), Colors.LightGray);

        public Color ButtonColor
        {
            get => GetValue(ButtonColorProperty);
            set => SetValue(ButtonColorProperty, value);
        }

        public FlatComboBox()
        {
            CornerRadius = new CornerRadius(0);
            MinHeight = 28;

            // ✅ Correct Avalonia 11 change notifications
            this.GetObservable(BorderColorProperty).Subscribe(_ => InvalidateVisual());
            this.GetObservable(ButtonColorProperty).Subscribe(_ => InvalidateVisual());
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var bounds = new Rect(Bounds.Size);

            // Border
            var borderBrush = new SolidColorBrush(BorderColor);
            context.DrawRectangle(new Pen(borderBrush, 1), bounds.Deflate(0.5));

            // Dropdown button area
            double buttonWidth = 24;
            var buttonRect = new Rect(bounds.Right - buttonWidth, bounds.Top + 1, buttonWidth - 2, bounds.Height - 2);

            context.FillRectangle(
                new SolidColorBrush(ButtonColor),
                buttonRect);

            // Arrow
            var cx = buttonRect.X + buttonRect.Width / 2;
            var cy = buttonRect.Y + buttonRect.Height / 2;

            var p1 = new Point(cx - 4, cy - 2);
            var p2 = new Point(cx + 4, cy - 2);
            var p3 = new Point(cx, cy + 2);

            var geo = new StreamGeometry();
            using (var ctx = geo.Open())
            {
                ctx.BeginFigure(p1, true);
                ctx.LineTo(p2);
                ctx.LineTo(p3);
                ctx.EndFigure(true);
            }

            context.DrawGeometry(Brushes.White, null, geo);
        }
    }
}
