using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using Nolvus.Api.Installer.Library;
using Nolvus.Core.Events;
using Nolvus.Core.Services;

namespace Nolvus.Components.Controls
{
    public class NolvusListBox : Control
    {
        public ObservableCollection<INolvusVersionDTO> Items { get; } = new ObservableCollection<INolvusVersionDTO>();

        public double ScalingFactor { get; set; } = 1;

        private const int ItemHeight = 40;
        private int _selectedIndex = -1;

        // Cache for ImageSharp.Image -> Avalonia Bitmap
        private readonly Dictionary<SixLabors.ImageSharp.Image, Bitmap> _bitmapCache =
            new Dictionary<SixLabors.ImageSharp.Image, Bitmap>();

        public NolvusListBox()
        {
            ClipToBounds = true;
        }

        private Bitmap? GetAvaloniaBitmap(SixLabors.ImageSharp.Image img)
        {
            if (img == null)
                return null;

            if (_bitmapCache.TryGetValue(img, out Bitmap cached))
                return cached;

            using MemoryStream ms = new MemoryStream();
            img.Save(ms, new PngEncoder());
            ms.Position = 0;

            Bitmap bmp = new Bitmap(ms);
            _bitmapCache[img] = bmp;
            return bmp;
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            Avalonia.Point pt = e.GetPosition(this);
            int index = (int)(pt.Y / ItemHeight);

            if (index >= 0 && index < Items.Count)
            {
                _selectedIndex = index;
                InvalidateVisual();
            }
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            // Background fill
            context.FillRectangle(
                new SolidColorBrush(Avalonia.Media.Color.FromRgb(54, 54, 54)),
                new Rect(0, 0, Bounds.Width, Bounds.Height));

            if (Items.Count == 0)
                return;

            Typeface nameFont = new Typeface("Segoe UI Light", FontStyle.Normal, FontWeight.Bold);
            Typeface infoFont = new Typeface("Microsoft Sans Serif", FontStyle.Normal, FontWeight.Normal);

            for (int i = 0; i < Items.Count; i++)
            {
                INolvusVersionDTO Nolvus = Items[i];
                double top = i * ItemHeight;

                bool isSelected = i == _selectedIndex;

                // Selection highlight
                if (isSelected)
                {
                    context.FillRectangle(
                        new SolidColorBrush(Avalonia.Media.Color.FromRgb(204, 122, 0)),
                        new Rect(0, top, Bounds.Width, ItemHeight));
                }

                // Draw Nolvuslication icon (scaled to row height)
                if (Nolvus.ImageObject is SixLabors.ImageSharp.Image img)
                {
                    Bitmap? bmp = GetAvaloniaBitmap(img);
                    if (bmp != null)
                    {
                        Rect sourceRect = new Rect(0, 0, bmp.PixelSize.Width, bmp.PixelSize.Height);
                        Rect destRect = new Rect(3, top + 2, 36, 36);
                        context.DrawImage(bmp, sourceRect, destRect);
                    }
                }

                // Nolvus Name
                new TextLayout(
                    Nolvus.Name ?? "",
                    nameFont,
                    12 * ScalingFactor,
                    Brushes.White)
                    .Draw(context, new Avalonia.Point(45, top + 2));

                // Description
                new TextLayout(
                    Nolvus.Description ?? "",
                    infoFont,
                    8.25 * ScalingFactor,
                    Brushes.White)
                    .Draw(context, new Avalonia.Point(45, top + 18));

                // Version
                new TextLayout(
                    $"v {Nolvus.Version}",
                    infoFont,
                    7 * ScalingFactor,
                    Brushes.White)
                    .Draw(context, new Avalonia.Point(45, top + 32));

                // Beta tag
                if (Nolvus.IsBeta)
                {
                    context.FillRectangle(
                        new SolidColorBrush(Colors.Orange),
                        new Rect(100, top + 20, 45, 15));

                    new TextLayout(
                        "Beta",
                        infoFont,
                        8.25 * ScalingFactor,
                        Brushes.White)
                        .Draw(context, new Avalonia.Point(105, top + 21));
                }

                // Maintenance tag
                if (Nolvus.Maintenance)
                {
                    context.FillRectangle(
                        new SolidColorBrush(Colors.OrangeRed),
                        new Rect(6, top + 20, 80, 15));

                    new TextLayout(
                        "Maintenance",
                        infoFont,
                        8.25 * ScalingFactor,
                        Brushes.White)
                        .Draw(context, new Avalonia.Point(8, top + 21));
                }
            }
        }

        protected override Avalonia.Size MeasureOverride(Avalonia.Size availableSize)
        {
            return new Avalonia.Size(availableSize.Width, Items.Count * ItemHeight);
        }
    }
}
