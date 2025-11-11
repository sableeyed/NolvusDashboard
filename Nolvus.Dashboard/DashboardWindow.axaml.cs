/*
 * Honestly by I can see I am going to have a horrible time. The worst part...?
 * Matching the old code style...
 * if(condition) {
 *  //do something
 * }
 * is superior. Having the first curly bracket on a new line is awful!
 */

using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Frames;
using Nolvus.Core.Enums;
using Nolvus.Components.Controls;
using Nolvus.Core.Events;
using Nolvus.Core.Services;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Nolvus.Dashboard.Core;
using Nolvus.Dashboard.Frames;
using Nolvus.Dashboard.Controls;
using Nolvus.Browser;

namespace Nolvus.Dashboard;

public partial class DashboardWindow : Window, IDashboard
{
    private int DefaultDpi = 96;
    private DashboardFrame LoadedFrame;
    public const int WM_NCLBUTTONDOWN = 0xA1; //?
    public const int HT_CAPTION = 0x2; //?
    private Image PicBox; //replace PictureBox since it's windoze only
    private ScaleTransform? _scale;

    #region Events

    event OnFrameLoadedHandler OnFrameLoadedEvent; //

    public event OnFrameLoadedHandler OnFrameLoaded
    {
        add
        {
            if (OnFrameLoadedEvent != null)
            {
                lock (OnFrameLoadedEvent)
                {
                    OnFrameLoadedEvent += value;
                }
            }
            else
            {
                OnFrameLoadedEvent = value;
            }
        }
        remove
        {
            if (OnFrameLoadedEvent != null)
            {
                lock (OnFrameLoadedEvent)
                {
                    OnFrameLoadedEvent -= value;
                }
            }
        }
    }


    event OnFrameLoadedHandler OnFrameLoadedAsyncEvent;
        public event OnFrameLoadedHandler OnFrameLoadedAsync
    {
        add
        {
            if (OnFrameLoadedAsyncEvent != null)
            {
                lock (OnFrameLoadedAsyncEvent)
                {
                    OnFrameLoadedAsyncEvent += value;
                }
            }
            else
            {
                OnFrameLoadedAsyncEvent = value;
            }
        }
        remove
        {
            if (OnFrameLoadedAsyncEvent != null)
            {
                lock (OnFrameLoadedAsyncEvent)
                {
                    OnFrameLoadedAsyncEvent -= value;
                }
            }
        }
    }

    #endregion

    #region Properties

    //CreateGraphics is the original system call so that won't work on linux
    public double ScalingFactor
    {
        //get { return 0.0; }
        get
        {
            double systemScale = 1.0;
            if (VisualRoot is TopLevel tl)
                return tl.RenderScaling;
            return systemScale * SettingsCache.UiScaleMultiplier;
        }
    }
    private void ApplyScaling(double scale)
    {
        if (RootScaleHost?.LayoutTransform is ScaleTransform st)
        {
            st.ScaleX = scale;
            st.ScaleY = scale;
        }
        else if (RootScaleHost is not null)
        {
            RootScaleHost.LayoutTransform = new ScaleTransform(scale, scale);
        }

        StripLblScaling.Text = $"[DPI: {(int)(scale * 100)}%]";
        if (Math.Abs(ScalingSlider.Value - scale) > 0.001)
            ScalingSlider.Value = scale;
    }


    //Not giving it an exe for linux lol
    private string DashboardExe
    {
        get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NolvusDashboard"); }
    }

    #endregion


    #region UI Methods

