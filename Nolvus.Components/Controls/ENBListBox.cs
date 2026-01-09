using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Nolvus.Core.Interfaces;

namespace Nolvus.Components.Controls
{
    public class ENBListBox : Control, IScrollable
    {
        /* =========================
         * Public API (close to upstream)
         * ========================= */

        public static readonly StyledProperty<IEnumerable<IENBPreset>?> ItemsProperty =
            AvaloniaProperty.Register<ENBListBox, IEnumerable<IENBPreset>?>(nameof(Items));

        public IEnumerable<IENBPreset>? Items
        {
            get => GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        public static readonly StyledProperty<int> SelectedIndexProperty =
            AvaloniaProperty.Register<ENBListBox, int>(nameof(SelectedIndex), -1);

        public int SelectedIndex
        {
            get => GetValue(SelectedIndexProperty);
            set => SetValue(SelectedIndexProperty, value);
        }

        public IENBPreset? SelectedItem
        {
            get
            {
                var list = Snapshot();
                if (SelectedIndex < 0 || SelectedIndex >= list.Count)
                    return null;
                return list[SelectedIndex];
            }
        }

        public double ScalingFactor { get; set; } = 1.0;

        /* =========================
         * Layout constants (WinForms parity)
         * ========================= */

        private const double ItemHeight = 110;        // WinForms ItemHeight = 110 in designer
        private const double ImgW = 150;
        private const double ImgH = 95;

        /* =========================
         * Colors (WinForms parity)
         * ========================= */

        private static readonly Color BgColor = Color.FromRgb(54, 54, 54);
        private static readonly Color SelectedBgColor = Color.FromRgb(204, 122, 0);
        private static readonly Color Silver = Color.FromRgb(192, 192, 192);

        /* =========================
         * State
         * ========================= */

        private readonly Dictionary<SixLabors.ImageSharp.Image, Bitmap> _bitmapCache = new();
        private Vector _offset;

        /* =========================
         * ctor
         * ========================= */

        public ENBListBox()
        {
            ClipToBounds = true;

            PointerPressed += OnPointerPressed;

            this.GetObservable(ItemsProperty).Subscribe(_ =>
            {
                CoerceSelection();
                InvalidateMeasure();
                InvalidateVisual();
                RaiseScrollInvalidated();
            });

            this.GetObservable(SelectedIndexProperty).Subscribe(_ =>
            {
                InvalidateVisual();
            });
        }

        /* =========================
         * Rendering
         * ========================= */

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            // Background
            context.FillRectangle(new SolidColorBrush(BgColor), Bounds);

            var list = Snapshot();
            if (list.Count == 0)
                return;

            // Fonts: close enough to WinForms defaults (Segoe UI / MS Sans Serif)
            var titleTypeface = new Typeface("Segoe UI");
            var infoTypeface = new Typeface("Segoe UI");

            // Sizes roughly matching WinForms: title ~12 bold, desc ~8.25, version ~7, badge ~8.25 bold
            double titleSize = 16 * ScalingFactor;
            double descSize = 12 * ScalingFactor;
            double versionSize = 11 * ScalingFactor;
            double badgeSize = 12 * ScalingFactor;

            // scroll offset
            double yStart = -Offset.Y;

            // widths
            double textLeft = 155;
            double textMaxWidth = Math.Max(0, Bounds.Width - textLeft - 10);

            for (int i = 0; i < list.Count; i++)
            {
                double top = yStart + (i * ItemHeight);

                // Cull offscreen
                if (top > Bounds.Height)
                    break;
                if (top + ItemHeight < 0)
                    continue;

                var preset = list[i];

                // Selected background (WinForms used orange selection background)
                if (i == SelectedIndex)
                {
                    context.FillRectangle(
                        new SolidColorBrush(SelectedBgColor),
                        new Rect(0, top, Bounds.Width, ItemHeight)
                    );
                }

                // ---- Image border and image ----
                var imgRect = new Rect(3, top + 5, ImgW, ImgH);
                context.DrawRectangle(new Pen(new SolidColorBrush(Silver), 1), imgRect);

                var bmp = GetBitmap(preset);
                if (bmp != null)
                {
                    context.DrawImage(
                        bmp,
                        new Rect(0, 0, bmp.PixelSize.Width, bmp.PixelSize.Height),
                        imgRect
                    );
                }

                // ---- Title (right side) ----
                {
                    var titleLayout = new TextLayout(
                        preset.Name ?? string.Empty,
                        titleTypeface,
                        titleSize,
                        Brushes.White,
                        TextAlignment.Left,
                        TextWrapping.NoWrap,
                        maxWidth: textMaxWidth
                    );
                    titleLayout.Draw(context, new Point(textLeft, top + 3));
                }

                // ---- Description (wrapped block) ----
                var descRect = new Rect(textLeft + 1, top + 30, textMaxWidth, 50);
                {
                    var descLayout = new TextLayout(
                        preset.Description ?? string.Empty,
                        infoTypeface,
                        descSize,
                        Brushes.White,
                        TextAlignment.Left,
                        TextWrapping.Wrap,
                        maxWidth: descRect.Width
                    );
                    descLayout.Draw(context, descRect.TopLeft);
                }

                // ---- Version badge (top-left of image) ----
                var versionRect = new Rect(3, top + 5, 40, 15);
                context.FillRectangle(new SolidColorBrush(BgColor), versionRect);

                {
                    var verLayout = new TextLayout(
                        $"v {preset.Version ?? ""}",
                        infoTypeface,
                        versionSize,
                        Brushes.White,
                        TextAlignment.Left,
                        TextWrapping.NoWrap
                    );

                    verLayout.Draw(context, new Point(6, top + 7));
                }

                // Redraw image border (WinForms did it twice)
                context.DrawRectangle(new Pen(new SolidColorBrush(Silver), 1), imgRect);

                // ---- Installed badge (bottom-left of image) ----
                var installedRect = new Rect(6, top + 93, 90, 15);
                var installedBrush = preset.Installed ? Brushes.Green : Brushes.OrangeRed;
                var installedText = preset.Installed ? "Installed" : "Not installed";

                context.FillRectangle(installedBrush, installedRect);

                {
                    var badgeLayout = new TextLayout(
                        installedText,
                        infoTypeface,
                        badgeSize,
                        Brushes.White,
                        TextAlignment.Left,
                        TextWrapping.NoWrap
                    );

                    badgeLayout.Draw(context, new Point(8, top + 93));
                }
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            // Let a ScrollViewer control the viewport; our Extent reports full content height.
            return new Size(
                availableSize.Width,
                Math.Min(availableSize.Height, Extent.Height)
            );
        }

        /* =========================
         * Input selection
         * ========================= */

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var pos = e.GetPosition(this);
            int index = (int)((pos.Y + Offset.Y) / ItemHeight);

            var list = Snapshot();
            if (index >= 0 && index < list.Count)
            {
                SelectedIndex = index;
                e.Handled = true;
            }
        }

