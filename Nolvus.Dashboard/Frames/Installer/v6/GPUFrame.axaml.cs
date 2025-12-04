using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Vcc.Nolvus.Api.Installer.Library;
using Nolvus.Core.Enums;
using Nolvus.Core.Misc;
using Vcc.Nolvus.Api.Installer.Services;
using Nolvus.Instance.Core;
using Nolvus.Dashboard.Core;
using Avalonia.Interactivity;

namespace Nolvus.Dashboard.Frames.Installer.v6
{
    public partial class GPUFrame : DashboardFrame
    {
        public GPUFrame(IDashboard dashboard, FrameParameters parameters)
            : base(dashboard, parameters)
        {
            InitializeComponent();
            BtnBack.Click += BtnBack_Click;
        }

        protected override async Task OnLoadedAsync()
        {
            var variantRequirement = Parameters["VariantRequirement"] as INolvusVariantRequirementDTO;
            var instance = ServiceSingleton.Instances.WorkingInstance;

            LblVariant.Text = instance.Performance.Variant;
            LblResolution.Text = instance.GetSelectedResolution();
            LblSREX.Text = instance.Performance.SREX == "TRUE" ? "Yes" : "No";

            var gpus = await ApiManager.Service.Installer.GetGPUs();

            GPUsGrid.ItemsSource = gpus.Select(x => new GPUObject
            {
                VRAM = x.VRAM.ToString(),
                GPU = $"{x.Vendor} {x.Name}",
                Supported = x.VRAM >= variantRequirement.VRAM,
                Image = null
            }).ToList();

            ServiceSingleton.Dashboard.Info("Supported GPUs");
        }

        private void BtnBack_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Dashboard.LoadFrameAsync<v6.PerformanceFrame>();
        }
    }
}
