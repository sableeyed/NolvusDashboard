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
        }
        protected override void OnClosed(EventArgs e)
        {
            _browser?.Dispose();
            _browser = null;

            base.OnClosed(e);
        }
    }
}
