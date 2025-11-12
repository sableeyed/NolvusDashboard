using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Nolvus.Browser.Core;
using Nolvus.Core.Events;
using Nolvus.Core.Interfaces;
using Xilium.CefGlue.Avalonia;

namespace Nolvus.Browser
{
    public partial class BrowserWindow : Window, IBrowserInstance
    {
        private ChromiumDownloader? _downloader;

        public BrowserWindow()
        {
            InitializeComponent();

            TitleBar.Title = "Nolvus Browser";
            TitleBar.CloseRequested += (_, __) => Close();
            TitleBar.PointerPressed += (_, e) =>
            {
                if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                    BeginMoveDrag(e);
            };

            //BrowserHost.Children.Add(_browser);
        }
        protected override void OnClosed(EventArgs e)
        {

            

            base.OnClosed(e);
        }

        
        /*
         * IBrowserInstance Interface
         */

        public async Task AwaitUserDownload(string Link, string FileName, DownloadProgressChangedHandler Progress)
        {
            await (LoadBrowser(Link, Progress) as ChromiumDownloader).AwaitDownload(FileName);
            CloseBrowser();
        }

        public async Task<string> GetNexusManualDownloadLink(string ModName, string Link, string NexusModId)
        {
            TitleBar.Title = "Manual download [" + ModName + "]";
            return await (LoadBrowser(Link, true) as ChromiumDownloader).AwaitDownloadLink(NexusModId);
        }

        public async Task NexusSSOAuthentication(string Id, string Slug)
        {
            //CanClose = true;
            TitleBar.Title = "Nexus SSO Authentication";
            await (LoadBrowser(string.Format("https://www.nexusmods.com/sso?id={0}&application={1}", Id, Slug), false) as ChromiumDownloader).AwaitNexusSSOAuthentication();
        }

        public void Navigate(string Link, string Title = null)
        {
            TitleBar.Title = Title;
            LoadBrowser(Link);
        }

        public void CloseBrowser()
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (_downloader?.Browser != null)
                {
                    BrowserHost.Children.Remove(_downloader.Browser);
                    _downloader.Browser.Dispose();
                }

                _downloader = null;
                OnBrowserClosed?.Invoke(this, EventArgs.Empty);
            });
        }

        public event OnBrowserClosedHandler? OnBrowserClosed;

        public ChromiumDownloader LoadBrowser(string url, DownloadProgressChangedHandler? progress = null)
        {
            // Dispose previous browser instance if any
            if (_downloader?.Browser != null)
            {
                BrowserHost.Children.Remove(_downloader.Browser);
                _downloader.Browser.Dispose();
            }

            _downloader = new ChromiumDownloader(url, LinkOnly: false, progress);
            _downloader.OnFileDownloadRequest += Downloader_OnFileDownloadRequest;

            _downloader.CreateBrowser();
            _downloader.HideLoadingRequested += () => Dispatcher.UIThread.Post(HideLoading);
            _downloader.PageInfoChanged += url => Dispatcher.UIThread.Post(() => SetInfo(url));
            _downloader.NavigationRequested += url => Dispatcher.UIThread.Post(() => LoadUrl(url));
            BrowserHost.Children.Add(_downloader.Browser);

            return _downloader;
        }

        public ChromiumDownloader LoadBrowser(string url, bool linkOnly)
        {
            if (_downloader?.Browser != null)
            {
                BrowserHost.Children.Remove(_downloader.Browser);
                _downloader.Browser.Dispose();
            }

            _downloader = new ChromiumDownloader(url, linkOnly, null);
            _downloader.OnFileDownloadRequest += Downloader_OnFileDownloadLinkRequest;

            _downloader.CreateBrowser();
            BrowserHost.Children.Add(_downloader.Browser);

            return _downloader;
        }

        public ChromiumDownloader LoadBrowser(string url)
        {
            if (_downloader?.Browser != null)
            {
                BrowserHost.Children.Remove(_downloader.Browser);
                _downloader.Browser.Dispose();
            }

            _downloader = new ChromiumDownloader(url, LinkOnly: false, OnProgress: null);

            _downloader.CreateBrowser();
            BrowserHost.Children.Add(_downloader.Browser);

            return _downloader;
        }

        private void HideLoading()
        {

        }

        private void SetInfo(string Url)
        {

        }
        
        private void LoadUrl(string Url)
        {
            _downloader.Browser.Address = Url;
        }

        private void Downloader_OnFileDownloadRequest(object sender, FileDownloadRequestEvent e)
        {
            // Show progress UI, enable cancel, etc.
        }

        private void Downloader_OnFileDownloadLinkRequest(object sender, FileDownloadRequestEvent e)
        {
            // This is used when capturing the manual Nexus link
        }
    }
}
