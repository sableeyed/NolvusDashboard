using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Nolvus.Core.Events;

namespace Nolvus.Components.Controls
{
    public class ModsBox : Control
    {
        public ObservableCollection<ModProgress> Items { get; } = new();

        public double ScalingFactor { get; set; } = 1;

        private const int ItemHeight = 50;

        private readonly Dictionary<SixLabors.ImageSharp.Image, Bitmap> _bitmapCache = new();

        public ModsBox()
        {
            ClipToBounds = true;

            Items.CollectionChanged += (_, __) =>
            {
                InvalidateMeasure();
                InvalidateVisual();
            };
        }

        private Bitmap? GetAvaloniaBitmap(SixLabors.ImageSharp.Image img)
        {
            if (img == null)
                return null;

            if (_bitmapCache.TryGetValue(img, out var cached))
                return cached;

            using var ms = new MemoryStream();
            img.Save(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
            ms.Position = 0;

            var bmp = new Bitmap(ms);
            _bitmapCache[img] = bmp;
            return bmp;
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            // Background
            context.FillRectangle(new SolidColorBrush(Color.FromRgb(54, 54, 54)), Bounds);

            if (Items.Count == 0)
                return;

            var titleFont = 14 * ScalingFactor;
            var infoFont = 11 * ScalingFactor;

            var titleTypeface = new Typeface("Segoe UI");
            var infoTypeface = new Typeface("Segoe UI");

            for (int index = 0; index < Items.Count; index++)
            {
                var item = Items[index];
                var top = ItemHeight * index;

                // Determine image width
                double imgWidth = 0;
                if (item.Image is SixLabors.ImageSharp.Image img)
                {
                    var bmp = GetAvaloniaBitmap(img);
                    if (bmp != null)
                    {
                        imgWidth = bmp.PixelSize.Width;
                        context.DrawImage(
                            bmp,
                            new Rect(0, 0, bmp.PixelSize.Width, bmp.PixelSize.Height),
                            new Rect(6, top + 6, bmp.PixelSize.Width, bmp.PixelSize.Height)
                        );
                    }
                }

                // Left margin after image
                double textLeft = 6 + imgWidth + 10;

                // Progress bar background highlight
                Color barColor = item.HasError
                    ? Color.FromArgb(40, 255, 0, 0)
                    : Color.FromArgb(40, 255, 165, 0);

                context.FillRectangle(
                    new SolidColorBrush(barColor),
                    new Rect(textLeft, top + 6, Bounds.Width - textLeft - 10, 36)
                );

                // Thin top accent line
                context.DrawRectangle(
                    new Pen(Brushes.Orange, 1),
                    new Rect(5, top + 6, Bounds.Width - 10, 1)
                );

                //
                // TEXT LAYOUT
                //

                // Mod Name
                new TextLayout(
                    item.Name ?? "",
                    titleTypeface,
                    titleFont,
                    Brushes.White)
                .Draw(context, new Point(textLeft, top + 6));

                // Percent OR Error (left aligned)
                string percentText = item.HasError ? "Error" : $"{item.PercentDone}%";
                var percentBrush = item.HasError ? Brushes.Red : Brushes.White;

                new TextLayout(
                    percentText,
                    infoTypeface,
                    infoFont,
                    percentBrush)
                .Draw(context, new Point(textLeft, top + 28));

                // Speed (after percent)
                if (item.Mbs > 0)
                {
                    new TextLayout(
                        $"{item.Mbs:0.0}MB/s",
                        infoTypeface,
                        infoFont,
                        Brushes.White)
                    .Draw(context, new Point(textLeft + 55, top + 28));
                }

                // Status text (after speed)
                var statusBrush = item.HasError ? Brushes.Red : Brushes.Orange;

                new TextLayout(
                    item.Status ?? "",
                    infoTypeface,
                    infoFont,
                    statusBrush)
                .Draw(context, new Point(textLeft + 120, top + 28));
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(availableSize.Width, Items.Count * ItemHeight);
        }

        //
        // Helpers for frame
        //

        public void AddItem(string name)
        {
            Items.Add(new ModProgress
            {
                Name = name,
                Status = "Queued",
                PercentDone = 0,
                HasError = false
            });
        }

        public void UpdateItem(string name, string status, int percent, double mbs = 0)
        {
            var item = Items.FirstOrDefault(x => x.Name == name);
            if (item == null)
                return;

            item.Status = status;
            item.PercentDone = percent;
            item.Mbs = mbs;

            InvalidateVisual();
        }

        public void RemoveItem(string name)
        {
            var item = Items.FirstOrDefault(x => x.Name == name);
            if (item != null)
                Items.Remove(item);
        }
    }
}
