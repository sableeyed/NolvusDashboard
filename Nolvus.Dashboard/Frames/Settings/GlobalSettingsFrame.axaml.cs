using Avalonia.Interactivity;
using Avalonia.Controls;
using Nolvus.Core.Enums;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Dashboard.Controls;
using Avalonia.Threading;
using System.Diagnostics;
using Avalonia;
using Avalonia.Media;

namespace Nolvus.Dashboard.Frames.Settings
{
    public partial class GlobalSettingsFrame : DashboardFrame
    {
        public GlobalSettingsFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();

            BtnBack.Click += BtnBack_Click;
            BtnSaveMegaInfo.Click += BtnSaveMegaInfo_Click;
            BtnCLearLogs.Click += BtnCLearLogs_Click;
            BtnClearCache.Click += BtnClearCache_Click;
        }

        protected override void OnLoad()
        {
            try
            {
                ServiceSingleton.Dashboard.Title("Nolvus Dashboard - [Settings]");
                ServiceSingleton.Dashboard.Info("Global Settings");

                TxtBxUserName.Text = ServiceSingleton.Globals.MegaEmail;
                TxtBxPassword.Text = ServiceSingleton.Globals.MegaPassword;

                TglBtnAnonymous.IsChecked = true;

                if (!ServiceSingleton.Globals.MegaAnonymousConnection)
                {
                    TglBtnAnonymous.IsChecked = false;
                }

                DrpDwnLstProcessCount.ItemsSource = GetProcessorCores();
                DrpDwnLstProcessCount.SelectedIndex = CoreIndex(GetProcessorCores());
                
                TglBtnAutoClick.IsChecked = ServiceSingleton.Settings.NexusAutoClick;
            }
            catch (Exception ex)
            {
                ServiceSingleton.Dashboard.Error("Error during global settings loading", ex.Message, ex.StackTrace);
            }
        }

        private int CoreIndex(List<int> Cores)
        {
            var Index = Cores.FindIndex(x => x == ServiceSingleton.Settings.ProcessCount);

            return Index == -1 ? 0 : Index;
        }

        protected List<int> GetProcessorCores()
        {
            List<int> Result = new List<int>();

            for (var i = 0; i < Environment.ProcessorCount; i++)
            {
                Result.Add(i + 1);
            }

            if (Result.Where(x => x == ServiceSingleton.Settings.ProcessCount).ToList().Count == 0)
            {
                Result.Add(ServiceSingleton.Settings.ProcessCount);
            }

            return Result;
        }

        private async void BtnBack_Click(object? sender, RoutedEventArgs e)
        {
            var window = TopLevel.GetTopLevel(this) as DashboardWindow;
            var returnType = window?.SettingsReturnFrameType;
            var returnParameters = window?.SettingsReturnFrameParameters;

            if (returnType != null && typeof(DashboardFrame).IsAssignableFrom(returnType))
            {
                var method = typeof(IDashboard).GetMethod("LoadFrame")!.MakeGenericMethod(returnType);
                method.Invoke(ServiceSingleton.Dashboard, new object?[] { returnParameters });
            }
            else
            {
                await ServiceSingleton.Dashboard.LoadFrameAsync<StartFrame>();
            }
        }

        private async void BtnSaveMegaInfo_Click(object? sender, RoutedEventArgs e)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            if (TglBtnAnonymous.IsChecked == true && TxtBxUserName.Text.Trim() == string.Empty && TxtBxPassword.Text.Trim() == string.Empty)
            {
                NolvusMessageBox.Show(owner, "Error", "Please enter your credentials!", MessageBoxType.Error);
            }
            else
            {
                try
                {
                    if (TglBtnAnonymous.IsChecked == false)
                    {
                        UpdateButton("Validating...");
                        await ServiceSingleton.Files.AuthenticateToMegaApi(TxtBxUserName.Text, TxtBxPassword.Text);
                    }
                }
                catch
                {
                    UpdateButton("Save");
                    NolvusMessageBox.Show(owner, "Error", "Unable to connect to mega.nz! Please review your credentials", MessageBoxType.Error);
                    return;
                }

                try
                {
                    ServiceSingleton.Globals.MegaAnonymousConnection = TglBtnAnonymous.IsChecked == true;

                    if (TxtBxUserName.Text != string.Empty)
                    {
                        ServiceSingleton.Globals.MegaEmail = TxtBxUserName.Text;
                    }

                    if (TxtBxPassword.Text != string.Empty)
                    {
                        ServiceSingleton.Globals.MegaPassword = TxtBxPassword.Text;
                    }
                    
                    NolvusMessageBox.Show(owner, "Information", "Your mega.nz configuration has been validating and saved", MessageBoxType.Info);
                    UpdateButton("Save");
                }
                catch (Exception ex)
                {
                    UpdateButton("Save");
                    NolvusMessageBox.Show(owner, "Error", string.Format("Unexpected error ocurred with message : {0}", ex.Message), MessageBoxType.Error);                                            
                }
            }
        }

        private void OpenMega(object? sender, RoutedEventArgs e)
        {
            var url = "https://mega.nz/register";
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

        private async Task UpdateButton(string Text)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                BtnSaveMegaInfo.Content = Text;
            });
        }

        private void TglBtnAnonymous_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ToggleSwitch.IsCheckedProperty)
            {
                bool anonymous = TglBtnAnonymous.IsChecked == true;

                // Disable when anonymous, enable when not anonymous
                TxtBxUserName.IsEnabled = !anonymous;
                TxtBxPassword.IsEnabled = !anonymous;

                if (anonymous)
                {
                    LblMegaMessage.Foreground = Brushes.Gray;
                    LblEmail.Foreground = Brushes.Gray;
                    LblPassword.Foreground = Brushes.Gray;
                }
                else
                {
                    LblMegaMessage.Foreground = Brushes.White;
                    LblEmail.Foreground = Brushes.White;
                    LblPassword.Foreground = Brushes.White;
                }
            }
        }

        private void OnProcessorCountChanged(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Settings.ProcessCount = (int)DrpDwnLstProcessCount.SelectedItem!;
        }

        private void BtnCLearLogs_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Logger.ClearLogs();
            NolvusMessageBox.Show(TopLevel.GetTopLevel(this) as Window, "Logs", "Logs have been cleaned", MessageBoxType.Info);
        }

        private async void BtnClearCache_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                await ServiceSingleton.Folders.ClearDownloadCache((s, p) =>
                {
                    ServiceSingleton.Dashboard.Status(string.Format("Clearing download cache ({0}%)...", p));
                    ServiceSingleton.Dashboard.Progress(p);
                });

                await ServiceSingleton.Folders.ClearExtractCache((s, p) =>
                {
                    ServiceSingleton.Dashboard.Status(string.Format("Clearing extract cache ({0}%)...", p));
                    ServiceSingleton.Dashboard.Progress(p);
                });
                
                var window = TopLevel.GetTopLevel(this) as Window;
                await NolvusMessageBox.Show(window, "Cache", "Cache has been cleared", MessageBoxType.Info);
            }
            finally
            {
                ServiceSingleton.Dashboard.NoStatus();
                ServiceSingleton.Dashboard.ProgressCompleted();
            }
        }

        private void TglBtnAutoClick_Changed(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Settings.NexusAutoClick = TglBtnAutoClick.IsChecked == true;
        }
    }
}