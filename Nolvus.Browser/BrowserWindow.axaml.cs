using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Xilium.CefGlue.Avalonia;

namespace Nolvus.Browser
{
    public partial class BrowserWindow : Window
    {
        private readonly AvaloniaCefBrowser ChromeBrowser;

        public BrowserWindow(string initialUrl)
        {
            InitializeComponent();

            TitleBar.Title = "Nolvus Browser";
            TitleBar.CloseRequested += (_, __) => Close();
            TitleBar.PointerPressed += OnTitleBarPointerPressed;

            ChromeBrowser = new AvaloniaCefBrowser
            {
                Address = initialUrl
            };

            BrowserHost.Children.Add(ChromeBrowser);

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
                Dispatcher.UIThread.Post(() => {TitleBar.Title = Title;});
            };
        }

        private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        }
    }
}
