using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using Nolvus.Core.Services;

namespace Nolvus.Components.Controls
{
    public delegate void SettingsHandler(object sender, EventArgs e);

    public class TitleBarControl : UserControl
    {
        private bool _SettingsEnabled = true;

        private TextBlock LblTitle;
        private TextBlock LblInfo;
        private Avalonia.Controls.Image AccountImage;
        private Button SettingsBox;

        private event SettingsHandler OnSettingsClickedEvent;

        public event SettingsHandler OnSettingsClicked
        {
            add
            {
                if (OnSettingsClickedEvent != null)
                {
                    lock (OnSettingsClickedEvent)
                        OnSettingsClickedEvent += value;
                }
                else
                {
                    OnSettingsClickedEvent = value;
                }
            }
            remove
            {
                if (OnSettingsClickedEvent != null)
                {
                    lock (OnSettingsClickedEvent)
                        OnSettingsClickedEvent -= value;
                }
            }
        }

        public string Title
        {
            get => LblTitle.Text;
            set => LblTitle.Text = value;
        }

        public string InfoCaption
        {
            get => LblInfo.Text;
            set => LblInfo.Text = value;
        }

        public TitleBarControl()
        {
            // Layout container
            var panel = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("40,*,*,40"),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(54, 54, 54)),
                Height = 36
            };

            AccountImage = new Avalonia.Controls.Image
            {
                Width = 28,
                Height = 28,
                Margin = new Thickness(6, 4),
                Stretch = Avalonia.Media.Stretch.UniformToFill
            };
            Grid.SetColumn(AccountImage, 0);

            LblTitle = new TextBlock
            {
                Foreground = Brushes.White,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Thickness(6, 0),
                FontSize = 14
            };
            Grid.SetColumn(LblTitle, 1);

            LblInfo = new TextBlock
            {
                Foreground = Brushes.Gray,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Thickness(4, 0),
                FontSize = 12
            };
            Grid.SetColumn(LblInfo, 2);

            SettingsBox = new Button
            {
                Content = "⚙",
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Cursor = new Cursor(StandardCursorType.Hand),
            };
            SettingsBox.PointerPressed += SettingsBox_Click;
            ToolTip.SetTip(SettingsBox, "Global settings");
            Grid.SetColumn(SettingsBox, 3);

            // Enable dragging the window by dragging any label text
            LblTitle.PointerPressed += BeginDrag;
            LblInfo.PointerPressed += BeginDrag;

            panel.Children.Add(AccountImage);
            panel.Children.Add(LblTitle);
            panel.Children.Add(LblInfo);
            panel.Children.Add(SettingsBox);

            Content = panel;
        }

        private void BeginDrag(object sender, PointerPressedEventArgs e)
        {
            var window = this.GetVisualRoot() as Window;
            window?.BeginMoveDrag(e);
        }

        public void ShowLoading()
        {
            SettingsBox.IsVisible = true;
        }

        public void HideLoading()
        {
            SettingsBox.IsVisible = false;
        }

        private void SettingsBox_Click(object? sender, PointerPressedEventArgs e)
        {
            if (_SettingsEnabled)
            {
                var handler = OnSettingsClickedEvent;
                handler?.Invoke(this, EventArgs.Empty);
            }
        }

        public void EnableSettings() => _SettingsEnabled = true;
        public void DisableSettings() => _SettingsEnabled = false;

        public bool SettingsEnabled => _SettingsEnabled;

        public void SetAccountImage(string url)
        {
            var img = SixLabors.ImageSharp.Image.Load(url);
            SetAccountImage(img);
        }

        public void SetAccountImage(SixLabors.ImageSharp.Image img)
        {
            using var ms = new MemoryStream();
            img.Save(ms, new PngEncoder());
            ms.Position = 0;
            AccountImage.Source = new Bitmap(ms);
        }
    }
}
