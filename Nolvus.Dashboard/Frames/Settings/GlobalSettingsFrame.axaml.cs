using Avalonia.Interactivity;
using Avalonia.Controls;
using Nolvus.Core.Enums;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Dashboard.Controls;
using Avalonia.Threading;
using System.Diagnostics;
using Tmds.DBus.Protocol;
using System.ComponentModel;
using Avalonia;

namespace Nolvus.Dashboard.Frames.Settings
{
    public partial class GlobalSettingsFrame : DashboardFrame
    {
        public GlobalSettingsFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();

            BtnBack.Click += BtnBack_Click;
            BtnSaveMegaInfo.Click += BtnSaveMegaInfo_Click;
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
            }
            catch (Exception ex)
            {
                ServiceSingleton.Dashboard.Error("Error during global settings loading", ex.Message, ex.StackTrace);
            }
        }

        private async void BtnBack_Click(object? sender, RoutedEventArgs e)
        {
            await ServiceSingleton.Dashboard.LoadFrameAsync<StartFrame>();
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
                PnlMessage.IsEnabled = !anonymous;
            }
        }
    }
}