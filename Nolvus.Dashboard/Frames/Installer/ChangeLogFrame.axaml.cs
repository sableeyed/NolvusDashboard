using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Dashboard.Controls;
using Nolvus.Core.Enums;
using Vcc.Nolvus.Api.Installer.Services;
using Vcc.Nolvus.Api.Installer.Library;
using Nolvus.Dashboard.Frames.Instance;
using Xilium.CefGlue;
using Xilium.CefGlue.Avalonia;

namespace Nolvus.Dashboard.Frames.Installer
{
    public partial class ChangeLogFrame : DashboardFrame
    {
        private string _FromVersion;
        private string _ToVersion;
        private string _ChangeLogUrl;
        private AvaloniaCefBrowser _Browser;
        private bool _Detached;

        public ChangeLogFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
            BtnContinue.Click += BtnContinue_Click;
            BtnPrevious.Click += BtnPrevious_Click;
        }

        protected override async Task OnLoadedAsync()
        {
            ServiceSingleton.Dashboard.Info("Change Log");

            INolvusInstance Instance = Parameters["Instance"] as INolvusInstance;

            _FromVersion = Instance.Version;
            _ToVersion = await Instance.GetLatestVersion();

            ServiceSingleton.Dashboard.Status("Loading...");

            _ChangeLogUrl = "https://www.nolvus.net/appendix/changelog?from=" + _FromVersion + "&to=" + _ToVersion;

            ShowChangeLog();
        }

        /// <summary>
        /// Renders the change logs in the frame's content panel.
        /// </summary>
        /// <remarks>
        /// The panel was always meant to hold them - it ships with nothing but a loading spinner
        /// over an empty area. While CEF was unreliable this asked the user first and handed the
        /// URL to their system browser instead, which left the spinner up for good.
        /// </remarks>
        private void ShowChangeLog()
        {
            // The version lookup ahead of this is a network call, so the user can have pressed Back
            // and left the frame by the time it returns. Nothing disposes a frame that is already
            // detached, so a browser built now would keep its CEF process for the whole session.
            if (_Detached)
                return;

            _Browser = new AvaloniaCefBrowser();

            // CEF raises these off the UI thread.
            _Browser.LoadEnd += (_, e) =>
            {
                if (e.Frame.IsMain)
                    Dispatcher.UIThread.Post(() => ChangeLogLoaded(null));
            };

            _Browser.LoadError += (_, e) =>
            {
                // Aborted is what a navigation cancelled by leaving the frame reports, which is not
                // something to tell the user about.
                if (e.Frame.IsMain && e.ErrorCode != CefErrorCode.Aborted)
                    Dispatcher.UIThread.Post(() => ChangeLogLoaded(e.ErrorText));
            };

            BrowserHost.Children.Add(_Browser);

            _Browser.Address = _ChangeLogUrl;
        }

        private void ChangeLogLoaded(string Error)
        {
            LoadingOverlay.IsVisible = false;

            ServiceSingleton.Dashboard.NoStatus();

            if (Error == null)
                return;

            // Nothing was rendered, so without this the panel is simply blank. The update itself
            // does not depend on the change logs, so this is not treated as an error.
            LblLoadError.Text = string.Format("Unable to load the change logs ({0}). You can read them at {1}", Error, _ChangeLogUrl);
            LblLoadError.IsVisible = true;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            _Detached = true;

            var Browser = _Browser;

            _Browser = null;

            if (Browser == null)
                return;

            // Frames are dropped by the dashboard rather than disposed, so the browser is released
            // here - otherwise its CEF process outlives the frame.
            try
            {
                BrowserHost.Children.Remove(Browser);
                Browser.Dispose();
            }
            catch (Exception ex)
            {
                ServiceSingleton.Logger?.Log($"[CHANGELOG] Unable to dispose the change log browser : {ex.Message}");
            }
        }

        private async void BtnContinue_Click(object? sender, RoutedEventArgs e)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            if (await (Parameters["Instance"] as INolvusInstance).LatestPackageRequireReInstall())
            {
                bool? result = await NolvusMessageBox.ShowConfirmation(owner, "Warning", "This new version requires a new game and new installation. Do you want to proceed?");
                if (result == true)
                {
                    ServiceSingleton.Dashboard.LoadFrame<DeleteFrame>(new FrameParameters(new FrameParameter() { Key = "Instance", Value = Parameters["Instance"] as INolvusInstance }, new FrameParameter() { Key = "Action", Value = InstanceAction.Delete }));
                }
            }
            else if (await (Parameters["Instance"] as INolvusInstance).LatestPackageRequireNewGame())
            {
                bool? result = await NolvusMessageBox.ShowConfirmation(owner, "Warning", "This new version requires a new game. Your current saves will not work. Do you want to proceed?");
                if (result == true)
                {
                    StartUpdate();
                }
            }
            else
            {
                StartUpdate();
            }
        }

        private async void StartUpdate()
        {
            ServiceSingleton.Instances.WorkingInstance = (Parameters["Instance"] as INolvusInstance);
            ServiceSingleton.Instances.PrepareInstanceForUpdate();
            await ServiceSingleton.Dashboard.LoadFrameAsync<PackageFrame>();
        }

        private void BtnPrevious_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Dashboard.LoadFrame<InstancesFrame>();
        }
    }
}