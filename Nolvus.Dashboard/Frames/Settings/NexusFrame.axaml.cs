using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Nolvus.Core.Services;
using Nolvus.Core.Enums;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Dashboard.Controls;
using Nolvus.Dashboard.Core;
using Nolvus.Browser;
using Nolvus.NexusApi.SSO;
using Avalonia.Threading;
using Nolvus.NexusApi.SSO.Events;
using Nolvus.NexusApi;
using Nolvus.Core.Misc;
using Nolvus.NexusApi.SSO.Responses;
using System.Threading.Tasks;
using System.Diagnostics;
using DynamicData;


namespace Nolvus.Dashboard.Frames.Settings
{
    public partial class NexusFrame : DashboardFrame
    {

        private NexusSSOManager NexusSSOManager;
        private BrowserWindow? nexusBrowser;

        public NexusFrame(IDashboard dashboard, FrameParameters parameters)
            : base(dashboard, parameters)
        {
            InitializeComponent();

            BtnAuthenticate.Click += BtnAuthenticate_Click;
            BtnPrevious.Click += BtnPrevious_Click;
            BtnNext.Click += BtnNext_Click;

            // Disable settings until authenticated
            (TopLevel.GetTopLevel(this) as DashboardWindow)?.DisableSettings();

            if (!Program.DebugMode)
            {
                ToggleMessage(false);
                UpdateNextButtonState();
            }

            NexusSSOManager = new NexusSSOManager();

            NexusSSOManager.OnAuthenticating += NexusSSOManager_OnAuthenticating;
            NexusSSOManager.OnAuthenticated += NexusSSOManager_OnAuthenticated;
            NexusSSOManager.OnRequestError += NexusSSOManager_OnRequestError;

        }

        public async Task ChangeButtonText(string Value)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                BtnAuthenticate.Content = Value;
            });
        }

        private async Task ToggleMessage(bool Visible)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                PnlMessage.IsVisible = Visible;
                PicBox.IsVisible = Visible;
                LblMessage.IsVisible = Visible;
            });
        }

        private async Task ToggleAuthenticateButton(bool Active)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                BtnAuthenticate.IsEnabled = Active;
            });
        }

        public async Task SetReturnMessage(string Message, bool Error)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ToggleMessage(true);

                if (!Error)
                {
                    PnlMessage.Background = new SolidColorBrush(Color.FromArgb(255, 92, 184, 92));
                    //Set PicBox to Info.png
                    LblMessage.Text = Message;
                }
                else
                {
                    PnlMessage.Background = new SolidColorBrush(Color.FromArgb(255, 217, 83, 79));
                    //set to Warning-Message.png
                    LblMessage.Text = Message;
                }

            });
        }

        private async void NexusSSOManager_OnBrowserClosed(object? sender, EventArgs eventArgs)
        {
            await ChangeButtonText("Nexus SSO Authentication");
            await ToggleAuthenticateButton(true);
        }

        private async void NexusSSOManager_OnAuthenticating(object? sender, AuthenticatingEventArgs eventArgs)
        {
            string url = $"https://www.nexusmods.com/sso?id={eventArgs.Id}&application={Strings.NolvusSlug}";
            if (nexusBrowser == null)
            {
                nexusBrowser = new BrowserWindow("about:blank");
                nexusBrowser.Closed += NexusSSOManager_OnBrowserClosed;
                nexusBrowser.Show();
            }
            nexusBrowser.Engine.Navigate(url);
            await ChangeButtonText("Authenticating...");
        }

        private async void NexusSSOManager_OnRequestError(object? sender, RequestErrorEventArgs eventArgs)
        {
            await SetReturnMessage(eventArgs.Message, true);
            await ToggleAuthenticateButton(true);
        }

        private async void NexusSSOManager_OnAuthenticated(object sender, AuthenticationEventArgs EventArgs)
        {
            await ChangeButtonText("Nexus SSO Authentication");
            SettingsCache.NexusApiKey = EventArgs.ApiKey;            
            await SetReturnMessage("Authentication successful! Click on the \"Next\" button", false);
            await ToggleAuthenticateButton(false);
            UpdateNextButtonState();
            nexusBrowser?.Close();
        }

        public void ShowLoading()
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                BtnNext.Content = "Validating...";
                BtnNext.IsEnabled = false;
            });
        }

        public void HideLoading()
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                BtnNext.Content = "Next";
                BtnNext.IsVisible = true;
            });
        }
        
        private async Task<bool> NexusAuthenticate()
        {
            try
            {
                await Task.Run(() =>
                {
                    ApiManager.Init(
                        SettingsCache.NexusApiKey,
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.75 Safari/537.36", Path.GetTempPath());
                });

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void BtnPrevious_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Dashboard.LoadFrame<GameFrame>();
        }

        private void BtnNext_Click(object? sender, RoutedEventArgs e)
        {
            if (Program.DebugMode)
            {
                ServiceSingleton.Dashboard.LoadFrame<NolvusFrame>();
            }
            var owner = TopLevel.GetTopLevel(this) as Window;

            if (string.IsNullOrWhiteSpace(SettingsCache.NexusApiKey))
            {
                _ = NolvusMessageBox.Show(owner,
                    "Nexus Login Required",
                    "Please authenticate using Nexus SSO before continuing.",
                    MessageBoxType.Warning);
                return;
            }

            //WE FINALLY FINISHED NEXUS FRAME AFTER LITERAL DAYS (BLAME CEF)
            ServiceSingleton.Dashboard.LoadFrame<NolvusFrame>();
        }

        private async void BtnAuthenticate_Click(object? sender, RoutedEventArgs e)
        {
            if (!NexusSSOManager.Authenticated)
            {
                ToggleMessage(false);
                ToggleAuthenticateButton(false);

                try
                {
                    await NexusSSOManager.Connect();
                    nexusBrowser = new BrowserWindow("about:blank");
                    nexusBrowser.Closed += NexusSSOManager_OnBrowserClosed;
                    nexusBrowser.Show();
                    await NexusSSOManager.Authenticate();
                }
                catch { }
            }
            else
            {
                var owner = TopLevel.GetTopLevel(this) as Window;
                NolvusMessageBox.Show(owner, "Info", "You are already authenticated", MessageBoxType.Info);
            }
        }

        private void UpdateNextButtonState()
        {
            BtnNext.IsEnabled = !string.IsNullOrWhiteSpace(SettingsCache.NexusApiKey);
        }

        private void OpenNexusInfo(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var url = "https://www.nolvus.net/appendix/installer/requirements";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { }
        }

    }
}
