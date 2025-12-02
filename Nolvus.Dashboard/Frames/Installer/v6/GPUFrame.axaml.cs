using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Vcc.Nolvus.Api.Installer.Library;
using Nolvus.Core.Enums;
using Nolvus.Core.Misc;
using Vcc.Nolvus.Api.Installer.Services;
using Nolvus.Instance.Core;
using Nolvus.Dashboard.Core;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Interactivity;

namespace Nolvus.Dashboard.Frames.Installer.v6
{
    public partial class GPUFrame : DashboardFrame
    {
        public GPUFrame(IDashboard Dashboard, FrameParameters Params) : base(Dashboard, Params)
        {
            InitializeComponent();
            BtnBack.Click += BtnBack_Click;
        }

        protected override async Task OnLoadedAsync()
        {
            INolvusVariantRequirementDTO VariantRequirement = Parameters["VariantRequirement"] as INolvusVariantRequirementDTO;
            INolvusInstance Instance = ServiceSingleton.Instances.WorkingInstance;

            LblVariant.Text = Instance.Performance.Variant;
            LblResolution.Text = Instance.GetSelectedResolution();
            LblSREX.Text = Instance.Performance.SREX == "TRUE" ? "Yes" : "No";

            var GPUs = await ApiManager.Service.Installer.GetGPUs();

            GPUsGrid.ItemsSource = GPUs.Select(x =>
            {
                return new GPUObject()
                {
                    VRAM = x.VRAM.ToString(),
                    GPU = string.Format("{0} {1}", x.Vendor, x.Name),
                    Supported = x.VRAM >= VariantRequirement.VRAM ? true : false,
                    Image = x.VRAM >= VariantRequirement.VRAM
                            ? LoadIconBytes("avares://NolvusDashboard/Assets/Check_01_24.png")
                            : LoadIconBytes("avares://NolvusDashboard/Assets/Cross_Close_24.png")
                };
            }).ToList();

            ServiceSingleton.Dashboard.Info("Supported GPUs");
        }

        private void BtnBack_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Dashboard.LoadFrameAsync<v6.PerformanceFrame>();
        }

        private byte[] LoadIconBytes(string assetPath)
        {
            using var stream = AssetLoader.Open(new Uri(assetPath));
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
    }
}