        /* =========================
         * Helpers
         * ========================= */

        private List<IENBPreset> Snapshot()
        {
            if (Items == null)
                return new List<IENBPreset>();

            if (Items is IList<IENBPreset> ilist)
                return new List<IENBPreset>(ilist);

            return new List<IENBPreset>(Items);
        }

        private void CoerceSelection()
        {
            var list = Snapshot();
            if (list.Count == 0)
            {
                SelectedIndex = -1;
                return;
            }

            if (SelectedIndex < 0 || SelectedIndex >= list.Count)
                SelectedIndex = 0;
        }

        private Bitmap? GetBitmap(IENBPreset preset)
        {
            // If your preset uses a different property/type, adjust this line only:
            var img = preset.Image as SixLabors.ImageSharp.Image;

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

        /* =========================
         * IScrollable (ScrollViewer support)
         * ========================= */

        public Vector Offset
        {
            get => _offset;
            set
            {
                if (_offset == value)
                    return;

                _offset = value;
                InvalidateVisual();
                RaiseScrollInvalidated();
            }
        }

        public Size Extent => new Size(Bounds.Width, Snapshot().Count * ItemHeight);
        public Size Viewport => Bounds.Size;

        public bool CanHorizontallyScroll { get; set; } = false;
        public bool CanVerticallyScroll { get; set; } = true;

        public event EventHandler? ScrollInvalidated;

        private void RaiseScrollInvalidated()
            => ScrollInvalidated?.Invoke(this, EventArgs.Empty);
    }
}
