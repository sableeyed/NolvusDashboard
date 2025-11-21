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

        private bool Retry
        {
            get
            {
                if (Parameters["Retry"] != null)
                {
                    return System.Convert.ToBoolean(Parameters["Retry"]);
                }

                return false;
            }
        }

        public ErrorFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();

            //PnlTitle.Background = ;
            LblTitle.Text = Title;
            LblError.Text = Message;
            LblTrace.Text = Trace;
            BtnRetry.IsVisible = Retry;
            ServiceSingleton.Dashboard.Title("Nolvus Dashboard - [Error]");
            ServiceSingleton.Dashboard.Info("Error");
        }

        private void BtnRetry_Click(object sender, EventArgs e)
        {
            ServiceSingleton.Dashboard.LoadFrame<ResumeFrame>();
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
