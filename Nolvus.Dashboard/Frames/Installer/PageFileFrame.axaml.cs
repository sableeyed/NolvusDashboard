using Avalonia.Input;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using System.Diagnostics;

namespace Nolvus.Dashboard.Frames.Installer
{
    public partial class PageFileFrame : DashboardFrame
    {
        public PageFileFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
        }

        private void LnkPageFile_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            try
            {
                var url = "https://wiki.archlinux.org/title/Swap";
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