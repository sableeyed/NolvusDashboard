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
using Avalonia.Platform;
using System.Runtime.CompilerServices;

namespace Nolvus.Components.Controls
{
    public delegate void SettingsHandler(object sender, EventArgs e);

    public class TitleBarControl : UserControl
    {
        private bool _SettingsEnabled = true;

        private TextBlock LblTitle = null!;
        private TextBlock LblInfo = null!;
        private Avalonia.Controls.Image AccountImage = null!;
        private Avalonia.Controls.Image AppIcon = null!;
        private Button SettingsButton = null!;
        private Button MinButton = null!;
        private Button MaxButton = null!;
        private Button CloseButton = null!;
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
                //ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*,Auto"),
                //VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(54, 54, 54)),
                ColumnDefinitions = new ColumnDefinitions("40,*,Auto,Auto,Auto,Auto,Auto,Auto"),
                Height = 50
            };

            AppIcon = new Avalonia.Controls.Image
            {
                Width = 20,
                Height = 20,
                Margin = new Thickness(10, 0),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(AppIcon, 0);

            LblTitle = new TextBlock
            {
                Foreground = Brushes.Orange,
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                FontSize = 16
            };
            Grid.SetColumn(LblTitle, 1);

            LblInfo = new TextBlock
            {
                Foreground = Brushes.Gray,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                FontSize = 13,
                Margin = new Thickness(6, 0)
            };
            Grid.SetColumn(LblInfo, 2);

            AccountImage = new Avalonia.Controls.Image
            {
                Width = 24,
                Height = 24,
                Margin = new Thickness(4, 0),
                Stretch = Avalonia.Media.Stretch.UniformToFill,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                IsVisible = true
            };
            Grid.SetColumn(AccountImage, 3);

            SettingsButton = MakeButton("⚙");
            // SettingsButton.Click += (_, _) =>
            // {
            //     if (_SettingsEnabled)
            //         OnSettingsClicked?.Invoke(this, EventArgs.Empty);
            // };
            Grid.SetColumn(SettingsButton, 4);

            MinButton = MakeButton("—");
            Grid.SetColumn(MinButton, 5);

            MaxButton = MakeButton("▢");
            Grid.SetColumn(MaxButton, 6);

            CloseButton = MakeButton("✕");
            Grid.SetColumn(CloseButton, 7);

            // LblTitle = new TextBlock
            // {
            //     Foreground = Brushes.White,
            //     VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            //     Margin = new Thickness(6, 0),
            //     FontSize = 14
            // };
            // Grid.SetColumn(LblTitle, 1);

            // LblInfo = new TextBlock
            // {
            //     Foreground = Brushes.Gray,
            //     VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            //     Margin = new Thickness(4, 0),
            //     FontSize = 12
            // };
            // Grid.SetColumn(LblInfo, 2);

            // SettingsBox = new Button
            // {
            //     Content = "⚙",
            //     Foreground = Brushes.White,
            //     Background = Brushes.Transparent,
            //     BorderBrush = Brushes.Transparent,
            //     Cursor = new Cursor(StandardCursorType.Hand),
            // };
            // SettingsBox.PointerPressed += SettingsBox_Click;
            // ToolTip.SetTip(SettingsBox, "Global settings");
            // Grid.SetColumn(SettingsBox, 3);

            // Enable dragging the window by dragging any label text
            // LblTitle.PointerPressed += BeginDrag;
            // LblInfo.PointerPressed += BeginDrag;

            panel.Children.Add(AppIcon);
            panel.Children.Add(LblTitle);
            panel.Children.Add(LblInfo);
            panel.Children.Add(AccountImage);
            panel.Children.Add(SettingsButton);
            panel.Children.Add(MinButton);
            panel.Children.Add(MaxButton);
            panel.Children.Add(CloseButton);

            Content = panel;
        }

        private Button MakeButton(string icon) =>
            new()
            {
                Content = icon,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                FontSize = 16,
                Padding = new Thickness(8, 0),
                Cursor = new Cursor(StandardCursorType.Hand),
            };

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
            _ = LoadAccountImageFromUrl(url);
        }

        public void SetAccountImage(SixLabors.ImageSharp.Image img)
        {
            using var ms = new MemoryStream();
            img.Save(ms, new PngEncoder());
            ms.Position = 0;
            AccountImage.Source = new Bitmap(ms);
        }

        private async Task LoadAccountImageFromUrl(string url)
        {
            try
            {
                using var http = new HttpClient();
                using var stream = await http.GetStreamAsync(url);
                using var img = SixLabors.ImageSharp.Image.Load(stream);
                await (ApplyImage(img));
            }
            catch (Exception ex)
            {
                ServiceSingleton.Logger.Log("Image downloading failed =>\n" + ex);
            }
        }

        private async Task ApplyImage(SixLabors.ImageSharp.Image img)
        {
            try
            {
                using var ms = new MemoryStream();
                img.Save(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                ms.Position = 0;

                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => 
                {
                    AccountImage.Source = new Avalonia.Media.Imaging.Bitmap(ms);
                });
            }
            catch (Exception ex)
            {
                ServiceSingleton.Logger.Log("Image applying failed =>\n" + ex);
            }
        }
    }
}