    private void ShowLoadingIndicator()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(ShowLoadingIndicator);
            return;
        }

        LoadingOverlay.IsVisible = true;
    }

    private void UnloadLoadingIndicator()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(UnloadLoadingIndicator);
            return;
        }

        LoadingOverlay.IsVisible = false;
    }

    private void AddFrame(DashboardFrame Frame)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => AddFrame(Frame));
            return;
        }

        //ContentPanel.Controls.Add(Frame);
        ContentHost.Content = Frame;
        LoadedFrame = Frame;
    }

    private void RemoveLoadedFrame()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => RemoveLoadedFrame());
            return;
        }

        if (LoadedFrame != null)
        {
            LoadedFrame.Close();
            LoadedFrame = null;
        }

        ContentHost.Content = null;
    }

    public void SetMaximumProgress(int Value)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => SetMaximumProgress(Value));
            return;
        }

        // this.ProgressBar.Maximum = Value;
    }

    public void Title(string Value)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => Title(Value));
            return;
        }

        TitleBarControl.Title = Value;
    }

    #endregion

    #region Interface Implementation

    public bool IsOlder(string LatestVersion)
    {
        string[] v1List = LatestVersion.Split(new char[] { '.' });
        string[] v2List = Version.Split(new char[] { '.' });

        for (int i = 0; i < v1List.Length; i++)
        {
            int _v1 = System.Convert.ToInt16(v1List[i]);
            int _v2 = System.Convert.ToInt16(v2List[i]);

            if (_v1 > _v2)
            {
                return true;
            }
            else if (_v1 < _v2)
            {
                return false;
            }
        }

        return false;
    }        
    public string Version
    {
        get
        {
            return ServiceSingleton.Globals.GetVersion(DashboardExe);
        }
    }
    public void LoadAccountImage(string Url)
    {

        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => LoadAccountImage(Url));
            return;
        }

        TitleBarControl.SetAccountImage(Url);
    }

    public void LoadAccountImage(SixLabors.ImageSharp.Image Image)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => LoadAccountImage(Image));
            return;
        }

        TitleBarControl.SetAccountImage(Image);
    }

    public void Status(string Value)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => Status(Value));
            return;
        }

        LblStatus.IsVisible = true;
        LblStatus.Text = Value;
    }

    public void NoStatus()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(NoStatus);
            return;
        }

        LblStatus.IsVisible = false;
        LblStatus.Text = string.Empty;
    }


    public void Progress(int Value)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => Progress(Value));
            return;
        }

        // this.ProgressBar.Visible = true;
        // this.ProgressBar.Value = true;
    }

    public void ProgressCompleted()
    {
        Progress(0);
    }

    public void Info(string Value)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => Info(Value));
            return;
        }

        StStripLblAdditionalInfo.IsVisible = true;
        StStripLblAdditionalInfo.Text = Value;
    }

    public void AdditionalInfo(string Value)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => AdditionalInfo(Value));
            return;
        }

        StStripLblAdditionalInfo.IsVisible = true;
        StStripLblAdditionalInfo.Text = Value;
    }

    public void AdditionalSecondaryInfo(string Value)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => AdditionalSecondaryInfo(Value));
            return;
        }

        StStripLblAdditionalInfo2.IsVisible = true;
        StStripLblAdditionalInfo2.Text = Value;
    }

    public void AdditionalTertiaryInfo(string Value)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => AdditionalTertiaryInfo(Value));
            return;
        }

        StStripLblAdditionalInfo3.IsVisible = true;
        StStripLblAdditionalInfo3.Text = Value;
    }

    public void ClearInfo()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => ClearInfo());
            return;
        }

        StStripLblInfo.Text = string.Empty;
        StStripLblAdditionalInfo.Text = string.Empty;
        StStripLblAdditionalInfo2.Text = string.Empty;
        StStripLblAdditionalInfo3.Text = string.Empty;
    }

    public void TitleInfo(string Value)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => TitleInfo(Value));
            return;
        }

        TitleBarControl.InfoCaption = "v" + ServiceSingleton.Dashboard.Version + " | " + Value;
    }

    public void NexusAccount(string Value)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => NexusAccount(Value));
            return;
        }

        StripLblNexus.IsVisible = true;
        StripLblNexus.Text = Value;
    }

    public void AccountType(string Value)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => AccountType(Value));
            return;
        }

        StripLblAccountType.IsVisible = true;
        StripLblAccountType.Text = Value;
    }

    public async Task<T> LoadFrameAsync<T>(FrameParameters Parameters = null) where T : DashboardFrame
    {
        RemoveLoadedFrame();

        ShowLoadingIndicator();

        //await Task.Delay(5000);

        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

        var Frame = await DashboardFrame.CreateAsync<T>(new object[] { this, Parameters });

        UnloadLoadingIndicator();

        ContentHost.Content = Frame;
        LoadedFrame = Frame;

        OnFrameLoadedEvent?.Invoke(this, EventArgs.Empty);
        OnFrameLoadedAsyncEvent?.Invoke(this, EventArgs.Empty);

        return Frame;
    }

    public T LoadFrame<T>(FrameParameters Parameters = null) where T : DashboardFrame
    {
        RemoveLoadedFrame();

        ShowLoadingIndicator();

        var Frame = DashboardFrame.Create<T>(new object[] { this, Parameters });

        UnloadLoadingIndicator();

        ContentHost.Content = Frame;
        LoadedFrame = Frame;

        OnFrameLoadedEvent?.Invoke(this, EventArgs.Empty);
        OnFrameLoadedAsyncEvent?.Invoke(this, EventArgs.Empty);

        return Frame;
    }

    public async Task Error(string Title, string Message, string Trace = null, bool Retry = false)
    {
        UnloadLoadingIndicator();

        ServiceSingleton.Dashboard.NoStatus();
        ServiceSingleton.Dashboard.ProgressCompleted();
        ServiceSingleton.Logger.Log("Error Form => " + Message);

        // await LoadFrameAsync<ErrorFrame>(new FrameParameters(FrameParameter.Create("Title", Title), FrameParameter.Create("Message", Message),
        //     FrameParameter.Create("Trace", Trace), FrameParameter.Create("Restry", Retry)));

    }

    public void ShutDown()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => ShutDown());
            return;
        }

        Close(); //avalonia version?
    }

    public void EnableSettings()
    {
        Console.WriteLine("Settings Enabled");
        TitleBarControl.EnableSettings();
    }

    public void DisableSettings()
    {
        Console.WriteLine("Settings Disabled");
        TitleBarControl.DisableSettings();
    }

    #endregion



    public DashboardWindow()
    {
        InitializeComponent();

        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        };


        ServiceSingleton.RegisterService<IDashboard>(this);
        ServiceSingleton.Logger.Log("You are running a currently non-functional Linux build of the Nolvus Dashboard");


        StStripLblInfo.Text = string.Empty;
        StStripLblAdditionalInfo.Text = string.Empty;
        StStripLblAdditionalInfo2.Text = string.Empty;
        StStripLblAdditionalInfo3.Text = string.Empty;
        StripLblAccountType.Text = string.Empty;
        StripLblNexus.Text = string.Empty;

        //Padding = new Padding(0, 50, 0, 0);


        // TitleBarControl = new TitleBarControl();
        //TitleBarControl.Width = 3000;
        // TitleBarControl.MouseDown += TitleBarControl_MouseDown;
        // TitleBarTextControl = TitleBarControl;
        
        TitleBarControl.OnSettingsClicked += TitleBarControl_OnSettingsClicked;
        TitleBarControl.Title = "Nolvus Dashboard";
        TitleBarControl.InfoCaption = string.Format("v{0} | Not logged in", ServiceSingleton.Dashboard.Version);
        var uri = new Uri("avares://NolvusDashboard/Assets/nolvus-ico.jpg");
        var logo = AssetLoader.Open(uri);
        if (logo != null)
        {
            TitleBarControl.SetAppIcon(new Bitmap(logo));
        }

        //LoadAccountImage("https://www.nolvus.net/assets/images/account/user-profile.png");

        //ProgressBar.Value = 0;
        //ProgressBar.Maximum = 100;

        StripLblScaling.Text = "[DPI:" + this.ScalingFactor * 100 + "%" + "]";
        DashboardProgressBar.IsVisible = false;
        LblStatus.IsVisible = false;

        Nolvus.Browser.Browser.InitCefIfNeeded("/tmp/nolvus_cef_cache");

    }

    private void ScalingSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        //ApplyScaling(e.NewValue);
        StripLblScaling.Text = $"[DPI: {(int)(e.NewValue * 100)}%]";
    }
    
    private void ScalingSlider_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is Slider s)
            ApplyScaling(s.Value);
    }

    private void ScalingSlider_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        // Covers the case where the user releases outside the slider
        if (sender is Slider s)
            ApplyScaling(s.Value);
    }


    private void Window_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void TitleBarControl_OnSettingsClicked(object? sender, EventArgs e)
    {   
        Console.WriteLine("Settings Clicked");
        if (!ServiceSingleton.Packages.Processing) 
        {
            if (TitleBarControl.SettingsEnabled) 
            {
                //ServiceSingleton.Dashboard.LoadFrame<GlobalSettingsFrame>();
                Console.WriteLine("TODO: Load Global Settings Frame");
            }
            else
            {   
                var owner = TopLevel.GetTopLevel(this) as Window;
                NolvusMessageBox.Show(owner, "Error", "This action can not be done now, please finish the Dashboard pre setup (Game path, nexus and Nolvus Connection)", MessageBoxType.Error);
            }
        }
        else
        {
            Console.WriteLine("Settings not allowed during modlist installation!");
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (ServiceSingleton.Packages.Processing)
        {
            // For now: block close (we’ll replace with Avalonia dialog later)
            e.Cancel = true;
        }

        base.OnClosing(e);
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            //Console.WriteLine("Frame creation disabled while debugging");
            await LoadFrameAsync<StartFrame>();
        });
    }
}
