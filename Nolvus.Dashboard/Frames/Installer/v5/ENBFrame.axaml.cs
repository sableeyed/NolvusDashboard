using Nolvus.Core.Frames;
using Vcc.Nolvus.Api.Installer.Library;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Instance.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Vcc.Nolvus.Api.Installer.Services;
using Nolvus.Core.Enums;
using Nolvus.Dashboard.Core;
using Nolvus.Components.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using Nolvus.Package.Mods;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp;
using Avalonia.Threading;
using Nolvus.Dashboard.Controls;
using System.Diagnostics;
using Avalonia.Input;

namespace Nolvus.Dashboard.Frames.Installer.v5
{
    public partial class ENBFrame : DashboardFrame
    {
        public ENBFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
        }

        private void LnkCabbageTutorial_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            const string url = "https://www.nolvus.net/guide/asc/enb";

            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to open tutorial link: " + ex.Message);
            }
        }
    }
}