using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.IO;
using System.Text;
using System.Net;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Nolvus.Dashboard.Controls;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Frames;
using Nolvus.Core.Services;
using Nolvus.Core.Enums;
using Nolvus.Package.Mods;
using System.Diagnostics;

namespace Nolvus.Dashboard.Frames
{
    public partial class ErrorFrame : DashboardFrame
    {

        private string Title
        {
            get
            {
                return Parameters["Title"].ToString();
            }
        }

        private string Message
        {
            get
            {
                return Parameters["Message"].ToString();
            }
        }

        private string Trace
        {
            get
            {
                if (Parameters["Trace"] != null)
                {
                    return Parameters["Trace"].ToString();
                }

                return string.Empty;
            }
        }

        private Func<Task> OnRetry => Parameters["OnRetry"] as Func<Task>;

        private Func<Task> OnBack => Parameters["OnBack"] as Func<Task>;

        private Func<Task> OnCancel => Parameters["OnCancel"] as Func<Task>;

        public ErrorFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();

            LblTitle.Text = Title;
            LblError.Text = Message;
            LblTrace.Text = Trace;

            BtnRetry.IsVisible = OnRetry is not null;
            BtnBack.IsVisible = OnBack is not null;
            BtnCancel.IsVisible = true;

            BtnRetry.Click += BtnRetry_Click;
            BtnBack.Click += BtnBack_Click;
            BtnCancel.Click += BtnCancel_Click;

            ServiceSingleton.Dashboard.Title("Nolvus Dashboard - [Error]");
            ServiceSingleton.Dashboard.Info("Error");
        }

        private async void BtnRetry_Click(object? sender, RoutedEventArgs e) => await Navigate(OnRetry);

        private async void BtnBack_Click(object? sender, RoutedEventArgs e) => await Navigate(OnBack);

        private async void BtnCancel_Click(object? sender, RoutedEventArgs e)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;

            bool? result = await NolvusMessageBox.ShowConfirmation(owner, "Cancel", "Are you sure you want to cancel?");

            if (result is true)
                await Navigate(OnCancel ?? DefaultCancel);
        }

        private Task DefaultCancel() => ServiceSingleton.Dashboard.LoadFrameAsync<StartFrame>();

        private async Task Navigate(Func<Task> Action)
        {
            if (Action is null)
                return;

            DisableButtons();

            try
            {
                await Action();
            }
            catch (Exception ex)
            {
                ServiceSingleton.Logger.Log("Error frame navigation failed => " + ex.Message);
                EnableButtons();
            }
        }

        private void BtnHelp_Click(object sender, EventArgs e)
        {
            var url = "https://www.nolvus.net/appendix/installer/faq";
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
