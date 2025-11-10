using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Xilium.CefGlue.Avalonia;

namespace Nolvus.Browser
{
    public partial class BrowserWindow : Window
    {
        private AvaloniaCefBrowser? _browser;

        public BrowserWindow()
        {
            InitializeComponent();

            _browser = new AvaloniaCefBrowser
            {
                Address = "https://www.google.com"
            };

            BrowserHost.Children.Add(_browser);

            GoButton.Click += OnGoClick;
            DevToolsButton.Click += OnDevToolsClick;
        }

        private void OnGoClick(object? sender, RoutedEventArgs e)
        {
            if (_browser == null) return;
            var url = AddressBox.Text;

            if (!string.IsNullOrWhiteSpace(url))
                _browser.Address = url!;
        }

        private void OnDevToolsClick(object? sender, RoutedEventArgs e)
        {
            _browser?.ShowDeveloperTools();
        }

        protected override void OnClosed(EventArgs e)
        {
            _browser?.Dispose();
            _browser = null;

            base.OnClosed(e);
        }
    }
}
