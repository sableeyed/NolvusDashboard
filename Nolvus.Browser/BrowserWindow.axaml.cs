using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Xilium.CefGlue.Avalonia;
using Nolvus.Browser.Core;
using Nolvus.Core.Services;

namespace Nolvus.Browser
{
    public partial class BrowserWindow : Window
    {
        private readonly AvaloniaCefBrowser ChromeBrowser;
        private readonly Nolvus.Browser.Core.Browser BrowserEngine;

        public Nolvus.Browser.Core.Browser Engine => BrowserEngine;

        private string _initialUrl;

        public BrowserWindow(string initialUrl)
        {
            InitializeComponent();

            _initialUrl = initialUrl;

            TitleBar.Title = "Nolvus Browser";
            TitleBar.CloseRequested += (_, __) => Close();
            TitleBar.PointerPressed += OnTitleBarPointerPressed;

            ChromeBrowser = new AvaloniaCefBrowser();

            BrowserEngine = new Nolvus.Browser.Core.Browser(ChromeBrowser);

            BrowserHost.Children.Add(ChromeBrowser);

            this.Opened += OnOpened;

            this.Closed += (_, __) =>
            {
                try
                {
                    ChromeBrowser.Dispose();
                }
                catch { }
            };

            /* SUBSCRIBE TO UPDATES FROM THE BACKEND SO WE CAN UPDATE UI COMPONENTS */
            ChromeBrowser.TitleChanged += (_, Title) =>
            {
                Dispatcher.UIThread.Post(() => { TitleBar.Title = Title; });
            };

            // BrowserEngine.HideLoadingRequested += () =>
            // {
            //     Dispatcher.UIThread.Post(HideLoading);
            // };
        }
        
        private void OnOpened(object? sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_initialUrl))
            {
                Engine.Navigate(_initialUrl);
            }
        }

        private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        }

        //UNUSED
        private void ShowLoading()
        {
            LoadingOverlay.IsVisible = true;
            BrowserHost.IsVisible = false;
        }

        private void HideLoading()
        {
            LoadingOverlay.IsVisible = false;
            BrowserHost.IsVisible = true;
        }
    }
}
