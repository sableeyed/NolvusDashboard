/*
 * Honestly by I can see I am going to have a horrible time. The worst part...?
 * Matching the old code style...
 * if(condition) {
 *  //do something
 * }
 * is superior. Having the first curly bracket on a new line is awful!
 */

using Avalonia.Controls;
using Avalonia.Threading;
using Nolvus.Dashboard.ViewModels;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Frames;
using Nolvus.Components.Controls;
using Nolvus.Core.Events;
using Nolvus.Core.Services;
using System.Dynamic;
using Avalonia.Controls.Chrome;
using Microsoft.AspNetCore.Components.Web;
using Avalonia.Input;

namespace Nolvus.Dashboard;

public partial class DashboardWindow : Window, IDashboard
{
    private int DefaultDpi = 96;
    private DashboardFrame LoadedFrame;
    //private TitleBarControl TitleBarControl; //This is in avalonia xml now
    public const int WM_NCLBUTTONDOWN = 0xA1; //?
    public const int HT_CAPTION = 0x2; //?
    private Image PicBox; //replace PictureBox since it's windoze only

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
        get { return 0.0; }
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
        //:worrystare:
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => ShowLoadingIndicator());
            return;
        }

        //Put an image here and manipulate it
        //Picbox stuff not gonna work
    }

    private void UnloadLoadingIndicator()
    {
        //:worrystare:
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => ShowLoadingIndicator());
            return;
        }

        //Put an image here and manipulate it
        //Picbox stuff not gonna work
    }

    private void AddFrame(DashboardFrame Frame)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => AddFrame(Frame));
            return;
        }

        //ContentPanel.Controls.Add(Frame);
        LoadedFrame = Frame;
    }

    private void DoLoad(DashboardFrame Frame)
    {
        // Frame.Height = 0; //TODO
        // Frame.Width = 0; //TODO
        // Frame.Anchor = 0; //TODO
        // //Must implement Core.Frames.DashboardFrame
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
        }
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

        this.TitleBarControl.Title = Value;
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

        //LblStatus.IsVisible = false;
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

        // StatusStripEx.Visible = true;
        // StStripLblAdditionalInfo.Text = Value;
    }

    public void AdditionalInfo(string Value)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => AdditionalInfo(Value));
            return;
        }

        // StatusStripEx.Visible = true;
        // StStripLblAdditionalInfo.Text = Value;
    }

    public void AdditionalSecondaryInfo(string Value)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => AdditionalSecondaryInfo(Value));
            return;
        }

        // StatusStripEx.Visible = true;
        // StStripLblAdditionalInfo2.Text = Value;
    }

    public void AdditionalTertiaryInfo(string Value)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => AdditionalTertiaryInfo(Value));
            return;
        }

        // StatusStripEx.Visible = true;
        // StStripLblAdditionalInfo3.Text = Value;
    }

    public void ClearInfo()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => ClearInfo());
            return;
        }

        // StStripLblInfo.Text = string.Empty;
        // StStripLblAdditionalInfo.Text = string.Empty;
        // StStripLblAdditionalInfo2.Text = string.Empty;
        // StStripLblAdditionalInfo3.Text = string.Empty;
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

        // StatusStripEx.Visible = true;
        // StripLblNexus.Text = Value;
    }

    public void AccountType(string Value)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => AccountType(Value));
            return;
        }

        // StatusStripEx.Visible = true;
        // StripLblAccountType.Text = Value;
    }

    public async Task<T> LoadFrameAsync<T>(FrameParameters Parameters = null) where T : DashboardFrame
    {
        RemoveLoadedFrame();

        ShowLoadingIndicator();

        var Frame = await DashboardFrame.CreateAsync<T>(new object[] { this, Parameters });

        UnloadLoadingIndicator();

        DoLoad(Frame);

        OnFrameLoadedAsyncEvent?.Invoke(this, new EventArgs());

        return Frame;
    }

    public T LoadFrame<T>(FrameParameters Parameters = null) where T : DashboardFrame
    {
        RemoveLoadedFrame();

        ShowLoadingIndicator();

        var Frame = DashboardFrame.Create<T>(new object[] { this, Parameters });

        UnloadLoadingIndicator();

        DoLoad(Frame);

        OnFrameLoadedEvent?.Invoke(this, new EventArgs());

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
        TitleBarControl.EnableSettings();
    }

    public void DisableSettings()
    {
        TitleBarControl.DisableSettings();
    }

    #endregion



    public DashboardWindow()
    {
        InitializeComponent();
        //DataContext = new DashboardMainViewModel();

        MinimizeButton.Click += (_, _) => WindowState = WindowState.Minimized;
        CloseButton.Click += (_, _) => Close();
        MaximizeButton.Click += (_, _) =>
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
        // TitleBarControl.OnSettingsClicked += TitleBarControl_OnSettingsClicked;

        TitleBarControl.Title = "Nolvus Dashboard";
        TitleBarControl.InfoCaption = string.Format("v{0} | Not logged in", ServiceSingleton.Dashboard.Version);

        //AccountImage
        LoadAccountImage("https://www.nolvus.net/assets/images/account/user-profile.png");

        // ProgressBar.Value = 0;
        // ProgressBar.Maximum = 100;

        // IconSize = new Size((int)Math.Round(IconSize.Width * ScalingFactor), (int)Math.Round(IconSize.Height * ScalingFactor));
        // StripLblScaling.Text = "[DPI:" + this.ScalingFactor * 100 + "%" + "]";
    }


    // private TitleBarControl_OnSettingsClicked(object sender, EventArgs e)
    // {
    //     if (!ServiceSingleton.Packages.Processing)
    //     {
    //         if (TitleBarControl.SettingsEnabled)
    //             ServiceSingleton.Dashboard.LoadFrame<GlobalSettingsFrame>();
    //         else
    //             ShowError("This action can not be done now, please finish the Dashboard pre setup (Game path, Nexus and Nolvus connection)", Nolvus.Core.Enums.MessageBoxType.Error);
    //         //Wrapper for NolvusMessageBox?
    //     }
    //     else
    //     {
    //         ShowError("This action is not allowed during mod list installation!", Nolvus.Core.Enums.MessageBoxType.Error);
    //     }
    // }

    private void TitleBar_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void TitleBarControl_OnSettingsClicked(object? sender, EventArgs e)
    {
        // if (!ServiceSingleton.Packages.Processing)
        // {
        //     if (SettingsButton.IsEnabled)
        //     {
        //         Console.Out.WriteLine("Settings Pressed");
        //         //ServiceSingleton.Dashboard.LoadFrame<GlobalSettingsFrame>();
        //     }
        //     else
        //     {
        //         ServiceSingleton.Logger.Log("Error: This action cannot be done now. Please finish the Dashboard pre setup");
        //     }
        // }
        // else
        // {
        //     ServiceSingleton.Logger.Log("Settings: This action is not allowed during mod list installation!");
        // }
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
        //await LoadFrameAsync<StartFrame>();
    }


}
