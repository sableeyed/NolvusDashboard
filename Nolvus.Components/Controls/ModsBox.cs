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

        // Accent color (#F28F1A)
        private static readonly Color AccentColor = Color.FromRgb(242, 143, 26);

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
            context.FillRectangle(
                new SolidColorBrush(Color.FromRgb(54, 54, 54)),
                Bounds
            );

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

                // ---- IMAGE ----
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

                // Text starts immediately after the image
                double textLeft = 6 + imgWidth + 10;

                // ---- PROGRESS BAR GEOMETRY ----
                var barRect = new Rect(
                    textLeft,
                    top + 6,
                    Bounds.Width - textLeft - 10,
                    36
                );

                // Track / background
                var trackColor = item.HasError
                    ? Color.FromArgb(40, 255, 0, 0)
                    : Color.FromArgb(30, 255, 255, 255);

                context.FillRectangle(
                    new SolidColorBrush(trackColor),
                    barRect
                );

                // Progress fill (growing)
                if (!item.HasError && item.PercentDone > 0)
                {
                    double progressWidth = barRect.Width * (item.PercentDone / 100.0);

                    context.FillRectangle(
                        new SolidColorBrush(AccentColor),
                        new Rect(barRect.X, barRect.Y, progressWidth, barRect.Height)
                    );

                    // Top accent line tied to progress
                    context.DrawLine(
                        new Pen(new SolidColorBrush(AccentColor), 1),
                        new Point(barRect.X, barRect.Y),
                        new Point(barRect.X + progressWidth, barRect.Y)
                    );
                }

                // ---- TEXT ----

                // Mod name
                new TextLayout(
                    item.Name ?? string.Empty,
                    titleTypeface,
                    titleFont,
                    Brushes.White)
                .Draw(context, new Point(textLeft, top + 6));

                // Percent or error
                string percentText = item.HasError ? "Error" : $"{item.PercentDone}%";
                var percentBrush = item.HasError ? Brushes.Red : Brushes.White;

                new TextLayout(
                    percentText,
                    infoTypeface,
                    infoFont,
                    percentBrush)
                .Draw(context, new Point(textLeft, top + 28));

                // Speed
                if (item.Mbs > 0)
                {
                    new TextLayout(
                        $"{item.Mbs:0.0}MB/s",
                        infoTypeface,
                        infoFont,
                        Brushes.White)
                    .Draw(context, new Point(textLeft + 55, top + 28));
                }

                // Status
                var statusBrush = item.HasError ? Brushes.Red : Brushes.White;

                new TextLayout(
                    item.Status ?? string.Empty,
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
