using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Input;
using Avalonia.Media.TextFormatting;
using Nolvus.Core.Services;
using Nolvus.Core.Events;

namespace Nolvus.Components.Controls
{
    public class ModsBox : Control
    {
        public ObservableCollection<ModProgress> Items { get; } = new ObservableCollection<ModProgress>();

        public double ScalingFactor { get; set; } = 1;

        private const int ItemHeight = 35;

        // Cache: SixLabors.ImageSharp.Image -> Avalonia Bitmap
        private readonly Dictionary<SixLabors.ImageSharp.Image, Avalonia.Media.Imaging.Bitmap> _bitmapCache = new();

        public ModsBox()
        {
            ClipToBounds = true;

            // Auto-update UI on add/remove
            Items.CollectionChanged += (_, __) =>
            {
                InvalidateMeasure();
                InvalidateVisual();
            };
        }

        private Avalonia.Media.Imaging.Bitmap? GetAvaloniaBitmap(SixLabors.ImageSharp.Image img)
        {
            if (img == null)
                return null;

            if (_bitmapCache.TryGetValue(img, out var cached))
                return cached;

            using var ms = new MemoryStream();
            img.Save(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
            ms.Position = 0;

            var bmp = new Avalonia.Media.Imaging.Bitmap(ms);
            _bitmapCache[img] = bmp;
            return bmp;
        }

        private int GetProgressWidth(int value)
        {
            double width = Math.Max(0, Bounds.Width - 100);
            return (int)(width * (value / 100.0));
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            // Background
            context.FillRectangle(
                new SolidColorBrush(Color.FromRgb(54, 54, 54)),
                Bounds);

            if (Items.Count == 0)
                return;

            var titleTypeface = new Typeface("Segoe UI Light");
            var infoTypeface = new Typeface("Segoe UI");

            for (int index = 0; index < Items.Count; index++)
            {
                var progress = Items[index];
                var top = ItemHeight * index;

                // Highlight bar
                Color barColor = progress.HasError
                    ? Color.FromArgb(30, 255, 0, 0)
                    : Color.FromArgb(30, 255, 165, 0);

                context.FillRectangle(
                    new SolidColorBrush(barColor),
                    new Rect(105, 5 + top, GetProgressWidth(progress.PercentDone), 30));

                // Thin orange line
                context.DrawRectangle(
                    new Pen(Brushes.Orange, 1),
                    new Rect(3, 5 + top, progress.PercentDone, 1));

                // Mod Name
                new TextLayout(progress.Name ?? "",
                    titleTypeface,
                    9 * ScalingFactor,
                    Brushes.White)
                    .Draw(context, new Point(105, 3 + top));

                // Image (optional)
                if (progress.Image is SixLabors.ImageSharp.Image img)
                {
                    var bmp = GetAvaloniaBitmap(img);
                    if (bmp != null)
                    {
                        context.DrawImage(
                            bmp,
                            new Rect(0, 0, bmp.PixelSize.Width, bmp.PixelSize.Height),
                            new Rect(3, 5 + top, bmp.PixelSize.Width, bmp.PixelSize.Height));
                    }
                }

                // Speed
                if (progress.Mbs != 0)
                {
                    new TextLayout($"{progress.Mbs:0.0}MB/s",
                        infoTypeface,
                        7 * ScalingFactor,
                        Brushes.White)
                        .Draw(context, new Point(105, 10 + top));
                }

                // Percent or Error
                if (!progress.HasError)
                {
                    new TextLayout($"{progress.PercentDone}%",
                        infoTypeface,
                        7 * ScalingFactor,
                        Brushes.White)
                        .Draw(context, new Point(105, 22 + top));
                }
                else
                {
                    new TextLayout("Error",
                        infoTypeface,
                        7 * ScalingFactor,
                        Brushes.Red)
                        .Draw(context, new Point(105, 22 + top));
                }

                // Status
                var statusBrush = progress.HasError ? Brushes.Red : Brushes.Orange;
                new TextLayout(progress.Status ?? "",
                    infoTypeface,
                    7 * ScalingFactor,
                    statusBrush)
                    .Draw(context, new Point(105, 20 + top));
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
            {
                Items.Remove(item);
            }
        }

    }
}
