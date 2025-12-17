using Avalonia.Controls;
using Avalonia.Input;
using Xilium.CefGlue.Avalonia;

namespace Nolvus.Browser
{
    public partial class BrowserWindow : Window
    {
        public AvaloniaCefBrowser Browser { get; }

        public BrowserWindow()
        {
            InitializeComponent();

            Browser = new AvaloniaCefBrowser();
            BrowserHost.Children.Add(Browser);
            TitleBar.Title = "Nolvus Browser";
            TitleBar.CloseRequested += (_, __) => Close();
            TitleBar.PointerPressed += TitleBar_PointerPressed;
        }

        public void Navigate(string url)
        {
            Browser.Address = url;
        }

        private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        }
    }
}
