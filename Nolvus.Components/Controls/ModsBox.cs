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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
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
        }

        private Avalonia.Media.Imaging.Bitmap? GetAvaloniaBitmap(SixLabors.ImageSharp.Image img)
        {
            if (img == null)
                return null;

            if (_bitmapCache.TryGetValue(img, out var cached))
                return cached;

            using var ms = new MemoryStream();
            img.Save(ms, new PngEncoder());
            ms.Position = 0;

            var bmp = new Avalonia.Media.Imaging.Bitmap(ms);
            _bitmapCache[img] = bmp;
            return bmp;
        }

        private int GetGlobalProgress(int value)
        {
            //return (int)(((Bounds.Width - 100) / 100) * value);
            return (int)((Bounds.Width - 100) * (value / 100.0));
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            // Background
            context.FillRectangle(
                new SolidColorBrush(Avalonia.Media.Color.FromRgb(54, 54, 54)),
                Bounds);

            if (Items.Count == 0)
                return;

            var titleTypeface = new Typeface("Segoe UI Light", FontStyle.Normal, FontWeight.Bold);
            var infoTypeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal);

            for (int index = 0; index < Items.Count; index++)
            {
                var progress = Items[index];
                var top = ItemHeight * index;

                // Progress background bar
                Avalonia.Media.Color barColor = progress.HasError
                    ? Avalonia.Media.Color.FromArgb(30, 255, 0, 0)
                    : Avalonia.Media.Color.FromArgb(30, 255, 165, 0);

                context.FillRectangle(
                    new SolidColorBrush(barColor),
                    new Rect(105, 5 + top, GetGlobalProgress(progress.GlobalDone), 30));

                // Thin progress bar indicator line
                context.DrawRectangle(
                    new Pen(Avalonia.Media.Brushes.Orange, 1),
                    new Rect(3, 5 + top, progress.PercentDone, 1));

                // Mod Name
                new TextLayout(progress.Name ?? "",
                    titleTypeface,
                    9 * ScalingFactor,
                    Avalonia.Media.Brushes.White)
                    .Draw(context, new Avalonia.Point(105, 3 + top));

                // Image (cached ImageSharp → Avalonia conversion)
                if (progress.Image is SixLabors.ImageSharp.Image img)
                {
                    var bmp = GetAvaloniaBitmap(img);
                    if (bmp != null)
                    {
                        context.DrawImage(
                            bmp,
                            new Rect(0, 0, bmp.PixelSize.Width, bmp.PixelSize.Height), // source
                            new Rect(3, 5 + top, bmp.PixelSize.Width, bmp.PixelSize.Height)); // destination

                        // context.DrawImage(
                        //     bmp,
                        //     new Rect(3, 5 + top, bmp.PixelSize.Width, bmp.PixelSize.Height));
                    }
                }

                // Speed
                if (progress.Mbs != 0)
                {
                    new TextLayout($"{progress.Mbs:0.0}MB/s",
                        infoTypeface,
                        7 * ScalingFactor,
                        Avalonia.Media.Brushes.White)
                        .Draw(context, new Avalonia.Point(105, 10 + top));
                }

                // Percent or Error
                if (!progress.HasError)
                {
                    new TextLayout($"{progress.PercentDone}%",
                        infoTypeface,
                        7 * ScalingFactor,
                        Avalonia.Media.Brushes.White)
                        .Draw(context, new Avalonia.Point(105, 22 + top));
                }
                else
                {
                    new TextLayout("Error",
                        infoTypeface,
                        7 * ScalingFactor,
                        Avalonia.Media.Brushes.Red)
                        .Draw(context, new Avalonia.Point(105, 22 + top));
                }

                // Status Line
                var statusBrush = progress.HasError ? Avalonia.Media.Brushes.Red : Avalonia.Media.Brushes.Orange;
                new TextLayout(progress.Status ?? "",
                    infoTypeface,
                    7 * ScalingFactor,
                    statusBrush)
                    .Draw(context, new Avalonia.Point(105, 20 + top));
            }
        }

        protected override Avalonia.Size MeasureOverride(Avalonia.Size availableSize)
        {
            return new Avalonia.Size(availableSize.Width, Items.Count * ItemHeight);
        }
    }
}
